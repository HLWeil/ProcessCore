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

let datasetWithCsvProvider () =
    let ds = Dataset("ds")
    ds.RegisterFragmentSelectorProvider(CsvFragmentSelectorProvider())
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

    testList "RFC 7111 CSV provider" [

        testList "parsing" [

            let provider = CsvFragmentSelectorProvider()

            let parse text =
                provider.TryParse text

            let roundtrip text =
                parse text
                |> Option.map provider.ToSelectorString

            testCase "parses row selector" <| fun _ ->
                Expect.equal
                    (parse "row=4")
                    (Some (RowSelector [{ First = Index 4; Last = Index 4 }]))
                    "single row selector"

            testCase "parses row range with last position" <| fun _ ->
                Expect.equal
                    (parse "#row=5-*")
                    (Some (RowSelector [{ First = Index 5; Last = Last }]))
                    "row range to last row"

            testCase "parses column selector and column range" <| fun _ ->
                Expect.equal
                    (parse "col=2;4-6")
                    (Some (ColumnSelector [
                        { First = Index 2; Last = Index 2 }
                        { First = Index 4; Last = Index 6 }
                    ]))
                    "column multi-selection"

            testCase "parses single cell selector" <| fun _ ->
                Expect.equal
                    (parse "cell=4,1")
                    (Some (CellSelector [
                        {
                            Rows = { First = Index 4; Last = Index 4 }
                            Columns = { First = Index 1; Last = Index 1 }
                        }
                    ]))
                    "single cell selector"

            testCase "parses cell rectangle selector" <| fun _ ->
                Expect.equal
                    (parse "cell=4,1-6,2")
                    (Some (CellSelector [
                        {
                            Rows = { First = Index 4; Last = Index 6 }
                            Columns = { First = Index 1; Last = Index 2 }
                        }
                    ]))
                    "cell rectangle selector"

            testCase "parses cell selector with last row and column" <| fun _ ->
                Expect.equal
                    (parse "cell=5,2-*,*")
                    (Some (CellSelector [
                        {
                            Rows = { First = Index 5; Last = Last }
                            Columns = { First = Index 2; Last = Last }
                        }
                    ]))
                    "cell rectangle ending at the last row and column"

            testCase "rejects invalid selector syntax" <| fun _ ->
                Expect.isNone (parse "col=0") "positions are one-based"
                Expect.isNone (parse "row=10-5") "inverse row range"
                Expect.isNone (parse "cell=10,10-5,5") "inverse cell range"
                Expect.isNone (parse "col=2-") "missing range end"
                Expect.isNone (parse "row=1;;3") "empty multi-selection item"
                Expect.isNone (parse "ROW=1") "scheme is lowercase"

            testCase "roundtrips normalized row selectors" <| fun _ ->
                Expect.equal (roundtrip "#row=4") (Some "row=4") "single row drops leading #"
                Expect.equal (roundtrip "row=5-*") (Some "row=5-*") "row range to last row"
                Expect.equal (roundtrip "row=3;6") (Some "row=3;6") "row multi-selection"

            testCase "roundtrips normalized column selectors" <| fun _ ->
                Expect.equal (roundtrip "#col=2") (Some "col=2") "single column drops leading #"
                Expect.equal (roundtrip "col=1-2") (Some "col=1-2") "column range"
                Expect.equal (roundtrip "col=2;4-6;*") (Some "col=2;4-6;*") "column multi-selection with last column"

            testCase "roundtrips normalized cell selectors" <| fun _ ->
                Expect.equal (roundtrip "#cell=4,1") (Some "cell=4,1") "single cell drops leading #"
                Expect.equal (roundtrip "cell=4,1-6,2") (Some "cell=4,1-6,2") "cell rectangle"
                Expect.equal (roundtrip "cell=5,2-*,*") (Some "cell=5,2-*,*") "cell rectangle to last row and column"

            testCase "roundtrip normalizes position whitespace" <| fun _ ->
                Expect.equal (roundtrip " row= 5 - * ") (Some "row=5-*") "row range whitespace"
                Expect.equal (roundtrip "cell= 4 , 1 - 6 , 2") (Some "cell=4,1-6,2") "cell coordinate whitespace"
        ]

        testCase "uses the RFC 7111 selectorFormat URI" <| fun _ ->
            let provider = CsvFragmentSelectorProvider()
            Expect.equal provider.SelectorFormat CsvFragmentSelectorProvider.SelectorFormatUri
                "CSV provider should advertise the RFC 7111 selector format URI"

        testCase "extracts zero-based single column index" <| fun _ ->
            Expect.equal (CsvFragmentSelectorProvider.TryGetZeroBasedColumnIndex("#col=4")) (Some 3) "single column selector should become zero-based"
            Expect.equal (CsvFragmentSelectorProvider.TryGetZeroBasedColumnIndex("col=1")) (Some 0) "leading fragment marker is optional"
            Expect.isNone (CsvFragmentSelectorProvider.TryGetZeroBasedColumnIndex("col=2-4")) "column ranges should not produce one index"
            Expect.isNone (CsvFragmentSelectorProvider.TryGetZeroBasedColumnIndex("col=2;4")) "multi-column selectors should not produce one index"
            Expect.isNone (CsvFragmentSelectorProvider.TryGetZeroBasedColumnIndex("row=4")) "row selectors should not produce a column index"
            Expect.isNone (CsvFragmentSelectorProvider.TryGetZeroBasedColumnIndex("cell=4,2")) "cell selectors should not produce a column index"
            Expect.isNone (CsvFragmentSelectorProvider.TryGetZeroBasedColumnIndex("col=0")) "invalid selectors should not produce an index"

        testCase "parses selectors with or without leading fragment marker" <| fun _ ->
            let ds = datasetWithCsvProvider ()
            let a = Data("file.csv", selector = "col=2-11", selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri)
            let b = Data("file.csv", selector = "#col=2-11", selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri)
            Expect.equal (relateWith ds a b) Exact "leading # is accepted but not semantically significant"

        testCase "column range contains column member" <| fun _ ->
            let ds = datasetWithCsvProvider ()
            let columns = Data("file.csv", selector = "col=2-11", selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri)
            let column = Data("file.csv", selector = "col=4", selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri)
            Expect.equal (relateWith ds columns column) Contains "column ranges contain inner columns"

        testCase "row range contains cell selection by row" <| fun _ ->
            let ds = datasetWithCsvProvider ()
            let rows = Data("file.csv", selector = "row=2-4", selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri)
            let cell = Data("file.csv", selector = "cell=3,8", selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri)
            Expect.equal (relateWith ds rows cell) Contains "row ranges contain cells in selected rows"

        testCase "column range contains cell selection by column" <| fun _ ->
            let ds = datasetWithCsvProvider ()
            let columns = Data("file.csv", selector = "col=2-4", selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri)
            let cell = Data("file.csv", selector = "cell=10,3", selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri)
            Expect.equal (relateWith ds columns cell) Contains "column ranges contain cells in selected columns"

        testCase "cell rectangle contains inner cell rectangle" <| fun _ ->
            let ds = datasetWithCsvProvider ()
            let outer = Data("file.csv", selector = "cell=4,1-6,2", selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri)
            let inner = Data("file.csv", selector = "cell=5,2", selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri)
            Expect.equal (relateWith ds outer inner) Contains "cell rectangles contain inner cells"

        testCase "semicolon multi-selection contains selected member" <| fun _ ->
            let ds = datasetWithCsvProvider ()
            let rows = Data("file.csv", selector = "row=3;6", selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri)
            let row = Data("file.csv", selector = "row=6", selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri)
            Expect.equal (relateWith ds rows row) Contains "multi-selections are treated as a union"

        testCase "disjoint CSV fragments do not connect" <| fun _ ->
            let ds = datasetWithCsvProvider ()
            let left = Data("file.csv", selector = "col=1-2", selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri)
            let right = Data("file.csv", selector = "col=4-5", selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri)
            Expect.equal (relateWith ds left right) Disjoint "non-overlapping column ranges are disjoint"

        testCase "overlapping CSV fragments are unknown without containment" <| fun _ ->
            let ds = datasetWithCsvProvider ()
            let left = Data("file.csv", selector = "col=1-3", selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri)
            let right = Data("file.csv", selector = "col=3-5", selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri)
            Expect.equal (relateWith ds left right) Unknown "overlap without containment remains opaque to traversal"

        testCase "star range contains later concrete selector" <| fun _ ->
            let ds = datasetWithCsvProvider ()
            let tail = Data("file.csv", selector = "row=5-*", selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri)
            let row = Data("file.csv", selector = "row=10", selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri)
            Expect.equal (relateWith ds tail row) Contains "star is treated as an open-ended last-position bound"

        testCase "syntax errors remain opaque" <| fun _ ->
            let ds = datasetWithCsvProvider ()
            let bad = Data("file.csv", selector = "col=2-", selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri)
            let good = Data("file.csv", selector = "col=2", selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri)
            Expect.equal (relateWith ds bad good) Unknown "invalid RFC 7111 syntax does not guess a relation"
    ]

    testList "Traversal" [

        testCase "whole file reaches sample through contained fragment" <| fun _ ->
            let source = Sample("Source")
            let fragment = Data("file.csv", selector = "range=2-3", selectorFormat = "test/range")
            let p = Process("produce-fragment")
            p.AddInputSample(source)
            p.AddOutputData(fragment)
            let ds = datasetWithFakeProvider ()
            ds.AddProcess(p)

            let whole = Data("file.csv")
            let upstream = ds.NodesUpstreamOf(DataNode whole) |> keys
            Expect.isTrue (upstream.Contains("M:Source")) "whole-resource query follows contained fragment output"

        testCase "fragment reaches sample through contained whole file" <| fun _ ->
            let source = Sample("Source")
            let whole = Data("file.csv")
            let p = Process("produce-whole")
            p.AddInputSample(source)
            p.AddOutputData(whole)
            let ds = datasetWithFakeProvider ()
            ds.AddProcess(p)

            let fragment = Data("file.csv", selector = "range=2-3", selectorFormat = "test/range")
            let upstream = ds.NodesUpstreamOf(DataNode fragment) |> keys
            Expect.isTrue (upstream.Contains("M:Source")) "fragment query follows containing whole-resource output"

        testCase "fragment reaches sample through outer fragment" <| fun _ ->
            let source = Sample("Source")
            let whole = Data("file.csv", selector = "range=2-4", selectorFormat = "test/range")
            let p = Process("produce-whole")
            p.AddInputSample(source)
            p.AddOutputData(whole)
            let ds = datasetWithFakeProvider ()
            ds.AddProcess(p)

            let fragment = Data("file.csv", selector = "range=3", selectorFormat = "test/range")
            let upstream = ds.NodesUpstreamOf(DataNode fragment) |> keys
            Expect.isTrue (upstream.Contains("M:Source")) "fragment query follows containing whole-resource output"

        testCase "disjoint fragments do not connect" <| fun _ ->
            let source = Sample("Source")
            let existing = Data("file.csv", selector = "range=1-2", selectorFormat = "test/range")
            let p = Process("produce-existing")
            p.AddInputSample(source)
            p.AddOutputData(existing)
            let ds = datasetWithFakeProvider ()
            ds.AddProcess(p)

            let query = Data("file.csv", selector = "range=4-5", selectorFormat = "test/range")
            let upstream = ds.NodesUpstreamOf(DataNode query) |> keys
            Expect.isFalse (upstream.Contains("M:Source")) "disjoint selector should not traverse"

        testCase "unknown fragment relation does not connect" <| fun _ ->
            let source = Sample("Source")
            let existing = Data("file.csv", selector = "range=1-5", selectorFormat = "test/range")
            let p = Process("produce-existing")
            p.AddInputSample(source)
            p.AddOutputData(existing)
            let ds = datasetWithFakeProvider ()
            ds.AddProcess(p)

            let query = Data("file.csv", selector = "range=3-8", selectorFormat = "test/range")
            let upstream = ds.NodesUpstreamOf(DataNode query) |> keys
            Expect.isFalse (upstream.Contains("M:Source")) "overlap without containment is unknown"

        testCase "connect only through correct fragment containment (separate processes)" <| fun _ ->
            let source1 = Sample("Source1")
            let source2 = Sample("Source2")
            let fragment1 = Data("file.csv", selector = "range=1-2", selectorFormat = "test/range")
            let fragment2 = Data("file.csv", selector = "range=4-5", selectorFormat = "test/range")
            let p1 = Process("produce-1")
            p1.AddInputSample(source1)
            p1.AddOutputData(fragment1)
            let p2 = Process("produce-2")
            p2.AddInputSample(source2)
            p2.AddOutputData(fragment2)
            let ds = datasetWithFakeProvider ()
            ds.AddProcess(p1)
            ds.AddProcess(p2)

            let whole = Data("file.csv", selector = "range=4-6", selectorFormat = "test/range")
            let scoped = ds.NodesUpstreamOf(DataNode whole) |> keys
            Expect.isFalse (scoped.Contains("M:Source1")) "out-of-scope related edge excluded"
            Expect.isTrue (scoped.Contains("M:Source2")) "in-scope related edge included"


        testCase "connect only through correct fragment containment (same process)" <| fun _ ->
            let source1 = Sample("Source1")
            let source2 = Sample("Source2")
            let fragment1 = Data("file.csv", selector = "range=1-2", selectorFormat = "test/range")
            let fragment2 = Data("file.csv", selector = "range=4-5", selectorFormat = "test/range")
            let p1 = Process("produce")
            p1.AddInputSample(source1)
            p1.AddOutputData(fragment1)
            p1.AddInputSample(source2)
            p1.AddOutputData(fragment2)
            let ds = datasetWithFakeProvider ()
            ds.AddProcess(p1)

            let whole = Data("file.csv", selector = "range=4-6", selectorFormat = "test/range")
            let scoped = ds.NodesUpstreamOf(DataNode whole) |> keys
            Expect.isFalse (scoped.Contains("M:Source1")) "out-of-scope related edge excluded"
            Expect.isTrue (scoped.Contains("M:Source2")) "in-scope related edge included"

        testCase "fragment reaches sample through outer fragment across processes" <| fun _ ->
            let source = Sample("Source")
            let sample = Sample("Sample")
            let data = Data("file.csv", selector = "range=2-4", selectorFormat = "test/range")
            let p1 = Process("produce-sample")
            let p2 = Process("produce-data")
            p1.AddInputSample(source)
            p1.AddOutputSample(sample)
            p2.AddInputSample(sample)
            p2.AddOutputData(data)
            let ds = datasetWithFakeProvider ()
            ds.AddProcess(p1)
            ds.AddProcess(p2)

            let fragment = Data("file.csv", selector = "range=3", selectorFormat = "test/range")
            let downstream = ds.NodesUpstreamOf(DataNode fragment) |> keys
            Expect.isTrue (downstream.Contains("M:Sample")) "contains intermediary sample sample"
            Expect.isTrue (downstream.Contains("M:Source")) "contains base source sample"

        testCase "sample reaches final data through fragment of full file" <| fun _ ->
            let source = Sample("Source")
            let intermediaryFile = Data("file.csv")
            let intermediaryFragment = Data("file.csv", selector = "range=2-4", selectorFormat = "test/range")
            let outputData = Data("outputFile.txt")
            let p1 = Process("produce-intermediary")
            let p2 = Process("produce-output")
            p1.AddInputSample(source)
            p1.AddOutputData(intermediaryFile)
            p2.AddInputData(intermediaryFragment)
            p2.AddOutputData(outputData)
            let ds = datasetWithFakeProvider ()
            ds.AddProcess(p1)
            ds.AddProcess(p2)

            let downstream = ds.NodesDownstreamOf(SampleNode source) |> keys
            Expect.isTrue (downstream.Contains("D:outputFile.txt")) "contains final output data"
            Expect.isTrue (downstream.Contains("D:file.csv")) "contains intermediary file data"

        testCase "sample reaches final data through fragment of fragment" <| fun _ ->
            let source = Sample("Source")
            let intermediaryOuterFragment = Data("file.csv", selector = "range=2-4", selectorFormat = "test/range")
            let intermediaryFragmentContained = Data("file.csv", selector = "range=3", selectorFormat = "test/range")
            let outputData1 = Data("outputFile.txt")
            let p1 = Process("produce-intermediary")
            let p2 = Process("produce-output")
            p1.AddInputSample(source)
            p1.AddOutputData(intermediaryOuterFragment)
            p2.AddInputData(intermediaryFragmentContained)
            p2.AddOutputData(outputData1)
            let ds = datasetWithFakeProvider ()
            ds.AddProcess(p1)
            ds.AddProcess(p2)

            let downstream = ds.NodesDownstreamOf(SampleNode source) |> keys
            Expect.isTrue (downstream.Contains("D:outputFile.txt")) "contains final output data"
            Expect.isTrue (downstream.Contains((DataNode intermediaryOuterFragment).Key())) "contains intermediary file data"

        testCase "sample reaches final data through fragment of fragment ignore disjunct" <| fun _ ->
            let source = Sample("Source")
            let intermediaryOuterFragment = Data("file.csv", selector = "range=2-4", selectorFormat = "test/range")
            let intermediaryFragmentContained = Data("file.csv", selector = "range=3", selectorFormat = "test/range")
            let intermediaryFragmentNotContained = Data("file.csv", selector = "range=5", selectorFormat = "test/range")
            let outputData1 = Data("outputFile1.txt")
            let outputData2 = Data("outputFile2.txt")
            let p1 = Process("produce-intermediary")
            let p2 = Process("produce-output")
            p1.AddInputSample(source)
            p1.AddOutputData(intermediaryOuterFragment)
            p2.AddInputData(intermediaryFragmentContained)
            p2.AddOutputData(outputData1)
            p2.AddInputData(intermediaryFragmentNotContained)
            p2.AddOutputData(outputData2)
            let ds = datasetWithFakeProvider ()
            ds.AddProcess(p1)
            ds.AddProcess(p2)

            let upstream = ds.NodesDownstreamOf(SampleNode source) |> keys
            Expect.isTrue (upstream.Contains("D:outputFile1.txt")) "contains final output data"
            Expect.isFalse (upstream.Contains("D:outputFile2.txt")) "does not contain output from disjoint fragment"
            Expect.isTrue (upstream.Contains((DataNode intermediaryOuterFragment).Key())) "contains intermediary file data"

        testCase "scope still restricts related fragment traversal" <| fun _ ->
            let source1 = Sample("Source1")
            let source2 = Sample("Source2")
            let fragment1 = Data("file.csv", selector = "range=1-2", selectorFormat = "test/range")
            let fragment2 = Data("file.csv", selector = "range=4-5", selectorFormat = "test/range")
            let p1 = Process("produce-1")
            p1.AddInputSample(source1)
            p1.AddOutputData(fragment1)
            let p2 = Process("produce-2")
            p2.AddInputSample(source2)
            p2.AddOutputData(fragment2)
            let ds = datasetWithFakeProvider ()
            ds.AddProcess(p1)
            ds.AddProcess(p2)

            let whole = Data("file.csv")
            let scoped = (DataNode whole).UpstreamNodes(scope = ResizeArray([| p2 |])) |> keys
            Expect.isFalse (scoped.Contains("M:Source1")) "out-of-scope related edge excluded"
            Expect.isTrue (scoped.Contains("M:Source2")) "in-scope related edge included"


        testCase "registered CSV provider enables traversal through RFC 7111 column containment" <| fun _ ->
            let source = Sample("Source")
            let columns = Data("file.csv", selector = "#col=2-11", selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri)
            let p = Process("produce-columns")
            p.AddInputSample(source)
            p.AddOutputData(columns)
            let ds = datasetWithCsvProvider ()
            ds.AddProcess(p)

            let column = Data("file.csv", selector = "#col=4", selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri)
            let upstream = ds.NodesUpstreamOf(DataNode column) |> keys
            Expect.isTrue (upstream.Contains("M:Source")) "CSV resolver connects contained RFC 7111 fragments"

    ]

    testList "dataRelatedForTraversal_Helper" [

        testCase "noProvider same file" <| fun _ ->
            let f = fun _ -> None
            let a = Data("file.csv")
            let b = Data("file.csv")
            Expect.isTrue (PathTraversal.dataRelatedForTraversal f a b) "same file"

        testCase "noProvider different files" <| fun _ ->
            let f = fun _ -> None
            let a = Data("file.csv")
            let b = Data("other.csv")
            Expect.isFalse (PathTraversal.dataRelatedForTraversal f a b) "different files are not related without a provider"

        //testCase "missing selectorFormat does not invoke providers" <| fun _ ->
        //    let f = fun _ -> RangeSelectorProvider() :> IFragmentSelectorProvider |> Some
        //    let a = Data("file.csv", selector = "range=1-10")
        //    let b = Data("file.csv", selector = "range=2-3")
        //    Expect.isFalse (PathTraversal.dataRelatedForTraversal f a b) "no selectorFormat means opaque"

        //testCase "provider is selected by selectorFormat" <| fun _ ->
        //    let f = fun _ -> RangeSelectorProvider() :> IFragmentSelectorProvider |> Some
        //    let a = Data("file.csv", selector = "range=1-10", selectorFormat = "test/range")
        //    let b = Data("file.csv", selector = "range=2-3", selectorFormat = "test/range")
        //    Expect.isTrue (PathTraversal.dataRelatedForTraversal f a b) "registered provider resolves containment"

        //testCase "provider parse failure yields unknown" <| fun _ ->
        //    let ds = datasetWithFakeProvider ()
        //    let a = Data("file.csv", selector = "not-a-range", selectorFormat = "test/range")
        //    let b = Data("file.csv", selector = "range=2-3", selectorFormat = "test/range")
        //    Expect.equal (relateWith ds a b) Unknown "unparseable selectors are opaque"

        //testCase "different selectorFormat values do not cross-resolve" <| fun _ ->
        //    let ds = datasetWithFakeProvider ()
        //    let a = Data("file.csv", selector = "range=1-10", selectorFormat = "test/range")
        //    let b = Data("file.csv", selector = "range=2-3", selectorFormat = "other/range")
        //    Expect.equal (relateWith ds a b) Unknown "formats must match"


    ]
]
