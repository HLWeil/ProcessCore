module ProcessCore.Yaml.Tests.Codecs.Sample

open Fable.Pyxpecto
open ProcessCore
open ProcessCore.Yaml

let tests = testList "Sample" [

    testCase "encode name only" <| fun _ ->
        let m    = Sample("Sample1")
        let yaml = Yaml.Sample.toYamlString None m
        Expect.isTrue (yaml.Contains("name: Sample1"))    "name"
        Expect.isTrue (yaml.Contains("type: Sample"))   "type"

    testCase "encode with additionalProperty" <| fun _ ->
        let m = Sample("Sample1")
        m.AddAdditionalProperty(Annotation("organism", value = "Arabidopsis thaliana"))
        let yaml = Yaml.Sample.toYamlString None m
        Expect.isTrue (yaml.Contains("additionalProperty")) "additionalProperty key"
        Expect.isTrue (yaml.Contains("organism"))           "organism name"

    testCase "encode with additionalType" <| fun _ ->
        let m    = Sample("Sample1", additionalType = "Sample")
        let yaml = Yaml.Sample.toYamlString None m
        Expect.isTrue (yaml.Contains("additionalType: Sample")) "additionalType"

    testCase "decode name only" <| fun _ ->
        let yaml = "type: Sample\nname: Source1\n"
        let m    = Yaml.Sample.fromYamlString true yaml
        Expect.equal m.Name           "Source1" "name"
        Expect.equal m.AdditionalType None      "no additionalType"
        Expect.equal m.AdditionalProperty.Count 0 "no additionalProperty"

    testCase "decode with additionalProperty" <| fun _ ->
        let yaml = """type: Sample
name: Sample1
additionalProperty:
  - type: Annotation
    name: organism
    value: Arabidopsis thaliana
  - type: Annotation
    name: age
    value: '4'
    unit: week
"""
        let m = Yaml.Sample.fromYamlString true yaml
        Expect.equal m.AdditionalProperty.Count 2 "two properties"
        Expect.equal m.AdditionalProperty.[0].Name "organism" "first prop name"
        Expect.equal m.AdditionalProperty.[1].Name "age"      "second prop name"
        Expect.equal m.AdditionalProperty.[1].Unit (Some "week") "second prop unit"

    testCase "decode with additionalProperty as id-references" <| fun _ ->
        // id references are skipped — no properties added
        let yaml = """type: Sample
name: Sample1
additionalProperty:
  - some-pv-id
"""
        let m = Yaml.Sample.fromYamlString true yaml
        Expect.equal m.AdditionalProperty.Count 0 "id refs skipped"

    testCase "back-edges not in output" <| fun _ ->
        // InputOf / OutputOf are not DynamicObj properties and must not appear in YAML
        let m    = Sample("Sample1")
        let yaml = Yaml.Sample.toYamlString None m
        Expect.isFalse (yaml.ToLowerInvariant().Contains("inputof"))  "no inputOf"
        Expect.isFalse (yaml.ToLowerInvariant().Contains("outputof")) "no outputOf"

    testCase "round-trip name only" <| fun _ ->
        let original = Sample("Source1")
        let yaml     = Yaml.Sample.toYamlString None original
        let decoded  = Yaml.Sample.fromYamlString true yaml
        Expect.equal decoded.Name           original.Name           "name"
        Expect.equal decoded.AdditionalType original.AdditionalType "additionalType"

    testCase "round-trip with additionalProperty" <| fun _ ->
        let original = Sample("Sample1", additionalType = "Sample")
        original.AddAdditionalProperty(Annotation("organism", value = "Arabidopsis thaliana"))
        original.AddAdditionalProperty(Annotation("age", value = "4", unit = "week"))
        let yaml    = Yaml.Sample.toYamlString None original
        let decoded = Yaml.Sample.fromYamlString true yaml
        Expect.equal decoded.Name                        original.Name           "name"
        Expect.equal decoded.AdditionalType              original.AdditionalType "additionalType"
        Expect.equal decoded.AdditionalProperty.Count    2                       "property count"
        Expect.equal decoded.AdditionalProperty.[0].Name "organism"              "first prop"
        Expect.equal decoded.AdditionalProperty.[1].Unit (Some "week")           "second prop unit"

]
