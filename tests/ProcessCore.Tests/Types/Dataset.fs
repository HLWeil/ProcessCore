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

    testCase "CollapseProcesses groups same name and equal process values" <| fun _ ->
        let nodeName (node: IONode) =
            match node with
            | MaterialNode m -> m.Name
            | DataNode d -> d.Path

        let makeProcess input value output =
            let p = LabProcess("MyProcess")
            p.AddInputMaterial(Material(input))
            p.AddParameterValue(PropertyValue("status", value = value, additionalType = "ParameterValue"))
            p.AddOutputMaterial(Material(output))
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
        let p1 = LabProcess("ProcessA")
        p1.AddInputMaterial(Material("Input1"))
        p1.AddParameterValue(PropertyValue("status", value = "ValueX", additionalType = "ParameterValue"))
        p1.AddOutputMaterial(Material("Output1"))

        let p2 = LabProcess("ProcessB")
        p2.AddInputMaterial(Material("Input2"))
        p2.AddParameterValue(PropertyValue("status", value = "ValueX", additionalType = "ParameterValue"))
        p2.AddOutputMaterial(Material("Output2"))

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
        let p1 = LabProcess("MyProcess")
        p1.AddInputMaterial(Material("Input1"))
        p1.AddParameterValue(PropertyValue("status", value = "ValueX", additionalType = "ParameterValue"))
        p1.AddOutputMaterial(Material("Output1"))

        let p2 = LabProcess("MyProcess")
        p2.AddInputMaterial(Material("Input2"))
        p2.AddParameterValue(PropertyValue("status", value = "ValueY", additionalType = "ParameterValue"))
        p2.AddOutputMaterial(Material("Output2"))

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
            | MaterialNode m -> m.Name
            | DataNode d -> d.Path

        let multi = LabProcess("MyProcess")
        multi.AddInputMaterial(Material("Input1"))
        multi.AddInputMaterial(Material("Input2"))
        multi.AddParameterValue(PropertyValue("status", value = "ValueX", additionalType = "ParameterValue"))
        multi.AddOutputMaterial(Material("Output1"))
        multi.AddOutputMaterial(Material("Output2"))

        let single = LabProcess("MyProcess")
        single.AddInputMaterial(Material("Input3"))
        single.AddParameterValue(PropertyValue("status", value = "ValueX", additionalType = "ParameterValue"))
        single.AddOutputMaterial(Material("Output3"))

        let separate = LabProcess("MyProcess")
        separate.AddInputMaterial(Material("Input4"))
        separate.AddParameterValue(PropertyValue("status", value = "ValueY", additionalType = "ParameterValue"))
        separate.AddOutputMaterial(Material("Output4"))

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
            | MaterialNode m -> m.Name
            | DataNode d -> d.Path

        let p1 = LabProcess("InputOnly")
        p1.AddInputMaterial(Material("Input1"))
        p1.AddParameterValue(PropertyValue("status", value = "ValueX", additionalType = "ParameterValue"))

        let p2 = LabProcess("InputOnly")
        p2.AddInputMaterial(Material("Input2"))
        p2.AddParameterValue(PropertyValue("status", value = "ValueX", additionalType = "ParameterValue"))

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
            | MaterialNode m -> m.Name
            | DataNode d -> d.Path

        let addStatus (p: LabProcess) =
            p.AddParameterValue(PropertyValue("status", value = "ValueX", additionalType = "ParameterValue"))
            p

        let both1 = addStatus (LabProcess("MixedShape"))
        both1.AddInputMaterial(Material("BothInput1"))
        both1.AddOutputMaterial(Material("BothOutput1"))

        let both2 = addStatus (LabProcess("MixedShape"))
        both2.AddInputMaterial(Material("BothInput2"))
        both2.AddOutputMaterial(Material("BothOutput2"))

        let inputOnly1 = addStatus (LabProcess("MixedShape"))
        inputOnly1.AddInputMaterial(Material("OnlyInput1"))

        let inputOnly2 = addStatus (LabProcess("MixedShape"))
        inputOnly2.AddInputMaterial(Material("OnlyInput2"))

        let outputOnly1 = addStatus (LabProcess("MixedShape"))
        outputOnly1.AddOutputMaterial(Material("OnlyOutput1"))

        let outputOnly2 = addStatus (LabProcess("MixedShape"))
        outputOnly2.AddOutputMaterial(Material("OnlyOutput2"))

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
            | MaterialNode m -> m.Name
            | DataNode d -> d.Path

        let p1 = LabProcess("MyProcess")
        p1.AddInputMaterial(Material("Input1"))
        p1.AddParameterValue(PropertyValue("status", value = "ValueX", additionalType = "ParameterValue"))
        p1.AddOutputMaterial(Material("Output1"))

        let p2 = LabProcess("MyProcess")
        p2.AddInputMaterial(Material("Input2"))
        p2.AddParameterValue(PropertyValue("status", value = "ValueX", additionalType = "ParameterValue"))
        p2.AddOutputMaterial(Material("Output2"))

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
