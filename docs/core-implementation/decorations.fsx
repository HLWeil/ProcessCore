(**
---
title: Decorations
category: Core Implementation
categoryindex: 3
index: 3
---

# Decorations

The core data model is intentionally small: `Dataset`, `Process`, `Recipe`, `Sample`, `Data`, and `Annotation` describe the shape of a process graph.
Domain specificity is added as decoration on top of that shared shape.

There are two complementary ways to do this:

1. Use `additionalType` and `additionalProperty` on core objects. This keeps the data close to the ProcessCore model and makes the extension queryable as typed `Annotation` annotations.
2. Use the inherited `DynamicObj` property bag for information that must be preserved but does not fit into the core model.

This page shows both approaches.
*)

(*** hide ***)
#r "../../src/ProcessCore/bin/Release/netstandard2.0/ProcessCore.dll"
#r "nuget: DynamicObj"
#r "nuget: YAMLicious, 1.0.0-alpha.10"
open DynamicObj
open ProcessCore

let valueOrBlank = Option.defaultValue ""

let pvSummary (pv: Annotation) =
    let typeText = pv.AdditionalType |> valueOrBlank
    let valueText = pv.ValueWithUnitText
    if valueText = "" then
        sprintf "%s: %s" typeText pv.Name
    else
        sprintf "%s: %s = %s" typeText pv.Name valueText

let yamlCodeBlock summary (text: string) =
    text
    |> System.Net.WebUtility.HtmlEncode
    |> sprintf "<details><summary>%s</summary><pre><code lang=\"yaml\" class=\"language-yaml\">%s</code></pre></details>" summary

(**
## Typed Decorations

The preferred extension path is to specialize core objects with `additionalType` and then attach ontologized `Annotation` records to the appropriate slot.
The example below builds a small proteomics-style assay without introducing new graph node types.
*)

let assay = Dataset("measurement1", additionalType = "Assay")
assay.Title <- Some "Proteomics assay"

let source =
    Sample("Base Culture", additionalType = "Source")

let organism =
    Annotation(
        "organism",
        value = "Arabidopsis thaliana",
        nameTAN = "https://bioregistry.io/SIO:010000",
        valueTAN = "https://bioregistry.io/NCBITaxon:3702",
        additionalType = "CharacteristicValue")

source.AddAdditionalProperty(organism)

let roomTemperatureSample =
    Sample("Cultivation Flask RT", additionalType = "Sample")

let temperature25 =
    Annotation(
        "temperature",
        value = "25",
        unit = "degree Celsius",
        nameTAN = "https://bioregistry.io/NCRO:0000029",
        unitTAN = "https://bioregistry.io/UO:0000027",
        additionalType = "FactorValue")

roomTemperatureSample.AddAdditionalProperty(temperature25)

let highTemperatureSample =
    Sample("Cultivation Flask HT", additionalType = "Sample")

let temperature30 =
    Annotation(
        "temperature",
        value = "30",
        unit = "degree Celsius",
        nameTAN = "https://bioregistry.io/NCRO:0000029",
        unitTAN = "https://bioregistry.io/UO:0000027",
        additionalType = "FactorValue")

highTemperatureSample.AddAdditionalProperty(temperature30)

let growthProtocol = Recipe(name = "Growth")
growthProtocol.AddLabEquipment(
    Annotation(
        "growth environment",
        value = "bioreactor",
        nameTAN = "https://bioregistry.io/OBI:0000997",
        valueTAN = "https://bioregistry.io/OBI:0001046",
        additionalType = "Component"))

let growthAt25 = Process("Growth", executesProtocol = growthProtocol)
growthAt25.AddInputSample(source)
growthAt25.AddOutputSample(roomTemperatureSample)
assay.AddProcess(growthAt25)

let growthAt30 = Process("Growth", executesProtocol = growthProtocol)
growthAt30.AddInputSample(source)
growthAt30.AddOutputSample(highTemperatureSample)
assay.AddProcess(growthAt30)

