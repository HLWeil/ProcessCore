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

- `objectOf` — the set of `LabProcess` instances for which this node is an **input**.
- `resultOf` — the set of `LabProcess` instances for which this node is an **output**.

These back-edges must be kept consistent eagerly: whenever a process's `inputs` or `outputs` list is mutated (add/remove), the corresponding back-edge on the affected node is updated in the same operation. This allows O(1) lookup of "which processes consume/produce this node" without scanning all processes.

## Path Type

A `Path` type is part of the core data model. It represents a sequence of `LabProcess` instances connected through their shared I/O nodes, i.e. a directed walk through the process graph. It is a **read-only view** — it has no CRUD API of its own and does not own the processes it references. It is produced by graph-traversal queries and exposes the ordered list of processes and the nodes connecting them.

It is basically the more capable continuation of the [ValueCollection](../references/ProcessCore/ValueCollection.fs) concept, but maintaining the full process context, allowing more than just retrieving property values.

## Out of Scope

The following are explicitly **not** part of this implementation:

- Any serialization or deserialization (JSON-LD, RO-Crate, XLSX, YAML, etc.)
- ISA decoration types (`Investigation`, `Study`, `Assay`, etc.)
- Workflow Run decoration types (`ArcWorkflow`, `ArcRun`, etc.)
- Datamap decoration types
- Validation logic