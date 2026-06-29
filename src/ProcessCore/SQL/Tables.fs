namespace ProcessCore.SQL

open Fable.Core

/// <summary>
/// Direction tag for a row in the <c>process_io</c> table.
/// </summary>
/// <remarks>
/// Persisted as the literal lowercase strings <c>"input"</c> and <c>"output"</c>; the
/// <c>StringEnum</c> attribute pins the JavaScript runtime representation to those literals.
/// </remarks>
[<StringEnum>]
type ProcessIoDirection =
    /// <summary>Indicates that the I/O participant is consumed by the process.</summary>
    | [<CompiledName("input")>] Input
    /// <summary>Indicates that the I/O participant is produced by the process.</summary>
    | [<CompiledName("output")>] Output

/// <summary>
/// Row of the <c>defined_term</c> table — a controlled-vocabulary term used as a typed identifier
/// throughout the data model (e.g. as ontology references for samples, units and parameter names).
/// </summary>
/// <param name="Id">Primary key — opaque identifier for the term.</param>
/// <param name="Type">Type discriminator (the term's class within the data model).</param>
/// <param name="Name">Human-readable label.</param>
/// <param name="Tan">Optional Term Accession Number (URL or CURIE) identifying the source ontology entry.</param>
/// <param name="InDefinedTermSetId">Optional id of the containing term set.</param>
/// <param name="InDefinedTermSetName">Optional human-readable name of the containing term set.</param>
[<AttachMembers>]
type DefinedTermRow(
    Id: string,
    Type: string,
    Name: string,
    ?Tan: string,
    ?InDefinedTermSetId: string,
    ?InDefinedTermSetName: string
) =

    /// <summary>Primary key.</summary>
    member val Id = Id with get, set
    /// <summary>Type discriminator.</summary>
    member val Type = Type with get, set
    /// <summary>Human-readable label.</summary>
    member val Name = Name with get, set
    /// <summary>Optional Term Accession Number.</summary>
    member val Tan = Tan with get, set
    /// <summary>Optional id of the containing term set.</summary>
    member val InDefinedTermSetId = InDefinedTermSetId with get, set
    /// <summary>Optional name of the containing term set.</summary>
    member val InDefinedTermSetName = InDefinedTermSetName with get, set

    /// <summary>Named-argument constructor exposed to JavaScript and Python callers.</summary>
    [<NamedParams>]
    static member create(
        Id: string,
        Type: string,
        Name: string,
        ?Tan: string,
        ?InDefinedTermSetId: string,
        ?InDefinedTermSetName: string
    ) =
        DefinedTermRow(
            Id,
            Type,
            Name,
            ?Tan = Tan,
            ?InDefinedTermSetId = InDefinedTermSetId,
            ?InDefinedTermSetName = InDefinedTermSetName
        )

/// <summary>
/// Row of the <c>recipe</c> table — a reusable protocol definition that can be executed by
/// one or more <see cref="ProcessRow"/> instances and parameterised through
/// <see cref="ProtocolParameterRow"/> entries.
/// </summary>
/// <param name="Id">Primary key.</param>
/// <param name="Type">Type discriminator (e.g. the protocol's schema.org class).</param>
/// <param name="AdditionalType">Optional refinement of <paramref name="Type"/>.</param>
/// <param name="Name">Optional protocol name.</param>
/// <param name="Description">Optional free-form description.</param>
/// <param name="Version">Optional version identifier.</param>
/// <param name="Url">Optional canonical URL pointing to the protocol document.</param>
/// <param name="IntendedUseId">Optional id of a defined term describing the intended use.</param>
/// <param name="IntendedUseText">Optional free-form intended-use description.</param>
[<AttachMembers>]
type RecipeRow(
    Id: string,
    Type: string,
    ?AdditionalType: string,
    ?Name: string,
    ?Description: string,
    ?Version: string,
    ?Url: string,
    ?IntendedUseId: string,
    ?IntendedUseText: string
) =

    /// <summary>Primary key.</summary>
    member val Id = Id with get, set
    /// <summary>Type discriminator.</summary>
    member val Type = Type with get, set
    /// <summary>Optional refinement of <see cref="Type"/>.</summary>
    member val AdditionalType = AdditionalType with get, set
    /// <summary>Optional protocol name.</summary>
    member val Name = Name with get, set
    /// <summary>Optional free-form description.</summary>
    member val Description = Description with get, set
    /// <summary>Optional version identifier.</summary>
    member val Version = Version with get, set
    /// <summary>Optional canonical URL.</summary>
    member val Url = Url with get, set
    /// <summary>Optional id of a defined term describing the intended use.</summary>
    member val IntendedUseId = IntendedUseId with get, set
    /// <summary>Optional free-form intended-use description.</summary>
    member val IntendedUseText = IntendedUseText with get, set

    /// <summary>Named-argument constructor exposed to JavaScript and Python callers.</summary>
    [<NamedParams>]
    static member create(
        Id: string,
        Type: string,
        ?AdditionalType: string,
        ?Name: string,
        ?Description: string,
        ?Version: string,
        ?Url: string,
        ?IntendedUseId: string,
        ?IntendedUseText: string
    ) =
        RecipeRow(
            Id,
            Type,
            ?AdditionalType = AdditionalType,
            ?Name = Name,
            ?Description = Description,
            ?Version = Version,
            ?Url = Url,
            ?IntendedUseId = IntendedUseId,
            ?IntendedUseText = IntendedUseText
        )

