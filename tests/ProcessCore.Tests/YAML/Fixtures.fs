module ProcessCore.Yaml.Tests.Fixtures

open ProcessCore
open ProcessCore.Yaml

// ─────────────────────────────────────────────────────────────────────────────
// Simple fixtures — let-bound values.
// Tests that mutate shared objects must reconstruct locally.
// ─────────────────────────────────────────────────────────────────────────────

let fixtureDT =
    DefinedTerm("cell growth", tan = "GO:0016049", inDefinedTermSet = "http://purl.obolibrary.org/obo/go.owl")

let fixtureFP =
    FormalParameter("temperature", nameTAN = "PATO:0000146", defaultValue = DefinedTerm("37°C"))

let fixturePV =
    Annotation(
        "Temperature",
        value          = "37",
        unit           = "°C",
        nameTAN        = "PATO:0000146",
        valueTAN       = "http://example.org/37",
        unitTAN        = "UO:0000027",
        additionalType = "Parameter")

let fixtureSample =
    let m = Sample("Sample1", additionalType = "Sample")
    m.AddAdditionalProperty(Annotation("organism", value = "Arabidopsis thaliana"))
    m.AddAdditionalProperty(Annotation("age", value = "4", unit = "week"))
    m

let fixtureData =
    let d = Data("rawData1.csv", selector = "Sheet1", selectorFormat = "excel", encodingFormat = "text/csv")
    d.AddAdditionalProperty(Annotation("instrument", value = "Q Exactive"))
    d

let fixtureRecipe =
    let lp = Recipe(
                name        = "extraction",
                description = "Standard protein extraction protocol",
                version     = "1.0",
                url         = "https://protocols.io/extraction-v1")
    lp.IntendedUse <- Some (DefinedTerm("cell growth"))
    lp.AddParameter(FormalParameter("temperature"))
    lp.AddLabEquipment(Annotation("centrifuge", value = "Eppendorf 5420"))
    lp.AddAdditionalProperty(Annotation("notes", value = "Keep on ice"))
    lp

let fixtureProcess =
    let proc = Process("p1")
    proc.AddInput(SampleNode fixtureSample)
    proc.AddOutput(DataNode fixtureData)
    proc.ExecutesProtocol <- Some fixtureRecipe
    proc.AddParameterValue(Annotation("temperature", value = "37", unit = "°C"))
    proc.AddParameterValue(Annotation("rpm", value = "200", unit = "rpm"))
    proc

let fixtureDataset =
    let ds = Dataset("DS-1")
    ds.AddProcess(fixtureProcess)
    ds.AddPart(Dataset("DS-1/assay"))
    ds

// ─────────────────────────────────────────────────────────────────────────────
// Graph fixtures (functions — always fresh instances)
// ─────────────────────────────────────────────────────────────────────────────

type LinearGraph =
    { DS  : Dataset
      P1  : Process
      P2  : Process
      P3  : Process }

let makeLinearGraph () =
    let source1  = Sample("Source1",  additionalType = "Source")
    let sample1  = Sample("Sample1",  additionalType = "Sample")
    let sample2  = Sample("Sample2",  additionalType = "Sample")
    let rawData1 = Data("rawData1.csv")

    let proto1 = Recipe(name = "extraction")
    proto1.IntendedUse <- Some (DefinedTerm("cell growth"))
    let p1 = Process("p1")
    p1.AddInput(SampleNode source1)
    p1.AddOutput(SampleNode sample1)
    p1.ExecutesProtocol <- Some proto1
    p1.AddParameterValue(Annotation("temperature", value = "37", unit = "°C"))
    p1.AddParameterValue(Annotation("rpm", value = "200", unit = "rpm"))

    let proto2 = Recipe(name = "digestion")
    let p2 = Process("p2")
    p2.AddInput(SampleNode sample1)
    p2.AddOutput(SampleNode sample2)
    p2.ExecutesProtocol <- Some proto2
    p2.AddParameterValue(Annotation("enzyme", value = "Trypsin"))

    let p3 = Process("p3")
    p3.AddInput(SampleNode sample2)
    p3.AddOutput(DataNode rawData1)

    let ds = Dataset("DS-A")
    ds.AddProcess(p1)
    ds.AddProcess(p2)
    ds.AddProcess(p3)

    { DS = ds; P1 = p1; P2 = p2; P3 = p3 }

