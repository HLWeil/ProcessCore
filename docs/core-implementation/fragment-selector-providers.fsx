(**
---
title: Fragment Selector Providers
category: Core Implementation
categoryindex: 3
index: 5
---

# Fragment Selector Providers

`Data.Path` identifies a file. `Data.Selector` can identify a fragment inside that file. `Data.SelectorFormat` tells ProcessCore which selector language to use when comparing two fragments.
*)

(*** hide ***)
#r "../../src/ProcessCore/bin/Release/netstandard2.0/ProcessCore.dll"
#r "nuget: DynamicObj"
open ProcessCore

let csvSelectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri

let data path selector =
    let d = Data(path)
    d.Selector <- Some selector
    d.SelectorFormat <- Some csvSelectorFormat
    d.EncodingFormat <- Some "text/csv"
    d

let materialNames (materials: seq<Material>) =
    materials |> Seq.map (fun m -> m.Name) |> Seq.toList

(**
The built-in CSV provider understands RFC 7111 row, column, and cell selectors.
*)

let provider = CsvFragmentSelectorProvider()
let resolver = provider :> IFragmentSelectorProvider


let selectorRelations =
    [ "same selector", provider.Relate (ColumnSelector [{ First = Index 1; Last = Index 3 }]) (ColumnSelector [{ First = Index 1; Last = Index 3 }])
      "column contains cell", resolver.TryRelate "col=1-3" "cell=2,2" |> Option.defaultValue Unknown
      "disjoint columns", resolver.TryRelate "col=1" "col=4" |> Option.defaultValue Unknown
      "overlap without containment", resolver.TryRelate "col=1-3" "col=3-5" |> Option.defaultValue Unknown ]

selectorRelations
(*** include-it ***)

(**
Providers matter when both data nodes have selectors. Without a registered provider, ProcessCore can see that the paths match but cannot know whether `col=1-3` contains `cell=2,2`.
*)

let dataset = Dataset("fragment-demo")

let exportedColumns = data "measurements.csv" "col=1-3"
let measuredCell = data "measurements.csv" "cell=2,2"
let interpretedSample = Material("Interpreted sample", additionalType = "Sample")

let export = LabProcess("Export CSV")
export.AddOutputData(exportedColumns)

let interpret = LabProcess("Interpret selected cell")
interpret.AddInputData(measuredCell)
interpret.AddOutputMaterial(interpretedSample)

dataset.AddProcess(export)
dataset.AddProcess(interpret)

let beforeRegistration =
    exportedColumns.DownstreamMaterials(scope = dataset.AllProcesses())
    |> materialNames

beforeRegistration
(*** include-it ***)

(**
Register the provider on the dataset. Registration is stored on the root dataset, so child datasets share the same selector-provider lookup.
*)

dataset.RegisterFragmentSelectorProvider(provider)

let registeredProvider =
    dataset.TryGetFragmentSelectorProvider(csvSelectorFormat)
    |> Option.map (fun p -> p.SelectorFormat)

registeredProvider
(*** include-it ***)

let afterRegistration =
    exportedColumns.DownstreamMaterials(scope = dataset.AllProcesses())
    |> materialNames

afterRegistration
(*** include-it ***)

(**
Custom selector languages implement `FragmentSelectorProviderBase<'Selector>`. The provider parses strings into a typed selector and returns a semantic relation.
*)

type PrefixSelectorProvider() =
    inherit FragmentSelectorProviderBase<string>()

    override _.SelectorFormat = "urn:example:prefix-selector"

    override _.TryParse(text: string) =
        if System.String.IsNullOrWhiteSpace text then None
        else Some (text.Trim('/'))

    override _.ToSelectorString(selector: string) =
        selector

    override _.Relate(container: string) (candidate: string) =
        if container = candidate then Exact
        elif candidate.StartsWith(container + "/") then Contains
        else Unknown

let customProviderResult =
    let p = PrefixSelectorProvider()
    (p :> IFragmentSelectorProvider).TryRelate "assay/table" "assay/table/row/1"

customProviderResult
(*** include-it ***)

(**
## What To Use When

| Task | API |
|------|-----|
| Mark a file fragment | `Data.Selector`, `Data.SelectorFormat` |
| Use CSV row/column/cell fragments | `CsvFragmentSelectorProvider` |
| Enable selector-aware traversal | `dataset.RegisterFragmentSelectorProvider(provider)` |
| Inspect registered providers | `TryGetFragmentSelectorProvider`, `GetFragmentSelectorProviders` |
| Add a selector language | `FragmentSelectorProviderBase<'Selector>` |
*)
