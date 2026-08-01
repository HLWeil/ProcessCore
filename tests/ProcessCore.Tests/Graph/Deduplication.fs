module ProcessCore.Tests.Graph.Deduplication

open Fable.Pyxpecto
open ProcessCore
open ProcessCore.Tests.Fixtures

let tests = testList "Deduplication" [

    testCase "reassigning the same input keeps one back-edge" <| fun _ ->
        let f = makeFixtureA()
        f.P2.SetInputSample(f.Sample1)
        Expect.equal f.Sample1.InputOf.Count 1 "a singular input has one back-edge per process"

    testCase "reassigning the same output keeps one back-edge" <| fun _ ->
        let f = makeFixtureA()
        f.P1.SetOutputSample(f.Sample1)
        Expect.equal f.Sample1.OutputOf.Count 1 "a singular output has one back-edge per process"

    testCase "shared node is same object instance" <| fun _ ->
        let f = makeFixtureA()
        // p2's input should be the exact same object as f.Sample1 (not a copy)
        let inputNode =
            match f.P2.Input with
            | Some (SampleNode m) -> m
            | _ -> failwith "Expected sample input"
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

    testCase "AddParameter (recipe): duplicate ignored" <| fun _ ->
        let proto  = Recipe("extraction")
        let fp     = FormalParameter("temperature")
        proto.AddParameter(fp)
        proto.AddParameter(fp)
        Expect.equal proto.Parameters.Count 1 "Adding FP with same name twice → one entry"

    testCase "AddComponent: duplicate ignored" <| fun _ ->
        let proto = Recipe("extraction")
        let pv    = Annotation("instrument", value = "Orbitrap")
        proto.AddComponent(pv)
        proto.AddComponent(pv)
        Expect.equal proto.Components.Count 1 "Adding same PV to Component twice → one entry"

    testCase "Share samples with same name across dataset" <| fun _ ->

        let d = Dataset("MyDataset")

        let process1 = Process("Process1")
        let process2 = Process("Process2")

        d.AddProcess(process1)
        d.AddProcess(process2)

        // Pooling of inputs into a single output
        // InputOne  \
        //            -> Process 1/ Process2 -> Output 1
        // OutputTwo /

        process1.SetInputSample(Sample("InputOne"))
        process2.SetInputSample(Sample("InputTwo"))

        process1.SetOutputSample(Sample("TheOutput"))
        process2.SetOutputSample(Sample("TheOutput"))

        Expect.equal 2 (process1.Output.Value.UpstreamNodes().Count) "TheOutput should have two upstream nodes (one from each process)"

    testList "IONodeRegistry" [

        testCase "pre-wired process: nodes registered when AddProcess is called" <| fun _ ->
            let p = Process("p")
            p.SetOutputSample(Sample("mat"))
            let d = Dataset("DS")
            d.AddProcess(p)
            // Re-adding an equal node should resolve to the same instance already in the registry
            let second = Process("p2")
            d.AddProcess(second)
            second.SetOutputSample(Sample("mat"))
            let inst1 = match p.Output.Value    with SampleNode m -> m | _ -> failwith "not sample"
            let inst2 = match second.Output.Value with SampleNode m -> m | _ -> failwith "not sample"
            Expect.isTrue (obj.ReferenceEquals(inst1, inst2))
                "Node added after AddProcess should resolve to the same canonical instance"

        testCase "two pre-wired processes: equal nodes become same instance after AddProcess" <| fun _ ->
            let p1 = Process("p1")
            let p2 = Process("p2")
            p1.SetOutputSample(Sample("shared"))
            p2.SetOutputSample(Sample("shared"))
            let d = Dataset("DS")
            d.AddProcess(p1)
            d.AddProcess(p2)
            let inst1 = match p1.Output.Value with SampleNode m -> m | _ -> failwith "not sample"
            let inst2 = match p2.Output.Value with SampleNode m -> m | _ -> failwith "not sample"
            Expect.isTrue (obj.ReferenceEquals(inst1, inst2))
                "Both processes should share the exact same canonical node object after being added to the dataset"

        testCase "RemoveProcess evicts a node no longer referenced" <| fun _ ->
            let p = Process("p")
            let d = Dataset("DS")
            d.AddProcess(p)
            p.SetOutputSample(Sample("evictMe"))
            let canonical = match p.Output.Value with SampleNode m -> m | _ -> failwith "not sample"
            d.RemoveProcess(p)
            // After removal, adding a new equal node should NOT resolve to the old canonical instance
            let p2 = Process("p2")
            d.AddProcess(p2)
            p2.SetOutputSample(Sample("evictMe"))
            let newInst = match p2.Output.Value with SampleNode m -> m | _ -> failwith "not sample"
            Expect.isFalse (obj.ReferenceEquals(canonical, newInst))
                "Evicted node should not be reused after the process that held it was removed"

        testCase "RemoveProcess does not evict a node shared with a surviving process" <| fun _ ->
            let p1 = Process("p1")
            let p2 = Process("p2")
            let d  = Dataset("DS")
            d.AddProcess(p1)
            d.AddProcess(p2)
            p1.SetOutputSample(Sample("shared"))
            p2.SetOutputSample(Sample("shared"))
            let canonical = match p1.Output.Value with SampleNode m -> m | _ -> failwith "not sample"
            d.RemoveProcess(p1)
            // p2 still holds the node; a third process should still resolve to the same canonical instance
            let p3 = Process("p3")
            d.AddProcess(p3)
            p3.SetOutputSample(Sample("shared"))
            let resolved = match p3.Output.Value with SampleNode m -> m | _ -> failwith "not sample"
            Expect.isTrue (obj.ReferenceEquals(canonical, resolved))
                "Canonical node must survive as long as at least one process still references it"

        testCase "AddPart propagates child nodes into parent registry" <| fun _ ->
            let child  = Dataset("child")
            let parent = Dataset("parent")
            let p = Process("p")
            child.AddProcess(p)
            p.SetOutputSample(Sample("node"))
            let childInst = match p.Output.Value with SampleNode m -> m | _ -> failwith "not sample"
            parent.AddPart(child)
            // A new process added to parent with an equal node should resolve to the child's canonical instance
            let p2 = Process("p2")
            parent.AddProcess(p2)
            p2.SetOutputSample(Sample("node"))
            let parentInst = match p2.Output.Value with SampleNode m -> m | _ -> failwith "not sample"
            Expect.isTrue (obj.ReferenceEquals(childInst, parentInst))
                "Node from child dataset should be canonical in the parent registry after AddPart"

        testCase "canonicalizing Samples merges compatible metadata and preserves back-edges" <| fun _ ->
            let parent = Dataset("parent")
            let canonicalAnnotation = Annotation("organism", value = "plant")
            let canonical = Sample("shared", additionalProperty = [ canonicalAnnotation ])
            let consumer = Process("consumer")
            parent.AddProcess(consumer)
            consumer.SetInputSample(canonical)

            let child = Dataset("child")
            let enrichedAnnotation =
                Annotation(
                    "organism",
                    value = "plant",
                    valueTAN = "https://example.org/plant",
                    additionalType = "CharacteristicValue")
            enrichedAnnotation.SetProperty("source", "study")
            let incoming =
                Sample(
                    "shared",
                    additionalType = "Sample",
                    additionalProperty = [
                        enrichedAnnotation
                        Annotation("temperature", value = "25", unit = "degree Celsius", additionalType = "FactorValue")
                    ])
            incoming.SetProperty("label", "enriched sample")
            let producer = Process("producer")
            producer.SetOutputSample(incoming)
            child.AddProcess(producer)

            parent.AddPart(child)

            let resolved = producer.OutputSample().Value
            Expect.isTrue (obj.ReferenceEquals(canonical, resolved)) "the first Sample remains canonical"
            Expect.equal canonical.AdditionalType (Some "Sample") "missing Sample type is filled"
            Expect.equal canonical.AdditionalProperty.Count 2 "matching and distinct annotations are merged"
            Expect.equal canonicalAnnotation.ValueTAN (Some "https://example.org/plant") "matching annotation metadata is enriched"
            Expect.equal canonicalAnnotation.AdditionalType (Some "CharacteristicValue") "matching annotation type is enriched"
            Expect.equal (canonicalAnnotation.TryGetPropertyValue("source") |> Option.map string) (Some "study")
                "matching annotation dynamic metadata is enriched"
            Expect.equal (canonical.TryGetPropertyValue("label") |> Option.map string) (Some "enriched sample")
                "Sample dynamic metadata is enriched"
            Expect.isTrue (canonical.InputOf.Contains(consumer)) "the canonical Sample keeps its input back-edge"
            Expect.isTrue (canonical.OutputOf.Contains(producer)) "the canonical Sample receives the output back-edge"

        testCase "canonicalizing Data merges compatible metadata and nested parts" <| fun _ ->
            let parent = Dataset("parent")
            let canonicalPart = Data("data/part.csv")
            let canonical = Data("data/result.csv", hasPart = [ canonicalPart ])
            parent.AddDataFile(canonical)

            let incomingPart = Data("data/part.csv", encodingFormat = "text/csv")
            incomingPart.SetProperty("label", "table part")
            let incoming =
                Data(
                    "data/result.csv",
                    selectorFormat = "https://example.org/selector",
                    encodingFormat = "text/csv",
                    additionalType = "Processed Data",
                    hasPart = [ incomingPart ],
                    additionalProperty = [ Annotation("quality", value = "checked") ])
            incoming.SetProperty("checksum", "abc123")
            let child = Dataset("child", dataFiles = [ incoming ])

            parent.AddPart(child)

            Expect.isTrue (obj.ReferenceEquals(canonical, child.DataFiles.[0])) "the first Data remains canonical"
            Expect.equal canonical.SelectorFormat (Some "https://example.org/selector") "missing selector format is filled"
            Expect.equal canonical.EncodingFormat (Some "text/csv") "missing encoding format is filled"
            Expect.equal canonical.AdditionalType (Some "Processed Data") "missing Data type is filled"
            Expect.equal canonical.AdditionalProperty.Count 1 "Data annotations are merged"
            Expect.equal (canonical.TryGetPropertyValue("checksum") |> Option.map string) (Some "abc123")
                "Data dynamic metadata is enriched"
            Expect.equal canonical.HasPart.Count 1 "equivalent nested Data parts remain deduplicated"
            Expect.isTrue (obj.ReferenceEquals(canonicalPart, canonical.HasPart.[0])) "the first nested Data remains canonical"
            Expect.equal canonicalPart.EncodingFormat (Some "text/csv") "nested Data metadata is enriched recursively"
            Expect.equal (canonicalPart.TryGetPropertyValue("label") |> Option.map string) (Some "table part")
                "nested Data dynamic metadata is enriched"

        testCase "canonicalizing equal nodes rejects conflicting scalar metadata" <| fun _ ->
            let dataset = Dataset("dataset")
            dataset.CanonicalizeNode(SampleNode(Sample("shared", additionalType = "Sample"))) |> ignore
            Expect.throws
                (fun () -> dataset.CanonicalizeNode(SampleNode(Sample("shared", additionalType = "UnexpectedType"))) |> ignore)
                "conflicting Sample types must be reported"

        testCase "canonicalizing Source and Sample roles normalizes to Sample" <| fun _ ->
            let dataset = Dataset("dataset")
            let canonical = Sample("shared", additionalType = "Source")
            dataset.CanonicalizeNode(SampleNode canonical) |> ignore
            dataset.CanonicalizeNode(SampleNode(Sample("shared", additionalType = "Sample"))) |> ignore
            Expect.equal canonical.AdditionalType (Some "Sample") "Sample subsumes its Source endpoint role"

        testCase "canonicalizing equal nodes rejects conflicting dynamic metadata" <| fun _ ->
            let dataset = Dataset("dataset")
            let canonical = Sample("shared")
            canonical.SetProperty("label", "first")
            dataset.CanonicalizeNode(SampleNode canonical) |> ignore
            let incoming = Sample("shared")
            incoming.SetProperty("label", "second")
            Expect.throws
                (fun () -> dataset.CanonicalizeNode(SampleNode incoming) |> ignore)
                "conflicting dynamic properties must be reported"

        testCase "canonicalizing equal nodes rejects conflicting matching-annotation metadata" <| fun _ ->
            let dataset = Dataset("dataset")
            let first = Annotation("organism", value = "plant", valueTAN = "https://example.org/plant-a")
            dataset.CanonicalizeNode(SampleNode(Sample("shared", additionalProperty = [ first ]))) |> ignore
            let second = Annotation("organism", value = "plant", valueTAN = "https://example.org/plant-b")
            Expect.throws
                (fun () ->
                    dataset.CanonicalizeNode(SampleNode(Sample("shared", additionalProperty = [ second ])))
                    |> ignore)
                "conflicting metadata on matching annotations must be reported"

        testCase "RemovePart evicts child-only nodes from parent registry" <| fun _ ->
            let child  = Dataset("child")
            let parent = Dataset("parent")
            let p = Process("p")
            child.AddProcess(p)
            p.SetOutputSample(Sample("childOnly"))
            parent.AddPart(child)
            let canonicalInParent = match p.Output.Value with SampleNode m -> m | _ -> failwith "not sample"
            parent.RemovePart(child)
            // After removal, adding an equal node to the parent should NOT resolve to the old instance
            let p2 = Process("p2")
            parent.AddProcess(p2)
            p2.SetOutputSample(Sample("childOnly"))
            let newInst = match p2.Output.Value with SampleNode m -> m | _ -> failwith "not sample"
            Expect.isFalse (obj.ReferenceEquals(canonicalInParent, newInst))
                "Child-only node must be evicted from parent registry after RemovePart"

        testCase "RemovePart: detached child rebuilds its own registry" <| fun _ ->
            let child  = Dataset("child")
            let parent = Dataset("parent")
            let p = Process("p")
            child.AddProcess(p)
            p.SetOutputSample(Sample("node"))
            let instanceBeforeAttach = match p.Output.Value with SampleNode m -> m | _ -> failwith "not sample"
            parent.AddPart(child)
            parent.RemovePart(child)
            // After detach, a new process added to the child should canonicalize against its own registry
            let p2 = Process("p2")
            child.AddProcess(p2)
            p2.SetOutputSample(Sample("node"))
            let resolvedInChild = match p2.Output.Value with SampleNode m -> m | _ -> failwith "not sample"
            Expect.isTrue (obj.ReferenceEquals(instanceBeforeAttach, resolvedInChild))
                "Detached child should canonicalize new equal nodes against its own rebuilt registry"

    ]
]
