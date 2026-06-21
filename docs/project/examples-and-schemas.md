---
title: Examples And Schemas
category: Project
categoryindex: 2
index: 4
---

# Examples And Schemas

Schemas are derived representations of the markdown specification. The markdown files in [docs/spec](../spec/index.md) remain the normative source.

## Schema Artifacts

| Location | Status |
|----------|--------|
| `schemas/sql/` | SQLite-oriented SQL profile with executable DDL and seed data |
| `schemas/yml/` | JSON Schema draft 2020-12 expressed in YAML for core entities |
| `schemas/document-db/` | Placeholder for a future document database representation |

The YAML schemas describe the current core vocabulary. In particular, `Process.yml` expects `inputs`, `outputs`, and `executesProtocol`.

## Examples

| Location | Status |
|----------|--------|
| `examples/core/minimal.yml` | Schema-shaped core example intended to match the current YAML schema vocabulary |
| `examples/isa/` | Legacy/profile-shaped ISA and Datamap examples kept as domain examples |
| `examples/workflow-run/` | Placeholder for future Workflow Run examples |

The current ISA examples are useful profile examples, but they are not currently guaranteed to validate against the strict core YAML schemas. They use some RO-Crate/Bioschemas profile terms such as `object`, `result`, `executesPlan`, `annotations`, and `additionalProperties`.

## Reconciliation Rule

When adding new examples, prefer one of these explicit categories:

- Core schema examples that use the current schema vocabulary and should be validatable.
- Profile-shaped examples that preserve upstream/profile terminology and are labeled as such.

This avoids mixing schema-valid core examples with legacy/profile examples that intentionally use another vocabulary.

## Core Example Shape

```yaml
id: dataset:demo
type: schema:Dataset
identifier: demo
processes:
  - id: process:extract
    type: bioschemas:LabProcess
    name: Extraction
    inputs:
      - id: sample:leaf
        type: bioschemas:Sample
        additionalType: Source
        name: Leaf tissue
    outputs:
      - type: File
        path: raw/extract.tsv
    executesProtocol:
      id: protocol:extraction
      type: bioschemas:LabProtocol
      name: Extraction
```
