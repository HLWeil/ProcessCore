namespace ProcessCore.SQL

open Fable.Core

/// <summary>
/// Schema metadata for a single SQLite table or view, paired with the codec functions that
/// translate between rows of type <typeparamref name="'row"/> and the on-the-wire
/// <see cref="SqlRow"/> / <see cref="SqlParameters"/> representations.
/// </summary>
/// <remarks>
/// Instances are typically obtained from the <see cref="Repository"/> module rather than constructed
/// directly. The repository's CRUD façades use <see cref="Name"/>, <see cref="Columns"/> and
/// <see cref="PrimaryKey"/> to synthesise SQL, and use <see cref="OfRow"/> / <see cref="ToParameters"/>
/// to marshal values across the <see cref="ISqliteDriver"/> boundary.
/// </remarks>
/// <param name="Name">SQL table or view name.</param>
/// <param name="Columns">Ordered list of column names. Used to build INSERT/SELECT column lists.</param>
/// <param name="PrimaryKey">
/// Ordered list of column names that together form the primary key. Used to build the WHERE clause
/// of UPDATE/DELETE/single-row SELECT statements and the ORDER BY clause of <c>list</c>.
/// </param>
/// <param name="OfRow">Decodes a query result row.</param>
/// <param name="ToParameters">Encodes a row to a parameter array.</param>
[<AttachMembers>]
type Table<'row>(
    Name: string,
    Columns: string[],
    PrimaryKey: string[],
    OfRow: SqlRow -> 'row,
    ToParameters: 'row -> SqlParameters
) =

    /// <summary>SQL table or view name.</summary>
    member val Name = Name with get, set
    /// <summary>Ordered column names.</summary>
    member val Columns = Columns with get, set
    /// <summary>Ordered primary-key column names.</summary>
    member val PrimaryKey = PrimaryKey with get, set
    /// <summary>Row decoder.</summary>
    member val OfRow = OfRow with get, set
    /// <summary>Row encoder.</summary>
    member val ToParameters = ToParameters with get, set

    /// <summary>Named-argument constructor exposed to JavaScript and Python callers.</summary>
    [<NamedParams>]
    static member create(
        Name: string,
        Columns: string[],
        PrimaryKey: string[],
        OfRow: SqlRow -> 'row,
        ToParameters: 'row -> SqlParameters
    ) =
        Table<'row>(Name, Columns, PrimaryKey, OfRow, ToParameters)

