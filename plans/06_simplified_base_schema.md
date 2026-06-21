# Plan: Simplified Base SQL Schema

## Goal

Define a new base SQL schema from scratch that is intentionally minimal.

It should contain only these tables:

- `Dataset`
- `Process`
- `Sample`
- `Data`
- `Annotation`

Core rule:

- `Process` is the edge list of the process graph

Core query feature:

- a `Paths` view aggregates every full root-to-leaf path in that graph

This schema is meant to be simple and queryable first. It is not trying to capture the full ARC model in one step.

## Design Principles

1. Keep the number of tables as small as possible.
2. Make graph traversal obvious from the schema itself.
3. Avoid helper junction tables.
4. Put dataset scoping directly on the rows instead of adding extra relation tables.
5. Keep parameters queryable without introducing many subtype tables.
6. Optimize for lineage/path queries over full semantic fidelity.

## Proposed Tables

### `Dataset`

Minimal container for one process graph.

```sql
CREATE TABLE Dataset (
    id          TEXT PRIMARY KEY,
    name        TEXT NOT NULL,
    description TEXT
);
```

Purpose:

- identifies a graph boundary
- gives a human-readable name for a graph
- conceptually contains the list of processes that belong to that graph

Representation of the process list:

- the dataset's process list is represented by all rows in `Process` with the same `dataset_id`
- no separate junction table is needed in the simplified schema

### `Sample`

Represents physical or conceptual sample nodes in the graph.

```sql
CREATE TABLE Sample (
    id          TEXT PRIMARY KEY,
    dataset_id  TEXT NOT NULL REFERENCES Dataset(id),
    name        TEXT NOT NULL,
    kind        TEXT
);
```

Purpose:

- stores sample nodes such as sources and samples
- stays intentionally generic

Notes:

- `kind` is optional and can later hold values such as `Source` or `Sample`
- no extra metadata tables in this simplified version

### `Data`

Represents file-like or fragment-like data nodes in the graph.

```sql
CREATE TABLE Data (
    id              TEXT PRIMARY KEY,
    dataset_id      TEXT NOT NULL REFERENCES Dataset(id),
    path            TEXT NOT NULL,
    selector        TEXT,
    encoding_format TEXT
);
```

Purpose:

- stores data nodes such as files or file fragments

Notes:

- `selector` is optional, so both whole files and fragments fit in the same table
- keep this flat and avoid extra metadata in the simplified base schema

### `Process`

Represents one directed edge in the process graph.

```sql
CREATE TABLE Process (
    id           TEXT PRIMARY KEY,
    dataset_id   TEXT NOT NULL REFERENCES Dataset(id),
    name         TEXT NOT NULL,
    input_type   TEXT NOT NULL CHECK (input_type IN ('Sample', 'Data')),
    input_id     TEXT NOT NULL,
    output_type  TEXT NOT NULL CHECK (output_type IN ('Sample', 'Data')),
    output_id    TEXT NOT NULL
);
```

Purpose:

- one row = one input -> output mapping
- each row belongs to exactly one dataset
- pooling is many rows with the same output
- splitting is many rows with the same input

Examples:

- pool:
  - `A -> C`
  - `B -> C`
- split:
  - `A -> B`
  - `A -> C`

### `Annotation`

Stores parameters in the simplified schema.

It should be able to attach either:

- to a `Process`, for process parameters such as temperature
- to a `Dataset`, for dataset-level parameters

```sql
CREATE TABLE Annotation (
    id          TEXT PRIMARY KEY,
    dataset_id  TEXT NOT NULL REFERENCES Dataset(id),
    owner_type  TEXT NOT NULL CHECK (owner_type IN ('Dataset', 'Process')),
    owner_id    TEXT NOT NULL,
    name        TEXT NOT NULL,
    value       TEXT,
    unit        TEXT
);
```

Purpose:

- keep one generic parameter table
- avoid separate `ProcessParameterValue` and `DatasetParameterValue` tables
- support path-based parameter queries and dataset-level parameter lookup

Important interpretation:

- if `owner_type = 'Process'`, the row is a parameter of one process edge
- if `owner_type = 'Dataset'`, the row is a parameter of the dataset as a whole

## Recommended Indexes

```sql
CREATE INDEX idx_sample_dataset ON Sample(dataset_id);
CREATE INDEX idx_data_dataset     ON Data(dataset_id);
CREATE INDEX idx_process_dataset  ON Process(dataset_id);
CREATE INDEX idx_process_input    ON Process(dataset_id, input_type, input_id);
CREATE INDEX idx_process_output   ON Process(dataset_id, output_type, output_id);
CREATE INDEX idx_property_dataset ON Annotation(dataset_id);
CREATE INDEX idx_property_owner   ON Annotation(dataset_id, owner_type, owner_id);
CREATE INDEX idx_property_name    ON Annotation(dataset_id, name);
```