/// <summary>
/// Row of the <c>formal_parameter</c> table — the schema-side declaration of a parameter that a
/// <see cref="RecipeRow"/> can take, optionally pinned to a default <see cref="AnnotationRow"/>.
/// </summary>
/// <param name="Id">Primary key.</param>
/// <param name="Type">Type discriminator.</param>
/// <param name="Name">Optional parameter name.</param>
/// <param name="NameTan">Optional Term Accession Number for the parameter name.</param>
/// <param name="DefaultValueId">Optional id of a <see cref="AnnotationRow"/> used as the default.</param>
[<AttachMembers>]
type FormalParameterRow(Id: string, Type: string, ?Name: string, ?NameTan: string, ?DefaultValueId: string) =

    /// <summary>Primary key.</summary>
    member val Id = Id with get, set
    /// <summary>Type discriminator.</summary>
    member val Type = Type with get, set
    /// <summary>Optional parameter name.</summary>
    member val Name = Name with get, set
    /// <summary>Optional Term Accession Number for the parameter name.</summary>
    member val NameTan = NameTan with get, set
    /// <summary>Optional id of the default property value.</summary>
    member val DefaultValueId = DefaultValueId with get, set

    /// <summary>Named-argument constructor exposed to JavaScript and Python callers.</summary>
    [<NamedParams>]
    static member create(Id: string, Type: string, ?Name: string, ?NameTan: string, ?DefaultValueId: string) =
        FormalParameterRow(Id, Type, ?Name = Name, ?NameTan = NameTan, ?DefaultValueId = DefaultValueId)

/// <summary>
/// Row of the <c>dataset</c> table — a top-level container that aggregates parts (other datasets or
/// data items), processes and additional properties.
/// </summary>
/// <param name="Id">Primary key.</param>
/// <param name="Type">Type discriminator.</param>
/// <param name="Identifier">External identifier (e.g. ARC identifier or DOI).</param>
/// <param name="AdditionalType">Optional refinement of <paramref name="Type"/>.</param>
/// <param name="Title">Optional dataset title.</param>
/// <param name="Description">Optional free-form description.</param>
[<AttachMembers>]
type DatasetRow(
    Id: string,
    Type: string,
    Identifier: string,
    ?AdditionalType: string,
    ?Title: string,
    ?Description: string
) =

    /// <summary>Primary key.</summary>
    member val Id = Id with get, set
    /// <summary>Type discriminator.</summary>
    member val Type = Type with get, set
    /// <summary>External identifier.</summary>
    member val Identifier = Identifier with get, set
    /// <summary>Optional refinement of <see cref="Type"/>.</summary>
    member val AdditionalType = AdditionalType with get, set
    /// <summary>Optional dataset title.</summary>
    member val Title = Title with get, set
    /// <summary>Optional free-form description.</summary>
    member val Description = Description with get, set

    /// <summary>Named-argument constructor exposed to JavaScript and Python callers.</summary>
    [<NamedParams>]
    static member create(
        Id: string,
        Type: string,
        Identifier: string,
        ?AdditionalType: string,
        ?Title: string,
        ?Description: string
    ) =
        DatasetRow(Id, Type, Identifier, ?AdditionalType = AdditionalType, ?Title = Title, ?Description = Description)

