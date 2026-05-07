module ProcessCore.Tests.Graph.ProcessGraphQueries

open Fable.Pyxpecto
open ProcessCore
open ProcessCore.Tests.Fixtures

let tests = testList "ProcessGraphQueries" [

    // ── TryGetProcess ─────────────────────────────────────────────────────────

    testCase "TryGetProcess found" <| fun _ ->
        let f  = makeFixtureA()
        let pg = ProcessGraph(f.DS.AllProcesses())
        Expect.equal (pg.TryGetProcess("p1")) (Some f.P1) "p1 found by name"

    testCase "TryGetProcess not found" <| fun _ ->
        let f  = makeFixtureA()
        let pg = ProcessGraph(f.DS.AllProcesses())
        Expect.equal (pg.TryGetProcess("does-not-exist")) None "missing → None"

    // ── FindProcessesByProtocolType ───────────────────────────────────────────

    testCase "FindProcessesByProtocolType" <| fun _ ->
        let f  = makeFixtureA()
        let pg = ProcessGraph(f.DS.AllProcesses())
        let names = pg.FindProcessesByProtocolType("cell growth") |> Seq.map (fun p -> p.Name) |> Set.ofSeq
        Expect.equal names (Set.ofList ["p1"]) "only p1 matches cell growth"

    // ── FindProcessesByPropertyValue ──────────────────────────────────────────

    testCase "FindProcessesByPropertyValue" <| fun _ ->
        let f  = makeFixtureA()
        let pg = ProcessGraph(f.DS.AllProcesses())
        let names = pg.FindProcessesByPropertyValue("temperature", "37") |> Seq.map (fun p -> p.Name) |> Set.ofSeq
        Expect.equal names (Set.ofList ["p1"]) "p1 has temperature=37"

    // ── FindProcessesByPropertyName ───────────────────────────────────────────

    testCase "FindProcessesByPropertyName" <| fun _ ->
        let f  = makeFixtureA()
        let pg = ProcessGraph(f.DS.AllProcesses())
        let names = pg.FindProcessesByPropertyName("enzyme") |> Seq.map (fun p -> p.Name) |> Set.ofSeq
        Expect.equal names (Set.ofList ["p2"]) "p2 has enzyme"

    // ── ProcessesForNode ──────────────────────────────────────────────────────

    testCase "ProcessesForNode — material" <| fun _ ->
        let f  = makeFixtureA()
        let pg = ProcessGraph(f.DS.AllProcesses())
        // Sample1: output of p1, input of p2
        let procs = pg.ProcessesForNode(MaterialNode f.Sample1)
        let names = procs |> Seq.map (fun p -> p.Name) |> Set.ofSeq
        Expect.equal names (Set.ofList ["p1";"p2"]) "Sample1 is in both p1 and p2"

    testCase "ProcessesForNode — scoped to subset" <| fun _ ->
        // Build a graph with only p1 in scope; p2 should not appear
        let f  = makeFixtureA()
        let pg = ProcessGraph(ResizeArray<LabProcess>([| f.P1 |]))
        let procs = pg.ProcessesForNode(MaterialNode f.Sample1)
        let names = procs |> Seq.map (fun p -> p.Name) |> Set.ofSeq
        Expect.equal names (Set.ofList ["p1"]) "only p1 is in scope"

    // ── PathsThrough ──────────────────────────────────────────────────────────

    testCase "PathsThrough — single path" <| fun _ ->
        let f  = makeFixtureA()
        let pg = ProcessGraph(f.DS.AllProcesses())
        // Sample1 is an output of p1 and an input of p2 — two seed processes.
        // Each seed produces one identical maximal path [p1,p2,p3], so 2 paths total.
        let paths = pg.PathsThrough(MaterialNode f.Sample1)
        Expect.equal paths.Count 2 "two seeds (p1, p2) each produce one path"
        // Both paths should cover all three processes
        for path in paths do
            let procs = path.Processes |> Seq.map (fun p -> p.Name) |> Set.ofSeq
            Expect.equal procs (Set.ofList ["p1";"p2";"p3"]) "each path covers all three processes"

    testCase "PathsThrough — branching graph" <| fun _ ->
        let f  = makeFixtureB()
        let pg = ProcessGraph(f.DS.AllProcesses())
        // Sample1 is in p1 (output), p2 (input) and p3 (input) — three seeds.
        // p1 as seed expands to 2 paths (one per branch), p2 and p3 each give 1 path → 4 total.
        let paths = pg.PathsThrough(MaterialNode f.Sample1)
        Expect.equal paths.Count 4 "branching: 3 seeds produce 4 paths total"
        // All unique process-name sets should be {p1,p2} or {p1,p3}
        let uniqueSets = paths |> Seq.map (fun p -> p.Processes |> Seq.map (fun x -> x.Name) |> Set.ofSeq) |> Set.ofSeq
        Expect.equal uniqueSets (Set.ofList [Set.ofList ["p1";"p2"]; Set.ofList ["p1";"p3"]])
            "two distinct path shapes"

    testCase "PathsThrough — node not in graph" <| fun _ ->
        let f  = makeFixtureA()
        let pg = ProcessGraph(f.DS.AllProcesses())
        let outsider = Material("Outsider")
        let paths = pg.PathsThrough(MaterialNode outsider)
        Expect.equal paths.Count 0 "node not in graph → no paths"

    // ── NodesDownstreamOf / NodesUpstreamOf ───────────────────────────────────

    testCase "NodesDownstreamOf" <| fun _ ->
        let f  = makeFixtureA()
        let pg = ProcessGraph(f.DS.AllProcesses())
        let nodes = pg.NodesDownstreamOf(MaterialNode f.Source1)
        let keys  = nodes |> Seq.map (fun n -> n.Key()) |> Set.ofSeq
        Expect.isTrue (keys.Contains "M:Sample1")      "Sample1 is downstream"
        Expect.isTrue (keys.Contains "M:Sample2")      "Sample2 is downstream"
        Expect.isTrue (keys.Contains "D:rawData1.csv") "rawData1.csv is downstream"

    testCase "NodesUpstreamOf" <| fun _ ->
        let f  = makeFixtureA()
        let pg = ProcessGraph(f.DS.AllProcesses())
        let nodes = pg.NodesUpstreamOf(DataNode f.RawData1)
        let keys  = nodes |> Seq.map (fun n -> n.Key()) |> Set.ofSeq
        Expect.isTrue (keys.Contains "M:Source1") "Source1 is upstream"
        Expect.isTrue (keys.Contains "M:Sample1") "Sample1 is upstream"
        Expect.isTrue (keys.Contains "M:Sample2") "Sample2 is upstream"

    // ── MaterialsDownstreamOf / MaterialsUpstreamOf ───────────────────────────

    testCase "MaterialsDownstreamOf" <| fun _ ->
        let f    = makeFixtureA()
        let pg   = ProcessGraph(f.DS.AllProcesses())
        let mats = pg.MaterialsDownstreamOf(MaterialNode f.Source1)
        let names = mats |> Seq.map (fun m -> m.Name) |> Set.ofSeq
        // NodesDownstreamOf collects all inputs+outputs of downstream processes including
        // the starting process p1, whose input is Source1 itself.
        Expect.equal names (Set.ofList ["Source1";"Sample1";"Sample2"])
            "downstream materials include Source1 (input of starting process p1)"

    testCase "MaterialsUpstreamOf" <| fun _ ->
        let f    = makeFixtureA()
        let pg   = ProcessGraph(f.DS.AllProcesses())
        let mats = pg.MaterialsUpstreamOf(DataNode f.RawData1)
        let names = mats |> Seq.map (fun m -> m.Name) |> Set.ofSeq
        Expect.equal names (Set.ofList ["Source1";"Sample1";"Sample2"])
            "all upstream materials from rawData1.csv"

    // ── DataDownstreamOf / DataUpstreamOf ─────────────────────────────────────

    testCase "DataDownstreamOf" <| fun _ ->
        let f  = makeFixtureA()
        let pg = ProcessGraph(f.DS.AllProcesses())
        let ds = pg.DataDownstreamOf(MaterialNode f.Source1)
        let ps = ds |> Seq.map (fun d -> d.Path) |> Set.ofSeq
        Expect.equal ps (Set.ofList ["rawData1.csv"]) "rawData1.csv is downstream data"

    testCase "DataUpstreamOf from a material root node is empty" <| fun _ ->
        let f  = makeFixtureA()
        let pg = ProcessGraph(f.DS.AllProcesses())
        // Source1 has no upstream processes at all → no data nodes returned
        let ds = pg.DataUpstreamOf(MaterialNode f.Source1)
        Expect.equal ds.Count 0 "no Data nodes upstream of Source1"

    // ── AllConnectedNodes ─────────────────────────────────────────────────────

    testCase "AllConnectedNodes via ProcessGraph" <| fun _ ->
        let f  = makeFixtureA()
        let pg = ProcessGraph(f.DS.AllProcesses())
        let pgNodes = pg.AllConnectedNodes(MaterialNode f.Sample1) |> Seq.map (fun n -> n.Key()) |> Set.ofSeq
        // ProcessGraph.AllConnectedNodes collects all nodes from paths, including Sample1 itself.
        // IONode.AllConnectedNodes explicitly excludes the start node.
        Expect.isTrue (pgNodes.Contains "M:Sample1")      "ProcessGraph version includes the query node"
        Expect.isTrue (pgNodes.Contains "M:Source1")      "Source1 is connected"
        Expect.isTrue (pgNodes.Contains "M:Sample2")      "Sample2 is connected"
        Expect.isTrue (pgNodes.Contains "D:rawData1.csv") "rawData1.csv is connected"

    // ── ConnectedMaterialsForNode / ConnectedDataForNode ──────────────────────

    testCase "ConnectedMaterialsForNode" <| fun _ ->
        let f    = makeFixtureA()
        let pg   = ProcessGraph(f.DS.AllProcesses())
        let mats = pg.ConnectedMaterialsForNode(MaterialNode f.Sample1)
        let names = mats |> Seq.map (fun m -> m.Name) |> Set.ofSeq
        // ProcessGraph.AllConnectedNodes includes the query node itself in path nodes
        Expect.equal names (Set.ofList ["Source1";"Sample1";"Sample2"])
            "connected materials include Sample1 itself (ProcessGraph version)"

    testCase "ConnectedDataForNode" <| fun _ ->
        let f  = makeFixtureA()
        let pg = ProcessGraph(f.DS.AllProcesses())
        let ds = pg.ConnectedDataForNode(MaterialNode f.Sample1)
        let ps = ds |> Seq.map (fun d -> d.Path) |> Set.ofSeq
        Expect.equal ps (Set.ofList ["rawData1.csv"]) "rawData1.csv is connected"

    // ── AllPropertyValuesForNode ──────────────────────────────────────────────

    testCase "AllPropertyValuesForNode" <| fun _ ->
        let f  = makeFixtureA()
        let pg = ProcessGraph(f.DS.AllProcesses())
        let pvs = pg.AllPropertyValuesForNode(MaterialNode f.Sample1)
        let names = pvs |> Seq.map (fun pv -> pv.Name) |> Set.ofSeq
        Expect.isTrue (names.Contains "temperature") "p1 temperature reachable from Sample1"
        Expect.isTrue (names.Contains "rpm")         "p1 rpm reachable from Sample1"
        Expect.isTrue (names.Contains "enzyme")      "p2 enzyme reachable from Sample1"

    // ── ProtocolParametersForNode ─────────────────────────────────────────────

    testCase "ProtocolParametersForNode" <| fun _ ->
        let f  = makeFixtureA()
        let pg = ProcessGraph(f.DS.AllProcesses())
        let fps = pg.ProtocolParametersForNode(MaterialNode f.Sample1)
        let names = fps |> Seq.map (fun fp -> fp.Name) |> Set.ofSeq
        // p1 protocol has temperature + rpm FPs; p2 protocol has none defined; p3 has no protocol
        Expect.isTrue (names.Contains "temperature") "temperature FP from p1"
        Expect.isTrue (names.Contains "rpm")         "rpm FP from p1"

    // ── MaterialsResultingFromCondition (name+value overload) ─────────────────

    testCase "MaterialsResultingFromCondition (name+value overload)" <| fun _ ->
        let f  = makeFixtureB()
        let pg = ProcessGraph(f.DS.AllProcesses())
        // p1 qualifies; terminal material outputs are SampleA and SampleB
        let mats = pg.MaterialsResultingFromCondition("cell growth", "temperature", "37")
        let names = mats |> Seq.map (fun m -> m.Name) |> Set.ofSeq
        Expect.equal names (Set.ofList ["SampleA";"SampleB"])
            "both branch terminals returned"

    // ── MaterialsResultingFromCondition (predicate overload) ──────────────────

    testCase "MaterialsResultingFromCondition (predicate overload)" <| fun _ ->
        let f  = makeFixtureB()
        let pg = ProcessGraph(f.DS.AllProcesses())
        // Custom predicate: any PV whose value is "37"
        let pred (pv: PropertyValue) = pv.Value = Some "37"
        let mats = pg.MaterialsResultingFromCondition("cell growth", pred)
        let names = mats |> Seq.map (fun m -> m.Name) |> Set.ofSeq
        Expect.equal names (Set.ofList ["SampleA";"SampleB"])
            "predicate overload returns same result"

]
