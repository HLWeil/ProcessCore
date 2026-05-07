module ProcessCore.Tests.Graph.PropertyValueSources

open Fable.Pyxpecto
open ProcessCore
open ProcessCore.Tests.Fixtures

let tests = testList "PropertyValueSources" [

    testCase "LabProcess.PropertyValuesByName — all 4 sources" <| fun _ ->
        let f = makeFixtureFourSources()
        // Each PV has a unique name: temperature, organism, growth_phase, instrument
        Expect.equal (f.Process.PropertyValuesByName("temperature").Count)  1 "ParameterValue source"
        Expect.equal (f.Process.PropertyValuesByName("organism").Count)     1 "Input node AdditionalProperty"
        Expect.equal (f.Process.PropertyValuesByName("growth_phase").Count) 1 "Output node AdditionalProperty"
        Expect.equal (f.Process.PropertyValuesByName("instrument").Count)   1 "Protocol LabEquipment"

    testCase "IONode.AllPropertyValues — all 4 sources" <| fun _ ->
        let f   = makeFixtureFourSources()
        let pvs = (MaterialNode f.InputNode).AllPropertyValues()
        let names = pvs |> Seq.map (fun pv -> pv.Name) |> Set.ofSeq
        Expect.isTrue (names.Contains "temperature")  "ParameterValue should be included"
        Expect.isTrue (names.Contains "organism")     "Input node property should be included"
        Expect.isTrue (names.Contains "growth_phase") "Output node property should be included"
        Expect.isTrue (names.Contains "instrument")   "Protocol component should be included"

    testCase "UpstreamPropertyValues — filters to upstream only" <| fun _ ->
        // The OutputNode is between the central process and the downstream process.
        // DownstreamOnlyPV is only on DownstreamProc → should NOT be included when
        // walking upstream from OutputNode.
        let f    = makeFixtureFourSources()
        let pvs  = (MaterialNode f.OutputNode).UpstreamPropertyValues()
        let names = pvs |> Seq.map (fun pv -> pv.Name) |> Set.ofSeq
        Expect.isFalse (names.Contains "downstream_param")
            "PV from DownstreamProc should not appear in upstream query from OutputNode"
        Expect.isTrue  (names.Contains "temperature")
            "ParameterValue on central process should be included"

    testCase "DownstreamPropertyValues — filters to downstream only" <| fun _ ->
        // Walk downstream from InputNode.
        // UpstreamOnlyPV is only on UpstreamProc → should NOT be included.
        let f    = makeFixtureFourSources()
        let pvs  = (MaterialNode f.InputNode).DownstreamPropertyValues()
        let names = pvs |> Seq.map (fun pv -> pv.Name) |> Set.ofSeq
        Expect.isFalse (names.Contains "upstream_param")
            "PV from UpstreamProc should not appear in downstream query from InputNode"
        Expect.isTrue  (names.Contains "temperature")
            "ParameterValue on central process should be included"

    testCase "UpstreamPropertyValues with protocolName filter" <| fun _ ->
        // Walk upstream from OutputNode filtered to protocol "four-source-protocol".
        // Only the central process has that protocol, so only its PVs appear.
        let f    = makeFixtureFourSources()
        let pvs  = (MaterialNode f.OutputNode).UpstreamPropertyValues(protocolName = "four-source-protocol")
        let names = pvs |> Seq.map (fun pv -> pv.Name) |> Set.ofSeq
        Expect.isTrue  (names.Contains "temperature") "Central process PV should appear"
        Expect.isFalse (names.Contains "upstream_param")
            "UpstreamProc has no matching protocol name — its PV should be filtered out"

    testCase "Deduplication across sources" <| fun _ ->
        // Put the same PV on both the process ParameterValue and the input node
        // AdditionalProperty. AllPropertyValues should deduplicate it.
        let m    = Material("Dedup_Input")
        let sharedPV = PropertyValue("dedup_name", value = "dedup_val")
        m.AddAdditionalProperty(sharedPV)
        let p = LabProcess("dedup_proc")
        p.AddInputMaterial(m)
        p.AddParameterValue(sharedPV)   // same PV object → same name/value
        let pvs = (MaterialNode m).AllPropertyValues()
        let count = pvs |> Seq.filter (fun pv -> pv.Name = "dedup_name") |> Seq.length
        Expect.equal count 1 "Identical PV appearing in two sources should be deduplicated"

    testCase "AllPropertyValues on Path" <| fun _ ->
        // Build a Path containing only the central process; the same four PVs
        // should be visible as through IONode.AllPropertyValues.
        let f    = makeFixtureFourSources()
        let path = Path(ResizeArray<LabProcess>([| f.Process |]))
        let pvs  = path.AllPropertyValues()
        let names = pvs |> Seq.map (fun pv -> pv.Name) |> Set.ofSeq
        Expect.isTrue (names.Contains "temperature")  "ParameterValue via Path"
        Expect.isTrue (names.Contains "organism")     "Input node property via Path"
        Expect.isTrue (names.Contains "growth_phase") "Output node property via Path"
        Expect.isTrue (names.Contains "instrument")   "Protocol component via Path"

]
