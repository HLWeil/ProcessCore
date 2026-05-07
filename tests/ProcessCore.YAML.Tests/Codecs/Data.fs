module ProcessCore.Yaml.Tests.Codecs.Data

open Fable.Pyxpecto
open ProcessCore
open ProcessCore.Yaml

let tests = testList "Data" [

    testCase "encode path only" <| fun _ ->
        let d    = Data("results.csv")
        let yaml = Yaml.Data.toYamlString None d
        Expect.isTrue (yaml.Contains("path: results.csv")) "path"
        Expect.isTrue (yaml.Contains("type: Data"))        "type"

    testCase "encode with selector" <| fun _ ->
        let d    = Data("results.csv", selector = "Sheet1", selectorFormat = "excel")
        let yaml = Yaml.Data.toYamlString None d
        // id is built as "results.csv#Sheet1" — contains '#' so gets quoted
        Expect.isTrue (yaml.Contains("selector: Sheet1"))          "selector"
        Expect.isTrue (yaml.Contains("selectorFormat: excel"))     "selectorFormat"

    testCase "encode all fields" <| fun _ ->
        let d    = Data("raw.csv", selector = "col=1", selectorFormat = "tsv", encodingFormat = "text/csv", additionalType = "RawData")
        let yaml = Yaml.Data.toYamlString None d
        Expect.isTrue (yaml.Contains("path: raw.csv"))              "path"
        Expect.isTrue (yaml.Contains("encodingFormat: text/csv"))   "encodingFormat"
        Expect.isTrue (yaml.Contains("additionalType: RawData"))    "additionalType"

    testCase "decode path only" <| fun _ ->
        let yaml = "type: Data\npath: results.csv\n"
        let d    = Yaml.Data.fromYamlString true yaml
        Expect.equal d.Path           "results.csv" "path"
        Expect.equal d.Selector       None           "no selector"
        Expect.equal d.EncodingFormat None           "no encodingFormat"

    testCase "decode with selector and selectorFormat" <| fun _ ->
        let yaml = "type: Data\npath: results.csv\nselector: Sheet1\nselectorFormat: excel\n"
        let d    = Yaml.Data.fromYamlString true yaml
        Expect.equal d.Selector       (Some "Sheet1") "selector"
        Expect.equal d.SelectorFormat (Some "excel")  "selectorFormat"

    testCase "id field goes to overflow — missing path throws" <| fun _ ->
        // 'id' is not a fallback for path; missing path must throw
        let yaml = "type: Data\nid: results.csv\n"
        Expect.throws (fun () -> Yaml.Data.fromYamlString true yaml |> ignore) "missing path throws"

    testCase "id field goes to overflow — present alongside path" <| fun _ ->
        // When both 'id' and 'path' are present, 'id' goes to overflow, 'path' is used
        let yaml = "type: Data\npath: results.csv\nid: some-override-id\n"
        let d    = Yaml.Data.fromYamlString false yaml
        Expect.equal d.Path "results.csv" "path not overridden by id"
        // 'id' should be stored as overflow
        let overflowId = d.TryGetTypedPropertyValue<string>("id")
        Expect.isSome overflowId "id stored in overflow"
        Expect.equal overflowId (Some "some-override-id") "id overflow value"

    testCase "decode with additionalProperty" <| fun _ ->
        let yaml = """type: Data
path: raw.csv
additionalProperty:
  - type: PropertyValue
    name: instrument
    value: Q Exactive
"""
        let d = Yaml.Data.fromYamlString true yaml
        Expect.equal d.AdditionalProperty.Count 1         "one property"
        Expect.equal d.AdditionalProperty.[0].Name "instrument" "prop name"

    testCase "back-edges not in output" <| fun _ ->
        let d    = Data("raw.csv")
        let yaml = Yaml.Data.toYamlString None d
        Expect.isFalse (yaml.ToLowerInvariant().Contains("inputof"))  "no inputOf"
        Expect.isFalse (yaml.ToLowerInvariant().Contains("outputof")) "no outputOf"

    testCase "round-trip path only" <| fun _ ->
        let original = Data("results.csv")
        let yaml     = Yaml.Data.toYamlString None original
        let decoded  = Yaml.Data.fromYamlString true yaml
        Expect.equal decoded.Path original.Path "path"

    testCase "round-trip all fields" <| fun _ ->
        let original = Data("raw.csv", selector = "Sheet1", selectorFormat = "excel", encodingFormat = "text/csv", additionalType = "RawData")
        original.AddAdditionalProperty(PropertyValue("instrument", value = "Q Exactive"))
        let yaml    = Yaml.Data.toYamlString None original
        let decoded = Yaml.Data.fromYamlString true yaml
        Expect.equal decoded.Path            original.Path            "path"
        Expect.equal decoded.Selector        original.Selector        "selector"
        Expect.equal decoded.SelectorFormat  original.SelectorFormat  "selectorFormat"
        Expect.equal decoded.EncodingFormat  original.EncodingFormat  "encodingFormat"
        Expect.equal decoded.AdditionalType  original.AdditionalType  "additionalType"
        Expect.equal decoded.AdditionalProperty.Count 1               "property count"

]
