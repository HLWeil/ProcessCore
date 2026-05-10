module ProcessCore.Tests.Table.TableWrite

open Fable.Pyxpecto
open ProcessCore
open ProcessCore.Table

// ─── helpers ─────────────────────────────────────────────────────────────────

/// Build a minimal single-process table: Source1 --[proc]--> Sample1, with a protocol
let makeBaseTable () =
    let source = Material("Source1", additionalType = "Source")
    let sample = Material("Sample1", additionalType = "Sample")
    let proto  = LabProtocol("extraction")
    let proc   = LabProcess("Growth")
    proc.AddInputMaterial(source)
    proc.AddOutputMaterial(sample)
    proc.ExecutesProtocol <- Some proto
    let ds = Dataset("DS")
    ds.AddProcess(proc)
    Table("Growth", ResizeArray([| proc |]), ds), proc, ds

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

        testCase "AddColumn — Characteristic stored on input material" <| fun _ ->
            let t, proc, _ = makeBaseTable()
            let organism = DefinedTerm("organism")
            t.AddColumn(CompositeHeader.Characteristic(organism),
                        ResizeArray([| CompositeCell.FreeText "E. coli" |]))
            match proc.Inputs |> Seq.tryHead with
            | Some (MaterialNode m) ->
                let pv = m.AdditionalProperty |> Seq.tryFind (fun p -> p.Name = "organism")
                Expect.isSome pv "Characteristic PV on input material"
            | _ -> failwith "No input material"

        testCase "AddColumn — Factor stored on output material" <| fun _ ->
            let t, proc, _ = makeBaseTable()
            let growthPhase = DefinedTerm("growth_phase")
            t.AddColumn(CompositeHeader.Factor(growthPhase),
                        ResizeArray([| CompositeCell.FreeText "log" |]))
            match proc.Outputs |> Seq.tryHead with
            | Some (MaterialNode m) ->
                let pv = m.AdditionalProperty |> Seq.tryFind (fun p -> p.Name = "growth_phase")
                Expect.isSome pv "Factor PV on output material"
            | _ -> failwith "No output material"

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
            let s1 = Material("S1", additionalType = "Source")
            let o1 = Material("O1", additionalType = "Sample")
            let s2 = Material("S2", additionalType = "Source")
            let o2 = Material("O2", additionalType = "Sample")
            let p1 = LabProcess("T")
            p1.AddInputMaterial(s1) ; p1.AddOutputMaterial(o1)
            let p2 = LabProcess("T")
            p2.AddInputMaterial(s2) ; p2.AddOutputMaterial(o2)
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
            proc.AddParameterValue(PropertyValue("rpm", value = "200", unit = "rpm", additionalType = "ParameterValue"))
            t.RemoveColumn(CompositeHeader.Parameter(rpm))
            let hasPV = proc.ParameterValue |> Seq.exists (fun pv -> pv.Name = "rpm")
            Expect.isFalse hasPV "Parameter PV removed"

        testCase "RemoveColumn — removes Characteristic from input" <| fun _ ->
            let t, proc, _ = makeBaseTable()
            let organism = DefinedTerm("organism")
            match proc.Inputs |> Seq.tryHead with
            | Some (MaterialNode m) ->
                m.AddAdditionalProperty(PropertyValue("organism", value = "Mouse", additionalType = "CharacteristicValue"))
            | _ -> ()
            t.RemoveColumn(CompositeHeader.Characteristic(organism))
            let hasPV =
                match proc.Inputs |> Seq.tryHead with
                | Some (MaterialNode m) -> m.AdditionalProperty |> Seq.exists (fun p -> p.Name = "organism")
                | _ -> false
            Expect.isFalse hasPV "Characteristic PV removed"

        testCase "RemoveColumn — removes Factor from output" <| fun _ ->
            let t, proc, _ = makeBaseTable()
            let growthPhase = DefinedTerm("growth_phase")
            match proc.Outputs |> Seq.tryHead with
            | Some (MaterialNode m) ->
                m.AddAdditionalProperty(PropertyValue("growth_phase", value = "log", additionalType = "FactorValue"))
            | _ -> ()
            t.RemoveColumn(CompositeHeader.Factor(growthPhase))
            let hasPV =
                match proc.Outputs |> Seq.tryHead with
                | Some (MaterialNode m) -> m.AdditionalProperty |> Seq.exists (fun p -> p.Name = "growth_phase")
                | _ -> false
            Expect.isFalse hasPV "Factor PV removed"

        testCase "RemoveColumn — removes Component from protocol" <| fun _ ->
            let t, proc, _ = makeBaseTable()
            match proc.ExecutesProtocol with
            | Some proto ->
                proto.AddLabEquipment(PropertyValue("instrument", value = "Orbitrap", additionalType = "Component"))
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

        testCase "AddRow — input cell sets material name" <| fun _ ->
            let t, proc, _ = makeBaseTable()
            let cells = ResizeArray([| CompositeCell.FreeText "Source2"; CompositeCell.FreeText "ref"; CompositeCell.FreeText "Sample2" |])
            t.AddRow(cells = cells)
            let newProc = t.Processes.[1]
            match newProc.Inputs |> Seq.tryHead with
            | Some (MaterialNode m) -> Expect.equal m.Name "Source2" "input name set"
            | _ -> failwith "expected input MaterialNode"

        testCase "AddRow — output cell sets material name" <| fun _ ->
            let t, proc, _ = makeBaseTable()
            let cols = t.Headers
            // Supply enough cells to cover columns. Last cell = output.
            let emptyCells = ResizeArray(Seq.init cols.Count (fun _ -> CompositeCell.FreeText ""))
            emptyCells.[emptyCells.Count - 1] <- CompositeCell.FreeText "Sample2_out"
            t.AddRow(cells = emptyCells)
            let newProc = t.Processes.[1]
            match newProc.Outputs |> Seq.tryHead with
            | Some (MaterialNode m) -> Expect.equal m.Name "Sample2_out" "output name set"
            | _ -> failwith "expected output MaterialNode"

        testCase "AddRow — Data cell creates DataNode input" <| fun _ ->
            let source = Material("Source1", additionalType = "Source")
            let raw    = Data("raw.csv")
            let proc   = LabProcess("M")
            proc.AddInputMaterial(source)
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
            let s1 = Material("S1", additionalType = "Source")
            let o1 = Material("O1", additionalType = "Sample")
            let s2 = Material("S2", additionalType = "Source")
            let o2 = Material("O2", additionalType = "Sample")
            let p1 = LabProcess("T")
            p1.AddInputMaterial(s1)
            p1.AddOutputMaterial(o1)
            let p2 = LabProcess("T")
            p2.AddInputMaterial(s2)
            p2.AddOutputMaterial(o2)
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
            | Some (MaterialNode m) -> Expect.equal m.Name "SInserted" "inserted at correct position"
            | _ -> failwith "expected MaterialNode at index 1"

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
            | Some (MaterialNode m) -> Expect.equal m.Name "UpdatedSource" "input name updated"
            | _ -> failwith "expected MaterialNode"

        testCase "UpdateRow — updates existing PV value" <| fun _ ->
            let t, proc, _ = makeBaseTable()
            proc.AddParameterValue(PropertyValue("rpm", value = "200", unit = "rpm", additionalType = "ParameterValue"))
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
            let s1 = Material("S1", additionalType = "Source")
            let o1 = Material("O1", additionalType = "Sample")
            let s2 = Material("S2", additionalType = "Source")
            let o2 = Material("O2", additionalType = "Sample")
            let p1 = LabProcess("T")
            p1.AddInputMaterial(s1)
            p1.AddOutputMaterial(o1)
            p1.AddParameterValue(PropertyValue("rpm", value = "200", unit = "rpm", additionalType = "ParameterValue"))
            let p2 = LabProcess("T")
            p2.AddInputMaterial(s2)
            p2.AddOutputMaterial(o2)
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

]
