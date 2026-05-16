module ProcessCore.Tests.Graph.FragmentSelectors

open Fable.Pyxpecto
open ProcessCore

type RangeSelector = { First: int; Last: int }

type RangeSelectorProvider() =
    
    inherit FragmentSelectorProviderBase<RangeSelector>()

    let tryParseInt (text: string) =
        match System.Int32.TryParse text with
        | true, value -> Some value
        | false, _ -> None

    override _.SelectorFormat = "test/range"
    override _.TryParse(text: string) = 
        if text.StartsWith("range=") then
            let body = text.Substring("range=".Length)
            let parts = body.Split([| '-' |])
            match parts with
            | [| one |] ->
                tryParseInt one
                |> Option.map (fun i -> { First = i; Last = i })
            | [| first; last |] ->
                match tryParseInt first, tryParseInt last with
                | Some f, Some l when f <= l -> Some { First = f; Last = l }
                | _ -> None
            | _ -> None
        else
            None
    override _.ToSelectorString(selector: RangeSelector) =
        if selector.First = selector.Last then
            $"range={selector.First}"
        else
            $"range={selector.First}-{selector.Last}"
    override _.Relate(container: RangeSelector) (candidate: RangeSelector) =
        if container = candidate then Exact
        elif container.First <= candidate.First && container.Last >= candidate.Last then Contains
        elif container.Last < candidate.First || candidate.Last < container.First then Disjoint
        else Unknown

let datasetWithFakeProvider () =
    let ds = Dataset("ds")
    ds.RegisterFragmentSelectorProvider(RangeSelectorProvider())
    ds

let relateWith (ds: Dataset) (container: Data) (candidate: Data) =
    FragmentSelectorResolution.relateDataWith ds.TryGetFragmentSelectorProvider (container) (candidate)

let keys (nodes: ResizeArray<IONode>) =
    nodes |> Seq.map (fun n -> n.Key()) |> Set.ofSeq