let makeNestedDataset () =
    let childA = Dataset("child-a")
    let childB = Dataset("child-b")
    childA.AddProcess(Process("proc-a"))
    childB.AddProcess(Process("proc-b"))
    let parent = Dataset("parent")
    parent.AddPart(childA)
    parent.AddPart(childB)
    parent

let proteomicsAssayString = """type: Dataset
additionalType: Assay
identifier: measurement1
creators:
  -
    type: Person
    givenName: Oliver
    affiliation:
      type: Organization
      name: RPTU University of Kaiserslautern
    email: mailto:maus@nfdi4plants.org
    familyName: Maus
    jobTitles:
      "@id": http://purl.org/spar/scoro/research-assistant
      "@type": DefinedTerm
      name: research assistant
      termCode: http://purl.org/spar/scoro/research-assistant
labProtocols:
    -
      "@id": "#Protocol_Growth"
      type: Recipe
      labEquipments:
        "@id": "#Component_growth_environment_bioreactor"
    -
      "@id": "#Protocol_Cell_Lysis"
      type: Recipe
      labEquipments:
        "@id": "#Component_centrifuge_Eppendorf™_Centrifuge_5420"
    -
      "@id": "#Protocol_MS_Run"
      type: Recipe
      labEquipments:
        "@id": "#Component_mass_spectrometer_Q_Exactive_9000"
    -
      "@id": "#Protocol_Computational_Proteome_Analysis"
      type: Recipe
annotations:
  -
    "@id": "#ParameterValue_sonicator_Fisherbrand_Model_705_Sonic_Dismembrator"
    type: Annotation
    additionalType: ParameterValue # inherits from Annotation
    name: sonicator
    nameTAN: https://bioregistry.io/OBI:0400114
    value: Fisherbrand™ Model 705 Sonic Dismembrator
    valueTAN: https://bioregistry.io/OBI:5453453
  -
    "@id": "#ParameterValue_time_10_minute"
    type: Annotation
    additionalType: ParameterValue
    name: time
    nameTAN: https://bioregistry.io/PATO:0000165
    value: 10
    unit: minute
    unitTAN: https://bioregistry.io/UO:0000031
  -
    "@id": "#ParameterValue_technical_replicate_group_1"
    type: Annotation
    additionalType: ParameterValue
    name: technical replicate group
    nameTAN: https://bioregistry.io/DPBO:1000184
    value: 1
  -
    "@id": "#ParameterValue_technical_replicate_group_2"
    type: Annotation
    additionalType: ParameterValue
    name: technical replicate group
    nameTAN: https://bioregistry.io/DPBO:1000184
    value: 2
  -
    "@id": "#ParameterValue_technical_replicate_group_3"
    type: Annotation
    additionalType: ParameterValue
    name: technical replicate group
    nameTAN: https://bioregistry.io/DPBO:1000184
    value: 3
  -
    "@id": "#ParameterValue_software_ProteomIqon"
    type: Annotation
    additionalType: ParameterValue
    name: software
    nameTAN: https://bioregistry.io/IAO_0000010
    value: ProteomIQon
  -
    "@id": "#CharacteristicValue_organism_Arabidopsis_thaliana"
    type: Annotation
    additionalType: CharacteristicValue
    name: organism
    nameTAN: https://bioregistry.io/SIO:010000
    value: Arabidopsis thaliana
    valueTAN: https://bioregistry.io/NCBITaxon:3702
  -
    "@id": "#FactorValue_temperature_25_degree_Celsius"
    type: Annotation
    additionalType: FactorValue
    name: temperature
    nameTAN: https://bioregistry.io/NCRO:0000029
    value: 25
    unit: degree Celsius
    unitTAN: https://bioregistry.io/UO:0000027
  -
    "@id": "#FactorValue_temperature_30_degree_Celsius"
    type: Annotation
    additionalType: FactorValue
    name: temperature
    nameTAN: https://bioregistry.io/NCRO:0000029
    value: 30
    unit: degree Celsius
    unitTAN: https://bioregistry.io/UO:0000027
  -
    "@id": "#Component_growth_environment_bioreactor"
    type: Annotation
    additionalType: Component
    name: growth environment
    nameTAN: https://bioregistry.io/OBI:0000997
    value: bioreactor
    valueTAN: https://bioregistry.io/OBI:0001046
  -
    "@id": "#Component_mass_spectrometer_Q_Exactive_9000"
    type: Annotation
    additionalType: Component
    name: mass spectrometer
    nameTAN: https://bioregistry.io/OBI:0000049
    value: Q Exactive 9000
processes:
  -
    # Possible worksheet grouping
    type: Process
    name: Growth
    inputs:
      - type: Sample # = additionalType: [Source]
        additionalType: Source
        name: Base Culture
        additionalProperty:
          -
           "@id": "#CharacteristicValue_organism_Arabidopsis_thaliana"
    outputs:
      - type: Sample
        additionalType: Sample
        name: Cultivation Flask RT
        additionalProperty:
          -
           "@id": "#FactorValue_temperature_25_degree_Celsius"
    executesProtocol:
      "@id": "#Protocol_Growth"
  -
    type: Process
    name: Growth
    inputs:
      - type: Sample
        additionalType: Source
        name: Base Culture
        additionalProperty:
          -
           "@id": "#CharacteristicValue_organism_Arabidopsis_thaliana"
    outputs:
      - type: Sample
        additionalType: Sample
        name: Cultivation Flask HT
        additionalProperty:
          -
           "@id": "#FactorValue_temperature_30_degree_Celsius"
    executesProtocol:
      "@id": "#Protocol_Growth"
  -
    type: Process
    name: Cell Lysis
    inputs:
      - type: Sample
        additionalType: Source # = additionalType: [Source]
        name: Cultivation Flask RT
    outputs:
      - type: Sample
        additionalType: Sample
        name: Eppi RT 1
    executesProtocol:
      "@id": "#Protocol_Cell_Lysis"
    parameterValue:
      - "@id": "#ParameterValue_time_10_minute"
      - "@id": "#ParameterValue_sonicator_Fisherbrand_Model_705_Sonic_Dismembrator"
      - "@id": "#ParameterValue_technical_replicate_group_1"
  -
    type: Process
    name: Cell Lysis
    inputs:
      - type: Sample # = additionalType: [Source]
        additionalType: Source
        name: Cultivation Flask RT
    outputs:
      - type: Sample
        additionalType: Sample
        name: Eppi RT 2
    executesProtocol:
      "@id": "#Protocol_Cell_Lysis"
    parameterValue:
      - "@id": "#ParameterValue_time_10_minute"
      - "@id": "#ParameterValue_sonicator_Fisherbrand_Model_705_Sonic_Dismembrator"
      - "@id": "#ParameterValue_technical_replicate_group_2"
  -
    type: Process
    name: Cell Lysis
    inputs:
      - type: Sample # = additionalType: [Source]
        additionalType: Source
        name: Cultivation Flask RT
    outputs:
      - type: Sample
        additionalType: Sample
        name: Eppi RT 3
    executesProtocol:
      "@id": "#Protocol_Cell_Lysis"
    parameterValue:
      - "@id": "#ParameterValue_time_10_minute"
      - "@id": "#ParameterValue_sonicator_Fisherbrand_Model_705_Sonic_Dismembrator"
      - "@id": "#ParameterValue_technical_replicate_group_3"
  -
    type: Process
    name: Cell Lysis
    inputs:
      - type: Sample # = additionalType: [Source]
        additionalType: Source
        name: Cultivation Flask HT
    outputs:
      - type: Sample
        additionalType: Sample
        name: Eppi HT 1
    executesProtocol:
      "@id": "#Protocol_Cell_Lysis"
    parameterValue:
      - "@id": "#ParameterValue_time_10_minute"
      - "@id": "#ParameterValue_sonicator_Fisherbrand_Model_705_Sonic_Dismembrator"
      - "@id": "#ParameterValue_technical_replicate_group_1"
  -
    type: Process
    name: Cell Lysis
    inputs:
      - type: Sample # = additionalType: [Source]
        additionalType: Source
        name: Cultivation Flask HT
    outputs:
      - type: Sample
        additionalType: Sample
        name: Eppi HT 2
    executesProtocol:
      "@id": "#Protocol_Cell_Lysis"
    parameterValue:
      - "@id": "#ParameterValue_time_10_minute"
      - "@id": "#ParameterValue_sonicator_Fisherbrand_Model_705_Sonic_Dismembrator"
      - "@id": "#ParameterValue_technical_replicate_group_2"
  -
    type: Process
    name: Cell Lysis
    inputs:
      - type: Sample # = additionalType: [Source]
        additionalType: Source
        name: Cultivation Flask HT
    outputs:
      - type: Sample
        additionalType: Sample
        name: Eppi HT 3
    executesProtocol:
      "@id": "#Protocol_Cell_Lysis"
    parameterValue:
      - "@id": "#ParameterValue_time_10_minute"
      - "@id": "#ParameterValue_sonicator_Fisherbrand_Model_705_Sonic_Dismembrator"
      - "@id": "#ParameterValue_technical_replicate_group_3"
  -
    type: Process
    name: MS Run
    inputs:
      - type: Sample
        additionalType: Sample
        name: Eppi RT 1
    outputs:
      - type: Data
        path: sample1.raw
    executesProtocol:
      "@id": "#Protocol_MS_Run"
  -
    type: Process
    name: MS Run
    inputs:
      - type: Sample
        additionalType: Sample
        name: Eppi RT 2
    outputs:
      - type: Data
        path: sample2.raw
    executesProtocol:
      "@id": "#Protocol_MS_Run"
  -
    type: Process
    name: MS Run
    inputs:
      - type: Sample
        additionalType: Sample
        name: Eppi RT 3
    outputs:
      - type: Data
        path: sample3.raw
    executesProtocol:
      "@id": "#Protocol_MS_Run"
  -
    type: Process
    name: MS Run
    inputs:
      - type: Sample
        additionalType: Sample
        name: Eppi HT 1
    outputs:
      - type: Data
        path: sample4.raw
    executesProtocol:
      "@id": "#Protocol_MS_Run"
  -
    type: Process
    name: MS Run
    inputs:
      - type: Sample
        additionalType: Sample
        name: Eppi HT 2
    outputs:
      - type: Data
        path: sample5.raw
    executesProtocol:
      "@id": "#Protocol_MS_Run"
  -
    type: Process
    name: MS Run
    inputs:
      - type: Sample
        additionalType: Sample
        name: Eppi HT 3
    outputs:
      - type: Data
        path: sample6.raw
    executesProtocol:
      "@id": "#Protocol_MS_Run"
  -
    type: Process
    name: Computational Proteome Analysis
    inputs:
      - type: Data
        path: sample1.raw
    outputs:
      - type: Data
        path: "proteomics_result.csv#col=12"
        encodingFormat: text/csv
        usageInfo: https://datatracker.ietf.org/doc/html/rfc7111
    executesProtocol:
      "@id": "#Protocol_Computational_Proteome_Analysis"
    parameterValue:
      "@id": "#ParameterValue_software_ProteomIqon"
  -
    type: Process
    name: Computational Proteome Analysis
    inputs:
      - type: Data
        path: sample2.raw
    outputs:
      - type: Data
        path: "proteomics_result.csv#col=13"
        encodingFormat: text/csv
        usageInfo: https://datatracker.ietf.org/doc/html/rfc7111
    executesProtocol:
      "@id": "#Protocol_Computational_Proteome_Analysis"
    parameterValue:
      "@id": "#ParameterValue_software_ProteomIqon"
  -
    type: Process
    name: Computational Proteome Analysis
    inputs:
      - type: Data
        path: sample3.raw
    outputs:
      - type: Data
        path: "proteomics_result.csv#col=14"
        encodingFormat: text/csv
        usageInfo: https://datatracker.ietf.org/doc/html/rfc7111
    executesProtocol:
      "@id": "#Protocol_Computational_Proteome_Analysis"
    parameterValue:
      "@id": "#ParameterValue_software_ProteomIqon"
  -
    type: Process
    name: Computational Proteome Analysis
    inputs:
      - type: Data
        path: sample4.raw
    outputs:
      - type: Data
        path: "proteomics_result.csv#col=15"
        encodingFormat: text/csv
        usageInfo: https://datatracker.ietf.org/doc/html/rfc7111
    executesProtocol:
      "@id": "#Protocol_Computational_Proteome_Analysis"
    parameterValue:
      "@id": "#ParameterValue_software_ProteomIqon"
  -
    type: Process
    name: Computational Proteome Analysis
    inputs:
      - type: Data
        path: sample5.raw
    outputs:
      - type: Data
        path: "proteomics_result.csv#col=16"
        encodingFormat: text/csv
        usageInfo: https://datatracker.ietf.org/doc/html/rfc7111
    executesProtocol:
      "@id": "#Protocol_Computational_Proteome_Analysis"
    parameterValue:
      "@id": "#ParameterValue_software_ProteomIqon"
  -
    type: Process
    name: Computational Proteome Analysis
    inputs:
      - type: Data
        path: sample6.raw
    outputs:
      - type: Data
        path: "proteomics_result.csv#col=17"
        encodingFormat: text/csv
        usageInfo: https://datatracker.ietf.org/doc/html/rfc7111
    executesProtocol:
      "@id": "#Protocol_Computational_Proteome_Analysis"
    parameterValue:
      "@id": "#ParameterValue_software_ProteomIqon"

additionalProperty:
  - # = generalProperty: (measurement type)
    type: Annotation
    name: variableMeasured
    nameTAN: https://schema.org/variableMeasured
    value: proteomics
    valueTAN: https://bioregistry.io/MS:1003348"""


