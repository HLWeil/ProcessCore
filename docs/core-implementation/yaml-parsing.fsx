(**
---
title: Reading And Writing YAML
category: Core Implementation
categoryindex: 3
index: 4
---

# Reading And Writing YAML

`ProcessCore.YML` turns YAML documents into the same in-memory graph objects used by `ProcessCore`.
*)

(*** hide ***)
#r "../../src/ProcessCore/bin/Release/netstandard2.0/ProcessCore.dll"
#r "../../src/ProcessCore.YML/bin/Release/netstandard2.0/ProcessCore.YML.dll"
#r "nuget: DynamicObj"
#r "nuget: YAMLicious, 1.0.0-alpha.10"
open System
open ProcessCore

let yamlCodeBlock summary (text: string) =
    text
    |> System.Net.WebUtility.HtmlEncode
    |> sprintf "<details><summary>%s</summary><pre><code lang=\"yaml\" class=\"language-yaml\">%s</code></pre></details>" summary

let firstLine (text: string) =
    text.Split([| '\r'; '\n' |], StringSplitOptions.RemoveEmptyEntries)
    |> Seq.tryHead
    |> Option.defaultValue text

(**
Load a profile-shaped assay example. Passing `false` means lenient mode: type decorations and extra profile fields are accepted and preserved where possible.
*)

let assayYaml =
    System.IO.Path.Combine(__SOURCE_DIRECTORY__, "../../examples/isa/assay_proteomics.yml")
    |> System.IO.File.ReadAllText

(*** hide ***)
yamlCodeBlock "Show assay YAML" assayYaml
(*** include-it-raw ***)

let assay = ProcessCore.Yaml.Dataset.fromYamlString false assayYaml

let assayShape =
    [ "identifier", assay.Identifier
      "additionalType", assay.AdditionalType |> Option.defaultValue ""
      "processes", string assay.Processes.Count
      "data nodes", string (assay.AllData().Count) ]

assayShape
(*** include-it ***)

(**
Strict mode is useful for core-shaped YAML. The same ISA/profile-shaped example contains extra fields, so strict mode rejects it.
*)

let strictModeResult =
    try
        ProcessCore.Yaml.Dataset.fromYamlString true assayYaml |> ignore
        "Strict mode accepted this YAML."
    with ex ->
        "Strict mode rejected this YAML: " + firstLine ex.Message

strictModeResult
(*** include-it ***)

(**
Writing can use inline objects or top-level indexes. Inline YAML is easy to inspect. Indexed YAML deduplicates repeated property values and protocols into `propertyValues` and `labProtocols` sections.
*)

let small = Dataset("yaml-demo")
let protocol = LabProtocol()
protocol.Name <- Some "Growth"
protocol.AddLabEquipment(PropertyValue("growth chamber", value = "chamber-1", additionalType = "Component"))

let source = Material("Seedling")
source.AdditionalType <- Some "Source"
source.AddAdditionalProperty(PropertyValue("organism", value = "Arabidopsis thaliana", additionalType = "CharacteristicValue"))

let sample = Material("Leaf sample")
sample.AdditionalType <- Some "Sample"
sample.AddAdditionalProperty(PropertyValue("temperature", value = "25", unit = "degree Celsius", additionalType = "FactorValue"))

let growth = LabProcess("Growth")
growth.ExecutesProtocol <- Some protocol
growth.AddInputMaterial(source)
growth.AddOutputMaterial(sample)
growth.AddParameterValue(PropertyValue("duration", value = "7", unit = "day", additionalType = "ParameterValue"))
small.AddProcess(growth)

let inlineYaml = ProcessCore.Yaml.Dataset.toYamlString (Some 2) small
let indexedYaml = ProcessCore.Yaml.Dataset.toYamlStringIndexed (Some 2) small

(*** hide ***)
yamlCodeBlock "Show inline YAML" inlineYaml
(*** include-it-raw ***)

(*** hide ***)
yamlCodeBlock "Show indexed YAML" indexedYaml
(*** include-it-raw ***)

(**
Round-tripping returns a new object graph with the same logical shape.
*)

let roundTripped = ProcessCore.Yaml.Dataset.fromYamlString true inlineYaml

let roundTripShape =
    [ "identifier", roundTripped.Identifier
      "processes", string roundTripped.Processes.Count
      "materials", string (roundTripped.AllMaterials().Count)
      "property values", string (roundTripped.AllPropertyValues().Count) ]

roundTripShape
(*** include-it ***)

(**
## What To Use When

| Task | API |
|------|-----|
| Read profile-shaped YAML | `ProcessCore.Yaml.Dataset.fromYamlString false` |
| Read strict core-shaped YAML | `ProcessCore.Yaml.Dataset.fromYamlString true` |
| Write inline YAML | `ProcessCore.Yaml.Dataset.toYamlString` |
| Write indexed YAML | `ProcessCore.Yaml.Dataset.toYamlStringIndexed` |
| Decode a specific type | `ProcessCore.Yaml.Material.fromYamlString`, `Data.fromYamlString`, etc. |
*)
