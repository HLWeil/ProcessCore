namespace ProcessCore.Table

open Fable.Core
open ProcessCore

// ─────────────────────────────────────────────────────────────────────────────
// IOType
// ─────────────────────────────────────────────────────────────────────────────

/// Discriminates the kind of entity referenced by an Input or Output column.
[<AttachMembers>]
[<RequireQualifiedAccess>]
type IOType =
    | Source
    | Sample
    | Data
    | FreeText of string

    /// Used to match only(!) IOType string to IOType (without Input/Output). This matching is case sensitive.
    ///
    /// Exmp. 1: "Source" --> Source
    ///
    /// Exmp. 2: "Raw Data File" | "RawDataFile" -> RawDataFile
    static member ofString (str: string) =
        match str with
        | "Source" | "Source Name"                  -> Source
        | "Sample" | "Sample Name"                  -> Sample
        | "RawDataFile" | "Raw Data File"
        | "DerivedDataFile" | "Derived Data File"
        | "ImageFile" | "Image File"
        | "Data"                                    -> Data
        | _                                         -> FreeText str // use str to not store `str.ToLower()`

// ─────────────────────────────────────────────────────────────────────────────
// CompositeHeader
// ─────────────────────────────────────────────────────────────────────────────

/// Typed column header. Carries the column role and, for annotation columns,
/// the ontology term identifying what is being described.
[<AttachMembers>]
[<RequireQualifiedAccess>]
type CompositeHeader =
    // Annotation columns — each carries a (name, TAN) pair
    | Parameter         of DefinedTerm
    | Characteristic    of DefinedTerm
    | Factor            of DefinedTerm
    | Component         of DefinedTerm
    // Protocol metadata columns
    | ProtocolREF
    | ProtocolType
    | ProtocolDescription
    | ProtocolUri
    | ProtocolVersion
    // Agent / time
    | Performer
    | Date
    // I/O columns
    | Input             of IOType
    | Output            of IOType
    // Fallback
    | FreeText          of string
    | Comment           of string
    member this.IsCvParamColumn =
        match this with
        | Parameter _ | Factor _| Characteristic _| Component _ -> true
        | anythingElse -> false

    /// <summary>
    /// Is true if this Building Block type is a TermColumn.
    ///
    /// The name "TermColumn" refers to all columns with the syntax "Parameter/Factor/etc [TERM-NAME]" and featured columns
    /// such as Protocol Type as these are also represented as a triplet of MainColumn-TSR-TAN.
    /// </summary>
    member this.IsTermColumn =
        match this with
        | Parameter _ | Factor _| Characteristic _| Component _
        | ProtocolType -> true
        | anythingElse -> false

    member this.IsDataColumn =
        match this with
        | Input IOType.Data | Output IOType.Data -> true
        | anythingElse -> false

    /// <summary>
    /// Is true if the Building Block type is a FeaturedColumn.
    ///
    /// A FeaturedColumn can be abstracted by Parameter/Factor/Characteristic and describes one common usecase of either.
    /// Such a block will contain TSR and TAN and can be used for directed Term search.
    /// </summary>
    member this.IsFeaturedColumn =
        match this with | ProtocolType -> true | anythingElse -> false

    /// <summary>
    /// This function gets the associated term accession for featured columns.
    ///
    /// It contains the hardcoded term accessions.
    /// </summary>
    member this.GetFeaturedColumnAccession =
        match this with
        | ProtocolType -> "DPBO:1000161"
        | anyelse -> failwith $"Tried matching {anyelse} in getFeaturedColumnAccession, but is not a featured column."

    /// <summary>
    /// This function gets the associated term accession for term columns.
    /// </summary>
    member this.GetColumnAccessionShort =
        match this with
        | ProtocolType -> this.GetFeaturedColumnAccession
        | Parameter dt -> dt.TermAccessionShort()
        | Factor dt -> dt.TermAccessionShort()
        | Characteristic dt -> dt.TermAccessionShort()
        | Component dt -> dt.TermAccessionShort()
        | anyelse -> failwith $"Tried matching {anyelse}, but is not a column with an accession."

    /// <summary>
    /// Is true if the Building Block type is parsed to a single column.
    ///
    /// This can be any input, output column, as well as for example: `ProtocolREF` and `Performer` with FreeText body cells.
    /// </summary>
    member this.IsSingleColumn =
        match this with
        | FreeText _
        | Input _ | Output _
        | Comment _
        | ProtocolREF | ProtocolDescription | ProtocolUri | ProtocolVersion | Performer | Date -> true
        | anythingElse -> false

    ///
    member this.IsIOType =
        match this with
        | Input io | Output io -> true
        | anythingElse -> false

    // lower case "i" because of clashing naming:
    // Issue: https://github.com/dotnet/fsharp/issues/10359
    // Proposed design: https://github.com/fsharp/fslang-design/blob/main/RFCs/FS-1079-union-properties-visible.md
    member this.isInput =
        match this with
        | Input io -> true
        | anythingElse -> false

    member this.isOutput =
        match this with
        | Output io -> true
        | anythingElse -> false

    member this.isParameter =
        match this with
        | Parameter _ -> true
        | anythingElse -> false

    member this.isFactor =
        match this with
        | Factor _ -> true
        | anythingElse -> false

    member this.isCharacteristic =
        match this with
        | Characteristic _ -> true
        | anythingElse -> false

    member this.isComponent =
        match this with
        | Component _ -> true
        | anythingElse -> false

    member this.isProtocolType =
        match this with
        | ProtocolType -> true
        | anythingElse -> false

    member this.isProtocolREF =
        match this with
        | ProtocolREF -> true
        | anythingElse -> false

    member this.isProtocolDescription =
        match this with
        | ProtocolDescription -> true
        | anythingElse -> false

    member this.isProtocolUri =
        match this with
        | ProtocolUri -> true
        | anythingElse -> false

    member this.isProtocolVersion =
        match this with
        | ProtocolVersion -> true
        | anythingElse -> false

    member this.isProtocolColumn =
        match this with
        | ProtocolREF | ProtocolDescription | ProtocolUri | ProtocolVersion | ProtocolType -> true
        | anythingElse -> false

    member this.isPerformer =
        match this with
        | Performer -> true
        | anythingElse -> false

    member this.isDate =
        match this with
        | Date -> true
        | anythingElse -> false

    member this.isComment =
        match this with
        | Comment _ -> true
        | anythingElse -> false

    member this.isFreeText =
        match this with
        | FreeText _ -> true
        | anythingElse -> false

    member this.TryInput() =
        match this with
        | Input io -> Some io
        | _ -> None

    member this.TryOutput() =
        match this with
        | Output io -> Some io
        | _ -> None

    member this.TryIOType() =
        match this with
        | Output io | Input io -> Some io
        | _ -> None

    member this.IsUnique =
        match this with
        | ProtocolType | ProtocolREF | ProtocolDescription | ProtocolUri | ProtocolVersion | Performer | Date | Input _ | Output _ -> true
        | _ -> false

    // member this.Copy() =
    //     match this with
    //     | Parameter oa -> Parameter (oa.Copy())
    //     | Factor oa -> Factor (oa.Copy())
    //     | Characteristic oa -> Characteristic (oa.Copy())
    //     | Component oa -> Component (oa.Copy())
    //     | _ -> this
