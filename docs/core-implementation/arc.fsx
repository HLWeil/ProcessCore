(**
---
title: ARC Layer
category: Core Implementation
categoryindex: 3
index: 2
---

# ARC Layer

`ARC` is the top-level wrapper for an ARC Scaffold. It inherits from `Dataset`, so all of the ordinary process-graph helpers still apply, but it also adds ARC-specific file handling for YAML packages and spreadsheet scaffolds.

Use `ARC` when you want the package itself to carry administrative metadata such as title, description, license, publication dates, agents, citations, and data contexts.
*)

(*** hide ***)
#r "../../src/ProcessCore/bin/Release/netstandard2.1/ProcessCore.dll"
#r "nuget: DynamicObj"
open ProcessCore


(**


## Explicit Representation APIs

`ARC` exposes file-system entry points for the two supported on-disk representations:

- YAML packages, stored as `arc.yml` in the ARC root.
- Spreadsheet scaffolds, stored as the collection of workbook files defined by the ARC scaffold layout.

Prefer the representation-specific methods when the format is known:

- `ARC.loadYML` and `ARC.loadYMLAsync` load `arc.yml`.
- `ARC.loadXLSX` and `ARC.loadXLSXAsync` load a spreadsheet scaffold.
- `arc.WriteYML` and `arc.WriteYMLAsync` write `arc.yml`.
- `arc.WriteXLSX` and `arc.WriteXLSXAsync` write a spreadsheet scaffold.

Each explicit method records the path and representation it used. A subsequent `Update` therefore writes the same representation.
*)

let arcPath = __SOURCE_DIRECTORY__ + "/../../examples/arc/demo-arc"
let arc = ARC.loadYML arcPath

arc.AddAgent(Agent("Bruce", familyName = "Wayne"))

do arc.Update()

// Choose the output representation explicitly.
arc.WriteYML("new-yml-arc-path")
arc.WriteXLSX("new-xlsx-arc-path")

(**
The same operations are available for cross-platform async code:
*)

open CrossAsync

crossAsync {
    let! yamlArc = ARC.loadYMLAsync arcPath
    do! yamlArc.WriteYMLAsync("new-yml-arc-path")

    let! spreadsheetArc = ARC.loadXLSXAsync("spreadsheet-arc-path")
    do! spreadsheetArc.WriteXLSXAsync("new-xlsx-arc-path")
}

(**
## Convenience Layer

`ARC.load` and `ARC.loadAsync` automatically choose a representation. They prefer `arc.yml` when it exists and otherwise try the spreadsheet scaffold reader. `Write` and `WriteAsync` are convenience aliases for the YAML-specific write methods. `Update` and `UpdateAsync` use the representation recorded by the last load or write.
*)

let autoDetectedArc = ARC.load arcPath
autoDetectedArc.Write("new-arc-path")
autoDetectedArc.Update()

(**
#### Write path determination

The `ArcPath` property stores the package root that was loaded or last written. 

*)

arc.ArcPath <- Some "new-arc-path"
arc.Update()

//or

arc.Update("new-arc-path")

(**
#### Representation Rules

`IsSpreadsheetScaffold` records which on-disk representation was loaded. `Update` uses this property to decide how to save.

Use the representation flag as follows:

- `IsSpreadsheetScaffold = true` means the package was loaded from spreadsheet files and should be updated with the scaffold writer.
- `IsSpreadsheetScaffold = false` means the package should be written as YAML in `arc.yml`.

`WriteYMLAsync` (and its `WriteAsync` alias) always writes YAML. It also clears `IsSpreadsheetScaffold`, so calling it converts a scaffold-loaded package into a YAML-backed package on disk. `WriteXLSXAsync` sets the flag.
`UpdateAsync` preserves the current representation choice:
*)

arc.IsSpreadsheetScaffold <- true
arc.Update() // uses the scaffold writer

arc.IsSpreadsheetScaffold <- false
arc.Update() // uses the YAML writer

(**
#### Working With Async Methods

All three main io methods have async versions. Use `loadAsync` to load an ARC package from disk, `WriteAsync` to write the current ARC as YAML, and `UpdateAsync` to persist the current ARC back to its existing location.

In Javascript, only these async methods are available and will be transpiled to promises, so use them in all cases for cross-platform code.
*)

crossAsync {
    let! arc = ARC.loadAsync arcPath
    do! arc.UpdateAsync()
    do! arc.WriteAsync("new-arc-path")
}

(**

## YAML Serialization

`ARC.toYamlString` writes the ARC package as indexed YAML. `ARC.fromYamlString` rebuilds a new `ARC` object from that document. Unsorted samples, data files, and recipes are written to the typed `samples`, `dataFiles`, and `recipes` fields. Runtime-only properties such as `ArcPath`, representation flags, registries, and graph back-edges are not serialized.
*)


