---
title: Dataset
category: ARC Core Profile
categoryindex: 4
index: 2
---

# Dataset

Container and context for data, processes, datamap entries, and administrative metadata. A Dataset groups a set of processes and data files that belong together.

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
| `license` | Text | COULD | License identifier, URL, or label |
| `datePublished` | Text | COULD | Publication date |
| `dateCreated` | Text | COULD | Creation date |
| `dateModified` | Text | COULD | Modification date |
| `processes` | [Process](Process.md) | SHOULD | Processes contained in this dataset |
| `hasPart` | [Dataset](Dataset.md) | SHOULD | Sub-datasets |
| `dataFiles` | [Data](Data.md) | COULD | Data files that belong to this dataset |
| `agents` | [Agent](../administrative/Agent.md) | COULD | Dataset agents |
| `citations` | [ScholarlyArticle](../administrative/ScholarlyArticle.md) | COULD | Publications cited by or associated with the dataset |
| `dataContexts` | [DataContext](../datamap/DataContext.md) | COULD | Dataset-level datamap context entries |
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
    d --dataFiles--> Data
    d --agents--> Agent
    d --citations--> ScholarlyArticle
    d --dataContexts--> DataContext
    Dataset --additionalProperty--> Annotation
    Dataset --identifier--> id
    Dataset --title--> na
    Dataset --description--> de


```


