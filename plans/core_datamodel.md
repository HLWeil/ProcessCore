# Core Datamodel

The core datamodel is a set of classes, implemented in F#, that represent the core entities and relationships defined in the [Process Core Model specification](../spec/core/README.md). These classes are designed to be used as a basis for building tools and applications that work with ARC data, providing a structured way to represent and manipulate the core concepts of the data model.

## Requirements

- The core datamodel should include classes for each of the core entities defined in the specification [Process Core Model specification](../spec/core/README.md)
- The datamodel should provide easy (CRUD) and performant API to manipulate all these core entities (e.g. add a process to a dataset, modify `ParameterValue`, or set/clear a process endpoint).
- The datamodel should provide easy and performant API to collect all entities of a specific type
- The datamodel should be designed to support efficient querying and traversal of the process graph, according to the use cases defined in [the querying use cases document](../spec/querying/use-cases.md).
- The datamodel should be designed to allow for decorations such as the ones defined in the [Decorations specification](../spec/decorations/README.md), i.e. type decorations like "Assay" should only sit on top of the base type "Dataset", so even when you're working with an "Assay" object, you should be able to easily access the underlying "Dataset" properties and relationships.
- Fable compatibility:
    - Use [AttachMembers] on all classes with members
    - Avoid using features that are not supported by Fable (e.g. certain .NET libraries, reflection, etc.)
    - Make use of ResizeArray and Dictionary for collections to ensure compatibility with JavaScript data structures.

## Mutability

Types are **mutable classes**. Collection fields such as `processes` and `parameterValue` are `ResizeArray`s that can be mutated in-place. Scalar fields are mutable properties. This makes CRUD operations natural and avoids copy-and-update overhead.

## Back-Edges

Each `Sample` and `Data` node must maintain two back-edge collections:

- `inputOf` — the set of `Process` instances for which this node is an **input**.
- `outputOf` — the set of `Process` instances for which this node is an **output**.

These back-edges must be kept consistent eagerly. `Process.SetInput`/`SetOutput` remove the back-edge from the previous endpoint, canonicalize and assign the replacement, then add its back-edge. `ClearInput`/`ClearOutput` remove the corresponding back-edge. This allows O(1) lookup of "which processes consume/produce this node" without scanning all processes.

Each `Process` instance must maintain a back-edge reference to its dataset: `processOf`

Each `dataset` instance must maintain a back-edge reference to its parent dataset (if any): `partOf`

## Object Identity and Distinctness

For each object in the datamodel, value equality is determined by type-specific identity fields and interpreted in the context of its container. For example, two `Sample` objects with the same name are considered identical across indefinite dataset hierarchies.

This distinctness must be kept consistent eagerly: whenever an object is added to a container (e.g. a `Sample` is assigned as a `Process.Input`), the container must check if an identical object already exists in that identity scope. If it does, the existing object is reused instead of the new one. This ensures that all references to a given entity point to the same object instance, maintaining consistency and enabling efficient graph traversal.
In the same sense, when datasets are nested, if a child dataset is added to a parent dataset, the parent must check if an identical child already exists in its collection. If it does, the existing child is reused instead of adding the new one.

`Process` storage is the deliberate exception to value-based container deduplication. `Process` equality may still compare identifying values, but `Dataset.AddProcess` rejects only re-adding the same owned instance; distinct equal processes are retained because they represent distinct graph edges/YAML lanes. Endpoint nodes, not processes, are canonicalized through the root registry.

### IONode registry

The **root dataset** of each hierarchy maintains a single `Dictionary<string, IONode>` keyed by `IONode.Key()`. This is the only registry in the hierarchy; child datasets hold no registry of their own. Canonicalization scope is therefore the entire dataset tree, which matches the identity rule that equal nodes are identical across datasets.

The root is reached by walking `PartOf` until `None`.

**Population rules:**

- `process.SetInput(node)` / `SetOutput(node)` — walk up to the root registry via `ProcessOf` and `PartOf`. If the key is present, substitute the canonical instance; otherwise insert the new node as canonical.
- `dataset.AddProcess(proc)` — canonicalize the optional `proc.Input` and `proc.Output`, repair their back-edges, and set `proc.ProcessOf`.
- `dataset.AddDataFile(data)` — canonicalize the data resource and any nested fragments before adding it to the dataset store.
- `dataset.AddPart(child)` — register the child's stored data files and all nodes reachable through `child.AllNodes()` into the root registry.
- `arc.AddSample(sample)` — canonicalize and pin the sample in the root registry. `ARC.DataFiles`, inherited from `Dataset`, provides the corresponding store for orphan data.

**Removal rules:**

