module ProcessCore.Tests.Graph.Traversal

open Fable.Pyxpecto
open ProcessCore
open ProcessCore.Tests.Fixtures

// helpers
let setOfNames (procs: ResizeArray<LabProcess>) = procs |> Seq.map (fun p -> p.Name) |> Set.ofSeq
let setOfMaterials (ms: ResizeArray<Material>) = ms |> Seq.map (fun m -> m.Name) |> Set.ofSeq
let setOfData (ds: ResizeArray<Data>) = ds |> Seq.map (fun d -> d.Path) |> Set.ofSeq
let nodeKeys (ns: ResizeArray<IONode>) = ns |> Seq.map (fun n -> n.Key()) |> Set.ofSeq

let tests = testList "Traversal" [

    // ── 5.1 AllConnectedProcesses / AllConnectedNodes ────────────────────────

    testList "AllConnectedProcesses" [

        testCase "from root node" <| fun _ ->
            let f     = makeFixtureA()
            let procs = (MaterialNode f.Source1).AllConnectedProcesses()
            Expect.equal (setOfNames procs) (Set.ofList ["p1";"p2";"p3"])
                "Source1 → all three processes"

        testCase "from mid node" <| fun _ ->
            let f     = makeFixtureA()
            let procs = (MaterialNode f.Sample1).AllConnectedProcesses()
            Expect.equal (setOfNames procs) (Set.ofList ["p1";"p2";"p3"])
                "Sample1 → all three processes"

        testCase "from leaf" <| fun _ ->
            let f     = makeFixtureA()
            let procs = (DataNode f.RawData1).AllConnectedProcesses()
            Expect.equal (setOfNames procs) (Set.ofList ["p1";"p2";"p3"])
                "rawData1.csv → all three processes"

        testCase "branching graph" <| fun _ ->
            let f     = makeFixtureB()
            let procs = (MaterialNode f.Source1).AllConnectedProcesses()
            Expect.equal (setOfNames procs) (Set.ofList ["p1";"p2";"p3"])
                "Source1 in B → all three processes including both branches"

        testCase "with scope" <| fun _ ->
            let f     = makeFixtureA()
            let scope = ResizeArray<LabProcess>([| f.P1 |])
            let procs = (MaterialNode f.Source1).AllConnectedProcesses(scope)
            Expect.equal (setOfNames procs) (Set.ofList ["p1"])
                "Scoped to p1 → only p1"

    ]

    testList "Processes" [

        testCase "direct processes for node" <| fun _ ->
            let f = makeFixtureA()
            let procs = (MaterialNode f.Sample1).Processes()
            Expect.equal (setOfNames procs) (Set.ofList ["p1";"p2"])
                "Sample1 is an output of p1 and an input of p2"

        testCase "direct processes scoped to subset" <| fun _ ->
            let f = makeFixtureA()
            let scope = ResizeArray<LabProcess>([| f.P1 |])
            let procs = (MaterialNode f.Sample1).Processes(scope)
            Expect.equal (setOfNames procs) (Set.ofList ["p1"])
                "Only p1 is visible in the explicit process scope"

    ]

    testList "AllConnectedNodes" [

        testCase "from root excludes self" <| fun _ ->
            let f     = makeFixtureA()
            let nodes = (MaterialNode f.Source1).AllConnectedNodes()
            let keys  = nodeKeys nodes
            Expect.isFalse (keys.Contains "M:Source1") "Should not include Source1 itself"
            Expect.isTrue  (keys.Contains "M:Sample1")  "Should include Sample1"
            Expect.isTrue  (keys.Contains "M:Sample2")  "Should include Sample2"
            Expect.isTrue  (keys.Contains "D:rawData1.csv") "Should include rawData1.csv"
            Expect.equal   keys.Count 3 "Should have exactly 3 connected nodes"

        testCase "with scope" <| fun _ ->
            let f     = makeFixtureA()
            let scope = ResizeArray<LabProcess>([| f.P1 |])
            let nodes = (MaterialNode f.Source1).AllConnectedNodes(scope)
            let keys  = nodeKeys nodes
            Expect.isTrue  (keys.Contains "M:Sample1") "Should include Sample1 (output of p1)"
            Expect.isFalse (keys.Contains "M:Sample2") "Should not include Sample2 (beyond p1 scope)"

    ]

    // ── 5.2 UpstreamProcesses / UpstreamNodes ────────────────────────────────

    testList "UpstreamProcesses" [

        testCase "from leaf" <| fun _ ->
            let f     = makeFixtureA()
            let procs = (DataNode f.RawData1).UpstreamProcesses()
            Expect.equal (setOfNames procs) (Set.ofList ["p1";"p2";"p3"])
                "rawData1.csv → upstream: p3, p2, p1"

        testCase "from mid" <| fun _ ->
            let f     = makeFixtureA()
            let procs = (MaterialNode f.Sample2).UpstreamProcesses()
            Expect.equal (setOfNames procs) (Set.ofList ["p2";"p1"])
                "Sample2 → upstream: p2, p1"

        testCase "from root" <| fun _ ->
            let f     = makeFixtureA()
            let procs = (MaterialNode f.Source1).UpstreamProcesses()
            Expect.equal procs.Count 0 "Source1 → no upstream processes"

        testCase "with scope" <| fun _ ->
            let f     = makeFixtureA()
            let scope = ResizeArray<LabProcess>([| f.P1; f.P2 |])
            // Start from Sample2 (output of p2, input of p3); p3 is NOT in scope,
            // so traversal goes upstream through p2 → Sample1 → p1 → Source1.
            let procs = (MaterialNode f.Sample2).UpstreamProcesses(scope)
            Expect.equal (setOfNames procs) (Set.ofList ["p1";"p2"])
                "Scoped to p1+p2 while querying from Sample2 → {p2, p1}"

    ]

    testList "UpstreamNodes" [

        testCase "from leaf" <| fun _ ->
            let f     = makeFixtureA()
            let nodes = (DataNode f.RawData1).UpstreamNodes()
            let keys  = nodeKeys nodes
            Expect.equal keys (Set.ofList ["M:Source1";"M:Sample1";"M:Sample2"])
                "rawData1.csv → {Sample2, Sample1, Source1}"

        testCase "from mid" <| fun _ ->
            let f     = makeFixtureA()
            let nodes = (MaterialNode f.Sample1).UpstreamNodes()
            let keys  = nodeKeys nodes
            Expect.equal keys (Set.ofList ["M:Source1"])
                "Sample1 → upstream: {Source1}"

        testCase "distinguish by process io order" <| fun _ ->
            let p     = LabProcess("MyProcess")
            p.AddInputMaterial(Material("Input1"))
            p.AddInputMaterial(Material("Input2"))
            p.AddOutputMaterial(Material("Output1"))
            p.AddOutputMaterial(Material("Output2"))
            let output2 = p.Outputs.[1] // Output2
            let nodes   = output2.UpstreamNodes()
            Expect.hasLength nodes 1 "Output2 should have exactly one upstream node (Input2)"
            Expect.equal (nodes[0].Key()) ("M:Input2") "Output2 → {Input2} in correct order"

    ]

    // ── 5.3 DownstreamProcesses / DownstreamNodes ────────────────────────────

    testList "DownstreamProcesses" [

        testCase "from root" <| fun _ ->
            let f     = makeFixtureA()
            let procs = (MaterialNode f.Source1).DownstreamProcesses()
            Expect.equal (setOfNames procs) (Set.ofList ["p1";"p2";"p3"])
                "Source1 → downstream: p1, p2, p3"

        testCase "from mid" <| fun _ ->
            let f     = makeFixtureA()
            let procs = (MaterialNode f.Sample1).DownstreamProcesses()
            Expect.equal (setOfNames procs) (Set.ofList ["p2";"p3"])
                "Sample1 → downstream: p2, p3"

        testCase "from leaf" <| fun _ ->
            let f     = makeFixtureA()
            let procs = (DataNode f.RawData1).DownstreamProcesses()
            Expect.equal procs.Count 0 "rawData1.csv → no downstream processes"

        testCase "distinguish by process io order" <| fun _ ->
            let p     = LabProcess("MyProcess")
            p.AddInputMaterial(Material("Input1"))
            p.AddInputMaterial(Material("Input2"))
            p.AddOutputMaterial(Material("Output1"))
            p.AddOutputMaterial(Material("Output2"))
            let input1  = p.Inputs.[0] // Input1
            let nodes   = input1.DownstreamNodes()
            Expect.hasLength nodes 1 "Input1 should have exactly one downstream node (Output1)"
            Expect.equal (nodes[0].Key()) ("M:Output1") "Input1 → {Output1} in correct order"

    ]

    testList "DownstreamNodes" [

        testCase "from root" <| fun _ ->
            let f     = makeFixtureA()
            let nodes = (MaterialNode f.Source1).DownstreamNodes()
            let keys  = nodeKeys nodes
            Expect.equal keys (Set.ofList ["M:Sample1";"M:Sample2";"D:rawData1.csv"])
                "Source1 → {Sample1, Sample2, rawData1.csv}"

        testCase "branching graph" <| fun _ ->
            let f     = makeFixtureB()
            let nodes = (MaterialNode f.Sample1).DownstreamNodes()
            let keys  = nodeKeys nodes
            Expect.equal keys (Set.ofList ["M:SampleA";"M:SampleB"])
                "Sample1 in B → {SampleA, SampleB}"

    ]

    // ── 5.4 RootNodes / FinalNodes ────────────────────────────────────────────

    testList "RootNodes and FinalNodes" [

        testCase "RootNodes from leaf" <| fun _ ->
            let f     = makeFixtureA()
            let roots = (DataNode f.RawData1).RootNodes()
            let keys  = nodeKeys roots
            Expect.equal keys (Set.ofList ["M:Source1"])
                "RootNodes from rawData1.csv → {Source1}"

        testCase "FinalNodes from root" <| fun _ ->
            let f      = makeFixtureA()
            let finals = (MaterialNode f.Source1).FinalNodes()
            let keys   = nodeKeys finals
            Expect.equal keys (Set.ofList ["D:rawData1.csv"])
                "FinalNodes from Source1 → {rawData1.csv}"

        testCase "RootNodes in branching graph" <| fun _ ->
            let f     = makeFixtureB()
            let roots = (MaterialNode f.SampleA).RootNodes()
            let keys  = nodeKeys roots
            Expect.equal keys (Set.ofList ["M:Source1"])
                "RootNodes from SampleA in B → {Source1}"

        testCase "FinalNodes in merging graph" <| fun _ ->
            let f      = makeFixtureC()
            let finals = (MaterialNode f.Source1).FinalNodes()
            let keys   = nodeKeys finals
            Expect.equal keys (Set.ofList ["M:FinalSample"])
                "FinalNodes from Source1 in C → {FinalSample}"

    ]

    // ── 5.5 UpstreamMaterials / DownstreamData / etc. ─────────────────────────

    testList "Typed traversal helpers" [

        testCase "UpstreamMaterials from data leaf" <| fun _ ->
            let f    = makeFixtureA()
            let mats = (DataNode f.RawData1).UpstreamMaterials()
            Expect.equal (setOfMaterials mats) (Set.ofList ["Source1";"Sample1";"Sample2"])
                "rawData1.csv → upstream materials: {Sample2, Sample1, Source1}"

        testCase "DownstreamData from root" <| fun _ ->
            let f    = makeFixtureA()
            let data = (MaterialNode f.Source1).DownstreamData()
            Expect.equal (setOfData data) (Set.ofList ["rawData1.csv"])
                "Source1 → downstream data: {rawData1.csv}"

        testCase "ConnectedMaterials from mid" <| fun _ ->
            let f    = makeFixtureA()
            let mats = (MaterialNode f.Sample1).ConnectedMaterials()
            Expect.equal (setOfMaterials mats) (Set.ofList ["Source1";"Sample2"])
                "Sample1 connected materials: {Source1, Sample2}"

        testCase "ConnectedData returns only Data nodes" <| fun _ ->
            let f    = makeFixtureA()
            let data = (MaterialNode f.Source1).ConnectedData()
            Expect.equal (setOfData data) (Set.ofList ["rawData1.csv"])
                "Source1 connected data: {rawData1.csv}"

    ]

]