/// <summary>
/// Row of the <c>sample</c> table — a tangible substance or sample that can act as input or
/// output of a <see cref="ProcessRow"/>.
/// </summary>
/// <param name="Id">Primary key.</param>
/// <param name="Type">Type discriminator.</param>
/// <param name="Name">Sample name.</param>
/// <param name="AdditionalType">Optional refinement of <paramref name="Type"/>.</param>
[<AttachMembers>]
type SampleRow(Id: string, Type: string, Name: string, ?AdditionalType: string) =

    /// <summary>Primary key.</summary>
    member val Id = Id with get, set
    /// <summary>Type discriminator.</summary>
    member val Type = Type with get, set
    /// <summary>Sample name.</summary>
    member val Name = Name with get, set
    /// <summary>Optional refinement of <see cref="Type"/>.</summary>
    member val AdditionalType = AdditionalType with get, set

    /// <summary>Named-argument constructor exposed to JavaScript and Python callers.</summary>
    [<NamedParams>]
    static member create(Id: string, Type: string, Name: string, ?AdditionalType: string) =
        SampleRow(Id, Type, Name, ?AdditionalType = AdditionalType)

/// <summary>
/// Row of the <c>data</c> table — a digital data item identified by a path and optional selector,
/// participating as input or output of a <see cref="ProcessRow"/>.
/// </summary>
/// <param name="Id">Primary key.</param>
/// <param name="Type">Type discriminator.</param>
/// <param name="Path">Path/URI of the data item, relative to the containing ARC.</param>
/// <param name="AdditionalType">Optional refinement of <paramref name="Type"/>.</param>
/// <param name="Selector">Optional selector identifying a sub-region of the data (e.g. a sheet name or JSON pointer).</param>
/// <param name="SelectorFormat">Optional format identifier for <paramref name="Selector"/>.</param>
/// <param name="EncodingFormat">Optional MIME type / encoding format of the data item.</param>
[<AttachMembers>]
type DataRow(
    Id: string,
    Type: string,
    Path: string,
    ?AdditionalType: string,
    ?Selector: string,
    ?SelectorFormat: string,
    ?EncodingFormat: string
) =

    /// <summary>Primary key.</summary>
    member val Id = Id with get, set
    /// <summary>Type discriminator.</summary>
    member val Type = Type with get, set
    /// <summary>Path/URI of the data item.</summary>
    member val Path = Path with get, set
    /// <summary>Optional refinement of <see cref="Type"/>.</summary>
    member val AdditionalType = AdditionalType with get, set
    /// <summary>Optional selector for a sub-region of the data.</summary>
    member val Selector = Selector with get, set
    /// <summary>Optional format identifier for <see cref="Selector"/>.</summary>
    member val SelectorFormat = SelectorFormat with get, set
    /// <summary>Optional MIME type / encoding format.</summary>
    member val EncodingFormat = EncodingFormat with get, set

    /// <summary>Named-argument constructor exposed to JavaScript and Python callers.</summary>
    [<NamedParams>]
    static member create(
        Id: string,
        Type: string,
        Path: string,
        ?AdditionalType: string,
        ?Selector: string,
        ?SelectorFormat: string,
        ?EncodingFormat: string
    ) =
        DataRow(
            Id,
            Type,
            Path,
            ?AdditionalType = AdditionalType,
            ?Selector = Selector,
            ?SelectorFormat = SelectorFormat,
            ?EncodingFormat = EncodingFormat
        )

/// <summary>
/// Row of the <c>process</c> table — a single execution of a <see cref="RecipeRow"/>,
/// connected to its inputs and outputs through <see cref="ProcessIoRow"/> and to its parameter values
/// through <see cref="ProcessParameterValueRow"/>.
/// </summary>
/// <param name="Id">Primary key.</param>
/// <param name="Type">Type discriminator.</param>
/// <param name="Name">Process name.</param>
/// <param name="AdditionalType">Optional refinement of <paramref name="Type"/>.</param>
/// <param name="ExecutesProtocolId">Optional id of the executed <see cref="RecipeRow"/>.</param>
[<AttachMembers>]
type ProcessRow(Id: string, Type: string, Name: string, ?AdditionalType: string, ?ExecutesProtocolId: string) =

    /// <summary>Primary key.</summary>
    member val Id = Id with get, set
    /// <summary>Type discriminator.</summary>
    member val Type = Type with get, set
    /// <summary>Process name.</summary>
    member val Name = Name with get, set
    /// <summary>Optional refinement of <see cref="Type"/>.</summary>
    member val AdditionalType = AdditionalType with get, set
    /// <summary>Optional id of the executed protocol.</summary>
    member val ExecutesProtocolId = ExecutesProtocolId with get, set

    /// <summary>Named-argument constructor exposed to JavaScript and Python callers.</summary>
    [<NamedParams>]
    static member create(
        Id: string,
        Type: string,
        Name: string,
        ?AdditionalType: string,
        ?ExecutesProtocolId: string
    ) =
        ProcessRow(Id, Type, Name, ?AdditionalType = AdditionalType, ?ExecutesProtocolId = ExecutesProtocolId)

