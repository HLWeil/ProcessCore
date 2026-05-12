module ProcessCore.Tests.Graph.Traversal

open Fable.Pyxpecto
open ProcessCore
open ProcessCore.Tests.Fixtures

// helpers
let setOfNames (procs: ResizeArray<LabProcess>) = procs |> Seq.map (fun p -> p.Name) |> Set.ofSeq
let setOfMaterials (ms: ResizeArray<Material>) = ms |> Seq.map (fun m -> m.Name) |> Set.ofSeq
let setOfData (ds: ResizeArray<Data>) = ds |> Seq.map (fun d -> d.Path) |> Set.ofSeq
let nodeKeys (ns: ResizeArray<IONode>) = ns |> Seq.map (fun n -> n.Key()) |> Set.ofSeq
let propertyValueNames (pvs: ResizeArray<PropertyValue>) = pvs |> Seq.map (fun pv -> pv.Name) |> Set.ofSeq

let private makeLinearDataPropertyValueFixture () =
    let material1 = Material("PV_Material1")
    material1.AddAdditionalProperty(PropertyValue("material1_characteristic", value = "m1"))

    let material2 = Material("PV_Material2")
    material2.AddAdditionalProperty(PropertyValue("material2_characteristic", value = "m2"))

    let data1 = Data("pv-data1.csv")
    data1.AddAdditionalProperty(PropertyValue("data1_property", value = "d1"))

    let material3 = Material("PV_Material3")
    material3.AddAdditionalProperty(PropertyValue("material3_characteristic", value = "m3"))

    let material4 = Material("PV_Material4")
    material4.AddAdditionalProperty(PropertyValue("material4_characteristic", value = "m4"))

    let data2 = Data("pv-data2.csv")
    data2.AddAdditionalProperty(PropertyValue("data2_property", value = "d2"))

    let protocol1 = LabProtocol("pv-protocol-1")
    protocol1.AddLabEquipment(PropertyValue("protocol1_component", value = "instrument-1"))

    let process1 = LabProcess("pv-process-1")
    process1.ExecutesProtocol <- Some protocol1
    process1.AddParameterValue(PropertyValue("process1_parameter", value = "p1"))
    process1.AddInputMaterial(material1)
    process1.AddInputMaterial(material3)
    process1.AddOutputMaterial(material2)
    process1.AddOutputMaterial(material4)

    let protocol2 = LabProtocol("pv-protocol-2")
    protocol2.AddLabEquipment(PropertyValue("protocol2_component", value = "instrument-2"))

    let process2 = LabProcess("pv-process-2")
    process2.ExecutesProtocol <- Some protocol2
    process2.AddParameterValue(PropertyValue("process2_parameter", value = "p2"))
    process2.AddInputMaterial(material2)
    process2.AddInputMaterial(material4)
    process2.AddOutputData(data1)
    process2.AddOutputData(data2)

    let dataset = Dataset("PV-linear")
    dataset.AddProcess(process1)
    dataset.AddProcess(process2)

    dataset, material1, material2, data1

let private expectedLinearPathPVNames =
    Set.ofList [
        "material1_characteristic"
        "material2_characteristic"
        "data1_property"
        "process1_parameter"
        "process2_parameter"
        "protocol1_component"
        "protocol2_component"
    ]

let private unrelatedParallelLanePVNames =
    Set.ofList [
        "material3_characteristic"
        "material4_characteristic"
        "data2_property"
    ]