- When a process is removed, each of its nodes is checked via `InputOf`/`OutputOf` back-edges. A node is evicted from the root registry only if no process, stored data file, or ARC pin remaining anywhere in the tree still references it.
- `arc.RemoveSample(sample)` removes the ARC pin but does not detach the sample from a process. The node is evicted only after its final stored or process reference is removed.
- When a child dataset is removed from a parent, apply the same per-node check to its stored data files and process nodes, then rebuild an independent registry for the detached hierarchy.

### Recipe registry

The root dataset also maintains a recipe registry keyed by recipe name and version. Named recipes in `ARC.Recipes` and `Process.ExecutesProtocol` share this registry across the complete dataset hierarchy. As with I/O nodes, the first inserted instance is canonical and later equal values reuse it without merging their fields.

`ARC.AddRecipe` pins a canonical recipe in the store. `ARC.RemoveRecipe` removes only that pin: processes that execute the recipe keep their reference, and the registry entry remains until its final process reference is removed. Replacing `Process.ExecutesProtocol`, removing a process, and attaching or detaching a child dataset all update the root recipe registry. Unnamed recipes are not registered because their mutable name has not established a stable identity key.

## ARC Unsorted Object Store

`ARC` provides canonical staging collections for profile objects that are not yet attached at their final graph position:

- `Samples : ResizeArray<Sample>` with `AddSample` and `RemoveSample`
- `Recipes : ResizeArray<Recipe>` with `AddRecipe` and `RemoveRecipe`
- inherited `DataFiles : ResizeArray<Data>` with `AddDataFile` and `RemoveDataFile`

Identity is Sample name, Data path plus selector, and Recipe name plus version. Canonicalization is insertion-order independent: adding an object to a store before linking it into a process, or linking it first and storing an equal object later, produces the same object reference. The existing canonical instance always wins and fields from later equal objects are not merged.

Store membership is explicit. Linking a stored object into a process does not remove it from the store, and removing it from the store does not detach existing process links. Call the corresponding remove method to stop storing it. Code should use these APIs rather than mutating the public `ResizeArray` collections directly, because the methods also maintain registry pins and eviction state.

### ARC YAML persistence

ARC YAML retains `type: Dataset` for compatibility and persists staging collections through typed top-level fields: `samples`, `dataFiles`, and `labProtocols`. Stored objects are decoded before processes, allowing process endpoints and protocol references to resolve to those same canonical instances. The indexed `labProtocols` field contains each canonical stored or process-linked recipe once, including recipes with the same name but different versions; explicit `@id` values remain authoritative.

The ARC serializer uses the shared Dataset YAML codecs. Runtime state such as `ArcPath`, `IsSpreadsheetScaffold`, registry data, `InputOf`, `OutputOf`, and dataset/process back-edges is never emitted. Genuinely unknown overflow properties continue to round-trip.

## Path Type

A `Path` type is part of the core data model. It represents a sequence of `Process` instances connected through their shared I/O nodes, i.e. a directed walk through the process graph. It is a **read-only view** — it has no CRUD API of its own and does not own the processes it references. It is produced by graph-traversal queries and exposes the ordered list of processes and the nodes connecting them.

It is basically the more capable continuation of the [ValueCollection](../references/ProcessCore/ValueCollection.fs) concept, but maintaining the full process context, allowing more than just retrieving property values.

## Querying

Querying methods are embedded directly on the entity objects (not in a separate query wrapper), following the pattern established by the reference query model (`ARCtrl.QueryModel.ProcessCore`).

### Annotation sources

When retrieving property values from a process context, four sources can contribute:

1. `process.ParameterValue` — parameters attached directly to the process
2. `process.Input?.AdditionalProperty` — characteristics attached to the optional input node
3. `process.Output?.AdditionalProperty` — factors attached to the optional output node
4. `process.ExecutesProtocol?.Components` — component annotations (equipment, reagents, software) attached to the recipe

All retrieval methods are named `*Annotations` (not `*ParameterValues`) to reflect this broad collection.

For graph traversal queries from an `IONode`, node-attached values are collected from the singular edge endpoints actually reached. A reached `Process` contributes its process-level `ParameterValue` and protocol `Components`; endpoint `AdditionalProperty` values come from the reached input/output nodes.

This applies equally when the member is called on `IONode`, `Sample`, or `Data`, and through dataset-scoped wrappers such as `UpstreamAnnotationsForNode` / `DownstreamAnnotationsForNode` / `AnnotationsForNode`. `Path.AllAnnotations` operates on the explicit process sequence supplied to the `Path` value and therefore collects from every present endpoint of those path processes.

### Traversal directions

Graph traversal methods come in three flavours:

- **Undirected** (`AllConnectedProcesses`, `AllConnectedNodes`, …) — BFS through both upstream and downstream edges simultaneously.
- **Upstream** (`UpstreamProcesses`, `UpstreamNodes`, …) — BFS following `OutputOf` back-edges to predecessor processes and each process's optional `Input` partner.
- **Downstream** (`DownstreamProcesses`, `DownstreamNodes`, …) — BFS following `InputOf` back-edges to successor processes and each process's optional `Output` partner.

