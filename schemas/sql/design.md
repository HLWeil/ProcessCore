# Core SQL Schema — Design

A SQLite relational mapping of the [ProcessCore model](../../spec/core/README.md). Scope: the core entities only. ISA, Workflow Run, and Datamap decorations layer on top later via the `additional_type` discriminator.

## Design Decisions

- **Primary keys** — every entity table uses `TEXT PRIMARY KEY`. Where the spec marks `id` as `MUST`, it is taken directly from the source. Where `id` is `COULD` (`LabProcess`, `LabProtocol`, `Data`), the application generates a local UUID at insert time.
- **`type` column omitted** — each table implicitly encodes its schema.org type. The `type` field from the spec is never stored.
- **`additional_type`** — kept as a nullable `TEXT` on every entity. This is the hook for future decorations and stays out of the way for plain core data.
- **1:N via FK on the child** — used when a child logically belongs to exactly one parent (e.g. `FormalParameter` → `LabProtocol`, `Dataset` → `Dataset` for `hasPart`).
- **Other list-valued relations via junction tables** — each list-valued property gets its own table with a `position INTEGER` column to preserve order.
- **`inputs`/`outputs` are split by target type** — because `LabProcess` inputs and outputs may be either `Material` or `Data`, each direction is split into two junction tables (4 total). This keeps FKs strict and avoids polymorphic references. The `position` column is the mechanism that preserves the spec's "input[N] corresponds to output[N]" contract — readers reconstruct the ordered sequences by sorting on `position` across both target tables.
- **`PropertyValue` is a first-class entity** — it has its own PK, and its associations to owners (`Dataset`, `Material`, `Data`, `LabProtocol`, `LabProcess`) live in separate junction tables. The schema permits a `PropertyValue` to be referenced from multiple owners; in practice each will typically have one owner.
- **No path/closure tables** — process paths are derivable from the I/O junction tables and may be added later as a view or a materialized closure table.

## Schema Overview

```mermaid
erDiagram
    dataset ||--o{ dataset : "hasPart (parent_id)"
    dataset ||--o{ dataset_process : ""
    lab_process ||--o{ dataset_process : ""
    lab_process }o--o| lab_protocol : "executes_protocol_id"
    lab_protocol ||--o{ formal_parameter : "parameters"
    lab_protocol }o--o| defined_term : "intended_use_id"
    formal_parameter }o--o| defined_term : "default_value_id"
    property_value }o--o| formal_parameter : "instance_of_id"
    lab_process ||--o{ process_input_material : ""
    material ||--o{ process_input_material : ""
    lab_process ||--o{ process_input_data : ""
    data ||--o{ process_input_data : ""
    lab_process ||--o{ process_output_material : ""
    material ||--o{ process_output_material : ""
    lab_process ||--o{ process_output_data : ""
    data ||--o{ process_output_data : ""
    lab_process ||--o{ process_parameter_value : ""
    property_value ||--o{ process_parameter_value : ""
    dataset ||--o{ dataset_additional_property : ""
    property_value ||--o{ dataset_additional_property : ""
    lab_protocol ||--o{ protocol_additional_property : ""
    property_value ||--o{ protocol_additional_property : ""
    material ||--o{ material_additional_property : ""
    property_value ||--o{ material_additional_property : ""
    data ||--o{ data_additional_property : ""
    property_value ||--o{ data_additional_property : ""
```

## Creation Order

FK dependencies require this order:

1. `defined_term`
2. `lab_protocol`
3. `formal_parameter`
4. `dataset`
5. `lab_process`
6. `material`
7. `data`
8. `property_value`
9. Junction tables (any order)

---

## Entity Tables

### `defined_term`

Ontology term annotation. Pure leaf — no outgoing FKs.

```mermaid
erDiagram
    defined_term {
        TEXT id PK
        TEXT name
        TEXT tan
        TEXT in_defined_term_set
    }
    lab_protocol }o--o| defined_term : "intended_use_id"
    formal_parameter }o--o| defined_term : "default_value_id"
```

