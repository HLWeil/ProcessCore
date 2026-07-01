module ProcessCore.Yaml.Tests.Codecs.Recipe

open Fable.Pyxpecto
open ProcessCore
open ProcessCore.Yaml

let tests = testList "Recipe" [

    testCase "encode minimal" <| fun _ ->
        let proto = Recipe()
        let yaml  = Yaml.Recipe.toYamlString None proto
        Expect.isTrue (yaml.Contains("type: Recipe")) "type"

    testCase "encode with name and url" <| fun _ ->
        let proto = Recipe(name = "extraction", url = "https://protocols.io/v1")
        let yaml  = Yaml.Recipe.toYamlString None proto
        Expect.isTrue (yaml.Contains("name: extraction"))              "name"
        Expect.isTrue (yaml.Contains("url: https://protocols.io/v1")) "url"

    testCase "encode with parameters sequence" <| fun _ ->
        let proto = Recipe(name = "extraction")
        proto.AddParameter(FormalParameter("temperature"))
        proto.AddParameter(FormalParameter("rpm"))
        let yaml = Yaml.Recipe.toYamlString None proto
        Expect.isTrue (yaml.Contains("parameters")) "parameters key"
        Expect.isTrue (yaml.Contains("temperature")) "temperature"
        Expect.isTrue (yaml.Contains("rpm"))         "rpm"

    testCase "encode with component sequence" <| fun _ ->
        let proto = Recipe(name = "centrifugation")
        proto.AddComponent(Annotation("centrifuge", value = "Eppendorf 5420"))
        let yaml = Yaml.Recipe.toYamlString None proto
        Expect.isTrue (yaml.Contains("components")) "component key"
        Expect.isTrue (yaml.Contains("centrifuge"))   "equipment name"

    testCase "encode with additionalProperty sequence" <| fun _ ->
        let proto = Recipe(name = "extraction")
        proto.AddAdditionalProperty(Annotation("notes", value = "Keep on ice"))
        let yaml = Yaml.Recipe.toYamlString None proto
        Expect.isTrue (yaml.Contains("additionalProperty")) "additionalProperty key"
        Expect.isTrue (yaml.Contains("notes"))              "notes name"

    testCase "encode with intendedUse" <| fun _ ->
        let proto = Recipe(name = "extraction")
        proto.IntendedUse <- Some (DefinedTerm("cell growth", tan = "GO:0016049"))
        let yaml = Yaml.Recipe.toYamlString None proto
        Expect.isTrue (yaml.Contains("intendedUse")) "intendedUse key"
        Expect.isTrue (yaml.Contains("cell growth")) "intendedUse name"

    testCase "decode minimal" <| fun _ ->
        let yaml  = "type: Recipe\n"
        let proto = Yaml.Recipe.fromYamlString true yaml
        Expect.equal proto.Name        None "no name"
        Expect.equal proto.Description None "no description"
        Expect.equal proto.Parameters.Count 0 "no parameters"

    testCase "decode all fields" <| fun _ ->
        let yaml = """type: Recipe
name: extraction
description: Standard protein extraction
version: '1.0'
url: https://protocols.io/v1
intendedUse:
  type: DefinedTerm
  name: cell growth
  TAN: GO:0016049
parameters:
  - type: FormalParameter
    name: temperature
"""
        let proto = Yaml.Recipe.fromYamlString true yaml
        Expect.equal proto.Name        (Some "extraction")                  "name"
        Expect.equal proto.Description (Some "Standard protein extraction") "description"
        Expect.equal proto.Version     (Some "1.0")                         "version"
        Expect.equal proto.Url         (Some "https://protocols.io/v1")     "url"
        Expect.isSome proto.IntendedUse                                     "intendedUse"
        Expect.equal proto.IntendedUse.Value.Name "cell growth"             "intendedUse name"
        Expect.equal proto.Parameters.Count 1                               "one parameter"
        Expect.equal proto.Parameters.[0].Name "temperature"                "parameter name"

    testCase "decode parameters as id-references" <| fun _ ->
        // id references are skipped
        let yaml = """type: Recipe
name: extraction
parameters:
  - some-fp-id
"""
        let proto = Yaml.Recipe.fromYamlString true yaml
        Expect.equal proto.Parameters.Count 0 "id refs skipped"

    testCase "decode intendedUse as id-reference" <| fun _ ->
        let yaml = "type: Recipe\nname: extraction\nintendedUse: some-dt-id\n"
        let proto = Yaml.Recipe.fromYamlString true yaml
        Expect.equal proto.IntendedUse None "id ref leaves IntendedUse as None"

    testCase "round-trip minimal" <| fun _ ->
        let original = Recipe(name = "extraction")
        let yaml     = Yaml.Recipe.toYamlString None original
        let decoded  = Yaml.Recipe.fromYamlString true yaml
        Expect.equal decoded.Name original.Name "name"

    testCase "round-trip all fields" <| fun _ ->
        let original = Recipe(name = "extraction", description = "desc", version = "1.0", url = "https://protocols.io/v1")
        original.IntendedUse <- Some (DefinedTerm("cell growth", tan = "GO:0016049"))
        original.AddParameter(FormalParameter("temperature", nameTAN = "PATO:0000146"))
        original.AddComponent(Annotation("centrifuge", value = "Eppendorf"))
        original.AddAdditionalProperty(Annotation("notes", value = "On ice"))
        let yaml    = Yaml.Recipe.toYamlString None original
        let decoded = Yaml.Recipe.fromYamlString true yaml
        Expect.equal decoded.Name        original.Name        "name"
        Expect.equal decoded.Description original.Description "description"
        Expect.equal decoded.Version     original.Version     "version"
        Expect.equal decoded.Url         original.Url         "url"
        Expect.isSome decoded.IntendedUse                     "intendedUse present"
        Expect.equal decoded.Parameters.Count  1              "parameters count"
        Expect.equal decoded.Components.Count 1             "component count"
        Expect.equal decoded.AdditionalProperty.Count 1       "additionalProperty count"

]
