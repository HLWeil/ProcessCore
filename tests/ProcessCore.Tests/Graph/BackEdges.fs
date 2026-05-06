module ProcessCore.Tests.Graph.BackEdges

open Fable.Pyxpecto
open ProcessCore

let tests = testList "BackEdges" [

    testCase "AddInput — material InputOf updated" <| fun _ ->
        let p = LabProcess("p")
        let m = Material("Sample1")
        p.AddInputMaterial(m)
        Expect.isTrue (m.InputOf |> Seq.exists (fun x -> x = p))
            "m.InputOf should contain p after AddInputMaterial"

    testCase "AddInput — data InputOf updated" <| fun _ ->
        let p = LabProcess("p")
        let d = Data("file.csv")
        p.AddInputData(d)
        Expect.isTrue (d.InputOf |> Seq.exists (fun x -> x = p))
            "d.InputOf should contain p after AddInputData"

    testCase "RemoveInput — material InputOf cleared" <| fun _ ->
        let p = LabProcess("p")
        let m = Material("Sample1")
        p.AddInputMaterial(m)
        p.RemoveInputMaterial(m)
        Expect.isFalse (m.InputOf |> Seq.exists (fun x -> x = p))
            "m.InputOf should no longer contain p after RemoveInputMaterial"

    testCase "AddOutput — material OutputOf updated" <| fun _ ->
        let p = LabProcess("p")
        let m = Material("Sample2")
        p.AddOutputMaterial(m)
        Expect.isTrue (m.OutputOf |> Seq.exists (fun x -> x = p))
            "m.OutputOf should contain p after AddOutputMaterial"

    testCase "RemoveOutput — material OutputOf cleared" <| fun _ ->
        let p = LabProcess("p")
        let m = Material("Sample2")
        p.AddOutputMaterial(m)
        p.RemoveOutputMaterial(m)
        Expect.isFalse (m.OutputOf |> Seq.exists (fun x -> x = p))
            "m.OutputOf should no longer contain p after RemoveOutputMaterial"

    testCase "AddOutput — data OutputOf updated" <| fun _ ->
        let p = LabProcess("p")
        let d = Data("output.csv")
        p.AddOutputData(d)
        Expect.isTrue (d.OutputOf |> Seq.exists (fun x -> x = p))
            "d.OutputOf should contain p after AddOutputData"

    testCase "RemoveOutput — data OutputOf cleared" <| fun _ ->
        let p = LabProcess("p")
        let d = Data("output.csv")
        p.AddOutputData(d)
        p.RemoveOutputData(d)
        Expect.isFalse (d.OutputOf |> Seq.exists (fun x -> x = p))
            "d.OutputOf should no longer contain p after RemoveOutputData"

    testCase "two processes sharing a node" <| fun _ ->
        let p1 = LabProcess("p1")
        let p2 = LabProcess("p2")
        let m  = Material("SharedSample")
        p1.AddInputMaterial(m)
        p2.AddInputMaterial(m)
        Expect.equal m.InputOf.Count 2 "SharedSample.InputOf should contain both processes"
        Expect.isTrue (m.InputOf |> Seq.exists (fun x -> x = p1)) "p1 in InputOf"
        Expect.isTrue (m.InputOf |> Seq.exists (fun x -> x = p2)) "p2 in InputOf"

    testCase "AddProcess — ProcessOf set" <| fun _ ->
        let ds = Dataset("DS-A")
        let p  = LabProcess("p1")
        ds.AddProcess(p)
        Expect.isSome p.ProcessOf "ProcessOf should be Some after AddProcess"
        Expect.equal p.ProcessOf.Value ds "ProcessOf should point to DS-A"

    testCase "RemoveProcess — ProcessOf cleared" <| fun _ ->
        let ds = Dataset("DS-A")
        let p  = LabProcess("p1")
        ds.AddProcess(p)
        ds.RemoveProcess(p)
        Expect.isNone p.ProcessOf "ProcessOf should be None after RemoveProcess"

    testCase "AddPart — PartOf set" <| fun _ ->
        let parent = Dataset("parent")
        let child  = Dataset("child")
        parent.AddPart(child)
        Expect.isSome child.PartOf "PartOf should be Some after AddPart"
        Expect.equal child.PartOf.Value parent "PartOf should point to parent"

    testCase "RemovePart — PartOf cleared" <| fun _ ->
        let parent = Dataset("parent")
        let child  = Dataset("child")
        parent.AddPart(child)
        parent.RemovePart(child)
        Expect.isNone child.PartOf "PartOf should be None after RemovePart"

    testCase "re-adding after removal re-establishes back-edge" <| fun _ ->
        let p = LabProcess("p")
        let m = Material("Sample1")
        p.AddInputMaterial(m)
        p.RemoveInputMaterial(m)
        Expect.isFalse (m.InputOf |> Seq.exists (fun x -> x = p)) "Back-edge cleared after removal"
        p.AddInputMaterial(m)
        Expect.isTrue (m.InputOf |> Seq.exists (fun x -> x = p)) "Back-edge re-established after re-add"

]