| Column | Type | Constraint | Source |
|---|---|---|---|
| `id` | TEXT | PK | spec `id` (MUST) |
| `name` | TEXT | NOT NULL | spec `name` |
| `tan` | TEXT | nullable | spec `TAN` |
| `in_defined_term_set` | TEXT | nullable | spec `inDefinedTermSet` (URL form) |

---

### `lab_protocol`

Planned procedure.

```mermaid
erDiagram
    lab_protocol {
        TEXT id PK
        TEXT additional_type
        TEXT name
        TEXT description
        TEXT version
        TEXT url
        TEXT intended_use_id FK
        TEXT intended_use_text
    }
    lab_protocol }o--o| defined_term : "intended_use_id"
    lab_protocol ||--o{ formal_parameter : "parameters"
    lab_protocol ||--o{ protocol_additional_property : ""
    lab_process }o--o| lab_protocol : "executes_protocol_id"
```

| Column | Type | Constraint | Source |
|---|---|---|---|
| `id` | TEXT | PK | spec `id` (COULD; app-generated if absent) |
| `additional_type` | TEXT | nullable | spec `additionalType` |
| `name` | TEXT | nullable | spec `name` |
| `description` | TEXT | nullable | spec `description` |
| `version` | TEXT | nullable | spec `version` |
| `url` | TEXT | nullable | spec `url` |
| `intended_use_id` | TEXT | FK → `defined_term.id` | spec `intendedUse` (DefinedTerm form) |
| `intended_use_text` | TEXT | nullable | spec `intendedUse` (Text form) |

`intended_use_id` and `intended_use_text` are mutually exclusive. The application enforces this; the schema does not.

---

### `formal_parameter`

Named parameter slot owned by exactly one protocol.

```mermaid
erDiagram
    formal_parameter {
        TEXT id PK
        TEXT protocol_id FK
        TEXT name
        TEXT name_tan
        TEXT default_value_id FK
    }
    lab_protocol ||--o{ formal_parameter : "parameters"
    formal_parameter }o--o| defined_term : "default_value_id"
    property_value }o--o| formal_parameter : "instance_of_id"
```

| Column | Type | Constraint | Source |
|---|---|---|---|
| `id` | TEXT | PK | spec `id` (MUST) |
| `protocol_id` | TEXT | FK → `lab_protocol.id` | inverse of `LabProtocol.parameters` |
| `name` | TEXT | nullable | spec `name` |
| `name_tan` | TEXT | nullable | spec `nameTAN` (URL) |
| `default_value_id` | TEXT | FK → `defined_term.id` | spec `defaultValue` |

A FK on the child suffices because each `FormalParameter` belongs to exactly one `LabProtocol`.

---

### `dataset`

Container for processes; nestable via self-referential `parent_id`.

```mermaid
erDiagram
    dataset {
        TEXT id PK
        TEXT additional_type
        TEXT identifier
        TEXT name
        TEXT description
        TEXT parent_id FK
    }
    dataset ||--o{ dataset : "hasPart (parent_id)"
    dataset ||--o{ dataset_process : ""
    dataset ||--o{ dataset_additional_property : ""
```

| Column | Type | Constraint | Source |
|---|---|---|---|
| `id` | TEXT | PK | spec `id` (MUST) |
| `additional_type` | TEXT | nullable | spec `additionalType` |
| `identifier` | TEXT | NOT NULL | spec `identifier` (MUST) |
| `name` | TEXT | nullable | spec `name` |
| `description` | TEXT | nullable | spec `description` |
| `parent_id` | TEXT | FK → `dataset.id`, nullable | inverse of `hasPart`; NULL = top-level |

`hasPart` is encoded as the inverse FK on the child. No junction table required.

---

### `lab_process`

Transformation node.

