module ProcessCore.Tests.Types.PropertyValue

open Fable.Pyxpecto
open ProcessCore

let tests = testList "PropertyValue" [

    testCase "construction with name only" <| fun _ ->
        let pv = PropertyValue("Temperature")
        Expect.equal pv.Name "Temperature" "Name should be set"
        Expect.isNone pv.Value         "Value should be None"
        Expect.isNone pv.Unit          "Unit should be None"
        Expect.isNone pv.NameTAN       "NameTAN should be None"
        Expect.isNone pv.ValueTAN      "ValueTAN should be None"
        Expect.isNone pv.UnitTAN       "UnitTAN should be None"
        Expect.isNone pv.AdditionalType "AdditionalType should be None"
        Expect.isNone pv.InstanceOf    "InstanceOf should be None"

    testCase "construction with all fields" <| fun _ ->
        let fp = FormalParameter("temperature")
        let pv = PropertyValue(
                    "temperature",
                    value          = "37",
                    unit           = "°C",
                    nameTAN        = "http://example.org/temp",
                    valueTAN       = "http://example.org/37",
                    unitTAN        = "http://example.org/celsius",
                    additionalType = "ParameterValue",
                    instanceOf     = fp)
        Expect.equal pv.Value          (Some "37")                          "Value"
        Expect.equal pv.Unit           (Some "°C")                          "Unit"
        Expect.equal pv.NameTAN        (Some "http://example.org/temp")     "NameTAN"
        Expect.equal pv.ValueTAN       (Some "http://example.org/37")       "ValueTAN"
        Expect.equal pv.UnitTAN        (Some "http://example.org/celsius")  "UnitTAN"
        Expect.equal pv.AdditionalType (Some "ParameterValue")              "AdditionalType"
        Expect.isSome pv.InstanceOf                                         "InstanceOf should be Some"
        Expect.equal pv.InstanceOf.Value fp                                 "InstanceOf value"

    testCase "equality same values" <| fun _ ->
        let pv1 = PropertyValue("temp", value = "37", unit = "°C", nameTAN = "http://example.org/t")
        let pv2 = PropertyValue("temp", value = "37", unit = "°C", nameTAN = "http://example.org/t")
        Expect.equal pv1 pv2 "Same name/value/unit/nameTAN → equal"

    testCase "equality ignores other fields" <| fun _ ->
        let pv1 = PropertyValue("temp", value = "37", valueTAN = "http://v1", unitTAN = "http://u1", additionalType = "ParameterValue")
        let pv2 = PropertyValue("temp", value = "37", valueTAN = "http://v2", unitTAN = "http://u2", additionalType = "FactorValue")
        Expect.equal pv1 pv2 "ValueTAN/UnitTAN/AdditionalType differences should not affect equality"

    testCase "inequality different name" <| fun _ ->
        let pv1 = PropertyValue("temperature", value = "37")
        let pv2 = PropertyValue("rpm",         value = "37")
        Expect.notEqual pv1 pv2 "Different names → not equal"

    testCase "inequality different value" <| fun _ ->
        let pv1 = PropertyValue("temp", value = "37")
        let pv2 = PropertyValue("temp", value = "42")
        Expect.notEqual pv1 pv2 "Different values → not equal"

    testCase "inequality different unit" <| fun _ ->
        let pv1 = PropertyValue("temp", value = "37", unit = "°C")
        let pv2 = PropertyValue("temp", value = "37", unit = "K")
        Expect.notEqual pv1 pv2 "Different units → not equal"

    testCase "inequality different nameTAN" <| fun _ ->
        let pv1 = PropertyValue("temp", nameTAN = "http://example.org/a")
        let pv2 = PropertyValue("temp", nameTAN = "http://example.org/b")
        Expect.notEqual pv1 pv2 "Different NameTAN → not equal"

    testCase "hash consistency" <| fun _ ->
        let pv1 = PropertyValue("temp", value = "37", unit = "°C", nameTAN = "http://example.org/t")
        let pv2 = PropertyValue("temp", value = "37", unit = "°C", nameTAN = "http://example.org/t")
        Expect.equal (pv1.GetHashCode()) (pv2.GetHashCode()) "Equal objects must have equal hash codes"

    testCase "mutation" <| fun _ ->
        let pv = PropertyValue("enzyme")
        Expect.isNone pv.Value "Value starts as None"
        pv.Value <- Some "Trypsin"
        Expect.equal pv.Value (Some "Trypsin") "Value should reflect mutation"

]
