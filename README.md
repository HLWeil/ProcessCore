# ProcessCore

ProcessCore is the F# implementation package for the ARC process data model. It provides a mutable in-memory model for process graphs, YAML codecs, SQLite profile helpers, graph traversal queries, and live table views over process data.

Documentation: https://hlweil.github.io/ProcessCore/


## Repository Docs

- [Project overview](docs/project/overview.md)
- [Normative specification](docs/spec/index.md)
- [Specification guide](docs/project/specification.md)
- [Implementation guide](docs/project/implementation.md)
- [Examples and schemas](docs/project/examples-and-schemas.md)
- [Reference material](docs/project/references.md)
- [Prior art notes](docs/project/prior-art.md)

## Install

```powershell
dotnet add package ProcessCore
```

```fsharp
#r "nuget: ProcessCore"
```

## What You Get

- Core ARC process graph types: `Dataset`, `LabProcess`, `LabProtocol`, `Material`, `Data`, `PropertyValue`, `FormalParameter`, and `DefinedTerm`.
- Query helpers for connected, upstream, downstream, and path-based traversal.
- Fragment-aware `Data` nodes with `path`, `selector`, `selectorFormat`, and pluggable selector providers.
- YAML encode/decode support for the core model, including strict and lenient type handling.
- SQLite profile row types, codecs, repository CRUD facades, and .NET SQLite driver support.
- A live tabular projection layer for reading and editing process graphs as tables.
- Fable-oriented project variants for JavaScript and Python output.

## Quick Start

```fsharp
open ProcessCore

let source = Material("Base Culture", additionalType = "Source")
let sample = Material("Cultivation Flask RT", additionalType = "Sample")

let growth =
    LabProcess(
        "Growth",
        parameterValue =
            [ PropertyValue("temperature", value = "25", unit = "degree Celsius") ]
    )

growth.AddInputMaterial(source)
growth.AddOutputMaterial(sample)

let assay = Dataset("measurement1", name = "Proteomics assay")
assay.AddProcess(growth)

let roots = assay.RootMaterials()
let finals = assay.FinalMaterials()
```

## YAML

```fsharp
let yaml =
    ProcessCore.Yaml.Dataset.toYamlString (Some 2) assay

let decoded =
    ProcessCore.Yaml.Dataset.fromYamlString false yaml
```

Use `processCoreOnly = true` for strict core type checks, or `false` when reading decorated/profile-shaped ARC YAML that may contain extension fields.

## SQLite Profile

The package includes SQL row types, row codecs, repository CRUD helpers, and a .NET SQLite driver:

```fsharp
open ProcessCore.SQL
open ProcessCore.SQL.DotNet

use driver = Sqlite.openInMemory()

DefinedTerm.insert(
    driver,
    DefinedTermRow("term:cell-growth", "DefinedTerm", "cell growth")
)

let terms = DefinedTerm.list(driver)
```

The executable SQLite schema and seed data live in `schemas/sql/` in the repository.

## Repository Build

```powershell
.\build.cmd BuildSolution
.\build.cmd RunTests
.\build.cmd BuildDocs
```

`RunTests` executes the shared Pyxpecto suite across the configured .NET, Python, and JavaScript paths.

## Status

This is an early implementation release. TypeScript and Python packaging and some aspects like administrative metadata and datamap are still under development. Also note that the core model is still evolving, so expect some breaking changes in future releases as we iterate on the design and implementation.
