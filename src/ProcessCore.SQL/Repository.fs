namespace ProcessCore.SQL

open Fable.Core

/// Table-shaped metadata used by the first repository layer.
[<AttachMembers>]
type Table<'row>(
    Name: string,
    Columns: string[],
    PrimaryKey: string[],
    OfRow: SqlRow -> 'row,
    ToParameters: 'row -> SqlParameters
) =

    member val Name = Name with get, set
    member val Columns = Columns with get, set
    member val PrimaryKey = PrimaryKey with get, set
    member val OfRow = OfRow with get, set
    member val ToParameters = ToParameters with get, set

    [<NamedParams>]
    static member create(
        Name: string,
        Columns: string[],
        PrimaryKey: string[],
        OfRow: SqlRow -> 'row,
        ToParameters: 'row -> SqlParameters
    ) =
        Table<'row>(Name, Columns, PrimaryKey, OfRow, ToParameters)

[<AttachMembers>]
type Repository =

    static member private table name columns primaryKey ofRow toParameters =
        Table(name, columns, primaryKey, ofRow, toParameters)

    static member DefinedTerm =
        Repository.table
            "defined_term"
            [| "id"; "type"; "name"; "tan"; "in_defined_term_set_id"; "in_defined_term_set_name" |]
            [| "id" |]
            DefinedTermRow.ofRow
            (fun row -> row.ToParameters())

    static member LabProtocol =
        Repository.table
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

    static member FormalParameter =
        Repository.table
            "formal_parameter"
            [| "id"; "type"; "name"; "name_tan"; "default_value_id" |]
            [| "id" |]
            FormalParameterRow.ofRow
            (fun row -> row.ToParameters())

    static member Dataset =
        Repository.table
            "dataset"
            [| "id"; "type"; "additional_type"; "identifier"; "name"; "description" |]
            [| "id" |]
            DatasetRow.ofRow
            (fun row -> row.ToParameters())

    static member Material =
        Repository.table
            "material"
            [| "id"; "type"; "additional_type"; "name" |]
            [| "id" |]
            MaterialRow.ofRow
            (fun row -> row.ToParameters())

    static member Data =
        Repository.table
            "data"
            [| "id"; "type"; "additional_type"; "path"; "selector"; "selector_format"; "encoding_format" |]
            [| "id" |]
            DataRow.ofRow
            (fun row -> row.ToParameters())

    static member LabProcess =
        Repository.table
            "lab_process"
            [| "id"; "type"; "additional_type"; "name"; "executes_protocol_id" |]
            [| "id" |]
            LabProcessRow.ofRow
            (fun row -> row.ToParameters())

    static member PropertyValue =
        Repository.table
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

    static member DatasetHasPart =
        Repository.table
            "dataset_has_part"
            [| "dataset_id"; "position"; "part_dataset_id"; "part_data_id" |]
            [| "dataset_id"; "position" |]
            DatasetHasPartRow.ofRow
            (fun row -> row.ToParameters())

    static member DatasetProcess =
        Repository.table
            "dataset_process"
            [| "dataset_id"; "position"; "process_id" |]
            [| "dataset_id"; "position" |]
            DatasetProcessRow.ofRow
            (fun row -> row.ToParameters())

    static member DatasetAdditionalProperty =
        Repository.table
            "dataset_additional_property"
            [| "dataset_id"; "position"; "property_value_id" |]
            [| "dataset_id"; "position" |]
            DatasetAdditionalPropertyRow.ofRow
            (fun row -> row.ToParameters())

    static member ProtocolParameter =
        Repository.table
            "protocol_parameter"
            [| "protocol_id"; "position"; "formal_parameter_id" |]
            [| "protocol_id"; "position" |]
            ProtocolParameterRow.ofRow
            (fun row -> row.ToParameters())

    static member ProcessIo =
        Repository.table
            "process_io"
            [| "process_id"; "direction"; "position"; "material_id"; "data_id" |]
            [| "process_id"; "direction"; "position" |]
            ProcessIoRow.ofRow
            (fun row -> row.ToParameters())

    static member ProcessParameterValue =
        Repository.table
            "process_parameter_value"
            [| "process_id"; "position"; "property_value_id" |]
            [| "process_id"; "position" |]
            ProcessParameterValueRow.ofRow
            (fun row -> row.ToParameters())

    static member ProtocolAdditionalProperty =
        Repository.table
            "protocol_additional_property"
            [| "protocol_id"; "position"; "property_value_id" |]
            [| "protocol_id"; "position" |]
            ProtocolAdditionalPropertyRow.ofRow
            (fun row -> row.ToParameters())

    static member MaterialAdditionalProperty =
        Repository.table
            "material_additional_property"
            [| "material_id"; "position"; "property_value_id" |]
            [| "material_id"; "position" |]
            MaterialAdditionalPropertyRow.ofRow
            (fun row -> row.ToParameters())

    static member DataAdditionalProperty =
        Repository.table
            "data_additional_property"
            [| "data_id"; "position"; "property_value_id" |]
            [| "data_id"; "position" |]
            DataAdditionalPropertyRow.ofRow
            (fun row -> row.ToParameters())

    static member EntityTables =
        [|
            Repository.DefinedTerm.Name
            Repository.LabProtocol.Name
            Repository.FormalParameter.Name
            Repository.Dataset.Name
            Repository.Material.Name
            Repository.Data.Name
            Repository.LabProcess.Name
            Repository.PropertyValue.Name
        |]

    static member AssociationTables =
        [|
            Repository.DatasetHasPart.Name
            Repository.DatasetProcess.Name
            Repository.DatasetAdditionalProperty.Name
            Repository.ProtocolParameter.Name
            Repository.ProcessIo.Name
            Repository.ProcessParameterValue.Name
            Repository.ProtocolAdditionalProperty.Name
            Repository.MaterialAdditionalProperty.Name
            Repository.DataAdditionalProperty.Name
        |]
