module ProcessCore.Tests.Integration

open Fable.Pyxpecto
open ProcessCore
open ProcessCore.Tests.Fixtures

// ─────────────────────────────────────────────────────────────────────────────
// Proteomics-style multi-step dataset used across all integration tests.
//
//   Source1 ──[growth]──► Sample1 ──[digestion]──► Sample2 ──[measurement]──► rawData1.csv
//
//   growth protocol      — IntendedUse = "cell growth"
//                          ParameterValues: temperature="37" (°C), rpm="200" (rpm)
//   digestion protocol   — IntendedUse = "protein digestion"
//                          ParameterValue: enzyme="Trypsin" (term, TAN)
//   measurement process  — no protocol
//
// The fixture is re-created inside every test (makeFixtureA()) so tests are
// fully independent and free of shared-state coupling.
// ─────────────────────────────────────────────────────────────────────────────

let tests = testList "Integration" [

    // ─── Use-case 1: growth temperature filter ────────────────────────────────
    //
    // "Give me all materials that result from a 'cell growth' process where
    //  temperature = 37°C."
    //
    // Expected: Sample1 (direct output of the growth process).
    // rawData1.csv is Data, not Material; Sample2 is consumed before it, so it
    // would only appear if it is not re-consumed by another process. In Fixture A,
    // Sample2 IS consumed by p3 → only Sample1 is a terminal material output.
    // Wait — let's trace: growth(p1) → output = Sample1;
    //   downstream from p1: p2 consumes Sample1 → p3 consumes Sample2 → rawData1.csv
    // Terminal outputs not consumed in subgraph = nodes not input to any other
    // subgraph process.  All material outputs of p1, p2, p3:
    //   p1→Sample1 (consumed by p2 ∈ subgraph), p2→Sample2 (consumed by p3 ∈ subgraph), p3→rawData1 (Data).
    // So no terminal Materials → the query returns empty for Fixture A's default
    // temperature.  Use Fixture B (branching) instead, where Sample1 → SampleA and SampleB
    // are NOT consumed by any further process.

    testCase "use-case 1 — growth temperature filter" <| fun _ ->
        // Fixture B: Source1 --[p1 growth@37°C]--> Sample1 --[p2]--> SampleA
        //                                                   --[p3]--> SampleB
        // p1 protocol IntendedUse="cell growth", parameter temperature=37°C
        // SampleA and SampleB are terminal → both should appear
        let f = makeFixtureB()
        let results = f.DS.MaterialsResultingFromCondition("cell growth", "temperature", "37")
        let names = results |> Seq.map (fun m -> m.Name) |> Set.ofSeq
        Expect.isTrue  (names.Contains("SampleA")) "SampleA downstream of 37°C growth"
        Expect.isTrue  (names.Contains("SampleB")) "SampleB downstream of 37°C growth"
        Expect.isFalse (names.Contains("Source1")) "Source1 is upstream, not downstream"

    testCase "use-case 1 — wrong temperature returns empty" <| fun _ ->
        let f = makeFixtureB()
        let results = f.DS.MaterialsResultingFromCondition("cell growth", "temperature", "4")
        Expect.equal results.Count 0 "no results for non-matching temperature"

    // ─── Use-case 2: all parameters for a sample ─────────────────────────────
    //
    // "Give me all parameters connected to Sample1 through the process graph."
    //
    // In Fixture A:
    //   upstream of Sample1  → p1 (temperature=37°C, rpm=200rpm)
    //   downstream of Sample1 → p2 (enzyme=Trypsin), p3 (no PV)
    // Expected PV names: temperature, rpm, enzyme

    testCase "use-case 2 — all parameters for a sample" <| fun _ ->
        let f = makeFixtureA()
        let node = MaterialNode f.Sample1
        let pvs  = f.DS.PropertyValuesForNode(node)
        let names = pvs |> Seq.map (fun pv -> pv.Name) |> Set.ofSeq
        Expect.isTrue (names.Contains("temperature")) "temperature PV included"
        Expect.isTrue (names.Contains("rpm"))         "rpm PV included"
        Expect.isTrue (names.Contains("enzyme"))      "enzyme PV included"
        Expect.equal  (pvs.Count) 3                   "exactly 3 PVs"

    testCase "use-case 2 — scoped to dataset excludes other datasets" <| fun _ ->
        // Construct an unrelated dataset with its own processes carrying same PV names
        let s = Material("Sx", additionalType = "Source")
        let o = Material("Ox", additionalType = "Sample")
        let px = LabProcess("px")
        px.AddInputMaterial(s)
        px.AddOutputMaterial(o)
        px.AddParameterValue(PropertyValue("temperature", value = "100", unit = "°C", additionalType = "ParameterValue"))
        let dsX = Dataset("DS-X")
        dsX.AddProcess(px)
        // The result from Fixture A's dataset must NOT include the "100°C" value
        let f = makeFixtureA()
        let node = MaterialNode f.Sample1
        let pvs  = f.DS.PropertyValuesForNode(node)
        let vals = pvs |> Seq.filter (fun pv -> pv.Name = "temperature") |> Seq.map (fun pv -> pv.Value) |> Seq.toList
        Expect.equal vals [Some "37"] "only Fixture A's temperature, not DS-X's"

    // ─── Use-case 3: all connected samples ───────────────────────────────────
    //
    // "Give me all nodes connected to Sample1 through the process graph."
    //
    // In Fixture A (linear chain), starting from Sample1:
    //   AllConnectedNodes returns all other nodes: Source1, Sample2, rawData1.csv
    //   (the node itself is excluded per AllConnectedNodes contract)

    testCase "use-case 3 — all connected nodes from mid-graph sample" <| fun _ ->
        let f = makeFixtureA()
        let connected = (MaterialNode f.Sample1).AllConnectedNodes()
        let keys = connected |> Seq.map (fun n -> n.Key()) |> Set.ofSeq
        // Must include Source1, Sample2, rawData1.csv; must NOT include Sample1 itself
        Expect.isTrue  (keys.Contains("M:Source1"))       "Source1 connected"
        Expect.isTrue  (keys.Contains("M:Sample2"))       "Sample2 connected"
        Expect.isTrue  (keys.Contains("D:rawData1.csv"))  "rawData1.csv connected"
        Expect.isFalse (keys.Contains("M:Sample1"))       "Sample1 itself excluded"

    testCase "use-case 3 — scoped to dataset" <| fun _ ->
        // Fixture D has two child datasets sharing Sample1.
        // Scoped to child1 only p1 is visible; connected nodes from Sample1 = {Source1}.
        let f = makeFixtureD()
        let scope = f.Child1.Processes
        let connected = (MaterialNode f.Sample1).AllConnectedNodes(scope = scope)
        let keys = connected |> Seq.map (fun n -> n.Key()) |> Set.ofSeq
        Expect.isTrue  (keys.Contains("M:Source1"))       "Source1 in child1 scope"
        Expect.isFalse (keys.Contains("D:rawData1.csv"))  "rawData1.csv not in child1 scope"

    // ─── ProcessGraph.PathsThrough — multi-path proteomics ───────────────────
    //
    // Build an investigation-level flat process list combining two parallel
    // experimental arms feeding the same final measurement step.
    //
    //   SourceA --[growth_a]--> SampleA --\
    //                                      [measurement]--> rawData.csv
    //   SourceB --[growth_b]--> SampleB --/
    //
    // PathsThrough(rawData.csv) should yield two distinct paths:
    //   [growth_a, measurement]  and  [growth_b, measurement]

    testCase "ProcessGraph.PathsThrough — multi-path proteomics" <| fun _ ->
        let sourceA = Material("SourceA", additionalType = "Source")
        let sampleA = Material("SampleA", additionalType = "Sample")
        let sourceB = Material("SourceB", additionalType = "Source")
        let sampleB = Material("SampleB", additionalType = "Sample")
        let raw     = Data("rawData.csv")

        let growthA = LabProcess("growth_a")
        growthA.AddInputMaterial(sourceA)
        growthA.AddOutputMaterial(sampleA)

        let growthB = LabProcess("growth_b")
        growthB.AddInputMaterial(sourceB)
        growthB.AddOutputMaterial(sampleB)

        let measurement = LabProcess("measurement")
        measurement.AddInputMaterial(sampleA)
        measurement.AddInputMaterial(sampleB)
        measurement.AddOutputData(raw)

        let ds = Dataset("investigation")
        ds.AddProcess(growthA)
        ds.AddProcess(growthB)
        ds.AddProcess(measurement)

        let graph = ProcessGraph(ds.AllProcesses())
        let paths = graph.PathsThrough(DataNode raw)

        // Each path should contain the measurement process and exactly one growth process
        let pathNames =
            paths
            |> Seq.map (fun path -> path.Processes |> Seq.map (fun p -> p.Name) |> Set.ofSeq)
            |> Seq.toList

        Expect.equal (paths.Count) 2 "two paths through rawData.csv"

        let hasGrowthA = pathNames |> List.exists (fun s -> s.Contains("growth_a"))
        let hasGrowthB = pathNames |> List.exists (fun s -> s.Contains("growth_b"))
        Expect.isTrue hasGrowthA "path through growth_a exists"
        Expect.isTrue hasGrowthB "path through growth_b exists"

        for path in paths do
            let pnames = path.Processes |> Seq.map (fun p -> p.Name) |> Set.ofSeq
            Expect.isTrue (pnames.Contains("measurement")) "measurement in every path"

]
