---
title: Dataset
category: Core Specification
categoryindex: 4
index: 2
---

# Dataset

Container and context for data and processes. A Dataset groups a set of processes that belong together and provides administrative metadata (identifier, title, description).

**Schema.org type**: `schema.org/Dataset`

Decorations specialize Dataset via `additionalType`:
- ISA: Investigation, Study, Assay
- Workflow Run: ARC Workflow, ARC Run
- Datamap

## Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `id` | Text | MUST | Unique identifier for the dataset |
| `type` | Text | MUST | `Dataset` |
| `additionalType` | Text | COULD | Discriminator for decoration type |
| `identifier` | Text | MUST | Identifying descriptor |
| `title` | Text | SHOULD | Human-readable dataset title |
| `description` | Text | SHOULD | Short description or abstract |
| `processes` | [Process](Process.md) | SHOULD | Processes contained in this dataset |
| `hasPart` | [Dataset](Dataset.md), [Data](Data.md) | SHOULD | Sub-datasets or data files |
| `additionalProperty` | [Annotation](Annotation.md) | COULD | Extensible metadata |

## Relationships

```mermaid
flowchart TD

    id@{ shape: stadium, label: "string" }
    na@{ shape: stadium, label: "string" }
    de@{ shape: stadium, label: "string" }

    d[Dataset]
    Dataset --processes--> Process
    d --hasPart--> Dataset
    d --hasPart--> Data
    Dataset --additionalProperty--> Annotation
    Dataset --identifier--> id
    Dataset --title--> na
    Dataset --description--> de


```
