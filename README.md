# ARC-Data-Model

Repo to collect planning and design of the upcoming changes regarding core ARC representation, data model and querying.


## Current status

### ARCtrl: Core Representation

https://github.com/nfdi4plants/ARCtrl/tree/main/src/Core

This is the current main datamodel in the ARC ecosystem. It uses a tabular representation for the experimental annotations, which is based on the ISA-Tab format. The main entities are:

#### Top level object hierarchy

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

#### ArcTable

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




### ARCtrl: ISA-JSON Representation

https://github.com/nfdi4plants/ARCtrl/tree/main/src/Core/Process

- Closely follows the ISA-JSON format
- Mostly Immutable Record types

### ARCtrl: RO-Crate Representation

#### Generic layer of basic JSON-LD objects:

https://github.com/nfdi4plants/ARCtrl/tree/main/src/ROCrate

- LDGraph (graph containing collection of flattened nodes)
- LDNode (main object representing any complex object)
- LDContext (context for the JSON-LD graph, containing mapping of terms to IRIs)
- LDRef (reference to another node in the graph, containing the @id of the referenced node)
- LDValue (simple value, such as string, number, boolean, or array of simple values)

##### Hierarchical 

```mermaid
flowchart TD
    node1[LDNode]
    node2[LDNode]
    node3[LDNode]
    value1@{ shape: stadium, label: "LDValue" }
    value2@{ shape: stadium, label: "LDValue" }

    node1 --property--> node2
    node1 --property--> value1
    node2 --property--> node3
    node2 --property--> value2

    node1 --context--> LDContext 
```

##### Flattened

```mermaid
flowchart TD
    g[LDGraph]

    node1[LDNode]
    node2[LDNode]
    node3[LDNode]
    value1@{ shape: stadium, label: "LDValue" }
    value2@{ shape: stadium, label: "LDValue" }
    ref2@{ shape: stadium, label: "LDRef" }
    ref3@{ shape: stadium, label: "LDRef" }

    node1 --property--> ref2
    node1 --property--> value1
    node2 --property--> ref3
    node2 --property--> value2

    g --contains--> node1
    g --contains--> node2
    g --contains--> node3

    g --context--> LDContext 
```

#### Static classes for Schema-Types and RO-Crate profile:

```mermaid
flowchart TD
    Dataset --about--> LabProcess
    LabProcess --"object"--> Sample
    LabProcess --"object"--> File
    LabProcess --result--> Sample
    LabProcess --result--> File
    LabProcess --parameterValue--> PropertyValue
    LabProcess --executesLabProtocol--> LabProtocol
    Sample --additionalProperty--> PropertyValue
```

### Querymodel: Core Extensions

https://github.com/nfdi4plants/ARCtrl.Querymodel/tree/main/src/ARCtrl.QueryModel

### Querymodel: RO-Crate Extensions

https://github.com/nfdi4plants/ARCtrl.Querymodel/tree/main/src/ARCtrl.QueryModel/ProcessCore


### Mappings and IO

```mermaid
flowchart LR
    ARCScaffold@{ shape: stadium, label: "ARCScaffold" }
    ISA-XLSX@{ shape: stadium, label: "ISA-XLSX" }
    RO-Crate@{ shape: stadium, label: "RO-Crate" }
    ISA-JSON@{ shape: stadium, label: "ISA-JSON" }
    ARC-JSON@{ shape: stadium, label: "ARC-JSON" }
    JSON-LD@{ shape: stadium, label: "JSON-LD" }
    YAML-LD@{ shape: stadium, label: "YAML-LD" }

    subgraph "ARCtrlModel"
        ARC
        ArcAssay
        ArcRun
    end

    ARC --> ArcAssay
    ARC --> ArcRun

    ARCScaffold <-- IO --> ARC
    ArcAssay <-- IO --> ISA-XLSX
    ArcAssay <-- IO --> ARC-JSON
    ArcRun <-- IO --> ISA-XLSX
    ArcRun <-- IO --> ARC-JSON
    ARCtrlModel <-- Conversion --> RO-CrateModel
    ISA-JSONModel <-- Conversion --> ARCtrlModel
    RO-CrateModel <-- IO --> RO-Crate
    RO-CrateModel <-- IO --> JSON-LD
    RO-CrateModel <-- IO --> YAML-LD
    ISA-JSON <-- IO --> ISA-JSONModel

```