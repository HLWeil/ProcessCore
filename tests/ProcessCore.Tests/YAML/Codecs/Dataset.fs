module ProcessCore.Yaml.Tests.Codecs.Dataset

open Fable.Pyxpecto
open ProcessCore
open ProcessCore.Yaml

let tests = testList "Dataset" [

    testCase "encode minimal" <| fun _ ->
        let ds   = Dataset("DS-1")
        let yaml = Yaml.Dataset.toYamlString None ds
        Expect.isTrue (yaml.Contains("identifier: DS-1")) "identifier"
        Expect.isTrue (yaml.Contains("type: Dataset"))    "type"

    testCase "encode and decode title" <| fun _ ->
        let original = Dataset("DS-1", title = "Proteomics assay")
        let yaml = Yaml.Dataset.toYamlString None original
        Expect.isTrue (yaml.Contains("title: Proteomics assay")) "title key"
        let decoded = Yaml.Dataset.fromYamlString true yaml
        Expect.equal decoded.Title (Some "Proteomics assay") "title round-trip"

    testCase "encode with processes" <| fun _ ->
        let ds   = Dataset("DS-1")
        ds.AddProcess(Process("p1"))
        let yaml = Yaml.Dataset.toYamlString None ds
        Expect.isTrue (yaml.Contains("processes")) "processes key"
        Expect.isTrue (yaml.Contains("p1"))        "process name"

    testCase "encode with hasPart" <| fun _ ->
        let parent = Dataset("parent")
        parent.AddPart(Dataset("child"))
        let yaml = Yaml.Dataset.toYamlString None parent
        Expect.isTrue (yaml.Contains("hasPart"))  "hasPart key"
        Expect.isTrue (yaml.Contains("child"))    "child identifier"

    testCase "encode with additionalProperty" <| fun _ ->
        let ds = Dataset("DS-1")
        ds.AddAdditionalProperty(Annotation("status", value = "complete"))
        let yaml = Yaml.Dataset.toYamlString None ds
        Expect.isTrue (yaml.Contains("additionalProperty")) "additionalProperty key"
        Expect.isTrue (yaml.Contains("status"))             "property name"

    testCase "decode minimal" <| fun _ ->
        let yaml = "type: Dataset\nidentifier: DS-1\n"
        let ds   = Yaml.Dataset.fromYamlString false yaml
        Expect.equal ds.Identifier "DS-1" "identifier"
        Expect.equal ds.Processes.Count 0 "no processes"
        Expect.equal ds.HasPart.Count   0 "no hasPart"

    testCase "id field goes to overflow — missing identifier throws" <| fun _ ->
        let yaml = "type: Dataset\nid: DS-1\n"
        Expect.throws (fun () -> Yaml.Dataset.fromYamlString false yaml |> ignore) "missing identifier throws"

    testCase "id field goes to overflow — present alongside identifier" <| fun _ ->
        let yaml = "type: Dataset\nidentifier: DS-1\nid: some-other-id\n"
        let ds   = Yaml.Dataset.fromYamlString false yaml
        Expect.equal ds.Identifier "DS-1" "identifier not overridden by id"
        let overflowId = ds.TryGetPropertyValue("id") |> Option.map string
        Expect.isSome overflowId "id stored in overflow"
        Expect.equal overflowId (Some "some-other-id") "id overflow value"

    testCase "decode with processes" <| fun _ ->
        let yaml = """type: Dataset
identifier: DS-1
processes:
  - type: Process
    name: p1
  - type: Process
    name: p2
"""
        let ds = Yaml.Dataset.fromYamlString false yaml
        Expect.equal ds.Processes.Count 2  "two processes"
        Expect.equal ds.Processes.[0].Name "p1" "first process"
        Expect.equal ds.Processes.[1].Name "p2" "second process"

    testCase "decode with hasPart as child datasets" <| fun _ ->
        let yaml = """type: Dataset
identifier: parent
hasPart:
  - type: Dataset
    identifier: child-a
  - type: Dataset
    identifier: child-b
"""
        let ds = Yaml.Dataset.fromYamlString false yaml
        Expect.equal ds.HasPart.Count 2       "two children"
        Expect.equal ds.HasPart.[0].Identifier "child-a" "first child"
        Expect.equal ds.HasPart.[1].Identifier "child-b" "second child"

    testCase "decode hasPart with empty type defaults to Dataset" <| fun _ ->
        // When hasPart item has no type field, it defaults to Dataset
        let yaml = """type: Dataset
identifier: parent
hasPart:
  - identifier: child-x
"""
        let ds = Yaml.Dataset.fromYamlString false yaml
        Expect.equal ds.HasPart.Count 1          "one child"
        Expect.equal ds.HasPart.[0].Identifier "child-x" "child identifier"

    testCase "decode with additionalProperty" <| fun _ ->
        let yaml = """type: Dataset
identifier: DS-1
additionalProperty:
  - type: Annotation
    name: status
    value: complete
"""
        let ds = Yaml.Dataset.fromYamlString false yaml
        Expect.equal ds.AdditionalProperty.Count 1         "one property"
        Expect.equal ds.AdditionalProperty.[0].Name "status" "property name"

    testCase "decode processes as id-references" <| fun _ ->
        let yaml = """type: Dataset
identifier: DS-1
processes:
  - some-process-id
"""
        let ds = Yaml.Dataset.fromYamlString false yaml
        Expect.equal ds.Processes.Count 0 "id ref processes skipped"

    testCase "ProcessOf back-edge after decode" <| fun _ ->
        let yaml = """type: Dataset
identifier: DS-1
processes:
  - type: Process
    name: p1
"""
        let ds   = Yaml.Dataset.fromYamlString false yaml
        let proc = ds.Processes.[0]
        Expect.isSome proc.ProcessOf "ProcessOf is set"
        Expect.equal proc.ProcessOf.Value.Identifier "DS-1" "ProcessOf points to parent"

    testCase "PartOf back-edge after decode" <| fun _ ->
        let yaml = """type: Dataset
identifier: parent
hasPart:
  - type: Dataset
    identifier: child
"""
        let ds    = Yaml.Dataset.fromYamlString false yaml
        let child = ds.HasPart.[0]
        Expect.isSome child.PartOf "PartOf is set"
        Expect.equal child.PartOf.Value.Identifier "parent" "PartOf points to parent"

    testCase "back-edges not in output" <| fun _ ->
        let ds   = Dataset("DS-1")
        let yaml = Yaml.Dataset.toYamlString None ds
        Expect.isFalse (yaml.ToLowerInvariant().Contains("partof"))    "no partOf"
        Expect.isFalse (yaml.ToLowerInvariant().Contains("processof")) "no processOf"

    testCase "round-trip minimal" <| fun _ ->
        let original = Dataset("DS-1")
        let yaml     = Yaml.Dataset.toYamlString None original
        let decoded  = Yaml.Dataset.fromYamlString false yaml
        Expect.equal decoded.Identifier original.Identifier "identifier"

    testCase "round-trip with processes" <| fun _ ->
        let original = Dataset("DS-1")
        let proc     = Process("p1")
        proc.AddInput(SampleNode (Sample("Source1")))
        proc.AddOutput(SampleNode (Sample("Sample1")))
        original.AddProcess(proc)
        let yaml    = Yaml.Dataset.toYamlString None original
        let decoded = Yaml.Dataset.fromYamlString false yaml
        Expect.equal decoded.Identifier    original.Identifier    "identifier"
        Expect.equal decoded.Processes.Count 1                    "process count"
        Expect.equal decoded.Processes.[0].Name "p1"              "process name"
        Expect.equal decoded.Processes.[0].Inputs.Count  1        "input count"
        Expect.equal decoded.Processes.[0].Outputs.Count 1        "output count"

    testCase "round-trip nested hasPart" <| fun _ ->
        let childA  = Dataset("child-a")
        let childB  = Dataset("child-b")
        let parent  = Dataset("parent")
        parent.AddPart(childA)
        parent.AddPart(childB)
        let yaml    = Yaml.Dataset.toYamlString None parent
        let decoded = Yaml.Dataset.fromYamlString false yaml
        Expect.equal decoded.Identifier    "parent"    "parent identifier"
        Expect.equal decoded.HasPart.Count 2           "two children"
        Expect.equal decoded.HasPart.[0].Identifier "child-a" "first child"
        Expect.equal decoded.HasPart.[1].Identifier "child-b" "second child"

    testCase "round-trip unified administrative and datamap fields" <| fun _ ->
        let agent = Agent("Ada", familyName = "Lovelace")
        let citation = ScholarlyArticle("Example methods", authors = [ agent ])
        let fragment = Data("results.csv", selector = "col=abundance", selectorFormat = "RFC7111")
        let data = Data("results.csv", hasPart = [ fragment ])
        let dataContext = DataContext(data, explication = DefinedTerm("protein abundance"))
        let original =
            Dataset(
                "DS-unified",
                license = "CC-BY-4.0",
                dateCreated = "2026-06-30",
                agents = [ agent ],
                citations = [ citation ],
                dataContexts = [ dataContext ],
                dataFiles = [ data ])

        let yaml = Yaml.Dataset.toYamlString None original
        let decoded = Yaml.Dataset.fromYamlString false yaml

        Expect.equal decoded.License (Some "CC-BY-4.0") "license should roundtrip"
        Expect.equal decoded.Agents.Count 1 "agent should roundtrip"
        Expect.equal decoded.Citations.Count 1 "citation should roundtrip"
        Expect.equal decoded.DataContexts.Count 1 "data context should roundtrip"
        Expect.equal decoded.DataFiles.Count 1 "data file should roundtrip"
        Expect.equal decoded.DataFiles.[0].HasPart.Count 1 "fragment should roundtrip"

]

