module ProcessCore.Yaml.Tests.ScalarStyle

open Fable.Pyxpecto
open ProcessCore.Yaml
open YAMLicious

// Round-trip a single string value through the style-less constructors so that the
// YAMLicious writer performs the automatic plain / double-quoted / block selection.
let private roundTripValue (value: string) =
    let ast = Helpers.yamlMap [ "k", Helpers.yamlValue value ]
    let yaml = Helpers.writeYaml (Some 2) ast
    let decoded = Reader.read yaml |> Helpers.tryGetField "k" |> Option.map Helpers.decodeString
    yaml, decoded

let tests = testList "ScalarStyle" [

    testCase "structurally unsafe values auto-resolve to double-quoted and round-trip" <| fun _ ->
        let cases =
            [ "cell#growth"; "cell: growth"; "*star"; "@ref"; "- dash"; "note # x"; ""; " padded"; "trailing " ]
        for value in cases do
            let yaml, decoded = roundTripValue value
            Expect.isTrue (yaml.Contains("k: \"")) $"'{value}' should be emitted double-quoted (got: {yaml})"
            Expect.equal decoded (Some value) $"'{value}' should round-trip exactly"

    testCase "plain-safe values stay unquoted and round-trip" <| fun _ ->
        let cases =
            [ "cell growth"; "GO:0016049"; "http://purl.obolibrary.org/obo/go.owl"; "it's fine"; "-5" ]
        for value in cases do
            let yaml, decoded = roundTripValue value
            Expect.isFalse (yaml.Contains("\"")) $"'{value}' should stay unquoted (got: {yaml})"
            Expect.equal decoded (Some value) $"'{value}' should round-trip exactly"

    testCase "multiline values become a block scalar rather than a quoted inline scalar" <| fun _ ->
        let yaml, decoded = roundTripValue "line1\nline2"
        Expect.isTrue (yaml.Contains("k: |")) $"multiline value should use a block scalar (got: {yaml})"
        Expect.isFalse (yaml.Contains("\\n")) "multiline value should not be collapsed into an escaped inline scalar"
        Expect.equal decoded (Some "line1\nline2") "multiline value should round-trip"

    testCase "map keys are auto-quoted when structurally unsafe" <| fun _ ->
        let yaml = Helpers.yamlMap [ "@id", Helpers.yamlValue "x" ] |> Helpers.writeYaml (Some 2)
        Expect.isTrue (yaml.Contains("\"@id\":")) $"unsafe key should be double-quoted (got: {yaml})"
        let decoded = Reader.read yaml |> Helpers.tryGetField "@id" |> Option.bind Helpers.tryDecodeString
        Expect.equal decoded (Some "x") "quoted key should decode back to @id with its value"

    testCase "quoting is idempotent across repeated read/write" <| fun _ ->
        let ast = Helpers.yamlMap [ "k", Helpers.yamlValue "cell: growth" ]
        let written1 = Helpers.writeYaml (Some 2) ast
        let written2 = Reader.read written1 |> Helpers.writeYaml (Some 2)
        Expect.equal written2 written1 "second write should equal the first"
]
