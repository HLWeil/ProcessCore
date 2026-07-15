# Plan for creating a yml schema

The YAML schema contains schema files for the following entities, defined in [the core specification](../docs/spec/core/README.md):

- Data
- Dataset
- DefinedTerm
- FormalParameter
- Recipe
- Process
- Sample
- Annotation

The schema files are placed in [schemas/yml](../schemas/yml/) and referenced in the documentation.

---

## YML Schema Implementation Plan

**Why YAML:**
- Human-readable and writable
- In this regard good diffability
- For diffability, we also want to keep the structure as flat as possible, avoiding deep nesting of objects. This favors a relational schema with references (e.g. `@id`) rather than embedding full objects within others.
- Supports complex data structures
- Widely used for configuration and data serialization
- Easy to integrate with various programming languages

**Alternatives considered and rejected:**
- RO-Crate JSON-LD: more verbose, great for FAIR packaging and interoperability but less convenient for day-to-day editing and querying.

### Design Approach

#### Technical Considerations

- **Use YAML Schema**: According to the [YAML Schema specification](../references/YAML%20Schema%20—%20ASDF%20Standard%201.6.0%20documentation.pdf), we can define a schema using YAML itself, specifying the expected structure, types, and constraints for each entity. This allows us to validate YAML documents against the schema and ensure they conform to the defined structure.

- **Follow core specification**: The schema will be designed to directly reflect the core entities and relationships defined in the ARC Data Model specification, ensuring that all required fields and constraints are represented.

- One file for each core entity (Data, Dataset, DefinedTerm, FormalParameter, Sample, Process, Annotation, Recipe) keeps the schema organized and modular.

- **References between schema files**: Use `$ref` to reference other schema files where entities are related (e.g. Process references Recipe, Dataset references Process).

- **Allow cross-referencing mechanism**: Use `id`(or `@id`) fields to allow entities to reference each other without embedding full objects, supporting a more relational structure.
  - Need to decide on whether we allow `id` only in specific types or for all. Also
  - Need to decide whether we define a generic mechanism to place collections of cross-referenced entities (e.g. a `registry` section) or allow them to be defined inline in the main document.

- **Allow extension**: Yes, as we need this for decorations. We can allow for additional properties using `additionalProperties: true` or a similar mechanism, while still enforcing the core structure.

- **Type value**: The value for the `type` field MUST be a string that corresponds to the name of the entity (e.g. "Data", "Dataset", "Process").

### Process I/O relationships: singular model, collapsed YAML

The core graph and the YAML document intentionally have different cardinalities at this boundary:

- In memory, one `Process` is one directed edge with `Input: IONode option` and `Output: IONode option`. Fan-in, fan-out, repeated lanes, and independent samples are represented by multiple process objects that may share endpoint nodes.
- On the wire, `Process.yml` keeps the plural `inputs` and `outputs` arrays. They are a compact, backwards-compatible serialization shape, not permission for plural endpoints in the datamodel.

#### Decoding YAML into edges

The Process codec returns a collection, even for the standalone `fromYamlString` API:

1. Decode `inputs` and `outputs` independently while preserving array positions. Inline `Sample`/`Data` objects become nodes; an unresolved id reference occupies its lane as `None`.
2. Let `edgeCount = max(inputs.Count, outputs.Count, 1)`.
3. Create one process for each index `0 .. edgeCount - 1`. Assign the input and output at that index when present and use `None` for the shorter side.
4. Copy `name`, `additionalType`, resolved protocol, parameter values, and lenient overflow properties to every process. Clone mutable nested protocol/annotation values so expanded edges can be edited independently.
5. Dataset decoding flattens every returned collection and calls `Dataset.AddProcess`, which establishes ownership, endpoint back-edges, and root-registry canonicalization.

Examples:

| YAML lane shape | Expanded processes |
|---|---|
| `inputs: [A]`, `outputs: [B]` | `(Some A, Some B)` |
| `inputs: [A, C]`, `outputs: [B, D]` | `(A, B)`, `(C, D)` |
| `inputs: [A, C]`, `outputs: [B]` | `(A, B)`, `(C, None)` |
| `inputs: []`, `outputs: [B]` | `(None, B)` |
| both omitted or empty | one `(None, None)` metadata-only process |

#### Encoding edges as collapsed YAML

Standalone Process encoding is deliberately literal: it emits omitted or one-element `inputs`/`outputs` arrays for its singular endpoints. Dataset encoding owns compact grouping:

1. Traverse `Dataset.Processes` in encounter order.
2. Group processes whose non-I/O state is structurally equal: name, additional type, executed protocol, parameter values, and overflow state must match.
3. Include endpoint-presence shape in the key. Both-sided, input-only, output-only, and endpoint-free edges never share a group; this prevents omission from changing lane alignment.
4. Preserve the first occurrence of each group and the within-group process order.
5. Emit one YAML Process mapping per group. Append each singular input and output to the plural arrays in that order. Indexed annotation and protocol references continue to be generated while groups are encoded.

The grouping key is serialization-only and does not deduplicate model processes: two equivalent edges remain two meaningful process instances after decoding and normal dataset CRUD. A decode → dataset encode → decode round trip must preserve the expanded graph, endpoint shape, and lane order even when the textual grouping changes.
