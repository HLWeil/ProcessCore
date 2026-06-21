# Core Datamodel Table Decoration

## Overview

The core datamodel represents experimental workflows as a process graph. The same information can also be expressed in a tabular format, where each table corresponds to a group of process nodes executing the same protocol step. This plan describes a set of types and APIs that provide a tabular view of the core datamodel — not a separate datamodel with its own storage, but a live projection of the underlying process graph.

**Reference documents:**
- [Conversion Specification](../references/TabularAndProcessConversion.md) — defines how processes map to table rows/columns and vice versa
- [Tabular Datamodel Types](../references/Tabular/ArcTableTypes.md) — reference type definitions (`IOType`, `CompositeHeader`, `CompositeCell`, `CompositeColumn`)
- [Tabular Datamodel API](../references/Tabular/ArcTableAPI.md) — reference API for `ArcTable` and `ArcTables`
- [Core Datamodel Implementation](../schemas/inmemory/ArcDataModel.fsproj)

---

## Design Principles

- The table layer sits **on top of** the core datamodel. `Table` holds references to the underlying `Process` nodes; edits to the table mutate those nodes directly, and changes to the graph are visible through the table.
- Types and API conventions follow the existing tabular datamodel (`ArcTable`, `ArcTables`) with adjustments to fit the core datamodel structure and semantics.
- All new types live in the namespace `ArcDataModel.Table`.

---

## Structural Mapping

### Grouping processes into tables

A `Dataset`'s processes are grouped into `Table` objects by **name**: all `Process` nodes in the dataset that share the same name belong to the same table. In the simple case, each table has one row per process node. If a process has multiple inputs and/or outputs, table rows are a projection over that process's input/output pairs as described in [Multi-I/O row projection](#multi-io-row-projection).

### Column roles and graph slots

Each column in a table corresponds to a consistently-typed slot across all process nodes in the group. The mapping is defined in full in the [Conversion Specification](../references/TabularAndProcessConversion.md#one-column--one-slot-across-all-process-nodes):

| Column role | Graph slot |
|---|---|
| `Input` | Name of the input entity node |
| `Output` | Name of the output entity node |
| `Parameter` | `Annotation` on the process node, `AdditionalType = "ParameterValue"` |
| `Factor` | `Annotation` on the output entity, `AdditionalType = "FactorValue"` |
| `Characteristic` | `Annotation` on the input entity, `AdditionalType = "CharacteristicValue"` |
| `Component` | `Annotation` on the protocol's `LabEquipment`, `AdditionalType = "Component"` |
| `ProtocolREF` | `Plan.Name` |
| `ProtocolType` | `Plan.IntendedUse` |
| `ProtocolDescription` | `Plan.Description` |
| `ProtocolUri` | `Plan.Url` |
| `ProtocolVersion` | `Plan.Version` |
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

### Multi-I/O row projection

The public table row index is not always the same thing as the underlying `Process` index. A `Table` must maintain or derive a row projection that maps each visible row to:

- one `Process`
- zero or one selected input entity for that visible row
- zero or one selected output entity for that visible row

For processes with more than one input or output, decompose produces one visible row per input/output pair. If the input and output counts differ, the shorter side is padded with empty cells. Cell updates for `Input`, `Output`, `Characteristic`, and `Factor` must target the projected input/output entity for that row, not blindly use the first input or output of the process.

The table's `RowCount`, `GetRow`, `GetCellAt`, `TryGetCellAt`, `AddRow`, `UpdateRow`, and `RemoveRow` APIs operate on visible table rows. When a visible row represents a multi-I/O projection of an existing process, row mutation must preserve the other projected rows for that same process unless the operation explicitly removes the underlying process.

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
- `AddColumn`, `RemoveColumn`, and cell/row update APIs must handle all supported column roles, not only annotation columns. `Input`, `Output`, `ProtocolREF`, `ProtocolType`, `ProtocolDescription`, `ProtocolUri`, and `ProtocolVersion` columns are first-class writable columns.
- Adding/removing a row creates/removes the corresponding `Process` node (and its input/output entities) in the parent `Dataset`.
- Modifying a cell updates the corresponding `Annotation`, entity name, or protocol field on the underlying process nodes.
- Missing Input or Output: if a table has no Input column but has Characteristic columns, a synthetic input entity is created per row (named `<tableName>_<rowIndex>`). The same applies symmetrically for Output and Factor columns.
- Multi-I/O: if a process node has more than one input or output entity, decompose produces one row per (input × output) pair; the shorter side is padded with empty cells.
- Protocol multiplicity: each process node references its own copy of a protocol object. Protocol column values may vary per row.
- Protocol-field writes must create a `Plan` for the row's process when one does not already exist. Component-column writes must also create a protocol when needed so the component `Annotation` has a valid graph slot.

### Technical difficulty coverage

The following implementation risks are explicitly part of this plan:

| Entry | Status in this plan |
|---|---|
| Non-annotation columns were ignored by `AddColumn` / `RemoveColumn` | Covered by the first-class writable column requirement above |
| Decompose/read/update used only the first input/output | Covered by Multi-I/O row projection |
| `RowCount` assumed one row per `Process` | Covered by Multi-I/O row projection |
| Characteristic/Factor writes failed when the carrier input/output was missing | Covered by the synthetic input/output rule |
| Annotation column order could not round-trip because column order metadata was missing | Covered by Required Changes and Column ordering |
| Protocol fields were not updated consistently by row/cell APIs | Covered by first-class writable protocol columns |
| Component writes failed when a process had no protocol | Covered by protocol-field/component write creation rule |

### `Tables` collection on `Dataset`

- `Dataset` exposes a `Tables` property returning a collection type equivalent to `ArcTables`.
- The collection provides table-level CRUD: `AddTable`, `GetTable`, `RemoveTable`, `RenameTable`, `MoveTable`, and column/row CRUD by table name or index.
- Grouping is computed lazily from the underlying `Processes` list; the `Tables` property is a live view, not a separate store.

