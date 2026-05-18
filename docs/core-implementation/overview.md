---
title: Overview
category: Core Implementation
categoryindex: 3
index: 1
---

# ProcessCore User Guide

ProcessCore is the in-memory F# library for building, decorating, reading, querying, and editing ARC process graphs.

The core model is intentionally small:

- A `Dataset` groups process graphs and nested datasets.
- A `LabProcess` connects `Material` and `Data` input/output nodes.
- A `LabProtocol` describes what the process executes.
- A `PropertyValue` annotates datasets, process parameters, input/output nodes, and protocol components.
- `additionalType`, `additionalProperty`, and `DynamicObj` carry domain-specific extensions without changing the shared graph shape.

The pages in this section are user-facing implementation guides. 

For normative field definitions, use the [core specification](../spec/core/overview.md). These project pages focus on using the F# library.

For API reference, see the [API docs](../reference/index.html).

## Recommended Path

1. [Creating A Dataset](creating-datasets.fsx) builds a graph from F# objects.
2. [Decorations](decorations.fsx) shows how to add domain specificity through typed annotations and `DynamicObj` overflow fields.
3. [Reading And Writing YAML](yaml-parsing.fsx) loads profile-shaped examples and writes inline or indexed YAML.
4. [Querying Process Graphs](querying.fsx) traverses upstream, downstream, and connected context.
5. [Fragment Selector Providers](fragment-selector-providers.fsx) makes file fragments first-class in traversal.
6. [Tabular Views](tables.fsx) edits process graphs through ISA-like table projections.
7. [Graph Identity, Back-Edges, And Scope](graph-invariants.md) explains the invariants behind shared nodes and scoped traversal.



## What To Use When

| Task | Start With |
|------|------------|
| Build a process graph in code | `Dataset`, `LabProcess`, `Material`, `Data` |
| Add domain-specific meaning | `AdditionalType`, `AddAdditionalProperty`, `AddParameterValue`, `DynamicObj.SetProperty` |
| Load ARC/profile-shaped YAML | `ProcessCore.Yaml.Dataset.fromYamlString false` |
| Validate stricter core-shaped YAML | `ProcessCore.Yaml.Dataset.fromYamlString true` |
| Ask provenance questions | `AllProcesses`, `UpstreamNodes`, `DownstreamPropertyValues`, `PathsThrough` |
| Work with file fragments | `Data.Selector`, `Data.SelectorFormat`, `RegisterFragmentSelectorProvider` |
| Edit as rows and columns | `dataset.Tables` from `ProcessCore.Table` |
| Understand surprising traversal behavior | Node canonicalization, back-edges, and explicit process scopes |
