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

- The table layer sits **on top of** the core datamodel. `Table` holds references to the underlying `LabProcess` nodes; edits to the table mutate those nodes directly, and changes to the graph are visible through the table.
- Types and API conventions follow the existing tabular datamodel (`ArcTable`, `ArcTables`) with adjustments to fit the core datamodel structure and semantics.
- All new types live in the namespace `ArcDataModel.Table`.

---

## Structural Mapping

### Grouping processes into tables

A `Dataset`'s processes are grouped into `Table` objects by **name**: all `LabProcess` nodes in the dataset that share the same name belong to the same table. Each table has one row per process node.

### Column roles and graph slots

Each column in a table corresponds to a consistently-typed slot across all process nodes in the group. The mapping is defined in full in the [Conversion Specification](../references/TabularAndProcessConversion.md#one-column--one-slot-across-all-process-nodes):

| Column role | Graph slot |
|---|---|
| `Input` | Name of the input entity node |
| `Output` | Name of the output entity node |
| `Parameter` | `PropertyValue` on the process node, `AdditionalType = "ParameterValue"` |
| `Factor` | `PropertyValue` on the output entity, `AdditionalType = "FactorValue"` |
| `Characteristic` | `PropertyValue` on the input entity, `AdditionalType = "CharacteristicValue"` |
| `Component` | `PropertyValue` on the protocol's `LabEquipment`, `AdditionalType = "Component"` |
| `ProtocolREF` | `LabProtocol.Name` |
| `ProtocolType` | `LabProtocol.IntendedUse` |
| `ProtocolDescription` | `LabProtocol.Description` |
| `ProtocolUri` | `LabProtocol.Url` |
| `ProtocolVersion` | `LabProtocol.Version` |
| `Performer` | `LabProcess` performer field (to be added) |
| `Comment` | `LabProcess` comments collection (to be added) |

### Column ordering

Because the process graph does not preserve column order, every annotation `PropertyValue` (Parameter, Factor, Characteristic, Component) must carry a `ColumnIndex: int option` field. This stores the column's ordinal position **within annotation columns only** (Input / Output / Protocol / Comment columns are not counted). During **compose** (table → processes), the column index is written from the column's position. During **decompose** (processes → table), annotation values are sorted by this field before assembling a row; values without an index are placed last. The fixed full column order on decompose is:

1. Input
2. ProtocolREF, ProtocolType, ProtocolDescription, ProtocolUri, ProtocolVersion
3. Characteristics (sorted by `ColumnIndex`)
4. Components (sorted by `ColumnIndex`)
5. Parameters (sorted by `ColumnIndex`)
6. Factors (sorted by `ColumnIndex`)
7. Comments
8. Output

---

## Required Changes to the Core Datamodel

(Skip for now. Skip performer and comments fields. Skip sorting of annotation columns across header types.)

- **`PropertyValue`**: Add `ColumnIndex: int option` field for annotation column order preservation.
- **`LabProcess`**: Add a `Performer` field and a `Comments` collection to support the corresponding column roles.

---

## Implementation Details

### `Table` type

- Wraps a `ResizeArray<LabProcess>` — the processes it represents.
- Exposes `Name`, `ColumnCount`, `RowCount`, `Headers` (`ResizeArray<CompositeHeader>`).
- Provides full cell, column, row, and protocol column APIs following the `ArcTable` reference.
- Adding/removing a row creates/removes the corresponding `LabProcess` node (and its input/output entities) in the parent `Dataset`.
- Modifying a cell updates the corresponding `PropertyValue`, entity name, or protocol field on the underlying process nodes.
- Missing Input or Output: if a table has no Input column but has Characteristic columns, a synthetic input entity is created per row (named `<tableName>_<rowIndex>`). The same applies symmetrically for Output and Factor columns.
- Multi-I/O: if a process node has more than one input or output entity, decompose produces one row per (input × output) pair; the shorter side is padded with empty cells.
- Protocol multiplicity: each process node references its own copy of a protocol object. Protocol column values may vary per row.

### `Tables` collection on `Dataset`

- `Dataset` exposes a `Tables` property returning a collection type equivalent to `ArcTables`.
- The collection provides table-level CRUD: `AddTable`, `GetTable`, `RemoveTable`, `RenameTable`, `MoveTable`, and column/row CRUD by table name or index.
- Grouping is computed lazily from the underlying `Processes` list; the `Tables` property is a live view, not a separate store.

