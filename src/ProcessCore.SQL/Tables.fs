namespace ProcessCore.SQL

[<RequireQualifiedAccess>]
type ProcessIoDirection =
    | Input
    | Output

    member this.Sql =
        match this with
        | ProcessIoDirection.Input -> "input"
        | ProcessIoDirection.Output -> "output"

[<RequireQualifiedAccess>]
module ProcessIoDirection =

    let ofSql value =
        match value with
        | "input" -> ProcessIoDirection.Input
        | "output" -> ProcessIoDirection.Output
        | other -> invalidArg "direction" $"Unknown process_io.direction '{other}'."

type DefinedTermRow =
    {
        Id: string
        Type: string
        Name: string
        Tan: string option
        InDefinedTermSetId: string option
        InDefinedTermSetName: string option
    }

type LabProtocolRow =
    {
        Id: string
        Type: string
        AdditionalType: string option
        Name: string option
        Description: string option
        Version: string option
        Url: string option
        IntendedUseId: string option
        IntendedUseText: string option
    }

type FormalParameterRow =
    {
        Id: string
        Type: string
        Name: string option
        NameTan: string option
        DefaultValueId: string option
    }

type DatasetRow =
    {
        Id: string
        Type: string
        AdditionalType: string option
        Identifier: string
        Name: string option
        Description: string option
    }

type MaterialRow =
    {
        Id: string
        Type: string
        AdditionalType: string option
        Name: string
    }

type DataRow =
    {
        Id: string
        Type: string
        AdditionalType: string option
        Path: string
        Selector: string option
        SelectorFormat: string option
        EncodingFormat: string option
    }

type LabProcessRow =
    {
        Id: string
        Type: string
        AdditionalType: string option
        Name: string
        ExecutesProtocolId: string option
    }

type PropertyValueRow =
    {
        Id: string
        Type: string
        AdditionalType: string option
        Name: string
        Value: string option
        Unit: string option
        NameTan: string option
        ValueTan: string option
        UnitTan: string option
        InstanceOfId: string option
    }

type DatasetHasPartRow =
    {
        DatasetId: string
        Position: int
        PartDatasetId: string option
        PartDataId: string option
    }

type DatasetProcessRow =
    {
        DatasetId: string
        Position: int
        ProcessId: string
    }

type DatasetAdditionalPropertyRow =
    {
        DatasetId: string
        Position: int
        PropertyValueId: string
    }

type ProtocolParameterRow =
    {
        ProtocolId: string
        Position: int
        FormalParameterId: string
    }

type ProcessIoRow =
    {
        ProcessId: string
        Direction: ProcessIoDirection
        Position: int
        MaterialId: string option
        DataId: string option
    }

type ProcessParameterValueRow =
    {
        ProcessId: string
        Position: int
        PropertyValueId: string
    }

type ProtocolAdditionalPropertyRow =
    {
        ProtocolId: string
        Position: int
        PropertyValueId: string
    }

type MaterialAdditionalPropertyRow =
    {
        MaterialId: string
        Position: int
        PropertyValueId: string
    }

type DataAdditionalPropertyRow =
    {
        DataId: string
        Position: int
        PropertyValueId: string
    }

type ProcessEdgeRow =
    {
        ProcessId: string
        InputPosition: int
        OutputPosition: int
        InputKind: string
        InputId: string
        OutputKind: string
        OutputId: string
    }

type PropertyValueOrphanRow =
    {
        Id: string
    }
