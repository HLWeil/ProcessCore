(**
---
title: Using DataContext
category: Core Implementation
categoryindex: 3
index: 8
---

# Using DataContext

`DataContext` describes what a data file or selected data fragment represents.
The `Data` object still owns file-location information such as `path`, `selector`, `selectorFormat`, and `encodingFormat`.
The `DataContext` adds semantic context around that target, such as its explication, object type, and unit.

This split lets process graphs stay focused on provenance while Datamap entries describe how to interpret selected data regions.
Typical workflows use `DataContext` to answer questions such as:

- Which columns in this table contain identifiers?
- Which data fragments represent abundance values?
- Which context covers this `Data` node produced by a process?
- Which selector should my downstream table library use?
*)

(*** hide ***)
#r "../../src/ProcessCore/bin/Release/netstandard2.1/ProcessCore.dll"
#r "nuget: DynamicObj"
open ProcessCore

let fsharpCodeBlock summary (text: string) =
    text
    |> System.Net.WebUtility.HtmlEncode
    |> sprintf "<details><summary>%s</summary><pre><code lang=\"fsharp\" class=\"language-fsharp\">%s</code></pre></details>" summary

(**
## Create DataContext Entries

Start with ontology-backed terms for the meanings you need to recover later.
`DefinedTerm.SemanticallyEquals` prefers TAN equality when both terms have TANs, and otherwise falls back to exact term equality.
*)

let proteinIdentifier = DefinedTerm("protein identifier", tan = "http://purl.obolibrary.org/obo/NCIT_C165059")
let lfqIntensity = DefinedTerm("LFQ intensity", tan = "http://purl.obolibrary.org/obo/MS_1001902")
let arbitraryUnit = DefinedTerm("arbitrary unit")

proteinIdentifier.SemanticallyEquals(DefinedTerm("protein accession", tan = "http://purl.obolibrary.org/obo/NCIT_C165059"))
(*** include-it ***)

(**
A dataset can contain whole-file `Data` entries and fragment-level `DataContext` entries.
For CSV and TSV fragments, register the RFC 7111 selector provider before asking containment questions.
*)

let contextDemo = Dataset("datacontext-demo")
contextDemo.RegisterFragmentSelectorProvider(CsvFragmentSelectorProvider())

let resultPath = "results/proteins.tsv"
let selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri
let tabularEncoding = "text/tab-separated-values"

let resultFile = Data(resultPath, encodingFormat = tabularEncoding)
let measuredColumn = Data(resultPath, selector = "#col=3", selectorFormat = selectorFormat, encodingFormat = tabularEncoding)

let analysis = Process("analysis")
analysis.AddOutputData(measuredColumn)

contextDemo.AddDataFile(resultFile)
contextDemo.AddProcess(analysis)

contextDemo.AddDataContext(
    DataContext(
        Data(resultPath, selector = "#col=1", selectorFormat = selectorFormat, encodingFormat = tabularEncoding),
        explication = proteinIdentifier,
        objectType = DefinedTerm("String")))

contextDemo.AddDataContext(
    DataContext(
        Data(resultPath, selector = "#col=2-5", selectorFormat = selectorFormat, encodingFormat = tabularEncoding),
        explication = lfqIntensity,
        objectType = DefinedTerm("Float"),
        unit = arbitraryUnit))

(**
## Find Contexts By File Path

`Dataset.DataContextsForPath` ignores selectors and returns every context attached to a file path.
This is useful when you know which file you will read, but still need to discover which fragments carry which meaning.
*)

let contextsForFile =
    contextDemo.DataContextsForPath(resultPath)
    |> Seq.choose (fun dc -> dc.Explication |> Option.map (fun term -> term.Name))
    |> Seq.toList

contextsForFile
(*** include-it ***)

(**
Use `DataContext.ExplicationEquals`, `ObjectTypeEquals`, and `UnitEquals` when matching semantic terms.
*)

let identifierContext =
    contextDemo.DataContextsForPath(resultPath)
    |> Seq.find (fun dc -> dc.ExplicationEquals(proteinIdentifier))

let identifierColumn =
    identifierContext.Data.Selector
    |> Option.bind CsvFragmentSelectorProvider.TryGetZeroBasedColumnIndex

identifierColumn
(*** include-it ***)

(**
## Match Contexts To Data Fragments

`Dataset.DataContextsCoveringData` compares a queried `Data` node with the `Data` targets on registered data contexts.
It returns exact matches and contexts whose selector contains the queried selector.
In the example below, the process graph produced column 3, and the LFQ context covers columns 2-5.
*)

let coveringContexts =
    contextDemo.DataContextsCoveringData(measuredColumn)
    |> Seq.choose (fun dc -> dc.Explication |> Option.map (fun term -> term.Name))
    |> Seq.toList

coveringContexts
(*** include-it ***)

(**
If you want the data nodes themselves, `Dataset.DataWithDataContextByExplication` scans `AllData()` and returns pairs of process data and matching contexts.
*)

let abundanceData =
    contextDemo.DataWithDataContextByExplication(lfqIntensity)
    |> Seq.map (fun (data, _) -> data.Selector |> Option.defaultValue "")
    |> Seq.toList

