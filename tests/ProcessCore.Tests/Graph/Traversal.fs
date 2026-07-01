module ProcessCore.Tests.Graph.Traversal

open Fable.Pyxpecto
open ProcessCore
open ProcessCore.Tests.Fixtures

// helpers
let setOfNames (procs: ResizeArray<Process>) = procs |> Seq.map (fun p -> p.Name) |> Set.ofSeq
let setOfSamples (ms: ResizeArray<Sample>) = ms |> Seq.map (fun m -> m.Name) |> Set.ofSeq
let setOfData (ds: ResizeArray<Data>) = ds |> Seq.map (fun d -> d.Path) |> Set.ofSeq
let nodeKeys (ns: ResizeArray<IONode>) = ns |> Seq.map (fun n -> n.Key()) |> Set.ofSeq
let annotationNames (pvs: ResizeArray<Annotation>) = pvs |> Seq.map (fun pv -> pv.Name) |> Set.ofSeq

let private makeLinearDataAnnotationFixture () =
    let sample1 = Sample("PV_Sample1")
    sample1.AddAdditionalProperty(Annotation("sample1_characteristic", value = "m1"))

    let sample2 = Sample("PV_Sample2")
    sample2.AddAdditionalProperty(Annotation("sample2_characteristic", value = "m2"))

    let data1 = Data("pv-data1.csv")
    data1.AddAdditionalProperty(Annotation("data1_property", value = "d1"))

    let sample3 = Sample("PV_Sample3")
    sample3.AddAdditionalProperty(Annotation("sample3_characteristic", value = "m3"))

    let sample4 = Sample("PV_Sample4")
    sample4.AddAdditionalProperty(Annotation("sample4_characteristic", value = "m4"))

    let data2 = Data("pv-data2.csv")
    data2.AddAdditionalProperty(Annotation("data2_property", value = "d2"))

    let protocol1 = Recipe("pv-protocol-1")
    protocol1.AddComponent(Annotation("protocol1_component", value = "instrument-1"))

    let process1 = Process("pv-process-1")
    process1.ExecutesProtocol <- Some protocol1
    process1.AddParameterValue(Annotation("process1_parameter", value = "p1"))
    process1.AddInputSample(sample1)
    process1.AddInputSample(sample3)
    process1.AddOutputSample(sample2)
    process1.AddOutputSample(sample4)

    let protocol2 = Recipe("pv-protocol-2")
    protocol2.AddComponent(Annotation("protocol2_component", value = "instrument-2"))

    let process2 = Process("pv-process-2")
    process2.ExecutesProtocol <- Some protocol2
    process2.AddParameterValue(Annotation("process2_parameter", value = "p2"))
    process2.AddInputSample(sample2)
    process2.AddInputSample(sample4)
    process2.AddOutputData(data1)
    process2.AddOutputData(data2)

    let dataset = Dataset("PV-linear")
    dataset.AddProcess(process1)
    dataset.AddProcess(process2)

    dataset, sample1, sample2, data1

let private expectedLinearPathPVNames =
    Set.ofList [
        "sample1_characteristic"
        "sample2_characteristic"
        "data1_property"
        "process1_parameter"
        "process2_parameter"
        "protocol1_component"
        "protocol2_component"
    ]

let private unrelatedParallelLanePVNames =
    Set.ofList [
        "sample3_characteristic"
        "sample4_characteristic"
        "data2_property"
    ]

let private expectNoParallelLaneAnnotations (pvs: ResizeArray<Annotation>) =
    let names = annotationNames pvs
    for name in unrelatedParallelLanePVNames do
        Expect.isFalse (names.Contains name) $"should not include {name} from the parallel IO pair"

