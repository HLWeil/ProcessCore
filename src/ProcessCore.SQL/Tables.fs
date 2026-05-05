namespace ProcessCore.SQL

open Fable.Core

[<StringEnum>]
type ProcessIoDirection =
    | [<CompiledName("input")>] Input
    | [<CompiledName("output")>] Output

[<AttachMembers>]
type DefinedTermRow(
    Id: string,
    Type: string,
    Name: string,
    ?Tan: string,
    ?InDefinedTermSetId: string,
    ?InDefinedTermSetName: string
) =

    member val Id = Id with get, set
    member val Type = Type with get, set
    member val Name = Name with get, set
    member val Tan = Tan with get, set
    member val InDefinedTermSetId = InDefinedTermSetId with get, set
    member val InDefinedTermSetName = InDefinedTermSetName with get, set

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

[<AttachMembers>]
type LabProtocolRow(
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

    member val Id = Id with get, set
    member val Type = Type with get, set
    member val AdditionalType = AdditionalType with get, set
    member val Name = Name with get, set
    member val Description = Description with get, set
    member val Version = Version with get, set
    member val Url = Url with get, set
    member val IntendedUseId = IntendedUseId with get, set
    member val IntendedUseText = IntendedUseText with get, set

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
        LabProtocolRow(
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

[<AttachMembers>]
type FormalParameterRow(Id: string, Type: string, ?Name: string, ?NameTan: string, ?DefaultValueId: string) =

    member val Id = Id with get, set
    member val Type = Type with get, set
    member val Name = Name with get, set
    member val NameTan = NameTan with get, set
    member val DefaultValueId = DefaultValueId with get, set

    [<NamedParams>]
    static member create(Id: string, Type: string, ?Name: string, ?NameTan: string, ?DefaultValueId: string) =
        FormalParameterRow(Id, Type, ?Name = Name, ?NameTan = NameTan, ?DefaultValueId = DefaultValueId)

[<AttachMembers>]
type DatasetRow(
    Id: string,
    Type: string,
    Identifier: string,
    ?AdditionalType: string,
    ?Name: string,
    ?Description: string
) =

    member val Id = Id with get, set
    member val Type = Type with get, set
    member val Identifier = Identifier with get, set
    member val AdditionalType = AdditionalType with get, set
    member val Name = Name with get, set
    member val Description = Description with get, set

    [<NamedParams>]
    static member create(
        Id: string,
        Type: string,
        Identifier: string,
        ?AdditionalType: string,
        ?Name: string,
        ?Description: string
    ) =
        DatasetRow(Id, Type, Identifier, ?AdditionalType = AdditionalType, ?Name = Name, ?Description = Description)

[<AttachMembers>]
type MaterialRow(Id: string, Type: string, Name: string, ?AdditionalType: string) =

    member val Id = Id with get, set
    member val Type = Type with get, set
    member val Name = Name with get, set
    member val AdditionalType = AdditionalType with get, set

    [<NamedParams>]
    static member create(Id: string, Type: string, Name: string, ?AdditionalType: string) =
        MaterialRow(Id, Type, Name, ?AdditionalType = AdditionalType)

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

    member val Id = Id with get, set
    member val Type = Type with get, set
    member val Path = Path with get, set
    member val AdditionalType = AdditionalType with get, set
    member val Selector = Selector with get, set
    member val SelectorFormat = SelectorFormat with get, set
    member val EncodingFormat = EncodingFormat with get, set

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

[<AttachMembers>]
type LabProcessRow(Id: string, Type: string, Name: string, ?AdditionalType: string, ?ExecutesProtocolId: string) =

    member val Id = Id with get, set
    member val Type = Type with get, set
    member val Name = Name with get, set
    member val AdditionalType = AdditionalType with get, set
    member val ExecutesProtocolId = ExecutesProtocolId with get, set

    [<NamedParams>]
    static member create(
        Id: string,
        Type: string,
        Name: string,
        ?AdditionalType: string,
        ?ExecutesProtocolId: string
    ) =
        LabProcessRow(Id, Type, Name, ?AdditionalType = AdditionalType, ?ExecutesProtocolId = ExecutesProtocolId)

[<AttachMembers>]
type PropertyValueRow(
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

    member val Id = Id with get, set
    member val Type = Type with get, set
    member val Name = Name with get, set
    member val AdditionalType = AdditionalType with get, set
    member val Value = Value with get, set
    member val Unit = Unit with get, set
    member val NameTan = NameTan with get, set
    member val ValueTan = ValueTan with get, set
    member val UnitTan = UnitTan with get, set
    member val InstanceOfId = InstanceOfId with get, set

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
        PropertyValueRow(
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

[<AttachMembers>]
type DatasetHasPartRow(DatasetId: string, Position: int, ?PartDatasetId: string, ?PartDataId: string) =

    member val DatasetId = DatasetId with get, set
    member val Position = Position with get, set
    member val PartDatasetId = PartDatasetId with get, set
    member val PartDataId = PartDataId with get, set

    [<NamedParams>]
    static member create(DatasetId: string, Position: int, ?PartDatasetId: string, ?PartDataId: string) =
        DatasetHasPartRow(DatasetId, Position, ?PartDatasetId = PartDatasetId, ?PartDataId = PartDataId)

[<AttachMembers>]
type DatasetProcessRow(DatasetId: string, Position: int, ProcessId: string) =

    member val DatasetId = DatasetId with get, set
    member val Position = Position with get, set
    member val ProcessId = ProcessId with get, set

    [<NamedParams>]
    static member create(DatasetId: string, Position: int, ProcessId: string) =
        DatasetProcessRow(DatasetId, Position, ProcessId)

[<AttachMembers>]
type DatasetAdditionalPropertyRow(DatasetId: string, Position: int, PropertyValueId: string) =

    member val DatasetId = DatasetId with get, set
    member val Position = Position with get, set
    member val PropertyValueId = PropertyValueId with get, set

    [<NamedParams>]
    static member create(DatasetId: string, Position: int, PropertyValueId: string) =
        DatasetAdditionalPropertyRow(DatasetId, Position, PropertyValueId)

[<AttachMembers>]
type ProtocolParameterRow(ProtocolId: string, Position: int, FormalParameterId: string) =

    member val ProtocolId = ProtocolId with get, set
    member val Position = Position with get, set
    member val FormalParameterId = FormalParameterId with get, set

    [<NamedParams>]
    static member create(ProtocolId: string, Position: int, FormalParameterId: string) =
        ProtocolParameterRow(ProtocolId, Position, FormalParameterId)

[<AttachMembers>]
type ProcessIoRow(
    ProcessId: string,
    Direction: ProcessIoDirection,
    Position: int,
    ?MaterialId: string,
    ?DataId: string
) =

    member val ProcessId = ProcessId with get, set
    member val Direction = Direction with get, set
    member val Position = Position with get, set
    member val MaterialId = MaterialId with get, set
    member val DataId = DataId with get, set

    [<NamedParams>]
    static member create(
        ProcessId: string,
        Direction: ProcessIoDirection,
        Position: int,
        ?MaterialId: string,
        ?DataId: string
    ) =
        ProcessIoRow(ProcessId, Direction, Position, ?MaterialId = MaterialId, ?DataId = DataId)

[<AttachMembers>]
type ProcessParameterValueRow(ProcessId: string, Position: int, PropertyValueId: string) =

    member val ProcessId = ProcessId with get, set
    member val Position = Position with get, set
    member val PropertyValueId = PropertyValueId with get, set

    [<NamedParams>]
    static member create(ProcessId: string, Position: int, PropertyValueId: string) =
        ProcessParameterValueRow(ProcessId, Position, PropertyValueId)

[<AttachMembers>]
type ProtocolAdditionalPropertyRow(ProtocolId: string, Position: int, PropertyValueId: string) =

    member val ProtocolId = ProtocolId with get, set
    member val Position = Position with get, set
    member val PropertyValueId = PropertyValueId with get, set

    [<NamedParams>]
    static member create(ProtocolId: string, Position: int, PropertyValueId: string) =
        ProtocolAdditionalPropertyRow(ProtocolId, Position, PropertyValueId)

[<AttachMembers>]
type MaterialAdditionalPropertyRow(MaterialId: string, Position: int, PropertyValueId: string) =

    member val MaterialId = MaterialId with get, set
    member val Position = Position with get, set
    member val PropertyValueId = PropertyValueId with get, set

    [<NamedParams>]
    static member create(MaterialId: string, Position: int, PropertyValueId: string) =
        MaterialAdditionalPropertyRow(MaterialId, Position, PropertyValueId)

[<AttachMembers>]
type DataAdditionalPropertyRow(DataId: string, Position: int, PropertyValueId: string) =

    member val DataId = DataId with get, set
    member val Position = Position with get, set
    member val PropertyValueId = PropertyValueId with get, set

    [<NamedParams>]
    static member create(DataId: string, Position: int, PropertyValueId: string) =
        DataAdditionalPropertyRow(DataId, Position, PropertyValueId)

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

    member val ProcessId = ProcessId with get, set
    member val InputPosition = InputPosition with get, set
    member val OutputPosition = OutputPosition with get, set
    member val InputKind = InputKind with get, set
    member val InputId = InputId with get, set
    member val OutputKind = OutputKind with get, set
    member val OutputId = OutputId with get, set

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

[<AttachMembers>]
type PropertyValueOrphanRow(Id: string) =

    member val Id = Id with get, set

    [<NamedParams>]
    static member create(Id: string) =
        PropertyValueOrphanRow(Id)
