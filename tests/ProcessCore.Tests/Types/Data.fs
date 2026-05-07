module ProcessCore.Tests.Types.Data

open Fable.Pyxpecto
open ProcessCore

let tests = testList "Data" [

    testCase "equality by path and selector" <| fun _ ->
        let d1 = Data("results.csv", selector = "Sheet1")
        let d2 = Data("results.csv", selector = "Sheet1")
        Expect.equal d1 d2 "Same path + selector → equal"

    testCase "equality selector None vs Some empty string" <| fun _ ->
        let d1 = Data("results.csv")
        let d2 = Data("results.csv", selector = "")
        Expect.notEqual d1 d2 "Selector=None and Selector=Some \"\" should not be equal"

    testCase "inequality different path" <| fun _ ->
        let d1 = Data("results.csv")
        let d2 = Data("output.csv")
        Expect.notEqual d1 d2 "Different path → not equal"

    testCase "AddAdditionalProperty deduplicates" <| fun _ ->
        let d  = Data("results.csv")
        let pv = PropertyValue("format", value = "tabular")
        d.AddAdditionalProperty(pv)
        d.AddAdditionalProperty(pv)
        Expect.equal d.AdditionalProperty.Count 1 "Identical PV added twice should result in one entry"

    testCase "RemoveAdditionalProperty" <| fun _ ->
        let d  = Data("results.csv")
        let pv = PropertyValue("format", value = "tabular")
        d.AddAdditionalProperty(pv)
        Expect.equal d.AdditionalProperty.Count 1 "PV should be present before removal"
        d.RemoveAdditionalProperty(pv)
        Expect.equal d.AdditionalProperty.Count 0 "PV should be removed"

    testCase "InputOf and OutputOf start empty" <| fun _ ->
        let d = Data("results.csv")
        Expect.equal d.InputOf.Count  0 "InputOf should start empty"
        Expect.equal d.OutputOf.Count 0 "OutputOf should start empty"

    testCase "EncodingFormat and AdditionalType fields" <| fun _ ->
        let d = Data("results.csv")
        Expect.isNone d.EncodingFormat "EncodingFormat starts as None"
        Expect.isNone d.AdditionalType "AdditionalType starts as None"
        d.EncodingFormat <- Some "text/csv"
        d.AdditionalType <- Some "Raw Data"
        Expect.equal d.EncodingFormat (Some "text/csv")   "EncodingFormat should reflect setter"
        Expect.equal d.AdditionalType (Some "Raw Data")   "AdditionalType should reflect setter"

]
