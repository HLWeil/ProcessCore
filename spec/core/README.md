# ProcessCore

The ProcessCore model is the foundation of the ARC Data Model. It abstracts the ISA process model away from Investigation/Study/Assay specifics, providing a generic graph that connects sources to created data via processes.

## Design Principles

- **Process-centric**: All experimental workflows are modeled as graphs of processes that transform inputs into outputs.
- **Extensible**: PropertyValues provide a key-value-unit extension mechanism. Core entities can attach cross-cutting metadata through `additionalProperty`, while decorations specialize core types without modifying them.
- **Representation-agnostic**: The model can be depicted as a SQL schema, a document database, or RDF/JSON-LD.

## Process Graph

```mermaid
flowchart LR

    md[Material/Data]
    d[Dataset]
    dp[PropertyValue]
    pp[PropertyValue]
    mdp[PropertyValue]

    d --hasPart--> Dataset
    Dataset --about--> LabProcess
    Dataset --additionalProperty--> dp
    LabProcess --"object"--> md
    LabProcess --parameterValue--> pp
    LabProcess --result--> md
    LabProcess --executesProtocol--> LabProtocol
    md --additionalProperty--> mdp
    pp --instanceOf--> FormalParameter
    LabProtocol --parameters--> FormalParameter
    LabProtocol --additionalProperty--> PropertyValue

```

## Core Types

| Type | Description | Spec |
|------|-------------|------|
| [Dataset](Dataset.md) | Container/context for processes | Dataset.md |
| [LabProcess](LabProcess.md) | Transformation node connecting inputs to outputs | LabProcess.md |
| [LabProtocol](LabProtocol.md) | Description of a planned procedure | LabProtocol.md |
| [Material](Material.md) | Input/output biological or digital material | Material.md |
| [Data](Data.md) | Data files | Data.md |
| [PropertyValue](PropertyValue.md) | Extensible key-value-unit triple | PropertyValue.md |
| [FormalParameter](FormalParameter.md) | Named parameter slot for prospective provenance | FormalParameter.md |
| [DefinedTerm](DefinedTerm.md) | Ontology annotation | DefinedTerm.md |

## Usage Philosophy

At the core of the ProcessCore model is the idea that experimental procedures can be represented as graphs of processes that transform inputs into outputs. This process-centric view allows for a flexible and extensible representation of complex experimental designs, while the use of PropertyValues enables rich metadata annotation without bloating the core schema. The model is designed to be agnostic to specific representations, making it adaptable to various storage and serialization formats.

The Dataset serves as a container for related processes, providing administrative metadata and grouping functionality. LabProcesses represent the actual transformations that occur, connecting materials and data through defined protocols. Materials can represent both physical samples and digital entities, while Data is reserved for files. PropertyValues offer a powerful mechanism for extending the model with additional metadata, and DefinedTerms allow for ontology-based annotations to ensure semantic clarity.