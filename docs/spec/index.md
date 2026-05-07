---
title: Specification
category: Specification
categoryindex: 3
index: 1
---

# Specification

The ARC Data Model specification defines ProcessCore and a set of decoration profiles that map domain-specific ARC concepts onto the shared process graph.

## Reading Order

1. [ProcessCore](core/overview.md)
2. [Decorations](decorations/overview.md)
3. [Querying](querying/use-cases.md)

## Principles

- Process-centric: experiments and workflows are modeled as processes connecting inputs to outputs.
- Extensible: `PropertyValue` and `additionalType` carry domain-specific information without changing core entities.
- Representation-aware but model-first: SQL and YAML schemas derive from the markdown spec.

## Main Areas

| Area | Description |
|------|-------------|
| [ProcessCore](core/overview.md) | Foundational model: Dataset, LabProcess, LabProtocol, Material, Data, PropertyValue, FormalParameter, and DefinedTerm |
| [ISA Decoration](decorations/isa/overview.md) | Investigation/Study/Assay and ISA-specific roles |
| [Workflow Run Decoration](decorations/workflow-run/overview.md) | Workflow and Run datasets, workflow protocols, and workflow invocations |
| [Datamap Decoration](decorations/datamap/overview.md) | Datamap datasets and DataContext fragment annotations |
| [Querying](querying/use-cases.md) | Query use cases and graph traversal notes |
