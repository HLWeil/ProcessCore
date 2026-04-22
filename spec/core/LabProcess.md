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

## Objects and Results

The core mechanism of processes is to connect inputs (objects) to outputs (results). To allow for grouping of multiple inputs and outputs, we allow `object` and `result` to be lists. In this case, both lists should be of the same length and the Nth object corresponds to the Nth result. This allows us to maintain a simple one-to-one mapping between objects and results while still supporting multiple inputs and outputs per process. 

```mermaid
flowchart TD

    subgraph objects
        o1[object 1]
        o2[object 2]
    end

    subgraph results
        r1[result 1]
        r2[result 2]
    end

    LabProcess --"object"--> objects
    LabProcess --"result"--> results

    o1 -.correspondsTo.-> r1
    o2 -.correspondsTo.-> r2

```