/// <summary>
/// Row of the <c>annotation</c> table — a typed name/value pair, optionally with a unit and
/// ontology references. Used as parameter value, additional property and default-value carrier.
/// </summary>
/// <param name="Id">Primary key.</param>
/// <param name="Type">Type discriminator.</param>
/// <param name="Name">Property name.</param>
/// <param name="AdditionalType">Optional refinement of <paramref name="Type"/>.</param>
/// <param name="Value">Optional value (free-form text).</param>
/// <param name="Unit">Optional unit (free-form text).</param>
/// <param name="NameTan">Optional Term Accession Number for the property name.</param>
/// <param name="ValueTan">Optional Term Accession Number for the value.</param>
/// <param name="UnitTan">Optional Term Accession Number for the unit.</param>
/// <param name="InstanceOfId">Optional id of the <see cref="FormalParameterRow"/> this value instantiates.</param>
[<AttachMembers>]
type AnnotationRow(
    Id: string,
    Type: string,
    Name: string,
    ?AdditionalType: string,
    ?Value: string,
    ?Unit: string,
    ?NameTan: string,
    ?ValueTan: string,
    ?UnitTan: string,
    ?InstanceOfId: string
) =

    /// <summary>Primary key.</summary>
    member val Id = Id with get, set
    /// <summary>Type discriminator.</summary>
    member val Type = Type with get, set
    /// <summary>Property name.</summary>
    member val Name = Name with get, set
    /// <summary>Optional refinement of <see cref="Type"/>.</summary>
    member val AdditionalType = AdditionalType with get, set
    /// <summary>Optional value.</summary>
    member val Value = Value with get, set
    /// <summary>Optional unit.</summary>
    member val Unit = Unit with get, set
    /// <summary>Optional TAN for the name.</summary>
    member val NameTan = NameTan with get, set
    /// <summary>Optional TAN for the value.</summary>
    member val ValueTan = ValueTan with get, set
    /// <summary>Optional TAN for the unit.</summary>
    member val UnitTan = UnitTan with get, set
    /// <summary>Optional id of the formal parameter this value instantiates.</summary>
    member val InstanceOfId = InstanceOfId with get, set

    /// <summary>Named-argument constructor exposed to JavaScript and Python callers.</summary>
    [<NamedParams>]
    static member create(
        Id: string,
        Type: string,
        Name: string,
        ?AdditionalType: string,
        ?Value: string,
        ?Unit: string,
        ?NameTan: string,
        ?ValueTan: string,
        ?UnitTan: string,
        ?InstanceOfId: string
    ) =
        AnnotationRow(
            Id,
            Type,
            Name,
            ?AdditionalType = AdditionalType,
            ?Value = Value,
            ?Unit = Unit,
            ?NameTan = NameTan,
            ?ValueTan = ValueTan,
            ?UnitTan = UnitTan,
            ?InstanceOfId = InstanceOfId
        )

