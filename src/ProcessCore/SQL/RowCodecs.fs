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

/// <summary>
/// Bidirectional codecs between row records and the on-the-wire <see cref="SqlRow"/> /
/// <see cref="SqlParameters"/> representations consumed by <see cref="ISqliteDriver"/>.
/// </summary>
/// <remarks>
/// Each entity type defined in <c>Tables.fs</c> is augmented with two members:
/// <list type="bullet">
///   <item><description>
///     <c>static member ofRow : SqlRow -&gt; row</c> — decodes a query result row, raising
///     <see cref="System.ArgumentException"/> with a fully qualified <c>table.column</c> tag if a
///     required column is missing or has the wrong storage class.
///   </description></item>
///   <item><description>
///     <c>member this.ToParameters : unit -&gt; SqlParameters</c> — encodes the row to the parameter
///     array consumed by the repository's INSERT/UPDATE statements.
///   </description></item>
/// </list>
/// The module is <c>AutoOpen</c>, so consumers obtain these members simply by opening the
/// <c>ProcessCore.SQL</c> namespace.
/// </remarks>
[<AutoOpen>]
module RowCodecExtensions =

    type DefinedTermRow with

        /// <summary>Decodes a <see cref="DefinedTermRow"/> from a <c>defined_term</c> result row.</summary>
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

        /// <summary>Encodes the row to <see cref="SqlParameters"/> for INSERT/UPDATE on <c>defined_term</c>.</summary>
        member this.ToParameters() : SqlParameters =
            [|
                RowCodecHelpers.textParam "id" this.Id
                RowCodecHelpers.textParam "type" this.Type
                RowCodecHelpers.textParam "name" this.Name
                RowCodecHelpers.textOptionParam "tan" this.Tan
                RowCodecHelpers.textOptionParam "in_defined_term_set_id" this.InDefinedTermSetId
                RowCodecHelpers.textOptionParam "in_defined_term_set_name" this.InDefinedTermSetName
            |]

    type RecipeRow with

        /// <summary>Decodes a <see cref="RecipeRow"/> from a <c>recipe</c> result row.</summary>
        static member ofRow(row: SqlRow) =
            let table = "recipe"

            RecipeRow(
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

        /// <summary>Encodes the row to <see cref="SqlParameters"/> for INSERT/UPDATE on <c>recipe</c>.</summary>
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

        /// <summary>Decodes a <see cref="FormalParameterRow"/> from a <c>formal_parameter</c> result row.</summary>
        static member ofRow(row: SqlRow) =
            let table = "formal_parameter"

            FormalParameterRow(
                RowCodecHelpers.text table "id" row,
                RowCodecHelpers.text table "type" row,
                ?Name = RowCodecHelpers.textOption table "name" row,
                ?NameTan = RowCodecHelpers.textOption table "name_tan" row,
                ?DefaultValueId = RowCodecHelpers.textOption table "default_value_id" row
            )

        /// <summary>Encodes the row to <see cref="SqlParameters"/> for INSERT/UPDATE on <c>formal_parameter</c>.</summary>
        member this.ToParameters() : SqlParameters =
            [|
                RowCodecHelpers.textParam "id" this.Id
                RowCodecHelpers.textParam "type" this.Type
                RowCodecHelpers.textOptionParam "name" this.Name
                RowCodecHelpers.textOptionParam "name_tan" this.NameTan
                RowCodecHelpers.textOptionParam "default_value_id" this.DefaultValueId
            |]

    type DatasetRow with

        /// <summary>Decodes a <see cref="DatasetRow"/> from a <c>dataset</c> result row.</summary>
        static member ofRow(row: SqlRow) =
            let table = "dataset"

            DatasetRow(
                RowCodecHelpers.text table "id" row,
                RowCodecHelpers.text table "type" row,
                RowCodecHelpers.text table "identifier" row,
                ?AdditionalType = RowCodecHelpers.textOption table "additional_type" row,
                ?Title = RowCodecHelpers.textOption table "title" row,
                ?Description = RowCodecHelpers.textOption table "description" row
            )

        /// <summary>Encodes the row to <see cref="SqlParameters"/> for INSERT/UPDATE on <c>dataset</c>.</summary>
        member this.ToParameters() : SqlParameters =
            [|
                RowCodecHelpers.textParam "id" this.Id
                RowCodecHelpers.textParam "type" this.Type
                RowCodecHelpers.textOptionParam "additional_type" this.AdditionalType
                RowCodecHelpers.textParam "identifier" this.Identifier
                RowCodecHelpers.textOptionParam "title" this.Title
                RowCodecHelpers.textOptionParam "description" this.Description
            |]

    type SampleRow with

        /// <summary>Decodes a <see cref="SampleRow"/> from a <c>sample</c> result row.</summary>
        static member ofRow(row: SqlRow) =
            let table = "sample"

            SampleRow(
                RowCodecHelpers.text table "id" row,
                RowCodecHelpers.text table "type" row,
                RowCodecHelpers.text table "name" row,
                ?AdditionalType = RowCodecHelpers.textOption table "additional_type" row
            )

        /// <summary>Encodes the row to <see cref="SqlParameters"/> for INSERT/UPDATE on <c>sample</c>.</summary>
        member this.ToParameters() : SqlParameters =
            [|
                RowCodecHelpers.textParam "id" this.Id
                RowCodecHelpers.textParam "type" this.Type
                RowCodecHelpers.textOptionParam "additional_type" this.AdditionalType
                RowCodecHelpers.textParam "name" this.Name
            |]

    type DataRow with

        /// <summary>Decodes a <see cref="DataRow"/> from a <c>data</c> result row.</summary>
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

        /// <summary>Encodes the row to <see cref="SqlParameters"/> for INSERT/UPDATE on <c>data</c>.</summary>
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

    type ProcessRow with

        /// <summary>Decodes a <see cref="ProcessRow"/> from a <c>process</c> result row.</summary>
        static member ofRow(row: SqlRow) =
            let table = "process"

            ProcessRow(
                RowCodecHelpers.text table "id" row,
                RowCodecHelpers.text table "type" row,
                RowCodecHelpers.text table "name" row,
                ?AdditionalType = RowCodecHelpers.textOption table "additional_type" row,
                ?ExecutesProtocolId = RowCodecHelpers.textOption table "executes_protocol_id" row
            )

        /// <summary>Encodes the row to <see cref="SqlParameters"/> for INSERT/UPDATE on <c>process</c>.</summary>
        member this.ToParameters() : SqlParameters =
            [|
                RowCodecHelpers.textParam "id" this.Id
                RowCodecHelpers.textParam "type" this.Type
                RowCodecHelpers.textOptionParam "additional_type" this.AdditionalType
                RowCodecHelpers.textParam "name" this.Name
                RowCodecHelpers.textOptionParam "executes_protocol_id" this.ExecutesProtocolId
            |]

    type AnnotationRow with

        /// <summary>Decodes a <see cref="AnnotationRow"/> from a <c>annotation</c> result row.</summary>
        static member ofRow(row: SqlRow) =
            let table = "annotation"

            AnnotationRow(
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

        /// <summary>Encodes the row to <see cref="SqlParameters"/> for INSERT/UPDATE on <c>annotation</c>.</summary>
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

        /// <summary>Decodes a <see cref="DatasetHasPartRow"/> from a <c>dataset_has_part</c> result row.</summary>
        static member ofRow(row: SqlRow) =
            let table = "dataset_has_part"

            DatasetHasPartRow(
                RowCodecHelpers.text table "dataset_id" row,
                RowCodecHelpers.int table "position" row,
                ?PartDatasetId = RowCodecHelpers.textOption table "part_dataset_id" row,
                ?PartDataId = RowCodecHelpers.textOption table "part_data_id" row
            )

        /// <summary>Encodes the row to <see cref="SqlParameters"/> for INSERT/UPDATE on <c>dataset_has_part</c>.</summary>
        member this.ToParameters() : SqlParameters =
            [|
                RowCodecHelpers.textParam "dataset_id" this.DatasetId
                RowCodecHelpers.intParam "position" this.Position
                RowCodecHelpers.textOptionParam "part_dataset_id" this.PartDatasetId
                RowCodecHelpers.textOptionParam "part_data_id" this.PartDataId
            |]

    type DatasetProcessRow with

        /// <summary>Decodes a <see cref="DatasetProcessRow"/> from a <c>dataset_process</c> result row.</summary>
        static member ofRow(row: SqlRow) =
            let table = "dataset_process"

            DatasetProcessRow(
                RowCodecHelpers.text table "dataset_id" row,
                RowCodecHelpers.int table "position" row,
                RowCodecHelpers.text table "process_id" row
            )

        /// <summary>Encodes the row to <see cref="SqlParameters"/> for INSERT/UPDATE on <c>dataset_process</c>.</summary>
        member this.ToParameters() : SqlParameters =
            [|
                RowCodecHelpers.textParam "dataset_id" this.DatasetId
                RowCodecHelpers.intParam "position" this.Position
                RowCodecHelpers.textParam "process_id" this.ProcessId
            |]

    type DatasetAdditionalPropertyRow with

        /// <summary>Decodes a <see cref="DatasetAdditionalPropertyRow"/> from a <c>dataset_additional_property</c> result row.</summary>
        static member ofRow(row: SqlRow) =
            let table = "dataset_additional_property"

            DatasetAdditionalPropertyRow(
                RowCodecHelpers.text table "dataset_id" row,
                RowCodecHelpers.int table "position" row,
                RowCodecHelpers.text table "annotation_id" row
            )

        /// <summary>Encodes the row to <see cref="SqlParameters"/> for INSERT/UPDATE on <c>dataset_additional_property</c>.</summary>
        member this.ToParameters() : SqlParameters =
            [|
                RowCodecHelpers.textParam "dataset_id" this.DatasetId
                RowCodecHelpers.intParam "position" this.Position
                RowCodecHelpers.textParam "annotation_id" this.AnnotationId
            |]

    type ProtocolParameterRow with

        /// <summary>Decodes a <see cref="ProtocolParameterRow"/> from a <c>protocol_parameter</c> result row.</summary>
        static member ofRow(row: SqlRow) =
            let table = "protocol_parameter"

            ProtocolParameterRow(
                RowCodecHelpers.text table "protocol_id" row,
                RowCodecHelpers.int table "position" row,
                RowCodecHelpers.text table "formal_parameter_id" row
            )

        /// <summary>Encodes the row to <see cref="SqlParameters"/> for INSERT/UPDATE on <c>protocol_parameter</c>.</summary>
        member this.ToParameters() : SqlParameters =
            [|
                RowCodecHelpers.textParam "protocol_id" this.ProtocolId
                RowCodecHelpers.intParam "position" this.Position
                RowCodecHelpers.textParam "formal_parameter_id" this.FormalParameterId
            |]

    type ProcessIoRow with

        /// <summary>Decodes a <see cref="ProcessIoRow"/> from a <c>process_io</c> result row, including the direction tag.</summary>
        static member ofRow(row: SqlRow) =
            let table = "process_io"

            ProcessIoRow(
                RowCodecHelpers.text table "process_id" row,
                RowCodecHelpers.text table "direction" row
                |> RowCodecHelpers.processIoDirectionOfText table "direction",
                RowCodecHelpers.int table "position" row,
                ?SampleId = RowCodecHelpers.textOption table "sample_id" row,
                ?DataId = RowCodecHelpers.textOption table "data_id" row
            )

        /// <summary>Encodes the row to <see cref="SqlParameters"/> for INSERT/UPDATE on <c>process_io</c>.</summary>
        member this.ToParameters() : SqlParameters =
            [|
                RowCodecHelpers.textParam "process_id" this.ProcessId
                RowCodecHelpers.textParam "direction" (RowCodecHelpers.processIoDirectionText this.Direction)
                RowCodecHelpers.intParam "position" this.Position
                RowCodecHelpers.textOptionParam "sample_id" this.SampleId
                RowCodecHelpers.textOptionParam "data_id" this.DataId
            |]

    type ProcessParameterValueRow with

        /// <summary>Decodes a <see cref="ProcessParameterValueRow"/> from a <c>process_parameter_value</c> result row.</summary>
        static member ofRow(row: SqlRow) =
            let table = "process_parameter_value"

            ProcessParameterValueRow(
                RowCodecHelpers.text table "process_id" row,
                RowCodecHelpers.int table "position" row,
                RowCodecHelpers.text table "annotation_id" row
            )

        /// <summary>Encodes the row to <see cref="SqlParameters"/> for INSERT/UPDATE on <c>process_parameter_value</c>.</summary>
        member this.ToParameters() : SqlParameters =
            [|
                RowCodecHelpers.textParam "process_id" this.ProcessId
                RowCodecHelpers.intParam "position" this.Position
                RowCodecHelpers.textParam "annotation_id" this.AnnotationId
            |]

    type ProtocolAdditionalPropertyRow with

        /// <summary>Decodes a <see cref="ProtocolAdditionalPropertyRow"/> from a <c>protocol_additional_property</c> result row.</summary>
        static member ofRow(row: SqlRow) =
            let table = "protocol_additional_property"

            ProtocolAdditionalPropertyRow(
                RowCodecHelpers.text table "protocol_id" row,
                RowCodecHelpers.int table "position" row,
                RowCodecHelpers.text table "annotation_id" row
            )

        /// <summary>Encodes the row to <see cref="SqlParameters"/> for INSERT/UPDATE on <c>protocol_additional_property</c>.</summary>
        member this.ToParameters() : SqlParameters =
            [|
                RowCodecHelpers.textParam "protocol_id" this.ProtocolId
                RowCodecHelpers.intParam "position" this.Position
                RowCodecHelpers.textParam "annotation_id" this.AnnotationId
            |]

    type SampleAdditionalPropertyRow with

        /// <summary>Decodes a <see cref="SampleAdditionalPropertyRow"/> from a <c>sample_additional_property</c> result row.</summary>
        static member ofRow(row: SqlRow) =
            let table = "sample_additional_property"

            SampleAdditionalPropertyRow(
                RowCodecHelpers.text table "sample_id" row,
                RowCodecHelpers.int table "position" row,
                RowCodecHelpers.text table "annotation_id" row
            )

        /// <summary>Encodes the row to <see cref="SqlParameters"/> for INSERT/UPDATE on <c>sample_additional_property</c>.</summary>
        member this.ToParameters() : SqlParameters =
            [|
                RowCodecHelpers.textParam "sample_id" this.SampleId
                RowCodecHelpers.intParam "position" this.Position
                RowCodecHelpers.textParam "annotation_id" this.AnnotationId
            |]

    type DataAdditionalPropertyRow with

        /// <summary>Decodes a <see cref="DataAdditionalPropertyRow"/> from a <c>data_additional_property</c> result row.</summary>
        static member ofRow(row: SqlRow) =
            let table = "data_additional_property"

            DataAdditionalPropertyRow(
                RowCodecHelpers.text table "data_id" row,
                RowCodecHelpers.int table "position" row,
                RowCodecHelpers.text table "annotation_id" row
            )

        /// <summary>Encodes the row to <see cref="SqlParameters"/> for INSERT/UPDATE on <c>data_additional_property</c>.</summary>
        member this.ToParameters() : SqlParameters =
            [|
                RowCodecHelpers.textParam "data_id" this.DataId
                RowCodecHelpers.intParam "position" this.Position
                RowCodecHelpers.textParam "annotation_id" this.AnnotationId
            |]

    type ProcessEdgeRow with

        /// <summary>Decodes a <see cref="ProcessEdgeRow"/> from a <c>process_edges</c> view row. Read-only — there is no <c>ToParameters</c>.</summary>
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

    type AnnotationOrphanRow with

        /// <summary>Decodes a <see cref="AnnotationOrphanRow"/> from a <c>annotation_orphans</c> view row. Read-only.</summary>
        static member ofRow(row: SqlRow) =
            let table = "annotation_orphans"

            AnnotationOrphanRow(RowCodecHelpers.text table "id" row)
