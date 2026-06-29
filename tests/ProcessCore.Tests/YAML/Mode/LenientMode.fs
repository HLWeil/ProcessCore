module ProcessCore.Yaml.Tests.Mode.LenientMode

open Fable.Pyxpecto
open ProcessCore
open ProcessCore.Yaml

// Lenient mode: processCoreOnly = false  → checkType is skipped entirely.
// Used when the YAML carries ISA decoration types instead of ProcessCore type names.

let tests = testList "LenientMode" [

    testCase "decorated type on DefinedTerm accepted" <| fun _ ->
        // ISA decoration uses "OntologyAnnotation" as the type string
        let yaml = "type: OntologyAnnotation\nname: cell growth\nTAN: GO:0016049\n"
        let dt = Yaml.DefinedTerm.decoder false (YAMLicious.Reader.read yaml)
        Expect.equal dt.Name "cell growth"    "name decoded"
        Expect.equal dt.TAN  (Some "GO:0016049") "TAN decoded"

    testCase "decorated type on Sample accepted" <| fun _ ->
        // ISA: type field might read "Sample" rather than "Sample"
        let yaml = "type: Sample\nname: S1\nadditionalType: Sample\n"
        let m = Yaml.Sample.decoder false (YAMLicious.Reader.read yaml)
        Expect.equal m.Name "S1" "name decoded"
        Expect.equal m.AdditionalType (Some "Sample") "additionalType decoded"

    testCase "decorated type on Data accepted" <| fun _ ->
        let yaml = "type: DataFile\npath: raw.csv\nencodingFormat: text/csv\n"
        let d = Yaml.Data.decoder false (YAMLicious.Reader.read yaml)
        Expect.equal d.Path "raw.csv" "path decoded"
        Expect.equal d.EncodingFormat (Some "text/csv") "encodingFormat decoded"

    testCase "decorated type on Recipe accepted" <| fun _ ->
        let yaml = "type: Recipe\nname: extraction\ndescription: desc\n"
        // Even strict would pass here, but lenient definitely passes
        let proto = Yaml.Recipe.decoder false (YAMLicious.Reader.read yaml)
        Expect.equal proto.Name        (Some "extraction") "name decoded"
        Expect.equal proto.Description (Some "desc")       "description decoded"

    testCase "decorated type on Process accepted" <| fun _ ->
        let yaml = "type: Assay\nname: p1\n"
        // "Assay" is not a ProcessCore type; lenient mode ignores it
        let proc = Yaml.Process.decoder false (YAMLicious.Reader.read yaml)
        Expect.equal proc.Name "p1" "name decoded despite decorated type"

    testCase "decorated type on Dataset accepted" <| fun _ ->
        let yaml = "type: Investigation\nidentifier: DS-1\ntitle: My Investigation\n"
        let ds = Yaml.Dataset.decoder false (YAMLicious.Reader.read yaml)
        Expect.equal ds.Identifier "DS-1"               "identifier decoded"
        Expect.equal ds.Title      (Some "My Investigation") "title decoded"

    testCase "completely absent type accepted" <| fun _ ->
        let yaml = "identifier: DS-no-type\n"
        let ds = Yaml.Dataset.decoder false (YAMLicious.Reader.read yaml)
        Expect.equal ds.Identifier "DS-no-type" "identifier decoded without type field"

    testCase "unknown arbitrary type accepted" <| fun _ ->
        let yaml = "type: SomeFutureExtension\nname: foo\n"
        let dt = Yaml.DefinedTerm.decoder false (YAMLicious.Reader.read yaml)
        Expect.equal dt.Name "foo" "name decoded with unknown type"

    testCase "field values still decoded" <| fun _ ->
        // Lenient mode still decodes all the field values normally
        let yaml = """type: Assay
identifier: assay-1
title: My Assay
additionalType: Assay
processes:
  - type: Growth
    name: grow1
    inputs:
      - type: Source
        name: BaseSource
    outputs:
      - type: Sample
        name: Product1
"""
        let ds = Yaml.Dataset.decoder false (YAMLicious.Reader.read yaml)
        Expect.equal ds.Identifier    "assay-1"      "identifier decoded"
        Expect.equal ds.Title         (Some "My Assay") "title decoded"
        Expect.equal ds.AdditionalType (Some "Assay") "additionalType decoded"
        Expect.equal ds.Processes.Count 1             "one process decoded"
        let proc = ds.Processes.[0]
        Expect.equal proc.Name "grow1"                "process name decoded"
        Expect.equal proc.Inputs.Count  1             "one input"
        Expect.equal proc.Outputs.Count 1             "one output"
        match proc.Inputs.[0] with
        | SampleNode m -> Expect.equal m.Name "BaseSource" "input name decoded"
        | DataNode _     -> failwith "Expected SampleNode (lenient default)"
        match proc.Outputs.[0] with
        | SampleNode m -> Expect.equal m.Name "Product1" "output name decoded"
        | DataNode _     -> failwith "Expected SampleNode (lenient default)"

]
