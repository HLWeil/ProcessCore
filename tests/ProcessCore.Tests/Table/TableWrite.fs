module ProcessCore.Tests.Table.TableWrite

open Fable.Pyxpecto
open ProcessCore
open ProcessCore.Table

// ─── helpers ─────────────────────────────────────────────────────────────────

/// Build a minimal single-process table: Source1 --[proc]--> Sample1, with a protocol
let makeBaseTable () =
    let source = Sample("Source1", additionalType = "Source")
    let sample = Sample("Sample1", additionalType = "Sample")
    let proto  = Plan("extraction")
    let proc   = Process("Growth")
    proc.AddInputSample(source)
    proc.AddOutputSample(sample)
    proc.ExecutesProtocol <- Some proto
    let ds = Dataset("DS")
    ds.AddProcess(proc)
    Table("Growth", ResizeArray([| proc |]), ds), proc, ds

let makeTable name processes =
    let ds = Dataset("DS")
    for p in processes do
        ds.AddProcess(p)
    Table(name, ResizeArray(processes), ds), ds

let tests = testList "TableWrite" [

    // ═════════════════════════════════════════════════════════════════════════
    // Column write API
    // ═════════════════════════════════════════════════════════════════════════

    testList "AddColumn" [

        testCase "AddColumn — Parameter appears in Decompose" <| fun _ ->
            let t, _, _ = makeBaseTable()
            let rpm = DefinedTerm("rpm")
            t.AddColumn(CompositeHeader.Parameter(rpm),
                        ResizeArray([| CompositeCell.Unitized("200", "rpm", None) |]))
            let hasParam = t.Headers |> Seq.exists (fun h -> match h with CompositeHeader.Parameter(dt) when dt.Name = "rpm" -> true | _ -> false)
            Expect.isTrue hasParam "Parameter column added"

        testCase "AddColumn — Parameter stored in ParameterValue list" <| fun _ ->
            let t, proc, _ = makeBaseTable()
            let rpm = DefinedTerm("rpm")
            t.AddColumn(CompositeHeader.Parameter(rpm),
                        ResizeArray([| CompositeCell.Unitized("200", "rpm", None) |]))
            let pv = proc.ParameterValue |> Seq.tryFind (fun pv -> pv.Name = "rpm")
            Expect.isSome pv "PV added to process.ParameterValue"

        testCase "AddColumn — Characteristic stored on input sample" <| fun _ ->
            let t, proc, _ = makeBaseTable()
            let organism = DefinedTerm("organism")
            t.AddColumn(CompositeHeader.Characteristic(organism),
                        ResizeArray([| CompositeCell.FreeText "E. coli" |]))
            match proc.Inputs |> Seq.tryHead with
            | Some (SampleNode m) ->
                let pv = m.AdditionalProperty |> Seq.tryFind (fun p -> p.Name = "organism")
                Expect.isSome pv "Characteristic PV on input sample"
            | _ -> failwith "No input sample"

        testCase "AddColumn — Factor stored on output sample" <| fun _ ->
            let t, proc, _ = makeBaseTable()
            let growthPhase = DefinedTerm("growth_phase")
            t.AddColumn(CompositeHeader.Factor(growthPhase),
                        ResizeArray([| CompositeCell.FreeText "log" |]))
            match proc.Outputs |> Seq.tryHead with
            | Some (SampleNode m) ->
                let pv = m.AdditionalProperty |> Seq.tryFind (fun p -> p.Name = "growth_phase")
                Expect.isSome pv "Factor PV on output sample"
            | _ -> failwith "No output sample"

        testCase "AddColumn — Component stored on protocol.LabEquipment" <| fun _ ->
            let t, proc, _ = makeBaseTable()
            let instrument = DefinedTerm("instrument")
            t.AddColumn(CompositeHeader.Component(instrument),
                        ResizeArray([| CompositeCell.FreeText "Orbitrap" |]))
            match proc.ExecutesProtocol with
            | Some proto ->
                let pv = proto.LabEquipment |> Seq.tryFind (fun p -> p.Name = "instrument")
                Expect.isSome pv "Component PV on protocol.LabEquipment"
            | None -> failwith "No protocol"

        testCase "AddColumn — non-annotation header is no-op (ProtocolREF)" <| fun _ ->
            let t, proc, _ = makeBaseTable()
            let colsBefore = t.ColumnCount
            t.AddColumn(CompositeHeader.ProtocolREF)
            Expect.equal t.ColumnCount colsBefore "ProtocolREF AddColumn is a no-op"

        testCase "AddColumn — fewer cells than rows uses FreeText empty for missing" <| fun _ ->
            // Two processes, but only supply one cell
            let s1 = Sample("S1", additionalType = "Source")
            let o1 = Sample("O1", additionalType = "Sample")
            let s2 = Sample("S2", additionalType = "Source")
            let o2 = Sample("O2", additionalType = "Sample")
            let p1 = Process("T")
            p1.AddInputSample(s1) ; p1.AddOutputSample(o1)
            let p2 = Process("T")
            p2.AddInputSample(s2) ; p2.AddOutputSample(o2)
            let ds = Dataset("DS")
            ds.AddProcess(p1) ; ds.AddProcess(p2)
            let t = Table("T", ResizeArray([| p1; p2 |]), ds)
            let temp = DefinedTerm("temperature")
            t.AddColumn(CompositeHeader.Parameter(temp),
                        ResizeArray([| CompositeCell.FreeText "37" |]))
            // p2 should still get an empty PV
            Expect.equal (p2.ParameterValue.Count) 1 "p2 gets a PV (with empty value)"
            Expect.equal p2.ParameterValue.[0].Value (Some "") "p2 PV value is empty"

    ]

    testList "RemoveColumn" [

        testCase "RemoveColumn — removes Parameter" <| fun _ ->
            let t, proc, _ = makeBaseTable()
            let rpm = DefinedTerm("rpm")
            proc.AddParameterValue(Annotation("rpm", value = "200", unit = "rpm", additionalType = "ParameterValue"))
            t.RemoveColumn(CompositeHeader.Parameter(rpm))
            let hasPV = proc.ParameterValue |> Seq.exists (fun pv -> pv.Name = "rpm")
            Expect.isFalse hasPV "Parameter PV removed"

        testCase "RemoveColumn — removes Characteristic from input" <| fun _ ->
            let t, proc, _ = makeBaseTable()
            let organism = DefinedTerm("organism")
            match proc.Inputs |> Seq.tryHead with
            | Some (SampleNode m) ->
                m.AddAdditionalProperty(Annotation("organism", value = "Mouse", additionalType = "CharacteristicValue"))
            | _ -> ()
            t.RemoveColumn(CompositeHeader.Characteristic(organism))
            let hasPV =
                match proc.Inputs |> Seq.tryHead with
                | Some (SampleNode m) -> m.AdditionalProperty |> Seq.exists (fun p -> p.Name = "organism")
                | _ -> false
            Expect.isFalse hasPV "Characteristic PV removed"

        testCase "RemoveColumn — removes Factor from output" <| fun _ ->
            let t, proc, _ = makeBaseTable()
            let growthPhase = DefinedTerm("growth_phase")
            match proc.Outputs |> Seq.tryHead with
            | Some (SampleNode m) ->
                m.AddAdditionalProperty(Annotation("growth_phase", value = "log", additionalType = "FactorValue"))
            | _ -> ()
            t.RemoveColumn(CompositeHeader.Factor(growthPhase))
            let hasPV =
                match proc.Outputs |> Seq.tryHead with
                | Some (SampleNode m) -> m.AdditionalProperty |> Seq.exists (fun p -> p.Name = "growth_phase")
                | _ -> false
            Expect.isFalse hasPV "Factor PV removed"

        testCase "RemoveColumn — removes Component from protocol" <| fun _ ->
            let t, proc, _ = makeBaseTable()
            match proc.ExecutesProtocol with
            | Some proto ->
                proto.AddLabEquipment(Annotation("instrument", value = "Orbitrap", additionalType = "Component"))
            | None -> ()
            let instrument = DefinedTerm("instrument")
            t.RemoveColumn(CompositeHeader.Component(instrument))
            let hasPV =
                match proc.ExecutesProtocol with
                | Some proto -> proto.LabEquipment |> Seq.exists (fun p -> p.Name = "instrument")
                | None -> false
            Expect.isFalse hasPV "Component PV removed"

        testCase "RemoveColumn — non-annotation header is no-op" <| fun _ ->
            let t, proc, _ = makeBaseTable()
            let before = t.ColumnCount
            t.RemoveColumn(CompositeHeader.ProtocolREF)
            Expect.equal t.ColumnCount before "ProtocolREF RemoveColumn is a no-op"

    ]

    // ═════════════════════════════════════════════════════════════════════════
    // Row write API
    // ═════════════════════════════════════════════════════════════════════════

    testList "AddRow" [

        testCase "AddRow — new process in Table.Processes list" <| fun _ ->
            // AddRow always appends to the table's own process list,
            // even though dataset.AddProcess deduplicates by name.
            let t, _, _ = makeBaseTable()
            Expect.equal (t.Processes.Count) 1 "before: 1"
            t.AddRow()
            Expect.equal (t.Processes.Count) 2 "Table.Processes has 2 after AddRow"

        testCase "AddRow — RowCount increases" <| fun _ ->
            let t, _, _ = makeBaseTable()
            let before = t.RowCount
            t.AddRow()
            Expect.equal t.RowCount (before + 1) "RowCount +1"

        testCase "AddRow — input cell sets sample name" <| fun _ ->
            let t, proc, _ = makeBaseTable()
            let cells = ResizeArray([| CompositeCell.FreeText "Source2"; CompositeCell.FreeText "ref"; CompositeCell.FreeText "Sample2" |])
            t.AddRow(cells = cells)
            let newProc = t.Processes.[1]
            match newProc.Inputs |> Seq.tryHead with
            | Some (SampleNode m) -> Expect.equal m.Name "Source2" "input name set"
            | _ -> failwith "expected input SampleNode"

        testCase "AddRow — output cell sets sample name" <| fun _ ->
            let t, proc, _ = makeBaseTable()
            let cols = t.Headers
            // Supply enough cells to cover columns. Last cell = output.
            let emptyCells = ResizeArray(Seq.init cols.Count (fun _ -> CompositeCell.FreeText ""))
            emptyCells.[emptyCells.Count - 1] <- CompositeCell.FreeText "Sample2_out"
            t.AddRow(cells = emptyCells)
            let newProc = t.Processes.[1]
            match newProc.Outputs |> Seq.tryHead with
            | Some (SampleNode m) -> Expect.equal m.Name "Sample2_out" "output name set"
            | _ -> failwith "expected output SampleNode"

        testCase "AddRow — Data cell creates DataNode input" <| fun _ ->
            let source = Sample("Source1", additionalType = "Source")
            let raw    = Data("raw.csv")
            let proc   = Process("M")
            proc.AddInputSample(source)
            proc.AddOutputData(raw)
            let ds = Dataset("DS")
            ds.AddProcess(proc)
            let t  = Table("M", ResizeArray([| proc |]), ds)
            // The output column is IOType.Data; supply a Data cell
            let headers = t.Headers
            let cells   = ResizeArray(Seq.init headers.Count (fun _ -> CompositeCell.FreeText ""))
            let outIdx  = headers |> Seq.findIndex (fun h -> match h with CompositeHeader.Output _ -> true | _ -> false)
            cells.[outIdx] <- CompositeCell.Data(Data("out2.csv"))
            t.AddRow(cells = cells)
            let newProc = t.Processes.[1]
            match newProc.Outputs |> Seq.tryHead with
            | Some (DataNode d) -> Expect.equal d.Path "out2.csv" "output DataNode path"
            | _ -> failwith "expected DataNode"

        testCase "AddRow — ProtocolREF cell sets protocol name" <| fun _ ->
            let t, _, _ = makeBaseTable()
            let headers = t.Headers
            let cells   = ResizeArray(Seq.init headers.Count (fun _ -> CompositeCell.FreeText ""))
            let refIdx  = headers |> Seq.findIndex (fun h -> h = CompositeHeader.ProtocolREF)
            cells.[refIdx] <- CompositeCell.FreeText "newProtocol"
            t.AddRow(cells = cells)
            let newProc = t.Processes.[1]
            match newProc.ExecutesProtocol with
            | Some proto ->
                // Protocol was cloned, then ProtocolREF cell overrides Name
                Expect.equal proto.Name (Some "newProtocol") "protocol name set from ProtocolREF cell"
            | None -> failwith "expected protocol"

        testCase "AddRow — protocol cloned from first row" <| fun _ ->
            let t, _, _ = makeBaseTable()
            // Supply an explicit ProtocolREF cell so the cloned name is not overwritten
            let headers = t.Headers
            let cells   = ResizeArray(Seq.init headers.Count (fun _ -> CompositeCell.FreeText ""))
            let refIdx  = headers |> Seq.findIndex (fun h -> h = CompositeHeader.ProtocolREF)
            cells.[refIdx] <- CompositeCell.FreeText "extraction"
            t.AddRow(cells = cells)
            let newProc = t.Processes.[1]
            match newProc.ExecutesProtocol with
            | Some proto -> Expect.equal proto.Name (Some "extraction") "protocol name cloned"
            | None       -> failwith "protocol not cloned"

        testCase "AddRow at index inserts at correct position" <| fun _ ->
            let s1 = Sample("S1", additionalType = "Source")
            let o1 = Sample("O1", additionalType = "Sample")
            let s2 = Sample("S2", additionalType = "Source")
            let o2 = Sample("O2", additionalType = "Sample")
            let p1 = Process("T")
            p1.AddInputSample(s1)
            p1.AddOutputSample(o1)
            let p2 = Process("T")
            p2.AddInputSample(s2)
            p2.AddOutputSample(o2)
            let ds = Dataset("DS")
            ds.AddProcess(p1)
            ds.AddProcess(p2)
            let t  = Table("T", ResizeArray([| p1; p2 |]), ds)
            let headers = t.Headers
            let cells   = ResizeArray(Seq.init headers.Count (fun _ -> CompositeCell.FreeText ""))
            let inIdx   = headers |> Seq.findIndex (fun h -> match h with CompositeHeader.Input _ -> true | _ -> false)
            cells.[inIdx] <- CompositeCell.FreeText "SInserted"
            t.AddRow(cells = cells, index = 1)
            Expect.equal t.RowCount 3 "3 rows after insert"
            match t.Processes.[1].Inputs |> Seq.tryHead with
            | Some (SampleNode m) -> Expect.equal m.Name "SInserted" "inserted at correct position"
            | _ -> failwith "expected SampleNode at index 1"

    ]

    testList "AppendRow" [

        testCase "AppendRow increases RowCount" <| fun _ ->
            let t, _, _ = makeBaseTable()
            t.AppendRow()
            Expect.equal t.RowCount 2 "RowCount = 2 after AppendRow"

    ]

    testList "RemoveRow" [

        testCase "RemoveRow removes process from table and dataset" <| fun _ ->
            // Use a single-row table so we can verify both table and dataset are emptied
            let t, proc, ds = makeBaseTable()
            t.RemoveRow(0)
            Expect.equal t.RowCount 0                         "RowCount after remove"
            Expect.equal ds.Processes.Count 0                 "dataset process count"
            Expect.isFalse (ds.Processes.Contains(proc))      "proc removed from dataset"

        testCase "RemoveRow out-of-range is a no-op" <| fun _ ->
            let t, _, ds = makeBaseTable()
            t.RemoveRow(99)
            Expect.equal t.RowCount 1 "RowCount unchanged"
            Expect.equal ds.Processes.Count 1 "dataset unchanged"

    ]

    testList "UpdateRow" [

        testCase "UpdateRow — updates input name" <| fun _ ->
            let t, proc, _ = makeBaseTable()
            let headers = t.Headers
            let cells   = ResizeArray(Seq.init headers.Count (fun _ -> CompositeCell.FreeText ""))
            let inIdx   = headers |> Seq.findIndex (fun h -> match h with CompositeHeader.Input _ -> true | _ -> false)
            cells.[inIdx] <- CompositeCell.FreeText "UpdatedSource"
            t.UpdateRow(0, cells)
            match proc.Inputs |> Seq.tryHead with
            | Some (SampleNode m) -> Expect.equal m.Name "UpdatedSource" "input name updated"
            | _ -> failwith "expected SampleNode"

        testCase "UpdateRow — updates existing PV value" <| fun _ ->
            let t, proc, _ = makeBaseTable()
            proc.AddParameterValue(Annotation("rpm", value = "200", unit = "rpm", additionalType = "ParameterValue"))
            let headers = t.Headers
            let cells   = ResizeArray(Seq.init headers.Count (fun _ -> CompositeCell.FreeText ""))
            let paramIdx = headers |> Seq.findIndex (fun h -> match h with CompositeHeader.Parameter(dt) when dt.Name = "rpm" -> true | _ -> false)
            cells.[paramIdx] <- CompositeCell.Unitized("300", "rpm", None)
            t.UpdateRow(0, cells)
            let pv = proc.ParameterValue |> Seq.find (fun pv -> pv.Name = "rpm")
            Expect.equal pv.Value (Some "300") "PV value updated"

        testCase "UpdateRow — adds new PV if not present" <| fun _ ->
            // p1 has "rpm"; p2 does not.
            // Table holds both in its processes list so "rpm" column appears in headers.
            // UpdateRow(1, ...) should add the rpm PV to p2.
            let s1 = Sample("S1", additionalType = "Source")
            let o1 = Sample("O1", additionalType = "Sample")
            let s2 = Sample("S2", additionalType = "Source")
            let o2 = Sample("O2", additionalType = "Sample")
            let p1 = Process("T")
            p1.AddInputSample(s1)
            p1.AddOutputSample(o1)
            p1.AddParameterValue(Annotation("rpm", value = "200", unit = "rpm", additionalType = "ParameterValue"))
            let p2 = Process("T")
            p2.AddInputSample(s2)
            p2.AddOutputSample(o2)
            // p2 has no rpm PV
            let ds = Dataset("DS")
            ds.AddProcess(p1)
            // Construct the table directly with both process references
            let t = Table("T", ResizeArray([| p1; p2 |]), ds)
            // Headers derived from both rows → includes Parameter("rpm")
            let headers  = t.Headers
            let cells    = ResizeArray(Seq.init headers.Count (fun _ -> CompositeCell.FreeText ""))
            let paramIdx = headers |> Seq.findIndex (fun h -> match h with CompositeHeader.Parameter(dt) when dt.Name = "rpm" -> true | _ -> false)
            cells.[paramIdx] <- CompositeCell.Unitized("400", "rpm", None)
            t.UpdateRow(1, cells)
            let pv = p2.ParameterValue |> Seq.tryFind (fun pv -> pv.Name = "rpm")
            Expect.isSome pv "PV added to p2"
            Expect.equal pv.Value.Value (Some "400") "PV value set"

    ]

    testList "Writable input/output columns" [

        testCase "AddColumn Input creates missing process" <| fun _ ->
            let t, ds = makeTable "T" [||] // no processes
            t.AddColumn(CompositeHeader.Input IOType.Sample,
                        ResizeArray([| CompositeCell.FreeText "Source1"; CompositeCell.FreeText "Source2" |]))
            Expect.equal ds.Processes.Count 1 "one process created"
            Expect.equal ds.Processes[0].Inputs.Count 2 " process gets an input"
            Expect.isTrue ds.Processes[0].Inputs[0].IsSampleNode "process input is SampleNode"
            Expect.equal (ds.Processes[0].Inputs[0].AsSample().Name) "Source1" "input name set from cell"

            Expect.equal t.RowCount 2 "two rows in table"

        // One process caries one value for each parameter. If we add a column with different values for the same parameter, we need to split the process into two (or more) so that each process has only one value for that parameter.
        testCase "AddColumn Parameter differing value leads to splitting processes" <| fun _ ->
            let p = Process("T")
            p.AddInputSample(Sample("S1"))
            p.AddInputSample(Sample("S2"))
            p.AddParameterValue(Annotation(name = "organism", value = "Arabidopsis"))

            let ds = Dataset("DS")
            ds.AddProcess(p)
            // confirm that table now contains two rows for p (since it has two inputs)
            let t = Table("T", ResizeArray([| p |]), ds)
            Expect.equal t.RowCount 2 "two rows for process with two inputs"

            t.AddColumn(
                CompositeHeader.Parameter(DefinedTerm("temperature")),
                ResizeArray([| CompositeCell.Unitized("37", "°C", None); CompositeCell.Unitized("25", "°C", None) |])
            )

            Expect.equal ds.Processes.Count 2 "process was split into two"

            Expect.equal ds.Processes[0].ParameterValue.Count 2 "first process has two PVs"
            Expect.equal ds.Processes[0].ParameterValue[0].Name "organism" "first PV is organism"
            Expect.equal ds.Processes[0].ParameterValue[0].Value (Some "Arabidopsis") "first PV value"
            Expect.equal ds.Processes[0].ParameterValue[1].Name "temperature" "second PV is temperature"
            Expect.equal ds.Processes[0].ParameterValue[1].Value (Some "37") "second PV value"

            Expect.equal ds.Processes[1].ParameterValue.Count 2 "second process has two PVs"
            Expect.equal ds.Processes[1].ParameterValue[0].Name "organism" "first PV is organism"
            Expect.equal ds.Processes[1].ParameterValue[0].Value (Some "Arabidopsis") "first PV value"
            Expect.equal ds.Processes[1].ParameterValue[1].Name "temperature" "second PV is temperature"
            Expect.equal ds.Processes[1].ParameterValue[1].Value (Some "25") "second PV value"


        testCase "AddColumn Input creates missing sample inputs from cells" <| fun _ ->
            let p1 = Process("Import")
            let p2 = Process("Import")
            let t, _ = makeTable "Import" [| p1; p2 |]

            t.AddColumn(
                CompositeHeader.Input IOType.Source,
                ResizeArray([| CompositeCell.FreeText "Source1"; CompositeCell.FreeText "Source2" |])
            )

            Expect.equal p1.Inputs.Count 1 "first process gets an input"
            Expect.equal p2.Inputs.Count 1 "second process gets an input"
            match p1.Inputs.[0], p2.Inputs.[0] with
            | SampleNode m1, SampleNode m2 ->
                Expect.equal m1.Name "Source1" "first input name"
                Expect.equal m1.AdditionalType (Some "Source") "first input type"
                Expect.equal m2.Name "Source2" "second input name"
            | other -> failwithf "Expected sample inputs but got %A" other

        testCase "AddColumn Output creates missing data outputs from mixed cell shapes" <| fun _ ->
            let p1 = Process("Export")
            let p2 = Process("Export")
            let t, _ = makeTable "Export" [| p1; p2 |]

            t.AddColumn(
                CompositeHeader.Output IOType.Data,
                ResizeArray([| CompositeCell.Data(Data("raw1.csv")); CompositeCell.FreeText "raw2.csv" |])
            )

            Expect.equal p1.Outputs.Count 1 "first process gets an output"
            Expect.equal p2.Outputs.Count 1 "second process gets an output"
            match p1.Outputs.[0], p2.Outputs.[0] with
            | DataNode d1, DataNode d2 ->
                Expect.equal d1.Path "raw1.csv" "Data cell path"
                Expect.equal d2.Path "raw2.csv" "FreeText cell converted to data path"
            | other -> failwithf "Expected data outputs but got %A" other

        testCase "AddColumn Input fills missing cells with empty sample nodes" <| fun _ ->
            let p1 = Process("Import")
            let p2 = Process("Import")
            let t, _ = makeTable "Import" [| p1; p2 |]

            t.AddColumn(CompositeHeader.Input IOType.Sample, ResizeArray([| CompositeCell.FreeText "Sample1" |]))

            Expect.equal p2.Inputs.Count 1 "missing input cell still creates the slot"
            match p2.Inputs.[0] with
            | SampleNode m ->
                Expect.equal m.Name "" "missing cell becomes empty text"
                Expect.equal m.AdditionalType (Some "Sample") "input type is preserved"
            | other -> failwithf "Expected sample input but got %A" other

        testCase "RemoveColumn Input removes projected inputs from every process" <| fun _ ->
            let p1 = Process("Cleanup")
            let p2 = Process("Cleanup")
            p1.AddInputSample(Sample("Source1", additionalType = "Source"))
            p2.AddInputSample(Sample("Source2", additionalType = "Source"))
            let t, _ = makeTable "Cleanup" [| p1; p2 |]

            t.RemoveColumn(CompositeHeader.Input IOType.Source)

            Expect.equal p1.Inputs.Count 0 "first input removed"
            Expect.equal p2.Inputs.Count 0 "second input removed"

        testCase "UpdateRow can replace sample output with data output when the output column type changes" <| fun _ ->
            let p = Process("Export")
            p.AddOutputSample(Sample("OldSample", additionalType = "Sample"))
            let t, _ = makeTable "Export" [| p |]

            t.RemoveColumn(CompositeHeader.Output IOType.Sample)
            t.AddColumn(CompositeHeader.Output IOType.Data, ResizeArray([| CompositeCell.Data(Data("result.csv")) |]))

            Expect.equal p.Outputs.Count 1 "one output slot remains"
            match p.Outputs.[0] with
            | DataNode d -> Expect.equal d.Path "result.csv" "output was recreated as data"
            | SampleNode _ -> Expect.isTrue false "output should be recreated as data"

    ]

    testList "Synthetic carrier nodes" [

        testCase "AddColumn Characteristic creates synthetic input when input is missing" <| fun _ ->
            let p = Process("Annotate")
            let t, _ = makeTable "Annotate" [| p |]

            t.AddColumn(
                CompositeHeader.Characteristic(DefinedTerm("organism")),
                ResizeArray([| CompositeCell.FreeText "E. coli" |])
            )

            Expect.equal p.Inputs.Count 1 "synthetic input created"
            match p.Inputs.[0] with
            | SampleNode m ->
                let pv = m.AdditionalProperty |> Seq.tryFind (fun pv -> pv.Name = "organism")
                Expect.isSome pv "characteristic stored on synthetic input"
                Expect.equal pv.Value.Value (Some "E. coli") "characteristic value"
            | other -> failwithf "Expected synthetic sample input but got %A" other

        testCase "AddColumn Factor creates synthetic output when output is missing" <| fun _ ->
            let p = Process("Annotate")
            let t, _ = makeTable "Annotate" [| p |]

            t.AddColumn(
                CompositeHeader.Factor(DefinedTerm("growth phase")),
                ResizeArray([| CompositeCell.FreeText "log" |])
            )

            Expect.equal p.Outputs.Count 1 "synthetic output created"
            match p.Outputs.[0] with
            | SampleNode m ->
                let pv = m.AdditionalProperty |> Seq.tryFind (fun pv -> pv.Name = "growth phase")
                Expect.isSome pv "factor stored on synthetic output"
                Expect.equal pv.Value.Value (Some "log") "factor value"
            | other -> failwithf "Expected synthetic sample output but got %A" other

    ]

    testList "Writable protocol columns" [

        testCase "AddColumn ProtocolREF creates protocols when missing" <| fun _ ->
            let p1 = Process("Protocolize")
            let p2 = Process("Protocolize")
            let t, _ = makeTable "Protocolize" [| p1; p2 |]

            t.AddColumn(
                CompositeHeader.ProtocolREF,
                ResizeArray([| CompositeCell.FreeText "extraction"; CompositeCell.FreeText "measurement" |])
            )

            Expect.equal (p1.ExecutesProtocol |> Option.bind (fun p -> p.Name)) (Some "extraction") "first protocol name"
            Expect.equal (p2.ExecutesProtocol |> Option.bind (fun p -> p.Name)) (Some "measurement") "second protocol name"

        testCase "UpdateRow updates protocol metadata columns" <| fun _ ->
            let p = Process("Protocolize")
            let proto = Plan("old")
            proto.Description <- Some "old description"
            p.ExecutesProtocol <- Some proto
            let t, _ = makeTable "Protocolize" [| p |]

            let headers = t.Headers
            let cells = ResizeArray(Seq.init headers.Count (fun _ -> CompositeCell.FreeText ""))
            let refIdx = headers |> Seq.findIndex (fun h -> h = CompositeHeader.ProtocolREF)
            let descIdx = headers |> Seq.findIndex (fun h -> h = CompositeHeader.ProtocolDescription)
            cells.[refIdx] <- CompositeCell.FreeText "new"
            cells.[descIdx] <- CompositeCell.FreeText "new description"
            t.UpdateRow(0, cells)

            Expect.equal proto.Name (Some "new") "protocol ref updated"
            Expect.equal proto.Description (Some "new description") "protocol description updated"

        testCase "AddColumn Component creates protocol when missing" <| fun _ ->
            let p = Process("Equip")
            let t, _ = makeTable "Equip" [| p |]

            t.AddColumn(
                CompositeHeader.Component(DefinedTerm("instrument")),
                ResizeArray([| CompositeCell.FreeText "Orbitrap" |])
            )

            Expect.isSome p.ExecutesProtocol "protocol created"
            let pv =
                p.ExecutesProtocol
                |> Option.bind (fun proto -> proto.LabEquipment |> Seq.tryFind (fun pv -> pv.Name = "instrument"))
            Expect.isSome pv "component stored on created protocol"
            Expect.equal pv.Value.Value (Some "Orbitrap") "component value"

    ]

]
