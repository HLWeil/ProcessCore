# Plan: Edge-Native SQL Schema Simplification

## Recommendation

Adopt the simpler model across the SQL design:

- each row in `Process` is exactly one input -> output edge
- pooling is represented by multiple rows with the same output
- splitting is represented by multiple rows with the same input

Examples:

- pooling:
  - `A -> C`
  - `B -> C`
- split:
  - `A -> B`
  - `A -> C`

This should become the core design, not just a projection layered on top of a more normalized schema.

## Why This Is The Better Fit

The current SQL designs reconstruct graph edges indirectly:

1. store process inputs in one table
2. store process outputs in another table
3. rebuild edge pairs through joins
4. traverse those derived edges to compute `Paths`

If the main purpose of the SQL schema is graph querying, that is backwards. The graph should be stored directly in the table we query most often.

With an edge-native `Process` table:

- input/output references are visible in the row itself
- seed data is much shorter and easier to inspect
- `Paths` becomes a direct recursive walk over `Process`
- the schema matches the mental model of provenance traversal

## Core Semantic Choice

This plan intentionally changes the SQL representation away from the original many-input/many-output process shape in the spec.

Instead, SQL will encode process connectivity as pairwise edges:

- one row means one traversable connection
- a pool is many incoming rows to the same output node
- a split is many outgoing rows from the same input node

This is a good fit for:

- lineage queries
- path enumeration
- root-to-leaf traversal
- upstream/downstream dependency lookup

This is weaker for:

- reconstructing one higher-level lab action as a single indivisible event
- distinguishing "jointly required inputs" from "multiple independent incoming edges"

That tradeoff is acceptable if the SQL schema is primarily a query model for graph traversal.

## Proposed Core Schema

### Keep These Entity Tables

- `DefinedTerm`
- `Annotation`
- `Protocol`
- `Sample`
- `Data`
- `Dataset`
- `Process`

### Redefine `Process` As The Edge Table

`Process` should directly carry both node references:

```sql
CREATE TABLE Process (
    id                          TEXT PRIMARY KEY,
    type                        TEXT NOT NULL,
    name                        TEXT NOT NULL,
    additional_type             TEXT,
    executes_protocol_id        TEXT REFERENCES Protocol(id),
    input_type                  TEXT NOT NULL CHECK (input_type IN ('Sample', 'Data')),
    input_id                    TEXT NOT NULL,
    output_type                 TEXT NOT NULL CHECK (output_type IN ('Sample', 'Data')),
    output_id                   TEXT NOT NULL,
    end_time                    TEXT,
    disambiguating_description  TEXT,
    description                 TEXT
);
```

Recommended indexes:

```sql
CREATE INDEX idx_process_input    ON Process(input_type, input_id);
CREATE INDEX idx_process_output   ON Process(output_type, output_id);
CREATE INDEX idx_process_protocol ON Process(executes_protocol_id);
CREATE INDEX idx_process_additional_type ON Process(additional_type);
```

This removes the need for:

- `ProcessObjectSample`
- `ProcessObjectData`
- `ProcessResultSample`
- `ProcessResultData`
- helper `ProcessEdge` view

## Parameters And Additional Process Metadata

Keep:

- `ProcessParameterValue`
- `ProcessAdditionalProperty`

Those tables should still reference `process_id`.

Consequence:

- if pooling or splitting is represented by several `Process` rows, shared parameters may need to be repeated across those rows

That duplication is acceptable in this design because it buys a much clearer and more queryable graph model.

If this later proves too repetitive, a second-level grouping field such as `process_group_id` can be added, but it should stay out of the first simplification pass.

## Node Representation

Keep `Sample` and `Data` as separate tables.

Use a shared node view for traversal and display:

```sql
CREATE VIEW NodeRef AS
SELECT 'Sample' AS node_type, id AS node_id, name AS node_name
FROM Sample
UNION ALL
SELECT 'Data' AS node_type,
       id AS node_id,
       CASE
           WHEN selector IS NOT NULL THEN path || '#' || selector
           ELSE path
       END AS node_name
FROM Data;
```

