---
title: Process
category: ARC Core Profile
categoryindex: 4
index: 3
---

# Process

Core transformation node in the process graph. A Process connects inputs to outputs and references the Protocol that was executed.

**Schema.org type**: `bioschemas.org/LabProcess`

Decorations specialize Process:
- ISA: Process
- Workflow Run: Workflow Invocation (CreateAction + Process)

## Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `id` | Text | COULD | Unique process identifier |
| `type` | Text | MUST | `Process` |
| `additionalType` | Text | COULD | Decoration discriminator, e.g. `Process` |
| `name` | Text | MUST | Name of the process |
| `inputs` | [Sample](Sample.md), [Data](Data.md) | SHOULD | Input(s) of the process |
| `outputs` | [Sample](Sample.md), [Data](Data.md) | SHOULD | Output(s) of the process |
| `executesProtocol` | [Recipe](Recipe.md) | SHOULD | Protocol that was executed |
| `parameterValue` | [Annotation](Annotation.md) | SHOULD | Parameter key-value pairs |

## Relationships

```mermaid
flowchart TD

    na@{ shape: stadium, label: "string" }

    Dataset --processes--> Process
    Process --inputs--> Sample/Data
    Process --"outputs"--> Sample/Data
    Process --executesProtocol--> Recipe
    Process --parameterValue--> Annotation
    Process --name--> na
```

## Inputs and Outputs

The core mechanism of processes is to connect inputs to outputs. To allow for grouping of multiple inputs and outputs, we allow `inputs` and `outputs` to be lists. In this case, both lists should be of the same length and the Nth input corresponds to the Nth output. This allows us to maintain a simple one-to-one mapping between inputs and outputs while still supporting multiple inputs and outputs per process. 

```mermaid
flowchart TD

    subgraph inputs
        o1[input 1]
        o2[input 2]
    end

    subgraph outputs
        r1[result 1]
        r2[result 2]
    end

    Process --inputs--> inputs
    Process --"outputs"--> outputs

    o1 -.correspondsTo.-> r1
    o2 -.correspondsTo.-> r2

```

