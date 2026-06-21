# Plan for creating a sql schema

The first version of the sql schema should contain tables for the following entities:

- Data
- Dataset
- DefinedTerm
- Sample
- Process
- Annotation
- Protocol

Additionally, a view called `Paths` should aggregate all FULL paths connected through processes, e.g.:

Dataset1:
A1 -> B1 -> C1
A1 -> B2

Dataset2:
C1 -> D1
A3 -> B3

results in

Paths:
A1 -> B1 -> C1
A1 -> B2
C1 -> D1
A3 -> B3

The following decorations should also be added to the schema:

- ISA
  - Assay and Study Dataset decorations
  - Annotation Subtype decorations
- Workflow Run
  - Workflow Invocation decoration
  - Workflow Run Dataset decoration
  - Annotation Subtype decorations

Open questions (do not address in this plan):

- how to represent the container entity (e.g. Investigation, ROCrate root entity)

---

## SQL Schema Implementation Plan

### SQL Dialect: SQLite

**Why SQLite:**
- Zero configuration, single-file database — no server to install or manage
- Ships with Python (`sqlite3`), has bindings for virtually every language
- Supports recursive CTEs (required for the `Paths` view)
- Supports views (required for decorations)
- Ideal for local/embedded use cases
- Excellent tooling: DB Browser for SQLite, DBeaver, VS Code extensions, CLI

**Alternatives considered and rejected:**
- PostgreSQL / MySQL: require server installation, overkill for local use
- DuckDB: good for analytics but less mature for schema-driven relational work

### Design Approach

- **Core tables** store all properties, including decoration-specific columns as nullable. This avoids complex table-per-subtype joins while keeping queries simple.
- **Junction tables** model all M:N relationships (process I/O, additionalProperty, etc.). Polymorphic I/O (Sample vs Data) uses separate junction tables per type rather than a type discriminator column, giving us proper foreign keys.
- **Decoration views** are filtered SELECTs on core tables by `additionalType`, presenting the domain-specific perspective.
- **Paths view** uses a recursive CTE over a helper `ProcessEdge` view to trace maximal chains through the process graph.

### Core Entity Tables (7)

#### `DefinedTerm`
| Column | Type | Constraint | Source |
|--------|------|------------|--------|
| id | TEXT | PK | `@id` |
| type | TEXT | NOT NULL | `@type` |
| name | TEXT | NOT NULL | `name` |
| term_code | TEXT | | `termCode` |
| in_defined_term_set | TEXT | | `inDefinedTermSet` |

#### `Annotation`
| Column | Type | Constraint | Source |
|--------|------|------------|--------|
| id | TEXT | PK | `@id` |
| type | TEXT | NOT NULL DEFAULT 'Annotation' | `@type` |
| additional_type | TEXT | | `additionalType` — discriminator for subtypes |
| name | TEXT | NOT NULL | `name` |
| value | TEXT | | `value` (stored as text) |
| property_id | TEXT | | `propertyID` (URL) |
| unit_code | TEXT | | `unitCode` (URL) |
| unit_text | TEXT | | `unitText` |
| value_reference | TEXT | | `valueReference` (URL) |
| example_of_work | TEXT | | WR: `exampleOfWork` (FormalParameter ref) |

#### `Protocol`
| Column | Type | Constraint | Source |
|--------|------|------------|--------|
| id | TEXT | PK | `@id` |
| type | TEXT | NOT NULL | `@type` |
| additional_type | TEXT | | decoration discriminator |
| name | TEXT | | `name` |
| description | TEXT | | `description` |
| intended_use_id | TEXT | FK -> DefinedTerm | `intendedUse` |
| version | TEXT | | `version` |
| url | TEXT | | `url` |
| programming_language | TEXT | | WR: `programmingLanguage` |

#### `Sample`
| Column | Type | Constraint | Source |
|--------|------|------------|--------|
| id | TEXT | PK | `@id` |
| type | TEXT | NOT NULL | `@type` |
| name | TEXT | NOT NULL | `name` |
| additional_type | TEXT | | ISA: `Sample` / `Source` |

#### `Data`
| Column | Type | Constraint | Source |
|--------|------|------------|--------|
| id | TEXT | PK | `@id` |
| type | TEXT | NOT NULL | `@type` |
| name | TEXT | NOT NULL | `name` |
| encoding_format | TEXT | | `encodingFormat` |
| disambiguating_description | TEXT | | `disambiguatingDescription` |

#### `Dataset`
| Column | Type | Constraint | Source |
|--------|------|------------|--------|
| id | TEXT | PK | `@id` |
| type | TEXT | NOT NULL DEFAULT 'Dataset' | `@type` |
| additional_type | TEXT | NOT NULL | `additionalType` discriminator |
| identifier | TEXT | NOT NULL | `identifier` |
| name | TEXT | | `name` |
| description | TEXT | | `description` |
| license | TEXT | | ISA Investigation: `license` |
| date_published | TEXT | | ISA Investigation: `datePublished` |
| date_created | TEXT | | ISA Investigation: `dateCreated` |
| measurement_method | TEXT | | ISA Assay / WR ARC Run |
| measurement_technique | TEXT | | ISA Assay / WR ARC Run |
| variable_measured | TEXT | | ISA Assay / WR ARC Run |
| main_entity_id | TEXT | FK -> Protocol | WR ARC Workflow: `mainEntity` |
| conforms_to | TEXT | | WR ARC Run: `conformsTo` |

