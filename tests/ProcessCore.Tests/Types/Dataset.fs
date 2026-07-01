module ProcessCore.Tests.Types.Dataset

open Fable.Pyxpecto
open ProcessCore

let tests = testList "Dataset" [

    testCase "title is optional and mutable" <| fun _ ->
        let ds = Dataset("DS-A", title = "Initial title")
        Expect.equal ds.Title (Some "Initial title") "constructor title"
        ds.Title <- Some "Updated title"
        Expect.equal ds.Title (Some "Updated title") "mutable title"

    testCase "equality by identifier" <| fun _ ->
        let ds1 = Dataset("DS-A")
        let ds2 = Dataset("DS-A")
        Expect.equal ds1 ds2 "Same identifier → equal"

    testCase "inequality different identifier" <| fun _ ->
        let ds1 = Dataset("DS-A")
        let ds2 = Dataset("DS-B")
        Expect.notEqual ds1 ds2 "Different identifiers → not equal"

    testCase "AddProcess sets ProcessOf back-edge" <| fun _ ->
        let ds = Dataset("DS-A")
        let p  = Process("p1")
        Expect.isNone p.ProcessOf "ProcessOf starts as None"
        ds.AddProcess(p)
        Expect.isSome p.ProcessOf "ProcessOf should be Some after AddProcess"
        Expect.equal p.ProcessOf.Value ds "ProcessOf should point to the dataset"

    testCase "AddProcess deduplicates reference Identity" <| fun _ ->
        let ds = Dataset("DS-A")
        let p  = Process("p1")
        ds.AddProcess(p)
        ds.AddProcess(p)
        Expect.equal ds.Processes.Count 1 "Same process added twice → one entry"

    testCase "AddProcess does not deduplicate different instances" <| fun _ ->
        let ds = Dataset("DS-A")
        let p1 = Process("p1")
        let p2 = Process("p1")
        ds.AddProcess(p1)
        ds.AddProcess(p2)
        Expect.equal ds.Processes.Count 2 "Different instances with same identifier → two entries"

    testCase "RemoveProcess clears ProcessOf" <| fun _ ->
        let ds = Dataset("DS-A")
        let p  = Process("p1")
        ds.AddProcess(p)
        ds.RemoveProcess(p)
        Expect.equal ds.Processes.Count 0 "Process should be removed"
        Expect.isNone p.ProcessOf "ProcessOf should be None after removal"

    testCase "TryGetProcess found" <| fun _ ->
        let ds = Dataset("DS-A")
        let p  = Process("p1")
        ds.AddProcess(p)
        let result = ds.TryGetProcess("p1")
        Expect.isSome result "Should find the process"
        Expect.equal result.Value p "Should return the correct process"

    testCase "TryGetProcess not found" <| fun _ ->
        let ds     = Dataset("DS-A")
        let result = ds.TryGetProcess("p99")
        Expect.isNone result "Should return None for missing process"

    testCase "GetProcess throws if missing" <| fun _ ->
        let ds = Dataset("DS-A")
        Expect.throws (fun () -> ds.GetProcess("p99") |> ignore) "Should throw for missing process"

    testCase "AddPart sets PartOf back-edge" <| fun _ ->
        let parent = Dataset("parent")
        let child  = Dataset("child")
        Expect.isNone child.PartOf "PartOf starts as None"
        parent.AddPart(child)
        Expect.isSome child.PartOf "PartOf should be Some after AddPart"
        Expect.equal child.PartOf.Value parent "PartOf should point to parent"

    testCase "AddPart deduplicates" <| fun _ ->
        let parent = Dataset("parent")
        let child  = Dataset("child")
        parent.AddPart(child)
        parent.AddPart(child)
        Expect.equal parent.HasPart.Count 1 "Same child added twice → one entry"

    testCase "RemovePart clears PartOf" <| fun _ ->
        let parent = Dataset("parent")
        let child  = Dataset("child")
        parent.AddPart(child)
        parent.RemovePart(child)
        Expect.equal parent.HasPart.Count 0 "Child should be removed"
        Expect.isNone child.PartOf "PartOf should be None after removal"

    testCase "TryGetPart found" <| fun _ ->
        let parent = Dataset("parent")
        let child  = Dataset("child")
        parent.AddPart(child)
        let result = parent.TryGetPart("child")
        Expect.isSome result "Should find the child dataset"
        Expect.equal result.Value child "Should return the correct child"

    testCase "TryGetPart not found" <| fun _ ->
        let parent = Dataset("parent")
        let result = parent.TryGetPart("missing")
        Expect.isNone result "Should return None for missing child"

    testCase "AddAdditionalProperty deduplicates" <| fun _ ->
        let ds = Dataset("DS-A")
        let pv = Annotation("licence", value = "CC-BY-4.0")
        ds.AddAdditionalProperty(pv)
        ds.AddAdditionalProperty(pv)
        Expect.equal ds.AdditionalProperty.Count 1 "Identical PV added twice → one entry"

    testCase "administrative and datamap collections are retained" <| fun _ ->
        let agent = Agent("Ada", familyName = "Lovelace")
        let citation = ScholarlyArticle("Example citation", authors = [ agent ])
        let dataFile = Data("results.csv")
        let dataContext = DataContext(dataFile, explication = DefinedTerm("protein abundance"))
        let ds =
            Dataset(
                "DS-admin",
                license = "CC-BY-4.0",
                datePublished = "2026-06-30",
                agents = [ agent ],
                citations = [ citation ],
                dataContexts = [ dataContext ],
                dataFiles = [ dataFile ])

        Expect.equal ds.License (Some "CC-BY-4.0") "License should be retained"
        Expect.equal ds.DatePublished (Some "2026-06-30") "DatePublished should be retained"
        Expect.equal ds.Agents.Count 1 "Agent should be retained"
        Expect.equal ds.Citations.Count 1 "Citation should be retained"
        Expect.equal ds.DataContexts.Count 1 "DataContext should be retained"
        Expect.equal ds.DataFiles.Count 1 "Data file should be retained"
        Expect.equal (ds.AllAgents().Count) 1 "AllAgents should discover agent"
        Expect.equal (ds.AllCitations().Count) 1 "AllCitations should discover citation"
        Expect.equal (ds.AllDataFiles().Count) 1 "AllDataFiles should discover data file"
        Expect.equal (ds.AllDataContexts().Count) 1 "AllDataContexts should discover data context"
        Expect.equal (ds.DataContextsForData(dataFile).Count) 1 "DataContextsForData should match by data target"

    testCase "DataContext semantic term helpers" <| fun _ ->
        let dc =
            DataContext(
                Data("results.csv"),
                explication = DefinedTerm("LFQ intensity", tan = "http://purl.obolibrary.org/obo/MS_1001902"),
                objectType = DefinedTerm("Float", tan = "http://purl.obolibrary.org/obo/NCIT_C48150"),
                unit = DefinedTerm("arbitrary unit"))

        Expect.isTrue (dc.ExplicationEquals(DefinedTerm("label-free quantification intensity", tan = "http://purl.obolibrary.org/obo/MS_1001902"))) "explication should match by TAN"
        Expect.isTrue (dc.ObjectTypeEquals(DefinedTerm("Float", tan = "http://purl.obolibrary.org/obo/NCIT_C48150"))) "object type should match"
        Expect.isTrue (dc.UnitEquals(DefinedTerm("arbitrary unit"))) "unit should match by exact term"
        Expect.isFalse (dc.ExplicationEquals(DefinedTerm("protein identifier"))) "different explication should not match"

    testCase "DataContextsForPath returns contexts across selectors" <| fun _ ->
        let file = Data("results.csv")
        let fragment = Data("results.csv", selector = "#col=2", selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri)
        let other = Data("other.csv")
        let ds =
            Dataset(
                "DS-datacontext-path",
                dataContexts = [
                    DataContext(file, explication = DefinedTerm("table"))
                    DataContext(fragment, explication = DefinedTerm("abundance"))
                    DataContext(other, explication = DefinedTerm("other"))
                ])

        let contexts = ds.DataContextsForPath("results.csv")
        Expect.equal contexts.Count 2 "both whole-file and fragment contexts should match the path"

    testCase "DataContextsCoveringData resolves exact and contained CSV fragments" <| fun _ ->
        let ds = Dataset("DS-datacontext-covering")
        ds.RegisterFragmentSelectorProvider(CsvFragmentSelectorProvider())

        let exact = DataContext(Data("results.csv", selector = "#col=2", selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri), explication = DefinedTerm("exact"))
        let range = DataContext(Data("results.csv", selector = "#col=2-4", selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri), explication = DefinedTerm("range"))
        let disjoint = DataContext(Data("results.csv", selector = "#col=6", selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri), explication = DefinedTerm("disjoint"))
        let unknown = DataContext(Data("results.csv", selector = "opaque-a", selectorFormat = "missing/provider"), explication = DefinedTerm("unknown"))

        ds.AddDataContext(exact)
        ds.AddDataContext(range)
        ds.AddDataContext(disjoint)
        ds.AddDataContext(unknown)

        let query = Data("results.csv", selector = "#col=2", selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri)
        let names =
            ds.DataContextsCoveringData(query)
            |> Seq.choose (fun dc -> dc.Explication |> Option.map (fun t -> t.Name))
            |> Set.ofSeq

        Expect.isTrue (names.Contains("exact")) "exact context should cover query data"
        Expect.isTrue (names.Contains("range")) "containing context should cover query data"
        Expect.isFalse (names.Contains("disjoint")) "disjoint context should not cover query data"
        Expect.isFalse (names.Contains("unknown")) "unknown selector relation should not cover query data"

    testCase "DataWithDataContextByExplication pairs data with covering contexts" <| fun _ ->
        let ds = Dataset("DS-datacontext-explication")
        ds.RegisterFragmentSelectorProvider(CsvFragmentSelectorProvider())

        let abundance = DefinedTerm("LFQ intensity", tan = "http://purl.obolibrary.org/obo/MS_1001902")
        let identifier = DefinedTerm("protein identifier", tan = "http://purl.obolibrary.org/obo/NCIT_C165059")
        let data = Data("results.csv", selector = "#col=3", selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri)
        let other = Data("results.csv", selector = "#col=8", selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri)
        let p = Process("analysis")
        p.AddOutputData(data)
        p.AddOutputData(other)
        ds.AddProcess(p)
        ds.AddDataContext(DataContext(Data("results.csv", selector = "#col=2-4", selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri), explication = abundance))
        ds.AddDataContext(DataContext(Data("results.csv", selector = "#col=1", selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri), explication = identifier))

        let pairs = ds.DataWithDataContextByExplication(abundance)

        let matchedData, matchedContext = pairs.[0]
        Expect.equal pairs.Count 1 "only the contained abundance data should be paired"
        Expect.equal matchedData data "paired data should be the matching process data"
        Expect.isTrue (matchedContext.ExplicationEquals(abundance)) "paired context should carry the requested explication"

    testCase "CollapseProcesses groups same name and equal process values" <| fun _ ->
        let nodeName (node: IONode) =
            match node with
            | SampleNode m -> m.Name
            | DataNode d -> d.Path

        let makeProcess input value output =
            let p = Process("MyProcess")
            p.AddInputSample(Sample(input))
            p.AddParameterValue(Annotation("status", value = value, additionalType = "ParameterValue"))
            p.AddOutputSample(Sample(output))
            p

        let ds = Dataset("DS-collapse")
        ds.AddProcess(makeProcess "Input1" "ValueX" "Output1")
        ds.AddProcess(makeProcess "Input2" "ValueX" "Output2")
        ds.AddProcess(makeProcess "Input3" "ValueY" "Output3")
        ds.AddProcess(makeProcess "Input4" "ValueY" "Output4")

        ds.CollapseProcesses()

        Expect.equal ds.Processes.Count 2 "two value groups remain"
        let valueX =
            ds.Processes
            |> Seq.find (fun p -> p.ParameterValue |> Seq.exists (fun pv -> pv.Value = Some "ValueX"))
        let valueY =
            ds.Processes
            |> Seq.find (fun p -> p.ParameterValue |> Seq.exists (fun pv -> pv.Value = Some "ValueY"))

        Expect.equal valueX.Inputs.Count 2 "ValueX group has two input lanes"
        Expect.equal valueX.Outputs.Count 2 "ValueX group has two output lanes"
        Expect.equal (nodeName valueX.Inputs[0]) "Input1" "first ValueX input"
        Expect.equal (nodeName valueX.Inputs[1]) "Input2" "second ValueX input"
        Expect.equal (nodeName valueX.Outputs[0]) "Output1" "first ValueX output"
        Expect.equal (nodeName valueX.Outputs[1]) "Output2" "second ValueX output"

        Expect.equal valueY.Inputs.Count 2 "ValueY group has two input lanes"
        Expect.equal valueY.Outputs.Count 2 "ValueY group has two output lanes"
        Expect.equal (nodeName valueY.Inputs[0]) "Input3" "first ValueY input"
        Expect.equal (nodeName valueY.Inputs[1]) "Input4" "second ValueY input"
        Expect.equal (nodeName valueY.Outputs[0]) "Output3" "first ValueY output"
        Expect.equal (nodeName valueY.Outputs[1]) "Output4" "second ValueY output"

    testCase "CollapseProcesses does not collapse processes with different names" <| fun _ ->
        let p1 = Process("ProcessA")
        p1.AddInputSample(Sample("Input1"))
        p1.AddParameterValue(Annotation("status", value = "ValueX", additionalType = "ParameterValue"))
        p1.AddOutputSample(Sample("Output1"))

        let p2 = Process("ProcessB")
        p2.AddInputSample(Sample("Input2"))
        p2.AddParameterValue(Annotation("status", value = "ValueX", additionalType = "ParameterValue"))
        p2.AddOutputSample(Sample("Output2"))

        let ds = Dataset("DS-collapse-names")
        ds.AddProcess(p1)
        ds.AddProcess(p2)

        ds.CollapseProcesses()

        Expect.equal ds.Processes.Count 2 "different process names remain separate"
        Expect.equal p1.Inputs.Count 1 "ProcessA keeps one input lane"
        Expect.equal p1.Outputs.Count 1 "ProcessA keeps one output lane"
        Expect.equal p2.Inputs.Count 1 "ProcessB keeps one input lane"
        Expect.equal p2.Outputs.Count 1 "ProcessB keeps one output lane"

    testCase "CollapseProcesses does not collapse when property values differ" <| fun _ ->
        let p1 = Process("MyProcess")
        p1.AddInputSample(Sample("Input1"))
        p1.AddParameterValue(Annotation("status", value = "ValueX", additionalType = "ParameterValue"))
        p1.AddOutputSample(Sample("Output1"))

        let p2 = Process("MyProcess")
        p2.AddInputSample(Sample("Input2"))
        p2.AddParameterValue(Annotation("status", value = "ValueY", additionalType = "ParameterValue"))
        p2.AddOutputSample(Sample("Output2"))

        let ds = Dataset("DS-collapse-values")
        ds.AddProcess(p1)
        ds.AddProcess(p2)

        ds.CollapseProcesses()

        Expect.equal ds.Processes.Count 2 "different process values remain separate"
        Expect.equal p1.Inputs.Count 1 "ValueX process keeps one input lane"
        Expect.equal p1.Outputs.Count 1 "ValueX process keeps one output lane"
        Expect.equal p2.Inputs.Count 1 "ValueY process keeps one input lane"
        Expect.equal p2.Outputs.Count 1 "ValueY process keeps one output lane"

    testCase "CollapseProcesses appends a single IO process to an existing multi-IO process" <| fun _ ->
        let nodeName (node: IONode) =
            match node with
            | SampleNode m -> m.Name
            | DataNode d -> d.Path

        let multi = Process("MyProcess")
        multi.AddInputSample(Sample("Input1"))
        multi.AddInputSample(Sample("Input2"))
        multi.AddParameterValue(Annotation("status", value = "ValueX", additionalType = "ParameterValue"))
        multi.AddOutputSample(Sample("Output1"))
        multi.AddOutputSample(Sample("Output2"))

        let single = Process("MyProcess")
        single.AddInputSample(Sample("Input3"))
        single.AddParameterValue(Annotation("status", value = "ValueX", additionalType = "ParameterValue"))
        single.AddOutputSample(Sample("Output3"))

        let separate = Process("MyProcess")
        separate.AddInputSample(Sample("Input4"))
        separate.AddParameterValue(Annotation("status", value = "ValueY", additionalType = "ParameterValue"))
        separate.AddOutputSample(Sample("Output4"))

        let ds = Dataset("DS-collapse-multi")
        ds.AddProcess(multi)
        ds.AddProcess(single)
        ds.AddProcess(separate)

        ds.CollapseProcesses()

        Expect.equal ds.Processes.Count 2 "matching IO process joins the multi-lane process while the other remains separate"
        Expect.equal multi.Inputs.Count 3 "collapsed process has three input lanes"
        Expect.equal multi.Outputs.Count 3 "collapsed process has three output lanes"
        Expect.equal (nodeName multi.Inputs[2]) "Input3" "single input is appended"
        Expect.equal (nodeName multi.Outputs[2]) "Output3" "single output is appended"
        Expect.equal separate.Inputs.Count 1 "separate process keeps one input lane"
        Expect.equal separate.Outputs.Count 1 "separate process keeps one output lane"

        let upstream = multi.Outputs[2].UpstreamNodes(scope = ds.Processes)
        Expect.equal upstream.Count 1 "appended output maps to its appended input"
        Expect.equal (nodeName upstream[0]) "Input3" "Output3 maps back to Input3"

    testCase "CollapseProcesses collapses processes with inputs only" <| fun _ ->
        let nodeName (node: IONode) =
            match node with
            | SampleNode m -> m.Name
            | DataNode d -> d.Path

        let p1 = Process("InputOnly")
        p1.AddInputSample(Sample("Input1"))
        p1.AddParameterValue(Annotation("status", value = "ValueX", additionalType = "ParameterValue"))

        let p2 = Process("InputOnly")
        p2.AddInputSample(Sample("Input2"))
        p2.AddParameterValue(Annotation("status", value = "ValueX", additionalType = "ParameterValue"))

        let ds = Dataset("DS-collapse-input-only")
        ds.AddProcess(p1)
        ds.AddProcess(p2)

        ds.CollapseProcesses()

        Expect.equal ds.Processes.Count 1 "input-only rows collapse"
        Expect.equal p1.Inputs.Count 2 "collapsed input-only process has both inputs"
        Expect.equal p1.Outputs.Count 0 "collapsed input-only process still has no outputs"
        Expect.equal (nodeName p1.Inputs[0]) "Input1" "first input lane"
        Expect.equal (nodeName p1.Inputs[1]) "Input2" "second input lane"

    testCase "CollapseProcesses keeps both-sided, input-only, and output-only groups separate" <| fun _ ->
        let nodeName (node: IONode) =
            match node with
            | SampleNode m -> m.Name
            | DataNode d -> d.Path

        let addStatus (p: Process) =
            p.AddParameterValue(Annotation("status", value = "ValueX", additionalType = "ParameterValue"))
            p

        let both1 = addStatus (Process("MixedShape"))
        both1.AddInputSample(Sample("BothInput1"))
        both1.AddOutputSample(Sample("BothOutput1"))

        let both2 = addStatus (Process("MixedShape"))
        both2.AddInputSample(Sample("BothInput2"))
        both2.AddOutputSample(Sample("BothOutput2"))

        let inputOnly1 = addStatus (Process("MixedShape"))
        inputOnly1.AddInputSample(Sample("OnlyInput1"))

        let inputOnly2 = addStatus (Process("MixedShape"))
        inputOnly2.AddInputSample(Sample("OnlyInput2"))

        let outputOnly1 = addStatus (Process("MixedShape"))
        outputOnly1.AddOutputSample(Sample("OnlyOutput1"))

        let outputOnly2 = addStatus (Process("MixedShape"))
        outputOnly2.AddOutputSample(Sample("OnlyOutput2"))

        let ds = Dataset("DS-collapse-mixed-shape")
        ds.AddProcess(both1)
        ds.AddProcess(both2)
        ds.AddProcess(inputOnly1)
        ds.AddProcess(inputOnly2)
        ds.AddProcess(outputOnly1)
        ds.AddProcess(outputOnly2)

        ds.CollapseProcesses()

        Expect.equal ds.Processes.Count 3 "each IO shape collapses separately"

        let both =
            ds.Processes
            |> Seq.find (fun p -> p.Inputs.Count = 2 && p.Outputs.Count = 2)
        let inputOnly =
            ds.Processes
            |> Seq.find (fun p -> p.Inputs.Count = 2 && p.Outputs.Count = 0)
        let outputOnly =
            ds.Processes
            |> Seq.find (fun p -> p.Inputs.Count = 0 && p.Outputs.Count = 2)

        Expect.isTrue (obj.ReferenceEquals(both, both1)) "both-sided representative is retained"
        Expect.isTrue (obj.ReferenceEquals(inputOnly, inputOnly1)) "input-only representative is retained"
        Expect.isTrue (obj.ReferenceEquals(outputOnly, outputOnly1)) "output-only representative is retained"

        Expect.equal (nodeName both.Inputs[1]) "BothInput2" "both-sided group has second input"
        Expect.equal (nodeName both.Outputs[1]) "BothOutput2" "both-sided group has second output"
        Expect.equal (nodeName inputOnly.Inputs[1]) "OnlyInput2" "input-only group has second input"
        Expect.equal (nodeName outputOnly.Outputs[1]) "OnlyOutput2" "output-only group has second output"

        let upstream = both.Outputs[1].UpstreamNodes(scope = ds.Processes)
        Expect.equal upstream.Count 1 "both-sided N-to-N lane is not polluted by input-only rows"
        Expect.equal (nodeName upstream[0]) "BothInput2" "BothOutput2 maps back to BothInput2"

    testCase "CollapseProcesses preserves positional N-to-N traversal and back-edges" <| fun _ ->
        let nodeName (node: IONode) =
            match node with
            | SampleNode m -> m.Name
            | DataNode d -> d.Path

        let p1 = Process("MyProcess")
        p1.AddInputSample(Sample("Input1"))
        p1.AddParameterValue(Annotation("status", value = "ValueX", additionalType = "ParameterValue"))
        p1.AddOutputSample(Sample("Output1"))

        let p2 = Process("MyProcess")
        p2.AddInputSample(Sample("Input2"))
        p2.AddParameterValue(Annotation("status", value = "ValueX", additionalType = "ParameterValue"))
        p2.AddOutputSample(Sample("Output2"))

        let ds = Dataset("DS-collapse-map")
        ds.AddProcess(p1)
        ds.AddProcess(p2)

        let input2 = p2.Inputs[0]
        let output2 = p2.Outputs[0]

        ds.CollapseProcesses()

        Expect.equal ds.Processes.Count 1 "matching processes collapse to one process"
        Expect.isNone p2.ProcessOf "retired process is detached from the dataset"
        Expect.equal (output2.GetOutputOf().Count) 1 "retired output back-edge is removed"
        Expect.isTrue (output2.GetOutputOf() |> Seq.exists (fun p -> obj.ReferenceEquals(p, p1)))
            "output back-edge points to representative process"

        let upstream = output2.UpstreamNodes(scope = ds.Processes)
        Expect.equal upstream.Count 1 "positional upstream traversal stays on the second lane"
        Expect.equal (nodeName upstream[0]) "Input2" "Output2 maps back to Input2"

        let downstream = input2.DownstreamNodes(scope = ds.Processes)
        Expect.equal downstream.Count 1 "positional downstream traversal stays on the second lane"
        Expect.equal (nodeName downstream[0]) "Output2" "Input2 maps forward to Output2"

]

