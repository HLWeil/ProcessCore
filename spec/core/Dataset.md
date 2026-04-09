# Dataset

Container and context for processes. A Dataset groups a set of processes that belong together and provides administrative metadata (identifier, title, description, creators).

**Schema.org type**: `schema.org/Dataset`

Decorations specialize Dataset via `additionalType`:
- ISA: Investigation, Study, Assay
- Workflow Run: ARC Workflow, ARC Run

## Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `@id` | Text | MUST | Unique identifier for the dataset |
| `@type` | Text | MUST | `schema.org/Dataset` |
| `additionalType` | Text | MUST | Discriminator for decoration type |
| `identifier` | Text | MUST | Identifying descriptor |
| `name` | Text | SHOULD | Title |
| `description` | Text | SHOULD | Short description or abstract |
| `creator` | [Person](Person.md) | SHOULD | Authors or owners |
| `about` | [Process](Process.md) | SHOULD | Processes contained in this dataset |
| `hasPart` | Dataset, [Data](Data.md) | SHOULD | Sub-datasets or data files |
| `additionalProperty` | [PropertyValue](PropertyValue.md) | COULD | Extensible metadata |

## Relationships

```mermaid
flowchart TD
    Dataset --about--> Process
    Dataset --hasPart--> Dataset
    Dataset --hasPart--> Data
    Dataset --creator--> Person
```