#### `Process`
| Column | Type | Constraint | Source |
|--------|------|------------|--------|
| id | TEXT | PK | `@id` |
| type | TEXT | NOT NULL | `@type` |
| name | TEXT | NOT NULL | `name` |
| additional_type | TEXT | | decoration discriminator |
| executes_protocol_id | TEXT | FK -> Protocol | `executesProtocol` |
| end_time | TEXT | | `endTime` |
| disambiguating_description | TEXT | | ISA Process: comments |
| description | TEXT | | WR WorkflowInvocation: execution details |

### Junction / Relationship Tables (16)

**Process I/O** (polymorphic, split by target type):
- `ProcessObjectSample` (process_id FK, sample_id FK) — PK both
- `ProcessObjectData` (process_id FK, data_id FK) — PK both
- `ProcessResultSample` (process_id FK, sample_id FK) — PK both
- `ProcessResultData` (process_id FK, data_id FK) — PK both

**Process -> Annotation:**
- `ProcessParameterValue` (process_id FK, propertyvalue_id FK)
- `ProcessAdditionalProperty` (process_id FK, propertyvalue_id FK)

**Dataset relationships:**
- `DatasetAbout` (dataset_id FK, process_id FK) — Dataset.about -> Process
- `DatasetHasPartData` (dataset_id FK, data_id FK) — Dataset.hasPart -> Data
- `DatasetHasPartDataset` (parent_id FK, child_id FK) — Dataset.hasPart -> Dataset

**AdditionalProperty per entity:**
- `DataAdditionalProperty` (data_id FK, propertyvalue_id FK)
- `DatasetAdditionalProperty` (dataset_id FK, propertyvalue_id FK)
- `SampleAdditionalProperty` (sample_id FK, propertyvalue_id FK)
- `ProtocolAdditionalProperty` (protocol_id FK, propertyvalue_id FK)
- `DefinedTermAdditionalProperty` (definedterm_id FK, propertyvalue_id FK)

**Other:**
- `SampleDerivesFrom` (sample_id FK, source_sample_id FK)
- `ProtocolComponent` (protocol_id FK, propertyvalue_id FK, role TEXT) — ISA Plan: labEquipment/reagent/computationalTool

### Decoration Views

**ISA decoration views** — filtered SELECTs on core tables:
- `Investigation` — Dataset WHERE additional_type = 'Investigation'
- `Study` — Dataset WHERE additional_type = 'Study'
- `Assay` — Dataset WHERE additional_type = 'Assay'
- `ParameterValue` — Annotation WHERE additional_type = 'ParameterValue'
- `CharacteristicValue` — Annotation WHERE additional_type = 'CharacteristicValue'
- `FactorValue` — Annotation WHERE additional_type = 'FactorValue'
- `Component` — Annotation WHERE additional_type = 'Component'

**Workflow Run decoration views:**
- `ArcWorkflow` — Dataset WHERE additional_type = 'ARC Workflow'
- `ArcRun` — Dataset WHERE additional_type = 'ARC Run'
- `WorkflowInvocation` — Process WHERE additional_type = 'Workflow Invocation'
- `WorkflowInput` — Annotation WHERE additional_type = 'Workflow Input'

### Paths View

The `Paths` view traces all maximal chains through the process graph — from root nodes (never a result of any process) to leaf nodes (never an object of any process).

**Implementation using helper views + recursive CTE:**

1. **`NodeRef`** — unifies Sample and Data into a single "node" reference:
   ```sql
   SELECT 'Sample' AS node_type, id, name FROM Sample
   UNION ALL
   SELECT 'Data', id, name FROM Data
   ```

2. **`ProcessEdge`** — all (object -> result) pairs through processes:
   ```
   Cross-join process objects x process results per process,
   across all 4 type combinations (Mat->Mat, Mat->Data, Data->Mat, Data->Data)
   ```

3. **`Paths`** — recursive CTE:
   - **Base case**: root nodes (appear as process objects but never as process results)
   - **Recursive step**: follow ProcessEdge from current node to next result, appending node name to path string
   - **Terminal filter**: only return rows where the last node is a leaf (never appears as ProcessEdge.object)
   - **Safety**: depth limit of 100 to prevent infinite loops
   - **Returns**: `path TEXT` — e.g. `"Base Culture -> Cultivation Flask RT -> Eppi RT 1 -> sample1.raw"`

### Files to Create

- **`schema/sqlite/001_core.sql`** — single migration file containing:
  1. PRAGMA statements (foreign_keys = ON, journal_mode = WAL)
  2. Core entity tables with CHECK constraints on required fields
  3. Junction tables with composite PKs and FK constraints
  4. Helper views (NodeRef, ProcessEdge)
  5. Decoration views (ISA + Workflow Run)
  6. Paths view (recursive CTE)
  7. Indexes on foreign key columns and additionalType discriminators

- **`schema/sqlite/seed_example.sql`** — proteomics assay example data for verification

### Verification

```bash
sqlite3 :memory: < schema/sqlite/001_core.sql < schema/sqlite/seed_example.sql
# Then:
#   SELECT * FROM Paths;
#   SELECT * FROM Assay;
#   SELECT * FROM ParameterValue;
```

Paths view should produce chains matching the proteomics example:
- `Base Culture -> Cultivation Flask RT -> Eppi RT 1 -> sample1.raw -> proteomics_result.csv#col=12`
- `Base Culture -> Cultivation Flask HT -> Eppi HT 1 -> sample4.raw -> proteomics_result.csv#col=15`
- etc.