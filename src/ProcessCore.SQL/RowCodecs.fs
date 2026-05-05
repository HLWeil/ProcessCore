namespace ProcessCore.SQL

module private RowCodecHelpers =

    let text table column row = SqlRow.text table column row

    let textOption table column row = SqlRow.textOption table column row

    let int table column row = SqlRow.int table column row

    let textParam name value = SqlParameter(name, SqlValue.Text value)

    let intParam name value = SqlParameter(name, SqlValue.Int value)

    let textOptionParam name value = SqlParameter(name, SqlValue.ofTextOption value)

    let processIoDirectionText direction =
        match direction with
        | ProcessIoDirection.Input -> "input"
        | ProcessIoDirection.Output -> "output"

    let processIoDirectionOfText table column value =
        match value with
        | "input" -> ProcessIoDirection.Input
        | "output" -> ProcessIoDirection.Output
        | other -> invalidArg $"{table}.{column}" $"Unknown process_io.direction '{other}'."

[<AutoOpen>]
module RowCodecExtensions =

    type DefinedTermRow with

        static member ofRow(row: SqlRow) =
            let table = "defined_term"

            DefinedTermRow(
                RowCodecHelpers.text table "id" row,
                RowCodecHelpers.text table "type" row,
                RowCodecHelpers.text table "name" row,
                ?Tan = RowCodecHelpers.textOption table "tan" row,
                ?InDefinedTermSetId = RowCodecHelpers.textOption table "in_defined_term_set_id" row,
                ?InDefinedTermSetName = RowCodecHelpers.textOption table "in_defined_term_set_name" row
            )

        member this.ToParameters() : SqlParameters =
            [|
                RowCodecHelpers.textParam "id" this.Id
                RowCodecHelpers.textParam "type" this.Type
                RowCodecHelpers.textParam "name" this.Name
                RowCodecHelpers.textOptionParam "tan" this.Tan
                RowCodecHelpers.textOptionParam "in_defined_term_set_id" this.InDefinedTermSetId
                RowCodecHelpers.textOptionParam "in_defined_term_set_name" this.InDefinedTermSetName
            |]

    type LabProtocolRow with

        static member ofRow(row: SqlRow) =
            let table = "lab_protocol"

            LabProtocolRow(
                RowCodecHelpers.text table "id" row,
                RowCodecHelpers.text table "type" row,
                ?AdditionalType = RowCodecHelpers.textOption table "additional_type" row,
                ?Name = RowCodecHelpers.textOption table "name" row,
                ?Description = RowCodecHelpers.textOption table "description" row,
                ?Version = RowCodecHelpers.textOption table "version" row,
                ?Url = RowCodecHelpers.textOption table "url" row,
                ?IntendedUseId = RowCodecHelpers.textOption table "intended_use_id" row,
                ?IntendedUseText = RowCodecHelpers.textOption table "intended_use_text" row
            )

        member this.ToParameters() : SqlParameters =
            [|
                RowCodecHelpers.textParam "id" this.Id
                RowCodecHelpers.textParam "type" this.Type
                RowCodecHelpers.textOptionParam "additional_type" this.AdditionalType
                RowCodecHelpers.textOptionParam "name" this.Name
                RowCodecHelpers.textOptionParam "description" this.Description
                RowCodecHelpers.textOptionParam "version" this.Version
                RowCodecHelpers.textOptionParam "url" this.Url
                RowCodecHelpers.textOptionParam "intended_use_id" this.IntendedUseId
                RowCodecHelpers.textOptionParam "intended_use_text" this.IntendedUseText
            |]

    type FormalParameterRow with

        static member ofRow(row: SqlRow) =
            let table = "formal_parameter"

            FormalParameterRow(
                RowCodecHelpers.text table "id" row,
                RowCodecHelpers.text table "type" row,
                ?Name = RowCodecHelpers.textOption table "name" row,
                ?NameTan = RowCodecHelpers.textOption table "name_tan" row,
                ?DefaultValueId = RowCodecHelpers.textOption table "default_value_id" row
            )

        member this.ToParameters() : SqlParameters =
            [|
                RowCodecHelpers.textParam "id" this.Id
                RowCodecHelpers.textParam "type" this.Type
                RowCodecHelpers.textOptionParam "name" this.Name
                RowCodecHelpers.textOptionParam "name_tan" this.NameTan
                RowCodecHelpers.textOptionParam "default_value_id" this.DefaultValueId
            |]

    type DatasetRow with

        static member ofRow(row: SqlRow) =
            let table = "dataset"

            DatasetRow(
                RowCodecHelpers.text table "id" row,
                RowCodecHelpers.text table "type" row,
                RowCodecHelpers.text table "identifier" row,
                ?AdditionalType = RowCodecHelpers.textOption table "additional_type" row,
                ?Name = RowCodecHelpers.textOption table "name" row,
                ?Description = RowCodecHelpers.textOption table "description" row
            )

        member this.ToParameters() : SqlParameters =
            [|
                RowCodecHelpers.textParam "id" this.Id
                RowCodecHelpers.textParam "type" this.Type
                RowCodecHelpers.textOptionParam "additional_type" this.AdditionalType
                RowCodecHelpers.textParam "identifier" this.Identifier
                RowCodecHelpers.textOptionParam "name" this.Name
                RowCodecHelpers.textOptionParam "description" this.Description
            |]

    type MaterialRow with

        static member ofRow(row: SqlRow) =
            let table = "material"

            MaterialRow(
                RowCodecHelpers.text table "id" row,
                RowCodecHelpers.text table "type" row,
                RowCodecHelpers.text table "name" row,
                ?AdditionalType = RowCodecHelpers.textOption table "additional_type" row
            )

        member this.ToParameters() : SqlParameters =
            [|
                RowCodecHelpers.textParam "id" this.Id
                RowCodecHelpers.textParam "type" this.Type
                RowCodecHelpers.textOptionParam "additional_type" this.AdditionalType
                RowCodecHelpers.textParam "name" this.Name
            |]

    type DataRow with

        static member ofRow(row: SqlRow) =
            let table = "data"

            DataRow(
                RowCodecHelpers.text table "id" row,
                RowCodecHelpers.text table "type" row,
                RowCodecHelpers.text table "path" row,
                ?AdditionalType = RowCodecHelpers.textOption table "additional_type" row,
                ?Selector = RowCodecHelpers.textOption table "selector" row,
                ?SelectorFormat = RowCodecHelpers.textOption table "selector_format" row,
                ?EncodingFormat = RowCodecHelpers.textOption table "encoding_format" row
            )

        member this.ToParameters() : SqlParameters =
            [|
                RowCodecHelpers.textParam "id" this.Id
                RowCodecHelpers.textParam "type" this.Type
                RowCodecHelpers.textOptionParam "additional_type" this.AdditionalType
                RowCodecHelpers.textParam "path" this.Path
                RowCodecHelpers.textOptionParam "selector" this.Selector
                RowCodecHelpers.textOptionParam "selector_format" this.SelectorFormat
                RowCodecHelpers.textOptionParam "encoding_format" this.EncodingFormat
            |]

    type LabProcessRow with

        static member ofRow(row: SqlRow) =
            let table = "lab_process"

            LabProcessRow(
                RowCodecHelpers.text table "id" row,
                RowCodecHelpers.text table "type" row,
                RowCodecHelpers.text table "name" row,
                ?AdditionalType = RowCodecHelpers.textOption table "additional_type" row,
                ?ExecutesProtocolId = RowCodecHelpers.textOption table "executes_protocol_id" row
            )

        member this.ToParameters() : SqlParameters =
            [|
                RowCodecHelpers.textParam "id" this.Id
                RowCodecHelpers.textParam "type" this.Type
                RowCodecHelpers.textOptionParam "additional_type" this.AdditionalType
                RowCodecHelpers.textParam "name" this.Name
                RowCodecHelpers.textOptionParam "executes_protocol_id" this.ExecutesProtocolId
            |]

    type PropertyValueRow with

        static member ofRow(row: SqlRow) =
            let table = "property_value"

            PropertyValueRow(
                RowCodecHelpers.text table "id" row,
                RowCodecHelpers.text table "type" row,
                RowCodecHelpers.text table "name" row,
                ?AdditionalType = RowCodecHelpers.textOption table "additional_type" row,
                ?Value = RowCodecHelpers.textOption table "value" row,
                ?Unit = RowCodecHelpers.textOption table "unit" row,
                ?NameTan = RowCodecHelpers.textOption table "name_tan" row,
                ?ValueTan = RowCodecHelpers.textOption table "value_tan" row,
                ?UnitTan = RowCodecHelpers.textOption table "unit_tan" row,
                ?InstanceOfId = RowCodecHelpers.textOption table "instance_of_id" row
            )

        member this.ToParameters() : SqlParameters =
            [|
                RowCodecHelpers.textParam "id" this.Id
                RowCodecHelpers.textParam "type" this.Type
                RowCodecHelpers.textOptionParam "additional_type" this.AdditionalType
                RowCodecHelpers.textParam "name" this.Name
                RowCodecHelpers.textOptionParam "value" this.Value
                RowCodecHelpers.textOptionParam "unit" this.Unit
                RowCodecHelpers.textOptionParam "name_tan" this.NameTan
                RowCodecHelpers.textOptionParam "value_tan" this.ValueTan
                RowCodecHelpers.textOptionParam "unit_tan" this.UnitTan
                RowCodecHelpers.textOptionParam "instance_of_id" this.InstanceOfId
            |]

    type DatasetHasPartRow with

        static member ofRow(row: SqlRow) =
            let table = "dataset_has_part"

            DatasetHasPartRow(
                RowCodecHelpers.text table "dataset_id" row,
                RowCodecHelpers.int table "position" row,
                ?PartDatasetId = RowCodecHelpers.textOption table "part_dataset_id" row,
                ?PartDataId = RowCodecHelpers.textOption table "part_data_id" row
            )

        member this.ToParameters() : SqlParameters =
            [|
                RowCodecHelpers.textParam "dataset_id" this.DatasetId
                RowCodecHelpers.intParam "position" this.Position
                RowCodecHelpers.textOptionParam "part_dataset_id" this.PartDatasetId
                RowCodecHelpers.textOptionParam "part_data_id" this.PartDataId
            |]

    type DatasetProcessRow with

        static member ofRow(row: SqlRow) =
            let table = "dataset_process"

            DatasetProcessRow(
                RowCodecHelpers.text table "dataset_id" row,
                RowCodecHelpers.int table "position" row,
                RowCodecHelpers.text table "process_id" row
            )

        member this.ToParameters() : SqlParameters =
            [|
                RowCodecHelpers.textParam "dataset_id" this.DatasetId
                RowCodecHelpers.intParam "position" this.Position
                RowCodecHelpers.textParam "process_id" this.ProcessId
            |]

    type DatasetAdditionalPropertyRow with

        static member ofRow(row: SqlRow) =
            let table = "dataset_additional_property"

            DatasetAdditionalPropertyRow(
                RowCodecHelpers.text table "dataset_id" row,
                RowCodecHelpers.int table "position" row,
                RowCodecHelpers.text table "property_value_id" row
            )

        member this.ToParameters() : SqlParameters =
            [|
                RowCodecHelpers.textParam "dataset_id" this.DatasetId
                RowCodecHelpers.intParam "position" this.Position
                RowCodecHelpers.textParam "property_value_id" this.PropertyValueId
            |]

    type ProtocolParameterRow with

        static member ofRow(row: SqlRow) =
            let table = "protocol_parameter"

            ProtocolParameterRow(
                RowCodecHelpers.text table "protocol_id" row,
                RowCodecHelpers.int table "position" row,
                RowCodecHelpers.text table "formal_parameter_id" row
            )

        member this.ToParameters() : SqlParameters =
            [|
                RowCodecHelpers.textParam "protocol_id" this.ProtocolId
                RowCodecHelpers.intParam "position" this.Position
                RowCodecHelpers.textParam "formal_parameter_id" this.FormalParameterId
            |]

    type ProcessIoRow with

        static member ofRow(row: SqlRow) =
            let table = "process_io"

            ProcessIoRow(
                RowCodecHelpers.text table "process_id" row,
                RowCodecHelpers.text table "direction" row
                |> RowCodecHelpers.processIoDirectionOfText table "direction",
                RowCodecHelpers.int table "position" row,
                ?MaterialId = RowCodecHelpers.textOption table "material_id" row,
                ?DataId = RowCodecHelpers.textOption table "data_id" row
            )

        member this.ToParameters() : SqlParameters =
            [|
                RowCodecHelpers.textParam "process_id" this.ProcessId
                RowCodecHelpers.textParam "direction" (RowCodecHelpers.processIoDirectionText this.Direction)
                RowCodecHelpers.intParam "position" this.Position
                RowCodecHelpers.textOptionParam "material_id" this.MaterialId
                RowCodecHelpers.textOptionParam "data_id" this.DataId
            |]

    type ProcessParameterValueRow with

        static member ofRow(row: SqlRow) =
            let table = "process_parameter_value"

            ProcessParameterValueRow(
                RowCodecHelpers.text table "process_id" row,
                RowCodecHelpers.int table "position" row,
                RowCodecHelpers.text table "property_value_id" row
            )

        member this.ToParameters() : SqlParameters =
            [|
                RowCodecHelpers.textParam "process_id" this.ProcessId
                RowCodecHelpers.intParam "position" this.Position
                RowCodecHelpers.textParam "property_value_id" this.PropertyValueId
            |]

    type ProtocolAdditionalPropertyRow with

        static member ofRow(row: SqlRow) =
            let table = "protocol_additional_property"

            ProtocolAdditionalPropertyRow(
                RowCodecHelpers.text table "protocol_id" row,
                RowCodecHelpers.int table "position" row,
                RowCodecHelpers.text table "property_value_id" row
            )

        member this.ToParameters() : SqlParameters =
            [|
                RowCodecHelpers.textParam "protocol_id" this.ProtocolId
                RowCodecHelpers.intParam "position" this.Position
                RowCodecHelpers.textParam "property_value_id" this.PropertyValueId
            |]

    type MaterialAdditionalPropertyRow with

        static member ofRow(row: SqlRow) =
            let table = "material_additional_property"

            MaterialAdditionalPropertyRow(
                RowCodecHelpers.text table "material_id" row,
                RowCodecHelpers.int table "position" row,
                RowCodecHelpers.text table "property_value_id" row
            )

        member this.ToParameters() : SqlParameters =
            [|
                RowCodecHelpers.textParam "material_id" this.MaterialId
                RowCodecHelpers.intParam "position" this.Position
                RowCodecHelpers.textParam "property_value_id" this.PropertyValueId
            |]

    type DataAdditionalPropertyRow with

        static member ofRow(row: SqlRow) =
            let table = "data_additional_property"

            DataAdditionalPropertyRow(
                RowCodecHelpers.text table "data_id" row,
                RowCodecHelpers.int table "position" row,
                RowCodecHelpers.text table "property_value_id" row
            )

        member this.ToParameters() : SqlParameters =
            [|
                RowCodecHelpers.textParam "data_id" this.DataId
                RowCodecHelpers.intParam "position" this.Position
                RowCodecHelpers.textParam "property_value_id" this.PropertyValueId
            |]

    type ProcessEdgeRow with

        static member ofRow(row: SqlRow) =
            let table = "process_edges"

            ProcessEdgeRow(
                RowCodecHelpers.text table "process_id" row,
                RowCodecHelpers.int table "input_position" row,
                RowCodecHelpers.int table "output_position" row,
                RowCodecHelpers.text table "input_kind" row,
                RowCodecHelpers.text table "input_id" row,
                RowCodecHelpers.text table "output_kind" row,
                RowCodecHelpers.text table "output_id" row
            )

    type PropertyValueOrphanRow with

        static member ofRow(row: SqlRow) =
            let table = "property_value_orphans"

            PropertyValueOrphanRow(RowCodecHelpers.text table "id" row)
