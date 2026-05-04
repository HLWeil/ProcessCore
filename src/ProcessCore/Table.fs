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
    | Material
    | Data
    | FreeText of string

// ─────────────────────────────────────────────────────────────────────────────
// CompositeHeader
// ─────────────────────────────────────────────────────────────────────────────

/// Typed column header. Carries the column role and, for annotation columns,
/// the ontology term identifying what is being described.
[<AttachMembers>]
[<RequireQualifiedAccess>]
type CompositeHeader =
    // Annotation columns — each carries a (name, TAN) pair
    | Parameter         of name: string * tan: string option
    | Characteristic    of name: string * tan: string option
    | Factor            of name: string * tan: string option
    | Component         of name: string * tan: string option
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
    | Term      of name: string * tan: string option
    /// Numeric value with unit term
    | Unitized  of value: string * unitName: string * unitTAN: string option
    /// Data file entity — used in Input/Output columns typed as IOType.Data;
    /// carries structured file metadata (path, selector, format, etc.)
    | Data      of ProcessCore.Data

// ─────────────────────────────────────────────────────────────────────────────
// CompositeColumn
// ─────────────────────────────────────────────────────────────────────────────

/// A column: one header paired with an ordered list of cells (one per row).
[<AttachMembers>]
type CompositeColumn(header: CompositeHeader, cells: ResizeArray<CompositeCell>) =

    new(header: CompositeHeader) = CompositeColumn(header, ResizeArray())

    member _.Header = header
    member _.Cells  = cells

    member _.ColumnCount = cells.Count

// ─────────────────────────────────────────────────────────────────────────────
// TableAux
// ─────────────────────────────────────────────────────────────────────────────

/// Auxiliary functions for composing and decomposing Table ↔ process graph.
module TableAux =

    /// IOType for a material node based on its AdditionalType tag.
    let MaterialIOType (m: Material) : IOType =
        match m.AdditionalType with
        | Some "Source"   -> IOType.Source
        | Some "Sample"   -> IOType.Sample
        | Some "Material" -> IOType.Material
        | Some other      -> IOType.FreeText other
        | None            -> IOType.Sample

    /// Cell for a material input/output node.
    let MaterialCell (m: Material) : CompositeCell =
        CompositeCell.FreeText m.Name

    /// Cell for a data input/output node.
    let DataCell (d: Data) : CompositeCell =
        CompositeCell.Data d

    /// Build a CompositeCell from a PropertyValue.
    let PVToCell (pv: PropertyValue) : CompositeCell =
        match pv.ValueTAN, pv.Unit with
        | _, Some u ->
            CompositeCell.Unitized(pv.Value |> Option.defaultValue "", u, pv.UnitTAN)
        | Some vtan, None ->
            CompositeCell.Term(pv.Value |> Option.defaultValue "", Some vtan)
        | None, None ->
            match pv.Value with
            | Some v -> CompositeCell.FreeText v
            | None   -> CompositeCell.FreeText ""

    /// Build a CompositeHeader from a PropertyValue.
    let PVToHeader (pv: PropertyValue) : CompositeHeader =
        let pair = (pv.Name, pv.NameTAN)
        match pv.AdditionalType with
        | Some "ParameterValue"      -> CompositeHeader.Parameter pair
        | Some "FactorValue"         -> CompositeHeader.Factor pair
        | Some "CharacteristicValue" -> CompositeHeader.Characteristic pair
        | Some "Component"           -> CompositeHeader.Component pair
        | _                          -> CompositeHeader.Parameter pair

    /// Apply a cell value back into a PropertyValue.
    let ApplyCellToPV (pv: PropertyValue, cell: CompositeCell) =
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
        | CompositeCell.Data _ -> ()  // Data cells don't map to PropertyValues

    /// Create a fresh PropertyValue from a header + cell + annotation type tag.
    let MakePV (header: CompositeHeader, cell: CompositeCell) : PropertyValue =
        let pv =
            match header with
            | CompositeHeader.Parameter(n, tan)      -> PropertyValue(n, NameTAN = tan, AdditionalType = Some "ParameterValue")
            | CompositeHeader.Characteristic(n, tan) -> PropertyValue(n, NameTAN = tan, AdditionalType = Some "CharacteristicValue")
            | CompositeHeader.Factor(n, tan)         -> PropertyValue(n, NameTAN = tan, AdditionalType = Some "FactorValue")
            | CompositeHeader.Component(n, tan)      -> PropertyValue(n, NameTAN = tan, AdditionalType = Some "Component")
            | _                                      -> PropertyValue("")
        ApplyCellToPV(pv, cell)
        pv

