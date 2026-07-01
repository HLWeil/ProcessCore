---
title: Profiles
category: Specification
categoryindex: 3
index: 1
---

# Profiles

The ARC Data Model specification defines three peer profiles that together form the general ARC RDM model. The implementation uses one unified object model; the profiles describe coherent subsets of the same model surface.

## Reading Order

1. [ARC Core](process_core/overview.md)
2. [Datamap](datamap/overview.md)
3. [Administrative](administrative/overview.md)
4. [Querying](../project/querying.md)

## Principles

- Process-centric: experiments and workflows are modeled as processes connecting inputs to outputs.
- Unified: ARC Core, Datamap, and Administrative properties are available on the shared model types rather than split into separate runtime objects.
- Extensible: `Annotation` and `additionalType` carry domain-specific information while typed properties cover the common profile surface.
- Representation-aware but model-first: SQL and YAML schemas derive from the markdown spec.

## Main Areas

| Area | Description |
|------|-------------|
| [ARC Core](process_core/overview.md) | Provenance model: Dataset, Process, Recipe, Sample, Data, Annotation, FormalParameter, and DefinedTerm |
| [Datamap](datamap/overview.md) | Data files, selected fragments, fragment descriptors, and dataset-level data contexts |
| [Administrative](administrative/overview.md) | Dataset agents, affiliations, citations, licenses, dates, and administrative metadata |
| [Decorations](decorations/overview.md) | ISA and Workflow Run mappings layered onto the unified model |
| [Querying](../project/querying.md) | Query use cases and graph traversal notes |
