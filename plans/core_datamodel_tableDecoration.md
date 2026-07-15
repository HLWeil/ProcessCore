# Core Datamodel Table Decoration

## Overview

The core datamodel represents experimental workflows as a process graph. The same information can also be expressed in a tabular format, where each table corresponds to a group of process nodes executing the same recipe step. This plan describes a set of types and APIs that provide a tabular view of the core datamodel — not a separate datamodel with its own storage, but a live projection of the underlying process graph.

**Reference documents:**
- [Conversion Specification](../references/TabularAndProcessConversion.md) — defines how processes map to table rows/columns and vice versa
- [Tabular Datamodel Types](../references/Tabular/ArcTableTypes.md) — reference type definitions (`IOType`, `CompositeHeader`, `CompositeCell`, `CompositeColumn`)
- [Tabular Datamodel API](../references/Tabular/ArcTableAPI.md) — reference API for `ArcTable` and `ArcTables`
- [Core Datamodel Implementation](../schemas/inmemory/ArcDataModel.fsproj)

---

## Design Principles

- The table layer sits **on top of** the core datamodel. `Table` holds references to the underlying `Process` nodes; edits to the table mutate those nodes directly, and changes to the graph are visible through the table.
- Types and API conventions follow the existing tabular datamodel (`ArcTable`, `ArcTables`) with adjustments to fit the core datamodel structure and semantics.
- All table types live in the namespace `ProcessCore.Table` and operate on entities from `ProcessCore`.

---

## Structural Mapping

### Grouping processes into tables

A `Dataset`'s processes are grouped into `Table` objects by **name**: all `Process` nodes in the dataset that share the same name belong to the same table. Every process is exactly one visible row. Consequently `Table.RowCount = Processes.Count`, and the visible row index addresses the process at the same index.

### Column roles and graph slots

