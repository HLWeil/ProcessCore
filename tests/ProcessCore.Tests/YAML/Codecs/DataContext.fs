module ProcessCore.Yaml.Tests.Codecs.DataContext

open Fable.Pyxpecto
open ProcessCore
open ProcessCore.Yaml

let tests = testList "DataContext" [

    testCase "roundtrip all fields" <| fun _ ->
        let data = Data("results.csv", selector = "#col=abundance", selectorFormat = "RFC7111", encodingFormat = "text/csv")
        let original =
            DataContext(
                data,
                explication = DefinedTerm("protein abundance", tan = "MS:1003348"),
                objectType = DefinedTerm("Float", tan = "NCIT:C48150"),
                unit = DefinedTerm("intensity", tan = "example:intensity"),
                label = "Abundance",
                description = "Protein abundance column",
                generatedBy = "LC-MS")

        let yaml = Yaml.DataContext.toYamlString None original
        let decoded = Yaml.DataContext.fromYamlString true yaml

        Expect.equal decoded.Data.Path original.Data.Path "data path should roundtrip through nested data"
        Expect.equal decoded.Data.Selector original.Data.Selector "data selector should roundtrip through nested data"
        Expect.equal decoded.Data.SelectorFormat original.Data.SelectorFormat "data selectorFormat should roundtrip through nested data"
        Expect.equal decoded.Data.EncodingFormat original.Data.EncodingFormat "data encodingFormat should roundtrip through nested data"
        Expect.equal decoded.Explication.Value.Name "protein abundance" "explication should roundtrip"
        Expect.equal decoded.Explication.Value.TAN (Some "MS:1003348") "explication TAN should roundtrip"
        Expect.equal decoded.ObjectType.Value.Name "Float" "objectType should roundtrip"
        Expect.equal decoded.Unit.Value.Name "intensity" "unit should roundtrip"
        Expect.equal decoded.Label original.Label "label should roundtrip"
        Expect.equal decoded.GeneratedBy original.GeneratedBy "generatedBy should roundtrip"

    testCase "roundtrip nested data target" <| fun _ ->
        let original =
            DataContext(
                Data("results.csv", selector = "#col=abundance", selectorFormat = "RFC7111", encodingFormat = "text/csv"),
                explication = DefinedTerm("protein abundance"))

        let yaml = Yaml.DataContext.toYamlString None original
        let decoded = Yaml.DataContext.fromYamlString true yaml

        Expect.equal decoded.Data.Path "results.csv" "nested data should roundtrip"
        Expect.equal decoded.Data.Selector (Some "#col=abundance") "nested data selector should roundtrip"

    testCase "strict mode rejects flat data target fields" <| fun _ ->
        let yaml = """type: DataContext
path: results.csv
selector: "#col=abundance"
explication: protein abundance
"""
        Expect.throws (fun () -> Yaml.DataContext.fromYamlString true yaml |> ignore) "flat data target fields should not be accepted"
]
