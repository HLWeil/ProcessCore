module ProcessCore.Yaml.Tests.Codecs.PropertyValue

open Fable.Pyxpecto
open ProcessCore
open ProcessCore.Yaml

let tests = testList "PropertyValue" [

    testCase "encode name only" <| fun _ ->
        let pv   = PropertyValue("organism")
        let yaml = Yaml.PropertyValue.toYamlString None pv
        Expect.isTrue (yaml.Contains("name: organism"))       "name"
        Expect.isTrue (yaml.Contains("type: PropertyValue"))  "type"

    testCase "encode all fields" <| fun _ ->
        let pv =
            PropertyValue(
                "Temperature",
                value          = "37",
                unit           = "°C",
                nameTAN        = "PATO:0000146",
                valueTAN       = "http://example.org/37",
                unitTAN        = "UO:0000027",
                additionalType = "Parameter")
        let yaml = Yaml.PropertyValue.toYamlString None pv
        Expect.isTrue (yaml.Contains("value: '37'") || yaml.Contains("value: 37"))   "value"
        Expect.isTrue (yaml.Contains("unit: °C"))                                     "unit"
        Expect.isTrue (yaml.Contains("nameTAN: PATO:0000146"))                        "nameTAN"
        Expect.isTrue (yaml.Contains("valueTAN:"))                                    "valueTAN key"
        Expect.isTrue (yaml.Contains("unitTAN: UO:0000027"))                          "unitTAN"
        Expect.isTrue (yaml.Contains("additionalType: Parameter"))                    "additionalType"

    testCase "encode instanceOf as inline FormalParameter" <| fun _ ->
        let fp   = FormalParameter("temperature", nameTAN = "PATO:0000146")
        let pv   = PropertyValue("temperature", value = "37", instanceOf = fp)
        let yaml = Yaml.PropertyValue.toYamlString None pv
        Expect.isTrue (yaml.Contains("instanceOf")) "instanceOf key"
        Expect.isTrue (yaml.Contains("FormalParameter")) "FormalParameter type inside instanceOf"

    testCase "decode name only" <| fun _ ->
        let yaml = "type: PropertyValue\nname: pH\n"
        let pv   = Yaml.PropertyValue.fromYamlString true yaml
        Expect.equal pv.Name  "pH"  "name"
        Expect.equal pv.Value None  "no value"
        Expect.equal pv.Unit  None  "no unit"

    testCase "decode all fields" <| fun _ ->
        let yaml = """type: PropertyValue
name: Temperature
additionalType: Parameter
value: '37'
unit: "°C"
nameTAN: PATO:0000146
valueTAN: http://example.org/37
unitTAN: UO:0000027
"""
        let pv = Yaml.PropertyValue.fromYamlString true yaml
        Expect.equal pv.Name           "Temperature"               "name"
        Expect.equal pv.AdditionalType (Some "Parameter")          "additionalType"
        Expect.equal pv.Value          (Some "37")                 "value"
        Expect.equal pv.Unit           (Some "°C")                 "unit"
        Expect.equal pv.NameTAN        (Some "PATO:0000146")       "nameTAN"
        Expect.equal pv.UnitTAN        (Some "UO:0000027")         "unitTAN"

    testCase "decode value as string when YAML stores number" <| fun _ ->
        // Bare YAML number should still be decoded as string via decodeString.
        let yaml = "type: PropertyValue\nname: count\nvalue: 42\n"
        let pv   = Yaml.PropertyValue.fromYamlString true yaml
        Expect.equal pv.Value (Some "42") "number decoded as string"

    testCase "decode instanceOf as id-reference" <| fun _ ->
        // id references are left unresolved — InstanceOf becomes None
        let yaml = "type: PropertyValue\nname: temperature\ninstanceOf: some-fp-id\n"
        let pv   = Yaml.PropertyValue.fromYamlString true yaml
        Expect.equal pv.InstanceOf None "id ref leaves InstanceOf as None"

    testCase "round-trip all fields" <| fun _ ->
        let original =
            PropertyValue(
                "Temperature",
                value          = "37",
                unit           = "°C",
                nameTAN        = "PATO:0000146",
                valueTAN       = "http://example.org/37",
                unitTAN        = "UO:0000027",
                additionalType = "Parameter")
        let yaml    = Yaml.PropertyValue.toYamlString None original
        let decoded = Yaml.PropertyValue.fromYamlString true yaml
        Expect.equal decoded.Name           original.Name           "name"
        Expect.equal decoded.Value          original.Value          "value"
        Expect.equal decoded.Unit           original.Unit           "unit"
        Expect.equal decoded.NameTAN        original.NameTAN        "nameTAN"
        Expect.equal decoded.ValueTAN       original.ValueTAN       "valueTAN"
        Expect.equal decoded.UnitTAN        original.UnitTAN        "unitTAN"
        Expect.equal decoded.AdditionalType original.AdditionalType "additionalType"

]
