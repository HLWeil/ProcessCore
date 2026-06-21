# Plan for creating a yml schema

The first version of the yml schema should contain schema files for the following entities, defined in [the core specification](spec/core/README.md):

- Data
- Dataset
- DefinedTerm
- FormalParameter
- Plan
- Process
- Sample
- Annotation

The schema files should be then placed in [schemas/yml](schemas/yml) and referenced in the documentation.

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
- RO-Crate JSON-LD: more verbose, great for FAIR packaging and interopability but less convenient for day-to-day editing and querying.

### Design Approach

#### Technical Considerations

- **Use YAML Schema**: According to the [YAML Schema specification](references/YAML%20Schema%20—%20ASDF%20Standard%201.6.0%20documentation.pdf), we can define a schema using YAML itself, specifying the expected structure, types, and constraints for each entity. This allows us to validate YAML documents against the schema and ensure they conform to the defined structure.

- **Follow core specification**: The schema will be designed to directly reflect the core entities and relationships defined in the ARC Data Model specification, ensuring that all required fields and constraints are represented.

- One file for each core entity (Data, Dataset, DefinedTerm, Sample, Process, Annotation, Protocol) to keep things organized and modular.

- **References between schema files**: Use `$ref` to reference other schema files where entities are related (e.g. Process references Protocol, Dataset references Process).

- **Allow cross-referencing mechanism**: Use `id`(or `@id`) fields to allow entities to reference each other without embedding full objects, supporting a more relational structure.
  - Need to decide on whether we allow `id` only in specific types or for all. Also
  - Need to decide whether we define a generic mechanism to place collections of cross-referenced entities (e.g. a `registry` section) or allow them to be defined inline in the main document.

- **Allow extension**: Yes, as we need this for decorations. We can allow for additional properties using `additionalProperties: true` or a similar mechanism, while still enforcing the core structure.

- **Type value**: The value for the `type` field MUST be a string that corresponds to the name of the entity (e.g. "Data", "Dataset", "Process").