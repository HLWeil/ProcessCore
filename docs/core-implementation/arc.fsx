(**
---
title: ARC Layer
category: Core Implementation
categoryindex: 3
index: 2
---

# ARC Layer

`ARC` is the top-level wrapper for an ARC Scaffold. It inherits from `Dataset`, so all of the ordinary process-graph helpers still apply, but it also adds ARC-specific file handling through `Write`, `Update`, and `load`.

Use `ARC` when you want the package itself to carry administrative metadata such as title, description, license, publication dates, agents, citations, and data contexts.
*)

(*** hide ***)
#r "../../src/ProcessCore/bin/Release/netstandard2.1/ProcessCore.dll"
#r "nuget: DynamicObj"
open ProcessCore


(**


## Load, Write and Serialize

`ARC` exposes file-system entry points for the two supported on-disk representations:

- YAML packages, stored as `arc.yml` in the ARC root.
- Spreadsheet scaffolds, stored as the collection of workbook files defined by the ARC scaffold layout.

The async methods are:

- `ARC.load` to load an ARC package from disk.
- `arc.Write` to write the current ARC as YAML.
- `arc.Update` to persist the current ARC back to its existing location.

`load` prefers `arc.yml` when it exists. If the file is missing, it falls back to the spreadsheet scaffold reader.
*)

let arc = ARC.load (__SOURCE_DIRECTORY__ + "/../../examples/arc/demo-arc")

arc.AddAgent(Agent("Bruce", familyName = "Wayne"))

do arc.Update()

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

`WriteAsync` always writes YAML. It also clears `IsSpreadsheetScaffold`, so calling it converts a scaffold-loaded package into a YAML-backed package on disk.
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

open CrossAsync

crossAsync {
    let! arc = ARC.loadAsync (__SOURCE_DIRECTORY__ + "/../../examples/arc/demo-arc")
    do! arc.UpdateAsync()
    do! arc.WriteAsync("new-arc-path")
}

(**

## YAML Serialization

`ARC.toYamlString` writes the ARC package as indexed YAML. `ARC.fromYamlString` rebuilds a new `ARC` object from that document.
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
The package keeps the same graph shape as `Dataset`, with some addtional file system capabilities.
*)

(**
## What To Use When

| Task | API |
|------|-----|
| Create an ARC | `ARC(identifier)` |
| Load an ARC from disk | `ARC.load`, `ARC.loadAsync` |
| Save as YAML on disk | `arc.Write`, `arc.WriteAsync` |
| Refresh in place on disk | `arc.Update`, `arc.UpdateAsync` |
| Add package metadata | `arc.Title`, `arc.Description`, `arc.License`, `arc.DatePublished`, `arc.DateCreated`, `arc.DateModified` |
| Record package contributors | `arc.AddAgent`, `arc.AddCitation` |
| Serialize ARC YAML | `arc.toYamlString`, `ARC.fromYamlString` |
*)