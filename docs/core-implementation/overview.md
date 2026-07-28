---
title: ARC Core User Guide
category: Core Implementation
categoryindex: 3
index: 1
---

# ARC Core User Guide

`ProcessCore` is the in-memory F# library for building, decorating, reading, querying, and editing ARC Core process graphs and ARC package metadata.

The ARC Core model is intentionally compact:

- `ARC` is the top-level package wrapper around a dataset graph and its administrative metadata.
- A `Dataset` groups process graphs, nested datasets, data files, data contexts, and administrative information.
- A `Process` connects `Sample` and `Data` input/output nodes.
- A `Recipe` describes what the process executes.
- An `Annotation` annotates datasets, process parameters, input/output nodes, and protocol components.
- A `DataContext` describes data files and selected data fragments with additional structural and contextual information.
- `Agent`, `Organization`, and `ScholarlyArticle` carry administrative profile metadata, while `ARC` adds package-level persistence, project-configured storage, and metadata handling.
- `additionalType`, `additionalProperty`, and `DynamicObj` carry domain-specific extensions without changing the shared graph shape.

The pages in this section are user-facing implementation guides.

For normative field definitions, use the [specification index](../spec/index.md). These project pages focus on using the F# library.

For API reference, see the [API docs](../reference/index.html).

## Loading the library

The `ProcessCore` library is available as a NuGet package for .NET and currently a NuGet package for JavaScript transpilation.

#### .NET
<a href="https://www.nuget.org/packages/ProcessCore"><img alt="Nuget" src="https://img.shields.io/nuget/v/ProcessCore?logo=nuget&color=%234fb3d9&label=ProcessCore"></a>

```
dotnet add package ProcessCore
```

```fsharp
#r "nuget: ProcessCore" // in scripts

open ProcessCore

let d = Dataset("MyDataset")
```

#### JavaScript

<a href="https://www.nuget.org/packages/ProcessCore.Javascript"><img alt="Nuget" src="https://img.shields.io/nuget/v/ProcessCore.Javascript?logo=nuget&color=%234fb3d9&label=ProcessCore.Javascript"></a>

```
dotnet add package ProcessCore.Javascript
```

```fsharp
#r "nuget: ProcessCore.Javascript" // in scripts

open ProcessCore

let d = Dataset("MyDataset")
```

## Recommended Path

1. [ARC Layer](arc.fsx) shows package-level metadata, explicit YAML/XLSX persistence, and `.arc/project.yml`-configured workspaces.
2. [Creating A Dataset](creating-datasets.fsx) builds a graph from F# objects.
3. [Decorations](decorations.fsx) shows how to add domain specificity through typed annotations and `DynamicObj` overflow fields.
4. [Reading And Writing YAML](yaml-parsing.fsx) loads profile-shaped examples and writes inline or indexed YAML.
5. [Querying Process Graphs](querying.fsx) traverses upstream, downstream, and connected context.
6. [Fragment Selector Providers](fragment-selector-providers.fsx) makes file fragments first-class in traversal.
7. [Using DataContext](data-contexts.fsx) describes Datamap entries and shows how to combine them with process annotations.
8. [Tabular Views](tables.fsx) edits process graphs through ISA-like table projections.
9. [Graph Identity, Back-Edges, And Scope](graph-invariants.md) explains the invariants behind shared nodes and scoped traversal.



## What To Use When

| Task | Start With |
|------|------------|
| Create or persist an ARC package | `ARC`, `ArcPath`, `Write`, `Update`, `ARC.fromYamlString` |
| Use a project-configured workspace | `.arc/project.yml`, `ARC.load`, `arc.Write`, `arc.Update` |
| Add application-specific project codecs | `DatasetCodec`, `CodecRegistry`, `ARC.loadProject`, `arc.WriteProject` |
| Build a process graph in code | `Dataset`, `Process`, `Sample`, `Data` |
| Add domain-specific meaning | `AdditionalType`, `AddAdditionalProperty`, `AddParameterValue`, `DynamicObj.SetProperty` |
| Load ARC/profile-shaped YAML | `ProcessCore.Yaml.Dataset.fromYamlString false` |
| Validate stricter core-shaped YAML | `ProcessCore.Yaml.Dataset.fromYamlString true` |
| Ask provenance questions | `AllProcesses`, `UpstreamNodes`, `DownstreamAnnotations`, `PathsThrough` |
| Work with file fragments | `Data.Selector`, `Data.SelectorFormat`, `RegisterFragmentSelectorProvider` |
| Use Datamap context in queries | `DataContextsForPath`, `DataContextsCoveringData`, `ExplicationEquals` |
| Edit as rows and columns | `dataset.Tables` from `ProcessCore.Table` |
| Understand surprising traversal behavior | Node canonicalization, back-edges, and explicit process scopes |


