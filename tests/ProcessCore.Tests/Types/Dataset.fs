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
        p.SetOutputData(data)
        let pOther = Process("analysis")
        pOther.SetOutputData(other)
        ds.AddProcess(p)
        ds.AddProcess(pOther)
        ds.AddDataContext(DataContext(Data("results.csv", selector = "#col=2-4", selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri), explication = abundance))
        ds.AddDataContext(DataContext(Data("results.csv", selector = "#col=1", selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri), explication = identifier))

        let pairs = ds.DataWithDataContextByExplication(abundance)

        let matchedData, matchedContext = pairs.[0]
        Expect.equal pairs.Count 1 "only the contained abundance data should be paired"
        Expect.equal matchedData data "paired data should be the matching process data"
        Expect.isTrue (matchedContext.ExplicationEquals(abundance)) "paired context should carry the requested explication"



]

