# Core Datamodel

The core datamodel is a set of classes, implemented in F#, that represent the core entities and relationships defined in the [Process Core Model specification](../spec/core/README.md). These classes are designed to be used as a basis for building tools and applications that work with ARC data, providing a structured way to represent and manipulate the core concepts of the data model.

## Requirements

- The core datamodel should include classes for each of the core entities defined in the specification [Process Core Model specification](../spec/core/README.md)
- The datamodel should provide easy (CRUD) and performant API to manipulate all these core entities (e.g. add process to dataset, modify parameterValue in process, remove input/output pair from process, etc.)
- The datamodel should provide easy and performant API to collect all entities of a specific type
- The datamodel should be designed to support efficient querying and traversal of the process graph, according to the use cases defined in [the querying use cases document](../spec/querying/use-cases.md).
- The datamodel should be designed to allow for decorations such as the ones defined in the [Decorations specification](../spec/decorations/README.md), i.e. type decorations like "Assay" should only sit on top of the base type "Dataset", so even when you're working with an "Assay" object, you should be able to easily access the underlying "Dataset" properties and relationships.
- Fable compatibility:
    - Use [AttachMembers] on all classes with members
    - Avoid using features that are not supported by Fable (e.g. certain .NET libraries, reflection, etc.)
    - Make use of ResizeArray and Dictionary for collections to ensure compatibility with JavaScript data structures.

## Mutability

Types are **mutable classes**. All collection fields (e.g. `inputs`, `outputs`, `processes`, `parameterValue`) are `ResizeArray`s that can be mutated in-place. Scalar fields are mutable properties. This makes CRUD operations natural and avoids the overhead of copy-and-update semantics for large graphs.

## Back-Edges

Each `Material` and `Data` node must maintain two back-edge collections:

- `inputOf` — the set of `LabProcess` instances for which this node is an **input**.
- `outputOf` — the set of `LabProcess` instances for which this node is an **output**.

These back-edges must be kept consistent eagerly: whenever a process's `inputs` or `outputs` list is mutated (add/remove), the corresponding back-edge on the affected node is updated in the same operation. This allows O(1) lookup of "which processes consume/produce this node" without scanning all processes.

Each `LabProcess` instance must maintain a back-edge reference to its dataset: `processOf`

Each `dataset` instance must maintain a back-edge reference to its parent dataset (if any): `partOf`

## Object Identity and Distinctness

For each object in the datamodel, identity is determined by it's values. Each type contains their own indicator fields of identity. For each type, this equality (or distinctness) is only given in a specific context of a container it is contained in. For example, two `Material` objects with the same name are considered identical across indefinite dataset hierarchies.

