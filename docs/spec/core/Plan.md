---
title: Plan
category: Core Specification
categoryindex: 4
index: 4
---

# Plan

Description of a planned procedure. Protocols define what a Process executes, including intended use, equipment, reagents, and software.

**Schema.org type**: `bioschemas.org/LabProtocol`

Decorations specialize Protocol:
- ISA: Plan
- Workflow Run: Workflow Protocol (SoftwareSourceCode + ComputationalWorkflow + Plan)

## Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `id` | Text | COULD | URL or identifier for the protocol |
| `type` | Text | MUST | `Process` |
| `additionalType` | Text | COULD | Decoration discriminator, e.g. `Plan` |
| `name` | Text | SHOULD | Main title |
| `parameters` | [FormalParameter](FormalParameter.md) | COULD | Prospectively specifies parameters for which values should be given in the execution of the protocol, Maps to `input` in Bioschemas type|
| `description` | Text | SHOULD | Short description or abstract |
| `intendedUse` | [DefinedTerm](DefinedTerm.md), Text | SHOULD | Protocol type as ontology term |
| `additionalProperty` | [Annotation](Annotation.md) | COULD | Extensible protocol metadata |
| `labEquipment` | [Annotation](Annotation.md) | COULD | Equipment used in the protocol |
| `version` | Text | COULD | Version identifier |
| `url` | URL | COULD | External protocol resource |

## Relationships

```mermaid
flowchart TD

    na@{ shape: stadium, label: "string" }
    de@{ shape: stadium, label: "string" }
    ve@{ shape: stadium, label: "string" }
    ur@{ shape: stadium, label: "URL" }
    av[Annotation]
    le[Annotation]

    Process --executesProtocol--> Plan
    Plan --intendedUse--> DefinedTerm
    Plan --additionalProperty--> av
    Plan --labEquipment--> le
    Plan --parameters--> FormalParameter
    Plan --name--> na
    Plan --description--> de
    Plan --version--> ve
    Plan --url--> ur

```
