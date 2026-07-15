module ProcessCore.Tests.Graph.AnnotationSources

open Fable.Pyxpecto
open ProcessCore
open ProcessCore.Tests.Fixtures

let tests = testList "AnnotationSources" [

    testCase "Process.AnnotationsByName — all 4 sources" <| fun _ ->
        let f = makeFixtureFourSources()
        // Each PV has a unique name: temperature, organism, growth_phase, instrument
        Expect.equal (f.Process.AnnotationsByName("temperature").Count)  1 "ParameterValue source"
        Expect.equal (f.Process.AnnotationsByName("organism").Count)     1 "Input node AdditionalProperty"
        Expect.equal (f.Process.AnnotationsByName("growth_phase").Count) 1 "Output node AdditionalProperty"
        Expect.equal (f.Process.AnnotationsByName("instrument").Count)   1 "Protocol Component"

    testCase "IONode.AllAnnotations — all 4 sources" <| fun _ ->
        let f   = makeFixtureFourSources()
        let pvs = (SampleNode f.InputNode).AllAnnotations()
        let names = pvs |> Seq.map (fun pv -> pv.Name) |> Set.ofSeq
        Expect.isTrue (names.Contains "temperature")  "ParameterValue should be included"
        Expect.isTrue (names.Contains "organism")     "Input node property should be included"
        Expect.isTrue (names.Contains "growth_phase") "Output node property should be included"
        Expect.isTrue (names.Contains "instrument")   "Protocol component should be included"

    testCase "UpstreamAnnotations — filters to upstream only" <| fun _ ->
        // The OutputNode is between the central process and the downstream process.
        // DownstreamOnlyPV is only on DownstreamProc → should NOT be included when
        // walking upstream from OutputNode.
        let f    = makeFixtureFourSources()
        let pvs  = (SampleNode f.OutputNode).UpstreamAnnotations()
        let names = pvs |> Seq.map (fun pv -> pv.Name) |> Set.ofSeq
        Expect.isFalse (names.Contains "downstream_param")
            "PV from DownstreamProc should not appear in upstream query from OutputNode"
        Expect.isTrue  (names.Contains "temperature")
            "ParameterValue on central process should be included"

    testCase "DownstreamAnnotations — filters to downstream only" <| fun _ ->
        // Walk downstream from InputNode.
        // UpstreamOnlyPV is only on UpstreamProc → should NOT be included.
        let f    = makeFixtureFourSources()
        let pvs  = (SampleNode f.InputNode).DownstreamAnnotations()
        let names = pvs |> Seq.map (fun pv -> pv.Name) |> Set.ofSeq
        Expect.isFalse (names.Contains "upstream_param")
            "PV from UpstreamProc should not appear in downstream query from InputNode"
        Expect.isTrue  (names.Contains "temperature")
            "ParameterValue on central process should be included"

    testCase "UpstreamAnnotations with protocolName filter" <| fun _ ->
        // Walk upstream from OutputNode filtered to protocol "four-source-protocol".
        // Only the central process has that protocol, so only its PVs appear.
        let f    = makeFixtureFourSources()
        let pvs  = (SampleNode f.OutputNode).UpstreamAnnotations(protocolName = "four-source-protocol")
        let names = pvs |> Seq.map (fun pv -> pv.Name) |> Set.ofSeq
        Expect.isTrue  (names.Contains "temperature") "Central process PV should appear"
        Expect.isFalse (names.Contains "upstream_param")
            "UpstreamProc has no matching protocol name — its PV should be filtered out"

    testCase "Deduplication across sources" <| fun _ ->
        // Put the same PV on both the process ParameterValue and the input node
        // AdditionalProperty. AllAnnotations should deduplicate it.
        let m    = Sample("Dedup_Input")
        let sharedPV = Annotation("dedup_name", value = "dedup_val")
        m.AddAdditionalProperty(sharedPV)
        let p = Process("dedup_proc")
        p.SetInputSample(m)
        p.AddParameterValue(sharedPV)   // same PV object → same name/value
        let pvs = (SampleNode m).AllAnnotations()
        let count = pvs |> Seq.filter (fun pv -> pv.Name = "dedup_name") |> Seq.length
        Expect.equal count 1 "Identical PV appearing in two sources should be deduplicated"

    testCase "AllAnnotations on Path" <| fun _ ->
        // Build a Path containing only the central process; the same four PVs
        // should be visible as through IONode.AllAnnotations.
        let f    = makeFixtureFourSources()
        let path = Path(ResizeArray<Process>([| f.Process |]))
        let pvs  = path.AllAnnotations()
        let names = pvs |> Seq.map (fun pv -> pv.Name) |> Set.ofSeq
        Expect.isTrue (names.Contains "temperature")  "ParameterValue via Path"
        Expect.isTrue (names.Contains "organism")     "Input node property via Path"
        Expect.isTrue (names.Contains "growth_phase") "Output node property via Path"
        Expect.isTrue (names.Contains "instrument")   "Protocol component via Path"

]
