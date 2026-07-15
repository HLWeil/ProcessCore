module ProcessCore.Tests.Table.TableRead

open Fable.Pyxpecto
open ProcessCore
open ProcessCore.Table

// ─── helpers ─────────────────────────────────────────────────────────────────

/// Build a minimal single-process table: Source1 --[proc]--> Sample1
let makeSingleProcessTable () =
    let source = Sample("Source1", additionalType = "Source")
    let sample = Sample("Sample1", additionalType = "Sample")
    let proc   = Process("Growth")
    proc.SetInputSample(source)
    proc.SetOutputSample(sample)
    let ds = Dataset("DS")
    ds.AddProcess(proc)
    Table("Growth", ResizeArray([| proc |]), ds), proc, ds

let freeTextValue (cell : CompositeCell) =
    match cell with
    | CompositeCell.FreeText value -> value
    | other -> failwithf "Expected FreeText cell but got %A" other

let tests = testList "TableRead" [

    // ── empty process list ────────────────────────────────────────────────────

    testCase "empty process list → empty columns" <| fun _ ->
        let ds = Dataset("DS")
        let t  = Table("empty", ResizeArray(), ds)
        Expect.equal (t.Decompose().Count) 0 "no columns for empty table"

    // ── single process — basic column presence ────────────────────────────────

    testCase "single process — input column first" <| fun _ ->
        let t, _, _ = makeSingleProcessTable()
        let cols = t.Decompose()
        match cols.[0].Header with
        | CompositeHeader.Input _ -> ()
        | h -> failwithf "Expected Input but got %A" h

    testCase "single process — output column last" <| fun _ ->
        let t, _, _ = makeSingleProcessTable()
        let cols = t.Decompose()
        match cols.[cols.Count - 1].Header with
        | CompositeHeader.Output _ -> ()
        | h -> failwithf "Expected Output but got %A" h

    testCase "single process — protocol ref column present when protocol set" <| fun _ ->
        let t, proc, _ = makeSingleProcessTable()
        proc.ExecutesProtocol <- Some (Recipe("myProtocol"))
        let headers = t.Headers
        let hasRef  = headers |> Seq.exists (fun h -> h = CompositeHeader.ProtocolREF)
        Expect.isTrue hasRef "ProtocolREF column present"

    testCase "single process — parameter column present" <| fun _ ->
        let t, proc, _ = makeSingleProcessTable()
        let temp = DefinedTerm("temperature")
        proc.AddParameterValue(Annotation("temperature", value = "37", unit = "°C", additionalType = "ParameterValue"))
        let headers = t.Headers
        let hasParam = headers |> Seq.exists (fun h -> match h with | CompositeHeader.Parameter(dt) when dt.Name = "temperature" -> true | _ -> false)
        Expect.isTrue hasParam "Parameter column for temperature"

    testCase "single process — characteristic column present" <| fun _ ->
        let t, proc, _ = makeSingleProcessTable()
        match proc.Input with
        | Some (SampleNode m) -> m.AddAdditionalProperty(Annotation("organism", value = "Mouse", additionalType = "CharacteristicValue"))
        | _ -> ()
        let headers = t.Headers
        let hasChar = headers |> Seq.exists (fun h -> match h with | CompositeHeader.Characteristic(dt) when dt.Name = "organism" -> true | _ -> false)
        Expect.isTrue hasChar "Characteristic column for organism"

    testCase "single process — factor column present" <| fun _ ->
        let t, proc, _ = makeSingleProcessTable()
        match proc.Output with
        | Some (SampleNode m) -> m.AddAdditionalProperty(Annotation("growth_phase", value = "log", additionalType = "FactorValue"))
        | _ -> ()
        let headers = t.Headers
        let hasFactor = headers |> Seq.exists (fun h -> match h with | CompositeHeader.Factor(dt) when dt.Name = "growth_phase" -> true | _ -> false)
        Expect.isTrue hasFactor "Factor column for growth_phase"

    testCase "single process — component column present" <| fun _ ->
        let t, proc, _ = makeSingleProcessTable()
        let proto = Recipe("extraction")
        proto.AddComponent(Annotation("instrument", value = "Orbitrap", additionalType = "Component"))
        proc.ExecutesProtocol <- Some proto
        let headers = t.Headers
        let hasComp = headers |> Seq.exists (fun h -> match h with | CompositeHeader.Component(dt) when dt.Name = "instrument" -> true | _ -> false)
        Expect.isTrue hasComp "Component column for instrument"

    testCase "column order: Input → ProtocolREF → Characteristic → Component → Parameter → Factor → Output" <| fun _ ->
        let source = Sample("Source1", additionalType = "Source")
        source.AddAdditionalProperty(Annotation("organism", value = "Mouse", additionalType = "CharacteristicValue"))
        let sample = Sample("Sample1", additionalType = "Sample")
        sample.AddAdditionalProperty(Annotation("growth_phase", value = "log", additionalType = "FactorValue"))
        let proto = Recipe("extraction")
        proto.AddComponent(Annotation("instrument", value = "Orbitrap", additionalType = "Component"))
        let proc = Process("Growth")
        proc.SetInputSample(source)
        proc.SetOutputSample(sample)
        proc.ExecutesProtocol <- Some proto
        proc.AddParameterValue(Annotation("temperature", value = "37", unit = "°C", additionalType = "ParameterValue"))
        let ds = Dataset("DS")
        ds.AddProcess(proc)
        let t = Table("Growth", ResizeArray([| proc |]), ds)
        let headers = t.Headers |> Seq.toList
        let findIdx pred = headers |> List.tryFindIndex pred
        let inputIdx  = findIdx (fun h -> match h with CompositeHeader.Input _ -> true | _ -> false)
        let refIdx    = findIdx (fun h -> h = CompositeHeader.ProtocolREF)
        let charIdx   = findIdx (fun h -> match h with CompositeHeader.Characteristic _ -> true | _ -> false)
        let compIdx   = findIdx (fun h -> match h with CompositeHeader.Component _ -> true | _ -> false)
        let paramIdx  = findIdx (fun h -> match h with CompositeHeader.Parameter _ -> true | _ -> false)
        let factorIdx = findIdx (fun h -> match h with CompositeHeader.Factor _ -> true | _ -> false)
        let outputIdx = findIdx (fun h -> match h with CompositeHeader.Output _ -> true | _ -> false)
        Expect.isTrue (inputIdx  < refIdx)    "Input before ProtocolREF"
        Expect.isTrue (refIdx    < charIdx)   "ProtocolREF before Characteristic"
        Expect.isTrue (charIdx   < compIdx)   "Characteristic before Component"
        Expect.isTrue (compIdx   < paramIdx)  "Component before Parameter"
        Expect.isTrue (paramIdx  < factorIdx) "Parameter before Factor"
        Expect.isTrue (factorIdx < outputIdx) "Factor before Output"

    // ── multiple rows ─────────────────────────────────────────────────────────

    testCase "multiple rows — RowCount and ColumnCount" <| fun _ ->
        let mk name =
            let s = Sample(name + "_in",  additionalType = "Source")
            let o = Sample(name + "_out", additionalType = "Sample")
            let p = Process("Growth")
            p.SetInputSample(s)
            p.SetOutputSample(o)
            p.AddParameterValue(Annotation("temperature", value = "37", unit = "°C", additionalType = "ParameterValue"))
            p
        let p1 = mk "A"
        let p2 = mk "B"
        let ds = Dataset("DS")
        ds.AddProcess(p1)
        ds.AddProcess(p2)
        let t = Table("Growth", ResizeArray([| p1; p2 |]), ds)
        Expect.equal t.RowCount 2 "2 rows"
        // Input + ProtocolREF-absent (no protocol) + Parameter + Output = 3 columns
        Expect.equal t.ColumnCount 3 "3 columns: Input, Parameter, Output"

    // ── data output ───────────────────────────────────────────────────────────

    testCase "data output — Output column typed as Data" <| fun _ ->
        let source = Sample("Source1", additionalType = "Source")
        let raw    = Data("rawData1.csv")
        let proc   = Process("Measurement")
        proc.SetInputSample(source)
        proc.SetOutputData(raw)
        let ds = Dataset("DS")
        ds.AddProcess(proc)
        let t = Table("Measurement", ResizeArray([| proc |]), ds)
        match t.TryGetOutputColumn() with
        | Some col ->
            match col.Header with
            | CompositeHeader.Output IOType.Data -> ()
            | h -> failwithf "Expected Output(Data) but got %A" h
        | None -> failwith "No output column"

    testCase "data output — cell is CompositeCell.Data" <| fun _ ->
        let source = Sample("Source1", additionalType = "Source")
        let raw    = Data("rawData1.csv")
        let proc   = Process("Measurement")
        proc.SetInputSample(source)
        proc.SetOutputData(raw)
        let ds = Dataset("DS")
        ds.AddProcess(proc)
        let t = Table("Measurement", ResizeArray([| proc |]), ds)
        let col = t.TryGetOutputColumn().Value
        match col.Cells.[0] with
        | CompositeCell.Data d -> Expect.equal d.Path "rawData1.csv" "data path"
        | _ -> failwith "Expected CompositeCell.Data"

    // ── Headers / ColumnCount / RowCount ──────────────────────────────────────

    testCase "Headers derives from Decompose" <| fun _ ->
        let t, proc, _ = makeSingleProcessTable()
        proc.AddParameterValue(Annotation("rpm", value = "200", unit = "rpm", additionalType = "ParameterValue"))
        let fromDecompose = t.Decompose() |> Seq.map (fun c -> c.Header) |> Seq.toList
        let fromHeaders   = t.Headers |> Seq.toList
        Expect.equal fromHeaders fromDecompose "Headers matches Decompose"

    // ── GetColumn ─────────────────────────────────────────────────────────────

    testCase "GetColumn by index" <| fun _ ->
        let t, _, _ = makeSingleProcessTable()
        let col = t.GetColumn(0)
        match col.Header with
        | CompositeHeader.Input _ -> ()
        | h -> failwithf "Expected Input at index 0 but got %A" h

    // ── TryGetColumnByHeader ──────────────────────────────────────────────────

    testCase "TryGetColumnByHeader — found" <| fun _ ->
        let t, proc, _ = makeSingleProcessTable()
        proc.AddParameterValue(Annotation("temperature", value = "37", unit = "°C", additionalType = "ParameterValue"))
        let result = t.TryGetColumnByHeader(fun h -> match h with CompositeHeader.Parameter(dt) when dt.Name = "temperature" -> true | _ -> false)
        Expect.isSome result "Parameter column found"

    testCase "TryGetColumnByHeader — not found" <| fun _ ->
        let t, _, _ = makeSingleProcessTable()
        let result = t.TryGetColumnByHeader(fun h -> match h with CompositeHeader.Factor _ -> true | _ -> false)
        Expect.isNone result "Factor column not present"

    // ── TryGetInputColumn / TryGetOutputColumn ────────────────────────────────

    testCase "TryGetInputColumn" <| fun _ ->
        let t, _, _ = makeSingleProcessTable()
        let col = t.TryGetInputColumn()
        Expect.isSome col "Input column present"

    testCase "TryGetOutputColumn" <| fun _ ->
        let t, _, _ = makeSingleProcessTable()
        let col = t.TryGetOutputColumn()
        Expect.isSome col "Output column present"

    // ── GetComponentColumns ───────────────────────────────────────────────────

    testCase "GetComponentColumns" <| fun _ ->
        let t, proc, _ = makeSingleProcessTable()
        let proto = Recipe("proto")
        proto.AddComponent(Annotation("instrument", value = "Orbitrap", additionalType = "Component"))
        proc.ExecutesProtocol <- Some proto
        let compCols = t.GetComponentColumns()
        Expect.equal compCols.Count 1 "one component column"
        match compCols.[0].Header with
        | CompositeHeader.Component(dt) when dt.Name = "instrument" -> ()
        | h -> failwithf "Expected Component(instrument) but got %A" h

    // ── GetCellAt / TryGetCellAt ──────────────────────────────────────────────

    testCase "GetCellAt" <| fun _ ->
        let t, _, _ = makeSingleProcessTable()
        let cell = t.GetCellAt(0, 0)  // column 0 = Input, row 0
        match cell with
        | CompositeCell.FreeText "Source1" -> ()
        | other -> failwithf "Expected FreeText Source1 but got %A" other

    testCase "TryGetCellAt — in range" <| fun _ ->
        let t, _, _ = makeSingleProcessTable()
        let cell = t.TryGetCellAt(0, 0)
        Expect.isSome cell "cell in range"

    testCase "TryGetCellAt — column out of range" <| fun _ ->
        let t, _, _ = makeSingleProcessTable()
        Expect.isNone (t.TryGetCellAt(99, 0)) "out of range column → None"

    testCase "TryGetCellAt — row out of range" <| fun _ ->
        let t, _, _ = makeSingleProcessTable()
        Expect.isNone (t.TryGetCellAt(0, 99)) "out of range row → None"

    // ── GetRow ────────────────────────────────────────────────────────────────

    testCase "GetRow returns one cell per column" <| fun _ ->
        let t, _, _ = makeSingleProcessTable()
        let row = t.GetRow(0)
        Expect.equal row.Count t.ColumnCount "one cell per column"



]
