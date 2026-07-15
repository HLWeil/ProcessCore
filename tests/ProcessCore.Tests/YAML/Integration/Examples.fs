module ProcessCore.Yaml.Tests.Integration.Examples

open Fable.Pyxpecto
open ProcessCore
open ProcessCore.Yaml

// let private examplesDir =
//     System.IO.Path.GetFullPath(
//         System.IO.Path.Combine(
//             System.AppDomain.CurrentDomain.BaseDirectory,
//             "../../../../../examples/isa"))

// let private readExample name =
//     System.IO.File.ReadAllText(System.IO.Path.Combine(examplesDir, name))

// The example files use ProcessCore type strings (Dataset, Process, Sample, Data)
// with ISA additionalType decorations. Strict mode (processCoreOnly=true) still
// rejects non-core ISA fields, while shared administrative fields decode normally.

let private loadInvestigation (processCoreOnly : bool) =
    // Yaml.Dataset.fromYamlString (readExample "investigation.yml")
    Yaml.Dataset.fromYamlString processCoreOnly ProcessCore.Yaml.Tests.Fixtures.investigationString


let private loadAssay (processCoreOnly : bool) =
    // Yaml.Dataset.fromYamlString (readExample "assay_proteomics.yml")
    Yaml.Dataset.fromYamlString processCoreOnly ProcessCore.Yaml.Tests.Fixtures.proteomicsAssayString

