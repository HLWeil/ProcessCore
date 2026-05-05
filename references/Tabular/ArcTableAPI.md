# ArcTable and ArcTables API Reference

> Source: `src/Core/Table/ArcTable.fs`, `src/Core/Table/ArcTables.fs`

---

## Enum: `TableJoinOptions`

Controls how columns are merged when joining two tables.

| Value | Description |
|---|---|
| `Headers` | Add only headers, no cell values |
| `WithUnit` | Add headers and unit information, without main value |
| `WithValues` | Add full columns including all cell values |

---

## Class: `ArcTable`

A mutable, named spreadsheet-like table with typed headers and a sparse value matrix.

### Constructor

```
ArcTable(name: string, ?headers: ResizeArray<CompositeHeader>, ?columns: ResizeArray<ResizeArray<CompositeCell>>)
```

### Properties

| Member | Type | Description |
|---|---|---|
| `Name` | `string` | Table name (internal setter) |
| `Headers` | `ResizeArray<CompositeHeader>` | Ordered list of column headers |
| `Values` | `ArcTableValues` | Internal sparse value store |
| `ColumnCount` | `int` | Number of columns |
| `RowCount` | `int` | Number of rows (settable internally) |
| `Columns` | `ResizeArray<CompositeColumn>` | All columns as `CompositeColumn` objects |

### Static Constructors

| Member | Signature | Description |
|---|---|---|
| `create` | `(name, headers, values) → ArcTable` | Direct construction |
| `init` | `(name: string) → ArcTable` | Empty table with no headers or values |
| `fromArcTableValues` | `(name, headers, values: ArcTableValues) → ArcTable` | Construct from pre-built value store |
| `createFromRows` | `(name, headers, rows: ResizeArray<ResizeArray<CompositeCell>>) → ArcTable` | Construct from row data |

### Validation

| Member | Signature | Description |
|---|---|---|
| `this.Validate` | `(?raiseException: bool) → bool` | Validate headers against values |
| `ArcTable.validate` | `(?raiseException: bool) → ArcTable → bool` | Static curried form |

### Cell API

| Member | Signature | Description |
|---|---|---|
| `this.TryGetCellAt` | `(column: int, row: int) → CompositeCell option` | Returns cell if it exists |
| `ArcTable.tryGetCellAt` | `(column, row) → ArcTable → CompositeCell option` | Static curried form |
| `this.GetCellAt` | `(column: int, row: int) → CompositeCell` | Returns cell, fails if out of bounds |
| `ArcTable.getCellAt` | `(column, row) → ArcTable → CompositeCell` | Static curried form |
| `this.UpdateCellAt` | `(columnIndex, rowIndex, cell, ?skipValidation) → unit` | Update existing cell; fails if out of bounds |
| `ArcTable.updateCellAt` | `(columnIndex, rowIndex, cell, ?skipValidation) → ArcTable → ArcTable` | Static curried form |
| `this.SetCellAt` | `(columnIndex, rowIndex, cell, ?skipValidation) → unit` | Update cell or extend rows if needed |
| `ArcTable.setCellAt` | `(columnIndex, rowIndex, cell, ?skipValidation) → ArcTable → ArcTable` | Static curried form |
| `this.UpdateCellsBy` | `(f: int → int → CompositeCell → CompositeCell, ?skipValidation) → unit` | Update all cells by function over (colIdx, rowIdx, cell) |
| `ArcTable.updateCellsBy` | `(f, ?skipValidation) → ArcTable → unit` | Static curried form |
| `this.UpdateCellBy` | `(columnIndex, rowIndex, f: CompositeCell → CompositeCell, ?skipValidation) → unit` | Update one cell by function |
| `ArcTable.updateCellBy` | `(columnIndex, rowIndex, f, ?skipValidation) → ArcTable → ArcTable` | Static curried form |

### Column Iteration

| Member | Signature | Description |
|---|---|---|
| `this.IterColumns` | `(action: CompositeColumn → unit) → unit` | Iterate all columns |
| `ArcTable.iterColumns` | `(action) → ArcTable → ArcTable` | Static curried form (operates on copy) |
| `this.IteriColumns` | `(action: int → CompositeColumn → unit) → unit` | Iterate all columns with index |
| `ArcTable.iteriColumns` | `(action) → ArcTable → ArcTable` | Static curried form (operates on copy) |

### Header API

| Member | Signature | Description |
|---|---|---|
| `this.UpdateHeader` | `(index: int, newHeader: CompositeHeader, ?forceConvertCells: bool) → unit` | Replace header at index; optionally converts existing cells |
| `ArcTable.updateHeader` | `(index, header) → ArcTable → ArcTable` | Static curried form |

### Column API