let assayDecoration =
    [ "identifier", assay.Identifier
      "dataset additionalType", assay.AdditionalType |> valueOrBlank
      "processes", string assay.Processes.Count
      "samples", string (assay.AllSamples().Count)
      "data nodes", string (assay.AllData().Count) ]

assayDecoration
(*** include-it ***)

(**
The dataset is still a `Dataset`, but `additionalType = "Assay"` tells downstream code which domain role it plays.
The same pattern is used for sample roles: the input is a `Source`, while the outputs are `Sample` samples.
*)

let sampleRoles =
    assay.AllSamples()
    |> Seq.countBy (fun sample -> sample.AdditionalType |> valueOrBlank)
    |> Seq.map (fun (role, count) -> role, count)
    |> Seq.toList

sampleRoles
(*** include-it ***)

(**
The first `Growth` process shows a compact ISA-style shape:

- The input sample is a `Source`.
- The output sample is a `Sample`.
- Characteristics are attached to input nodes via `AdditionalProperty`.
- Factors are attached to output nodes via `AdditionalProperty`.
*)

let growthInput =
    growthAt25.InputSamples()
    |> Seq.head

let growthOutput =
    growthAt25.OutputSamples()
    |> Seq.head

let growthDecoration =
    [ "process", growthAt25.Name
      "input", sprintf "%s (%s)" growthInput.Name (growthInput.AdditionalType |> valueOrBlank)
      "input annotations", growthInput.AdditionalProperty |> Seq.map pvSummary |> String.concat "; "
      "output", sprintf "%s (%s)" growthOutput.Name (growthOutput.AdditionalType |> valueOrBlank)
      "output annotations", growthOutput.AdditionalProperty |> Seq.map pvSummary |> String.concat "; "
      "protocol components", growthProtocol.LabEquipment |> Seq.map pvSummary |> String.concat "; " ]

growthDecoration
(*** include-it ***)

(**
Process parameters use the same `Annotation` type, but they live on the `Process.ParameterValue` slot.
Here, cell lysis records the sonicator, lysis duration, and technical replicate group as `ParameterValue` decorations.
*)

let sonicator =
    Annotation(
        "sonicator",
        value = "Fisherbrand Model 705 Sonic Dismembrator",
        nameTAN = "https://bioregistry.io/OBI:0400114",
        valueTAN = "https://bioregistry.io/OBI:5453453",
        additionalType = "ParameterValue")

let lysisTime =
    Annotation(
        "time",
        value = "10",
        unit = "minute",
        nameTAN = "https://bioregistry.io/PATO:0000165",
        unitTAN = "https://bioregistry.io/UO:0000031",
        additionalType = "ParameterValue")

let technicalReplicate =
    Annotation(
        "technical replicate group",
        value = "1",
        nameTAN = "https://bioregistry.io/DPBO:1000184",
        additionalType = "ParameterValue")

let lysis = Process("Cell Lysis")
lysis.AddInputSample(roomTemperatureSample)
lysis.AddOutputSample(Sample("Eppi RT 1", additionalType = "Sample"))
lysis.AddParameterValue(sonicator)
lysis.AddParameterValue(lysisTime)
lysis.AddParameterValue(technicalReplicate)
assay.AddProcess(lysis)

lysis.ParameterValue
|> Seq.map pvSummary
|> Seq.toList
(*** include-it ***)

(**
The practical benefit of this approach is that extensions remain easy to query.
For example, all samples produced under the 25 degree Celsius growth factor can be found with ordinary F# sequence operations.
*)

let samplesAt25Degrees =
    assay.AllSamples()
    |> Seq.filter (fun sample ->
        sample.AdditionalType = Some "Sample"
        && sample.AdditionalProperty
           |> Seq.exists (fun pv ->
               pv.AdditionalType = Some "FactorValue"
               && pv.Name = "temperature"
               && pv.Value = Some "25"))
    |> Seq.map (fun sample -> sample.Name)
    |> Seq.toList