/// <summary>
/// Static catalogue of <see cref="Table{TRow}"/> descriptors, one per persisted table in the
/// data model.
/// </summary>
/// <remarks>
/// Each value bundles the table's name, column list, primary key and row codecs so that the CRUD
/// façades (<see cref="DefinedTerm"/>, <see cref="LabProtocol"/>, …) and external callers can build
/// statements without hard-coding column lists.
/// </remarks>
[<RequireQualifiedAccess>]
module Repository =

    let private table name columns primaryKey ofRow toParameters =
        Table(name, columns, primaryKey, ofRow, toParameters)

    /// <summary>Descriptor for the <c>defined_term</c> table. Primary key: <c>id</c>.</summary>
    let DefinedTerm =
        table
            "defined_term"
            [| "id"; "type"; "name"; "tan"; "in_defined_term_set_id"; "in_defined_term_set_name" |]
            [| "id" |]
            DefinedTermRow.ofRow
            (fun row -> row.ToParameters())

    /// <summary>Descriptor for the <c>lab_protocol</c> table. Primary key: <c>id</c>.</summary>
    let LabProtocol =
        table
            "lab_protocol"
            [|
                "id"
                "type"
                "additional_type"
                "name"
                "description"
                "version"
                "url"
                "intended_use_id"
                "intended_use_text"
            |]
            [| "id" |]
            LabProtocolRow.ofRow
            (fun row -> row.ToParameters())

    /// <summary>Descriptor for the <c>formal_parameter</c> table. Primary key: <c>id</c>.</summary>
    let FormalParameter =
        table
            "formal_parameter"
            [| "id"; "type"; "name"; "name_tan"; "default_value_id" |]
            [| "id" |]
            FormalParameterRow.ofRow
            (fun row -> row.ToParameters())

    /// <summary>Descriptor for the <c>dataset</c> table. Primary key: <c>id</c>.</summary>
    let Dataset =
        table
            "dataset"
            [| "id"; "type"; "additional_type"; "identifier"; "name"; "description" |]
            [| "id" |]
            DatasetRow.ofRow
            (fun row -> row.ToParameters())

    /// <summary>Descriptor for the <c>material</c> table. Primary key: <c>id</c>.</summary>
    let Material =
        table
            "material"
            [| "id"; "type"; "additional_type"; "name" |]
            [| "id" |]
            MaterialRow.ofRow
            (fun row -> row.ToParameters())

    /// <summary>Descriptor for the <c>data</c> table. Primary key: <c>id</c>.</summary>
    let Data =
        table
            "data"
            [| "id"; "type"; "additional_type"; "path"; "selector"; "selector_format"; "encoding_format" |]
            [| "id" |]
            DataRow.ofRow
            (fun row -> row.ToParameters())

    /// <summary>Descriptor for the <c>lab_process</c> table. Primary key: <c>id</c>.</summary>
    let LabProcess =
        table
            "lab_process"
            [| "id"; "type"; "additional_type"; "name"; "executes_protocol_id" |]
            [| "id" |]
            LabProcessRow.ofRow
            (fun row -> row.ToParameters())

    /// <summary>Descriptor for the <c>property_value</c> table. Primary key: <c>id</c>.</summary>
    let PropertyValue =
        table
            "property_value"
            [|
                "id"
                "type"
                "additional_type"
                "name"
                "value"
                "unit"
                "name_tan"
                "value_tan"
                "unit_tan"
                "instance_of_id"
            |]
            [| "id" |]
            PropertyValueRow.ofRow
            (fun row -> row.ToParameters())

    /// <summary>Descriptor for the <c>dataset_has_part</c> association table. Primary key: <c>(dataset_id, position)</c>.</summary>
    let DatasetHasPart =
        table
            "dataset_has_part"
            [| "dataset_id"; "position"; "part_dataset_id"; "part_data_id" |]
            [| "dataset_id"; "position" |]
            DatasetHasPartRow.ofRow
            (fun row -> row.ToParameters())

    /// <summary>Descriptor for the <c>dataset_process</c> association table. Primary key: <c>(dataset_id, position)</c>.</summary>
    let DatasetProcess =
        table
            "dataset_process"
            [| "dataset_id"; "position"; "process_id" |]
            [| "dataset_id"; "position" |]
            DatasetProcessRow.ofRow
            (fun row -> row.ToParameters())

    /// <summary>Descriptor for the <c>dataset_additional_property</c> association table. Primary key: <c>(dataset_id, position)</c>.</summary>
    let DatasetAdditionalProperty =
        table
            "dataset_additional_property"
            [| "dataset_id"; "position"; "property_value_id" |]
            [| "dataset_id"; "position" |]
            DatasetAdditionalPropertyRow.ofRow
            (fun row -> row.ToParameters())

    /// <summary>Descriptor for the <c>protocol_parameter</c> association table. Primary key: <c>(protocol_id, position)</c>.</summary>
    let ProtocolParameter =
        table
            "protocol_parameter"
            [| "protocol_id"; "position"; "formal_parameter_id" |]
            [| "protocol_id"; "position" |]
            ProtocolParameterRow.ofRow
            (fun row -> row.ToParameters())

    /// <summary>Descriptor for the <c>process_io</c> association table. Primary key: <c>(process_id, direction, position)</c>.</summary>
    let ProcessIo =
        table
            "process_io"
            [| "process_id"; "direction"; "position"; "material_id"; "data_id" |]
            [| "process_id"; "direction"; "position" |]
            ProcessIoRow.ofRow
            (fun row -> row.ToParameters())

    /// <summary>Descriptor for the <c>process_parameter_value</c> association table. Primary key: <c>(process_id, position)</c>.</summary>
    let ProcessParameterValue =
        table
            "process_parameter_value"
            [| "process_id"; "position"; "property_value_id" |]
            [| "process_id"; "position" |]
            ProcessParameterValueRow.ofRow
            (fun row -> row.ToParameters())

    /// <summary>Descriptor for the <c>protocol_additional_property</c> association table. Primary key: <c>(protocol_id, position)</c>.</summary>
    let ProtocolAdditionalProperty =
        table
            "protocol_additional_property"
            [| "protocol_id"; "position"; "property_value_id" |]
            [| "protocol_id"; "position" |]
            ProtocolAdditionalPropertyRow.ofRow
            (fun row -> row.ToParameters())

    /// <summary>Descriptor for the <c>material_additional_property</c> association table. Primary key: <c>(material_id, position)</c>.</summary>
    let MaterialAdditionalProperty =
        table
            "material_additional_property"
            [| "material_id"; "position"; "property_value_id" |]
            [| "material_id"; "position" |]
            MaterialAdditionalPropertyRow.ofRow
            (fun row -> row.ToParameters())

    /// <summary>Descriptor for the <c>data_additional_property</c> association table. Primary key: <c>(data_id, position)</c>.</summary>
    let DataAdditionalProperty =
        table
            "data_additional_property"
            [| "data_id"; "position"; "property_value_id" |]
            [| "data_id"; "position" |]
            DataAdditionalPropertyRow.ofRow
            (fun row -> row.ToParameters())

    /// <summary>
    /// Names of the entity tables (independent rows with their own primary key), in canonical
    /// declaration order. Useful for schema introspection, fixture loading and bulk truncate.
    /// </summary>
    let EntityTables () =
        [|
            DefinedTerm.Name
            LabProtocol.Name
            FormalParameter.Name
            Dataset.Name
            Material.Name
            Data.Name
            LabProcess.Name
            PropertyValue.Name
        |]

    /// <summary>
    /// Names of the association tables (rows that link two or more entities, typically with an
    /// ordering position), in canonical declaration order.
    /// </summary>
    let AssociationTables () =
        [|
            DatasetHasPart.Name
            DatasetProcess.Name
            DatasetAdditionalProperty.Name
            ProtocolParameter.Name
            ProcessIo.Name
            ProcessParameterValue.Name
            ProtocolAdditionalProperty.Name
            MaterialAdditionalProperty.Name
            DataAdditionalProperty.Name
        |]