Each column in a table corresponds to a consistently-typed slot across all process nodes in the group. The mapping is defined in full in the [Conversion Specification](../references/TabularAndProcessConversion.md#one-column--one-slot-across-all-process-nodes):

| Column role | Graph slot |
|---|---|
| `Input` | Name of the input entity node |
| `Output` | Name of the output entity node |
| `Parameter` | `Annotation` on the process node, `AdditionalType = "ParameterValue"` |
| `Factor` | `Annotation` on the output entity, `AdditionalType = "FactorValue"` |
| `Characteristic` | `Annotation` on the input entity, `AdditionalType = "CharacteristicValue"` |
| `Component` | `Annotation` in the recipe's `Components`, `AdditionalType = "Component"` |
| `ProtocolREF` | `Recipe.Name` |
| `ProtocolType` | `Recipe.IntendedUse` |
| `ProtocolDescription` | `Recipe.Description` |
| `ProtocolUri` | `Recipe.Url` |
| `ProtocolVersion` | `Recipe.Version` |
| `Performer` | `Process` performer field (to be added) |
| `Comment` | `Process` comments collection (to be added) |

### Column ordering

Because the process graph does not preserve column order, every annotation `Annotation` (Parameter, Factor, Characteristic, Component) must carry `ColumnIndex` as extensible `DynamicObj` metadata, accessed through helper functions in the table module rather than as a core `Annotation` field. This stores the column's ordinal position **within annotation columns only** (Input / Output / Protocol / Comment columns are not counted). During **compose** (table → processes), the column index is written from the column's position. During **decompose** (processes → table), annotation values are sorted by this metadata before assembling a row; values without an index are placed last. The fixed full column order on decompose is:

1. Input
2. ProtocolREF, ProtocolType, ProtocolDescription, ProtocolUri, ProtocolVersion
3. Characteristics (sorted by `ColumnIndex`)
4. Components (sorted by `ColumnIndex`)
5. Parameters (sorted by `ColumnIndex`)
6. Factors (sorted by `ColumnIndex`)
7. Comments
8. Output

---

### Singular process rows

The public table row index is the underlying `Process` index. A row consists of one process, its optional singular `Input`, and its optional singular `Output`; there is no projected-row index or lane expansion.

`Decompose`, `GetRow`, `GetCellAt`, and `TryGetCellAt` read directly from those endpoints. Blank input/output cells represent `None`. Endpoint writes use `SetInput`/`SetOutput` or `ClearInput`/`ClearOutput`, so canonicalization and back-edges remain correct. Characteristic and Factor writes target the same row process's input and output respectively.

`AddRow` constructs one process, `UpdateRow` mutates one process without changing process count, and `RemoveRow` removes one process. No table operation clones a process by lane, sampleizes multiple endpoints, or preserves sibling projections because such state cannot exist in the core model.

---

## Required Changes to the Core Datamodel

(Skip performer and comments fields for now. Skip sorting of annotation columns across header types for now.)

- **`Annotation`**: Do not add a dedicated `ColumnIndex` field. Preserve annotation column order through `DynamicObj` metadata and table-module helper functions.
- **`Process`**: Add a `Performer` field and a `Comments` collection to support the corresponding column roles.

---

## Implementation Details

### `Table` type

- Wraps a `ResizeArray<Process>` — the processes it represents.
- Exposes `Name`, `ColumnCount`, `RowCount`, `Headers` (`ResizeArray<CompositeHeader>`).
- Provides full cell, column, row, and protocol column APIs following the `ArcTable` reference.
- `AddColumn` and cell/row update APIs handle `Input`, `Output`, `ProtocolREF`, `ProtocolType`, `ProtocolDescription`, `ProtocolUri`, and `ProtocolVersion` as writable roles. `RemoveColumn` clears Input/Output endpoints and removes annotation columns; removing a protocol header is currently a no-op, while protocol values are cleared or replaced through row/cell writes.
- Adding/removing a row creates/removes exactly one corresponding `Process` node in the parent `Dataset`; endpoint nodes are registered or evicted through normal dataset graph maintenance.
- Modifying a cell updates the corresponding `Annotation`, entity name, or recipe field on the underlying process nodes.
- Missing Input or Output: if a table has no Input column but has Characteristic columns, a synthetic input entity is created per row (named `<tableName>_<rowIndex>`). The same applies symmetrically for Output and Factor columns.
- Singular I/O: each row reads and writes only `Process.Input` and `Process.Output`; missing endpoints remain blank cells/`None`.
- Recipe multiplicity: each process node references its own copy of a recipe object. Protocol-named spreadsheet column values may vary per row.
- Protocol-field writes must create a `Recipe` for the row's process when one does not already exist. Component-column writes must also create a recipe when needed so the component `Annotation` has a valid graph slot.

### Technical difficulty coverage

The following implementation risks are explicitly part of this plan:

| Entry | Status in this plan |
|---|---|
| Non-annotation columns were ignored by column APIs | Input/Output and protocol fields are writable through `AddColumn`; Input/Output are clearable through `RemoveColumn`; protocol-header removal remains a no-op |
| Decompose/read/update must address the correct endpoint | Trivial under singular `Input`/`Output`; the row process is the complete edge |
| Row and process counts could diverge under projection | Excluded by the invariant `RowCount = Processes.Count` |
| Characteristic/Factor writes failed when the carrier input/output was missing | Covered by the synthetic input/output rule |
| Annotation column order could not round-trip because column order metadata was missing | Covered by Required Changes and Column ordering |
| Protocol fields were not updated consistently by row/cell APIs | Covered by first-class writable protocol columns |
| Component writes failed when a process had no recipe | Covered by protocol-field/component write creation rule |

### `Tables` collection on `Dataset`

- `Dataset` exposes a `Tables` property returning a collection type equivalent to `ArcTables`.
- The collection provides table-level CRUD: `AddTable`, `GetTable`, `RemoveTable`, `RenameTable`, `MoveTable`, and column/row CRUD by table name or index.
- Grouping is computed lazily from the underlying `Processes` list; the `Tables` property is a live view, not a separate store.