// ─────────────────────────────────────────────────────────────────────────────
// CompositeCell
// ─────────────────────────────────────────────────────────────────────────────

/// Typed cell value.
[<AttachMembers>]
[<RequireQualifiedAccess>]
type CompositeCell =
    /// Plain string (e.g. I/O name, protocol field, comment)
    | FreeText  of string
    /// Ontology term reference: display name + optional TAN
    #if FABLE_COMPILER_PYTHON
    | Term      of string * string option
    #else
    | Term      of name: string * tan: string option
    #endif
    /// Numeric value with unit term
    #if FABLE_COMPILER_PYTHON
    | Unitized  of string * string * string option
    #else
    | Unitized  of value: string * unitName: string * unitTAN: string option
    #endif
    /// Data file entity — used in Input/Output columns typed as IOType.Data;
    /// carries structured file metadata (path, selector, format, etc.)
    | Data      of ProcessCore.Data

// ─────────────────────────────────────────────────────────────────────────────
// CompositeColumn
// ─────────────────────────────────────────────────────────────────────────────

/// A column: one header paired with an ordered list of cells (one per row).
[<AttachMembers>]
type CompositeColumn(header: CompositeHeader, cells: ResizeArray<CompositeCell>) =

    member _.Header = header
    member _.Cells  = cells

    member _.ColumnCount = cells.Count

// ─────────────────────────────────────────────────────────────────────────────
// TableAux
// ─────────────────────────────────────────────────────────────────────────────

/// Auxiliary functions for composing and decomposing Table ↔ process graph.
module TableAux =

    [<Literal>]
    let ColumnIndexKey = "ColumnIndex"

    /// Annotation-column position stored as extensible Annotation metadata.
    let TryGetColumnIndex (pv: Annotation) =
        match pv.TryGetPropertyValue(ColumnIndexKey) with
        | Some index -> Some index
        | None -> pv.TryGetPropertyValue("columnIndex")
        |> Option.bind (fun ci ->
            match ci with
            | :? int as i -> Some i
            | :? string as s ->
                match System.Int32.TryParse(s) with
                | true, i -> Some i
                | _ -> None
            | _ -> None
        )

    /// Store annotation-column position as extensible Annotation metadata.
    let SetColumnIndex (pv: Annotation) (index: int option) =
        match index with
        | Some index -> pv.SetProperty(ColumnIndexKey, index)
        | None -> ()

    /// IOType for a sample node based on its AdditionalType tag.
    let SampleIOType (m: Sample) : IOType =
        match m.AdditionalType with
        | Some "Source"   -> IOType.Source
        | Some "Sample"   -> IOType.Sample
        | Some other      -> IOType.FreeText other
        | None            -> IOType.Sample

    /// Cell for a sample input/output node.
    let SampleCell (m: Sample) : CompositeCell =
        CompositeCell.FreeText m.Name

    /// Cell for a data input/output node.
    let DataCell (d: Data) : CompositeCell =
        CompositeCell.Data d

    /// Build a CompositeCell from a Annotation.
    let PVToCell (pv: Annotation) : CompositeCell =
        match pv.ValueTAN, pv.Unit with
        | _, Some u ->
            CompositeCell.Unitized(pv.Value |> Option.defaultValue "", u, pv.UnitTAN)
        | Some vtan, None ->
            CompositeCell.Term(pv.Value |> Option.defaultValue "", Some vtan)
        | None, None ->
            match pv.Value with
            | Some v -> CompositeCell.FreeText v
            | None   -> CompositeCell.FreeText ""

    /// Build a CompositeHeader from a Annotation.
    let PVToHeader (pv: Annotation) : CompositeHeader =
        let pair = DefinedTerm(pv.Name, ?tan = pv.NameTAN)
        match pv.AdditionalType with
        | Some "ParameterValue"      -> CompositeHeader.Parameter pair
        | Some "FactorValue"         -> CompositeHeader.Factor pair
        | Some "CharacteristicValue" -> CompositeHeader.Characteristic pair
        | Some "Component"           -> CompositeHeader.Component pair
        | _                          -> CompositeHeader.Parameter pair

    /// Apply a cell value back into a Annotation.
    let ApplyCellToPV (pv: Annotation, cell: CompositeCell) =
        match cell with
        | CompositeCell.FreeText v ->
            pv.Value    <- Some v
            pv.ValueTAN <- None
            pv.Unit     <- None
            pv.UnitTAN  <- None
        | CompositeCell.Term(n, tan) ->
            pv.Value    <- Some n
            pv.ValueTAN <- tan
            pv.Unit     <- None
            pv.UnitTAN  <- None
        | CompositeCell.Unitized(v, u, uTAN) ->
            pv.Value   <- Some v
            pv.Unit    <- Some u
            pv.UnitTAN <- uTAN
            pv.ValueTAN <- None
        | CompositeCell.Data _ -> ()  // Data cells don't map to Annotations

    /// Create a fresh Annotation from a header + cell + annotation type tag.
    let MakePV (header: CompositeHeader, cell: CompositeCell) : Annotation =
        let pv =
            match header with
            | CompositeHeader.Parameter(dt)      -> Annotation(dt.Name, NameTAN = dt.TAN, AdditionalType = Some "ParameterValue")
            | CompositeHeader.Characteristic(dt) -> Annotation(dt.Name, NameTAN = dt.TAN, AdditionalType = Some "CharacteristicValue")
            | CompositeHeader.Factor(dt)         -> Annotation(dt.Name, NameTAN = dt.TAN, AdditionalType = Some "FactorValue")
            | CompositeHeader.Component(dt)      -> Annotation(dt.Name, NameTAN = dt.TAN, AdditionalType = Some "Component")
            | _                                  -> Annotation("")
        ApplyCellToPV(pv, cell)
        pv

// ─────────────────────────────────────────────────────────────────────────────
// Table
// ─────────────────────────────────────────────────────────────────────────────

