module ProcessCore.Tests.Graph.Deduplication

open Fable.Pyxpecto
open ProcessCore
open ProcessCore.Tests.Fixtures

let tests = testList "Deduplication" [

    testCase "AddInput: identical node can be added" <| fun _ ->
        let f = makeFixtureA()
        // Sample1 is already an input of p2 (added during fixture construction)
        let countBefore = f.P2.Inputs.Count
        f.P2.AddInputMaterial(f.Sample1)
        Expect.equal f.P2.Inputs.Count (countBefore + 1) "Adding Sample1 again to p2 inputs should create a second entry"

    testCase "AddOutput: identical node can be added" <| fun _ ->
        let f = makeFixtureA()
        // Sample1 is already an output of p1
        let countBefore = f.P1.Outputs.Count
        f.P1.AddOutputMaterial(f.Sample1)
        Expect.equal f.P1.Outputs.Count (countBefore + 1) "Adding Sample1 again to p1 outputs should create a second entry"

    testCase "shared node is same object instance" <| fun _ ->
        let f = makeFixtureA()
        // p2's input should be the exact same object as f.Sample1 (not a copy)
        let inputNode =
            f.P2.Inputs
            |> Seq.pick (fun n -> match n with | MaterialNode m when m = f.Sample1 -> Some m | _ -> None)
        Expect.isTrue (obj.ReferenceEquals(inputNode, f.Sample1)) "Deduplicated node should be the same object instance"

    testCase "AddProcess: duplicate ignored" <| fun _ ->
        let f           = makeFixtureA()
        let countBefore = f.DS.Processes.Count
        f.DS.AddProcess(f.P1)
        Expect.equal f.DS.Processes.Count countBefore "Adding p1 to DS-A a second time should leave count unchanged"

    testCase "AddPart: duplicate child ignored" <| fun _ ->
        let f     = makeFixtureD()
        let countBefore = f.Parent.HasPart.Count
        f.Parent.AddPart(f.Child1)
        Expect.equal f.Parent.HasPart.Count countBefore "Adding child1 to parent a second time should leave count unchanged"

    testCase "AddParameterValue: duplicate not ignored" <| fun _ ->
        let f           = makeFixtureA()
        let countBefore = f.P1.ParameterValue.Count
        let pv          = f.P1.ParameterValue.[0]
        f.P1.AddParameterValue(pv)
        Expect.equal f.P1.ParameterValue.Count (countBefore + 1) "Adding the same PV again should create another entry"

    testCase "AddParameter (protocol): duplicate ignored" <| fun _ ->
        let proto  = LabProtocol("extraction")
        let fp     = FormalParameter("temperature")
        proto.AddParameter(fp)
        proto.AddParameter(fp)
        Expect.equal proto.Parameters.Count 1 "Adding FP with same name twice → one entry"

    testCase "AddLabEquipment: duplicate ignored" <| fun _ ->
        let proto = LabProtocol("extraction")
        let pv    = PropertyValue("instrument", value = "Orbitrap")
        proto.AddLabEquipment(pv)
        proto.AddLabEquipment(pv)
        Expect.equal proto.LabEquipment.Count 1 "Adding same PV to LabEquipment twice → one entry"

    testCase "Share materials with same name across dataset" <| fun _ ->
        
        let d = Dataset("MyDataset")

        let process1 = LabProcess("MyProcess")
        let process2 = LabProcess("MyProcess")

        d.AddProcess(process1)
        d.AddProcess(process2)

        // Pooling of inputs into a single output
        // InputOne  \
        //            -> Process 1/ Process2 -> Output 1
        // OutputTwo /

        process1.AddInputMaterial(Material("InputOne"))
        process2.AddInputMaterial(Material("InputTwo"))

        process1.AddOutputMaterial(Material("TheOutput"))
        process2.AddOutputMaterial(Material("TheOutput"))

        Expect.equal 2 (process1.Outputs[0].UpstreamNodes().Count) "TheOutput should have two upstream nodes (one from each process)"
]