let investigationString = """type: Dataset
additionalType: Investigation
identifier: ara_prot_2023
title: Validation of Proteins in Arabidopsis thaliana
description: Some people say that Arabidopsis thaliana does not contain any proteins, but lives on vibes. This investigation aims to validate the presence of proteins in Arabidopsis thaliana using various experimental techniques, including proteomics and chill.
creators:
  type: Person
  givenName: Oliver
  affiliation:
    type: Organization
    name: RPTU University of Kaiserslautern
  email: mailto:maus@nfdi4plants.org
  familyName: Maus
  jobTitles:
    "@id": http://purl.org/spar/scoro/research-assistant
    "@type": DefinedTerm
    name: research assistant
    termCode: http://purl.org/spar/scoro/research-assistant
additionalProperty:
  - type: Annotation
    name: latitude
    nameTAN: http://www.eionet.europa.eu/gemet/concept/14936
    value: 49.4431
    unit: degree
    unitTAN: http://purl.obolibrary.org/obo/UO_0000185
  - type: Annotation
    name: longitude
    nameTAN: http://www.eionet.europa.eu/gemet/concept/14936
    value: 7.7682
    unit: degree
    unitTAN: http://purl.obolibrary.org/obo/UO_0000185
  - type: Annotation
    name: aim
    nameTAN: https://spec.edmcouncil.org/fibo/ontology/FND/GoalsAndObjectives/Objectives/Aim
    value: To validate the presence of proteins in Arabidopsis thaliana using various experimental techniques, including proteomics and chill."""
