module ProcessCore.Tests.Graph.PathQueries

open Fable.Pyxpecto
open ProcessCore
open ProcessCore.Tests.Fixtures

let tests = testList "PathQueries" [

    // ── Path.Length / Head / Last ─────────────────────────────────────────────

    testCase "Path.Length" <| fun _ ->
        let f    = makeFixtureA()
        let path = Path(ResizeArray<Process>([| f.P1; f.P2; f.P3 |]))
        Expect.equal path.Length 3 "linear fixture → Length = 3"

    testCase "Path.Head" <| fun _ ->
        let f    = makeFixtureA()
        let path = Path(ResizeArray<Process>([| f.P1; f.P2; f.P3 |]))
        Expect.equal path.Head (Some f.P1) "Head is p1"

    testCase "Path.Last" <| fun _ ->
        let f    = makeFixtureA()
        let path = Path(ResizeArray<Process>([| f.P1; f.P2; f.P3 |]))
        Expect.equal path.Last (Some f.P3) "Last is p3"

    // ── empty Path ────────────────────────────────────────────────────────────

    testCase "empty Path" <| fun _ ->
        let path = Path(ResizeArray<Process>())
        Expect.equal path.Head   None "empty Head is None"
        Expect.equal path.Last   None "empty Last is None"
        Expect.equal path.Length 0    "empty Length is 0"
        Expect.equal (path.Nodes().Count) 0 "empty Nodes() is empty"

    // ── Path.Nodes / Samples / DataNodes ────────────────────────────────────

    testCase "Path.Nodes() deduplicates shared nodes" <| fun _ ->
        // p1 output = Sample1 = p2 input → appears only once
        let f    = makeFixtureA()
        let path = Path(ResizeArray<Process>([| f.P1; f.P2 |]))
        let keys = path.Nodes() |> Seq.map (fun n -> n.Key()) |> Set.ofSeq
        // Source1, Sample1, Sample2 — Sample1 is shared between p1 and p2
        Expect.equal keys (Set.ofList ["M:Source1";"M:Sample1";"M:Sample2"])
            "3 distinct nodes; Sample1 not doubled"

    testCase "Path.Samples()" <| fun _ ->
        let f    = makeFixtureA()
        let path = Path(ResizeArray<Process>([| f.P1; f.P2; f.P3 |]))
        let names = path.Samples() |> Seq.map (fun m -> m.Name) |> Set.ofSeq
        Expect.equal names (Set.ofList ["Source1";"Sample1";"Sample2"])
            "Samples excludes rawData1.csv"

    testCase "Path.DataNodes()" <| fun _ ->
        let f    = makeFixtureA()
        let path = Path(ResizeArray<Process>([| f.P1; f.P2; f.P3 |]))
        let paths_ = path.DataNodes() |> Seq.map (fun d -> d.Path) |> Set.ofSeq
        Expect.equal paths_ (Set.ofList ["rawData1.csv"]) "rawData1.csv is the only data node"

    // ── Path.ContainsNode ─────────────────────────────────────────────────────

    testCase "Path.ContainsNode — present" <| fun _ ->
        let f    = makeFixtureA()
        let path = Path(ResizeArray<Process>([| f.P1; f.P2; f.P3 |]))
        Expect.isTrue (path.ContainsNode(SampleNode f.Sample1))
            "Sample1 is in the path"

    testCase "Path.ContainsNode — absent" <| fun _ ->
        let f    = makeFixtureA()
        let path = Path(ResizeArray<Process>([| f.P1 |]))
        // rawData1 is an output of p3, not in the p1-only path
        Expect.isFalse (path.ContainsNode(DataNode f.RawData1))
            "rawData1 is not in a p1-only path"

    // ── Path.TerminalInputs / TerminalOutputs ─────────────────────────────────

    testCase "Path.TerminalInputs" <| fun _ ->
        let f    = makeFixtureA()
        let path = Path(ResizeArray<Process>([| f.P1; f.P2; f.P3 |]))
        let keys = path.TerminalInputs() |> Seq.map (fun n -> n.Key()) |> Set.ofSeq
        // Source1 is an input of p1 and never an output in the path
        Expect.equal keys (Set.ofList ["M:Source1"]) "Source1 is the only terminal input"

    testCase "Path.TerminalOutputs" <| fun _ ->
        let f    = makeFixtureA()
        let path = Path(ResizeArray<Process>([| f.P1; f.P2; f.P3 |]))
        let keys = path.TerminalOutputs() |> Seq.map (fun n -> n.Key()) |> Set.ofSeq
        // rawData1 is an output of p3 and never an input in the path
        Expect.equal keys (Set.ofList ["D:rawData1.csv"]) "rawData1.csv is the only terminal output"

    // ── Path.AllAnnotations / AnnotationsByName ─────────────────────────

    testCase "Path.AllAnnotations — all 4 sources" <| fun _ ->
        let f    = makeFixtureFourSources()
        let path = Path(ResizeArray<Process>([| f.Process |]))
        let pvs  = path.AllAnnotations()
        let names = pvs |> Seq.map (fun pv -> pv.Name) |> Set.ofSeq
        // ParamPV = temperature, InputPV = organism, OutputPV = growth_phase, ComponentPV = instrument
        Expect.isTrue (names.Contains "temperature")  "parameter source"
        Expect.isTrue (names.Contains "organism")     "input node source"
        Expect.isTrue (names.Contains "growth_phase") "output node source"
        Expect.isTrue (names.Contains "instrument")   "recipe component source"

    testCase "Path.AnnotationsByName" <| fun _ ->
        let f    = makeFixtureA()
        let path = Path(ResizeArray<Process>([| f.P1; f.P2; f.P3 |]))
        let pvs  = path.AnnotationsByName("temperature")
        Expect.equal pvs.Count 1 "exactly one temperature PV"
        Expect.equal pvs.[0].Value (Some "37") "value is 37"

    // ── Path.RecipeParameters ───────────────────────────────────────────────

    testCase "Path.RecipeParameters" <| fun _ ->
        let f    = makeFixtureA()
        let path = Path(ResizeArray<Process>([| f.P1; f.P2; f.P3 |]))
        let fps  = path.RecipeParameters()
        // p1 has temperature+rpm, p2 has no FPs defined in fixture, p3 no recipe
        let names = fps |> Seq.map (fun fp -> fp.Name) |> Set.ofSeq
        Expect.isTrue (names.Contains "temperature") "temperature FP from p1 recipe"
        Expect.isTrue (names.Contains "rpm")         "rpm FP from p1 recipe"

]
