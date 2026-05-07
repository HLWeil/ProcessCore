module ProcessCore.Tests.Types.Dataset

open Fable.Pyxpecto
open ProcessCore

let tests = testList "Dataset" [

    testCase "equality by identifier" <| fun _ ->
        let ds1 = Dataset("DS-A")
        let ds2 = Dataset("DS-A")
        Expect.equal ds1 ds2 "Same identifier → equal"

    testCase "inequality different identifier" <| fun _ ->
        let ds1 = Dataset("DS-A")
        let ds2 = Dataset("DS-B")
        Expect.notEqual ds1 ds2 "Different identifiers → not equal"

    testCase "default constructor" <| fun _ ->
        let ds = Dataset()
        Expect.equal ds.Identifier "" "Default identifier should be empty string"

    testCase "AddProcess sets ProcessOf back-edge" <| fun _ ->
        let ds = Dataset("DS-A")
        let p  = LabProcess("p1")
        Expect.isNone p.ProcessOf "ProcessOf starts as None"
        ds.AddProcess(p)
        Expect.isSome p.ProcessOf "ProcessOf should be Some after AddProcess"
        Expect.equal p.ProcessOf.Value ds "ProcessOf should point to the dataset"

    testCase "AddProcess deduplicates reference Identity" <| fun _ ->
        let ds = Dataset("DS-A")
        let p  = LabProcess("p1")
        ds.AddProcess(p)
        ds.AddProcess(p)
        Expect.equal ds.Processes.Count 1 "Same process added twice → one entry"

    testCase "AddProcess does not deduplicate different instances" <| fun _ ->
        let ds = Dataset("DS-A")
        let p1 = LabProcess("p1")
        let p2 = LabProcess("p1")
        ds.AddProcess(p1)
        ds.AddProcess(p2)
        Expect.equal ds.Processes.Count 2 "Different instances with same identifier → two entries"

    testCase "RemoveProcess clears ProcessOf" <| fun _ ->
        let ds = Dataset("DS-A")
        let p  = LabProcess("p1")
        ds.AddProcess(p)
        ds.RemoveProcess(p)
        Expect.equal ds.Processes.Count 0 "Process should be removed"
        Expect.isNone p.ProcessOf "ProcessOf should be None after removal"

    testCase "TryGetProcess found" <| fun _ ->
        let ds = Dataset("DS-A")
        let p  = LabProcess("p1")
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
        let pv = PropertyValue("licence", value = "CC-BY-4.0")
        ds.AddAdditionalProperty(pv)
        ds.AddAdditionalProperty(pv)
        Expect.equal ds.AdditionalProperty.Count 1 "Identical PV added twice → one entry"

]
