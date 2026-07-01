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
    // "Give me all samples that result from a 'cell growth' process where
    //  temperature = 37°C."
    //
    // Expected: Sample1 (direct output of the growth process).
    // rawData1.csv is Data, not Sample; Sample2 is consumed before it, so it
    // would only appear if it is not re-consumed by another process. In Fixture A,
    // Sample2 IS consumed by p3 → only Sample1 is a terminal sample output.
    // Wait — let's trace: growth(p1) → output = Sample1;
    //   downstream from p1: p2 consumes Sample1 → p3 consumes Sample2 → rawData1.csv
    // Terminal outputs not consumed in subgraph = nodes not input to any other
    // subgraph process.  All sample outputs of p1, p2, p3:
    //   p1→Sample1 (consumed by p2 ∈ subgraph), p2→Sample2 (consumed by p3 ∈ subgraph), p3→rawData1 (Data).
    // So no terminal Samples → the query returns empty for Fixture A's default
    // temperature.  Use Fixture B (branching) instead, where Sample1 → SampleA and SampleB
    // are NOT consumed by any further process.

    testCase "use-case 1 — growth temperature filter" <| fun _ ->
        // Fixture B: Source1 --[p1 growth@37°C]--> Sample1 --[p2]--> SampleA
        //                                                   --[p3]--> SampleB
        // p1 protocol IntendedUse="cell growth", parameter temperature=37°C
        // SampleA and SampleB are terminal → both should appear
        let f = makeFixtureB()
        let results = f.DS.SamplesResultingFromConditionBy("cell growth", fun pv -> pv.Name = "temperature" && pv.Value = Some "37")
        let names = results |> Seq.map (fun m -> m.Name) |> Set.ofSeq
        Expect.isTrue  (names.Contains("SampleA")) "SampleA downstream of 37°C growth"
        Expect.isTrue  (names.Contains("SampleB")) "SampleB downstream of 37°C growth"
        Expect.isFalse (names.Contains("Source1")) "Source1 is upstream, not downstream"

    testCase "use-case 1 — wrong temperature returns empty" <| fun _ ->
        let f = makeFixtureB()
        let results = f.DS.SamplesResultingFromConditionBy("cell growth", fun pv -> pv.Name = "temperature" && pv.Value = Some "4")
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
        let node = SampleNode f.Sample1
        let pvs  = f.DS.AnnotationsForNode(node)
        let names = pvs |> Seq.map (fun pv -> pv.Name) |> Set.ofSeq
        Expect.isTrue (names.Contains("temperature")) "temperature PV included"
        Expect.isTrue (names.Contains("rpm"))         "rpm PV included"
        Expect.isTrue (names.Contains("enzyme"))      "enzyme PV included"
        Expect.equal  (pvs.Count) 3                   "exactly 3 PVs"

    testCase "use-case 2 — scoped to dataset excludes other datasets" <| fun _ ->
        // Construct an unrelated dataset with its own processes carrying same PV names
        let s = Sample("Sx", additionalType = "Source")
        let o = Sample("Ox", additionalType = "Sample")
        let px = Process("px")
        px.AddInputSample(s)
        px.AddOutputSample(o)
        px.AddParameterValue(Annotation("temperature", value = "100", unit = "°C", additionalType = "ParameterValue"))
        let dsX = Dataset("DS-X")
        dsX.AddProcess(px)
        // The result from Fixture A's dataset must NOT include the "100°C" value
        let f = makeFixtureA()
        let node = SampleNode f.Sample1
        let pvs  = f.DS.AnnotationsForNode(node)
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
        let connected = (SampleNode f.Sample1).AllConnectedNodes()
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
        let connected = (SampleNode f.Sample1).AllConnectedNodes(scope = scope)
        let keys = connected |> Seq.map (fun n -> n.Key()) |> Set.ofSeq
        Expect.isTrue  (keys.Contains("M:Source1"))       "Source1 in child1 scope"
        Expect.isFalse (keys.Contains("D:rawData1.csv"))  "rawData1.csv not in child1 scope"

    // ─── Dataset.PathsThrough — multi-path proteomics ────────────────────────
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

    testCase "Dataset.PathsThrough — multi-path proteomics" <| fun _ ->
        let sourceA = Sample("SourceA", additionalType = "Source")
        let sampleA = Sample("SampleA", additionalType = "Sample")
        let sourceB = Sample("SourceB", additionalType = "Source")
        let sampleB = Sample("SampleB", additionalType = "Sample")
        let raw     = Data("rawData.csv")

        let growthA = Process("growth_a")
        growthA.AddInputSample(sourceA)
        growthA.AddOutputSample(sampleA)

        let growthB = Process("growth_b")
        growthB.AddInputSample(sourceB)
        growthB.AddOutputSample(sampleB)

        let measurement = Process("measurement")
        measurement.AddInputSample(sampleA)
        measurement.AddInputSample(sampleB)
        measurement.AddOutputData(raw)

        let ds = Dataset("investigation")
        ds.AddProcess(growthA)
        ds.AddProcess(growthB)
        ds.AddProcess(measurement)

        let paths = ds.PathsThrough(DataNode raw)

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

    testCase "metadata-powered data analysis combines process annotations and datamap contexts" <| fun _ ->
        let temperature = DefinedTerm("temperature", tan = "https://bioregistry.io/NCRO:0000029")
        let biologicalReplicate = DefinedTerm("biological replicate group", tan = "https://bioregistry.io/DPBO:1000183")
        let technicalReplicate = DefinedTerm("technical replicate group", tan = "https://bioregistry.io/DPBO:1000184")
        let proteinIdentifier = DefinedTerm("protein identifier", tan = "http://purl.obolibrary.org/obo/NCIT_C165059")
        let lfqIntensity = DefinedTerm("LFQ intensity", tan = "http://purl.obolibrary.org/obo/MS_1001902")

        let ds = Dataset("metadata-powered-analysis")
        ds.RegisterFragmentSelectorProvider(CsvFragmentSelectorProvider())
        ds.AddDataFile(Data("proteomics_result.tsv", encodingFormat = "text/tab-separated-values"))
        ds.AddDataContext(
            DataContext(
                Data("proteomics_result.tsv", selector = "#col=1", selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri, encodingFormat = "text/tab-separated-values"),
                explication = proteinIdentifier,
                objectType = DefinedTerm("String")))
        ds.AddDataContext(
            DataContext(
                Data("proteomics_result.tsv", selector = "#col=2-5", selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri, encodingFormat = "text/tab-separated-values"),
                explication = lfqIntensity,
                objectType = DefinedTerm("Float")))

        let source = Sample("Base culture", additionalType = "Source")

        let addResult condition bioRep techRep selector =
            let culture = Sample($"Culture {condition} C replicate {bioRep}", additionalType = "Sample")
            culture.AddAdditionalProperty(Annotation("temperature", value = condition, unit = "degree Celsius", nameTAN = temperature.TAN.Value, additionalType = "FactorValue"))

            let aliquot = Sample($"Aliquot {condition} C replicate {bioRep}.{techRep}", additionalType = "Sample")
            aliquot.AddAdditionalProperty(Annotation("biological replicate group", value = bioRep, nameTAN = biologicalReplicate.TAN.Value, additionalType = "CharacteristicValue"))
            aliquot.AddAdditionalProperty(Annotation("technical replicate group", value = techRep, nameTAN = technicalReplicate.TAN.Value, additionalType = "CharacteristicValue"))

            let data = Data("proteomics_result.tsv", selector = selector, selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri, encodingFormat = "text/tab-separated-values")

            let growth = Process($"Growth {condition} C {bioRep}.{techRep}")
            growth.AddInputSample(source)
            growth.AddOutputSample(culture)

            let preparation = Process($"Prepare sample {condition} C {bioRep}.{techRep}")
            preparation.AddInputSample(culture)
            preparation.AddOutputSample(aliquot)

            let analysis = Process($"Computational proteome analysis {condition} C {bioRep}.{techRep}")
            analysis.AddInputSample(aliquot)
            analysis.AddOutputData(data)

            ds.AddProcess(growth)
            ds.AddProcess(preparation)
            ds.AddProcess(analysis)
            data

        let selectedA = addResult "35" "1" "1" "#col=2"
        let selectedB = addResult "35" "1" "2" "#col=3"
        let _otherCondition = addResult "40" "1" "1" "#col=4"
        let _otherReplicate = addResult "35" "2" "1" "#col=5"

        let hasUpstreamValue term value data =
            ds.UpstreamAnnotationsForNode(DataNode data)
            |> Seq.exists (fun pv -> pv.NameEquals(term) && pv.Value = Some value)

        let selected =
            ds.FinalData()
            |> Seq.filter (fun data -> hasUpstreamValue temperature "35" data)
            |> Seq.filter (fun data -> hasUpstreamValue biologicalReplicate "1" data)
            |> Seq.toList

        let selectedSelectors = selected |> List.map (fun data -> data.Selector.Value) |> Set.ofList
        Expect.equal selectedSelectors (Set.ofList [ selectedA.Selector.Value; selectedB.Selector.Value ]) "condition and replicate filters should select the expected result columns"

        let filePaths = selected |> List.map (fun data -> data.Path) |> Set.ofList
        Expect.equal filePaths (Set.ofList [ "proteomics_result.tsv" ]) "selected fragments should point to one matrix file"

        let indexContext =
            ds.DataContextsForPath("proteomics_result.tsv")
            |> Seq.find (fun dc -> dc.ExplicationEquals(proteinIdentifier))

        let indexColumn =
            indexContext.Data.Selector
            |> Option.bind (fun selector -> CsvFragmentSelectorProvider.TryGetZeroBasedColumnIndex(selector))

        Expect.equal indexColumn (Some 0) "protein identifier context should identify the first zero-based column"

        let abundancePairs =
            selected
            |> Seq.collect (fun data ->
                ds.DataContextsCoveringData(data)
                |> Seq.filter (fun dc -> dc.ExplicationEquals(lfqIntensity))
                |> Seq.map (fun dc -> data, dc))
            |> Seq.toList

        Expect.equal abundancePairs.Length 2 "both selected result columns should be covered by the LFQ Datamap context"

        let selectedColumnIndices =
            abundancePairs
            |> List.map (fun (data, _) -> data.Selector |> Option.bind (fun selector -> CsvFragmentSelectorProvider.TryGetZeroBasedColumnIndex(selector)))
            |> Set.ofList

        Expect.equal selectedColumnIndices (Set.ofList [ Some 1; Some 2 ]) "selected data fragments should resolve to dataframe column positions"

        let technicalReplicateLabels =
            selected
            |> List.map (fun data ->
                ds.UpstreamAnnotationsForNode(DataNode data)
                |> Seq.find (fun pv -> pv.NameEquals(technicalReplicate))
                |> fun pv -> data.Selector.Value, pv.ValueText)
            |> Set.ofList

        Expect.equal technicalReplicateLabels (Set.ofList [ "#col=2", "1"; "#col=3", "2" ]) "selected columns should retain process-derived labels"

        let allAbundanceData =
            ds.DataWithDataContextByExplication(lfqIntensity)
            |> Seq.map (fun (data, _) -> data.Selector.Value)
            |> Set.ofSeq

        Expect.equal allAbundanceData (Set.ofList [ "#col=2"; "#col=3"; "#col=4"; "#col=5" ]) "explication lookup should find every abundance data fragment"

]