| Member | Signature | Description |
|---|---|---|
| `this.AddColumn` | `(header, ?cells, ?index, ?forceReplace) → unit` | Add column at position (default: append) |
| `ArcTable.addColumn` | `(header, ?cells, ?index, ?forceReplace) → ArcTable → ArcTable` | Static curried form |
| `this.AddColumnFill` | `(header, cell, ?index, ?forceReplace) → unit` | Add column filled with a single repeated cell |
| `ArcTable.addColumnFill` | `(header, cell, ?index, ?forceReplace) → ArcTable → ArcTable` | Static curried form |
| `this.AddColumns` | `(columns: seq<CompositeColumn>, ?index, ?forceReplace) → unit` | Add multiple columns |
| `ArcTable.addColumns` | `(columns: CompositeColumn[], ?index) → ArcTable → ArcTable` | Static curried form |
| `this.UpdateColumn` | `(columnIndex, header, ?cells) → unit` | Replace header and cells at index |
| `ArcTable.updateColumn` | `(columnIndex, header, ?cells) → ArcTable → ArcTable` | Static curried form |
| `this.InsertColumn` | `(index, header, ?cells) → unit` | Insert column at index |
| `ArcTable.insertColumn` | `(index, header, ?cells) → ArcTable → ArcTable` | Static curried form |
| `this.AppendColumn` | `(header, ?cells) → unit` | Append column at end |
| `ArcTable.appendColumn` | `(header, ?cells) → ArcTable → ArcTable` | Static curried form |
| `this.RemoveColumn` | `(index: int) → unit` | Remove column at index |
| `ArcTable.removeColumn` | `(index) → ArcTable → ArcTable` | Static curried form |
| `this.RemoveColumns` | `(indexArr: int[]) → unit` | Remove multiple columns (highest-first) |
| `ArcTable.removeColumns` | `(indexArr) → ArcTable → ArcTable` | Static curried form |
| `this.GetColumn` | `(columnIndex, ?failOnMissingCell) → CompositeColumn` | Get column; fills gaps with empty cells by default |
| `ArcTable.getColumn` | `(index, ?failOnMissingCell) → ArcTable → CompositeColumn` | Static curried form |
| `this.TryGetColumnByHeader` | `(header, ?failOnMissingCell) → CompositeColumn option` | Find column by header equality |
| `ArcTable.tryGetColumnByHeader` | `(header, ?failOnMissingCell) → ArcTable → CompositeColumn option` | Static curried form |
| `this.TryGetColumnByHeaderBy` | `(predicate, ?failOnMissingCell) → CompositeColumn option` | Find column by header predicate |
| `ArcTable.tryGetColumnByHeaderBy` | `(predicate, ?failOnMissingCell) → ArcTable → CompositeColumn option` | Static curried form |
| `this.GetColumnByHeader` | `(header, ?failOnMissingCell) → CompositeColumn` | Find column by header; fails if not found |
| `ArcTable.getColumnByHeader` | `(header, ?failOnMissingCell) → ArcTable → CompositeColumn` | Static curried form |
| `this.MoveColumn` | `(startCol: int, endCol: int) → unit` | Move column from one index to another |
| `ArcTable.moveColumn` | `(startCol, endCol) → ArcTable → ArcTable` | Static curried form |

### Input / Output Column API

| Member | Signature | Description |
|---|---|---|
| `this.TryGetInputColumn` | `() → CompositeColumn option` | Find the Input-typed column |
| `ArcTable.tryGetInputColumn` | `() → ArcTable → CompositeColumn option` | Static curried form |
| `this.GetInputColumn` | `() → CompositeColumn` | Get the Input column; fails if absent |
| `ArcTable.getInputColumn` | `() → ArcTable → CompositeColumn` | Static curried form |
| `this.TryGetOutputColumn` | `() → CompositeColumn option` | Find the Output-typed column |
| `ArcTable.tryGetOutputColumn` | `() → ArcTable → CompositeColumn option` | Static curried form |
| `this.GetOutputColumn` | `() → CompositeColumn` | Get the Output column; fails if absent |
| `ArcTable.getOutputColumn` | `() → ArcTable → CompositeColumn` | Static curried form |

### Protocol Column API