let tests = testList "Examples" [

    testCase "investigation identifier" <| fun _ ->
        let inv = loadInvestigation(false)
        Expect.equal inv.Identifier "ara_prot_2023" "identifier"

    testCase "investigation name" <| fun _ ->
        let inv = loadInvestigation(false)
        Expect.equal inv.Title
                     (Some "Validation of Proteins in Arabidopsis thaliana")
                     "name"

    testCase "investigation additionalType" <| fun _ ->
        let inv = loadInvestigation(false)
        Expect.equal inv.AdditionalType (Some "Investigation") "additionalType"

    testCase "investigation additionalProperty count" <| fun _ ->
        let inv = loadInvestigation(false)
        Expect.equal inv.AdditionalProperty.Count 3 "three additionalProperties"

    testCase "investigation PV names" <| fun _ ->
        let inv  = loadInvestigation(false)
        let names = inv.AdditionalProperty |> Seq.map (fun pv -> pv.Name) |> Seq.toList
        Expect.equal names ["latitude"; "longitude"; "aim"] "property names"


    testCase "investigation lenient mode passes" <| fun _ ->
        // processCoreOnly=false should not throw.
        let inv  = Yaml.Dataset.fromYamlString false ProcessCore.Yaml.Tests.Fixtures.investigationString
        Expect.equal inv.Identifier "ara_prot_2023" "lenient mode decode ok"

    testCase "investigation strict mode throws" <| fun _ ->
        // processCoreOnly=true should throw.
        Expect.throws (fun () -> Yaml.Dataset.fromYamlString true ProcessCore.Yaml.Tests.Fixtures.investigationString |> ignore)
                      "strict mode decode should throw"

    testCase "assay identifier" <| fun _ ->
        let assay = loadAssay(false)
        Expect.equal assay.Identifier "measurement1" "identifier"

    testCase "assay process count" <| fun _ ->
        let assay = loadAssay(false)
        // 2 Growth + 6 Cell Lysis + 6 MS Run + 6 CPA = 20
        Expect.equal assay.Processes.Count 20 "twenty processes"

    testCase "assay first process name" <| fun _ ->
        let assay = loadAssay(false)
        Expect.equal assay.Processes.[0].Name "Growth" "first process is Growth"

    testCase "assay first process input name" <| fun _ ->
        let assay = loadAssay(false)
        let input = assay.Processes.[0].Input.Value
        match input with
        | SampleNode m -> Expect.equal m.Name "Base Culture" "first input name"
        | DataNode _     -> failwith "Expected SampleNode"

    testCase "assay first process input additionalType" <| fun _ ->
        let assay = loadAssay(false)
        let input = assay.Processes.[0].Input.Value
        match input with
        | SampleNode m -> Expect.equal m.AdditionalType (Some "Source") "additionalType"
        | DataNode _     -> failwith "Expected SampleNode"

    testCase "assay cell lycis process correctly resolved parameter values" <| fun _ ->
        let assay = loadAssay(false)
        let p = assay.GetProcess("Cell Lysis")
        Expect.hasLength p.ParameterValue 3 "three parameter values"
        Expect.equal p.ParameterValue.[0].Name "time" "first parameter value name"
        Expect.equal p.ParameterValue.[1].Name "sonicator" "second parameter value name"
        Expect.equal p.ParameterValue.[2].Name "technical replicate group" "third parameter value name"

    testCase "assay resolves indexed protocol references" <| fun _ ->
        let assay = loadAssay(false)
        let proc = assay.Processes.[0]
        Expect.isSome proc.ExecutesProtocol "executesProtocol resolved"
        Expect.equal proc.ExecutesProtocol.Value.Components.Count 1 "protocol equipment resolved"
        Expect.equal proc.ExecutesProtocol.Value.Components.[0].Name "growth environment" "equipment name"

    testCase "assay resolves indexed property value references" <| fun _ ->
        let assay = loadAssay(false)
        let proc =
            assay.Processes
            |> Seq.find (fun p -> p.Name = "Cell Lysis" && p.Output.IsSome)
        Expect.equal proc.ParameterValue.Count 3 "parameter values resolved"
        Expect.equal proc.ParameterValue.[0].Name "time" "first parameter value"

    testCase "assay MS Run output is Data" <| fun _ ->
        let assay = loadAssay(false)
        let msRun =
            assay.Processes
            |> Seq.find (fun p -> p.Name = "MS Run")
        Expect.isSome msRun.Output "one output"
        match msRun.Output.Value with
        | DataNode d -> Expect.isTrue (d.Path.EndsWith(".raw")) "MS Run output is .raw data file"
        | SampleNode _ -> failwith "Expected DataNode for MS Run output"

    testCase "assay CPA output path" <| fun _ ->
        let assay = loadAssay(false)
        let cpa =
            assay.Processes
            |> Seq.find (fun p -> p.Name = "Computational Proteome Analysis")
        Expect.isSome cpa.Output "one output"
        match cpa.Output.Value with
        | DataNode d ->
            Expect.isTrue (d.Path.StartsWith("proteomics_result.csv")) "CPA output file"
        | SampleNode _ -> failwith "Expected DataNode for CPA output"

    testCase "assay agents decode as typed administrative metadata" <| fun _ ->
        let assay = loadAssay(false)
        Expect.isTrue (assay.Agents.Count > 0) "agents decoded onto Dataset.Agents"

    testCase "assay labProtocols in overflow" <| fun _ ->
        let assay = loadAssay(false)
        let hasRecipes =
            assay.GetProperties(true)
            |> Seq.exists (fun kv -> kv.Key = "labProtocols")
        Expect.isTrue hasRecipes "labProtocols stored in overflow"

    testCase "assay strict mode fails" <| fun _ ->
        // processCoreOnly=true should throw because of unknown ISA fields like labProtocols.
        let decode = fun () -> Yaml.Dataset.fromYamlString true ProcessCore.Yaml.Tests.Fixtures.proteomicsAssayString |> ignore
        Expect.throws decode "strict mode should throw on unknown fields"

    // testCase "datamap raw YAML load" <| fun _ ->
    //     // datamap is not a Dataset, just test that the raw YAML can be parsed.
    //     let yaml = readExample "datamap_proteomics.yml"
    //     let element = YAMLicious.Reader.read yaml
    //     // The root object should have a 'datacontexts' key.
    //     let hasDataContexts =
    //         ProcessCore.Yaml.Helpers.getMappings element
    //         |> List.exists (fun (k, _) -> k = "datacontexts")
    //     Expect.isTrue hasDataContexts "datacontexts key present in raw parse"

]