samplesAt25Degrees
(*** include-it ***)

(**
## DynamicObj Extensions

All main ProcessCore classes inherit from `DynamicObj`. This gives each object a property bag for extension data that should be preserved, but that does not naturally belong in the process graph.

Use this for metadata such as facility layout, local tracking fields, UI state, or profile-specific fields that a core-only library should not interpret.
The example below adds an experimental facility layout to a dataset.
*)

let facilityDataset = Dataset("facility-layout-demo", additionalType = "Assay")
facilityDataset.Title <- Some "Greenhouse proteomics assay"

let environmentalControls = DynamicObj()
environmentalControls.SetProperty("temperatureSetpoint", "22 degree Celsius")
environmentalControls.SetProperty("relativeHumiditySetpoint", "60 percent")
environmentalControls.SetProperty("photoperiod", "16 h light / 8 h dark")

let facilityLayout = DynamicObj()
facilityLayout.SetProperty("facilityName", "Phytotron A")
facilityLayout.SetProperty("room", "Growth room 2")
facilityLayout.SetProperty("bench", "North bench")
facilityLayout.SetProperty("instrumentBay", "LC-MS bay 1")
facilityLayout.SetProperty("coordinateSystem", "room-grid")
facilityLayout.SetProperty("locationCode", "A-02-N-03")
facilityLayout.SetProperty("environmentalControls", environmentalControls)

facilityDataset.SetProperty("experimentalFacilityLayout", facilityLayout)

let recoveredFacility =
    facilityDataset.TryGetTypedPropertyValue<DynamicObj>("experimentalFacilityLayout")

let facilitySummary =
    match recoveredFacility with
    | Some layout ->
        [ "facility", layout.TryGetTypedPropertyValue<string>("facilityName") |> valueOrBlank
          "room", layout.TryGetTypedPropertyValue<string>("room") |> valueOrBlank
          "bench", layout.TryGetTypedPropertyValue<string>("bench") |> valueOrBlank
          "location", layout.TryGetTypedPropertyValue<string>("locationCode") |> valueOrBlank ]
    | None ->
        [ "facility", "missing" ]

facilitySummary
(*** include-it ***)

(**
The YAML writer emits DynamicObj properties as overflow fields after the known ProcessCore fields.
This keeps the data round-trippable without requiring the core model to know what an `experimentalFacilityLayout` is.
*)

let facilityYaml =
    ProcessCore.Yaml.Dataset.toYamlString (Some 2) facilityDataset

(*** hide ***)
yamlCodeBlock "Show dataset YAML with DynamicObj extension" facilityYaml
(*** include-it-raw ***)

(**
Read it back in lenient mode to preserve the extension field.
Strict mode is for core-only documents and rejects unknown fields.
*)

let roundTrippedFacility =
    ProcessCore.Yaml.Dataset.fromYamlString false facilityYaml

let roundTrippedLayout =
    roundTrippedFacility.TryGetTypedPropertyValue<DynamicObj>("experimentalFacilityLayout")

roundTrippedLayout.IsSome
(*** include-it ***)

(**
## What To Use When

| Task | API |
|------|-----|
| Give a core object a domain role | `AdditionalType` |
| Attach characteristics or factors to samples/data | `node.AddAdditionalProperty` |
| Attach process parameters | `process.AddParameterValue` |
| Attach protocol components | `protocol.AddLabEquipment` |
| Keep extensions ontologized and queryable | `Annotation(name, value, unit, nameTAN, valueTAN, unitTAN)` |
| Preserve metadata outside the core graph | `SetProperty`, `TryGetTypedPropertyValue` from `DynamicObj` |
| Read/write decorated YAML | `ProcessCore.Yaml.Dataset.fromYamlString false`, `toYamlString` |
| Enforce core-only YAML | `ProcessCore.Yaml.Dataset.fromYamlString true` |
*)