These are enough for:

- path traversal
- upstream/downstream lookup
- filtering paths within one dataset
- listing all processes in a dataset
- querying process parameters along a path
- querying dataset parameters from any leaf node

## Helper View: `NodeRef`

The schema should still have only four tables, but one helper view makes querying much easier.

```sql
CREATE VIEW NodeRef AS
SELECT
    dataset_id,
    'Sample' AS node_type,
    id AS node_id,
    name AS node_name
FROM Sample
UNION ALL
SELECT
    dataset_id,
    'Data' AS node_type,
    id AS node_id,
    CASE
        WHEN selector IS NOT NULL THEN path || '#' || selector
        ELSE path
    END AS node_name
FROM Data;
```

Purpose:

- gives one common node surface for path queries
- avoids repeating sample/data display logic in every recursive query

## Dataset Contains Processes

In the simplified schema, `Dataset` should be understood as containing a list of processes.

In relational form, that list is represented as:

```sql
SELECT *
FROM Process
WHERE dataset_id = ?;
```

So the containment rule is:

- `Dataset` owns the graph
- `Process` rows are the members of that graph
- `Sample` and `Data` rows are the nodes used by those process edges

This keeps the model simple while still matching the idea that a dataset contains its processes.

## `Paths` View

### Goal

`Paths` should return every full path from a root node to a leaf node inside a dataset.

Definition:

- root node: appears as a process input, but never as a process output in the same dataset
- leaf node: appears as a process output, but never as a process input in the same dataset

### Suggested Shape

```sql
Paths(
    dataset_id,
    path_id,
    length,
    root_type,
    root_id,
    leaf_type,
    leaf_id,
    path_rendered
)
```

### Suggested Implementation

Use a recursive CTE directly over `Process`.

Base step:

- start from all process rows whose input node is a root node

Recursive step:

- continue where the next process input matches the current process output
- stay within the same dataset

Terminal step:

- keep only walks whose current output node has no further outgoing process

### Sketch

```sql
CREATE VIEW Paths AS
WITH RECURSIVE walk(
    dataset_id, path_id, path_rendered,
    root_type, root_id,
    current_type, current_id,
    depth
) AS (
    SELECT
        p.dataset_id,
        p.input_type || ':' || p.input_id || '|' || p.output_type || ':' || p.output_id,
        in_nr.node_name || ' -> ' || out_nr.node_name,
        p.input_type,
        p.input_id,
        p.output_type,
        p.output_id,
        1
    FROM Process p
    JOIN NodeRef in_nr
      ON in_nr.dataset_id = p.dataset_id
     AND in_nr.node_type  = p.input_type
     AND in_nr.node_id    = p.input_id
    JOIN NodeRef out_nr
      ON out_nr.dataset_id = p.dataset_id
     AND out_nr.node_type  = p.output_type
     AND out_nr.node_id    = p.output_id
    WHERE NOT EXISTS (
        SELECT 1
        FROM Process prev
        WHERE prev.dataset_id  = p.dataset_id
          AND prev.output_type = p.input_type
          AND prev.output_id   = p.input_id
    )

    UNION ALL

    SELECT
        w.dataset_id,
        w.path_id || '|' || p.output_type || ':' || p.output_id,
        w.path_rendered || ' -> ' || out_nr.node_name,
        w.root_type,
        w.root_id,
        p.output_type,
        p.output_id,
        w.depth + 1
    FROM walk w
    JOIN Process p
      ON p.dataset_id  = w.dataset_id
     AND p.input_type  = w.current_type
     AND p.input_id    = w.current_id
    JOIN NodeRef out_nr
      ON out_nr.dataset_id = p.dataset_id
     AND out_nr.node_type  = p.output_type
     AND out_nr.node_id    = p.output_id
    WHERE w.depth < 100
)
SELECT
    w.dataset_id,
    w.path_id,
    w.depth AS length,
    w.root_type,
    w.root_id,
    w.current_type AS leaf_type,
    w.current_id   AS leaf_id,
    w.path_rendered
FROM walk w
WHERE NOT EXISTS (
    SELECT 1
    FROM Process next
    WHERE next.dataset_id = w.dataset_id
      AND next.input_type = w.current_type
      AND next.input_id   = w.current_id
);
```

The exact SQL can be cleaned up in implementation, but the important part is the structure:

- recursion is directly over `Process`
- `NodeRef` is only for rendering names
- path aggregation is dataset-local

## Query Patterns

The simplified schema must support path-aware parameter queries.

### 1. At What Temperature Was The Sample Created That Led To This Data Leaf Node?

Meaning:

- start from a leaf data node
- find the full path that leads to it
- inspect the processes on that path
- return all `Annotation` rows named `temperature`