/// <summary>
/// Row of the <c>dataset_has_part</c> association table — links a parent dataset to one of its
/// parts, which is either a child dataset or a data item, at a given ordered position.
/// </summary>
/// <param name="DatasetId">Parent dataset id.</param>
/// <param name="Position">Zero-based ordering position within the parent.</param>
/// <param name="PartDatasetId">Optional id of a child dataset. Mutually exclusive with <paramref name="PartDataId"/>.</param>
/// <param name="PartDataId">Optional id of a data item. Mutually exclusive with <paramref name="PartDatasetId"/>.</param>
[<AttachMembers>]
type DatasetHasPartRow(DatasetId: string, Position: int, ?PartDatasetId: string, ?PartDataId: string) =

    /// <summary>Parent dataset id.</summary>
    member val DatasetId = DatasetId with get, set
    /// <summary>Position within the parent.</summary>
    member val Position = Position with get, set
    /// <summary>Optional child dataset id.</summary>
    member val PartDatasetId = PartDatasetId with get, set
    /// <summary>Optional data item id.</summary>
    member val PartDataId = PartDataId with get, set

    /// <summary>Named-argument constructor exposed to JavaScript and Python callers.</summary>
    [<NamedParams>]
    static member create(DatasetId: string, Position: int, ?PartDatasetId: string, ?PartDataId: string) =
        DatasetHasPartRow(DatasetId, Position, ?PartDatasetId = PartDatasetId, ?PartDataId = PartDataId)

/// <summary>
/// Row of the <c>dataset_process</c> association table — links a dataset to one of its
/// <see cref="ProcessRow"/> entries at a given ordered position.
/// </summary>
/// <param name="DatasetId">Owning dataset id.</param>
/// <param name="Position">Zero-based ordering position within the dataset.</param>
/// <param name="ProcessId">Referenced lab-process id.</param>
[<AttachMembers>]
type DatasetProcessRow(DatasetId: string, Position: int, ProcessId: string) =

    /// <summary>Owning dataset id.</summary>
    member val DatasetId = DatasetId with get, set
    /// <summary>Position within the dataset.</summary>
    member val Position = Position with get, set
    /// <summary>Referenced lab-process id.</summary>
    member val ProcessId = ProcessId with get, set

    /// <summary>Named-argument constructor exposed to JavaScript and Python callers.</summary>
    [<NamedParams>]
    static member create(DatasetId: string, Position: int, ProcessId: string) =
        DatasetProcessRow(DatasetId, Position, ProcessId)

/// <summary>
/// Row of the <c>dataset_additional_property</c> association table — attaches an ordered
/// <see cref="AnnotationRow"/> to a dataset.
/// </summary>
/// <param name="DatasetId">Owning dataset id.</param>
/// <param name="Position">Zero-based ordering position within the dataset.</param>
/// <param name="AnnotationId">Referenced property-value id.</param>
[<AttachMembers>]
type DatasetAdditionalPropertyRow(DatasetId: string, Position: int, AnnotationId: string) =

    /// <summary>Owning dataset id.</summary>
    member val DatasetId = DatasetId with get, set
    /// <summary>Position within the dataset.</summary>
    member val Position = Position with get, set
    /// <summary>Referenced property-value id.</summary>
    member val AnnotationId = AnnotationId with get, set

    /// <summary>Named-argument constructor exposed to JavaScript and Python callers.</summary>
    [<NamedParams>]
    static member create(DatasetId: string, Position: int, AnnotationId: string) =
        DatasetAdditionalPropertyRow(DatasetId, Position, AnnotationId)

/// <summary>
/// Row of the <c>protocol_parameter</c> association table — attaches an ordered
/// <see cref="FormalParameterRow"/> to a <see cref="RecipeRow"/>.
/// </summary>
/// <param name="ProtocolId">Owning protocol id.</param>
/// <param name="Position">Zero-based ordering position within the protocol.</param>
/// <param name="FormalParameterId">Referenced formal-parameter id.</param>
[<AttachMembers>]
type ProtocolParameterRow(ProtocolId: string, Position: int, FormalParameterId: string) =

    /// <summary>Owning protocol id.</summary>
    member val ProtocolId = ProtocolId with get, set
    /// <summary>Position within the protocol.</summary>
    member val Position = Position with get, set
    /// <summary>Referenced formal-parameter id.</summary>
    member val FormalParameterId = FormalParameterId with get, set

    /// <summary>Named-argument constructor exposed to JavaScript and Python callers.</summary>
    [<NamedParams>]
    static member create(ProtocolId: string, Position: int, FormalParameterId: string) =
        ProtocolParameterRow(ProtocolId, Position, FormalParameterId)

