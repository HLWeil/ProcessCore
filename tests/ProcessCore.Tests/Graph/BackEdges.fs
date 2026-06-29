module ProcessCore.Tests.Graph.BackEdges

open Fable.Pyxpecto
open ProcessCore

let tests = testList "BackEdges" [

    testCase "AddInput — sample InputOf updated" <| fun _ ->
        let p = Process("p")
        let m = Sample("Sample1")
        p.AddInputSample(m)
        Expect.isTrue (m.InputOf |> Seq.exists (fun x -> x = p))
            "m.InputOf should contain p after AddInputSample"

    testCase "AddInput — data InputOf updated" <| fun _ ->
        let p = Process("p")
        let d = Data("file.csv")
        p.AddInputData(d)
        Expect.isTrue (d.InputOf |> Seq.exists (fun x -> x = p))
            "d.InputOf should contain p after AddInputData"

    testCase "RemoveInput — sample InputOf cleared" <| fun _ ->
        let p = Process("p")
        let m = Sample("Sample1")
        p.AddInputSample(m)
        p.RemoveInputSample(m)
        Expect.isFalse (m.InputOf |> Seq.exists (fun x -> x = p))
            "m.InputOf should no longer contain p after RemoveInputSample"

    testCase "AddOutput — sample OutputOf updated" <| fun _ ->
        let p = Process("p")
        let m = Sample("Sample2")
        p.AddOutputSample(m)
        Expect.isTrue (m.OutputOf |> Seq.exists (fun x -> x = p))
            "m.OutputOf should contain p after AddOutputSample"

    testCase "RemoveOutput — sample OutputOf cleared" <| fun _ ->
        let p = Process("p")
        let m = Sample("Sample2")
        p.AddOutputSample(m)
        p.RemoveOutputSample(m)
        Expect.isFalse (m.OutputOf |> Seq.exists (fun x -> x = p))
            "m.OutputOf should no longer contain p after RemoveOutputSample"

    testCase "AddOutput — data OutputOf updated" <| fun _ ->
        let p = Process("p")
        let d = Data("output.csv")
        p.AddOutputData(d)
        Expect.isTrue (d.OutputOf |> Seq.exists (fun x -> x = p))
            "d.OutputOf should contain p after AddOutputData"

    testCase "RemoveOutput — data OutputOf cleared" <| fun _ ->
        let p = Process("p")
        let d = Data("output.csv")
        p.AddOutputData(d)
        p.RemoveOutputData(d)
        Expect.isFalse (d.OutputOf |> Seq.exists (fun x -> x = p))
            "d.OutputOf should no longer contain p after RemoveOutputData"

    testCase "two processes sharing a node" <| fun _ ->
        let p1 = Process("p1")
        let p2 = Process("p2")
        let m  = Sample("SharedSample")
        p1.AddInputSample(m)
        p2.AddInputSample(m)
        Expect.equal m.InputOf.Count 2 "SharedSample.InputOf should contain both processes"
        Expect.isTrue (m.InputOf |> Seq.exists (fun x -> x = p1)) "p1 in InputOf"
        Expect.isTrue (m.InputOf |> Seq.exists (fun x -> x = p2)) "p2 in InputOf"

    testCase "AddProcess — ProcessOf set" <| fun _ ->
        let ds = Dataset("DS-A")
        let p  = Process("p1")
        ds.AddProcess(p)
        Expect.isSome p.ProcessOf "ProcessOf should be Some after AddProcess"
        Expect.equal p.ProcessOf.Value ds "ProcessOf should point to DS-A"

    testCase "RemoveProcess — ProcessOf cleared" <| fun _ ->
        let ds = Dataset("DS-A")
        let p  = Process("p1")
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
        let p = Process("p")
        let m = Sample("Sample1")
        p.AddInputSample(m)
        p.RemoveInputSample(m)
        Expect.isFalse (m.InputOf |> Seq.exists (fun x -> x = p)) "Back-edge cleared after removal"
        p.AddInputSample(m)
        Expect.isTrue (m.InputOf |> Seq.exists (fun x -> x = p)) "Back-edge re-established after re-add"

    // Two *distinct* Process objects with the same name must both appear in a
    // shared sample's OutputOf back-edge set. The HashSet uses reference equality
    // so name-equal but distinct objects are stored separately.
    testCase "two same-named processes both appear in OutputOf back-edges" <| fun _ ->
        let p1 = Process("SameName")
        let p2 = Process("SameName")
        let m  = Sample("SharedOutput")
        p1.AddOutputSample(m)
        p2.AddOutputSample(m)
        Expect.equal m.OutputOf.Count 2
            "OutputOf should contain both distinct process objects even though they share a name"

]