This distinctness must be kept consistent eagerly: whenever an object is added to a container (e.g. a `Material` to a `LabProcess`'s `inputs`), the container must check if an identical object already exists in that container. If it does, the existing object is reused instead of adding the new one. This ensures that all references to a given entity point to the same object instance, maintaining consistency and enabling efficient graph traversal.
In the same sense, when datasets are nested, if a child dataset is added to a parent dataset, the parent must check if an identical child already exists in its collection. If it does, the existing child is reused instead of adding the new one.

### IONode registry

The **root dataset** of each hierarchy maintains a single `Dictionary<string, IONode>` keyed by `IONode.Key()`. This is the only registry in the hierarchy; child datasets hold no registry of their own. Canonicalization scope is therefore the entire dataset tree, which matches the identity rule that equal nodes are identical across datasets.

The root is reached by walking `PartOf` until `None`.

**Population rules:**

- `process.AddInput(node)` / `AddOutput(node)` — walk up to the root registry via `PartOf`. If the key is present, substitute the canonical instance; otherwise insert the new node as canonical.
- `dataset.AddProcess(proc)` — register all nodes already in `proc.Inputs` and `proc.Outputs` into the root registry.
- `dataset.AddPart(child)` — register all nodes reachable through `child.AllNodes()` into the root registry.

**Removal rules:**

- When a process is removed, each of its nodes is checked via `InputOf`/`OutputOf` back-edges. A node is evicted from the root registry only if no process remaining anywhere in the tree still references it.
- When a child dataset is removed from a parent, apply the same per-node check for all nodes reachable through the child.

## Path Type

A `Path` type is part of the core data model. It represents a sequence of `LabProcess` instances connected through their shared I/O nodes, i.e. a directed walk through the process graph. It is a **read-only view** — it has no CRUD API of its own and does not own the processes it references. It is produced by graph-traversal queries and exposes the ordered list of processes and the nodes connecting them.

It is basically the more capable continuation of the [ValueCollection](../references/ProcessCore/ValueCollection.fs) concept, but maintaining the full process context, allowing more than just retrieving property values.

## Querying

Querying methods are embedded directly on the entity objects (not in a separate query wrapper), following the pattern established by the reference query model (`ARCtrl.QueryModel.ProcessCore`).

### PropertyValue sources

When retrieving property values from a process context, four sources can contribute:

1. `process.ParameterValue` — parameters attached directly to the process
2. `process.Inputs[*].AdditionalProperty` — characteristics attached to input nodes
3. `process.Outputs[*].AdditionalProperty` — factors attached to output nodes
4. `process.ExecutesProtocol?.LabEquipment` — components (equipment, reagents, software) attached to the protocol via `labEquipment`

All retrieval methods are named `*PropertyValues` (not `*ParameterValues`) to reflect this broad collection.

For graph traversal queries from an `IONode`, node-attached values are collected on an input/output-pair basis. A reached `LabProcess` contributes its process-level `ParameterValue` and protocol `LabEquipment`, but only the `AdditionalProperty` values of nodes actually reached by the traversal are included. If two independent IO lanes share the same processes, querying from one lane must not collect `AdditionalProperty` values from the other lane.

This applies equally when the member is called on `IONode`, `Material`, or `Data`, and through dataset-scoped wrappers such as `UpstreamPropertyValuesForNode` / `DownstreamPropertyValuesForNode` / `PropertyValuesForNode`. `Path.AllPropertyValues` operates on the explicit process sequence supplied to the `Path` value and therefore collects from all input/output nodes of those path processes.

### Traversal directions

Graph traversal methods come in three flavours:

- **Undirected** (`AllConnectedProcesses`, `AllConnectedNodes`, …) — BFS through both upstream and downstream edges simultaneously.
- **Upstream** (`UpstreamProcesses`, `UpstreamNodes`, …) — BFS following `OutputOf` back-edges to predecessor processes and their inputs.
- **Downstream** (`DownstreamProcesses`, `DownstreamNodes`, …) — BFS following `InputOf` back-edges to successor processes and their outputs.

### Positional N-to-N mapping within a process

Within a single `LabProcess` the I/O slots are positionally paired: the **Nth input corresponds to the Nth output**. Directional traversal (`UpstreamNodes`, `DownstreamNodes`, and their `*Processes` counterparts) exploits this by walking only through the positional peer rather than through all inputs or outputs. For example, `Output[1].UpstreamNodes()` follows only `Input[1]` of the producing process, not `Input[0]`.

This mapping applies when a process has **equal numbers of inputs and outputs**. When the counts differ (e.g. a merge or split step) the traversal falls back to considering all inputs or all outputs.

### Root and final nodes

A node is a **root** if no in-scope process produces it as output (`IsRootNode`).  
A node is a **final** if no in-scope process consumes it as input (`IsFinalNode`).

### Data fragment selector support

`Data` represents both whole data resources and selected fragments. Fragment-level addressing is expressed through `Path`, optional `Selector`, optional `SelectorFormat`, and optional `EncodingFormat`.

The core datamodel should provide a generic, extensible fragment-selector resolution layer for graph traversal and dataset queries. The core must not define a closed selector vocabulary. Selector-specific behavior is supplied by resolver/spec implementations registered by users or higher-level packages.

Fragment-aware behavior is opt-in by resolver availability, with one generic exception: if two `Data` nodes have the same `Path` and only one has a `Selector`, the whole-resource node is treated as containing the selected-fragment node. If no resolver is registered for a selector format, all other comparisons fall back to exact matching (`Path` plus `Selector`). Unsupported, unknown, or intentionally opaque selector formats therefore remain backwards-compatible.

The first required semantic relations are:

- exact: both data nodes identify the same resource or fragment
- contains: one data node identifies a resource or fragment containing the other
- unknown/disjoint: no traversal relationship beyond exact fallback

Graph traversal should use these relations through resolver-aware query paths rather than by changing `Data` equality. `Data` equality and node registry keys should remain exact (`Path` plus `Selector`) so canonicalization stays deterministic.

Specific selector implementations should sit on top of the generic core mechanism. The first concrete resolver should target RFC 7111 CSV selectors, because current examples use selectors such as `#col=1` and `#col=2-11`. Spreadsheet fragment selectors should be added as a separate resolver following the fragment-level FAIRness specification, not hard-coded into `Data`.


### Optional scope

All traversal methods accept an optional `scope: ResizeArray<LabProcess>` parameter. When supplied, the BFS is restricted to processes within that set. This allows scoping queries to a single `Dataset` without creating a separate graph object.

### Placement of query methods

| Entity | Methods |
|--------|---------|
| `IONode` | Full traversal and PropertyValue retrieval; `IsRootNode`, `IsFinalNode` |
| `Material`, `Data` | Delegate to `IONode` wrapper (`MaterialNode this` / `DataNode this`) |
| `LabProcess` | `InputMaterials`, `InputData`, `OutputMaterials`, `OutputData`, `ProtocolParameters`, `PropertyValuesByName` |
| `Dataset` | `AllNodes`, `RootNodes`, `FinalNodes`, `AllPropertyValues(?protocolName)`, `PropertyValuesForNode`, `UpstreamPropertyValuesForNode`, `DownstreamPropertyValuesForNode`, `ProcessesForNode`, `PathsThrough`, `NodesUpstreamOf`, `NodesDownstreamOf`, `ConnectedMaterialsForNode`, `ConnectedDataForNode`, `ProtocolParametersForNode`, `FindProcessesByProtocolType`, `FindProcessesByPropertyValue`, `FindProcessesByPropertyName`, `MaterialsResultingFromCondition` |
| `Path` | `AllPropertyValues`, `PropertyValuesByName`, `ProtocolParameters`, `TerminalInputs`, `TerminalOutputs` |

## Target folder

The core datamodel implementation will be located in `src/ProcessCore/`, around a [ProcessCore.fsproj](../src/ProcessCore/ProcessCore.fsproj) file.

## Out of Scope

The following are explicitly **not** part of this implementation:

- Any serialization or deserialization (JSON-LD, RO-Crate, XLSX, YAML, etc.)
- ISA decoration types (`Investigation`, `Study`, `Assay`, etc.)
- Workflow Run decoration types (`ArcWorkflow`, `ArcRun`, etc.)
- Datamap decoration types
- Validation logic