| Member | Signature | Description |
|---|---|---|
| `this.AddProtocolTypeColumn` | `(?types, ?index, ?forceReplace) → unit` | Add ProtocolType column |
| `this.AddProtocolVersionColumn` | `(?versions, ?index, ?forceReplace) → unit` | Add ProtocolVersion column |
| `this.AddProtocolUriColumn` | `(?uris, ?index, ?forceReplace) → unit` | Add ProtocolUri column |
| `this.AddProtocolDescriptionColumn` | `(?descriptions, ?index, ?forceReplace) → unit` | Add ProtocolDescription column |
| `this.AddProtocolNameColumn` | `(?names, ?index, ?forceReplace) → unit` | Add ProtocolREF column |
| `this.GetProtocolTypeColumn` | `() → CompositeColumn` | Get ProtocolType column |
| `this.GetProtocolVersionColumn` | `() → CompositeColumn` | Get ProtocolVersion column |
| `this.GetProtocolUriColumn` | `() → CompositeColumn` | Get ProtocolUri column |
| `this.GetProtocolDescriptionColumn` | `() → CompositeColumn` | Get ProtocolDescription column |
| `this.GetProtocolNameColumn` | `() → CompositeColumn` | Get ProtocolREF column |
| `this.TryGetProtocolNameColumn` | `() → CompositeColumn option` | Try get ProtocolREF column |
| `this.GetComponentColumns` | `() → ResizeArray<CompositeColumn>` | Get all Component-typed columns |

### Row API

| Member | Signature | Description |
|---|---|---|
| `this.AddRow` | `(?cells, ?index) → unit` | Add row at position (default: append) |
| `ArcTable.addRow` | `(?cells, ?index) → ArcTable → ArcTable` | Static curried form |
| `this.AppendRow` | `(?cells) → unit` | Append row at end |
| `ArcTable.appendRow` | `(?cells) → ArcTable → ArcTable` | Static curried form |
| `this.InsertRow` | `(index, ?cells) → unit` | Insert row at index |
| `ArcTable.insertRow` | `(index, ?cells) → ArcTable → ArcTable` | Static curried form |
| `this.AddRows` | `(rows, ?index) → unit` | Add multiple rows |
| `ArcTable.addRows` | `(rows, ?index) → ArcTable → ArcTable` | Static curried form |
| `this.AddRowsEmpty` | `(rowCount, ?index) → unit` | Add N empty rows |
| `ArcTable.addRowsEmpty` | `(rowCount, ?index) → ArcTable → ArcTable` | Static curried form |
| `this.UpdateRow` | `(rowIndex, cells) → unit` | Replace all cells in a row |
| `ArcTable.updateRow` | `(rowIndex, cells) → ArcTable → ArcTable` | Static curried form |
| `this.RemoveRow` | `(index: int) → unit` | Remove row at index |
| `ArcTable.removeRow` | `(index) → ArcTable → ArcTable` | Static curried form |
| `this.RemoveRows` | `(indexArr: int[]) → unit` | Remove multiple rows (highest-first) |
| `ArcTable.removeRows` | `(indexArr) → ArcTable → ArcTable` | Static curried form |
| `this.GetRow` | `(rowIndex, ?SkipValidation) → ResizeArray<CompositeCell>` | Get all cells of a row |
| `ArcTable.getRow` | `(index) → ArcTable → ResizeArray<CompositeCell>` | Static curried form |

### Join / Merge API

| Member | Signature | Description |
|---|---|---|
| `this.Join` | `(table, ?index, ?joinOptions, ?forceReplace) → unit` | Merge another table's columns into this one |
| `ArcTable.join` | `(table, ?index, ?joinOptions, ?forceReplace) → ArcTable → ArcTable` | Static curried form |
| `ArcTable.append` | `(table1, table2) → ArcTable` | Append all rows of table2 to table1; aligns by headers |

### Split API

| Member | Signature | Description |
|---|---|---|
| `ArcTable.SplitByColumnValues` | `(columnIndex) → ArcTable → ResizeArray<ArcTable>` | Split table row-wise by unique values in the given column |
| `ArcTable.SplitByColumnValuesByHeader` | `(header) → ArcTable → ResizeArray<ArcTable>` | Split row-wise by unique values in the named column |
| `ArcTable.SplitByProtocolREF` | `ArcTable → ResizeArray<ArcTable>` | Split row-wise by unique ProtocolREF values |

### Reference / Protocol Update API

| Member | Signature | Description |
|---|---|---|
| `ArcTable.updateReferenceByAnnotationTable` | `(refTable, annotationTable) → ArcTable` | Merge annotation table values into a protocol reference table |
| `ArcTable.setRowCount` | `(newRowCount) → ArcTable → ArcTable` | Return copy with updated row count |

### Utility

| Member | Signature | Description |
|---|---|---|
| `this.Copy` | `() → ArcTable` | Deep copy |
| `this.RescanValueMap` | `() → unit` | Rebuild the internal value deduplication map |
| `this.StructurallyEquals` | `(other: ArcTable) → bool` | Value-based equality via hash |
| `this.ReferenceEquals` | `(other: ArcTable) → bool` | Reference identity check |
| `this.Equals` | `(other: obj) → bool` | Override; delegates to structural equality |
| `this.GetHashCode` | `() → int` | Hash of name + headers + values |
| `this.ToString` | `() → string` | Pretty-printed table; truncates at 50 rows |