/// A live tabular view of a group of Process nodes that share the same name.
/// Wraps the underlying processes directly — reads decompose on the fly,
/// writes compose back into the process graph.
[<AttachMembers>]
type Table(name: string, processes: ResizeArray<Process>, dataset: Dataset) =

    member private _.ClonePV(pv: Annotation) =
        let clone = Annotation(
            pv.Name,
            ?value = pv.Value,
            ?unit = pv.Unit,
            ?nameTAN = pv.NameTAN,
            ?valueTAN = pv.ValueTAN,
            ?unitTAN = pv.UnitTAN,
            ?additionalType = pv.AdditionalType,
            ?instanceOf = pv.InstanceOf
        )
        TableAux.SetColumnIndex clone (TableAux.TryGetColumnIndex pv)
        clone

    member private this.CloneNode(node: IONode) =
        match node with
        | SampleNode m ->
            let clone = Sample(m.Name, ?additionalType = m.AdditionalType)
            for pv in m.AdditionalProperty do
                clone.AddAdditionalProperty(this.ClonePV(pv))
            SampleNode clone
        | DataNode d ->
            let clone =
                Data(
                    d.Path,
                    ?selector = d.Selector,
                    ?selectorFormat = d.SelectorFormat,
                    ?encodingFormat = d.EncodingFormat,
                    ?additionalType = d.AdditionalType
                )
            for pv in d.AdditionalProperty do
                clone.AddAdditionalProperty(this.ClonePV(pv))
            DataNode clone

    member private this.CloneProtocol(proto: Recipe) =
        let clone = Recipe()
        clone.Name <- proto.Name
        clone.Description <- proto.Description
        clone.Version <- proto.Version
        clone.Url <- proto.Url
        clone.IntendedUse <- proto.IntendedUse
        clone.AdditionalType <- proto.AdditionalType
        for fp in proto.Parameters do clone.AddParameter(fp)
        for pv in proto.Components do clone.AddComponent(this.ClonePV(pv))
        for pv in proto.AdditionalProperty do clone.AddAdditionalProperty(this.ClonePV(pv))
        clone

    member private this.EnsureProtocol(p: Process) =
        match p.ExecutesProtocol with
        | Some proto -> proto
        | None ->
            let proto = Recipe()
            p.ExecutesProtocol <- Some proto
            proto

    member private _.SetSampleType(m: Sample, ioType: IOType) =
        match ioType with
        | IOType.Source -> m.AdditionalType <- Some "Source"
        | IOType.Sample -> m.AdditionalType <- Some "Sample"
        | IOType.FreeText t -> m.AdditionalType <- Some t
        | IOType.Data -> ()

    member private this.NodeFromCell(ioType: IOType, cell: CompositeCell, fallbackName: string) =
        match cell, ioType with
        | CompositeCell.Data d, _ -> this.CloneNode(DataNode d)
        | CompositeCell.FreeText path, IOType.Data -> DataNode(Data(path))
        | CompositeCell.FreeText value, _ ->
            let m = Sample(value)
            this.SetSampleType(m, ioType)
            SampleNode m
        | _, IOType.Data -> DataNode(Data(fallbackName))
        | _, _ ->
            let m = Sample(fallbackName)
            this.SetSampleType(m, ioType)
            SampleNode m

    member private _.NodeCell(node: IONode) =
        match node with
        | SampleNode m -> TableAux.SampleCell m
        | DataNode d -> TableAux.DataCell d

    member private this.ProjectedRows() =
        let rows = ResizeArray<Process * int option * int option>()
        for p in processes do
            let rowCount = max 1 (max p.Inputs.Count p.Outputs.Count)
            for i in 0 .. rowCount - 1 do
                let inputIndex = if i < p.Inputs.Count then Some i else None
                let outputIndex = if i < p.Outputs.Count then Some i else None
                rows.Add((p, inputIndex, outputIndex))
        rows

    member private _.NodeAt(nodes: ResizeArray<IONode>, index: int option) =
        match index with
        | Some i when i >= 0 && i < nodes.Count -> Some nodes.[i]
        | _ -> None

    member private this.SelectedInput(p: Process, index: int option) =
        this.NodeAt(p.Inputs, index)

    member private this.SelectedOutput(p: Process, index: int option) =
        this.NodeAt(p.Outputs, index)

    member private this.EnsureInput(p: Process, index: int option, ioType: IOType, cell: CompositeCell) =
        match this.SelectedInput(p, index) with
        | Some node -> node
        | None ->
            let rowName =
                match index with
                | Some i -> sprintf "%s_%d" name i
                | None -> sprintf "%s_%d" name p.Inputs.Count
            let node = this.NodeFromCell(ioType, cell, rowName)
            p.AddInput(node)
            node

    member private this.EnsureOutput(p: Process, index: int option, ioType: IOType, cell: CompositeCell) =
        match this.SelectedOutput(p, index) with
        | Some node -> node
        | None ->
            let rowName =
                match index with
                | Some i -> sprintf "%s_%d_out" name i
                | None -> sprintf "%s_%d_out" name p.Outputs.Count
            let node = this.NodeFromCell(ioType, cell, rowName)
            p.AddOutput(node)
            node

    member private this.ReplaceInput(p: Process, existing: IONode option, ioType: IOType, cell: CompositeCell) =
        existing |> Option.iter p.RemoveInput
        p.AddInput(this.NodeFromCell(ioType, cell, ""))

    member private this.ReplaceOutput(p: Process, existing: IONode option, ioType: IOType, cell: CompositeCell) =
        existing |> Option.iter p.RemoveOutput
        p.AddOutput(this.NodeFromCell(ioType, cell, ""))

    member private this.ApplyCellToNode(node: IONode, ioType: IOType, cell: CompositeCell, isInput: bool, p: Process) =
        match node, cell, ioType with
        | DataNode d, CompositeCell.Data d2, _ ->
            d.Path <- d2.Path
            d.Selector <- d2.Selector
            d.SelectorFormat <- d2.SelectorFormat
            d.EncodingFormat <- d2.EncodingFormat
            d.AdditionalType <- d2.AdditionalType
        | DataNode d, CompositeCell.FreeText path, IOType.Data ->
            d.Path <- path
        | SampleNode _, CompositeCell.FreeText _, IOType.Data
        | SampleNode _, CompositeCell.Data _, _
        | DataNode _, CompositeCell.FreeText _, _ ->
            if isInput then this.ReplaceInput(p, Some node, ioType, cell)
            else this.ReplaceOutput(p, Some node, ioType, cell)
        | SampleNode m, CompositeCell.FreeText value, _ ->
            m.Name <- value
            this.SetSampleType(m, ioType)
        | _, _, _ -> ()

    member private this.CloneProcessForRow(p: Process, inputIndex: int option, outputIndex: int option) =
        let clone = Process(p.Name, ?additionalType = p.AdditionalType)
        match p.ExecutesProtocol with
        | Some proto -> clone.ExecutesProtocol <- Some(this.CloneProtocol(proto))
        | None -> ()
        match this.SelectedInput(p, inputIndex) with
        | Some node -> clone.AddInput(this.CloneNode(node))
        | None -> ()
        match this.SelectedOutput(p, outputIndex) with
        | Some node -> clone.AddOutput(this.CloneNode(node))
        | None -> ()
        for pv in p.ParameterValue do
            clone.AddParameterValue(this.ClonePV(pv))
        clone

    member private this.SampleizeProjectedRows() =
        let replacements = ResizeArray<Process * ResizeArray<Process>>()
        for p in processes |> Seq.toArray do
            let rowCount = max 1 (max p.Inputs.Count p.Outputs.Count)
            if rowCount > 1 then
                let clones = ResizeArray<Process>()
                for i in 0 .. rowCount - 1 do
                    let inputIndex = if i < p.Inputs.Count then Some i else None
                    let outputIndex = if i < p.Outputs.Count then Some i else None
                    clones.Add(this.CloneProcessForRow(p, inputIndex, outputIndex))
                replacements.Add((p, clones))
        for oldProc, clones in replacements do
            let idx = processes.IndexOf(oldProc)
            if idx >= 0 then processes.RemoveAt(idx)
            dataset.RemoveProcess(oldProc)
            let mutable insertAt = if idx >= 0 then idx else processes.Count
            for clone in clones do
                processes.Insert(insertAt, clone)
                insertAt <- insertAt + 1
                dataset.AddProcess(clone)

    member private _.CellAt(cells: ResizeArray<CompositeCell>, rowIndex: int) =
        if rowIndex < cells.Count then cells.[rowIndex] else CompositeCell.FreeText ""

    member private _.FindPV(pvs: seq<Annotation>, additionalType: string, name: string) =
        pvs |> Seq.tryFind (fun pv -> pv.AdditionalType = Some additionalType && pv.Name = name)

    member private this.AnnotationColumnIndex() =
        this.Decompose()
        |> Seq.filter (fun c -> c.Header.IsCvParamColumn)
        |> Seq.length

    /// Derive the ordered list of CompositeColumns from the current process list.
    /// Column order: Input → ProtocolREF → ProtocolType → ProtocolDesc → ProtocolUri →
    ///   ProtocolVersion → Characteristics → Components → Parameters → Factors → Output
    member this.Decompose() : ResizeArray<CompositeColumn> =
        let cols = ResizeArray<CompositeColumn>()
        this.SampleizeProjectedRows()
        if processes.Count = 0 then cols
        else

        // ── helpers ────────────────────────────────────────────────────────

        // Collect annotation PVs from all rows for a given AdditionalType,
        // deduplicated by name, ordered by ColumnIndex then first-seen.
        let collectAnnotationHeaders (additionalType: string) (getPVs: Process -> seq<Annotation>) =
            let seen = System.Collections.Generic.Dictionary<string, int>() // name → first colIdx or int.MaxValue
            for p in processes do
                for pv: Annotation in getPVs p do
                    if pv.AdditionalType = Some additionalType then
                        if not (seen.ContainsKey(pv.Name)) then
                            let idx = TableAux.TryGetColumnIndex pv |> Option.defaultValue System.Int32.MaxValue
                            seen.[pv.Name] <- idx
            seen |> Seq.sortBy (fun kv -> kv.Value) |> Seq.map (fun kv -> kv.Key) |> ResizeArray

        // First process used as representative for protocol/IO structure
        let rep = processes.[0]

        // ── Input column ───────────────────────────────────────────────────
        let hasInput = processes |> Seq.exists (fun p -> p.Inputs.Count > 0)
        if hasInput then
            let ioType =
                match rep.Inputs |> Seq.tryHead with
                | Some (SampleNode m) -> TableAux.SampleIOType m
                | Some (DataNode _)     -> IOType.Data
                | None                  -> IOType.Sample
            let cells = ResizeArray<CompositeCell>()
            for p in processes do
                match p.Inputs |> Seq.tryHead with
                | Some (SampleNode m) -> cells.Add(TableAux.SampleCell m)
                | Some (DataNode d)     -> cells.Add(TableAux.DataCell d)
                | None                  -> cells.Add(CompositeCell.FreeText "")
            cols.Add(CompositeColumn(CompositeHeader.Input ioType, cells))

        // ── Protocol columns ───────────────────────────────────────────────
        let hasProtocol = processes |> Seq.exists (fun p -> p.ExecutesProtocol.IsSome)
        if hasProtocol then
            // REF
            let refCells = ResizeArray<CompositeCell>()
            for p in processes do
                let v = p.ExecutesProtocol |> Option.bind (fun pr -> pr.Name) |> Option.defaultValue ""
                refCells.Add(CompositeCell.FreeText v)
            cols.Add(CompositeColumn(CompositeHeader.ProtocolREF, refCells))
            // Type
            let typeCells = ResizeArray<CompositeCell>()
            let hasType = processes |> Seq.exists (fun p -> p.ExecutesProtocol |> Option.bind (fun pr -> pr.IntendedUse) |> Option.isSome)
            if hasType then
                for p in processes do
                    let cell =
                        match p.ExecutesProtocol |> Option.bind (fun pr -> pr.IntendedUse) with
                        | Some dt -> CompositeCell.Term(dt.Name, dt.TAN)
                        | None    -> CompositeCell.FreeText ""
                    typeCells.Add(cell)
                cols.Add(CompositeColumn(CompositeHeader.ProtocolType, typeCells))
            // Description
            let hasDes = processes |> Seq.exists (fun p -> p.ExecutesProtocol |> Option.bind (fun pr -> pr.Description) |> Option.isSome)
            if hasDes then
                let desCells = ResizeArray<CompositeCell>()
                for p in processes do
                    let v = p.ExecutesProtocol |> Option.bind (fun pr -> pr.Description) |> Option.defaultValue ""
                    desCells.Add(CompositeCell.FreeText v)
                cols.Add(CompositeColumn(CompositeHeader.ProtocolDescription, desCells))
            // Uri
            let hasUri = processes |> Seq.exists (fun p -> p.ExecutesProtocol |> Option.bind (fun pr -> pr.Url) |> Option.isSome)
            if hasUri then
                let uriCells = ResizeArray<CompositeCell>()
                for p in processes do
                    let v = p.ExecutesProtocol |> Option.bind (fun pr -> pr.Url) |> Option.defaultValue ""
                    uriCells.Add(CompositeCell.FreeText v)
                cols.Add(CompositeColumn(CompositeHeader.ProtocolUri, uriCells))
            // Version
            let hasVer = processes |> Seq.exists (fun p -> p.ExecutesProtocol |> Option.bind (fun pr -> pr.Version) |> Option.isSome)
            if hasVer then
                let verCells = ResizeArray<CompositeCell>()
                for p in processes do
                    let v = p.ExecutesProtocol |> Option.bind (fun pr -> pr.Version) |> Option.defaultValue ""
                    verCells.Add(CompositeCell.FreeText v)
                cols.Add(CompositeColumn(CompositeHeader.ProtocolVersion, verCells))

        // ── Annotation helper: build one column per unique PV name ─────────
        let addAnnotationColumns (additionalType: string) (getPVs: Process -> seq<Annotation>) =
            // Gather distinct names ordered by ColumnIndex of first occurrence
            let seen = System.Collections.Generic.Dictionary<string, int>()
            for p in processes do
                for pv: Annotation in getPVs p do
                    if pv.AdditionalType = Some additionalType && not (seen.ContainsKey(pv.Name)) then
                        seen.[pv.Name] <- TableAux.TryGetColumnIndex pv |> Option.defaultValue System.Int32.MaxValue
            let orderedNames = seen |> Seq.sortBy (fun kv -> kv.Value) |> Seq.map (fun kv -> kv.Key) |> ResizeArray
            for pvName in orderedNames do
                // representative PV for header
                let repPV =
                    processes
                    |> Seq.collect getPVs
                    |> Seq.tryFind (fun pv -> pv.AdditionalType = Some additionalType && pv.Name = pvName)
                match repPV with
                | None -> ()
                | Some rPV ->
                    let header = TableAux.PVToHeader rPV
                    let cells = ResizeArray<CompositeCell>()
                    for p in processes do
                        let pv = getPVs p |> Seq.tryFind (fun pv -> pv.AdditionalType = Some additionalType && pv.Name = pvName)
                        match pv with
                        | Some pv -> cells.Add(TableAux.PVToCell pv)
                        | None    -> cells.Add(CompositeCell.FreeText "")
                    cols.Add(CompositeColumn(header, cells))

        // ── Characteristics (from input nodes) ─────────────────────────────
        let inputPVs (p: Process) =
            p.Inputs |> Seq.collect (fun n ->
                match n with
                | SampleNode m -> m.AdditionalProperty :> seq<_>
                | DataNode d     -> d.AdditionalProperty :> seq<_>)
        addAnnotationColumns "CharacteristicValue" inputPVs

        // ── Components (from protocol Component) ────────────────────────
        let equipPVs (p: Process) =
            match p.ExecutesProtocol with
            | Some proto -> proto.Components :> seq<_>
            | None       -> Seq.empty
        addAnnotationColumns "Component" equipPVs

        // ── Parameters (from process ParameterValue) ───────────────────────
        addAnnotationColumns "ParameterValue" (fun p -> p.ParameterValue :> seq<_>)

        // ── Factors (from output nodes) ────────────────────────────────────
        let outputPVs (p: Process) =
            p.Outputs |> Seq.collect (fun n ->
                match n with
                | SampleNode m -> m.AdditionalProperty :> seq<_>
                | DataNode d     -> d.AdditionalProperty :> seq<_>)
        addAnnotationColumns "FactorValue" outputPVs

        // ── Output column ──────────────────────────────────────────────────
        let hasOutput = processes |> Seq.exists (fun p -> p.Outputs.Count > 0)
        if hasOutput then
            let ioType =
                match rep.Outputs |> Seq.tryHead with
                | Some (SampleNode m) -> TableAux.SampleIOType m
                | Some (DataNode _)     -> IOType.Data
                | None                  -> IOType.Sample
            let cells = ResizeArray<CompositeCell>()
            for p in processes do
                match p.Outputs |> Seq.tryHead with
                | Some (SampleNode m) -> cells.Add(TableAux.SampleCell m)
                | Some (DataNode d)     -> cells.Add(TableAux.DataCell d)
                | None                  -> cells.Add(CompositeCell.FreeText "")
            cols.Add(CompositeColumn(CompositeHeader.Output ioType, cells))

        cols

    // ── Public properties ─────────────────────────────────────────────────────

    member _.Name = name

    /// The underlying process nodes this table projects.
    member _.Processes = processes

    /// The parent dataset.
    member _.Dataset = dataset

    /// Number of visible rows in the table projection.
    member this.RowCount =
        this.ProjectedRows().Count

    /// Derive headers from the current process state.
    member this.Headers : ResizeArray<CompositeHeader> =
        this.Decompose() |> Seq.map (fun c -> c.Header) |> ResizeArray

    /// Number of columns (derived from current process state).
    member this.ColumnCount = this.Decompose().Count

    /// All columns as CompositeColumn objects (derived live).
    member this.Columns : ResizeArray<CompositeColumn> = this.Decompose()

    // ── Cell / Column read API ────────────────────────────────────────────────

    /// Get a column by index. Raises if out of range.
    member this.GetColumn(columnIndex: int) : CompositeColumn =
        this.Decompose().[columnIndex]

    /// Try to find the first column whose header matches the predicate.
    member this.TryGetColumnByHeader(predicate: CompositeHeader -> bool) : CompositeColumn option =
        this.Decompose() |> Seq.tryFind (fun c -> predicate c.Header)

    /// Get the Input-typed column, if present.
    member this.TryGetInputColumn() : CompositeColumn option =
        this.TryGetColumnByHeader(fun h -> match h with | CompositeHeader.Input _ -> true | _ -> false)

    /// Get the Output-typed column, if present.
    member this.TryGetOutputColumn() : CompositeColumn option =
        this.TryGetColumnByHeader(fun h -> match h with | CompositeHeader.Output _ -> true | _ -> false)

    /// Get all Component-typed columns.
    member this.GetComponentColumns() : ResizeArray<CompositeColumn> =
        this.Decompose() |> Seq.filter (fun c -> match c.Header with | CompositeHeader.Component _ -> true | _ -> false) |> ResizeArray

    /// Try to get a cell at (columnIndex, rowIndex). Returns None if out of range.
    member this.TryGetCellAt(columnIndex: int, rowIndex: int) : CompositeCell option =
        let cols = this.Decompose()
        if columnIndex < 0 || columnIndex >= cols.Count then None
        elif rowIndex < 0 || rowIndex >= cols.[columnIndex].Cells.Count then None
        else Some cols.[columnIndex].Cells.[rowIndex]

    /// Get a cell at (columnIndex, rowIndex). Raises if out of range.
    member this.GetCellAt(columnIndex: int, rowIndex: int) : CompositeCell =
        this.Decompose().[columnIndex].Cells.[rowIndex]

    /// Get all cells of a row as an ordered sequence (one cell per column).
    member this.GetRow(rowIndex: int) : ResizeArray<CompositeCell> =
        this.Decompose() |> Seq.map (fun c -> c.Cells.[rowIndex]) |> ResizeArray

    // ── Column write API ──────────────────────────────────────────────────────

    /// Add an annotation column (Parameter/Factor/Characteristic/Component) to the table.
    /// Appends the corresponding Annotation to the right slot of every process node.
    /// Non-annotation headers (Input, Output, ProtocolREF, etc.) are ignored here —
    /// use the row API for I/O and protocol fields.
    member this.AddColumn(header: CompositeHeader, ?cells: ResizeArray<CompositeCell>) =
        let cells = cells |> Option.defaultValue (ResizeArray())
        let annotationIndex = this.AnnotationColumnIndex()
        let addPV rowIdx (getList: unit -> ResizeArray<Annotation>) =
            let pv = TableAux.MakePV(header, this.CellAt(cells, rowIdx))
            TableAux.SetColumnIndex pv (Some annotationIndex)
            getList().Add(pv)
        let ensureOneProcess () =
            if processes.Count = 0 then
                let p = Process(name)
                processes.Add(p)
                dataset.AddProcess(p)
        match header with
        | CompositeHeader.Input ioType ->
            if processes.Count = 0 then
                let p = Process(name)
                let cellCount = if cells.Count = 0 then 1 else cells.Count
                for i in 0 .. cellCount - 1 do
                    p.AddInput(this.NodeFromCell(ioType, this.CellAt(cells, i), sprintf "%s_%d" name i))
                processes.Add(p)
                dataset.AddProcess(p)
            else
                this.SampleizeProjectedRows()
                for i in 0 .. processes.Count - 1 do
                    processes.[i].AddInput(this.NodeFromCell(ioType, this.CellAt(cells, i), sprintf "%s_%d" name i))
        | CompositeHeader.Output ioType ->
            if processes.Count = 0 then
                let p = Process(name)
                let cellCount = if cells.Count = 0 then 1 else cells.Count
                for i in 0 .. cellCount - 1 do
                    p.AddOutput(this.NodeFromCell(ioType, this.CellAt(cells, i), sprintf "%s_%d_out" name i))
                processes.Add(p)
                dataset.AddProcess(p)
            else
                this.SampleizeProjectedRows()
                for i in 0 .. processes.Count - 1 do
                    processes.[i].AddOutput(this.NodeFromCell(ioType, this.CellAt(cells, i), sprintf "%s_%d_out" name i))
        | CompositeHeader.Parameter _ ->
            ensureOneProcess()
            this.SampleizeProjectedRows()
            for i in 0 .. processes.Count - 1 do
                addPV i (fun () -> processes.[i].ParameterValue)
        | CompositeHeader.Characteristic _ ->
            ensureOneProcess()
            this.SampleizeProjectedRows()
            for i in 0 .. processes.Count - 1 do
                let p = processes.[i]
                match this.EnsureInput(p, Some 0, IOType.Sample, CompositeCell.FreeText(sprintf "%s_%d" name i)) with
                | SampleNode m -> addPV i (fun () -> m.AdditionalProperty)
                | DataNode d -> addPV i (fun () -> d.AdditionalProperty)
        | CompositeHeader.Factor _ ->
            ensureOneProcess()
            this.SampleizeProjectedRows()
            for i in 0 .. processes.Count - 1 do
                let p = processes.[i]
                match this.EnsureOutput(p, Some 0, IOType.Sample, CompositeCell.FreeText(sprintf "%s_%d_out" name i)) with
                | SampleNode m -> addPV i (fun () -> m.AdditionalProperty)
                | DataNode d -> addPV i (fun () -> d.AdditionalProperty)
        | CompositeHeader.Component _ ->
            ensureOneProcess()
            this.SampleizeProjectedRows()
            for i in 0 .. processes.Count - 1 do
                let proto = this.EnsureProtocol(processes.[i])
                addPV i (fun () -> proto.Components)
        | CompositeHeader.ProtocolREF ->
            if cells.Count > 0 then
                ensureOneProcess()
                this.SampleizeProjectedRows()
                for i in 0 .. processes.Count - 1 do
                    match this.CellAt(cells, i) with
                    | CompositeCell.FreeText v -> (this.EnsureProtocol(processes.[i])).Name <- Some v
                    | _ -> ()
        | CompositeHeader.ProtocolType ->
            if cells.Count > 0 then
                ensureOneProcess()
                this.SampleizeProjectedRows()
                for i in 0 .. processes.Count - 1 do
                    match this.CellAt(cells, i) with
                    | CompositeCell.Term(n, tan) ->
                        let dt = DefinedTerm(n)
                        dt.TAN <- tan
                        (this.EnsureProtocol(processes.[i])).IntendedUse <- Some dt
                    | CompositeCell.FreeText n ->
                        (this.EnsureProtocol(processes.[i])).IntendedUse <- Some(DefinedTerm(n))
                    | _ -> ()
        | CompositeHeader.ProtocolDescription ->
            if cells.Count > 0 then
                ensureOneProcess()
                this.SampleizeProjectedRows()
                for i in 0 .. processes.Count - 1 do
                    match this.CellAt(cells, i) with
                    | CompositeCell.FreeText v -> (this.EnsureProtocol(processes.[i])).Description <- Some v
                    | _ -> ()
        | CompositeHeader.ProtocolUri ->
            if cells.Count > 0 then
                ensureOneProcess()
                this.SampleizeProjectedRows()
                for i in 0 .. processes.Count - 1 do
                    match this.CellAt(cells, i) with
                    | CompositeCell.FreeText v -> (this.EnsureProtocol(processes.[i])).Url <- Some v
                    | _ -> ()
        | CompositeHeader.ProtocolVersion ->
            if cells.Count > 0 then
                ensureOneProcess()
                this.SampleizeProjectedRows()
                for i in 0 .. processes.Count - 1 do
                    match this.CellAt(cells, i) with
                    | CompositeCell.FreeText v -> (this.EnsureProtocol(processes.[i])).Version <- Some v
                    | _ -> ()
        | _ -> ()

    /// Remove the first annotation column matching the given header from every process node.
    member _.RemoveColumn(header: CompositeHeader) =
        let removeFirst (pvList: ResizeArray<Annotation>) (additionalType: string) (name: string) =
            let idx = pvList |> Seq.tryFindIndex (fun pv -> pv.AdditionalType = Some additionalType && pv.Name = name)
            match idx with
            | Some i -> pvList.RemoveAt(i)
            | None   -> ()
        match header with
        | CompositeHeader.Input _ ->
            for p in processes do
                while p.Inputs.Count > 0 do
                    p.RemoveInput(p.Inputs.[0])
        | CompositeHeader.Output _ ->
            for p in processes do
                while p.Outputs.Count > 0 do
                    p.RemoveOutput(p.Outputs.[0])
        | CompositeHeader.Parameter(dt) ->
            for p in processes do removeFirst p.ParameterValue "ParameterValue" dt.Name
        | CompositeHeader.Characteristic(dt) ->
            for p in processes do
                match p.Inputs |> Seq.tryHead with
                | Some (SampleNode m) -> removeFirst m.AdditionalProperty "CharacteristicValue" dt.Name
                | Some (DataNode d)     -> removeFirst d.AdditionalProperty "CharacteristicValue" dt.Name
                | None -> ()
        | CompositeHeader.Factor(dt) ->
            for p in processes do
                match p.Outputs |> Seq.tryHead with
                | Some (SampleNode m) -> removeFirst m.AdditionalProperty "FactorValue" dt.Name
                | Some (DataNode d)     -> removeFirst d.AdditionalProperty "FactorValue" dt.Name
                | None -> ()
        | CompositeHeader.Component(dt) ->
            for p in processes do
                match p.ExecutesProtocol with
                | Some proto -> removeFirst proto.Components "Component" dt.Name
                | None -> ()
        | _ -> ()

    // ── Row write API ─────────────────────────────────────────────────────────

    /// Derive the current column structure (header list) from existing processes.
    /// Used when composing a new row to know which slots to fill.
    member private this.CurrentHeaders() = this.Decompose() |> Seq.map (fun c -> c.Header) |> ResizeArray

    /// Create and register a new Process for this table, using the existing
    /// process structure as a template. Optionally supply cell values for each column.
    member this.AddRow(?cells: ResizeArray<CompositeCell>, ?index: int) =
        let headers = this.CurrentHeaders()
        let cells   = cells |> Option.defaultValue (ResizeArray(Seq.init headers.Count (fun _ -> CompositeCell.FreeText "")))
        let rowIdx  = processes.Count  // index within this table (for synthetic node naming)

        let proc = Process(name)

        // Clone protocol from first process if available
        match processes |> Seq.tryHead |> Option.bind (fun p -> p.ExecutesProtocol) with
        | Some proto ->
            let p2 = Recipe()
            p2.Name        <- proto.Name
            p2.Description <- proto.Description
            p2.Version     <- proto.Version
            p2.Url         <- proto.Url
            p2.IntendedUse <- proto.IntendedUse
            proc.ExecutesProtocol <- Some p2
        | None -> ()

        // Apply each cell to the correct graph slot
        for colIdx in 0 .. headers.Count - 1 do
            let header = headers.[colIdx]
            let cell   = if colIdx < cells.Count then cells.[colIdx] else CompositeCell.FreeText ""
            match header with
            | CompositeHeader.Input ioType ->
                let node =
                    match cell, ioType with
                    | CompositeCell.Data d, _         -> IONode.DataNode d
                    | CompositeCell.FreeText n, IOType.Data ->
                        let d = Data(n)
                        IONode.DataNode d
                    | CompositeCell.FreeText n, _ ->
                        let m = Sample(n)
                        match ioType with
                        | IOType.Source   -> m.AdditionalType <- Some "Source"
                        | IOType.Sample   -> m.AdditionalType <- Some "Sample"
                        | IOType.FreeText t -> m.AdditionalType <- Some t
                        | _ -> ()
                        IONode.SampleNode m
                    | _, _ -> IONode.SampleNode(Sample(sprintf "%s_%d" name rowIdx))
                proc.AddInput(node)
            | CompositeHeader.Output ioType ->
                let node =
                    match cell, ioType with
                    | CompositeCell.Data d, _         -> IONode.DataNode d
                    | CompositeCell.FreeText n, IOType.Data ->
                        let d = Data(n)
                        IONode.DataNode d
                    | CompositeCell.FreeText n, _ ->
                        let m = Sample(n)
                        match ioType with
                        | IOType.Source   -> m.AdditionalType <- Some "Source"
                        | IOType.Sample   -> m.AdditionalType <- Some "Sample"
                        | IOType.FreeText t -> m.AdditionalType <- Some t
                        | _ -> ()
                        IONode.SampleNode m
                    | _, _ -> IONode.SampleNode(Sample(sprintf "%s_%d_out" name rowIdx))
                proc.AddOutput(node)
            | CompositeHeader.Parameter(dt) ->
                let pv = Annotation(dt.Name)
                pv.NameTAN        <- dt.TAN
                pv.AdditionalType <- Some "ParameterValue"
                TableAux.ApplyCellToPV(pv, cell)
                proc.AddParameterValue(pv)
            | CompositeHeader.Characteristic(dt) ->
                let pv = Annotation(dt.Name)
                pv.NameTAN        <- dt.TAN
                pv.AdditionalType <- Some "CharacteristicValue"
                TableAux.ApplyCellToPV(pv, cell)
                match proc.Inputs |> Seq.tryHead with
                | Some (SampleNode m) -> m.AddAdditionalProperty(pv)
                | Some (DataNode d)     -> d.AddAdditionalProperty(pv)
                | None -> ()
            | CompositeHeader.Factor(dt) ->
                let pv = Annotation(dt.Name)
                pv.NameTAN        <- dt.TAN
                pv.AdditionalType <- Some "FactorValue"
                TableAux.ApplyCellToPV(pv, cell)
                match proc.Outputs |> Seq.tryHead with
                | Some (SampleNode m) -> m.AddAdditionalProperty(pv)
                | Some (DataNode d)     -> d.AddAdditionalProperty(pv)
                | None -> ()
            | CompositeHeader.Component(dt) ->
                let pv = Annotation(dt.Name)
                pv.NameTAN        <- dt.TAN
                pv.AdditionalType <- Some "Component"
                TableAux.ApplyCellToPV(pv, cell)
                match proc.ExecutesProtocol with
                | Some proto -> proto.AddComponent(pv)
                | None -> ()
            | CompositeHeader.ProtocolREF ->
                match cell with
                | CompositeCell.FreeText n ->
                    match proc.ExecutesProtocol with
                    | Some proto -> proto.Name <- Some n
                    | None ->
                        let proto = Recipe()
                        proto.Name <- Some n
                        proc.ExecutesProtocol <- Some proto
                | _ -> ()
            | CompositeHeader.ProtocolType ->
                match cell with
                | CompositeCell.Term(n, tan) ->
                    match proc.ExecutesProtocol with
                    | Some proto ->
                        let dt = DefinedTerm(n)
                        dt.TAN <- tan
                        proto.IntendedUse <- Some dt
                    | None -> ()
                | _ -> ()
            | CompositeHeader.ProtocolDescription ->
                match cell with
                | CompositeCell.FreeText v ->
                    match proc.ExecutesProtocol with
                    | Some proto -> proto.Description <- Some v
                    | None -> ()
                | _ -> ()
            | CompositeHeader.ProtocolUri ->
                match cell with
                | CompositeCell.FreeText v ->
                    match proc.ExecutesProtocol with
                    | Some proto -> proto.Url <- Some v
                    | None -> ()
                | _ -> ()
            | CompositeHeader.ProtocolVersion ->
                match cell with
                | CompositeCell.FreeText v ->
                    match proc.ExecutesProtocol with
                    | Some proto -> proto.Version <- Some v
                    | None -> ()
                | _ -> ()
            | _ -> ()

        // Insert at the requested position within the table's process slice
        match index with
        | Some i when i >= 0 && i < processes.Count ->
            processes.Insert(i, proc)
            dataset.Processes.Insert(dataset.Processes.IndexOf(processes.[i + 1]), proc)
        | _ ->
            processes.Add(proc)
            dataset.AddProcess(proc)

    /// Append an empty row (one new Process with no annotations).
    member this.AppendRow(?cells: ResizeArray<CompositeCell>) =
        this.AddRow(?cells = cells)

    /// Remove the row at `rowIndex` from both the table view and the parent dataset.
    member _.RemoveRow(rowIndex: int) =
        if rowIndex >= 0 && rowIndex < processes.Count then
            let proc = processes.[rowIndex]
            processes.RemoveAt(rowIndex)
            dataset.RemoveProcess(proc)

    /// Replace all cells in the row at `rowIndex` by updating the underlying process node.
    member this.UpdateRow(rowIndex: int, cells: ResizeArray<CompositeCell>) =
        this.SampleizeProjectedRows()
        if rowIndex >= 0 && rowIndex < processes.Count then
            let headers = this.CurrentHeaders()
            let p       = processes.[rowIndex]
            for colIdx in 0 .. headers.Count - 1 do
                let header = headers.[colIdx]
                let cell   = if colIdx < cells.Count then cells.[colIdx] else CompositeCell.FreeText ""
                match header with
                | CompositeHeader.Parameter(dt) ->
                    match p.TryGetParameterValue(dt.Name) with
                    | Some pv -> TableAux.ApplyCellToPV(pv, cell)
                    | None    ->
                        let pv = Annotation(dt.Name)
                        pv.AdditionalType <- Some "ParameterValue"
                        TableAux.ApplyCellToPV(pv, cell)
                        p.AddParameterValue(pv)
                | CompositeHeader.Input ioType ->
                    match p.Inputs |> Seq.tryHead with
                    | Some node -> this.ApplyCellToNode(node, ioType, cell, true, p)
                    | None -> p.AddInput(this.NodeFromCell(ioType, cell, sprintf "%s_%d" name rowIndex))
                | CompositeHeader.Output ioType ->
                    match p.Outputs |> Seq.tryHead with
                    | Some node -> this.ApplyCellToNode(node, ioType, cell, false, p)
                    | None -> p.AddOutput(this.NodeFromCell(ioType, cell, sprintf "%s_%d_out" name rowIndex))
                | CompositeHeader.Characteristic(dt) ->
                    let pvList =
                        match p.Inputs |> Seq.tryHead with
                        | Some (SampleNode m) -> Some m.AdditionalProperty
                        | Some (DataNode d)     -> Some d.AdditionalProperty
                        | None                  -> None
                    match pvList with
                    | Some lst ->
                        match lst |> Seq.tryFind (fun pv -> pv.AdditionalType = Some "CharacteristicValue" && pv.Name = dt.Name) with
                        | Some pv -> TableAux.ApplyCellToPV(pv, cell)
                        | None    ->
                            let pv = Annotation(dt.Name)
                            pv.AdditionalType <- Some "CharacteristicValue"
                            TableAux.ApplyCellToPV(pv, cell)
                            lst.Add(pv)
                    | None -> ()
                | CompositeHeader.Factor(dt) ->
                    let pvList =
                        match p.Outputs |> Seq.tryHead with
                        | Some (SampleNode m) -> Some m.AdditionalProperty
                        | Some (DataNode d)     -> Some d.AdditionalProperty
                        | None                  -> None
                    match pvList with
                    | Some lst ->
                        match lst |> Seq.tryFind (fun pv -> pv.AdditionalType = Some "FactorValue" && pv.Name = dt.Name) with
                        | Some pv -> TableAux.ApplyCellToPV(pv, cell)
                        | None    ->
                            let pv = Annotation(dt.Name)
                            pv.AdditionalType <- Some "FactorValue"
                            TableAux.ApplyCellToPV(pv, cell)
                            lst.Add(pv)
                    | None -> ()
                | CompositeHeader.Component(dt) ->
                    let proto = this.EnsureProtocol(p)
                    match proto.Components |> Seq.tryFind (fun pv -> pv.Name = dt.Name) with
                    | Some pv -> TableAux.ApplyCellToPV(pv, cell)
                    | None    ->
                        let pv = Annotation(dt.Name)
                        pv.AdditionalType <- Some "Component"
                        TableAux.ApplyCellToPV(pv, cell)
                        proto.AddComponent(pv)
                | CompositeHeader.ProtocolREF ->
                    match cell with
                    | CompositeCell.FreeText v -> (this.EnsureProtocol(p)).Name <- Some v
                    | _ -> ()
                | CompositeHeader.ProtocolType ->
                    match cell with
                    | CompositeCell.Term(n, tan) ->
                        let dt = DefinedTerm(n)
                        dt.TAN <- tan
                        (this.EnsureProtocol(p)).IntendedUse <- Some dt
                    | CompositeCell.FreeText n ->
                        (this.EnsureProtocol(p)).IntendedUse <- Some(DefinedTerm(n))
                    | _ -> ()
                | CompositeHeader.ProtocolDescription ->
                    match cell with
                    | CompositeCell.FreeText v -> (this.EnsureProtocol(p)).Description <- Some v
                    | _ -> ()
                | CompositeHeader.ProtocolUri ->
                    match cell with
                    | CompositeCell.FreeText v -> (this.EnsureProtocol(p)).Url <- Some v
                    | _ -> ()
                | CompositeHeader.ProtocolVersion ->
                    match cell with
                    | CompositeCell.FreeText v -> (this.EnsureProtocol(p)).Version <- Some v
                    | _ -> ()
                | _ -> ()