This is why `Paths` alone is not enough. We also need a step-level view of the path.

Recommended addition:

- either expose a `PathSteps` view
- or repeat the same recursive walk in a query CTE

Suggested `PathSteps` shape:

```sql
PathSteps(
    dataset_id,
    path_id,
    step,
    process_id,
    input_type,
    input_id,
    output_type,
    output_id
)
```

Then the query becomes conceptually:

```sql
SELECT pv.*
FROM Paths p
JOIN PathSteps ps
  ON ps.dataset_id = p.dataset_id
 AND ps.path_id    = p.path_id
JOIN Annotation pv
  ON pv.dataset_id = ps.dataset_id
 AND pv.owner_type = 'Process'
 AND pv.owner_id   = ps.process_id
WHERE p.dataset_id = ?
  AND p.leaf_type  = 'Data'
  AND p.leaf_id    = ?
  AND pv.name      = 'temperature';
```

This answers:

- which temperature parameters occur on the lineage that produced a given leaf node

### 2. Query A Dataset Parameter Based On A Leaf Node

Meaning:

- identify the dataset that contains the leaf node
- return dataset-level parameters for that dataset

Conceptually:

```sql
SELECT pv.*
FROM Paths p
JOIN Annotation pv
  ON pv.dataset_id = p.dataset_id
 AND pv.owner_type = 'Dataset'
 AND pv.owner_id   = p.dataset_id
WHERE p.leaf_type = 'Data'
  AND p.leaf_id   = ?;
```

This answers:

- dataset-level settings, annotations, or parameters starting from a leaf node

## Recommended Addition: `PathSteps`

Even though the user explicitly asked for `Paths`, practical querying becomes much better if the simplified schema also exposes path steps.

So the minimum useful query layer is:

- `Paths` for full aggregated paths
- `PathSteps` for joining path segments back to `Process`

Without `PathSteps`, path-based parameter queries become unnecessarily awkward.

## Simplification Tradeoffs

This schema becomes much easier to understand, but it intentionally gives things up.

What gets simpler:

- only five tables
- `Process` is directly readable as a graph edge
- one generic `Annotation` table instead of many parameter tables
- no process I/O junction tables
- `Paths` is straightforward
- seed data is much shorter

What gets weaker:

- no protocol layer
- only a very small parameter/property model
- no rich distinction between many parameter subtypes
- no explicit distinction between process metadata and edge metadata beyond `owner_type`
- no DB-enforced polymorphic foreign keys from `Process` to `Sample`/`Data`
- no DB-enforced polymorphic foreign keys from `Annotation.owner_id` to `Dataset`/`Process`
- less semantic detail than the full ARC model

That is acceptable because this is explicitly a simplified base schema.

## Validation Strategy

Because `Process.input_id` and `Process.output_id` can point to either `Sample` or `Data`, SQLite cannot enforce both targets with a normal foreign key.

Also, because `Annotation.owner_id` can point to either `Dataset` or `Process`, SQLite cannot enforce that polymorphic reference directly either.

So validation should happen through:

- smoke-test queries
- path-generation checks
- seed consistency tests

Useful validation queries:

- list all processes in a dataset
- find process rows whose input node does not exist
- find process rows whose output node does not exist
- find property values whose owner row does not exist
- find paths per dataset
- find temperature values along the path to a leaf node
- find dataset parameters from a leaf node
- detect obvious cycles if needed

## Seeding Approach

The simplified seed should follow these rules:

1. insert one dataset
2. insert all sample nodes
3. insert all data nodes
4. insert one `Process` row per graph edge
5. insert `Annotation` rows for process parameters and dataset parameters
6. verify `Paths` returns the expected root-to-leaf chains
7. verify parameter queries work from leaf nodes

This makes the seed file read like a graph definition rather than a relational reconstruction.

## Implementation Plan

### Phase 1

Create a new simplified schema file rather than modifying the richer schema first.

Suggested location:

- `schemas/sql/simplified/001_core.sql`

### Phase 2

Create a minimal seed example for the simplified schema.

Suggested location:

- `schemas/sql/simplified/seed_example.sql`

### Phase 3

Add the `Paths` view and a `PathSteps` view, then verify them against the example graph.

### Phase 4

Document the simplified schema separately from the richer SQL variants so both can evolve independently.

## Bottom Line

The simplified base schema should be:

- `Dataset`
- `Sample`
- `Data`
- `Process`
- `Annotation`

with:

- `Process` as the edge list
- `Annotation` attached to either `Process` or `Dataset`
- `NodeRef` as a helper view
- `Paths` as the recursive root-to-leaf aggregation view
- `PathSteps` as the process-level join surface for path-based parameter queries

This is the smallest schema that still expresses the ARC process graph in a queryable relational form.