## `Paths` In The Edge-Native Design

### Why `Paths` Gets Simpler

With this design, `Process` already is the edge list. There is no need to build a derived `ProcessEdge` first.

The recursive walk only needs to follow:

- current node = previous row's output
- next step = rows whose input matches that output

### Root And Leaf Definitions

- root node: a node that appears in `Process.input_*` but never in `Process.output_*`
- leaf node: a node that appears in `Process.output_*` but never in `Process.input_*`

### Suggested Recursive Strategy

1. Start with all `Process` rows whose input is a root node.
2. Build the initial rendered path as `input -> output`.
3. Recursively join to the next `Process` row where:
   - `next.input_type = current.output_type`
   - `next.input_id = current.output_id`
4. Continue until no further outgoing process exists.
5. Return only terminal walks ending at leaf nodes.

### Suggested Outputs

`Paths`:

```sql
Paths(
    path_id,
    length,
    root_type,
    root_id,
    leaf_type,
    leaf_id,
    path_rendered
)
```

Optional `PathSteps`:

```sql
PathSteps(
    path_id,
    step,
    process_id,
    input_type,
    input_id,
    output_type,
    output_id
)
```

## Querying Implications

This design does not make all querying harder. It changes which queries are easiest.

### Queries That Become Easier

- all root-to-leaf traversals
- all upstream/downstream lineage lookups
- "which processes connect node X to node Y?"
- "which leaves descend from this root input?"
- computing `Paths`

### Queries That Become Harder Or Less Exact

- identifying one conceptual multi-input/multi-output operation as a single unit
- asking whether several inputs were jointly required rather than simply converging on the same output
- deduplicating repeated process metadata in pooled/split regions

So the simplified edge-native design is excellent for provenance traversal, but weaker for exact event semantics.

## Concrete Migration Plan

### Phase 1: Simplify The Core SQL DDL

Update:

- `schemas/sql/view_only/001_core.sql`
- `schemas/sql/closure_table/001_core.sql`
- optionally `schemas/sql/001_core.sql` if it remains the top-level reference

Changes:

- redefine `Process` to include `input_*` and `output_*`
- delete the four process I/O tables
- remove the derived `ProcessEdge` view
- keep all non-graph entity tables unchanged where possible

### Phase 2: Rebuild `Paths`

For `view_only`:

- make `Paths` a recursive view directly over `Process`
- optionally expose `PathSteps` from the same recursive walk

For `closure_table`:

- keep sampleized `Paths` and `PathSteps` tables if desired
- rewrite the refresh script to read directly from `Process`

### Phase 3: Rewrite Seed Data

Update:

- `schemas/sql/view_only/seed_example.sql`
- `schemas/sql/closure_table/seed_example.sql`
- `schemas/sql/seed_example.sql` if still used

Rule:

- every lineage hop becomes one `Process` insert

That means the proteomics example becomes visually much closer to the graph it is meant to represent.

### Phase 4: Update Documentation And Smoke Tests

Update:

- `schemas/sql/testing.md`
- `plans/03_sqlite.md`
- any comments in SQL files that still refer to separate process object/result tables

Verification should cover:

- `SELECT * FROM Paths`
- `SELECT * FROM PathSteps`
- sample lineage queries from root sample to terminal data
- factor/parameter queries that still join correctly through `ProcessParameterValue`

## Open Design Note

The only significant unresolved question is whether repeated edges that belong to the same conceptual action should later share a lightweight grouping key.

For now, the plan should assume:

- no grouping key
- duplicate process metadata where necessary
- optimize first for graph clarity and simple recursion

That keeps the schema minimal and aligned with the edge-native design goal.

## Bottom Line

The SQL schema should treat `Process` as the graph edge itself.

That means:

- one row = one input -> output mapping
- pooling and splitting are represented structurally by repeated rows
- `Paths` becomes a direct recursive traversal over `Process`
- the schema gets much simpler for lineage work

The main cost is that exact higher-level process-event semantics become less explicit. For the current goals, that is a reasonable tradeoff.