// ─────────────────────────────────────────────────────────────────────────────
// Tables
// ─────────────────────────────────────────────────────────────────────────────

/// Ordered, named collection of Table objects backed by a Dataset.
/// Groups processes by name: each unique process name in the dataset becomes one Table.
[<AttachMembers>]
type Tables(dataset: Dataset) =

    /// Build the live list of Table objects by grouping dataset processes by name.
    member _.GetTables() : ResizeArray<Table> =
        let groups = System.Collections.Generic.Dictionary<string, ResizeArray<Process>>()
        let order  = ResizeArray<string>()
        for p in dataset.Processes do
            if not (groups.ContainsKey(p.Name)) then
                groups.[p.Name] <- ResizeArray()
                order.Add(p.Name)
            groups.[p.Name].Add(p)
        order |> Seq.map (fun n -> Table(n, groups.[n], dataset)) |> ResizeArray

    /// Number of tables (= number of distinct process names).
    member this.TableCount = this.GetTables().Count

    /// Names of all tables in order.
    member this.TableNames : ResizeArray<string> =
        this.GetTables() |> Seq.map (fun t -> t.Name) |> ResizeArray

    /// Get a table by index.
    member this.GetTableAt(index: int) : Table =
        this.GetTables().[index]

    /// Get a table by name. Raises if not found.
    member this.GetTable(name: string) : Table =
        this.GetTables() |> Seq.find (fun t -> t.Name = name)

    /// Try to get a table by name.
    member this.TryGetTable(name: string) : Table option =
        this.GetTables() |> Seq.tryFind (fun t -> t.Name = name)

    /// Add a new empty table (creates no processes until rows are added).
    /// Fails if a table with that name already exists.
    member this.AddTable(name: string) : Table =
        if this.GetTables() |> Seq.exists (fun t -> t.Name = name) then
            failwithf "Table '%s' already exists." name
        Table(name, ResizeArray(), dataset)

    /// Remove all processes belonging to the named table from the dataset.
    member _.RemoveTable(name: string) =
        let toRemove = dataset.Processes |> Seq.filter (fun p -> p.Name = name) |> ResizeArray
        for p in toRemove do dataset.RemoveProcess(p)

    /// Rename all processes belonging to `oldName` to `newName`.
    member _.RenameTable(oldName: string, newName: string) =
        for p in dataset.Processes do
            if p.Name = oldName then p.Name <- newName

    /// Add a row to the named table.
    member this.AddRow(tableName: string, ?cells: ResizeArray<CompositeCell>, ?rowIndex: int) =
        this.GetTable(tableName).AddRow(?cells = cells, ?index = rowIndex)

    /// Remove a row from the named table.
    member this.RemoveRow(tableName: string, rowIndex: int) =
        this.GetTable(tableName).RemoveRow(rowIndex)

    /// Add a column to the named table.
    member this.AddColumn(tableName: string, header: CompositeHeader, ?cells: ResizeArray<CompositeCell>) =
        this.GetTable(tableName).AddColumn(header, ?cells = cells)

    /// Remove a column from the named table.
    member this.RemoveColumn(tableName: string, header: CompositeHeader) =
        this.GetTable(tableName).RemoveColumn(header)

// ─────────────────────────────────────────────────────────────────────────────
// Dataset extension
// ─────────────────────────────────────────────────────────────────────────────

[<AutoOpen>]
module DatasetTableExtensions =
    type ProcessCore.Dataset with
        /// A live tabular view of all processes in this dataset, grouped by process name.
        member this.Tables = Tables(this)
