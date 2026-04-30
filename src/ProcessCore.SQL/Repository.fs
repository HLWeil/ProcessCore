namespace ProcessCore.SQL

/// Table-shaped metadata used by the first repository layer.
type Table<'row> =
    {
        Name: string
        Columns: string list
        PrimaryKey: string list
        OfRow: SqlRow -> 'row
        ToParameters: 'row -> SqlParameters
    }

[<RequireQualifiedAccess>]
module Repository =

    let private table name columns primaryKey ofRow toParameters =
        {
            Name = name
            Columns = columns
            PrimaryKey = primaryKey
            OfRow = ofRow
            ToParameters = toParameters
        }

    let definedTerm =
        table
            "defined_term"
            [ "id"; "type"; "name"; "tan"; "in_defined_term_set_id"; "in_defined_term_set_name" ]
            [ "id" ]
            RowCodecs.DefinedTerm.ofRow
            RowCodecs.DefinedTerm.toParameters

    let labProtocol =
        table
            "lab_protocol"
            [ "id"; "type"; "additional_type"; "name"; "description"; "version"; "url"; "intended_use_id"; "intended_use_text" ]
            [ "id" ]
            RowCodecs.LabProtocol.ofRow
            RowCodecs.LabProtocol.toParameters

    let formalParameter =
        table
            "formal_parameter"
            [ "id"; "type"; "name"; "name_tan"; "default_value_id" ]
            [ "id" ]
            RowCodecs.FormalParameter.ofRow
            RowCodecs.FormalParameter.toParameters

    let dataset =
        table
            "dataset"
            [ "id"; "type"; "additional_type"; "identifier"; "name"; "description" ]
            [ "id" ]
            RowCodecs.Dataset.ofRow
            RowCodecs.Dataset.toParameters

    let material =
        table
            "material"
            [ "id"; "type"; "additional_type"; "name" ]
            [ "id" ]
            RowCodecs.Material.ofRow
            RowCodecs.Material.toParameters

    let data =
        table
            "data"
            [ "id"; "type"; "additional_type"; "path"; "selector"; "selector_format"; "encoding_format" ]
            [ "id" ]
            RowCodecs.Data.ofRow
            RowCodecs.Data.toParameters

    let labProcess =
        table
            "lab_process"
            [ "id"; "type"; "additional_type"; "name"; "executes_protocol_id" ]
            [ "id" ]
            RowCodecs.LabProcess.ofRow
            RowCodecs.LabProcess.toParameters

    let propertyValue =
        table
            "property_value"
            [ "id"; "type"; "additional_type"; "name"; "value"; "unit"; "name_tan"; "value_tan"; "unit_tan"; "instance_of_id" ]
            [ "id" ]
            RowCodecs.PropertyValue.ofRow
            RowCodecs.PropertyValue.toParameters

    let datasetHasPart =
        table
            "dataset_has_part"
            [ "dataset_id"; "position"; "part_dataset_id"; "part_data_id" ]
            [ "dataset_id"; "position" ]
            RowCodecs.DatasetHasPart.ofRow
            RowCodecs.DatasetHasPart.toParameters

    let datasetProcess =
        table
            "dataset_process"
            [ "dataset_id"; "position"; "process_id" ]
            [ "dataset_id"; "position" ]
            RowCodecs.DatasetProcess.ofRow
            RowCodecs.DatasetProcess.toParameters

    let datasetAdditionalProperty =
        table
            "dataset_additional_property"
            [ "dataset_id"; "position"; "property_value_id" ]
            [ "dataset_id"; "position" ]
            RowCodecs.DatasetAdditionalProperty.ofRow
            RowCodecs.DatasetAdditionalProperty.toParameters

    let protocolParameter =
        table
            "protocol_parameter"
            [ "protocol_id"; "position"; "formal_parameter_id" ]
            [ "protocol_id"; "position" ]
            RowCodecs.ProtocolParameter.ofRow
            RowCodecs.ProtocolParameter.toParameters

    let processIo =
        table
            "process_io"
            [ "process_id"; "direction"; "position"; "material_id"; "data_id" ]
            [ "process_id"; "direction"; "position" ]
            RowCodecs.ProcessIo.ofRow
            RowCodecs.ProcessIo.toParameters

    let processParameterValue =
        table
            "process_parameter_value"
            [ "process_id"; "position"; "property_value_id" ]
            [ "process_id"; "position" ]
            RowCodecs.ProcessParameterValue.ofRow
            RowCodecs.ProcessParameterValue.toParameters

    let protocolAdditionalProperty =
        table
            "protocol_additional_property"
            [ "protocol_id"; "position"; "property_value_id" ]
            [ "protocol_id"; "position" ]
            RowCodecs.ProtocolAdditionalProperty.ofRow
            RowCodecs.ProtocolAdditionalProperty.toParameters

    let materialAdditionalProperty =
        table
            "material_additional_property"
            [ "material_id"; "position"; "property_value_id" ]
            [ "material_id"; "position" ]
            RowCodecs.MaterialAdditionalProperty.ofRow
            RowCodecs.MaterialAdditionalProperty.toParameters

    let dataAdditionalProperty =
        table
            "data_additional_property"
            [ "data_id"; "position"; "property_value_id" ]
            [ "data_id"; "position" ]
            RowCodecs.DataAdditionalProperty.ofRow
            RowCodecs.DataAdditionalProperty.toParameters

    let entityTables =
        [
            definedTerm.Name
            labProtocol.Name
            formalParameter.Name
            dataset.Name
            material.Name
            data.Name
            labProcess.Name
            propertyValue.Name
        ]

    let associationTables =
        [
            datasetHasPart.Name
            datasetProcess.Name
            datasetAdditionalProperty.Name
            protocolParameter.Name
            processIo.Name
            processParameterValue.Name
            protocolAdditionalProperty.Name
            materialAdditionalProperty.Name
            dataAdditionalProperty.Name
        ]
