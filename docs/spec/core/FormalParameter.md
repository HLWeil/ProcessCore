---
title: FormalParameter
category: Core Specification
categoryindex: 4
index: 8
---

# FormalParameter

Describes the shape and type of protocol inputs/outputs, providing prospective provenance.

**Schema.org type**: `bioschemas.org/FormalParameter`

## Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `id` | Text | MUST | Unique identifier |
| `type` | Text | MUST | `FormalParameter` |
| `name` | Text | SHOULD | Parameter slot name (should match workflow parameter) |
| `nameTAN` | URL | SHOULD | Key ontology reference |
| `defaultValue` | DefinedTerm | COULD | Default value for input |

## Relationships

```mermaid
flowchart TD

    na@{ shape: stadium, label: "string" }
    nt@{ shape: stadium, label: "URL" }

    Plan --parameters--> FormalParameter
    FormalParameter --name--> na
    FormalParameter --nameTAN--> nt
    FormalParameter --defaultValue--> DefinedTerm
```
