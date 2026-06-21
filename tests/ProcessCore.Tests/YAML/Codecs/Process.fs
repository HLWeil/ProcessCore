module ProcessCore.Yaml.Tests.Codecs.Process

open Fable.Pyxpecto
open ProcessCore
open ProcessCore.Yaml

let tests = testList "Process" [

    testCase "encode name only" <| fun _ ->
        let proc = Process("p1")
        let yaml = Yaml.Process.toYamlString None proc
        Expect.isTrue (yaml.Contains("name: p1"))          "name"
        Expect.isTrue (yaml.Contains("type: Process"))  "type"

    testCase "encode with sample input" <| fun _ ->
        let proc = Process("p1")
        proc.AddInput(SampleNode (Sample("Source1", additionalType = "Source")))
        let yaml = Yaml.Process.toYamlString None proc
        Expect.isTrue (yaml.Contains("inputs"))   "inputs key"
        Expect.isTrue (yaml.Contains("Source1"))  "sample name"

    testCase "encode with data output" <| fun _ ->
        let proc = Process("p1")
        proc.AddOutput(DataNode (Data("results.csv")))
        let yaml = Yaml.Process.toYamlString None proc
        Expect.isTrue (yaml.Contains("outputs"))     "outputs key"
        Expect.isTrue (yaml.Contains("results.csv")) "data path"

    testCase "encode with executesProtocol" <| fun _ ->
        let proto = Plan(name = "extraction")
        let proc  = Process("p1")
        proc.ExecutesProtocol <- Some proto
        let yaml  = Yaml.Process.toYamlString None proc
        Expect.isTrue (yaml.Contains("executesProtocol")) "executesProtocol key"
        Expect.isTrue (yaml.Contains("extraction"))       "protocol name"

    testCase "encode with parameterValues" <| fun _ ->
        let proc = Process("p1")
        proc.AddParameterValue(Annotation("temperature", value = "37", unit = "°C"))
        let yaml = Yaml.Process.toYamlString None proc
        Expect.isTrue (yaml.Contains("parameterValue")) "parameterValue key"
        Expect.isTrue (yaml.Contains("temperature"))    "param name"

    testCase "decode name only" <| fun _ ->
        let yaml = "type: Process\nname: p1\n"
        let proc = Yaml.Process.fromYamlString true yaml
        Expect.equal proc.Name "p1" "name"
        Expect.equal proc.Inputs.Count  0 "no inputs"
        Expect.equal proc.Outputs.Count 0 "no outputs"

    testCase "decode sample input" <| fun _ ->
        let yaml = """type: Process
name: p1
inputs:
  - type: Sample
    name: Source1
    additionalType: Source
"""
        let proc = Yaml.Process.fromYamlString true yaml
        Expect.equal proc.Inputs.Count 1 "one input"
        match proc.Inputs.[0] with
        | SampleNode m -> Expect.equal m.Name "Source1" "sample name"
        | DataNode _     -> failwith "Expected SampleNode"

    testCase "decode data output" <| fun _ ->
        let yaml = """type: Process
name: p1
outputs:
  - type: Data
    path: results.csv
"""
        let proc = Yaml.Process.fromYamlString true yaml
        Expect.equal proc.Outputs.Count 1 "one output"
        match proc.Outputs.[0] with
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
        let proc = Yaml.Process.fromYamlString true yaml
        Expect.equal proc.Outputs.Count 1 "one output"
        match proc.Outputs.[0] with
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
        let proc = Yaml.Process.fromYamlString true yaml
        Expect.equal proc.Inputs.Count  0 "id ref input skipped"
        Expect.equal proc.Outputs.Count 0 "id ref output skipped"

    testCase "decode executesProtocol as inline object" <| fun _ ->
        let yaml = """type: Process
name: p1
executesProtocol:
  type: Plan
  name: extraction
"""
        let proc = Yaml.Process.fromYamlString true yaml
        Expect.isSome proc.ExecutesProtocol "executesProtocol is Some"
        Expect.equal proc.ExecutesProtocol.Value.Name (Some "extraction") "protocol name"

    testCase "decode executesProtocol as id-reference" <| fun _ ->
        let yaml = "type: Process\nname: p1\nexecutesProtocol: some-proto-id\n"
        let proc = Yaml.Process.fromYamlString true yaml
        Expect.equal proc.ExecutesProtocol None "id ref leaves ExecutesProtocol as None"

    testCase "decode parameterValues" <| fun _ ->
        let yaml = """type: Process
name: p1
parameterValue:
  - type: Annotation
    name: temperature
    value: '37'
    unit: "°C"
"""
        let proc = Yaml.Process.fromYamlString true yaml
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
        let decoded  = Yaml.Process.fromYamlString true yaml
        Expect.equal decoded.Name original.Name "name"

    testCase "round-trip with inputs and outputs" <| fun _ ->
        let original = Process("p1")
        original.AddInput(SampleNode (Sample("Source1", additionalType = "Source")))
        original.AddOutput(SampleNode (Sample("Sample1", additionalType = "Sample")))
        let yaml    = Yaml.Process.toYamlString None original
        let decoded = Yaml.Process.fromYamlString true yaml
        Expect.equal decoded.Inputs.Count  1 "inputs count"
        Expect.equal decoded.Outputs.Count 1 "outputs count"
        match decoded.Inputs.[0] with
        | SampleNode m -> Expect.equal m.Name "Source1" "input name"
        | _ -> failwith "Expected SampleNode"

    testCase "round-trip with protocol and parameters" <| fun _ ->
        let proto = Plan(name = "extraction")
        proto.AddParameter(FormalParameter("temperature"))
        let original = Process("p1")
        original.ExecutesProtocol <- Some proto
        original.AddParameterValue(Annotation("temperature", value = "37", unit = "°C"))
        let yaml    = Yaml.Process.toYamlString None original
        let decoded = Yaml.Process.fromYamlString true yaml
        Expect.isSome decoded.ExecutesProtocol              "executesProtocol present"
        Expect.equal decoded.ParameterValue.Count 1         "parameterValue count"
        Expect.equal decoded.ParameterValue.[0].Value (Some "37") "param value"

]
