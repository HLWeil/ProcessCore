module ProcessCore.Tests.Graph.DatasetQueries

open Fable.Pyxpecto
open ProcessCore
open ProcessCore.Tests.Fixtures

let tests = testList "DatasetQueries" [

    // ── AllProcesses ──────────────────────────────────────────────────────────

    testCase "AllProcesses — flat dataset" <| fun _ ->
        let f = makeFixtureA()
        let procs = f.DS.AllProcesses()
        let names = procs |> Seq.map (fun p -> p.Name) |> Set.ofSeq
        Expect.equal names (Set.ofList ["p1";"p2";"p3"]) "DS-A has exactly p1, p2, p3"

    testCase "AllProcesses — nested datasets" <| fun _ ->
        let f = makeFixtureD()
        let procs = f.Parent.AllProcesses()
        let names = procs |> Seq.map (fun p -> p.Name) |> Set.ofSeq
        Expect.equal names (Set.ofList ["p1";"p2"]) "parent collects from both children"

    // ── AllMaterials / AllData / AllNodes ─────────────────────────────────────

    testCase "AllMaterials deduplicates shared nodes" <| fun _ ->
        let f = makeFixtureD()
        let mats = f.Parent.AllMaterials()
        let names = mats |> Seq.map (fun m -> m.Name) |> Set.ofSeq
        // Sample1 appears in both children but should be in the result only once
        Expect.equal names (Set.ofList ["Source1";"Sample1"]) "Sample1 counted once"
        Expect.equal mats.Count 2 "exactly 2 distinct materials"

    testCase "AllData" <| fun _ ->
        let f = makeFixtureA()
        let data = f.DS.AllData()
        let paths = data |> Seq.map (fun d -> d.Path) |> Set.ofSeq
        Expect.equal paths (Set.ofList ["rawData1.csv"]) "rawData1.csv is in AllData"

    testCase "AllNodes includes both types" <| fun _ ->
        let f = makeFixtureA()
        let nodes = f.DS.AllNodes()
        // Source1, Sample1, Sample2, rawData1.csv
        Expect.equal nodes.Count 4 "4 distinct nodes in DS-A"

    // ── RootNodes / FinalNodes ────────────────────────────────────────────────

    testCase "RootNodes" <| fun _ ->
        let f = makeFixtureA()
        let roots = f.DS.RootNodes()
        let keys  = roots |> Seq.map (fun n -> n.Key()) |> Set.ofSeq
        Expect.equal keys (Set.ofList ["M:Source1"]) "Source1 is the only root"

    testCase "FinalNodes" <| fun _ ->
        let f = makeFixtureA()
        let finals = f.DS.FinalNodes()
        let keys   = finals |> Seq.map (fun n -> n.Key()) |> Set.ofSeq
        Expect.equal keys (Set.ofList ["D:rawData1.csv"]) "rawData1.csv is the only final node"

    testCase "RootMaterials" <| fun _ ->
        let f    = makeFixtureA()
        let mats = f.DS.RootMaterials()
        let names = mats |> Seq.map (fun m -> m.Name) |> Set.ofSeq
        Expect.equal names (Set.ofList ["Source1"]) "Source1 is the root material"

    testCase "RootData is empty for DS-A" <| fun _ ->
        let f = makeFixtureA()
        Expect.equal (f.DS.RootData().Count) 0 "no Data root nodes in DS-A"

    testCase "FinalMaterials is empty for DS-A" <| fun _ ->
        let f = makeFixtureA()
        Expect.equal (f.DS.FinalMaterials().Count) 0 "no Material final nodes in DS-A"

    testCase "FinalData" <| fun _ ->
        let f    = makeFixtureA()
        let data = f.DS.FinalData()
        let paths = data |> Seq.map (fun d -> d.Path) |> Set.ofSeq
        Expect.equal paths (Set.ofList ["rawData1.csv"]) "rawData1.csv is the final data node"

    // ── AllPropertyValues ─────────────────────────────────────────────────────

    testCase "AllPropertyValues — no filter" <| fun _ ->
        let f  = makeFixtureA()
        let pvs = f.DS.AllPropertyValues()
        let names = pvs |> Seq.map (fun pv -> pv.Name) |> Set.ofSeq
        // p1: temperature, rpm; p2: enzyme; p3: nothing
        Expect.isTrue (names.Contains "temperature") "temperature PV present"
        Expect.isTrue (names.Contains "rpm")         "rpm PV present"
        Expect.isTrue (names.Contains "enzyme")      "enzyme PV present"

    testCase "AllPropertyValues — protocolName filter" <| fun _ ->
        let f   = makeFixtureA()
        let pvs = f.DS.AllPropertyValues(protocolName = "extraction")
        let names = pvs |> Seq.map (fun pv -> pv.Name) |> Set.ofSeq
        Expect.isTrue  (names.Contains "temperature") "temperature in extraction"
        Expect.isTrue  (names.Contains "rpm")         "rpm in extraction"
        Expect.isFalse (names.Contains "enzyme")      "enzyme not in extraction"

    // ── PropertyValuesForNode ─────────────────────────────────────────────────

    testCase "PropertyValuesForNode — upstream + downstream" <| fun _ ->
        let f   = makeFixtureA()
        // Sample1 is output of p1 (upstream) and input of p2 (downstream)
        let pvs = f.DS.PropertyValuesForNode(MaterialNode f.Sample1)
        let names = pvs |> Seq.map (fun pv -> pv.Name) |> Set.ofSeq
        Expect.isTrue (names.Contains "temperature") "upstream p1 parameter present"
        Expect.isTrue (names.Contains "rpm")         "upstream p1 parameter present"
        Expect.isTrue (names.Contains "enzyme")      "downstream p2 parameter present"

    testCase "UpstreamPropertyValuesForNode" <| fun _ ->
        let f    = makeFixtureA()
        // From Sample2 (output of p2, input of p3): upstream → p2 + p1
        let pvs  = f.DS.UpstreamPropertyValuesForNode(MaterialNode f.Sample2)
        let names = pvs |> Seq.map (fun pv -> pv.Name) |> Set.ofSeq
        Expect.isTrue  (names.Contains "enzyme")      "p2 enzyme is upstream"
        Expect.isTrue  (names.Contains "temperature") "p1 temperature is upstream"

    testCase "DownstreamPropertyValuesForNode" <| fun _ ->
        let f   = makeFixtureA()
        // From Sample1: downstream → p2 (enzyme), p3 (nothing)
        let pvs = f.DS.DownstreamPropertyValuesForNode(MaterialNode f.Sample1)
        let names = pvs |> Seq.map (fun pv -> pv.Name) |> Set.ofSeq
        Expect.isTrue  (names.Contains "enzyme")      "p2 enzyme is downstream"
        Expect.isFalse (names.Contains "temperature") "p1 temperature is upstream, not downstream"

    // ── FindProcessesByProtocolType ───────────────────────────────────────────

    testCase "FindProcessesByProtocolType" <| fun _ ->
        let f     = makeFixtureA()
        let procs = f.DS.FindProcessesByProtocolType("cell growth")
        let names = procs |> Seq.map (fun p -> p.Name) |> Set.ofSeq
        Expect.equal names (Set.ofList ["p1"]) "only p1 has intendedUse=cell growth"

    testCase "FindProcessesByProtocolType — no match" <| fun _ ->
        let f     = makeFixtureA()
        let procs = f.DS.FindProcessesByProtocolType("unknown-type")
        Expect.equal procs.Count 0 "unknown protocol type → empty"

    // ── FindProcessesByPropertyValue ──────────────────────────────────────────

    testCase "FindProcessesByPropertyValue — param source" <| fun _ ->
        let f     = makeFixtureA()
        let procs = f.DS.FindProcessesByPropertyValue("temperature", "37")
        let names = procs |> Seq.map (fun p -> p.Name) |> Set.ofSeq
        Expect.equal names (Set.ofList ["p1"]) "p1 has temperature=37"

    testCase "FindProcessesByPropertyValue — input node source" <| fun _ ->
        // Construct a process whose input material has an AdditionalProperty
        let mat = Material("TestMat")
        mat.AddAdditionalProperty(PropertyValue("organism", value = "Mouse", additionalType = "CharacteristicValue"))
        let proc = LabProcess("proc-char")
        proc.AddInputMaterial(mat)
        let ds = Dataset("DS-char")
        ds.AddProcess(proc)
        let procs = ds.FindProcessesByPropertyValue("organism", "Mouse")
        let names = procs |> Seq.map (fun p -> p.Name) |> Set.ofSeq
        Expect.equal names (Set.ofList ["proc-char"]) "found via input node AdditionalProperty"

    testCase "FindProcessesByPropertyValue — output node source" <| fun _ ->
        let mat = Material("OutMat")
        mat.AddAdditionalProperty(PropertyValue("growth_phase", value = "log", additionalType = "FactorValue"))
        let proc = LabProcess("proc-factor")
        proc.AddOutputMaterial(mat)
        let ds = Dataset("DS-factor")
        ds.AddProcess(proc)
        let procs = ds.FindProcessesByPropertyValue("growth_phase", "log")
        let names = procs |> Seq.map (fun p -> p.Name) |> Set.ofSeq
        Expect.equal names (Set.ofList ["proc-factor"]) "found via output node AdditionalProperty"

    testCase "FindProcessesByPropertyValue — protocol component source" <| fun _ ->
        let proto = LabProtocol("instrument-protocol")
        proto.AddLabEquipment(PropertyValue("instrument", value = "Orbitrap", additionalType = "Component"))
        let proc = LabProcess("proc-comp")
        proc.ExecutesProtocol <- Some proto
        let ds = Dataset("DS-comp")
        ds.AddProcess(proc)
        let procs = ds.FindProcessesByPropertyValue("instrument", "Orbitrap")
        let names = procs |> Seq.map (fun p -> p.Name) |> Set.ofSeq
        Expect.equal names (Set.ofList ["proc-comp"]) "found via protocol LabEquipment"

    // ── FindProcessesByPropertyName ───────────────────────────────────────────

    testCase "FindProcessesByPropertyName" <| fun _ ->
        let f     = makeFixtureA()
        // temperature exists on p1 regardless of value
        let procs = f.DS.FindProcessesByPropertyName("temperature")
        let names = procs |> Seq.map (fun p -> p.Name) |> Set.ofSeq
        Expect.equal names (Set.ofList ["p1"]) "p1 has temperature (by name only)"

    // ── MaterialsResultingFromCondition ───────────────────────────────────────

    testCase "MaterialsResultingFromCondition — use-case 1" <| fun _ ->
        let f    = makeFixtureA()
        // Protocol type = "cell growth", param temperature = 37
        // Qualifying process = p1. p1's downstream subgraph: p1→p2→p3.
        // Terminal output of subgraph = rawData1.csv (DataNode) → excluded from Material results.
        // NOTE: expected is [] — the terminal output is a DataNode, not a Material.
        let mats = f.DS.MaterialsResultingFromCondition("cell growth", "temperature", "37")
        Expect.equal mats.Count 0
            "terminal output is rawData1.csv (DataNode), no Material terminal outputs"

    testCase "MaterialsResultingFromCondition — no qualifying process" <| fun _ ->
        let f    = makeFixtureA()
        let mats = f.DS.MaterialsResultingFromCondition("unknown-type", "temperature", "37")
        Expect.equal mats.Count 0 "no qualifying processes → empty"

    testCase "MaterialsResultingFromCondition — branching downstream" <| fun _ ->
        let f    = makeFixtureB()
        // p1 is qualifying: protocol type "cell growth", temperature=37
        // p1's downstream subgraph: p1→p2 and p1→p3
        // Terminal outputs: SampleA (output of p2, no successor) and SampleB (output of p3, no successor)
        let mats = f.DS.MaterialsResultingFromCondition("cell growth", "temperature", "37")
        let names = mats |> Seq.map (fun m -> m.Name) |> Set.ofSeq
        Expect.equal names (Set.ofList ["SampleA";"SampleB"])
            "branching: both terminal output materials returned"

    testCase "MaterialsResultingFromCondition — predicate overload" <| fun _ ->
        let f = makeFixtureB()
        let pred (pv: PropertyValue) = pv.Value = Some "37"
        let mats = f.DS.MaterialsResultingFromCondition("cell growth", pred)
        let names = mats |> Seq.map (fun m -> m.Name) |> Set.ofSeq
        Expect.equal names (Set.ofList ["SampleA";"SampleB"])
            "predicate overload returns both terminal branch materials"

    // ── Dataset-scoped node/path queries ──────────────────────────────────────

    testCase "ProcessesForNode" <| fun _ ->
        let f = makeFixtureA()
        let procs = f.DS.ProcessesForNode(MaterialNode f.Sample1)
        let names = procs |> Seq.map (fun p -> p.Name) |> Set.ofSeq
        Expect.equal names (Set.ofList ["p1";"p2"]) "Sample1 is in p1 and p2"

    testCase "PathsThrough — linear graph" <| fun _ ->
        let f = makeFixtureA()
        let paths = f.DS.PathsThrough(MaterialNode f.Sample1)
        Expect.equal paths.Count 2 "two seed processes each produce one maximal path"
        for path in paths do
            let names = path.Processes |> Seq.map (fun p -> p.Name) |> Set.ofSeq
            Expect.equal names (Set.ofList ["p1";"p2";"p3"]) "each path covers all three processes"

    testCase "PathsThrough — branching graph" <| fun _ ->
        let f = makeFixtureB()
        let paths = f.DS.PathsThrough(MaterialNode f.Sample1)
        Expect.equal paths.Count 4 "branching: three seeds produce four paths"
        let uniqueSets =
            paths
            |> Seq.map (fun p -> p.Processes |> Seq.map (fun x -> x.Name) |> Set.ofSeq)
            |> Set.ofSeq
        Expect.equal uniqueSets (Set.ofList [Set.ofList ["p1";"p2"]; Set.ofList ["p1";"p3"]])
            "two distinct path shapes"

    testCase "NodesDownstreamOf" <| fun _ ->
        let f = makeFixtureA()
        let nodes = f.DS.NodesDownstreamOf(MaterialNode f.Source1)
        let keys = nodes |> Seq.map (fun n -> n.Key()) |> Set.ofSeq
        Expect.equal keys (Set.ofList ["M:Sample1";"M:Sample2";"D:rawData1.csv"])
            "downstream nodes exclude the query node itself"

    testCase "NodesUpstreamOf" <| fun _ ->
        let f = makeFixtureA()
        let nodes = f.DS.NodesUpstreamOf(DataNode f.RawData1)
        let keys = nodes |> Seq.map (fun n -> n.Key()) |> Set.ofSeq
        Expect.equal keys (Set.ofList ["M:Source1";"M:Sample1";"M:Sample2"])
            "all material nodes upstream from rawData1.csv"

    testCase "ConnectedMaterialsForNode excludes query node" <| fun _ ->
        let f = makeFixtureA()
        let mats = f.DS.ConnectedMaterialsForNode(MaterialNode f.Sample1)
        let names = mats |> Seq.map (fun m -> m.Name) |> Set.ofSeq
        Expect.equal names (Set.ofList ["Source1";"Sample2"])
            "IONode-owned connected-node contract excludes Sample1 itself"

    testCase "ProtocolParametersForNode" <| fun _ ->
        let f = makeFixtureA()
        let fps = f.DS.ProtocolParametersForNode(MaterialNode f.Sample1)
        let names = fps |> Seq.map (fun fp -> fp.Name) |> Set.ofSeq
        Expect.isTrue (names.Contains "temperature") "temperature FP from p1"
        Expect.isTrue (names.Contains "rpm") "rpm FP from p1"

]