---

## Class: `ArcTables`

A mutable, ordered, named collection of `ArcTable` objects with unique table names.

### Constructor

```
ArcTables(initTables: ResizeArray<ArcTable>)
```

Implements `IEnumerable<ArcTable>`.

### Properties

| Member | Type | Description |
|---|---|---|
| `Tables` | `ResizeArray<ArcTable>` | The underlying table list (mutable) |
| `TableNames` | `string list` | Names of all tables in order |
| `TableCount` | `int` | Number of tables |
| `Item` | `int → ArcTable` | Index access |

### Static Constructors

| Member | Signature | Description |
|---|---|---|
| `ArcTables.ofSeq` | `(seq<ArcTable>) → ArcTables` | Construct from any sequence |

### Table API

| Member | Signature | Description |
|---|---|---|
| `this.AddTable` | `(table, ?index) → unit` | Insert table at position (default: append); fails on duplicate name |
| `this.AddTables` | `(tables, ?index) → unit` | Insert multiple tables; validates all names unique |
| `this.InitTable` | `(tableName, ?index) → ArcTable` | Create and insert an empty table; returns it |
| `this.InitTables` | `(tableNames, ?index) → unit` | Create and insert multiple empty tables |
| `this.GetTableAt` | `(index: int) → ArcTable` | Get table by index |
| `this.GetTable` | `(name: string) → ArcTable` | Get table by name; fails if not found |
| `this.UpdateTableAt` | `(index, table) → unit` | Replace table at index |
| `this.UpdateTable` | `(name, table) → unit` | Replace table by name |
| `this.SetTableAt` | `(index, table) → unit` | Replace or append table at index |
| `this.SetTable` | `(name, table) → unit` | Replace by name if exists, otherwise append |
| `this.RemoveTableAt` | `(index: int) → unit` | Remove table at index |
| `this.RemoveTable` | `(name: string) → unit` | Remove table by name |
| `this.MapTableAt` | `(index, updateFun: ArcTable → unit) → unit` | Apply an in-place mutation function to table at index |
| `this.MapTable` | `(name, updateFun) → unit` | Apply mutation function to named table |
| `this.RenameTableAt` | `(index, newName) → unit` | Rename table at index |
| `this.RenameTable` | `(name, newName) → unit` | Rename table by current name |
| `this.MoveTable` | `(oldIndex, newIndex) → unit` | Move table to a different position |

### Column CRUD API (by table index or name)

| Member | Signature | Description |
|---|---|---|
| `this.AddColumnAt` | `(tableIndex, header, ?cells, ?columnIndex, ?forceReplace) → unit` | Add column to table at index |
| `this.AddColumn` | `(tableName, header, ?cells, ?columnIndex, ?forceReplace) → unit` | Add column to named table |
| `this.RemoveColumnAt` | `(tableIndex, columnIndex) → unit` | Remove column from table at index |
| `this.RemoveColumn` | `(tableName, columnIndex) → unit` | Remove column from named table |
| `this.UpdateColumnAt` | `(tableIndex, columnIndex, header, ?cells) → unit` | Replace column in table at index |
| `this.UpdateColumn` | `(tableName, columnIndex, header, ?cells) → unit` | Replace column in named table |
| `this.GetColumnAt` | `(tableIndex, columnIndex) → CompositeColumn` | Get column from table at index |
| `this.GetColumn` | `(tableName, columnIndex) → CompositeColumn` | Get column from named table |

### Row CRUD API (by table index or name)

| Member | Signature | Description |
|---|---|---|
| `this.AddRowAt` | `(tableIndex, ?cells, ?rowIndex) → unit` | Add row to table at index |
| `this.AddRow` | `(tableName, ?cells, ?rowIndex) → unit` | Add row to named table |
| `this.RemoveRowAt` | `(tableIndex, rowIndex) → unit` | Remove row from table at index |
| `this.RemoveRow` | `(tableName, rowIndex) → unit` | Remove row from named table |
| `this.UpdateRowAt` | `(tableIndex, rowIndex, cells) → unit` | Replace row in table at index |
| `this.UpdateRow` | `(tableName, rowIndex, cells) → unit` | Replace row in named table |
| `this.GetRowAt` | `(tableIndex, rowIndex) → ResizeArray<CompositeCell>` | Get row from table at index |
| `this.GetRow` | `(tableName, rowIndex) → ResizeArray<CompositeCell>` | Get row from named table |

### Reference Table Merge

| Member | Signature | Description |
|---|---|---|
| `ArcTables.updateReferenceTablesBySheets` | `(referenceTables, sheetTables, ?keepUnusedRefTables) → ArcTables` | Merge annotation sheet values into protocol reference tables, matching by ProtocolREF name; splits tables by ProtocolREF before matching |