// ─────────────────────────────────────────────────────────────────────────────
// Table
// ─────────────────────────────────────────────────────────────────────────────

/// A live tabular view of a group of LabProcess nodes that share the same name.
/// Wraps the underlying processes directly — reads decompose on the fly,
/// writes compose back into the process graph.
[<AttachMembers>]
type Table(name: string, processes: ResizeArray<LabProcess>, dataset: Dataset) =

    /// Derive the ordered list of CompositeColumns from the current process list.
    /// Column order: Input → ProtocolREF → ProtocolType → ProtocolDesc → ProtocolUri →
    ///   ProtocolVersion → Characteristics → Components → Parameters → Factors → Output
    member this.Decompose() : ResizeArray<CompositeColumn> =
        let cols = ResizeArray<CompositeColumn>()
        if processes.Count = 0 then cols
        else

        // ── helpers ────────────────────────────────────────────────────────

        // Collect annotation PVs from all rows for a given AdditionalType,
        // deduplicated by name, ordered by ColumnIndex then first-seen.
        let collectAnnotationHeaders (additionalType: string) (getPVs: LabProcess -> seq<PropertyValue>) =
            let seen = System.Collections.Generic.Dictionary<string, int>() // name → first colIdx or int.MaxValue
            for p in processes do
                for pv: PropertyValue in getPVs p do
                    if pv.AdditionalType = Some additionalType then
                        if not (seen.ContainsKey(pv.Name)) then
                            let idx = System.Int32.MaxValue
                            seen.[pv.Name] <- idx
            seen |> Seq.sortBy (fun kv -> kv.Value) |> Seq.map (fun kv -> kv.Key) |> ResizeArray

        // First process used as representative for protocol/IO structure
        let rep = processes.[0]

        // ── Input column ───────────────────────────────────────────────────
        let hasInput = processes |> Seq.exists (fun p -> p.Inputs.Count > 0)
        if hasInput then
            let ioType =
                match rep.Inputs |> Seq.tryHead with
                | Some (MaterialNode m) -> TableAux.MaterialIOType m
                | Some (DataNode _)     -> IOType.Data
                | None                  -> IOType.Sample
            let cells = ResizeArray<CompositeCell>()
            for p in processes do
                match p.Inputs |> Seq.tryHead with
                | Some (MaterialNode m) -> cells.Add(TableAux.MaterialCell m)
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
        let addAnnotationColumns (additionalType: string) (getPVs: LabProcess -> seq<PropertyValue>) =
            // Gather distinct names ordered by ColumnIndex of first occurrence
            let seen = System.Collections.Generic.Dictionary<string, int>()
            for p in processes do
                for pv: PropertyValue in getPVs p do
                    if pv.AdditionalType = Some additionalType && not (seen.ContainsKey(pv.Name)) then
                        seen.[pv.Name] <- System.Int32.MaxValue
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
        let inputPVs (p: LabProcess) =
            p.Inputs |> Seq.collect (fun n ->
                match n with
                | MaterialNode m -> m.AdditionalProperty :> seq<_>
                | DataNode d     -> d.AdditionalProperty :> seq<_>)
        addAnnotationColumns "CharacteristicValue" inputPVs

        // ── Components (from protocol LabEquipment) ────────────────────────
        let equipPVs (p: LabProcess) =
            match p.ExecutesProtocol with
            | Some proto -> proto.LabEquipment :> seq<_>
            | None       -> Seq.empty
        addAnnotationColumns "Component" equipPVs

        // ── Parameters (from process ParameterValue) ───────────────────────
        addAnnotationColumns "ParameterValue" (fun p -> p.ParameterValue :> seq<_>)

        // ── Factors (from output nodes) ────────────────────────────────────
        let outputPVs (p: LabProcess) =
            p.Outputs |> Seq.collect (fun n ->
                match n with
                | MaterialNode m -> m.AdditionalProperty :> seq<_>
                | DataNode d     -> d.AdditionalProperty :> seq<_>)
        addAnnotationColumns "FactorValue" outputPVs

        // ── Output column ──────────────────────────────────────────────────
        let hasOutput = processes |> Seq.exists (fun p -> p.Outputs.Count > 0)
        if hasOutput then
            let ioType =
                match rep.Outputs |> Seq.tryHead with
                | Some (MaterialNode m) -> TableAux.MaterialIOType m
                | Some (DataNode _)     -> IOType.Data
                | None                  -> IOType.Sample
            let cells = ResizeArray<CompositeCell>()
            for p in processes do
                match p.Outputs |> Seq.tryHead with
                | Some (MaterialNode m) -> cells.Add(TableAux.MaterialCell m)
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

    /// Number of rows (one per process node).
    member _.RowCount = processes.Count

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
    /// Appends the corresponding PropertyValue to the right slot of every process node.
    /// Non-annotation headers (Input, Output, ProtocolREF, etc.) are ignored here —
    /// use the row API for I/O and protocol fields.
    member _.AddColumn(header: CompositeHeader, ?cells: ResizeArray<CompositeCell>) =
        let cells = cells |> Option.defaultValue (ResizeArray())
        let addPV (p: LabProcess) (rowIdx: int) (getList: unit -> ResizeArray<PropertyValue>) =
            let cell = if rowIdx < cells.Count then cells.[rowIdx] else CompositeCell.FreeText ""
            let pv   = TableAux.MakePV(header, cell)
            getList().Add(pv)
        match header with
        | CompositeHeader.Parameter _ ->
            for i in 0 .. processes.Count - 1 do
                addPV processes.[i] i (fun () -> processes.[i].ParameterValue)
        | CompositeHeader.Characteristic _ ->
            for i in 0 .. processes.Count - 1 do
                let p = processes.[i]
                match p.Inputs |> Seq.tryHead with
                | Some (MaterialNode m) -> addPV p i (fun () -> m.AdditionalProperty)
                | Some (DataNode d)     -> addPV p i (fun () -> d.AdditionalProperty)
                | None                  -> ()
        | CompositeHeader.Factor _ ->
            for i in 0 .. processes.Count - 1 do
                let p = processes.[i]
                match p.Outputs |> Seq.tryHead with
                | Some (MaterialNode m) -> addPV p i (fun () -> m.AdditionalProperty)
                | Some (DataNode d)     -> addPV p i (fun () -> d.AdditionalProperty)
                | None                  -> ()
        | CompositeHeader.Component _ ->
            for i in 0 .. processes.Count - 1 do
                let p = processes.[i]
                match p.ExecutesProtocol with
                | Some proto -> addPV p i (fun () -> proto.LabEquipment)
                | None       -> ()
        | _ -> ()  // non-annotation columns handled elsewhere

    /// Remove the first annotation column matching the given header from every process node.
    member _.RemoveColumn(header: CompositeHeader) =
        let removeFirst (pvList: ResizeArray<PropertyValue>) (additionalType: string) (name: string) =
            let idx = pvList |> Seq.tryFindIndex (fun pv -> pv.AdditionalType = Some additionalType && pv.Name = name)
            match idx with
            | Some i -> pvList.RemoveAt(i)
            | None   -> ()
        match header with
        | CompositeHeader.Parameter(n, _) ->
            for p in processes do removeFirst p.ParameterValue "ParameterValue" n
        | CompositeHeader.Characteristic(n, _) ->
            for p in processes do
                match p.Inputs |> Seq.tryHead with
                | Some (MaterialNode m) -> removeFirst m.AdditionalProperty "CharacteristicValue" n
                | Some (DataNode d)     -> removeFirst d.AdditionalProperty "CharacteristicValue" n
                | None -> ()
        | CompositeHeader.Factor(n, _) ->
            for p in processes do
                match p.Outputs |> Seq.tryHead with
                | Some (MaterialNode m) -> removeFirst m.AdditionalProperty "FactorValue" n
                | Some (DataNode d)     -> removeFirst d.AdditionalProperty "FactorValue" n
                | None -> ()
        | CompositeHeader.Component(n, _) ->
            for p in processes do
                match p.ExecutesProtocol with
                | Some proto -> removeFirst proto.LabEquipment "Component" n
                | None -> ()
        | _ -> ()

    // ── Row write API ─────────────────────────────────────────────────────────

    /// Derive the current column structure (header list) from existing processes.
    /// Used when composing a new row to know which slots to fill.
    member private this.CurrentHeaders() = this.Decompose() |> Seq.map (fun c -> c.Header) |> ResizeArray

    /// Create and register a new LabProcess for this table, using the existing
    /// process structure as a template. Optionally supply cell values for each column.
    member this.AddRow(?cells: ResizeArray<CompositeCell>, ?index: int) =
        let headers = this.CurrentHeaders()
        let cells   = cells |> Option.defaultValue (ResizeArray(Seq.init headers.Count (fun _ -> CompositeCell.FreeText "")))
        let rowIdx  = processes.Count  // index within this table (for synthetic node naming)

        let proc = LabProcess(name)

        // Clone protocol from first process if available
        match processes |> Seq.tryHead |> Option.bind (fun p -> p.ExecutesProtocol) with
        | Some proto ->
            let p2 = LabProtocol()
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
                        let m = Material(n)
                        match ioType with
                        | IOType.Source   -> m.AdditionalType <- Some "Source"
                        | IOType.Sample   -> m.AdditionalType <- Some "Sample"
                        | IOType.Material -> m.AdditionalType <- Some "Material"
                        | IOType.FreeText t -> m.AdditionalType <- Some t
                        | _ -> ()
                        IONode.MaterialNode m
                    | _, _ -> IONode.MaterialNode(Material(sprintf "%s_%d" name rowIdx))
                proc.AddInput(node)
            | CompositeHeader.Output ioType ->
                let node =
                    match cell, ioType with
                    | CompositeCell.Data d, _         -> IONode.DataNode d
                    | CompositeCell.FreeText n, IOType.Data ->
                        let d = Data(n)
                        IONode.DataNode d
                    | CompositeCell.FreeText n, _ ->
                        let m = Material(n)
                        match ioType with
                        | IOType.Source   -> m.AdditionalType <- Some "Source"
                        | IOType.Sample   -> m.AdditionalType <- Some "Sample"
                        | IOType.Material -> m.AdditionalType <- Some "Material"
                        | IOType.FreeText t -> m.AdditionalType <- Some t
                        | _ -> ()
                        IONode.MaterialNode m
                    | _, _ -> IONode.MaterialNode(Material(sprintf "%s_%d_out" name rowIdx))
                proc.AddOutput(node)
            | CompositeHeader.Parameter(n, tan) ->
                let pv = PropertyValue(n)
                pv.NameTAN        <- tan
                pv.AdditionalType <- Some "ParameterValue"
                TableAux.ApplyCellToPV(pv, cell)
                proc.AddParameterValue(pv)
            | CompositeHeader.Characteristic(n, tan) ->
                let pv = PropertyValue(n)
                pv.NameTAN        <- tan
                pv.AdditionalType <- Some "CharacteristicValue"
                TableAux.ApplyCellToPV(pv, cell)
                match proc.Inputs |> Seq.tryHead with
                | Some (MaterialNode m) -> m.AddAdditionalProperty(pv)
                | Some (DataNode d)     -> d.AddAdditionalProperty(pv)
                | None -> ()
            | CompositeHeader.Factor(n, tan) ->
                let pv = PropertyValue(n)
                pv.NameTAN        <- tan
                pv.AdditionalType <- Some "FactorValue"
                TableAux.ApplyCellToPV(pv, cell)
                match proc.Outputs |> Seq.tryHead with
                | Some (MaterialNode m) -> m.AddAdditionalProperty(pv)
                | Some (DataNode d)     -> d.AddAdditionalProperty(pv)
                | None -> ()
            | CompositeHeader.Component(n, tan) ->
                let pv = PropertyValue(n)
                pv.NameTAN        <- tan
                pv.AdditionalType <- Some "Component"
                TableAux.ApplyCellToPV(pv, cell)
                match proc.ExecutesProtocol with
                | Some proto -> proto.AddLabEquipment(pv)
                | None -> ()
            | CompositeHeader.ProtocolREF ->
                match cell with
                | CompositeCell.FreeText n ->
                    match proc.ExecutesProtocol with
                    | Some proto -> proto.Name <- Some n
                    | None ->
                        let proto = LabProtocol()
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

    /// Append an empty row (one new LabProcess with no annotations).
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
        if rowIndex >= 0 && rowIndex < processes.Count then
            let headers = this.CurrentHeaders()
            let p       = processes.[rowIndex]
            for colIdx in 0 .. headers.Count - 1 do
                let header = headers.[colIdx]
                let cell   = if colIdx < cells.Count then cells.[colIdx] else CompositeCell.FreeText ""
                match header with
                | CompositeHeader.Parameter(n, _) ->
                    match p.TryGetParameterValue(n) with
                    | Some pv -> TableAux.ApplyCellToPV(pv, cell)
                    | None    ->
                        let pv = PropertyValue(n)
                        pv.AdditionalType <- Some "ParameterValue"
                        TableAux.ApplyCellToPV(pv, cell)
                        p.AddParameterValue(pv)
                | CompositeHeader.Input _ ->
                    match cell, p.Inputs |> Seq.tryHead with
                    | CompositeCell.FreeText n, Some (MaterialNode m) -> m.Name <- n
                    | CompositeCell.FreeText n, Some (DataNode d)     -> d.Path <- n
                    | CompositeCell.Data d2,    Some (DataNode d)     ->
                        d.Path           <- d2.Path
                        d.Selector       <- d2.Selector
                        d.SelectorFormat <- d2.SelectorFormat
                        d.EncodingFormat <- d2.EncodingFormat
                    | _ -> ()
                | CompositeHeader.Output _ ->
                    match cell, p.Outputs |> Seq.tryHead with
                    | CompositeCell.FreeText n, Some (MaterialNode m) -> m.Name <- n
                    | CompositeCell.FreeText n, Some (DataNode d)     -> d.Path <- n
                    | CompositeCell.Data d2,    Some (DataNode d)     ->
                        d.Path           <- d2.Path
                        d.Selector       <- d2.Selector
                        d.SelectorFormat <- d2.SelectorFormat
                        d.EncodingFormat <- d2.EncodingFormat
                    | _ -> ()
                | CompositeHeader.Characteristic(n, _) ->
                    let pvList =
                        match p.Inputs |> Seq.tryHead with
                        | Some (MaterialNode m) -> Some m.AdditionalProperty
                        | Some (DataNode d)     -> Some d.AdditionalProperty
                        | None                  -> None
                    match pvList with
                    | Some lst ->
                        match lst |> Seq.tryFind (fun pv -> pv.AdditionalType = Some "CharacteristicValue" && pv.Name = n) with
                        | Some pv -> TableAux.ApplyCellToPV(pv, cell)
                        | None    ->
                            let pv = PropertyValue(n)
                            pv.AdditionalType <- Some "CharacteristicValue"
                            TableAux.ApplyCellToPV(pv, cell)
                            lst.Add(pv)
                    | None -> ()
                | CompositeHeader.Factor(n, _) ->
                    let pvList =
                        match p.Outputs |> Seq.tryHead with
                        | Some (MaterialNode m) -> Some m.AdditionalProperty
                        | Some (DataNode d)     -> Some d.AdditionalProperty
                        | None                  -> None
                    match pvList with
                    | Some lst ->
                        match lst |> Seq.tryFind (fun pv -> pv.AdditionalType = Some "FactorValue" && pv.Name = n) with
                        | Some pv -> TableAux.ApplyCellToPV(pv, cell)
                        | None    ->
                            let pv = PropertyValue(n)
                            pv.AdditionalType <- Some "FactorValue"
                            TableAux.ApplyCellToPV(pv, cell)
                            lst.Add(pv)
                    | None -> ()
                | CompositeHeader.Component(n, _) ->
                    match p.ExecutesProtocol with
                    | Some proto ->
                        match proto.LabEquipment |> Seq.tryFind (fun pv -> pv.Name = n) with
                        | Some pv -> TableAux.ApplyCellToPV(pv, cell)
                        | None    ->
                            let pv = PropertyValue(n)
                            pv.AdditionalType <- Some "Component"
                            TableAux.ApplyCellToPV(pv, cell)
                            proto.AddLabEquipment(pv)
                    | None -> ()
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
        let groups = System.Collections.Generic.Dictionary<string, ResizeArray<LabProcess>>()
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