abundanceData
(*** include-it ***)

(**
ARC Core stops at identifying paths, selectors, and metadata.
It does not load dataframes, compute correlations, or render plots.
After ARC Core identifies `resultPath`, `identifierColumn`, and `abundanceData`, pass those values to the table or plotting library of your choice.

## Metadata-Powered Analysis

This applied example follows the metadata-powered data analysis pattern from the fragment-level FAIRness paper:
combine process metadata with Datamap entries to find data columns of interest.
The setup below creates a small process graph and Datamap.
Column 1 contains protein identifiers, while columns 2-5 contain LFQ intensity values.
*)

(*** hide ***)
let paperSetupSource = """
let temperature = DefinedTerm("temperature", tan = "https://bioregistry.io/NCRO:0000029")
let biologicalReplicate = DefinedTerm("biological replicate group", tan = "https://bioregistry.io/DPBO:1000183")
let technicalReplicate = DefinedTerm("technical replicate group", tan = "https://bioregistry.io/DPBO:1000184")
let proteinIdentifier = DefinedTerm("protein identifier", tan = "http://purl.obolibrary.org/obo/NCIT_C165059")
let lfqIntensity = DefinedTerm("LFQ intensity", tan = "http://purl.obolibrary.org/obo/MS_1001902")

let dataset = Dataset("metadata-powered-analysis")
dataset.RegisterFragmentSelectorProvider(CsvFragmentSelectorProvider())

dataset.AddDataFile(Data("proteomics_result.tsv", encodingFormat = "text/tab-separated-values"))
dataset.AddDataContext(
    DataContext(
        Data("proteomics_result.tsv", selector = "#col=1", selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri, encodingFormat = "text/tab-separated-values"),
        explication = proteinIdentifier,
        objectType = DefinedTerm("String")))
dataset.AddDataContext(
    DataContext(
        Data("proteomics_result.tsv", selector = "#col=2-5", selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri, encodingFormat = "text/tab-separated-values"),
        explication = lfqIntensity,
        objectType = DefinedTerm("Float")))

let source = Sample("Base culture", additionalType = "Source")

let addResult condition bioRep techRep selector =
    let culture = Sample($"Culture {condition} C replicate {bioRep}", additionalType = "Sample")
    culture.AddAdditionalProperty(Annotation("temperature", value = condition, unit = "degree Celsius", nameTAN = temperature.TAN.Value, additionalType = "FactorValue"))

    let aliquot = Sample($"Aliquot {condition} C replicate {bioRep}.{techRep}", additionalType = "Sample")
    aliquot.AddAdditionalProperty(Annotation("biological replicate group", value = bioRep, nameTAN = biologicalReplicate.TAN.Value, additionalType = "CharacteristicValue"))
    aliquot.AddAdditionalProperty(Annotation("technical replicate group", value = techRep, nameTAN = technicalReplicate.TAN.Value, additionalType = "CharacteristicValue"))

    let data = Data("proteomics_result.tsv", selector = selector, selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri, encodingFormat = "text/tab-separated-values")

    let growth = Process($"Growth {condition} C {bioRep}.{techRep}")
    growth.AddInputSample(source)
    growth.AddOutputSample(culture)

    let preparation = Process($"Prepare sample {condition} C {bioRep}.{techRep}")
    preparation.AddInputSample(culture)
    preparation.AddOutputSample(aliquot)

    let analysis = Process($"Computational proteome analysis {condition} C {bioRep}.{techRep}")
    analysis.AddInputSample(aliquot)
    analysis.AddOutputData(data)

    dataset.AddProcess(growth)
    dataset.AddProcess(preparation)
    dataset.AddProcess(analysis)
    data

addResult "35" "1" "1" "#col=2" |> ignore
addResult "35" "1" "2" "#col=3" |> ignore
addResult "40" "1" "1" "#col=4" |> ignore
addResult "35" "2" "1" "#col=5" |> ignore
"""

(*** hide ***)
fsharpCodeBlock "Show example data setup" paperSetupSource
(*** include-it-raw ***)

(*** hide ***)
let temperature = DefinedTerm("temperature", tan = "https://bioregistry.io/NCRO:0000029")
let biologicalReplicate = DefinedTerm("biological replicate group", tan = "https://bioregistry.io/DPBO:1000183")
let technicalReplicate = DefinedTerm("technical replicate group", tan = "https://bioregistry.io/DPBO:1000184")

let dataset = Dataset("metadata-powered-analysis")
dataset.RegisterFragmentSelectorProvider(CsvFragmentSelectorProvider())

dataset.AddDataFile(Data("proteomics_result.tsv", encodingFormat = "text/tab-separated-values"))
dataset.AddDataContext(
    DataContext(
        Data("proteomics_result.tsv", selector = "#col=1", selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri, encodingFormat = "text/tab-separated-values"),
        explication = proteinIdentifier,
        objectType = DefinedTerm("String")))