let tests = testList "Traversal" [

    // ── 5.1 AllConnectedProcesses / AllConnectedNodes ────────────────────────

    testList "AllConnectedProcesses" [

        testCase "from root node" <| fun _ ->
            let f     = makeFixtureA()
            let procs = (SampleNode f.Source1).AllConnectedProcesses()
            Expect.equal (setOfNames procs) (Set.ofList ["p1";"p2";"p3"])
                "Source1 → all three processes"

        testCase "from mid node" <| fun _ ->
            let f     = makeFixtureA()
            let procs = (SampleNode f.Sample1).AllConnectedProcesses()
            Expect.equal (setOfNames procs) (Set.ofList ["p1";"p2";"p3"])
                "Sample1 → all three processes"

        testCase "from leaf" <| fun _ ->
            let f     = makeFixtureA()
            let procs = (DataNode f.RawData1).AllConnectedProcesses()
            Expect.equal (setOfNames procs) (Set.ofList ["p1";"p2";"p3"])
                "rawData1.csv → all three processes"

        testCase "branching graph" <| fun _ ->
            let f     = makeFixtureB()
            let procs = (SampleNode f.Source1).AllConnectedProcesses()
            Expect.equal (setOfNames procs) (Set.ofList ["p1";"p2";"p3"])
                "Source1 in B → all three processes including both branches"

        testCase "with scope" <| fun _ ->
            let f     = makeFixtureA()
            let scope = ResizeArray<Process>([| f.P1 |])
            let procs = (SampleNode f.Source1).AllConnectedProcesses(scope)
            Expect.equal (setOfNames procs) (Set.ofList ["p1"])
                "Scoped to p1 → only p1"

    ]

    testList "Processes" [

        testCase "direct processes for node" <| fun _ ->
            let f = makeFixtureA()
            let procs = (SampleNode f.Sample1).Processes()
            Expect.equal (setOfNames procs) (Set.ofList ["p1";"p2"])
                "Sample1 is an output of p1 and an input of p2"

        testCase "direct processes scoped to subset" <| fun _ ->
            let f = makeFixtureA()
            let scope = ResizeArray<Process>([| f.P1 |])
            let procs = (SampleNode f.Sample1).Processes(scope)
            Expect.equal (setOfNames procs) (Set.ofList ["p1"])
                "Only p1 is visible in the explicit process scope"

    ]

    testList "AllConnectedNodes" [

        testCase "from root excludes self" <| fun _ ->
            let f     = makeFixtureA()
            let nodes = (SampleNode f.Source1).AllConnectedNodes()
            let keys  = nodeKeys nodes
            Expect.isFalse (keys.Contains "M:Source1") "Should not include Source1 itself"
            Expect.isTrue  (keys.Contains "M:Sample1")  "Should include Sample1"
            Expect.isTrue  (keys.Contains "M:Sample2")  "Should include Sample2"
            Expect.isTrue  (keys.Contains "D:rawData1.csv") "Should include rawData1.csv"
            Expect.equal   keys.Count 3 "Should have exactly 3 connected nodes"

        testCase "with scope" <| fun _ ->
            let f     = makeFixtureA()
            let scope = ResizeArray<Process>([| f.P1 |])
            let nodes = (SampleNode f.Source1).AllConnectedNodes(scope)
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
            let procs = (SampleNode f.Sample2).UpstreamProcesses()
            Expect.equal (setOfNames procs) (Set.ofList ["p2";"p1"])
                "Sample2 → upstream: p2, p1"

        testCase "from root" <| fun _ ->
            let f     = makeFixtureA()
            let procs = (SampleNode f.Source1).UpstreamProcesses()
            Expect.equal procs.Count 0 "Source1 → no upstream processes"

        testCase "with scope" <| fun _ ->
            let f     = makeFixtureA()
            let scope = ResizeArray<Process>([| f.P1; f.P2 |])
            // Start from Sample2 (output of p2, input of p3); p3 is NOT in scope,
            // so traversal goes upstream through p2 → Sample1 → p1 → Source1.
            let procs = (SampleNode f.Sample2).UpstreamProcesses(scope)
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
            let nodes = (SampleNode f.Sample1).UpstreamNodes()
            let keys  = nodeKeys nodes
            Expect.equal keys (Set.ofList ["M:Source1"])
                "Sample1 → upstream: {Source1}"

        testCase "distinguish by process io order" <| fun _ ->
            let p     = Process("MyProcess")
            p.AddInputSample(Sample("Input1"))
            p.AddInputSample(Sample("Input2"))
            p.AddOutputSample(Sample("Output1"))
            p.AddOutputSample(Sample("Output2"))
            let output2 = p.Outputs.[1] // Output2
            let nodes   = output2.UpstreamNodes()
            Expect.hasLength nodes 1 "Output2 should have exactly one upstream node (Input2)"
            Expect.equal (nodes[0].Key()) ("M:Input2") "Output2 → {Input2} in correct order"

    ]

    // ── 5.3 DownstreamProcesses / DownstreamNodes ────────────────────────────

    testList "DownstreamProcesses" [

        testCase "from root" <| fun _ ->
            let f     = makeFixtureA()
            let procs = (SampleNode f.Source1).DownstreamProcesses()
            Expect.equal (setOfNames procs) (Set.ofList ["p1";"p2";"p3"])
                "Source1 → downstream: p1, p2, p3"

        testCase "from mid" <| fun _ ->
            let f     = makeFixtureA()
            let procs = (SampleNode f.Sample1).DownstreamProcesses()
            Expect.equal (setOfNames procs) (Set.ofList ["p2";"p3"])
                "Sample1 → downstream: p2, p3"

        testCase "from leaf" <| fun _ ->
            let f     = makeFixtureA()
            let procs = (DataNode f.RawData1).DownstreamProcesses()
            Expect.equal procs.Count 0 "rawData1.csv → no downstream processes"

        testCase "distinguish by process io order" <| fun _ ->
            let p     = Process("MyProcess")
            p.AddInputSample(Sample("Input1"))
            p.AddInputSample(Sample("Input2"))
            p.AddOutputSample(Sample("Output1"))
            p.AddOutputSample(Sample("Output2"))
            let input1  = p.Inputs.[0] // Input1
            let nodes   = input1.DownstreamNodes()
            Expect.hasLength nodes 1 "Input1 should have exactly one downstream node (Output1)"
            Expect.equal (nodes[0].Key()) ("M:Output1") "Input1 → {Output1} in correct order"

    ]

    testList "DownstreamNodes" [

        testCase "from root" <| fun _ ->
            let f     = makeFixtureA()
            let nodes = (SampleNode f.Source1).DownstreamNodes()
            let keys  = nodeKeys nodes
            Expect.equal keys (Set.ofList ["M:Sample1";"M:Sample2";"D:rawData1.csv"])
                "Source1 → {Sample1, Sample2, rawData1.csv}"

        testCase "branching graph" <| fun _ ->
            let f     = makeFixtureB()
            let nodes = (SampleNode f.Sample1).DownstreamNodes()
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
            let finals = (SampleNode f.Source1).FinalNodes()
            let keys   = nodeKeys finals
            Expect.equal keys (Set.ofList ["D:rawData1.csv"])
                "FinalNodes from Source1 → {rawData1.csv}"

        testCase "RootNodes in branching graph" <| fun _ ->
            let f     = makeFixtureB()
            let roots = (SampleNode f.SampleA).RootNodes()
            let keys  = nodeKeys roots
            Expect.equal keys (Set.ofList ["M:Source1"])
                "RootNodes from SampleA in B → {Source1}"

        testCase "FinalNodes in merging graph" <| fun _ ->
            let f      = makeFixtureC()
            let finals = (SampleNode f.Source1).FinalNodes()
            let keys   = nodeKeys finals
            Expect.equal keys (Set.ofList ["M:FinalSample"])
                "FinalNodes from Source1 in C → {FinalSample}"

    ]

    // ── 5.5 UpstreamSamples / DownstreamData / etc. ─────────────────────────

    testList "Typed traversal helpers" [

        testCase "UpstreamSamples from data leaf" <| fun _ ->
            let f    = makeFixtureA()
            let mats = (DataNode f.RawData1).UpstreamSamples()
            Expect.equal (setOfSamples mats) (Set.ofList ["Source1";"Sample1";"Sample2"])
                "rawData1.csv → upstream samples: {Sample2, Sample1, Source1}"

        testCase "DownstreamData from root" <| fun _ ->
            let f    = makeFixtureA()
            let data = (SampleNode f.Source1).DownstreamData()
            Expect.equal (setOfData data) (Set.ofList ["rawData1.csv"])
                "Source1 → downstream data: {rawData1.csv}"

        testCase "ConnectedSamples from mid" <| fun _ ->
            let f    = makeFixtureA()
            let mats = (SampleNode f.Sample1).ConnectedSamples()
            Expect.equal (setOfSamples mats) (Set.ofList ["Source1";"Sample2"])
                "Sample1 connected samples: {Source1, Sample2}"

        testCase "ConnectedData returns only Data nodes" <| fun _ ->
            let f    = makeFixtureA()
            let data = (SampleNode f.Source1).ConnectedData()
            Expect.equal (setOfData data) (Set.ofList ["rawData1.csv"])
                "Source1 connected data: {rawData1.csv}"

    ]

    testList "Annotation traversal" [

        testCase "Data.UpstreamAnnotations collects process, protocol, and IONode values" <| fun _ ->
            let _, _, _, data1 = makeLinearDataAnnotationFixture()
            let pvs = data1.UpstreamAnnotations()
            Expect.equal (annotationNames pvs) expectedLinearPathPVNames
                "data1 upstream query collects all Annotations along sample1 -> process1 -> sample2 -> process2 -> data1"
            expectNoParallelLaneAnnotations pvs

        testCase "Data.UpstreamAnnotations ignores unconnected IONodes in process" <| fun _ ->
            let f = makeFixtureE()
            let data1 = f.Data1
            let pvs = data1.UpstreamAnnotations()
            Expect.hasLength pvs 5 "should have 5 PVs from the main path"
            Expect.sequenceEqual pvs [f.Source1PV; f.P1PV; f.Sample1PV; f.P2PV; f.Data1PV] "Should contain exactly the nodes from the path"

        testCase "IONode.UpstreamAnnotations on data collects all sources" <| fun _ ->
            let _, _, _, data1 = makeLinearDataAnnotationFixture()
            let pvs = (DataNode data1).UpstreamAnnotations()
            Expect.equal (annotationNames pvs) expectedLinearPathPVNames
                "DataNode upstream query has the same complete source coverage"
            expectNoParallelLaneAnnotations pvs

        testCase "Sample.DownstreamAnnotations collects process, protocol, and IONode values" <| fun _ ->
            let _, sample1, _, _ = makeLinearDataAnnotationFixture()
            let pvs = sample1.DownstreamAnnotations()
            Expect.equal (annotationNames pvs) expectedLinearPathPVNames
                "sample1 downstream query collects all Annotations along the full path"
            expectNoParallelLaneAnnotations pvs

        testCase "Sample.DownstreamAnnotations ignores unconnected IONodes in process" <| fun _ ->
            let f = makeFixtureE()
            let sample1 = f.Source1
            let pvs = sample1.DownstreamAnnotations()
            Expect.hasLength pvs 5 "should have 5 PVs from the main path"
            Expect.sequenceEqual pvs [f.Source1PV; f.P1PV; f.Sample1PV; f.P2PV; f.Data1PV] "Should contain exactly the nodes from the path"

        testCase "Sample.DownstreamAnnotations does not walk upstream" <| fun _ ->
            let f = makeFixtureE()
            let sample1 = f.Sample1
            let pvs = sample1.DownstreamAnnotations()
            Expect.hasLength pvs 3 "should have 3 PVs from the main path"
            Expect.sequenceEqual pvs [f.Sample1PV; f.P2PV; f.Data1PV] "Should contain exactly the nodes from the path"

        testCase "IONode.DownstreamAnnotations on sample collects all sources" <| fun _ ->
            let _, sample1, _, _ = makeLinearDataAnnotationFixture()
            let pvs = (SampleNode sample1).DownstreamAnnotations()
            Expect.equal (annotationNames pvs) expectedLinearPathPVNames
                "SampleNode downstream query has the same complete source coverage"
            expectNoParallelLaneAnnotations pvs

        testCase "Sample.AllAnnotations collects process, protocol, and IONode values" <| fun _ ->
            let _, _, sample2, _ = makeLinearDataAnnotationFixture()
            let pvs = sample2.AllAnnotations()
            Expect.equal (annotationNames pvs) expectedLinearPathPVNames
                "all-connected query from the middle node collects all sources in both directions"
            expectNoParallelLaneAnnotations pvs

        testCase "Dataset.UpstreamAnnotationsForNode on data collects all sources" <| fun _ ->
            let ds, _, _, data1 = makeLinearDataAnnotationFixture()
            let pvs = ds.UpstreamAnnotationsForNode(DataNode data1)
            Expect.equal (annotationNames pvs) expectedLinearPathPVNames
                "dataset-scoped upstream query keeps complete property value coverage"
            expectNoParallelLaneAnnotations pvs

        testCase "Dataset.DownstreamAnnotationsForNode on sample collects all sources" <| fun _ ->
            let ds, sample1, _, _ = makeLinearDataAnnotationFixture()
            let pvs = ds.DownstreamAnnotationsForNode(SampleNode sample1)
            Expect.equal (annotationNames pvs) expectedLinearPathPVNames
                "dataset-scoped downstream query keeps complete property value coverage"
            expectNoParallelLaneAnnotations pvs

        testCase "Dataset.AnnotationsForNode all directions collects all sources" <| fun _ ->
            let ds, _, sample2, _ = makeLinearDataAnnotationFixture()
            let pvs = ds.AnnotationsForNode(SampleNode sample2)
            Expect.equal (annotationNames pvs) expectedLinearPathPVNames
                "dataset-scoped upstream + downstream query keeps complete property value coverage"
            expectNoParallelLaneAnnotations pvs

        testCase "Path.AllAnnotations collects all sources on the path" <| fun _ ->
            let ds, _, _, _ = makeLinearDataAnnotationFixture()
            let path = Path(ds.AllProcesses())
            let pvs = path.AllAnnotations()
            let expected =
                Set.union
                    expectedLinearPathPVNames
                    unrelatedParallelLanePVNames
            Expect.equal (annotationNames pvs) expected
                "explicit Path query includes every IO node on the supplied process path"

    ]

]
