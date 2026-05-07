module ProcessCore.Tests.Types.Material

open Fable.Pyxpecto
open ProcessCore

let tests = testList "Material" [

    testCase "equality by name" <| fun _ ->
        let m1 = Material("Sample1", additionalType = "Sample")
        let m2 = Material("Sample1", additionalType = "Source")
        Expect.equal m1 m2 "Same name → equal regardless of other fields"

    testCase "inequality different name" <| fun _ ->
        let m1 = Material("Sample1")
        let m2 = Material("Sample2")
        Expect.notEqual m1 m2 "Different names → not equal"

    testCase "AddAdditionalProperty deduplicates" <| fun _ ->
        let m  = Material("Sample1")
        let pv = PropertyValue("organism", value = "E. coli")
        m.AddAdditionalProperty(pv)
        m.AddAdditionalProperty(pv)
        Expect.equal m.AdditionalProperty.Count 1 "Identical PV added twice should result in one entry"

    testCase "RemoveAdditionalProperty" <| fun _ ->
        let m  = Material("Sample1")
        let pv = PropertyValue("organism", value = "E. coli")
        m.AddAdditionalProperty(pv)
        Expect.equal m.AdditionalProperty.Count 1 "PV should be present before removal"
        m.RemoveAdditionalProperty(pv)
        Expect.equal m.AdditionalProperty.Count 0 "PV should be removed"

    testCase "RemoveAdditionalProperty no-op for missing" <| fun _ ->
        let m  = Material("Sample1")
        let pv = PropertyValue("organism", value = "E. coli")
        // should not throw
        m.RemoveAdditionalProperty(pv)
        Expect.equal m.AdditionalProperty.Count 0 "Count should still be zero"

    testCase "InputOf and OutputOf start empty" <| fun _ ->
        let m = Material("Sample1")
        Expect.equal m.InputOf.Count  0 "InputOf should start empty"
        Expect.equal m.OutputOf.Count 0 "OutputOf should start empty"

]
