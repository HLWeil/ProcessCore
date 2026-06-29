module ProcessCore.Tests.Types.IONode

open Fable.Pyxpecto
open ProcessCore
open ProcessCore.Tests

let tests = testList "IONode" [

    testCase "SampleNode.Key()" <| fun _ ->
        let m    = Sample("Sample1")
        let node = SampleNode m
        Expect.equal (node.Key()) "M:Sample1" "SampleNode key should be M:<name>"

    testCase "DataNode.Key() without selector" <| fun _ ->
        let d    = Data("results.csv")
        let node = DataNode d
        Expect.equal (node.Key()) "D:results.csv" "DataNode key without selector should be D:<path>"

    testCase "DataNode.Key() with selector" <| fun _ ->
        let d    = Data("results.csv", selector = "Sheet1")
        let node = DataNode d
        Expect.equal (node.Key()) "D:results.csvSheet1" "DataNode key with selector should concatenate path+selector"

    testCase "EqualTo same sample" <| fun _ ->
        let m1 = Sample("Sample1")
        let m2 = Sample("Sample1")
        Expect.isTrue (SampleNode(m1).EqualTo(SampleNode(m2))) "Same-valued SampleNodes should be equal"

    testCase "EqualTo same data" <| fun _ ->
        let d1 = Data("results.csv", selector = "Sheet1")
        let d2 = Data("results.csv", selector = "Sheet1")
        Expect.isTrue (DataNode(d1).EqualTo(DataNode(d2))) "Same-valued DataNodes should be equal"

    testCase "EqualTo different data" <| fun _ ->
        let d1 = Data("results.csv", selector = "Sheet1")
        let d2 = Data("results.csv", selector = "Sheet2")
        Expect.isFalse (DataNode(d1).EqualTo(DataNode(d2))) "Different Data selectors should not be equal"

    testCase "EqualTo different types" <| fun _ ->
        let m = Sample("Sample1")
        let d = Data("Sample1")
        Expect.isFalse (SampleNode(m).EqualTo(DataNode(d))) "SampleNode vs DataNode should not be equal"

    testCase "GetInputOf delegates to sample" <| fun _ ->
        let m    = Sample("Sample1")
        let p    = Process("p1")
        let node = SampleNode m
        p.AddInputSample(m)
        let inputOf = node.GetInputOf()
        Expect.isTrue (inputOf |> Seq.exists (fun x -> x = p)) "GetInputOf should return the process"

    testCase "GetInputOf delegates to data" <| fun _ ->
        let d    = Data("results.csv")
        let p    = Process("p1")
        let node = DataNode d
        p.AddInputData(d)
        let inputOf = node.GetInputOf()
        Expect.isTrue (inputOf |> Seq.exists (fun x -> x = p)) "GetInputOf should return the data-consuming process"

    testCase "GetOutputOf delegates to sample" <| fun _ ->
        let m    = Sample("Sample1")
        let p    = Process("p1")
        let node = SampleNode m
        p.AddOutputSample(m)
        let outputOf = node.GetOutputOf()
        Expect.isTrue (outputOf |> Seq.exists (fun x -> x = p)) "GetOutputOf should return the process"

    testCase "GetOutputOf delegates to data" <| fun _ ->
        let d    = Data("results.csv")
        let p    = Process("p1")
        let node = DataNode d
        p.AddOutputData(d)
        let outputOf = node.GetOutputOf()
        Expect.isTrue (outputOf |> Seq.exists (fun x -> x = p)) "GetOutputOf should return the data-producing process"

    testCase "IsRootNode: no predecessor in graph" <| fun _ ->
        let f = Fixtures.makeFixtureA()
        let node = SampleNode f.Source1
        Expect.isTrue (node.IsRootNode()) "Source1 has no predecessor → IsRootNode = true"

    testCase "IsRootNode: has predecessor" <| fun _ ->
        let f    = Fixtures.makeFixtureA()
        let node = SampleNode f.Sample1
        Expect.isFalse (node.IsRootNode()) "Sample1 has predecessor p1 → IsRootNode = false"

    testCase "IsFinalNode: no successor in graph" <| fun _ ->
        let f    = Fixtures.makeFixtureA()
        let node = DataNode f.RawData1
        Expect.isTrue (node.IsFinalNode()) "rawData1.csv has no successor → IsFinalNode = true"

    testCase "IsFinalNode: has successor" <| fun _ ->
        let f    = Fixtures.makeFixtureA()
        let node = SampleNode f.Sample1
        Expect.isFalse (node.IsFinalNode()) "Sample1 is consumed by p2 → IsFinalNode = false"

    testCase "IsRootNode: scoped to one dataset" <| fun _ ->
        // In Fixture D, Sample1 is output of p1 (in child1) but input of p2 (in child2).
        // When scoped to child2's processes, Sample1 looks like a root node.
        let f     = Fixtures.makeFixtureD()
        let node  = SampleNode f.Sample1
        let scope = f.Child2.Processes
        Expect.isTrue (node.IsRootNode(scope)) "Sample1 is root within child2 scope"
        // Without scope restriction it is not a root (p1 produces it)
        Expect.isFalse (node.IsRootNode()) "Sample1 is not root in the global graph"

]