/// <summary>
/// Row of the <c>process_io</c> association table — attaches a <see cref="SampleRow"/> or
/// <see cref="DataRow"/> to a <see cref="ProcessRow"/> on either the input or output side at
/// an ordered position.
/// </summary>
/// <param name="ProcessId">Owning lab-process id.</param>
/// <param name="Direction">Whether the entry is consumed (<see cref="ProcessIoDirection.Input"/>) or produced (<see cref="ProcessIoDirection.Output"/>).</param>
/// <param name="Position">Zero-based ordering position within the direction-specific side of the process.</param>
/// <param name="SampleId">Optional referenced sample id. Mutually exclusive with <paramref name="DataId"/>.</param>
/// <param name="DataId">Optional referenced data id. Mutually exclusive with <paramref name="SampleId"/>.</param>
[<AttachMembers>]
type ProcessIoRow(
    ProcessId: string,
    Direction: ProcessIoDirection,
    Position: int,
    ?SampleId: string,
    ?DataId: string
) =

    /// <summary>Owning lab-process id.</summary>
    member val ProcessId = ProcessId with get, set
    /// <summary>Input or output side.</summary>
    member val Direction = Direction with get, set
    /// <summary>Position within the direction-specific side.</summary>
    member val Position = Position with get, set
    /// <summary>Optional referenced sample id.</summary>
    member val SampleId = SampleId with get, set
    /// <summary>Optional referenced data id.</summary>
    member val DataId = DataId with get, set

    /// <summary>Named-argument constructor exposed to JavaScript and Python callers.</summary>
    [<NamedParams>]
    static member create(
        ProcessId: string,
        Direction: ProcessIoDirection,
        Position: int,
        ?SampleId: string,
        ?DataId: string
    ) =
        ProcessIoRow(ProcessId, Direction, Position, ?SampleId = SampleId, ?DataId = DataId)

/// <summary>
/// Row of the <c>process_parameter_value</c> association table — attaches an ordered
/// <see cref="AnnotationRow"/> to a <see cref="ProcessRow"/> as a parameter value.
/// </summary>
/// <param name="ProcessId">Owning lab-process id.</param>
/// <param name="Position">Zero-based ordering position within the process.</param>
/// <param name="AnnotationId">Referenced property-value id.</param>
[<AttachMembers>]
type ProcessParameterValueRow(ProcessId: string, Position: int, AnnotationId: string) =

    /// <summary>Owning lab-process id.</summary>
    member val ProcessId = ProcessId with get, set
    /// <summary>Position within the process.</summary>
    member val Position = Position with get, set
    /// <summary>Referenced property-value id.</summary>
    member val AnnotationId = AnnotationId with get, set

    /// <summary>Named-argument constructor exposed to JavaScript and Python callers.</summary>
    [<NamedParams>]
    static member create(ProcessId: string, Position: int, AnnotationId: string) =
        ProcessParameterValueRow(ProcessId, Position, AnnotationId)

/// <summary>
/// Row of the <c>protocol_additional_property</c> association table — attaches an ordered
/// <see cref="AnnotationRow"/> to a <see cref="RecipeRow"/>.
/// </summary>
/// <param name="ProtocolId">Owning protocol id.</param>
/// <param name="Position">Zero-based ordering position within the protocol.</param>
/// <param name="AnnotationId">Referenced property-value id.</param>
[<AttachMembers>]
type ProtocolAdditionalPropertyRow(ProtocolId: string, Position: int, AnnotationId: string) =

    /// <summary>Owning protocol id.</summary>
    member val ProtocolId = ProtocolId with get, set
    /// <summary>Position within the protocol.</summary>
    member val Position = Position with get, set
    /// <summary>Referenced property-value id.</summary>
    member val AnnotationId = AnnotationId with get, set

    /// <summary>Named-argument constructor exposed to JavaScript and Python callers.</summary>
    [<NamedParams>]
    static member create(ProtocolId: string, Position: int, AnnotationId: string) =
        ProtocolAdditionalPropertyRow(ProtocolId, Position, AnnotationId)

/// <summary>
/// Row of the <c>sample_additional_property</c> association table — attaches an ordered
/// <see cref="AnnotationRow"/> to a <see cref="SampleRow"/>.
/// </summary>
/// <param name="SampleId">Owning sample id.</param>
/// <param name="Position">Zero-based ordering position within the sample.</param>
/// <param name="AnnotationId">Referenced property-value id.</param>
[<AttachMembers>]
type SampleAdditionalPropertyRow(SampleId: string, Position: int, AnnotationId: string) =

    /// <summary>Owning sample id.</summary>
    member val SampleId = SampleId with get, set
    /// <summary>Position within the sample.</summary>
    member val Position = Position with get, set
    /// <summary>Referenced property-value id.</summary>
    member val AnnotationId = AnnotationId with get, set

    /// <summary>Named-argument constructor exposed to JavaScript and Python callers.</summary>
    [<NamedParams>]
    static member create(SampleId: string, Position: int, AnnotationId: string) =
        SampleAdditionalPropertyRow(SampleId, Position, AnnotationId)