### Deprecated in-memory positional I/O

The former plural `Inputs`/`Outputs`, positional N-to-N mapping, traversal fallback, and `Dataset.CollapseProcesses()` are deprecated and removed. Every in-memory process is one edge with optional singular endpoints. YAML still exposes plural arrays as a compact wire representation; expansion, positional padding, and serialization-time grouping are specified in [the YAML plan](yml_plan.md#process-io-relationships-singular-model-collapsed-yaml).

### Root and final nodes

A node is a **root** if no in-scope process produces it as output (`IsRootNode`).
A node is a **final** if no in-scope process consumes it as input (`IsFinalNode`).

### Data fragment selector support

See [reference paper pdf](../references/Fragment_Level_FAIRness.pdf) or [reference paper tex](../references/Fragment_Level_FAIRness.tex) for the rationale and design notes on this feature.

`Data` represents both whole data resources and selected fragments. Fragment-level addressing is expressed through `Path`, optional `Selector`, optional `SelectorFormat`, and optional `EncodingFormat`.

The core datamodel should provide a generic, extensible fragment-selector resolution layer for graph traversal and dataset queries. The core must not define a closed selector vocabulary. Selector-specific behavior is supplied by resolver/spec implementations registered by users or higher-level packages.

Resolver selection must consider `SelectorFormat`. A `Data` node that uses a `Selector` should provide a `SelectorFormat` value so the resolver layer can choose the correct selector implementation and avoid interpreting the same selector string under the wrong selector language.

Fragment-aware behavior is opt-in by resolver availability, with one generic exception: if two `Data` nodes have the same `Path` and only one has a `Selector`, the whole-resource node is treated as containing the selected-fragment node. If no resolver is registered for a selector format, all other comparisons fall back to exact matching (`Path` plus `Selector`). Unsupported, unknown, or intentionally opaque selector formats therefore remain backwards-compatible.

The first required semantic relations are:

- exact: both data nodes identify the same resource or fragment
- contains: one data node identifies a resource or fragment containing the other
- unknown/disjoint: no traversal relationship beyond exact fallback

Graph traversal should use these relations through resolver-aware query paths rather than by changing `Data` equality. `Data` equality and node registry keys should remain exact (`Path` plus `Selector`) so canonicalization stays deterministic. The API to the traversal methods should not be altered, but instead should always make use of the resolver layer when comparing `Data` nodes, if available.

Specific selector implementations should sit on top of the generic core mechanism. The first concrete resolver should target [RFC 7111 CSV selectors](https://datatracker.ietf.org/doc/html/rfc7111), because current examples use selectors such as `#col=1` and `#col=2-11`. Spreadsheet fragment selectors should be added as a separate resolver following the fragment-level FAIRness specification, not hard-coded into `Data`.


### Optional scope

All traversal methods accept an optional `scope: ResizeArray<Process>` parameter. When supplied, the BFS is restricted to processes within that set. This allows scoping queries to a single `Dataset` without creating a separate graph object.

### Placement of query methods

| Entity | Methods |
|--------|---------|
| `IONode` | Full traversal and Annotation retrieval; `IsRootNode`, `IsFinalNode` |
| `Sample`, `Data` | Delegate to `IONode` wrapper (`SampleNode this` / `DataNode this`) |
| `Process` | `InputSample`, `InputData`, `OutputSample`, `OutputData` (all option-returning), `ProtocolParameters`, `AnnotationsByName` |
| `Dataset` | `AllNodes`, `RootNodes`, `FinalNodes`, `AllAnnotations(?protocolName)`, `AnnotationsForNode`, `UpstreamAnnotationsForNode`, `DownstreamAnnotationsForNode`, `ProcessesForNode`, `PathsThrough`, `NodesUpstreamOf`, `NodesDownstreamOf`, `ConnectedSamplesForNode`, `ConnectedDataForNode`, `ProtocolParametersForNode`, `FindProcessesByProtocolType`, `FindProcessesByAnnotation`, `FindProcessesByPropertyName`, `SamplesResultingFromCondition` |
| `Path` | `AllAnnotations`, `AnnotationsByName`, `ProtocolParameters`, `TerminalInputs`, `TerminalOutputs` |

## Target folder

The core datamodel implementation will be located in `src/ProcessCore/`, around a [ProcessCore.fsproj](../src/ProcessCore/ProcessCore.fsproj) file.

## Out of Scope

The following are explicitly **not** part of this implementation:

- Serialization formats other than the ARC YAML persistence behavior documented above (JSON-LD, RO-Crate, XLSX, etc.)
- ISA decoration types (`Investigation`, `Study`, `Assay`, etc.)
- Workflow Run decoration types (`ArcWorkflow`, `ArcRun`, etc.)
- Datamap decoration types
- Validation logic
