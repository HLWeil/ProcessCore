---
title: Person
category: ISA Decoration
categoryindex: 5
index: 7
---

# Person

Individual contributor or performer in the experimental workflow.

**Schema.org type**: `schema.org/Person`

## Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `id` | Text | COULD | Unique identifier |
| `type` | Text | MUST | `schema.org/Person` |
| `givenName` | Text | MUST | Given name |
| `familyName` | Text | SHOULD | Family name |
| `email` | Text | SHOULD | Email address |
| `affiliation` | [Organization](Organization.md) | SHOULD | Affiliated organization |
| `identifier` | Text, Annotation | SHOULD | ORCID or other identifier |
| `additionalProperty` | [Annotation](../../core/Annotation.md) | COULD | Extensible person metadata not covered by the base properties |
| `jobTitle` | [DefinedTerm](../../core/DefinedTerm.md) | COULD | Job title |

## Relationships

```mermaid
flowchart TD

    gn@{ shape: stadium, label: "string" }
    fn@{ shape: stadium, label: "string" }
    e@{ shape: stadium, label: "E-MAIL" }
    i@{ shape: stadium, label: "ORCID" }

    Dataset --creator--> Person
    Process --agent--> Person
    Person --affiliation--> Organization
    Person --jobTitle--> DefinedTerm
    Person --givenName--> gn
    Person --familyName--> fn
    Person --email--> e
    Person --identifier--> i
    Person --additionalProperty--> Annotation
```
