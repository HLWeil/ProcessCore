namespace ProcessCore.SQL

[<RequireQualifiedAccess>]
module RowCodecs =

    let private text table column row = SqlRow.text table column row

    let private textOption table column row = SqlRow.textOption table column row

    let private int table column row = SqlRow.int table column row

    let private p name value = name, value

    let private textParam name value = p name (SqlValue.Text value)

    let private intParam name value = p name (SqlValue.Int value)

    let private textOptionParam name value = p name (SqlValue.ofTextOption value)

    [<RequireQualifiedAccess>]
    module DefinedTerm =

        let ofRow row =
            {
                Id = text "defined_term" "id" row
                Type = text "defined_term" "type" row
                Name = text "defined_term" "name" row
                Tan = textOption "defined_term" "tan" row
                InDefinedTermSetId = textOption "defined_term" "in_defined_term_set_id" row
                InDefinedTermSetName = textOption "defined_term" "in_defined_term_set_name" row
            }

        let toParameters (row: DefinedTermRow) =
            [
                textParam "id" row.Id
                textParam "type" row.Type
                textParam "name" row.Name
                textOptionParam "tan" row.Tan
                textOptionParam "in_defined_term_set_id" row.InDefinedTermSetId
                textOptionParam "in_defined_term_set_name" row.InDefinedTermSetName
            ]

    [<RequireQualifiedAccess>]
    module LabProtocol =

        let ofRow row =
            {
                Id = text "lab_protocol" "id" row
                Type = text "lab_protocol" "type" row
                AdditionalType = textOption "lab_protocol" "additional_type" row
                Name = textOption "lab_protocol" "name" row
                Description = textOption "lab_protocol" "description" row
                Version = textOption "lab_protocol" "version" row
                Url = textOption "lab_protocol" "url" row
                IntendedUseId = textOption "lab_protocol" "intended_use_id" row
                IntendedUseText = textOption "lab_protocol" "intended_use_text" row
            }

        let toParameters (row: LabProtocolRow) =
            [
                textParam "id" row.Id
                textParam "type" row.Type
                textOptionParam "additional_type" row.AdditionalType
                textOptionParam "name" row.Name
                textOptionParam "description" row.Description
                textOptionParam "version" row.Version
                textOptionParam "url" row.Url
                textOptionParam "intended_use_id" row.IntendedUseId
                textOptionParam "intended_use_text" row.IntendedUseText
            ]

    [<RequireQualifiedAccess>]
    module FormalParameter =

        let ofRow row =
            {
                Id = text "formal_parameter" "id" row
                Type = text "formal_parameter" "type" row
                Name = textOption "formal_parameter" "name" row
                NameTan = textOption "formal_parameter" "name_tan" row
                DefaultValueId = textOption "formal_parameter" "default_value_id" row
            }

        let toParameters (row: FormalParameterRow) =
            [
                textParam "id" row.Id
                textParam "type" row.Type
                textOptionParam "name" row.Name
                textOptionParam "name_tan" row.NameTan
                textOptionParam "default_value_id" row.DefaultValueId
            ]

    [<RequireQualifiedAccess>]
    module Dataset =

        let ofRow row =
            {
                Id = text "dataset" "id" row
                Type = text "dataset" "type" row
                AdditionalType = textOption "dataset" "additional_type" row
                Identifier = text "dataset" "identifier" row
                Name = textOption "dataset" "name" row
                Description = textOption "dataset" "description" row
            }

        let toParameters (row: DatasetRow) =
            [
                textParam "id" row.Id
                textParam "type" row.Type
                textOptionParam "additional_type" row.AdditionalType
                textParam "identifier" row.Identifier
                textOptionParam "name" row.Name
                textOptionParam "description" row.Description
            ]

    [<RequireQualifiedAccess>]
    module Material =

        let ofRow row =
            {
                Id = text "material" "id" row
                Type = text "material" "type" row
                AdditionalType = textOption "material" "additional_type" row
                Name = text "material" "name" row
            }

        let toParameters (row: MaterialRow) =
            [
                textParam "id" row.Id
                textParam "type" row.Type
                textOptionParam "additional_type" row.AdditionalType
                textParam "name" row.Name
            ]

    [<RequireQualifiedAccess>]
    module Data =

        let ofRow row =
            {
                Id = text "data" "id" row
                Type = text "data" "type" row
                AdditionalType = textOption "data" "additional_type" row
                Path = text "data" "path" row
                Selector = textOption "data" "selector" row
                SelectorFormat = textOption "data" "selector_format" row
                EncodingFormat = textOption "data" "encoding_format" row
            }

        let toParameters (row: DataRow) =
            [
                textParam "id" row.Id
                textParam "type" row.Type
                textOptionParam "additional_type" row.AdditionalType
                textParam "path" row.Path
                textOptionParam "selector" row.Selector
                textOptionParam "selector_format" row.SelectorFormat
                textOptionParam "encoding_format" row.EncodingFormat
            ]

    [<RequireQualifiedAccess>]
    module LabProcess =

        let ofRow row =
            {
                Id = text "lab_process" "id" row
                Type = text "lab_process" "type" row
                AdditionalType = textOption "lab_process" "additional_type" row
                Name = text "lab_process" "name" row
                ExecutesProtocolId = textOption "lab_process" "executes_protocol_id" row
            }

        let toParameters (row: LabProcessRow) =
            [
                textParam "id" row.Id
                textParam "type" row.Type
                textOptionParam "additional_type" row.AdditionalType
                textParam "name" row.Name
                textOptionParam "executes_protocol_id" row.ExecutesProtocolId
            ]

    [<RequireQualifiedAccess>]
    module PropertyValue =

        let ofRow row =
            {
                Id = text "property_value" "id" row
                Type = text "property_value" "type" row
                AdditionalType = textOption "property_value" "additional_type" row
                Name = text "property_value" "name" row
                Value = textOption "property_value" "value" row
                Unit = textOption "property_value" "unit" row
                NameTan = textOption "property_value" "name_tan" row
                ValueTan = textOption "property_value" "value_tan" row
                UnitTan = textOption "property_value" "unit_tan" row
                InstanceOfId = textOption "property_value" "instance_of_id" row
            }

        let toParameters (row: PropertyValueRow) =
            [
                textParam "id" row.Id
                textParam "type" row.Type
                textOptionParam "additional_type" row.AdditionalType
                textParam "name" row.Name
                textOptionParam "value" row.Value
                textOptionParam "unit" row.Unit
                textOptionParam "name_tan" row.NameTan
                textOptionParam "value_tan" row.ValueTan
                textOptionParam "unit_tan" row.UnitTan
                textOptionParam "instance_of_id" row.InstanceOfId
            ]

    [<RequireQualifiedAccess>]
    module DatasetHasPart =

        let ofRow row =
            {
                DatasetId = text "dataset_has_part" "dataset_id" row
                Position = int "dataset_has_part" "position" row
                PartDatasetId = textOption "dataset_has_part" "part_dataset_id" row
                PartDataId = textOption "dataset_has_part" "part_data_id" row
            }

        let toParameters (row: DatasetHasPartRow) =
            [
                textParam "dataset_id" row.DatasetId
                intParam "position" row.Position
                textOptionParam "part_dataset_id" row.PartDatasetId
                textOptionParam "part_data_id" row.PartDataId
            ]

    [<RequireQualifiedAccess>]
    module DatasetProcess =

        let ofRow row =
            {
                DatasetId = text "dataset_process" "dataset_id" row
                Position = int "dataset_process" "position" row
                ProcessId = text "dataset_process" "process_id" row
            }

        let toParameters (row: DatasetProcessRow) =
            [
                textParam "dataset_id" row.DatasetId
                intParam "position" row.Position
                textParam "process_id" row.ProcessId
            ]

    [<RequireQualifiedAccess>]
    module DatasetAdditionalProperty =

        let ofRow row =
            {
                DatasetId = text "dataset_additional_property" "dataset_id" row
                Position = int "dataset_additional_property" "position" row
                PropertyValueId = text "dataset_additional_property" "property_value_id" row
            }

        let toParameters (row: DatasetAdditionalPropertyRow) =
            [
                textParam "dataset_id" row.DatasetId
                intParam "position" row.Position
                textParam "property_value_id" row.PropertyValueId
            ]

    [<RequireQualifiedAccess>]
    module ProtocolParameter =

        let ofRow row =
            {
                ProtocolId = text "protocol_parameter" "protocol_id" row
                Position = int "protocol_parameter" "position" row
                FormalParameterId = text "protocol_parameter" "formal_parameter_id" row
            }

        let toParameters (row: ProtocolParameterRow) =
            [
                textParam "protocol_id" row.ProtocolId
                intParam "position" row.Position
                textParam "formal_parameter_id" row.FormalParameterId
            ]

    [<RequireQualifiedAccess>]
    module ProcessIo =

        let ofRow row =
            {
                ProcessId = text "process_io" "process_id" row
                Direction = text "process_io" "direction" row |> ProcessIoDirection.ofSql
                Position = int "process_io" "position" row
                MaterialId = textOption "process_io" "material_id" row
                DataId = textOption "process_io" "data_id" row
            }

        let toParameters (row: ProcessIoRow) =
            [
                textParam "process_id" row.ProcessId
                textParam "direction" row.Direction.Sql
                intParam "position" row.Position
                textOptionParam "material_id" row.MaterialId
                textOptionParam "data_id" row.DataId
            ]

    [<RequireQualifiedAccess>]
    module ProcessParameterValue =

        let ofRow row =
            {
                ProcessId = text "process_parameter_value" "process_id" row
                Position = int "process_parameter_value" "position" row
                PropertyValueId = text "process_parameter_value" "property_value_id" row
            }

        let toParameters (row: ProcessParameterValueRow) =
            [
                textParam "process_id" row.ProcessId
                intParam "position" row.Position
                textParam "property_value_id" row.PropertyValueId
            ]

    [<RequireQualifiedAccess>]
    module ProtocolAdditionalProperty =

        let ofRow row =
            {
                ProtocolId = text "protocol_additional_property" "protocol_id" row
                Position = int "protocol_additional_property" "position" row
                PropertyValueId = text "protocol_additional_property" "property_value_id" row
            }

        let toParameters (row: ProtocolAdditionalPropertyRow) =
            [
                textParam "protocol_id" row.ProtocolId
                intParam "position" row.Position
                textParam "property_value_id" row.PropertyValueId
            ]

    [<RequireQualifiedAccess>]
    module MaterialAdditionalProperty =

        let ofRow row =
            {
                MaterialId = text "material_additional_property" "material_id" row
                Position = int "material_additional_property" "position" row
                PropertyValueId = text "material_additional_property" "property_value_id" row
            }

        let toParameters (row: MaterialAdditionalPropertyRow) =
            [
                textParam "material_id" row.MaterialId
                intParam "position" row.Position
                textParam "property_value_id" row.PropertyValueId
            ]

    [<RequireQualifiedAccess>]
    module DataAdditionalProperty =

        let ofRow row =
            {
                DataId = text "data_additional_property" "data_id" row
                Position = int "data_additional_property" "position" row
                PropertyValueId = text "data_additional_property" "property_value_id" row
            }

        let toParameters (row: DataAdditionalPropertyRow) =
            [
                textParam "data_id" row.DataId
                intParam "position" row.Position
                textParam "property_value_id" row.PropertyValueId
            ]

    [<RequireQualifiedAccess>]
    module ProcessEdge =

        let ofRow row =
            {
                ProcessId = text "process_edges" "process_id" row
                InputPosition = int "process_edges" "input_position" row
                OutputPosition = int "process_edges" "output_position" row
                InputKind = text "process_edges" "input_kind" row
                InputId = text "process_edges" "input_id" row
                OutputKind = text "process_edges" "output_kind" row
                OutputId = text "process_edges" "output_id" row
            }

    [<RequireQualifiedAccess>]
    module PropertyValueOrphan =

        let ofRow row =
            {
                Id = text "property_value_orphans" "id" row
            }
