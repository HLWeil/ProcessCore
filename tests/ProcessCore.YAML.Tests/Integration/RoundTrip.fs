module ProcessCore.Yaml.Tests.Integration.RoundTrip

open Fable.Pyxpecto
open ProcessCore
open ProcessCore.Yaml
open ProcessCore.Yaml.Tests.Fixtures

let tests = testList "RoundTrip" [

    testCase "linear graph round-trip" <| fun _ ->
        let graph = makeLinearGraph()
        let yaml  = Yaml.Dataset.toYamlString None graph.DS

        let decoded = Yaml.Dataset.fromYamlString false yaml

        // Dataset identity
        Expect.equal decoded.Identifier "DS-A" "dataset identifier"
        Expect.equal decoded.Processes.Count 3 "three processes"

        // Process names preserved in order
        let names = decoded.Processes |> Seq.map (fun p -> p.Name) |> Seq.toList
        Expect.equal names ["p1"; "p2"; "p3"] "process names"

        // p1 inputs / outputs
        let p1 = decoded.Processes.[0]
        Expect.equal p1.Inputs.Count  1 "p1 one input"
        Expect.equal p1.Outputs.Count 1 "p1 one output"
        match p1.Inputs.[0] with
        | MaterialNode m -> Expect.equal m.Name "Source1" "p1 input name"
        | DataNode _     -> failwith "Expected MaterialNode for p1 input"
        match p1.Outputs.[0] with
        | MaterialNode m -> Expect.equal m.Name "Sample1" "p1 output name"
        | DataNode _     -> failwith "Expected MaterialNode for p1 output"

        // p1 parameter values
        Expect.equal p1.ParameterValue.Count 2 "p1 two parameter values"
        Expect.equal p1.ParameterValue.[0].Name "temperature" "p1 param 0 name"
        Expect.equal p1.ParameterValue.[0].Value (Some "37")  "p1 param 0 value"
        Expect.equal p1.ParameterValue.[0].Unit  (Some "°C")  "p1 param 0 unit"

        // p3 data output
        let p3 = decoded.Processes.[2]
        Expect.equal p3.Outputs.Count 1 "p3 one output"
        match p3.Outputs.[0] with
        | DataNode d -> Expect.equal d.Path "rawData1.csv" "p3 data path"
        | MaterialNode _ -> failwith "Expected DataNode for p3 output"

    testCase "nested dataset round-trip" <| fun _ ->
        let original = makeNestedDataset()
        let yaml     = Yaml.Dataset.toYamlString None original
        let decoded  = Yaml.Dataset.fromYamlString false yaml

        Expect.equal decoded.Identifier    "parent"  "parent identifier"
        Expect.equal decoded.HasPart.Count 2         "two children"

        let ids = decoded.HasPart |> Seq.map (fun d -> d.Identifier) |> Seq.toList
        Expect.equal ids ["child-a"; "child-b"] "child identifiers"

        // Each child retains its own process
        Expect.equal decoded.HasPart.[0].Processes.Count 1 "child-a has one process"
        Expect.equal decoded.HasPart.[1].Processes.Count 1 "child-b has one process"
        Expect.equal decoded.HasPart.[0].Processes.[0].Name "proc-a" "child-a process name"
        Expect.equal decoded.HasPart.[1].Processes.[0].Name "proc-b" "child-b process name"

        // PartOf back-edges are set
        Expect.isSome decoded.HasPart.[0].PartOf "child-a PartOf set"
        Expect.equal decoded.HasPart.[0].PartOf.Value.Identifier "parent" "child-a PartOf identifier"

    testCase "parameterValues round-trip" <| fun _ ->
        let ds  = Dataset("DS-PV")
        let proc = LabProcess("p-pv")
        proc.AddParameterValue(PropertyValue("temperature", value = "37",  unit = "°C",  nameTAN = "PATO:0000146"))
        proc.AddParameterValue(PropertyValue("rpm",         value = "200", unit = "rpm", nameTAN = "NCIT:C43060"))
        proc.AddParameterValue(PropertyValue("enzyme",      value = "Trypsin"))
        ds.AddProcess(proc)

        let yaml    = Yaml.Dataset.toYamlString None ds
        let decoded = Yaml.Dataset.fromYamlString false yaml

        let pvs = decoded.Processes.[0].ParameterValue
        Expect.equal pvs.Count 3 "three parameter values"
        Expect.equal pvs.[0].Name    "temperature"       "pv0 name"
        Expect.equal pvs.[0].Value   (Some "37")          "pv0 value"
        Expect.equal pvs.[0].Unit    (Some "°C")          "pv0 unit"
        Expect.equal pvs.[0].NameTAN (Some "PATO:0000146") "pv0 nameTAN"
        Expect.equal pvs.[1].Name    "rpm"                "pv1 name"
        Expect.equal pvs.[1].Value   (Some "200")         "pv1 value"
        Expect.equal pvs.[2].Name    "enzyme"             "pv2 name"
        Expect.equal pvs.[2].Value   (Some "Trypsin")     "pv2 value"

    testCase "protocol round-trip" <| fun _ ->
        let proto = LabProtocol(
                        name        = "extraction",
                        description = "Standard extraction",
                        version     = "1.0",
                        url         = "https://protocols.io/extraction-v1")
        proto.IntendedUse <- Some (DefinedTerm("cell growth", tan = "GO:0016049"))
        proto.AddParameter(FormalParameter("temperature", nameTAN = "PATO:0000146"))
        proto.AddParameter(FormalParameter("rpm"))
        proto.AddLabEquipment(PropertyValue("centrifuge", value = "Eppendorf 5420"))
        proto.AddAdditionalProperty(PropertyValue("notes", value = "Keep on ice"))

        let proc = LabProcess("p1")
        proc.ExecutesProtocol <- Some proto
        let ds = Dataset("DS-proto")
        ds.AddProcess(proc)

        let yaml    = Yaml.Dataset.toYamlString None ds
        let decoded = Yaml.Dataset.fromYamlString false yaml

        let p      = decoded.Processes.[0]
        Expect.isSome p.ExecutesProtocol "executesProtocol present"
        let dp = p.ExecutesProtocol.Value
        Expect.equal dp.Name        (Some "extraction")            "protocol name"
        Expect.equal dp.Description (Some "Standard extraction")   "protocol description"
        Expect.equal dp.Version     (Some "1.0")                   "protocol version"
        Expect.equal dp.Url         (Some "https://protocols.io/extraction-v1") "protocol url"
        Expect.isSome dp.IntendedUse                               "intendedUse present"
        Expect.equal dp.IntendedUse.Value.Name "cell growth"       "intendedUse name"
        Expect.equal dp.IntendedUse.Value.TAN (Some "GO:0016049")  "intendedUse TAN"
        Expect.equal dp.Parameters.Count 2                         "two parameters"
        Expect.equal dp.Parameters.[0].Name "temperature"          "param 0 name"
        Expect.equal dp.Parameters.[0].NameTAN (Some "PATO:0000146") "param 0 nameTAN"
        Expect.equal dp.LabEquipment.Count 1                       "one labEquipment"
        Expect.equal dp.AdditionalProperty.Count 1                 "one additionalProperty"

    testCase "whitespace option" <| fun _ ->
        let ds = Dataset("DS-ws")
        let proc = LabProcess("p1")
        proc.AddParameterValue(PropertyValue("temperature", value = "37"))
        ds.AddProcess(proc)

        let yaml2 = Yaml.Dataset.toYamlString (Some 2) ds
        let yaml4 = Yaml.Dataset.toYamlString (Some 4) ds

        // Both should decode to equivalent structures
        let dec2 = Yaml.Dataset.fromYamlString false yaml2
        let dec4 = Yaml.Dataset.fromYamlString false yaml4
        Expect.equal dec2.Identifier dec4.Identifier "same identifier"
        Expect.equal dec2.Processes.Count dec4.Processes.Count "same process count"
        Expect.equal dec2.Processes.[0].ParameterValue.[0].Value
                     dec4.Processes.[0].ParameterValue.[0].Value
                     "same parameter value"

        // 4-space YAML should be longer than 2-space for nested content
        Expect.isTrue (yaml4.Length > yaml2.Length) "4-space output longer than 2-space"

    testCase "Decode.fromYamlString entry point" <| fun _ ->
        let yaml    = "type: Dataset\nidentifier: DS-decode\n"
        let decoded = Decode.fromYamlString (Yaml.Dataset.decoder true) yaml
        Expect.equal decoded.Identifier "DS-decode" "identifier via Decode.fromYamlString"

    testCase "Encode.toYamlString entry point" <| fun _ ->
        let ds   = Dataset("DS-encode")
        let yaml = Encode.toYamlString 2 (Yaml.Dataset.encoder ds)
        Expect.isTrue (yaml.Contains("identifier: DS-encode")) "identifier via Encode.toYamlString"
        Expect.isTrue (yaml.Contains("type: Dataset"))         "type via Encode.toYamlString"

]
