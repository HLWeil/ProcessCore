# ProcessCore — Query Model Reference

Previous implementation of a set of types and functions to query an ARC process graph. These F# source files are preserved as reference material from the ARCtrl codebase (`ARCtrl.QueryModel.ProcessCore` namespace) and are not modified by this project.

## Files

- [KnowledgeGraph.fs](KnowledgeGraph.fs) — Shared context and edge-label constants used across the query model.
- [PropertyValue.fs](PropertyValue.fs) — Queryable wrapper for a single property value node.
- [ValueCollection.fs](ValueCollection.fs) — Typed collections for filtering and accessing property values.
- [ProcessCollection.fs](ProcessCollection.fs) — Core queryable graph types: processes, protocols, and I/O nodes.

---

### [KnowledgeGraph.fs](KnowledgeGraph.fs)

Defines the shared linked-data context and the string constants used as edge labels when indexing process inputs and outputs into the graph.

**Constants / values:**

- `objectOf` — edge label added to input nodes pointing to the processes they are consumed by.
- `resultOf` — edge label added to output nodes pointing to the processes that produced them.
- `context` — the default `LDContext` combining the Bioschemas and ARC v1.2 base contexts.

---

### [PropertyValue.fs](PropertyValue.fs)

A queryable wrapper around a single `LDNode` that has been validated as a `PropertyValue`.

**Types:**

| Type | Description |
|------|-------------|
| `QPropertyValue` | Wraps an `LDNode` and exposes typed accessors for the name, value, unit, and category of a property value. Provides boolean flags (`IsCharacteristic`, `IsParameter`, `IsFactor`, `IsComponent`) to distinguish the ISA role of the value. |

---

### [ValueCollection.fs](ValueCollection.fs)

Typed, filterable collections for sets of `QPropertyValue` instances.

**Types:**

| Type | Description |
|------|-------------|
| `QValueCollection` | A collection of `QPropertyValue` items. Supports retrieval by index, name, or `OntologyAnnotation` category, and provides filtered sub-collections for characteristics, parameters, factors, and components. |
| `IOQValueCollection` | A collection of `QPropertyValue` items keyed by `(inputName, outputName)` pairs. Used to associate values with specific I/O node combinations across the process graph. |

---

### [ProcessCollection.fs](ProcessCollection.fs)

The main query layer over the process graph. Builds on `ARCtrl.ROCrate` graph nodes to provide traversal, filtering, and value extraction across the experimental workflow.

**Types:**

| Type | Description |
|------|-------------|
| `ProcessSequence` | A flat, indexed collection of `QLabProcess` instances. Provides fast lookup by process ID and serves as an optional pre-computed process index that can be passed to `QGraph` static methods to avoid repeated graph scans. |
| `QGraph` | The top-level queryable graph. Extends `LDGraph` and, on construction, eagerly indexes `objectOf`/`resultOf` back-edges onto every I/O node. Provides static traversal methods (forward, backward, root/final detection) and value-extraction helpers. |
| `QLabProcess` | A queryable wrapper for a single process node. Exposes typed access to input/output `IONode` lists, the associated `QLabProtocol`, and the `QValueCollection` of all parameter/characteristic/factor values for that step. |
| `QLabProtocol` | A queryable wrapper for a protocol node referenced by a `QLabProcess`. Exposes the protocol name and its component `QPropertyValue` list. |
| `IONode` | A queryable wrapper for a material or data node (source, sample, or file). Exposes name, type flags (`IsSample`, `IsSource`, `IsMaterial`, `IsFile`), and the node's additional properties as `QPropertyValue` instances. |
| `QDataContext` | A queryable wrapper for a dataset/context node. Provides access to the processes and I/O nodes that belong to a specific experimental context. |

