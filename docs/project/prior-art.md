---
title: Prior Art
category: Project
categoryindex: 2
index: 8
---

# Prior Art

The ARC Data Model builds on existing ARCtrl representations and query-model work. This page keeps that background separate from the current repository overview.

## ARCtrl Core Representation

Source: <https://github.com/nfdi4plants/ARCtrl/tree/main/src/Core>

The current ARC ecosystem has historically used a tabular representation for experimental annotations, based on ISA-Tab. The main shape is:

```mermaid
flowchart TD
    ARC[ARC = Investigation]
    ARC --> ArcStudy
    ARC --> ArcAssay
    ARC --> ArcRun
    ARC --> ArcWorkflow
    ArcStudy --> ArcTables
    ArcAssay --> ArcTables
    ArcRun --> ArcTables
    ArcTables --> ArcTable
    ArcRun --> CWLInputValues
    ArcRun --> CWLProcessingUnit
    ArcWorkflow --> CWLProcessingUnit
```

## ArcTable

```mermaid
flowchart TD
    ArcTable
    ArcTable --> CompositeHeader
    ArcTable --> CompositeCell
    io[Input/Output]
    param[Parameter/Factor/...]
    prot[ProtocolREF/Protocol...]
    CompositeHeader --o io
    CompositeHeader --o param
    CompositeHeader --o prot
    CompositeCell --o Unitized
    CompositeCell --o Freetext
    CompositeCell --o Term
    CompositeCell --o File
```

## ARCtrl ISA-JSON Representation

Source: <https://github.com/nfdi4plants/ARCtrl/tree/main/src/Core/Process>

- Closely follows ISA-JSON.
- Mostly uses immutable record types.

## ARCtrl RO-Crate Representation

Source: <https://github.com/nfdi4plants/ARCtrl/tree/main/src/ROCrate>

The generic linked-data layer uses flattened JSON-LD-style objects:

- `LDGraph`
- `LDNode`
- `LDContext`
- `LDRef`
- `LDValue`

```mermaid
flowchart TD
    Dataset --about--> LabProcess
    Dataset --additionalProperty--> PropertyValue
    LabProcess --object--> Sample
    LabProcess --object--> File
    LabProcess --result--> Sample
    LabProcess --result--> File
    LabProcess --parameterValue--> PropertyValue
    LabProcess --additionalProperty--> PropertyValue
    LabProcess --executesLabProtocol--> LabProtocol
    Sample --additionalProperty--> PropertyValue
    File --additionalProperty--> PropertyValue
    LabProtocol --additionalProperty--> PropertyValue
```

## Query Model

Sources:

- <https://github.com/nfdi4plants/ARCtrl.Querymodel/tree/main/src/ARCtrl.QueryModel>
- <https://github.com/nfdi4plants/ARCtrl.Querymodel/tree/main/src/ARCtrl.QueryModel/ProcessCore>

The current repo keeps preserved query-model reference material under `references/ProcessCore/` and implements current graph/path helpers in `src/ProcessCore/`.

## Mappings And IO

```mermaid
flowchart LR
    ARCScaffold@{ shape: stadium, label: "ARCScaffold" }
    ISA_XLSX@{ shape: stadium, label: "ISA-XLSX" }
    RO_Crate@{ shape: stadium, label: "RO-Crate" }
    ISA_JSON@{ shape: stadium, label: "ISA-JSON" }
    ISA_JSONModel@{ shape: stadium, label: "ISA-JSONModel" }
    ARC_JSON@{ shape: stadium, label: "ARC-JSON" }
    JSON_LD@{ shape: stadium, label: "JSON-LD" }
    YAML_LD@{ shape: stadium, label: "YAML-LD" }

    subgraph ARCtrlModel
        ARC
        ArcAssay
        ArcRun
    end

    ARC --> ArcAssay
    ARC --> ArcRun

    ARCScaffold <-- IO --> ARC
    ArcAssay <-- IO --> ISA_XLSX
    ArcAssay <-- IO --> ARC_JSON
    ArcRun <-- IO --> ISA_XLSX
    ArcRun <-- IO --> ARC_JSON
    ARCtrlModel <-- Conversion --> RO_Crate
    ISA_JSONModel <-- Conversion --> ARCtrlModel
    ISA_JSON <-- IO --> ISA_JSONModel
    RO_Crate <-- IO --> JSON_LD
    RO_Crate <-- IO --> YAML_LD
```
