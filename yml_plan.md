# Plan for creating a yml schema

The first version of the yml schema should contain schema files for the following entities:

- Data
- Dataset
- DefinedTerm
- Material
- Process
- PropertyValue
- Protocol

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

- **Use YAML Schema**: Or JSON Schema? Need to discuss

- **Follow core specification**: The schema will be designed to directly reflect the core entities and relationships defined in the ARC Data Model specification, ensuring that all required fields and constraints are represented.

- One file for each core entity (Data, Dataset, DefinedTerm, Material, Process, PropertyValue, Protocol) to keep things organized and modular.

- **References between schema files**: Use `$ref` to reference other schema files where entities are related (e.g. Process references Protocol, Dataset references Process).

- **Allow cross-referencing mechanism**: Use `id`(or `@id`) fields to allow entities to reference each other without embedding full objects, supporting a more relational structure. 
  - Need to decide on whether we allow `id` only in specific types or for all. Also 
  - Need to decide whether we define a generic mechanism to place collections of cross-referenced entities (e.g. a `registry` section) or allow them to be defined inline in the main document.

#### Model details

- Allow multiple objects and results per Process, i.e. process grouping? This would reduce file sizes tremendously? We need to see about impact on diffing

- Nest Data into datacontext objects?