```mermaid
erDiagram
    lab_process {
        TEXT id PK
        TEXT additional_type
        TEXT name
        TEXT executes_protocol_id FK
    }
    lab_process }o--o| lab_protocol : "executes_protocol_id"
    lab_process ||--o{ dataset_process : ""
    lab_process ||--o{ process_input_material : ""
    lab_process ||--o{ process_input_data : ""
    lab_process ||--o{ process_output_material : ""
    lab_process ||--o{ process_output_data : ""
    lab_process ||--o{ process_parameter_value : ""
```

| Column | Type | Constraint | Source |
|---|---|---|---|
| `id` | TEXT | PK | spec `id` (COULD; app-generated if absent) |
| `additional_type` | TEXT | nullable | spec `additionalType` |
| `name` | TEXT | NOT NULL | spec `name` (MUST) |
| `executes_protocol_id` | TEXT | FK → `lab_protocol.id`, nullable | spec `executesProtocol` |

---

### `material`

Biological / chemical / digital material.

```mermaid
erDiagram
    material {
        TEXT id PK
        TEXT additional_type
        TEXT name
    }
    material ||--o{ process_input_material : ""
    material ||--o{ process_output_material : ""
    material ||--o{ material_additional_property : ""
```

| Column | Type | Constraint | Source |
|---|---|---|---|
| `id` | TEXT | PK | spec `id` (MUST) |
| `additional_type` | TEXT | nullable | spec `additionalType` |
| `name` | TEXT | NOT NULL | spec `name` (MUST) |

---

### `data`

File or fragment.

```mermaid
erDiagram
    data {
        TEXT id PK
        TEXT additional_type
        TEXT path
        TEXT selector
        TEXT selector_format
        TEXT encoding_format
    }
    data ||--o{ process_input_data : ""
    data ||--o{ process_output_data : ""
    data ||--o{ data_additional_property : ""
```

| Column | Type | Constraint | Source |
|---|---|---|---|
| `id` | TEXT | PK | spec `id` (COULD; app-generated if absent) |
| `additional_type` | TEXT | nullable | spec `additionalType` |
| `path` | TEXT | NOT NULL | spec `path` (MUST) |
| `selector` | TEXT | nullable | spec `selector` |
| `selector_format` | TEXT | nullable | spec `selectorFormat` (URL) |
| `encoding_format` | TEXT | nullable | spec `encodingFormat` |

---

### `property_value`

Key-value-unit triple. Owned via junction tables.

```mermaid
erDiagram
    property_value {
        TEXT id PK
        TEXT additional_type
        TEXT name
        TEXT value
        TEXT unit
        TEXT name_tan
        TEXT value_tan
        TEXT unit_tan
        TEXT instance_of_id FK
    }
    property_value }o--o| formal_parameter : "instance_of_id"
    property_value ||--o{ dataset_additional_property : ""
    property_value ||--o{ protocol_additional_property : ""
    property_value ||--o{ material_additional_property : ""
    property_value ||--o{ data_additional_property : ""
    property_value ||--o{ process_parameter_value : ""
```

| Column | Type | Constraint | Source |
|---|---|---|---|
| `id` | TEXT | PK | spec `id` (MUST) |
| `additional_type` | TEXT | nullable | spec `additionalType` |
| `name` | TEXT | NOT NULL | spec `name` (MUST) |
| `value` | TEXT | nullable | spec `value` (numbers serialized as text) |
| `unit` | TEXT | nullable | spec `unit` |
| `name_tan` | TEXT | nullable | spec `nameTAN` (URL) |
| `value_tan` | TEXT | nullable | spec `valueTAN` (URL) |
| `unit_tan` | TEXT | nullable | spec `unitTAN` (URL) |
| `instance_of_id` | TEXT | FK → `formal_parameter.id`, nullable | spec `instanceOf` |

---

## Junction Tables

All junctions use a composite primary key `(owner_id, target_id)` and carry a `position INTEGER NOT NULL` column to preserve list order.

### `dataset_process` — `Dataset.processes` → `LabProcess`

