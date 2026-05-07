---
title: Material
category: Core Specification
categoryindex: 4
index: 5
---

# Material

Input or output biological, chemical, or digital material in the process graph. Materials can derive from other materials, forming provenance chains.

**Schema.org type**: `bioschemas.org/Sample`

Decorations specialize Material:
- ISA: Sample, Source

## Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `id` | Text | MUST | Unique material name |
| `type` | Text | MUST | Material type |
| `additionalType` | Text | COULD | Decoration discriminator, e.g. `Sample` |
| `name` | Text | MUST | Name identifying the material |
| `additionalProperty` | [PropertyValue](PropertyValue.md) | SHOULD | Characteristics, factors, or other extensible metadata |

## Relationships

```mermaid
flowchart TD

    na@{ shape: stadium, label: "string" }

    LabProcess --inputs"--> Material
    LabProcess --"outputs"--> Material
    Material --additionalProperty--> PropertyValue
    Material --name--> na
```
