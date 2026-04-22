# LabProtocol

Description of a planned procedure. Protocols define what a Process executes, including intended use, equipment, reagents, and software.

**Schema.org type**: `bioschemas.org/LabProtocol`

Decorations specialize Protocol:
- ISA: LabProtocol
- Workflow Run: Workflow Protocol (SoftwareSourceCode + ComputationalWorkflow + LabProtocol)

## Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `id` | Text | COULD | URL or identifier for the protocol |
| `type` | Text | MUST | Protocol type |
| `additionalType` | Text | COULD | Decoration discriminator, e.g. `LabProtocol` |
| `name` | Text | SHOULD | Main title |
| `parameters` | [FormalParameter](FormalParameter.md) | COULD | Prospectively specifies parameters for which values should be given in the execution of the protocol, Maps to `input` in Bioschemas type|
| `description` | Text | SHOULD | Short description or abstract |
| `intendedUse` | [DefinedTerm](DefinedTerm.md), Text | SHOULD | Protocol type as ontology term |
| `additionalProperty` | [PropertyValue](PropertyValue.md) | COULD | Extensible protocol metadata |
| `version` | Text | COULD | Version identifier |
| `url` | URL | COULD | External protocol resource |

## Relationships

```mermaid
flowchart TD

    na@{ shape: stadium, label: "string" }
    de@{ shape: stadium, label: "string" }
    ve@{ shape: stadium, label: "string" }
    ur@{ shape: stadium, label: "URL" }

    LabProcess --executesProtocol--> LabProtocol
    LabProtocol --intendedUse--> DefinedTerm
    LabProtocol --additionalProperty--> PropertyValue
    LabProtocol --parameters--> FormalParameter
    LabProtocol --name--> na
    LabProtocol --description--> de
    LabProtocol --version--> ve
    LabProtocol --url--> ur

```
