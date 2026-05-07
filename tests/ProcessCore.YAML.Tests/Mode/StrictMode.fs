module ProcessCore.Yaml.Tests.Mode.StrictMode

open Fable.Pyxpecto
open ProcessCore
open ProcessCore.Yaml

let tests = testList "StrictMode" [

    testCase "correct type passes — DefinedTerm" <| fun _ ->
        let yaml = "type: DefinedTerm\nname: foo\n"
        // must not throw
        let dt = Yaml.DefinedTerm.fromYamlString yaml
        Expect.equal dt.Name "foo" "name decoded"

    testCase "wrong type on DefinedTerm raises" <| fun _ ->
        let yaml = "type: Material\nname: foo\n"
        Expect.throws (fun () -> Yaml.DefinedTerm.fromYamlString yaml |> ignore)
                      "wrong type raises in strict mode"

    testCase "wrong type on Material raises" <| fun _ ->
        let yaml = "type: Sample\nname: S1\n"
        // 'Sample' is an ISA decoration — strict mode rejects it for Material.decoder
        Expect.throws (fun () -> Yaml.Material.fromYamlString yaml |> ignore)
                      "wrong type raises for Material in strict mode"

    testCase "wrong type on Data raises" <| fun _ ->
        let yaml = "type: File\npath: raw.csv\n"
        // 'File' is a legacy alias handled by LabProcess.decodeIONode, not Data.decoder directly
        Expect.throws (fun () -> Yaml.Data.fromYamlString yaml |> ignore)
                      "wrong type raises for Data in strict mode"

    testCase "wrong type on LabProtocol raises" <| fun _ ->
        let yaml = "type: Protocol\nname: extraction\n"
        Expect.throws (fun () -> Yaml.LabProtocol.fromYamlString yaml |> ignore)
                      "wrong type raises for LabProtocol"

    testCase "wrong type on LabProcess raises" <| fun _ ->
        let yaml = "type: Process\nname: p1\n"
        Expect.throws (fun () -> Yaml.LabProcess.fromYamlString yaml |> ignore)
                      "wrong type raises for LabProcess"

    ptestCase "wrong type on Dataset raises" <| fun _ ->
        let yaml = "type: Investigation\nidentifier: DS-1\n"
        Expect.throws (fun () -> Yaml.Dataset.fromYamlString false yaml |> ignore)
                      "wrong type raises for Dataset"

    testCase "missing type field passes — all types" <| fun _ ->
        // When type is absent, checkType is a no-op → decode should succeed
        Expect.equal (Yaml.DefinedTerm.fromYamlString  "name: foo\n").Name       "foo"  "DefinedTerm no type"
        Expect.equal (Yaml.FormalParameter.fromYamlString "name: rpm\n").Name    "rpm"  "FormalParameter no type"
        Expect.equal (Yaml.PropertyValue.fromYamlString "name: pH\n").Name       "pH"   "PropertyValue no type"
        Expect.equal (Yaml.Material.fromYamlString      "name: S1\n").Name       "S1"   "Material no type"
        Expect.equal (Yaml.Data.fromYamlString          "path: raw.csv\n").Path  "raw.csv" "Data no type"
        Expect.equal (Yaml.LabProtocol.fromYamlString   "name: prot\n").Name     (Some "prot") "LabProtocol no type"
        Expect.equal (Yaml.LabProcess.fromYamlString    "name: p1\n").Name       "p1"   "LabProcess no type"
        Expect.equal (Yaml.Dataset.fromYamlString false "identifier: DS-1\n").Identifier "DS-1" "Dataset no type"

]
