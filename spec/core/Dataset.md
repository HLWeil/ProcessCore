# Dataset

Container and context for processes. A Dataset groups a set of processes that belong together and provides administrative metadata (identifier, title, description).

**Schema.org type**: `schema.org/Dataset`

Decorations specialize Dataset via `additionalType`:
- ISA: Investigation, Study, Assay
- Workflow Run: ARC Workflow, ARC Run

## Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `id` | Text | MUST | Unique identifier for the dataset |
| `type` | Text | MUST | `schema.org/Dataset` |
| `additionalType` | Text | MUST | Discriminator for decoration type |
| `identifier` | Text | MUST | Identifying descriptor |
| `name` | Text | SHOULD | Title |
| `description` | Text | SHOULD | Short description or abstract |
| `about` | [Process](Process.md) | SHOULD | Processes contained in this dataset |
| `hasPart` | Dataset | SHOULD | Sub-datasets or data files |
| `additionalProperty` | [PropertyValue](PropertyValue.md) | COULD | Extensible metadata |

## Relationships

```mermaid
flowchart TD

    id@{ shape: stadium, label: "string" }
    na@{ shape: stadium, label: "string" }
    de@{ shape: stadium, label: "string" }

    d[Dataset]
    Dataset --about--> Process
    d --hasPart--> Dataset
    Dataset --creator--> Person
    Dataset --additionalProperty--> PropertyValue
    Dataset --identifier--> id
    Dataset --name--> na
    Dataset --description--> de


```