```mermaid
erDiagram
    dataset_process {
        TEXT dataset_id PK,FK
        TEXT process_id PK,FK
        INTEGER position
    }
    dataset ||--o{ dataset_process : ""
    lab_process ||--o{ dataset_process : ""
```

### `dataset_additional_property` — `Dataset.additionalProperty` → `PropertyValue`

```mermaid
erDiagram
    dataset_additional_property {
        TEXT dataset_id PK,FK
        TEXT property_value_id PK,FK
        INTEGER position
    }
    dataset ||--o{ dataset_additional_property : ""
    property_value ||--o{ dataset_additional_property : ""
```

### `process_input_material` — `LabProcess.inputs` → `Material`

```mermaid
erDiagram
    process_input_material {
        TEXT process_id PK,FK
        TEXT material_id PK,FK
        INTEGER position
    }
    lab_process ||--o{ process_input_material : ""
    material ||--o{ process_input_material : ""
```

### `process_input_data` — `LabProcess.inputs` → `Data`

```mermaid
erDiagram
    process_input_data {
        TEXT process_id PK,FK
        TEXT data_id PK,FK
        INTEGER position
    }
    lab_process ||--o{ process_input_data : ""
    data ||--o{ process_input_data : ""
```

### `process_output_material` — `LabProcess.outputs` → `Material`

```mermaid
erDiagram
    process_output_material {
        TEXT process_id PK,FK
        TEXT material_id PK,FK
        INTEGER position
    }
    lab_process ||--o{ process_output_material : ""
    material ||--o{ process_output_material : ""
```

### `process_output_data` — `LabProcess.outputs` → `Data`

```mermaid
erDiagram
    process_output_data {
        TEXT process_id PK,FK
        TEXT data_id PK,FK
        INTEGER position
    }
    lab_process ||--o{ process_output_data : ""
    data ||--o{ process_output_data : ""
```

The `position` column on the four I/O junctions, taken together, is what preserves the input[N] ↔ output[N] correspondence guaranteed by the spec. Readers reconstruct the ordered sequences by sorting all rows for a given `process_id` across both the `material` and `data` variants, on `position`.

### `process_parameter_value` — `LabProcess.parameterValue` → `PropertyValue`

```mermaid
erDiagram
    process_parameter_value {
        TEXT process_id PK,FK
        TEXT property_value_id PK,FK
        INTEGER position
    }
    lab_process ||--o{ process_parameter_value : ""
    property_value ||--o{ process_parameter_value : ""
```

### `protocol_additional_property` — `LabProtocol.additionalProperty` → `PropertyValue`

```mermaid
erDiagram
    protocol_additional_property {
        TEXT protocol_id PK,FK
        TEXT property_value_id PK,FK
        INTEGER position
    }
    lab_protocol ||--o{ protocol_additional_property : ""
    property_value ||--o{ protocol_additional_property : ""
```

### `material_additional_property` — `Material.additionalProperty` → `PropertyValue`

```mermaid
erDiagram
    material_additional_property {
        TEXT material_id PK,FK
        TEXT property_value_id PK,FK
        INTEGER position
    }
    material ||--o{ material_additional_property : ""
    property_value ||--o{ material_additional_property : ""
```

### `data_additional_property` — `Data.additionalProperty` → `PropertyValue`

```mermaid
erDiagram
    data_additional_property {
        TEXT data_id PK,FK
        TEXT property_value_id PK,FK
        INTEGER position
    }
    data ||--o{ data_additional_property : ""
    property_value ||--o{ data_additional_property : ""
```

---

## Counts

8 entity tables + 10 junction tables = **18 tables**.

## Out of Scope

- ISA, Workflow Run, and Datamap decorations
- Process path views or closure tables (deferred; derivable from the I/O junctions)
- `Dataset.creator` → `Person` (appears in the spec diagram but not the property table)
- `FormalParameter.workExample` (appears in the spec diagram but not the property table)
- `value` numeric typing — `PropertyValue.value` is stored as `TEXT`. A future `value_numeric REAL` column with app-level dispatch could be added if numeric range queries become a requirement.
