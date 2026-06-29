module ProcessCore.Yaml.Tests.Mode.StrictMode

open Fable.Pyxpecto
open ProcessCore
open ProcessCore.Yaml

let tests = testList "StrictMode" [

    testCase "correct type passes — DefinedTerm" <| fun _ ->
        let yaml = "type: DefinedTerm\nname: foo\n"
        // must not throw
        let dt = Yaml.DefinedTerm.fromYamlString true yaml
        Expect.equal dt.Name "foo" "name decoded"

    testCase "wrong type on DefinedTerm raises" <| fun _ ->
        let yaml = "type: Sample\nname: foo\n"
        Expect.throws (fun () -> Yaml.DefinedTerm.fromYamlString true yaml |> ignore)
                      "wrong type raises in strict mode"

    testCase "wrong type on Sample raises" <| fun _ ->
        let yaml = "type: Data\nname: S1\n"
        // 'Sample' is an ISA decoration — strict mode rejects it for Sample.decoder
        Expect.throws (fun () -> Yaml.Sample.fromYamlString true yaml |> ignore)
                      "wrong type raises for Sample in strict mode"

    testCase "wrong type on Data raises" <| fun _ ->
        let yaml = "type: File\npath: raw.csv\n"
        // 'File' is a legacy alias handled by Process.decodeIONode, not Data.decoder directly
        Expect.throws (fun () -> Yaml.Data.fromYamlString true yaml |> ignore)
                      "wrong type raises for Data in strict mode"

    testCase "wrong type on Recipe raises" <| fun _ ->
        let yaml = "type: Protocol\nname: extraction\n"
        Expect.throws (fun () -> Yaml.Recipe.fromYamlString true yaml |> ignore)
                      "wrong type raises for Recipe"

    testCase "wrong type on Process raises" <| fun _ ->
        let yaml = "type: Recipe\nname: p1\n"
        Expect.throws (fun () -> Yaml.Process.fromYamlString true yaml |> ignore)
                      "wrong type raises for Process"

    testCase "wrong type on Dataset raises" <| fun _ ->
        let yaml = "type: Investigation\nidentifier: DS-1\n"
        Expect.throws (fun () -> Yaml.Dataset.fromYamlString true yaml |> ignore)
                      "wrong type raises for Dataset"

    testCase "missing type field passes — all types" <| fun _ ->
        // When type is absent, checkType is a no-op → decode should succeed
        Expect.equal (Yaml.DefinedTerm.fromYamlString true  "name: foo\n").Name       "foo"  "DefinedTerm no type"
        Expect.equal (Yaml.FormalParameter.fromYamlString true "name: rpm\n").Name    "rpm"  "FormalParameter no type"
        Expect.equal (Yaml.Annotation.fromYamlString true "name: pH\n").Name       "pH"   "Annotation no type"
        Expect.equal (Yaml.Sample.fromYamlString      true "name: S1\n").Name       "S1"   "Sample no type"
        Expect.equal (Yaml.Data.fromYamlString          true "path: raw.csv\n").Path  "raw.csv" "Data no type"
        Expect.equal (Yaml.Recipe.fromYamlString   true "name: prot\n").Name     (Some "prot") "Recipe no type"
        Expect.equal (Yaml.Process.fromYamlString    true "name: p1\n").Name       "p1"   "Process no type"
        Expect.equal (Yaml.Dataset.fromYamlString false "identifier: DS-1\n").Identifier "DS-1" "Dataset no type"

]
