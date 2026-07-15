module ProcessCore.Yaml.Tests.Codecs.Process

open Fable.Pyxpecto
open ProcessCore
open ProcessCore.Yaml

let decodeOne processCoreOnly yaml =
    Yaml.Process.fromYamlString processCoreOnly yaml |> Seq.exactlyOne

let tests = testList "Process" [

    testCase "encode name only" <| fun _ ->
        let proc = Process("p1")
        let yaml = Yaml.Process.toYamlString None proc
        Expect.isTrue (yaml.Contains("name: p1"))          "name"
        Expect.isTrue (yaml.Contains("type: Process"))  "type"

    testCase "encode with sample input" <| fun _ ->
        let proc = Process("p1")
        proc.SetInput(SampleNode (Sample("Source1", additionalType = "Source")))
        let yaml = Yaml.Process.toYamlString None proc
        Expect.isTrue (yaml.Contains("inputs"))   "inputs key"
        Expect.isTrue (yaml.Contains("Source1"))  "sample name"

    testCase "encode with data output" <| fun _ ->
        let proc = Process("p1")
        proc.SetOutput(DataNode (Data("results.csv")))
        let yaml = Yaml.Process.toYamlString None proc
        Expect.isTrue (yaml.Contains("outputs"))     "outputs key"
        Expect.isTrue (yaml.Contains("results.csv")) "data path"

    testCase "encode with executesRecipe" <| fun _ ->
        let proto = Recipe(name = "extraction")
        let proc  = Process("p1")
        proc.ExecutesRecipe <- Some proto
        let yaml  = Yaml.Process.toYamlString None proc
        Expect.isTrue (yaml.Contains("executesRecipe")) "executesRecipe key"
        Expect.isTrue (yaml.Contains("extraction"))       "recipe name"

    testCase "encode with parameterValues" <| fun _ ->
        let proc = Process("p1")
        proc.AddParameterValue(Annotation("temperature", value = "37", unit = "°C"))
        let yaml = Yaml.Process.toYamlString None proc
        Expect.isTrue (yaml.Contains("parameterValue")) "parameterValue key"
        Expect.isTrue (yaml.Contains("temperature"))    "param name"

    testCase "decode name only" <| fun _ ->
        let yaml = "type: Process\nname: p1\n"
        let proc = decodeOne true yaml
        Expect.equal proc.Name "p1" "name"
        Expect.isNone proc.Input "no input"
        Expect.isNone proc.Output "no output"

    testCase "decode sample input" <| fun _ ->
        let yaml = """type: Process
name: p1
inputs:
  - type: Sample
    name: Source1
    additionalType: Source
"""
        let proc = decodeOne true yaml
        Expect.isSome proc.Input "one input"
        match proc.Input.Value with
        | SampleNode m -> Expect.equal m.Name "Source1" "sample name"
        | DataNode _     -> failwith "Expected SampleNode"

    testCase "decode data output" <| fun _ ->
        let yaml = """type: Process
name: p1
outputs:
  - type: Data
    path: results.csv
"""
        let proc = decodeOne true yaml
        Expect.isSome proc.Output "one output"
        match proc.Output.Value with
        | DataNode d     -> Expect.equal d.Path "results.csv" "data path"
        | SampleNode _ -> failwith "Expected DataNode"

    testCase "decode data by File legacy type alias" <| fun _ ->
        // 'type: File' is a legacy alias for 'type: Data'
        let yaml = """type: Process
name: p1
outputs:
  - type: File
    path: results.csv
"""
        let proc = decodeOne true yaml
        Expect.isSome proc.Output "one output"
        match proc.Output.Value with
        | DataNode d     -> Expect.equal d.Path "results.csv" "data path via File alias"
        | SampleNode _ -> failwith "Expected DataNode"

    testCase "decode io as id-references" <| fun _ ->
        // id references (plain strings) are skipped
        let yaml = """type: Process
name: p1
inputs:
  - some-sample-id
outputs:
  - some-data-id
"""
        let proc = decodeOne true yaml
        Expect.isNone proc.Input "id ref input skipped"
        Expect.isNone proc.Output "id ref output skipped"

    testCase "decode executesRecipe as inline object" <| fun _ ->
        let yaml = """type: Process
name: p1
executesRecipe:
  type: Recipe
  name: extraction
"""
        let proc = decodeOne true yaml
        Expect.isSome proc.ExecutesRecipe "executesRecipe is Some"
        Expect.equal proc.ExecutesRecipe.Value.Name (Some "extraction") "recipe name"

    testCase "decode executesRecipe as id-reference" <| fun _ ->
        let yaml = "type: Process\nname: p1\nexecutesRecipe: some-proto-id\n"
        let proc = decodeOne true yaml
        Expect.equal proc.ExecutesRecipe None "id ref leaves ExecutesRecipe as None"

    testCase "decode parameterValues" <| fun _ ->
        let yaml = """type: Process
name: p1
parameterValue:
  - type: Annotation
    name: temperature
    value: '37'
    unit: "°C"
"""
        let proc = decodeOne true yaml
        Expect.equal proc.ParameterValue.Count 1               "one parameter value"
        Expect.equal proc.ParameterValue.[0].Name "temperature" "param name"
        Expect.equal proc.ParameterValue.[0].Unit (Some "°C")   "param unit"

    testCase "back-edges not in output" <| fun _ ->
        let proc = Process("p1")
        let yaml = Yaml.Process.toYamlString None proc
        Expect.isFalse (yaml.ToLowerInvariant().Contains("processof")) "no processOf"

    testCase "round-trip name only" <| fun _ ->
        let original = Process("p1")
        let yaml     = Yaml.Process.toYamlString None original
        let decoded  = decodeOne true yaml
        Expect.equal decoded.Name original.Name "name"

    testCase "round-trip with inputs and outputs" <| fun _ ->
        let original = Process("p1")
        original.SetInput(SampleNode (Sample("Source1", additionalType = "Source")))
        original.SetOutput(SampleNode (Sample("Sample1", additionalType = "Sample")))
        let yaml    = Yaml.Process.toYamlString None original
        let decoded = decodeOne true yaml
        Expect.isSome decoded.Input "input present"
        Expect.isSome decoded.Output "output present"
        match decoded.Input.Value with
        | SampleNode m -> Expect.equal m.Name "Source1" "input name"
        | _ -> failwith "Expected SampleNode"

    testCase "round-trip with recipe and parameters" <| fun _ ->
        let proto = Recipe(name = "extraction")
        proto.AddParameter(FormalParameter("temperature"))
        let original = Process("p1")
        original.ExecutesRecipe <- Some proto
        original.AddParameterValue(Annotation("temperature", value = "37", unit = "°C"))
        let yaml    = Yaml.Process.toYamlString None original
        let decoded = decodeOne true yaml
        Expect.isSome decoded.ExecutesRecipe              "executesRecipe present"
        Expect.equal decoded.ParameterValue.Count 1         "parameterValue count"
        Expect.equal decoded.ParameterValue.[0].Value (Some "37") "param value"

    testCase "decode collapsed YAML into singular process edges" <| fun _ ->
        let yaml = """type: Process
name: paired
inputs:
  - { type: Sample, name: Input1 }
  - { type: Sample, name: Input2 }
outputs:
  - { type: Sample, name: Output1 }
  - { type: Sample, name: Output2 }
"""
        let decoded = Yaml.Process.fromYamlString true yaml
        Expect.equal decoded.Count 2 "one process per positional pair"
        Expect.equal (decoded.[0].Input.Value.Key()) "M:Input1" "first input"
        Expect.equal (decoded.[0].Output.Value.Key()) "M:Output1" "first output"
        Expect.equal (decoded.[1].Input.Value.Key()) "M:Input2" "second input"
        Expect.equal (decoded.[1].Output.Value.Key()) "M:Output2" "second output"

    testCase "decode unequal YAML arrays with optional padding" <| fun _ ->
        let yaml = """type: Process
name: padded
inputs:
  - { type: Sample, name: Input1 }
outputs:
  - { type: Sample, name: Output1 }
  - { type: Sample, name: Output2 }
"""
        let decoded = Yaml.Process.fromYamlString true yaml
        Expect.equal decoded.Count 2 "longer side determines process count"
        Expect.isSome decoded.[0].Input "first edge has an input"
        Expect.isNone decoded.[1].Input "second edge is output-only"
        Expect.isSome decoded.[1].Output "second output is retained"

]
