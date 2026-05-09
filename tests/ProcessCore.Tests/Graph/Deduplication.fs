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

    testList "IONodeRegistry" [

        testCase "pre-wired process: nodes registered when AddProcess is called" <| fun _ ->
            let p = LabProcess("p")
            p.AddOutputMaterial(Material("mat"))
            let d = Dataset("DS")
            d.AddProcess(p)
            // Re-adding an equal node should resolve to the same instance already in the registry
            let second = LabProcess("p2")
            d.AddProcess(second)
            second.AddOutputMaterial(Material("mat"))
            let inst1 = match p.Outputs.[0]    with MaterialNode m -> m | _ -> failwith "not material"
            let inst2 = match second.Outputs.[0] with MaterialNode m -> m | _ -> failwith "not material"
            Expect.isTrue (obj.ReferenceEquals(inst1, inst2))
                "Node added after AddProcess should resolve to the same canonical instance"

        testCase "two pre-wired processes: equal nodes become same instance after AddProcess" <| fun _ ->
            let p1 = LabProcess("p1")
            let p2 = LabProcess("p2")
            p1.AddOutputMaterial(Material("shared"))
            p2.AddOutputMaterial(Material("shared"))
            let d = Dataset("DS")
            d.AddProcess(p1)
            d.AddProcess(p2)
            let inst1 = match p1.Outputs.[0] with MaterialNode m -> m | _ -> failwith "not material"
            let inst2 = match p2.Outputs.[0] with MaterialNode m -> m | _ -> failwith "not material"
            Expect.isTrue (obj.ReferenceEquals(inst1, inst2))
                "Both processes should share the exact same canonical node object after being added to the dataset"

        testCase "RemoveProcess evicts a node no longer referenced" <| fun _ ->
            let p = LabProcess("p")
            let d = Dataset("DS")
            d.AddProcess(p)
            p.AddOutputMaterial(Material("evictMe"))
            let canonical = match p.Outputs.[0] with MaterialNode m -> m | _ -> failwith "not material"
            d.RemoveProcess(p)
            // After removal, adding a new equal node should NOT resolve to the old canonical instance
            let p2 = LabProcess("p2")
            d.AddProcess(p2)
            p2.AddOutputMaterial(Material("evictMe"))
            let newInst = match p2.Outputs.[0] with MaterialNode m -> m | _ -> failwith "not material"
            Expect.isFalse (obj.ReferenceEquals(canonical, newInst))
                "Evicted node should not be reused after the process that held it was removed"

        testCase "RemoveProcess does not evict a node shared with a surviving process" <| fun _ ->
            let p1 = LabProcess("p1")
            let p2 = LabProcess("p2")
            let d  = Dataset("DS")
            d.AddProcess(p1)
            d.AddProcess(p2)
            p1.AddOutputMaterial(Material("shared"))
            p2.AddOutputMaterial(Material("shared"))
            let canonical = match p1.Outputs.[0] with MaterialNode m -> m | _ -> failwith "not material"
            d.RemoveProcess(p1)
            // p2 still holds the node; a third process should still resolve to the same canonical instance
            let p3 = LabProcess("p3")
            d.AddProcess(p3)
            p3.AddOutputMaterial(Material("shared"))
            let resolved = match p3.Outputs.[0] with MaterialNode m -> m | _ -> failwith "not material"
            Expect.isTrue (obj.ReferenceEquals(canonical, resolved))
                "Canonical node must survive as long as at least one process still references it"

        testCase "AddPart propagates child nodes into parent registry" <| fun _ ->
            let child  = Dataset("child")
            let parent = Dataset("parent")
            let p = LabProcess("p")
            child.AddProcess(p)
            p.AddOutputMaterial(Material("node"))
            let childInst = match p.Outputs.[0] with MaterialNode m -> m | _ -> failwith "not material"
            parent.AddPart(child)
            // A new process added to parent with an equal node should resolve to the child's canonical instance
            let p2 = LabProcess("p2")
            parent.AddProcess(p2)
            p2.AddOutputMaterial(Material("node"))
            let parentInst = match p2.Outputs.[0] with MaterialNode m -> m | _ -> failwith "not material"
            Expect.isTrue (obj.ReferenceEquals(childInst, parentInst))
                "Node from child dataset should be canonical in the parent registry after AddPart"

        testCase "RemovePart evicts child-only nodes from parent registry" <| fun _ ->
            let child  = Dataset("child")
            let parent = Dataset("parent")
            let p = LabProcess("p")
            child.AddProcess(p)
            p.AddOutputMaterial(Material("childOnly"))
            parent.AddPart(child)
            let canonicalInParent = match p.Outputs.[0] with MaterialNode m -> m | _ -> failwith "not material"
            parent.RemovePart(child)
            // After removal, adding an equal node to the parent should NOT resolve to the old instance
            let p2 = LabProcess("p2")
            parent.AddProcess(p2)
            p2.AddOutputMaterial(Material("childOnly"))
            let newInst = match p2.Outputs.[0] with MaterialNode m -> m | _ -> failwith "not material"
            Expect.isFalse (obj.ReferenceEquals(canonicalInParent, newInst))
                "Child-only node must be evicted from parent registry after RemovePart"

        testCase "RemovePart: detached child rebuilds its own registry" <| fun _ ->
            let child  = Dataset("child")
            let parent = Dataset("parent")
            let p = LabProcess("p")
            child.AddProcess(p)
            p.AddOutputMaterial(Material("node"))
            let instanceBeforeAttach = match p.Outputs.[0] with MaterialNode m -> m | _ -> failwith "not material"
            parent.AddPart(child)
            parent.RemovePart(child)
            // After detach, a new process added to the child should canonicalize against its own registry
            let p2 = LabProcess("p2")
            child.AddProcess(p2)
            p2.AddOutputMaterial(Material("node"))
            let resolvedInChild = match p2.Outputs.[0] with MaterialNode m -> m | _ -> failwith "not material"
            Expect.isTrue (obj.ReferenceEquals(instanceBeforeAttach, resolvedInChild))
                "Detached child should canonicalize new equal nodes against its own rebuilt registry"

    ]
]
