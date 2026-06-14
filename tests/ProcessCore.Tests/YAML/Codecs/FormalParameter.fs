module ProcessCore.Yaml.Tests.Codecs.FormalParameter

open Fable.Pyxpecto
open ProcessCore
open ProcessCore.Yaml

let tests = testList "FormalParameter" [

    testCase "encode name only" <| fun _ ->
        let fp   = FormalParameter("temperature")
        let yaml = Yaml.FormalParameter.toYamlString None fp
        Expect.isTrue (yaml.Contains("name: temperature"))      "name"
        Expect.isTrue (yaml.Contains("type: FormalParameter"))  "type"

    testCase "encode with nameTAN" <| fun _ ->
        let fp   = FormalParameter("temperature", nameTAN = "PATO:0000146")
        let yaml = Yaml.FormalParameter.toYamlString None fp
        Expect.isTrue (yaml.Contains("nameTAN: PATO:0000146")) "nameTAN"

    testCase "encode with defaultValue" <| fun _ ->
        let fp   = FormalParameter("temperature", defaultValue = DefinedTerm("37°C"))
        let yaml = Yaml.FormalParameter.toYamlString None fp
        Expect.isTrue (yaml.Contains("defaultValue")) "defaultValue key present"
        Expect.isTrue (yaml.Contains("37°C"))         "defaultValue name present"

    testCase "decode name only" <| fun _ ->
        let yaml = "type: FormalParameter\nname: rpm\n"
        let fp   = Yaml.FormalParameter.fromYamlString true yaml
        Expect.equal fp.Name       "rpm"  "name"
        Expect.equal fp.NameTAN    None   "no nameTAN"
        Expect.equal fp.DefaultValue None  "no defaultValue"

    testCase "decode with defaultValue as inline object" <| fun _ ->
        let yaml = "type: FormalParameter\nname: temperature\ndefaultValue:\n  type: DefinedTerm\n  name: 37\n"
        let fp   = Yaml.FormalParameter.fromYamlString true yaml
        Expect.isSome fp.DefaultValue "defaultValue is Some"
        Expect.equal fp.DefaultValue.Value.Name "37" "defaultValue name"

    testCase "decode with defaultValue as id-reference" <| fun _ ->
        // id references are left unresolved — DefaultValue becomes None
        let yaml = "type: FormalParameter\nname: temperature\ndefaultValue: some-id-ref\n"
        let fp   = Yaml.FormalParameter.fromYamlString true yaml
        Expect.equal fp.DefaultValue None "id ref leaves DefaultValue as None"

    testCase "round-trip name only" <| fun _ ->
        let original = FormalParameter("enzyme")
        let yaml     = Yaml.FormalParameter.toYamlString None original
        let decoded  = Yaml.FormalParameter.fromYamlString true yaml
        Expect.equal decoded.Name original.Name "name"

    testCase "round-trip all fields" <| fun _ ->
        let original = FormalParameter("temperature", nameTAN = "PATO:0000146", defaultValue = DefinedTerm("37°C"))
        let yaml     = Yaml.FormalParameter.toYamlString None original
        let decoded  = Yaml.FormalParameter.fromYamlString true yaml
        Expect.equal decoded.Name         original.Name         "name"
        Expect.equal decoded.NameTAN      original.NameTAN      "nameTAN"
        Expect.isSome decoded.DefaultValue                      "defaultValue present"
        Expect.equal decoded.DefaultValue.Value.Name "37°C"     "defaultValue name"

]