let tests = testList "Fragment selectors" [

    testList "Data relation" [

        testCase "whole file vs same whole file is exact" <| fun _ ->
            let a = Data("file.csv")
            let b = Data("file.csv")
            Expect.equal (FragmentSelectorResolution.relateData a b) Exact "same whole resource"

        testCase "whole file contains same-path fragment" <| fun _ ->
            let whole = Data("file.csv")
            let fragment = Data("file.csv", selector = "opaque")
            Expect.equal (FragmentSelectorResolution.relateData whole fragment) Contains "whole contains fragment"

        testCase "fragment vs whole file is unknown in container direction" <| fun _ ->
            let fragment = Data("file.csv", selector = "opaque")
            let whole = Data("file.csv")
            Expect.equal (FragmentSelectorResolution.relateData fragment whole) Unknown "fragment is not a known container of whole"

        testCase "missing provider falls back to exact selector matching" <| fun _ ->
            let a = Data("file.csv", selector = "opaque", selectorFormat = "missing/provider")
            let b = Data("file.csv", selector = "opaque", selectorFormat = "missing/provider")
            let c = Data("file.csv", selector = "other", selectorFormat = "missing/provider")
            Expect.equal (FragmentSelectorResolution.relateData a b) Exact "same opaque selector"
            Expect.equal (FragmentSelectorResolution.relateData a c) Unknown "different opaque selector"

        testCase "missing selectorFormat does not invoke providers" <| fun _ ->
            let ds = datasetWithFakeProvider ()
            let a = Data("file.csv", selector = "range=1-10")
            let b = Data("file.csv", selector = "range=2-3")
            Expect.equal (relateWith ds a b) Unknown "no selectorFormat means opaque"

        testCase "provider is selected by selectorFormat" <| fun _ ->
            let ds = datasetWithFakeProvider ()
            let a = Data("file.csv", selector = "range=1-10", selectorFormat = "test/range")
            let b = Data("file.csv", selector = "range=2-3", selectorFormat = "test/range")
            Expect.equal (relateWith ds a b) Contains "registered provider resolves containment"

        testCase "provider parse failure yields unknown" <| fun _ ->
            let ds = datasetWithFakeProvider ()
            let a = Data("file.csv", selector = "not-a-range", selectorFormat = "test/range")
            let b = Data("file.csv", selector = "range=2-3", selectorFormat = "test/range")
            Expect.equal (relateWith ds a b) Unknown "unparseable selectors are opaque"

        testCase "different selectorFormat values do not cross-resolve" <| fun _ ->
            let ds = datasetWithFakeProvider ()
            let a = Data("file.csv", selector = "range=1-10", selectorFormat = "test/range")
            let b = Data("file.csv", selector = "range=2-3", selectorFormat = "other/range")
            Expect.equal (relateWith ds a b) Unknown "formats must match"
    ]

    testList "Provider lifecycle" [

        testCase "register, list, try-get, and unregister provider" <| fun _ ->
            let ds = Dataset("ds")
            let provider = RangeSelectorProvider()
            ds.RegisterFragmentSelectorProvider(provider)

            Expect.equal (ds.TryGetFragmentSelectorProvider("test/range")) (Some (provider :> IFragmentSelectorProvider))
                "registered provider should be returned by selectorFormat"
            Expect.equal (ds.GetFragmentSelectorProviders() |> Seq.map (fun p -> p.SelectorFormat) |> Set.ofSeq) (Set.ofList ["test/range"])
                "registered provider should appear in provider list"

            ds.UnregisterFragmentSelectorProvider("test/range")
            Expect.isNone (ds.TryGetFragmentSelectorProvider("test/range"))
                "unregistered provider should not be returned"
            Expect.equal (ds.GetFragmentSelectorProviders() |> Seq.length) 0
                "provider list should be empty after unregister"

        testCase "child provider is registered in parent root when part is added" <| fun _ ->
            let parent = Dataset("parent")
            let child = datasetWithFakeProvider ()

            parent.AddPart(child)

            Expect.isSome (parent.TryGetFragmentSelectorProvider("test/range"))
                "parent root should expose providers from an added child dataset"
            Expect.isSome (child.TryGetFragmentSelectorProvider("test/range"))
                "child lookup should resolve through the shared root while attached"

    ]

    testList "Traversal" [

        testCase "whole file reaches material through contained fragment" <| fun _ ->
            let source = Material("Source")
            let fragment = Data("file.csv", selector = "range=2-3", selectorFormat = "test/range")
            let p = LabProcess("produce-fragment")
            p.AddInputMaterial(source)
            p.AddOutputData(fragment)
            let ds = datasetWithFakeProvider ()
            ds.AddProcess(p)

            let whole = Data("file.csv")
            let upstream = ds.NodesUpstreamOf(DataNode whole) |> keys
            Expect.isTrue (upstream.Contains("M:Source")) "whole-resource query follows contained fragment output"

        testCase "fragment reaches material through contained whole file" <| fun _ ->
            let source = Material("Source")
            let whole = Data("file.csv")
            let p = LabProcess("produce-whole")
            p.AddInputMaterial(source)
            p.AddOutputData(whole)
            let ds = datasetWithFakeProvider ()
            ds.AddProcess(p)

            let fragment = Data("file.csv", selector = "range=2-3", selectorFormat = "test/range")
            let upstream = ds.NodesUpstreamOf(DataNode fragment) |> keys
            Expect.isTrue (upstream.Contains("M:Source")) "fragment query follows containing whole-resource output"

        testCase "fragment reaches material through outer fragment" <| fun _ ->
            let source = Material("Source")
            let whole = Data("file.csv", selector = "range=2-4", selectorFormat = "test/range")
            let p = LabProcess("produce-whole")
            p.AddInputMaterial(source)
            p.AddOutputData(whole)
            let ds = datasetWithFakeProvider ()
            ds.AddProcess(p)

            let fragment = Data("file.csv", selector = "range=3", selectorFormat = "test/range")
            let upstream = ds.NodesUpstreamOf(DataNode fragment) |> keys
            Expect.isTrue (upstream.Contains("M:Source")) "fragment query follows containing whole-resource output"

        testCase "disjoint fragments do not connect" <| fun _ ->
            let source = Material("Source")
            let existing = Data("file.csv", selector = "range=1-2", selectorFormat = "test/range")
            let p = LabProcess("produce-existing")
            p.AddInputMaterial(source)
            p.AddOutputData(existing)
            let ds = datasetWithFakeProvider ()
            ds.AddProcess(p)

            let query = Data("file.csv", selector = "range=4-5", selectorFormat = "test/range")
            let upstream = ds.NodesUpstreamOf(DataNode query) |> keys
            Expect.isFalse (upstream.Contains("M:Source")) "disjoint selector should not traverse"

        testCase "unknown fragment relation does not connect" <| fun _ ->
            let source = Material("Source")
            let existing = Data("file.csv", selector = "range=1-5", selectorFormat = "test/range")
            let p = LabProcess("produce-existing")
            p.AddInputMaterial(source)
            p.AddOutputData(existing)
            let ds = datasetWithFakeProvider ()
            ds.AddProcess(p)

            let query = Data("file.csv", selector = "range=3-8", selectorFormat = "test/range")
            let upstream = ds.NodesUpstreamOf(DataNode query) |> keys
            Expect.isFalse (upstream.Contains("M:Source")) "overlap without containment is unknown"

        testCase "connect only through correct fragment containment (separate processes)" <| fun _ ->
            let source1 = Material("Source1")
            let source2 = Material("Source2")
            let fragment1 = Data("file.csv", selector = "range=1-2", selectorFormat = "test/range")
            let fragment2 = Data("file.csv", selector = "range=4-5", selectorFormat = "test/range")
            let p1 = LabProcess("produce-1")
            p1.AddInputMaterial(source1)
            p1.AddOutputData(fragment1)
            let p2 = LabProcess("produce-2")
            p2.AddInputMaterial(source2)
            p2.AddOutputData(fragment2)
            let ds = datasetWithFakeProvider ()
            ds.AddProcess(p1)
            ds.AddProcess(p2)

            let whole = Data("file.csv", selector = "range=4-6", selectorFormat = "test/range")
            let scoped = ds.NodesUpstreamOf(DataNode whole) |> keys
            Expect.isFalse (scoped.Contains("M:Source1")) "out-of-scope related edge excluded"
            Expect.isTrue (scoped.Contains("M:Source2")) "in-scope related edge included"


        testCase "connect only through correct fragment containment (same process)" <| fun _ ->
            let source1 = Material("Source1")
            let source2 = Material("Source2")
            let fragment1 = Data("file.csv", selector = "range=1-2", selectorFormat = "test/range")
            let fragment2 = Data("file.csv", selector = "range=4-5", selectorFormat = "test/range")
            let p1 = LabProcess("produce")
            p1.AddInputMaterial(source1)
            p1.AddOutputData(fragment1)
            p1.AddInputMaterial(source2)
            p1.AddOutputData(fragment2)
            let ds = datasetWithFakeProvider ()
            ds.AddProcess(p1)

            let whole = Data("file.csv", selector = "range=4-6", selectorFormat = "test/range")
            let scoped = ds.NodesUpstreamOf(DataNode whole) |> keys
            Expect.isFalse (scoped.Contains("M:Source1")) "out-of-scope related edge excluded"
            Expect.isTrue (scoped.Contains("M:Source2")) "in-scope related edge included"

        testCase "fragment reaches material through outer fragment across processes" <| fun _ ->
            let source = Material("Source")
            let sample = Material("Sample")
            let data = Data("file.csv", selector = "range=2-4", selectorFormat = "test/range")
            let p1 = LabProcess("produce-sample")
            let p2 = LabProcess("produce-data")
            p1.AddInputMaterial(source)
            p1.AddOutputMaterial(sample)
            p2.AddInputMaterial(sample)
            p2.AddOutputData(data)
            let ds = datasetWithFakeProvider ()
            ds.AddProcess(p1)
            ds.AddProcess(p2)

            let fragment = Data("file.csv", selector = "range=3", selectorFormat = "test/range")
            let downstream = ds.NodesUpstreamOf(DataNode fragment) |> keys
            Expect.isTrue (downstream.Contains("M:Sample")) "contains intermediary sample material"
            Expect.isTrue (downstream.Contains("M:Source")) "contains base source material"

        testCase "material reaches final data through fragment of full file" <| fun _ ->
            let source = Material("Source")
            let intermediaryFile = Data("file.csv")
            let intermediaryFragment = Data("file.csv", selector = "range=2-4", selectorFormat = "test/range")
            let outputData = Data("outputFile.txt")
            let p1 = LabProcess("produce-intermediary")
            let p2 = LabProcess("produce-output")
            p1.AddInputMaterial(source)
            p1.AddOutputData(intermediaryFile)
            p2.AddInputData(intermediaryFragment)
            p2.AddOutputData(outputData)
            let ds = datasetWithFakeProvider ()
            ds.AddProcess(p1)
            ds.AddProcess(p2)

            let downstream = ds.NodesDownstreamOf(MaterialNode source) |> keys
            Expect.isTrue (downstream.Contains("D:outputFile.txt")) "contains final output data"
            Expect.isTrue (downstream.Contains("D:file.csv")) "contains intermediary file data"

        testCase "material reaches final data through fragment of fragment" <| fun _ ->
            let source = Material("Source")
            let intermediaryOuterFragment = Data("file.csv", selector = "range=2-4", selectorFormat = "test/range")
            let intermediaryFragmentContained = Data("file.csv", selector = "range=3", selectorFormat = "test/range")
            let outputData1 = Data("outputFile.txt")
            let p1 = LabProcess("produce-intermediary")
            let p2 = LabProcess("produce-output")
            p1.AddInputMaterial(source)
            p1.AddOutputData(intermediaryOuterFragment)
            p2.AddInputData(intermediaryFragmentContained)
            p2.AddOutputData(outputData1)
            let ds = datasetWithFakeProvider ()
            ds.AddProcess(p1)
            ds.AddProcess(p2)

            let downstream = ds.NodesDownstreamOf(MaterialNode source) |> keys
            Expect.isTrue (downstream.Contains("D:outputFile.txt")) "contains final output data"
            Expect.isTrue (downstream.Contains((DataNode intermediaryOuterFragment).Key())) "contains intermediary file data"

        testCase "material reaches final data through fragment of fragment ignore disjunct" <| fun _ ->
            let source = Material("Source")
            let intermediaryOuterFragment = Data("file.csv", selector = "range=2-4", selectorFormat = "test/range")
            let intermediaryFragmentContained = Data("file.csv", selector = "range=3", selectorFormat = "test/range")
            let intermediaryFragmentNotContained = Data("file.csv", selector = "range=5", selectorFormat = "test/range")
            let outputData1 = Data("outputFile1.txt")
            let outputData2 = Data("outputFile2.txt")
            let p1 = LabProcess("produce-intermediary")
            let p2 = LabProcess("produce-output")
            p1.AddInputMaterial(source)
            p1.AddOutputData(intermediaryOuterFragment)
            p2.AddInputData(intermediaryFragmentContained)
            p2.AddOutputData(outputData1)
            p2.AddInputData(intermediaryFragmentNotContained)
            p2.AddOutputData(outputData2)
            let ds = datasetWithFakeProvider ()
            ds.AddProcess(p1)
            ds.AddProcess(p2)

            let upstream = ds.NodesDownstreamOf(MaterialNode source) |> keys
            Expect.isTrue (upstream.Contains("D:outputFile1.txt")) "contains final output data"
            Expect.isFalse (upstream.Contains("D:outputFile2.txt")) "does not contain output from disjoint fragment"
            Expect.isTrue (upstream.Contains((DataNode intermediaryOuterFragment).Key())) "contains intermediary file data"

        testCase "scope still restricts related fragment traversal" <| fun _ ->
            let source1 = Material("Source1")
            let source2 = Material("Source2")
            let fragment1 = Data("file.csv", selector = "range=1-2", selectorFormat = "test/range")
            let fragment2 = Data("file.csv", selector = "range=4-5", selectorFormat = "test/range")
            let p1 = LabProcess("produce-1")
            p1.AddInputMaterial(source1)
            p1.AddOutputData(fragment1)
            let p2 = LabProcess("produce-2")
            p2.AddInputMaterial(source2)
            p2.AddOutputData(fragment2)
            let ds = datasetWithFakeProvider ()
            ds.AddProcess(p1)
            ds.AddProcess(p2)

            let whole = Data("file.csv")
            let scoped = (DataNode whole).UpstreamNodes(scope = ResizeArray([| p2 |])) |> keys
            Expect.isFalse (scoped.Contains("M:Source1")) "out-of-scope related edge excluded"
            Expect.isTrue (scoped.Contains("M:Source2")) "in-scope related edge included"


    ]
]
