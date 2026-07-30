(**
---
title: ARC Layer
category: Core Implementation
categoryindex: 3
index: 2
---

# ARC Layer

`ARC` is the top-level wrapper for an ARC workspace. It inherits from `Dataset`,
so all of the ordinary process-graph helpers still apply, but it also adds
ARC-specific file handling for YAML packages, spreadsheet scaffolds, and
project-configured workspaces.

Use `ARC` when you want the package itself to carry administrative metadata such as title, description, license, publication dates, agents, citations, and data contexts.
*)

(*** hide ***)
#r "../../src/ProcessCore/bin/Release/netstandard2.1/ProcessCore.dll"
#r "nuget: DynamicObj"
open ProcessCore


(**


## Explicit Representation APIs

`ARC` exposes explicit file-system entry points for the two built-in on-disk
representations:

- YAML packages, stored as `arc.yml` in the ARC root.
- Spreadsheet scaffolds, stored as the collection of workbook files defined by the ARC scaffold layout.

Prefer the representation-specific methods when the format is known or when
project discovery must be bypassed:

- `ARC.loadYML` and `ARC.loadYMLAsync` load `arc.yml`.
- `ARC.loadXLSX` and `ARC.loadXLSXAsync` load a spreadsheet scaffold.
- `arc.WriteYML` and `arc.WriteYMLAsync` write `arc.yml`.
- `arc.WriteXLSX` and `arc.WriteXLSXAsync` write a spreadsheet scaffold.

These methods do not inspect `.arc/project.yml`. Each explicit method records
the path and representation it used. A subsequent `Update` writes the same
representation unless the selected destination now contains an authoritative
project file.
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
## Project-Configured Workspaces

The generic `ARC.load` and `ARC.loadAsync` methods recognize a project document
at the exact path `<workspace-root>/.arc/project.yml`. When it exists, the
project defines which files represent the root ARC and its direct child
Datasets:

```text
workspace/
|-- .arc/
|   |-- project.yml
|   `-- profiles/
|       `-- isa-xlsx.yml
|-- isa.investigation.xlsx
|-- studies/
`-- assays/
```

The project is authoritative. An invalid project, unavailable profile, unknown
codec, missing primary file, or ambiguous rule is reported as a project error;
generic loading does not silently fall back to `arc.yml` or scaffold discovery.
Use `ARC.loadYML[Async]` or `ARC.loadXLSX[Async]` when that explicit bypass is
intended.

When no project file exists, the existing discovery remains in place:
`ARC.load[Async]` loads `arc.yml` when present and otherwise tries the
spreadsheet scaffold reader.

### Project and profile documents

A project can contain rules directly, reference reusable profiles, or do both.
Local profile paths are relative to `.arc`; URL profiles are absolute HTTP(S)
URLs and are downloaded when the project is resolved.

```yaml
# .arc/project.yml
type: ArcWorkspaceProject

rules:
  - id: arc
    codec: dataset.yml
    target: root
    path: arc.yml
```

This is the basic project-backed layout: the whole ARC is one recursive
Dataset YAML document. ISA is an optional decoration, not the default ARC
shape. An ISA-XLSX decoration profile can be referenced when that layout is
wanted:

```yaml
workspaceProfiles:
  - url: "https://example.org/arc/isa-xlsx-scaffold.yml"
```

The URL is illustrative and has no canonical status. A repository-local
profile can instead use `file: profiles/isa-xlsx.yml`.

A project-local rule with the same target can replace a profile rule. For
example, this changes only the profile's root rule and retains its other rules:

```yaml
type: ArcWorkspaceProject

workspaceProfiles:
  - url: "https://raw.githubusercontent.com/HLWeil/ProcessCore/refs/heads/main/examples/isa_xlsx_workspace_profile.yml"

rules:
  - id: yaml-root
    codec: dataset.yml
    target: root
    path: hello.yml
```

Root matches root; identifier and additional-type targets match exact,
case-sensitive values of the same target kind. Replacement applies to the whole
rule. IDs need not match, and codec, path, and files are not inherited or
merged.

The referenced document must be an `ArcWorkspaceProfile`:

```yaml
# .arc/profiles/isa-xlsx.yml
type: ArcWorkspaceProfile
id: arc.isa.xlsx.scaffold
version: "1.0"

rules:
  - id: investigation
    codec: isa.investigation.xlsx
    target: root
    path: isa.investigation.xlsx

  - id: assay
    codec: isa.assay.xlsx
    target:
      additionalType: Assay
    path: "assays/{dataset.identifier}/isa.assay.xlsx"
    files:
      - id: datamap
        path: isa.datamap.xlsx
      - id: dataset-placeholder
        path: dataset/.gitkeep
        create: empty
```

Profiles contribute their rules in reference order. Before validation, a local
rule removes every profile-contributed rule with the same target, then local
rules are appended. Unrelated profile rules remain. The effective rules must
contain exactly one `root` rule. Profile IDs and qualified rule IDs must be
unique; profile declaration order never chooses a winner for conflicting
targets or paths.

The complete optional ISA Study, Assay, Workflow, and Run layouts are shown in
the [profile examples](../spec/project_file.md#12-profile-examples).
See the [project-file specification](../spec/project_file.md) for the complete
document grammar and validation requirements.

### Rules, targets, and files

Each storage rule connects one complete Dataset to one exact codec and primary
file:

| Field | Meaning |
|---|---|
| `id` | Logical rule name used for qualification and diagnostics |
| `codec` | Exact ID from the active `CodecRegistry` |
| `target: root` | The top-level ARC; exactly one rule must select it |
| `target.identifier` | One direct child with that exact identifier |
| `target.additionalType` | Direct children with that `AdditionalType` |
| `path` | Safe path relative to the workspace root |
| `files` | Optional named files relative to the primary file's directory |

`{dataset.identifier}` may occur once in a path segment, with optional literal
text before or after it (for example, `assay_{dataset.identifier}.yml`). On read
it captures the Dataset identifier; on write it renders the selected Dataset
identifier. An exact `identifier` rule reserves that child and takes precedence
over an `additionalType` rule, which makes local relocation rules possible
without editing a reusable profile.

An auxiliary file without `create` is codec-managed. It is optional on read and
is written only when the codec returns content under its declared logical ID.
`create: empty` instead asks the project layer to create an unconditional
zero-byte file. All primary and auxiliary paths are checked for confinement and
collisions before codec execution.

The built-in registry is `CodecRegistry.standard` and contains:

```text
isa.investigation.xlsx
isa.study.xlsx
isa.assay.xlsx
isa.workflow.xlsx
isa.run.xlsx
dataset.yml
```

The registry name means that these codecs ship with the library. It does not
make either ISA scaffold an ARC default. A root `dataset.yml` rule writing
`arc.yml` is the simplest profile.

The Study, Assay, Workflow, and Run codecs understand a declared auxiliary file
with ID `datamap`. A missing Datamap is valid; one is emitted only when the
Dataset contains data contexts.

`dataset.yml` uses the lenient Dataset YAML parser. An optional ISA-decorated
YAML scaffold can use it for all five rules with paths such as
`isa.investigation.yml` and
`assays/{dataset.identifier}/isa.assay.yml`. Data contexts live directly in
each Dataset document, so these rules do not declare a Datamap auxiliary file.

When writing a split scaffold, direct children with prepared child bindings are
omitted from the root YAML and written completely in their child YAML files.
Unselected direct children and deeper nested Datasets remain inline. On read,
an external child replaces an inline root child with the same identifier; no
fields are merged.

### Loading, writing, and updating

The ordinary ARC facade uses `CodecRegistry.standard`:

```fsharp
let workspaceRoot = "path/to/workspace"

// Resolves .arc/project.yml when present.
let projectArc = ARC.load workspaceRoot

// Uses a project at the destination when present; otherwise writes arc.yml.
projectArc.Write("path/to/destination")

// Re-resolves the project at ArcPath on every update.
projectArc.Update()
```

`Write[Async]` does not create a project document. It uses one already present
at the destination; without one it writes `arc.yml`. `Update[Async]` selects the
explicit destination when supplied, otherwise `ArcPath`, and re-resolves that
destination's project and URL profiles on every call. Project handling never
creates, rewrites, or deletes `.arc/project.yml` or referenced profile
documents, and it does not remove stale codec outputs.

The `ArcPath` property stores the package root that was loaded or last written:
*)

let autoDetectedArc = ARC.load arcPath
autoDetectedArc.Write("new-arc-path")
autoDetectedArc.Update()

arc.ArcPath <- Some "new-arc-path"
arc.Update()

// Or select the update destination directly.
arc.Update("new-arc-path")

(**
For cross-platform code use the asynchronous methods. JavaScript exposes only
these methods, where they transpile to promises:
*)

crossAsync {
    let! arc = ARC.loadAsync arcPath
    do! arc.UpdateAsync()
    do! arc.WriteAsync("new-arc-path")
}

(**
`IsSpreadsheetScaffold` records the fallback representation used when no
project is present. `WriteYML[Async]` clears the flag and `WriteXLSX[Async]`
sets it. `Update[Async]` checks for a destination project first; only without a
project does the flag choose between the explicit YAML and scaffold writers.

### Custom codecs and structured errors

The generic `ARC.load[Async]`, `Write[Async]`, and `Update[Async]` methods
deliberately use only the standard registry. Use the explicit
`ARC.loadProject[Async]` and `arc.WriteProject[Async]` methods when a project
names application-specific codecs. Registries are immutable, and
`CodecRegistry.add` rejects invalid or duplicate codec IDs:

```fsharp
let registry =
    match CodecRegistry.add customCodec CodecRegistry.standard with
    | Ok registry -> registry
    | Error error -> failwith error.Message

crossAsync {
    match! ARC.loadProjectAsync(registry, workspaceRoot) with
    | Ok arc ->
        // Work with the project-backed ARC.
        return! arc.WriteProjectAsync(registry, workspaceRoot)
    | Error error ->
        printfn "Project %A error: %s" error.Kind error.Message
        return Error error
}
```

A `DatasetCodec` reads and writes a complete Dataset from a `CodecInput` or
`CodecOutput`: `Primary` contains the rule's anchor bytes and `Files` is the
declared auxiliary-resource map keyed by logical ID. The codec receives a
`CodecContext` with the relative anchor, Dataset factory, and
`ExternalChildIdentifiers`, so it never needs to derive companion filesystem
paths and can omit direct children stored by other prepared bindings.

`ARC.loadProject[Async]` and `arc.WriteProject[Async]` require an exact project
file and return `Result<_, ProjectError>` without representation fallback. The
generic convenience methods raise `ProjectException` for the same structured
error. `ProjectError` identifies the error kind and may include the rule ID,
codec ID, anchor or URL, and underlying cause.
*)

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

let metadataArc =
    ARC(
        "demo-arc",
        title = "Demo ARC package",
        description = "A small ARC package with administrative metadata.",
        license = "CC-BY-4.0",
        datePublished = "2026-07-03",
        dateCreated = "2026-07-03",
        dateModified = "2026-07-03")

metadataArc.AddAgent(curator)
metadataArc.AddCitation(article)

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

(**
## What To Use When

| Task | API |
|------|-----|
| Create an ARC | `ARC(identifier)` |
| Load using `.arc/project.yml` when present | `ARC.load`, `ARC.loadAsync` |
| Load a project with custom codecs | `ARC.loadProject`, `ARC.loadProjectAsync` |
| Write using a destination project | `arc.Write`, `arc.WriteAsync`, `arc.WriteProject`, `arc.WriteProjectAsync` |
| Use the built-in ISA workbook or Dataset-YAML codecs | `CodecRegistry.standard` |
| Extend project handling with a codec | `DatasetCodec`, `CodecRegistry.add` |
| Load YAML | `ARC.loadYML`, `ARC.loadYMLAsync` |
| Load a spreadsheet scaffold | `ARC.loadXLSX`, `ARC.loadXLSXAsync` |
| Save as YAML | `arc.WriteYML`, `arc.WriteYMLAsync` |
| Save as a spreadsheet scaffold | `arc.WriteXLSX`, `arc.WriteXLSXAsync` |
| Refresh using the destination project or recorded representation | `arc.Update`, `arc.UpdateAsync` |
| Add package metadata | `arc.Title`, `arc.Description`, `arc.License`, `arc.DatePublished`, `arc.DateCreated`, `arc.DateModified` |
| Record package contributors | `arc.AddAgent`, `arc.AddCitation` |
| Serialize ARC YAML | `arc.toYamlString`, `ARC.fromYamlString` |
*)