/// <summary>
/// Row of the <c>data_additional_property</c> association table — attaches an ordered
/// <see cref="AnnotationRow"/> to a <see cref="DataRow"/>.
/// </summary>
/// <param name="DataId">Owning data id.</param>
/// <param name="Position">Zero-based ordering position within the data item.</param>
/// <param name="AnnotationId">Referenced property-value id.</param>
[<AttachMembers>]
type DataAdditionalPropertyRow(DataId: string, Position: int, AnnotationId: string) =

    /// <summary>Owning data id.</summary>
    member val DataId = DataId with get, set
    /// <summary>Position within the data item.</summary>
    member val Position = Position with get, set
    /// <summary>Referenced property-value id.</summary>
    member val AnnotationId = AnnotationId with get, set

    /// <summary>Named-argument constructor exposed to JavaScript and Python callers.</summary>
    [<NamedParams>]
    static member create(DataId: string, Position: int, AnnotationId: string) =
        DataAdditionalPropertyRow(DataId, Position, AnnotationId)

/// <summary>
/// Row of the <c>process_edges</c> view — a denormalised pairing of one input and one output of the
/// same lab process, used to traverse the data-flow graph without joining the I/O table to itself.
/// </summary>
/// <remarks>
/// This row type is read-only; the view has no insert/update/delete repository. The kind columns
/// indicate which underlying entity table the corresponding id refers to (e.g. <c>"sample"</c>
/// or <c>"data"</c>).
/// </remarks>
/// <param name="ProcessId">Lab-process id shared by the input and output side.</param>
/// <param name="InputPosition">Position of the input on its side of the process.</param>
/// <param name="OutputPosition">Position of the output on its side of the process.</param>
/// <param name="InputKind">Kind tag of the input entity (e.g. <c>"sample"</c> or <c>"data"</c>).</param>
/// <param name="InputId">Id of the input entity.</param>
/// <param name="OutputKind">Kind tag of the output entity.</param>
/// <param name="OutputId">Id of the output entity.</param>
[<AttachMembers>]
type ProcessEdgeRow(
    ProcessId: string,
    InputPosition: int,
    OutputPosition: int,
    InputKind: string,
    InputId: string,
    OutputKind: string,
    OutputId: string
) =

    /// <summary>Shared lab-process id.</summary>
    member val ProcessId = ProcessId with get, set
    /// <summary>Position of the input on its side.</summary>
    member val InputPosition = InputPosition with get, set
    /// <summary>Position of the output on its side.</summary>
    member val OutputPosition = OutputPosition with get, set
    /// <summary>Kind tag of the input entity.</summary>
    member val InputKind = InputKind with get, set
    /// <summary>Id of the input entity.</summary>
    member val InputId = InputId with get, set
    /// <summary>Kind tag of the output entity.</summary>
    member val OutputKind = OutputKind with get, set
    /// <summary>Id of the output entity.</summary>
    member val OutputId = OutputId with get, set

    /// <summary>Named-argument constructor exposed to JavaScript and Python callers.</summary>
    [<NamedParams>]
    static member create(
        ProcessId: string,
        InputPosition: int,
        OutputPosition: int,
        InputKind: string,
        InputId: string,
        OutputKind: string,
        OutputId: string
    ) =
        ProcessEdgeRow(ProcessId, InputPosition, OutputPosition, InputKind, InputId, OutputKind, OutputId)

/// <summary>
/// Row of the <c>annotation_orphans</c> view — the id of a <see cref="AnnotationRow"/>
/// that is not referenced by any owning entity. Useful for housekeeping and integrity checks.
/// </summary>
/// <param name="Id">Id of an unreferenced property value.</param>
[<AttachMembers>]
type AnnotationOrphanRow(Id: string) =

    /// <summary>Id of an unreferenced property value.</summary>
    member val Id = Id with get, set

    /// <summary>Named-argument constructor exposed to JavaScript and Python callers.</summary>
    [<NamedParams>]
    static member create(Id: string) =
        AnnotationOrphanRow(Id)
