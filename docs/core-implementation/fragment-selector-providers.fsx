(**
---
title: Fragment Selector Providers
category: Core Implementation
categoryindex: 3
index: 7
---

# Fragment Selector Providers

`Data.Path` identifies a file. `Data.Selector` can identify a fragment inside that file. `Data.SelectorFormat` tells ProcessCore which selector language to use when comparing two fragments.
*)

(*** hide ***)
#r "../../src/ProcessCore/bin/Release/netstandard2.1/ProcessCore.dll"
#r "nuget: DynamicObj"
open ProcessCore

(**

### CSV Fragment Selector

The built-in CSV provider understands [RFC 7111](https://tools.ietf.org/html/rfc7111) row, column, and cell selectors.

With the provider, we can read and write textual selectors into their typed representation.
*)

let provider = CsvFragmentSelectorProvider()

let columnSelector = provider.TryParse "col=1-3"

(*** include-value: columnSelector ***)

let cellSelector = provider.TryParse "cell=2,2"

(*** include-value: cellSelector ***)

(**

The provider can also relate two selectors. The relation can be either `Exact`, `Contains`, or `Disjunct`.
In this case, `col=1-3` contains `cell=2,2`.

*)



// same selector is exact match
provider.Relate (columnSelector.Value) (columnSelector.Value)
(*** include-it ***)

// column contains cell
provider.Relate (columnSelector.Value) (cellSelector.Value)
(*** include-it ***)

// disjoint columns
provider.Relate (columnSelector.Value) ((provider.TryParse "col=4-6").Value)
(*** include-it ***)

(**
Providers matter when both data nodes have selectors. Without a registered provider, ProcessCore can see that the paths match but cannot know whether `col=1-3` contains `cell=2,2`.
*)

let dataset = Dataset("fragment-demo")

let exportedColumns = Data(path = "measurements.csv", selector = "col=1-3", selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri, encodingFormat = "text/csv")
let measuredCell = Data(path = "measurements.csv", selector = "cell=2,2", selectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri, encodingFormat = "text/csv")
let interpretedSample = Sample("Interpreted sample", additionalType = "Sample")

let export = Process("Export CSV")
export.SetOutputData(exportedColumns)

let interpret = Process("Interpret selected cell")
interpret.SetInputData(measuredCell)
interpret.SetOutputSample(interpretedSample)

dataset.AddProcess(export)
dataset.AddProcess(interpret)

let beforeRegistration =
    exportedColumns.DownstreamSamples(scope = dataset.AllProcesses())
    |> Seq.map (fun m -> m.Name)

beforeRegistration
(*** include-it ***)

(**
Register the provider on the dataset. Registration is stored on the root dataset, so child datasets share the same selector-provider lookup.
*)

dataset.RegisterFragmentSelectorProvider(provider)
// dataset.RegisterFragmentSelectorProvider(provider :> IFragmentSelectorProvider) // also works when upcast to interface

let registeredProvider =
    dataset.TryGetFragmentSelectorProvider(CsvFragmentSelectorProvider.SelectorFormatUri)
    |> Option.map (fun p -> p.SelectorFormat)

registeredProvider
(*** include-it ***)

let afterRegistration =
    exportedColumns.DownstreamSamples(scope = dataset.AllProcesses())
    |> Seq.map (fun m -> m.Name)

afterRegistration
(*** include-it ***)

(**
### Custom Fragment Selector Providers

The idea behind the inclusion of generic fragment selectors syntax into the ProcessCore is so that any kind of fragment can be defined given a proper fragment selector specification.
In the datamodel, this corresponds to an implementation of the `IFragmentSelectorProvider` interface, which can be registered on a dataset and will be used to relate any two selectors with the same `SelectorFormat`.

Usually, you should inherit from `FragmentSelectorProviderBase<'Selector>`, which implements `IFragmentSelectorProvider` and requires parsers and typed comparers.
The provider parses strings into a typed selector and returns a semantic relation.
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