dataset.AddDataContext(
    DataContext(
        Data("proteomics_result.tsv", selector = "#col=2-5", selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri, encodingFormat = "text/tab-separated-values"),
        explication = lfqIntensity,
        objectType = DefinedTerm("Float")))

let source = Sample("Base culture", additionalType = "Source")

let addResult condition bioRep techRep selector =
    let culture = Sample($"Culture {condition} C replicate {bioRep}", additionalType = "Sample")
    culture.AddAdditionalProperty(Annotation("temperature", value = condition, unit = "degree Celsius", nameTAN = temperature.TAN.Value, additionalType = "FactorValue"))

    let aliquot = Sample($"Aliquot {condition} C replicate {bioRep}.{techRep}", additionalType = "Sample")
    aliquot.AddAdditionalProperty(Annotation("biological replicate group", value = bioRep, nameTAN = biologicalReplicate.TAN.Value, additionalType = "CharacteristicValue"))
    aliquot.AddAdditionalProperty(Annotation("technical replicate group", value = techRep, nameTAN = technicalReplicate.TAN.Value, additionalType = "CharacteristicValue"))

    let data = Data("proteomics_result.tsv", selector = selector, selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri, encodingFormat = "text/tab-separated-values")

    let growth = Process($"Growth {condition} C {bioRep}.{techRep}")
    growth.AddInputSample(source)
    growth.AddOutputSample(culture)

    let preparation = Process($"Prepare sample {condition} C {bioRep}.{techRep}")
    preparation.AddInputSample(culture)
    preparation.AddOutputSample(aliquot)

    let analysis = Process($"Computational proteome analysis {condition} C {bioRep}.{techRep}")
    analysis.AddInputSample(aliquot)
    analysis.AddOutputData(data)

    dataset.AddProcess(growth)
    dataset.AddProcess(preparation)
    dataset.AddProcess(analysis)
    data

addResult "35" "1" "1" "#col=2" |> ignore
addResult "35" "1" "2" "#col=3" |> ignore
addResult "40" "1" "1" "#col=4" |> ignore
addResult "35" "2" "1" "#col=5" |> ignore

(**
### Select Data By Process Metadata

The selected data nodes are final data fragments whose upstream process graph contains both temperature 35 and biological replicate group 1.
*)

let hasUpstreamValue term value data =
    dataset.UpstreamAnnotationsForNode(DataNode data)
    |> Seq.exists (fun pv -> pv.NameEquals(term) && pv.Value = Some value)

let selectedData =
    dataset.FinalData()
    |> Seq.filter (fun data -> hasUpstreamValue temperature "35" data)
    |> Seq.filter (fun data -> hasUpstreamValue biologicalReplicate "1" data)
    |> Seq.toList

let selectedSelectors =
    selectedData
    |> List.map (fun data -> data.Selector.Value)

selectedSelectors
(*** include-it ***)

(**
### Resolve Datamap Selectors

Find the index column by explication, then find the LFQ intensity context that covers each selected data fragment.
*)

let indexColumn =
    dataset.DataContextsForPath("proteomics_result.tsv")
    |> Seq.find (fun dc -> dc.ExplicationEquals(proteinIdentifier))
    |> fun dc -> dc.Data.Selector
    |> Option.bind CsvFragmentSelectorProvider.TryGetZeroBasedColumnIndex

indexColumn
(*** include-it ***)

let abundanceColumns =
    selectedData
    |> Seq.collect (fun data ->
        dataset.DataContextsCoveringData(data)
        |> Seq.filter (fun dc -> dc.ExplicationEquals(lfqIntensity))
        |> Seq.map (fun _ -> data.Selector.Value))
    |> Seq.toList

abundanceColumns
(*** include-it ***)

(**
At this point, an analysis script can read `proteomics_result.tsv`, use `indexColumn` as the row index, and keep `abundanceColumns` for the correlation or heatmap workflow.
ARC Core deliberately stops at identifying and relating metadata-backed file fragments; it does not load dataframes, compute correlations, or render plots.

### Label Selected Columns

Because the selected data nodes remain connected to their process graph, plotting labels can come from upstream process metadata instead of file-internal headers.
*)

let labels =
    selectedData
    |> List.map (fun data ->
        let technicalReplicateValue =
            dataset.UpstreamAnnotationsForNode(DataNode data)
            |> Seq.find (fun pv -> pv.NameEquals(technicalReplicate))
        data.Selector.Value, technicalReplicateValue.ValueText)

labels
(*** include-it ***)

(**
## What To Use When

| Task | API |
|------|-----|
| Compare ontology-backed terms | `DefinedTerm.SemanticallyEquals` |
| Match annotations by ontology-backed name | `Annotation.NameEquals` |
| Match DataContext semantics | `DataContext.ExplicationEquals` |
| Find contexts for one file path | `Dataset.DataContextsForPath` |
| Find contexts that cover a data fragment | `Dataset.DataContextsCoveringData` |
| Find data nodes by context explication | `Dataset.DataWithDataContextByExplication` |
| Convert `#col=N` to a dataframe index | `CsvFragmentSelectorProvider.TryGetZeroBasedColumnIndex` |
*)