let arcYaml = arc.toYamlString(2)
let arcRoundTrip = ARC.fromYamlString arcYaml




(**
## Create An ARC object in memory

The ARC layer still starts with an identifier, but the package object can also hold top-level metadata about the collection.
*)

let leadOrganization = Organization("ARC Core Lab", id = "https://example.org/organizations/arc-core-lab")
let curator = Agent("Ada", familyName = "Lovelace", email = "ada@example.org", affiliation = leadOrganization)
let article = ScholarlyArticle("ARC Core model walkthrough", authors = [ curator ])

let arc =
    ARC(
        "demo-arc",
        title = "Demo ARC package",
        description = "A small ARC package with administrative metadata.",
        license = "CC-BY-4.0",
        datePublished = "2026-07-03",
        dateCreated = "2026-07-03",
        dateModified = "2026-07-03")

arc.AddAgent(curator)
arc.AddCitation(article)

(**
The package keeps the same graph shape as `Dataset`, with some additional file-system capabilities.

## Stage and Link Unsorted Objects

Samples and recipes that do not yet belong to a process can be staged directly on the ARC. Orphan data uses the inherited `Dataset.DataFiles` collection. Constructor arguments and the corresponding add methods both establish canonical instances.
*)

let stagedSample = Sample("sample-1")
let stagedData = Data("data/measurement.csv")
let stagedRecipe = Recipe("measure", version = "1")

let stagedArc =
    ARC(
        "staging-demo",
        samples = [ stagedSample ],
        dataFiles = [ stagedData ],
        recipes = [ stagedRecipe ])

// Equivalent incremental APIs:
stagedArc.AddSample(Sample("sample-2"))
stagedArc.AddDataFile(Data("data/second-measurement.csv"))
stagedArc.AddRecipe(Recipe("normalize", version = "1"))

(**
Equal objects are canonical across the ARC and all nested datasets. Linking a later equal value therefore reuses the first stored instance without merging fields from the later value.
*)

let measurement = Process("measurement")
stagedArc.AddProcess(measurement)
measurement.SetInputSample(Sample("sample-1"))
measurement.SetOutputData(Data("data/measurement.csv"))
measurement.ExecutesRecipe <- Some(Recipe("measure", version = "1"))

obj.ReferenceEquals(stagedSample, measurement.InputSample().Value) // true
obj.ReferenceEquals(stagedData, measurement.OutputData().Value) // true
obj.ReferenceEquals(stagedRecipe, measurement.ExecutesRecipe.Value) // true

(**
Store membership is explicit: linking does not remove staged values, and removing a staged value does not detach it from a process. Use `RemoveSample`, `RemoveDataFile`, or `RemoveRecipe` when the ARC should stop storing an object.
*)

stagedArc.RemoveSample(stagedSample)
stagedArc.RemoveDataFile(stagedData)
stagedArc.RemoveRecipe(stagedRecipe)

// The process still owns all three links.
measurement.InputSample().IsSome,
measurement.OutputData().IsSome,
measurement.ExecutesRecipe.IsSome

(**
YAML string and file round-trips preserve staged values as their concrete types. When a staged value is also linked to a process, both locations resolve to the same reference after decoding. Unknown YAML overflow properties also continue to round-trip.
*)

let stagedYaml = stagedArc.toYamlString(2)
let decodedStagingArc = ARC.fromYamlString stagedYaml
*)

(**
## What To Use When

| Task | API |
|------|-----|
| Create an ARC | `ARC(identifier)` |
| Load YAML | `ARC.loadYML`, `ARC.loadYMLAsync` |
| Load a spreadsheet scaffold | `ARC.loadXLSX`, `ARC.loadXLSXAsync` |
| Save as YAML | `arc.WriteYML`, `arc.WriteYMLAsync` |
| Save as a spreadsheet scaffold | `arc.WriteXLSX`, `arc.WriteXLSXAsync` |
| Auto-detect and load | `ARC.load`, `ARC.loadAsync` |
| Save as YAML (convenience) | `arc.Write`, `arc.WriteAsync` |
| Refresh in place on disk | `arc.Update`, `arc.UpdateAsync` |
| Add package metadata | `arc.Title`, `arc.Description`, `arc.License`, `arc.DatePublished`, `arc.DateCreated`, `arc.DateModified` |
| Record package contributors | `arc.AddAgent`, `arc.AddCitation` |
| Serialize ARC YAML | `arc.toYamlString`, `ARC.fromYamlString` |
*)
