# LabProcess

Core transformation node in the process graph. A Process connects inputs (objects) to outputs (results) and references the Protocol that was executed.

**Schema.org type**: `bioschemas.org/LabProcess`

Decorations specialize Process:
- ISA: LabProcess
- Workflow Run: Workflow Invocation (CreateAction + LabProcess)

## Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `id` | Text | COULD | Unique process identifier |
| `type` | Text | MUST | Process type |
| `additionalType` | Text | COULD | Decoration discriminator, e.g. `LabProcess` |
| `name` | Text | MUST | Name of the process |
| `object` | [Material](Material.md), [Data](Data.md) | SHOULD | Input(s) of the process |
| `result` | [Material](Material.md), [Data](Data.md) | SHOULD | Output(s) of the process |
| `executesProtocol` | [LabProtocol](LabProtocol.md) | SHOULD | Protocol that was executed |
| `parameterValue` | [PropertyValue](PropertyValue.md) | SHOULD | Parameter key-value pairs |

## Relationships

```mermaid
flowchart TD

    na@{ shape: stadium, label: "string" }

    Dataset --about--> LabProcess
    LabProcess --"object"--> Material/Data
    LabProcess --result--> Material/Data
    LabProcess --executesProtocol--> LabProtocol
    LabProcess --parameterValue--> PropertyValue
    LabProcess --name--> na
```