let private expectNoParallelLanePropertyValues (pvs: ResizeArray<PropertyValue>) =
    let names = propertyValueNames pvs
    for name in unrelatedParallelLanePVNames do
        Expect.isFalse (names.Contains name) $"should not include {name} from the parallel IO pair"

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

    testList "PropertyValue traversal" [

        testCase "Data.UpstreamPropertyValues collects process, protocol, and IONode values" <| fun _ ->
            let _, _, _, data1 = makeLinearDataPropertyValueFixture()
            let pvs = data1.UpstreamPropertyValues()
            Expect.equal (propertyValueNames pvs) expectedLinearPathPVNames
                "data1 upstream query collects all PropertyValues along material1 -> process1 -> material2 -> process2 -> data1"
            expectNoParallelLanePropertyValues pvs

        testCase "Data.UpstreamPropertyValues ignores unconnected IONodes in process" <| fun _ ->
            let f = makeFixtureE()
            let data1 = f.Data1
            let pvs = data1.UpstreamPropertyValues()
            Expect.hasLength pvs 5 "should have 5 PVs from the main path"
            Expect.sequenceEqual pvs [f.Source1PV; f.P1PV; f.Sample1PV; f.P2PV; f.Data1PV] "Should contain exactly the nodes from the path"

        testCase "IONode.UpstreamPropertyValues on data collects all sources" <| fun _ ->
            let _, _, _, data1 = makeLinearDataPropertyValueFixture()
            let pvs = (DataNode data1).UpstreamPropertyValues()
            Expect.equal (propertyValueNames pvs) expectedLinearPathPVNames
                "DataNode upstream query has the same complete source coverage"
            expectNoParallelLanePropertyValues pvs

        testCase "Material.DownstreamPropertyValues collects process, protocol, and IONode values" <| fun _ ->
            let _, material1, _, _ = makeLinearDataPropertyValueFixture()
            let pvs = material1.DownstreamPropertyValues()
            Expect.equal (propertyValueNames pvs) expectedLinearPathPVNames
                "material1 downstream query collects all PropertyValues along the full path"
            expectNoParallelLanePropertyValues pvs

        testCase "Material.DownstreamPropertyValues ignores unconnected IONodes in process" <| fun _ ->
            let f = makeFixtureE()
            let material1 = f.Source1
            let pvs = material1.DownstreamPropertyValues()
            Expect.hasLength pvs 5 "should have 5 PVs from the main path"
            Expect.sequenceEqual pvs [f.Source1PV; f.P1PV; f.Sample1PV; f.P2PV; f.Data1PV] "Should contain exactly the nodes from the path"

        testCase "Material.DownstreamPropertyValues does not walk upstream" <| fun _ ->
            let f = makeFixtureE()
            let material1 = f.Sample1
            let pvs = material1.DownstreamPropertyValues()
            Expect.hasLength pvs 3 "should have 3 PVs from the main path"
            Expect.sequenceEqual pvs [f.Sample1PV; f.P2PV; f.Data1PV] "Should contain exactly the nodes from the path"

        testCase "IONode.DownstreamPropertyValues on material collects all sources" <| fun _ ->
            let _, material1, _, _ = makeLinearDataPropertyValueFixture()
            let pvs = (MaterialNode material1).DownstreamPropertyValues()
            Expect.equal (propertyValueNames pvs) expectedLinearPathPVNames
                "MaterialNode downstream query has the same complete source coverage"
            expectNoParallelLanePropertyValues pvs

        testCase "Material.AllPropertyValues collects process, protocol, and IONode values" <| fun _ ->
            let _, _, material2, _ = makeLinearDataPropertyValueFixture()
            let pvs = material2.AllPropertyValues()
            Expect.equal (propertyValueNames pvs) expectedLinearPathPVNames
                "all-connected query from the middle node collects all sources in both directions"
            expectNoParallelLanePropertyValues pvs

        testCase "Dataset.UpstreamPropertyValuesForNode on data collects all sources" <| fun _ ->
            let ds, _, _, data1 = makeLinearDataPropertyValueFixture()
            let pvs = ds.UpstreamPropertyValuesForNode(DataNode data1)
            Expect.equal (propertyValueNames pvs) expectedLinearPathPVNames
                "dataset-scoped upstream query keeps complete property value coverage"
            expectNoParallelLanePropertyValues pvs

        testCase "Dataset.DownstreamPropertyValuesForNode on material collects all sources" <| fun _ ->
            let ds, material1, _, _ = makeLinearDataPropertyValueFixture()
            let pvs = ds.DownstreamPropertyValuesForNode(MaterialNode material1)
            Expect.equal (propertyValueNames pvs) expectedLinearPathPVNames
                "dataset-scoped downstream query keeps complete property value coverage"
            expectNoParallelLanePropertyValues pvs

        testCase "Dataset.PropertyValuesForNode all directions collects all sources" <| fun _ ->
            let ds, _, material2, _ = makeLinearDataPropertyValueFixture()
            let pvs = ds.PropertyValuesForNode(MaterialNode material2)
            Expect.equal (propertyValueNames pvs) expectedLinearPathPVNames
                "dataset-scoped upstream + downstream query keeps complete property value coverage"
            expectNoParallelLanePropertyValues pvs

        testCase "Path.AllPropertyValues collects all sources on the path" <| fun _ ->
            let ds, _, _, _ = makeLinearDataPropertyValueFixture()
            let path = Path(ds.AllProcesses())
            let pvs = path.AllPropertyValues()
            let expected =
                Set.union
                    expectedLinearPathPVNames
                    unrelatedParallelLanePVNames
            Expect.equal (propertyValueNames pvs) expected
                "explicit Path query includes every IO node on the supplied process path"

    ]

]