module private Crud =

    let private parameterFor column (parameters: SqlParameters) =
        parameters
        |> Array.tryFind (fun parameter -> parameter.Name = column)
        |> Option.defaultWith (fun () -> invalidArg column $"Missing SQL parameter for column '{column}'.")

    let private parameterName column = "$" + column

    let private columnList (columns: string[]) =
        String.concat ", " columns

    let private whereClause primaryKey =
        primaryKey
        |> Array.map (fun column -> $"{column} = {parameterName column}")
        |> String.concat " AND "

    let private orderClause primaryKey =
        primaryKey
        |> String.concat ", "

    let insert (table: Table<'row>) (driver: ISqliteDriver) (row: 'row) =
        let columns = columnList table.Columns

        let values =
            table.Columns
            |> Array.map parameterName
            |> String.concat ", "

        let sql = $"INSERT INTO {table.Name} ({columns}) VALUES ({values});"
        driver.Execute sql (table.ToParameters row)

    let update (table: Table<'row>) (driver: ISqliteDriver) (row: 'row) =
        let setColumns =
            table.Columns
            |> Array.filter (fun column -> table.PrimaryKey |> Array.contains column |> not)

        if setColumns.Length = 0 then
            invalidOp $"Table '{table.Name}' has no non-primary-key columns to update."

        let setClause =
            setColumns
            |> Array.map (fun column -> $"{column} = {parameterName column}")
            |> String.concat ", "

        let sql = $"UPDATE {table.Name} SET {setClause} WHERE {whereClause table.PrimaryKey};"
        driver.Execute sql (table.ToParameters row)

    let delete (table: Table<'row>) (driver: ISqliteDriver) (keyParameters: SqlParameters) =
        let sql = $"DELETE FROM {table.Name} WHERE {whereClause table.PrimaryKey};"
        driver.Execute sql keyParameters

    let get (table: Table<'row>) (driver: ISqliteDriver) (keyParameters: SqlParameters) =
        let sql = $"SELECT {columnList table.Columns} FROM {table.Name} WHERE {whereClause table.PrimaryKey};"

        match driver.Query sql keyParameters with
        | [| row |] -> Some(table.OfRow row)
        | [||] -> None
        | rows -> invalidOp $"Expected at most one row from '{table.Name}', but got {rows.Length}."

    let list (table: Table<'row>) (driver: ISqliteDriver) =
        let sql = $"SELECT {columnList table.Columns} FROM {table.Name} ORDER BY {orderClause table.PrimaryKey};"
        driver.Query sql [||] |> Array.map table.OfRow

    let listView (name: string) (columns: string[]) (orderColumns: string[]) ofRow (driver: ISqliteDriver) =
        let sql = $"SELECT {columnList columns} FROM {name} ORDER BY {orderClause orderColumns};"
        driver.Query sql [||] |> Array.map ofRow

    let keyFromRow (table: Table<'row>) row =
        let parameters = table.ToParameters row

        table.PrimaryKey
        |> Array.map (fun column -> parameterFor column parameters)

    let key1 name value =
        [| SqlParameter(name, SqlValue.Text value) |]

    let key2 name1 value1 name2 value2 =
        [| SqlParameter(name1, SqlValue.Text value1); SqlParameter(name2, SqlValue.Int value2) |]

    let processIoKey processId direction position =
        let directionValue =
            match direction with
            | ProcessIoDirection.Input -> "input"
            | ProcessIoDirection.Output -> "output"

        [|
            SqlParameter("process_id", SqlValue.Text processId)
            SqlParameter("direction", SqlValue.Text directionValue)
            SqlParameter("position", SqlValue.Int position)
        |]

/// <summary>CRUD façade for the <c>defined_term</c> table.</summary>
[<AttachMembers>]
type DefinedTerm =

    /// <summary>Inserts a new row. Raises if the row violates a UNIQUE or FK constraint.</summary>
    static member insert (driver: ISqliteDriver, row: DefinedTermRow) = Crud.insert Repository.DefinedTerm driver row
    /// <summary>Updates the row identified by <c>row.Id</c>. Affects zero rows if no match exists.</summary>
    static member update (driver: ISqliteDriver, row: DefinedTermRow) = Crud.update Repository.DefinedTerm driver row
    /// <summary>Deletes the row with the given id.</summary>
    static member delete (driver: ISqliteDriver, id: string) = Crud.delete Repository.DefinedTerm driver (Crud.key1 "id" id)
    /// <summary>Returns the row with the given id, or <c>None</c> if no such row exists.</summary>
    static member get (driver: ISqliteDriver, id: string) = Crud.get Repository.DefinedTerm driver (Crud.key1 "id" id)
    /// <summary>Returns all rows ordered by primary key.</summary>
    static member list (driver: ISqliteDriver) = Crud.list Repository.DefinedTerm driver

/// <summary>CRUD façade for the <c>lab_protocol</c> table.</summary>
[<AttachMembers>]
type LabProtocol =

    /// <summary>Inserts a new row.</summary>
    static member insert (driver: ISqliteDriver, row: LabProtocolRow) = Crud.insert Repository.LabProtocol driver row
    /// <summary>Updates the row identified by <c>row.Id</c>.</summary>
    static member update (driver: ISqliteDriver, row: LabProtocolRow) = Crud.update Repository.LabProtocol driver row
    /// <summary>Deletes the row with the given id.</summary>
    static member delete (driver: ISqliteDriver, id: string) = Crud.delete Repository.LabProtocol driver (Crud.key1 "id" id)
    /// <summary>Returns the row with the given id, or <c>None</c>.</summary>
    static member get (driver: ISqliteDriver, id: string) = Crud.get Repository.LabProtocol driver (Crud.key1 "id" id)
    /// <summary>Returns all rows ordered by primary key.</summary>
    static member list (driver: ISqliteDriver) = Crud.list Repository.LabProtocol driver

/// <summary>CRUD façade for the <c>formal_parameter</c> table.</summary>
[<AttachMembers>]
type FormalParameter =

    /// <summary>Inserts a new row.</summary>
    static member insert (driver: ISqliteDriver, row: FormalParameterRow) = Crud.insert Repository.FormalParameter driver row
    /// <summary>Updates the row identified by <c>row.Id</c>.</summary>
    static member update (driver: ISqliteDriver, row: FormalParameterRow) = Crud.update Repository.FormalParameter driver row
    /// <summary>Deletes the row with the given id.</summary>
    static member delete (driver: ISqliteDriver, id: string) = Crud.delete Repository.FormalParameter driver (Crud.key1 "id" id)
    /// <summary>Returns the row with the given id, or <c>None</c>.</summary>
    static member get (driver: ISqliteDriver, id: string) = Crud.get Repository.FormalParameter driver (Crud.key1 "id" id)
    /// <summary>Returns all rows ordered by primary key.</summary>
    static member list (driver: ISqliteDriver) = Crud.list Repository.FormalParameter driver

/// <summary>CRUD façade for the <c>dataset</c> table.</summary>
[<AttachMembers>]
type Dataset =

    /// <summary>Inserts a new row.</summary>
    static member insert (driver: ISqliteDriver, row: DatasetRow) = Crud.insert Repository.Dataset driver row
    /// <summary>Updates the row identified by <c>row.Id</c>.</summary>
    static member update (driver: ISqliteDriver, row: DatasetRow) = Crud.update Repository.Dataset driver row
    /// <summary>Deletes the row with the given id.</summary>
    static member delete (driver: ISqliteDriver, id: string) = Crud.delete Repository.Dataset driver (Crud.key1 "id" id)
    /// <summary>Returns the row with the given id, or <c>None</c>.</summary>
    static member get (driver: ISqliteDriver, id: string) = Crud.get Repository.Dataset driver (Crud.key1 "id" id)
    /// <summary>Returns all rows ordered by primary key.</summary>
    static member list (driver: ISqliteDriver) = Crud.list Repository.Dataset driver

/// <summary>CRUD façade for the <c>material</c> table.</summary>
[<AttachMembers>]
type Material =

    /// <summary>Inserts a new row.</summary>
    static member insert (driver: ISqliteDriver, row: MaterialRow) = Crud.insert Repository.Material driver row
    /// <summary>Updates the row identified by <c>row.Id</c>.</summary>
    static member update (driver: ISqliteDriver, row: MaterialRow) = Crud.update Repository.Material driver row
    /// <summary>Deletes the row with the given id.</summary>
    static member delete (driver: ISqliteDriver, id: string) = Crud.delete Repository.Material driver (Crud.key1 "id" id)
    /// <summary>Returns the row with the given id, or <c>None</c>.</summary>
    static member get (driver: ISqliteDriver, id: string) = Crud.get Repository.Material driver (Crud.key1 "id" id)
    /// <summary>Returns all rows ordered by primary key.</summary>
    static member list (driver: ISqliteDriver) = Crud.list Repository.Material driver

/// <summary>CRUD façade for the <c>data</c> table.</summary>
[<AttachMembers>]
type Data =

    /// <summary>Inserts a new row.</summary>
    static member insert (driver: ISqliteDriver, row: DataRow) = Crud.insert Repository.Data driver row
    /// <summary>Updates the row identified by <c>row.Id</c>.</summary>
    static member update (driver: ISqliteDriver, row: DataRow) = Crud.update Repository.Data driver row
    /// <summary>Deletes the row with the given id.</summary>
    static member delete (driver: ISqliteDriver, id: string) = Crud.delete Repository.Data driver (Crud.key1 "id" id)
    /// <summary>Returns the row with the given id, or <c>None</c>.</summary>
    static member get (driver: ISqliteDriver, id: string) = Crud.get Repository.Data driver (Crud.key1 "id" id)
    /// <summary>Returns all rows ordered by primary key.</summary>
    static member list (driver: ISqliteDriver) = Crud.list Repository.Data driver

/// <summary>CRUD façade for the <c>lab_process</c> table.</summary>
[<AttachMembers>]
type LabProcess =

    /// <summary>Inserts a new row.</summary>
    static member insert (driver: ISqliteDriver, row: LabProcessRow) = Crud.insert Repository.LabProcess driver row
    /// <summary>Updates the row identified by <c>row.Id</c>.</summary>
    static member update (driver: ISqliteDriver, row: LabProcessRow) = Crud.update Repository.LabProcess driver row
    /// <summary>Deletes the row with the given id.</summary>
    static member delete (driver: ISqliteDriver, id: string) = Crud.delete Repository.LabProcess driver (Crud.key1 "id" id)
    /// <summary>Returns the row with the given id, or <c>None</c>.</summary>
    static member get (driver: ISqliteDriver, id: string) = Crud.get Repository.LabProcess driver (Crud.key1 "id" id)
    /// <summary>Returns all rows ordered by primary key.</summary>
    static member list (driver: ISqliteDriver) = Crud.list Repository.LabProcess driver

/// <summary>CRUD façade for the <c>property_value</c> table.</summary>
[<AttachMembers>]
type PropertyValue =

    /// <summary>Inserts a new row.</summary>
    static member insert (driver: ISqliteDriver, row: PropertyValueRow) = Crud.insert Repository.PropertyValue driver row
    /// <summary>Updates the row identified by <c>row.Id</c>.</summary>
    static member update (driver: ISqliteDriver, row: PropertyValueRow) = Crud.update Repository.PropertyValue driver row
    /// <summary>Deletes the row with the given id.</summary>
    static member delete (driver: ISqliteDriver, id: string) = Crud.delete Repository.PropertyValue driver (Crud.key1 "id" id)
    /// <summary>Returns the row with the given id, or <c>None</c>.</summary>
    static member get (driver: ISqliteDriver, id: string) = Crud.get Repository.PropertyValue driver (Crud.key1 "id" id)
    /// <summary>Returns all rows ordered by primary key.</summary>
    static member list (driver: ISqliteDriver) = Crud.list Repository.PropertyValue driver

/// <summary>CRUD façade for the <c>dataset_has_part</c> association table. Primary key: <c>(dataset_id, position)</c>.</summary>
[<AttachMembers>]
type DatasetHasPart =

    /// <summary>Inserts a new row.</summary>
    static member insert (driver: ISqliteDriver, row: DatasetHasPartRow) = Crud.insert Repository.DatasetHasPart driver row
    /// <summary>Updates the row identified by <c>(row.DatasetId, row.Position)</c>.</summary>
    static member update (driver: ISqliteDriver, row: DatasetHasPartRow) = Crud.update Repository.DatasetHasPart driver row
    /// <summary>Deletes the row with the given composite key.</summary>
    static member delete (driver: ISqliteDriver, datasetId: string, position: int) = Crud.delete Repository.DatasetHasPart driver (Crud.key2 "dataset_id" datasetId "position" position)
    /// <summary>Returns the row with the given composite key, or <c>None</c>.</summary>
    static member get (driver: ISqliteDriver, datasetId: string, position: int) = Crud.get Repository.DatasetHasPart driver (Crud.key2 "dataset_id" datasetId "position" position)
    /// <summary>Returns all rows ordered by primary key.</summary>
    static member list (driver: ISqliteDriver) = Crud.list Repository.DatasetHasPart driver

/// <summary>CRUD façade for the <c>dataset_process</c> association table. Primary key: <c>(dataset_id, position)</c>.</summary>
[<AttachMembers>]
type DatasetProcess =

    /// <summary>Inserts a new row.</summary>
    static member insert (driver: ISqliteDriver, row: DatasetProcessRow) = Crud.insert Repository.DatasetProcess driver row
    /// <summary>Updates the row identified by <c>(row.DatasetId, row.Position)</c>.</summary>
    static member update (driver: ISqliteDriver, row: DatasetProcessRow) = Crud.update Repository.DatasetProcess driver row
    /// <summary>Deletes the row with the given composite key.</summary>
    static member delete (driver: ISqliteDriver, datasetId: string, position: int) = Crud.delete Repository.DatasetProcess driver (Crud.key2 "dataset_id" datasetId "position" position)
    /// <summary>Returns the row with the given composite key, or <c>None</c>.</summary>
    static member get (driver: ISqliteDriver, datasetId: string, position: int) = Crud.get Repository.DatasetProcess driver (Crud.key2 "dataset_id" datasetId "position" position)
    /// <summary>Returns all rows ordered by primary key.</summary>
    static member list (driver: ISqliteDriver) = Crud.list Repository.DatasetProcess driver

/// <summary>CRUD façade for the <c>dataset_additional_property</c> association table. Primary key: <c>(dataset_id, position)</c>.</summary>
[<AttachMembers>]
type DatasetAdditionalProperty =

    /// <summary>Inserts a new row.</summary>
    static member insert (driver: ISqliteDriver, row: DatasetAdditionalPropertyRow) = Crud.insert Repository.DatasetAdditionalProperty driver row
    /// <summary>Updates the row identified by <c>(row.DatasetId, row.Position)</c>.</summary>
    static member update (driver: ISqliteDriver, row: DatasetAdditionalPropertyRow) = Crud.update Repository.DatasetAdditionalProperty driver row
    /// <summary>Deletes the row with the given composite key.</summary>
    static member delete (driver: ISqliteDriver, datasetId: string, position: int) = Crud.delete Repository.DatasetAdditionalProperty driver (Crud.key2 "dataset_id" datasetId "position" position)
    /// <summary>Returns the row with the given composite key, or <c>None</c>.</summary>
    static member get (driver: ISqliteDriver, datasetId: string, position: int) = Crud.get Repository.DatasetAdditionalProperty driver (Crud.key2 "dataset_id" datasetId "position" position)
    /// <summary>Returns all rows ordered by primary key.</summary>
    static member list (driver: ISqliteDriver) = Crud.list Repository.DatasetAdditionalProperty driver

/// <summary>CRUD façade for the <c>protocol_parameter</c> association table. Primary key: <c>(protocol_id, position)</c>.</summary>
[<AttachMembers>]
type ProtocolParameter =

    /// <summary>Inserts a new row.</summary>
    static member insert (driver: ISqliteDriver, row: ProtocolParameterRow) = Crud.insert Repository.ProtocolParameter driver row
    /// <summary>Updates the row identified by <c>(row.ProtocolId, row.Position)</c>.</summary>
    static member update (driver: ISqliteDriver, row: ProtocolParameterRow) = Crud.update Repository.ProtocolParameter driver row
    /// <summary>Deletes the row with the given composite key.</summary>
    static member delete (driver: ISqliteDriver, protocolId: string, position: int) = Crud.delete Repository.ProtocolParameter driver (Crud.key2 "protocol_id" protocolId "position" position)
    /// <summary>Returns the row with the given composite key, or <c>None</c>.</summary>
    static member get (driver: ISqliteDriver, protocolId: string, position: int) = Crud.get Repository.ProtocolParameter driver (Crud.key2 "protocol_id" protocolId "position" position)
    /// <summary>Returns all rows ordered by primary key.</summary>
    static member list (driver: ISqliteDriver) = Crud.list Repository.ProtocolParameter driver

/// <summary>CRUD façade for the <c>process_io</c> association table. Primary key: <c>(process_id, direction, position)</c>.</summary>
[<AttachMembers>]
type ProcessIo =

    /// <summary>Inserts a new row.</summary>
    static member insert (driver: ISqliteDriver, row: ProcessIoRow) = Crud.insert Repository.ProcessIo driver row
    /// <summary>Updates the row identified by <c>(row.ProcessId, row.Direction, row.Position)</c>.</summary>
    static member update (driver: ISqliteDriver, row: ProcessIoRow) = Crud.update Repository.ProcessIo driver row
    /// <summary>Deletes the row with the given composite key.</summary>
    static member delete (driver: ISqliteDriver, processId: string, direction: ProcessIoDirection, position: int) = Crud.delete Repository.ProcessIo driver (Crud.processIoKey processId direction position)
    /// <summary>Returns the row with the given composite key, or <c>None</c>.</summary>
    static member get (driver: ISqliteDriver, processId: string, direction: ProcessIoDirection, position: int) = Crud.get Repository.ProcessIo driver (Crud.processIoKey processId direction position)
    /// <summary>Returns all rows ordered by primary key.</summary>
    static member list (driver: ISqliteDriver) = Crud.list Repository.ProcessIo driver

/// <summary>CRUD façade for the <c>process_parameter_value</c> association table. Primary key: <c>(process_id, position)</c>.</summary>
[<AttachMembers>]
type ProcessParameterValue =

    /// <summary>Inserts a new row.</summary>
    static member insert (driver: ISqliteDriver, row: ProcessParameterValueRow) = Crud.insert Repository.ProcessParameterValue driver row
    /// <summary>Updates the row identified by <c>(row.ProcessId, row.Position)</c>.</summary>
    static member update (driver: ISqliteDriver, row: ProcessParameterValueRow) = Crud.update Repository.ProcessParameterValue driver row
    /// <summary>Deletes the row with the given composite key.</summary>
    static member delete (driver: ISqliteDriver, processId: string, position: int) = Crud.delete Repository.ProcessParameterValue driver (Crud.key2 "process_id" processId "position" position)
    /// <summary>Returns the row with the given composite key, or <c>None</c>.</summary>
    static member get (driver: ISqliteDriver, processId: string, position: int) = Crud.get Repository.ProcessParameterValue driver (Crud.key2 "process_id" processId "position" position)
    /// <summary>Returns all rows ordered by primary key.</summary>
    static member list (driver: ISqliteDriver) = Crud.list Repository.ProcessParameterValue driver

/// <summary>CRUD façade for the <c>protocol_additional_property</c> association table. Primary key: <c>(protocol_id, position)</c>.</summary>
[<AttachMembers>]
type ProtocolAdditionalProperty =

    /// <summary>Inserts a new row.</summary>
    static member insert (driver: ISqliteDriver, row: ProtocolAdditionalPropertyRow) = Crud.insert Repository.ProtocolAdditionalProperty driver row
    /// <summary>Updates the row identified by <c>(row.ProtocolId, row.Position)</c>.</summary>
    static member update (driver: ISqliteDriver, row: ProtocolAdditionalPropertyRow) = Crud.update Repository.ProtocolAdditionalProperty driver row
    /// <summary>Deletes the row with the given composite key.</summary>
    static member delete (driver: ISqliteDriver, protocolId: string, position: int) = Crud.delete Repository.ProtocolAdditionalProperty driver (Crud.key2 "protocol_id" protocolId "position" position)
    /// <summary>Returns the row with the given composite key, or <c>None</c>.</summary>
    static member get (driver: ISqliteDriver, protocolId: string, position: int) = Crud.get Repository.ProtocolAdditionalProperty driver (Crud.key2 "protocol_id" protocolId "position" position)
    /// <summary>Returns all rows ordered by primary key.</summary>
    static member list (driver: ISqliteDriver) = Crud.list Repository.ProtocolAdditionalProperty driver

/// <summary>CRUD façade for the <c>material_additional_property</c> association table. Primary key: <c>(material_id, position)</c>.</summary>
[<AttachMembers>]
type MaterialAdditionalProperty =

    /// <summary>Inserts a new row.</summary>
    static member insert (driver: ISqliteDriver, row: MaterialAdditionalPropertyRow) = Crud.insert Repository.MaterialAdditionalProperty driver row
    /// <summary>Updates the row identified by <c>(row.MaterialId, row.Position)</c>.</summary>
    static member update (driver: ISqliteDriver, row: MaterialAdditionalPropertyRow) = Crud.update Repository.MaterialAdditionalProperty driver row
    /// <summary>Deletes the row with the given composite key.</summary>
    static member delete (driver: ISqliteDriver, materialId: string, position: int) = Crud.delete Repository.MaterialAdditionalProperty driver (Crud.key2 "material_id" materialId "position" position)
    /// <summary>Returns the row with the given composite key, or <c>None</c>.</summary>
    static member get (driver: ISqliteDriver, materialId: string, position: int) = Crud.get Repository.MaterialAdditionalProperty driver (Crud.key2 "material_id" materialId "position" position)
    /// <summary>Returns all rows ordered by primary key.</summary>
    static member list (driver: ISqliteDriver) = Crud.list Repository.MaterialAdditionalProperty driver

/// <summary>CRUD façade for the <c>data_additional_property</c> association table. Primary key: <c>(data_id, position)</c>.</summary>
[<AttachMembers>]
type DataAdditionalProperty =

    /// <summary>Inserts a new row.</summary>
    static member insert (driver: ISqliteDriver, row: DataAdditionalPropertyRow) = Crud.insert Repository.DataAdditionalProperty driver row
    /// <summary>Updates the row identified by <c>(row.DataId, row.Position)</c>.</summary>
    static member update (driver: ISqliteDriver, row: DataAdditionalPropertyRow) = Crud.update Repository.DataAdditionalProperty driver row
    /// <summary>Deletes the row with the given composite key.</summary>
    static member delete (driver: ISqliteDriver, dataId: string, position: int) = Crud.delete Repository.DataAdditionalProperty driver (Crud.key2 "data_id" dataId "position" position)
    /// <summary>Returns the row with the given composite key, or <c>None</c>.</summary>
    static member get (driver: ISqliteDriver, dataId: string, position: int) = Crud.get Repository.DataAdditionalProperty driver (Crud.key2 "data_id" dataId "position" position)
    /// <summary>Returns all rows ordered by primary key.</summary>
    static member list (driver: ISqliteDriver) = Crud.list Repository.DataAdditionalProperty driver

/// <summary>
/// Read-only façade for the <c>process_edges</c> view, which exposes one row per
/// (input, output) pair sharing a lab process. Useful for graph traversal of the data flow.
/// </summary>
[<AttachMembers>]
type ProcessEdges =

    /// <summary>Returns all edges ordered by <c>(process_id, input_position, output_position, input_id, output_id)</c>.</summary>
    static member list (driver: ISqliteDriver) =
        Crud.listView
            "process_edges"
            [| "process_id"; "input_position"; "output_position"; "input_kind"; "input_id"; "output_kind"; "output_id" |]
            [| "process_id"; "input_position"; "output_position"; "input_id"; "output_id" |]
            ProcessEdgeRow.ofRow
            driver

/// <summary>
/// Read-only façade for the <c>property_value_orphans</c> view, which lists ids of
/// <see cref="PropertyValueRow"/> entries no entity references. Useful for housekeeping.
/// </summary>
[<AttachMembers>]
type PropertyValueOrphans =

    /// <summary>Returns all orphan ids ordered by id.</summary>
    static member list (driver: ISqliteDriver) =
        Crud.listView
            "property_value_orphans"
            [| "id" |]
            [| "id" |]
            PropertyValueOrphanRow.ofRow
            driver
