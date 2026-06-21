# Plan: Patch SQL schema for Data entity restructure

## Summary of Changes in the Last 2 Commits

**Commit `957a6c2`** — added `yml_plan.md` (no schema impact).

**Commit `54bbd3e`** — "various small changes" to `spec/core/*`:

1. **`@id` → `id`, `@type` → `type`** across all core entities — doc cosmetics. The SQL schema already uses `id`/`type`, **no patch needed**.
2. **`id` changed from MUST to COULD** on `Data`, `Person`, `Process`, `Protocol`. In SQL these are PKs and must exist; auto-generation is an app-layer concern. **No schema patch needed**.
3. **`additionalType` explicitly added** to `Process` and `Protocol` spec tables (was previously only implied via decoration docs). SQL already has `additional_type` columns on both. **No patch needed**.
4. **`Data` entity restructured (the only real change)**:
   - **REMOVED**: `name` (MUST), `disambiguatingDescription` (COULD)
   - **ADDED**: `additionalType` (COULD) — decoration discriminator, e.g. `Raw Data`
   - **ADDED**: `path` (MUST) — file path; replaces `name` as identifier
   - **ADDED**: `selector` (COULD) — fragment selector narrowing the target
   - **ADDED**: `selectorFormat` (COULD) — URL describing selector syntax (e.g. RFC 7111)
   - `encodingFormat` description updated to "MIME format of the target data object or fragment"

This splits the old `name`-embedded fragment pattern (`"proteomics_result.csv#col=12"`) into structured `path` + `selector` fields.

## Patches

### 1. `schemas/sql/001_core.sql` — `Data` table

Replace the `Data` table definition with:

```sql
CREATE TABLE Data (
    id              TEXT PRIMARY KEY,
    type            TEXT NOT NULL,
    additional_type TEXT,
    path            TEXT NOT NULL,
    selector        TEXT,
    selector_format TEXT,
    encoding_format TEXT
);
```

### 2. `schemas/sql/001_core.sql` — index

Add:

```sql
CREATE INDEX idx_data_additional_type ON Data(additional_type);
```

### 3. `schemas/sql/001_core.sql` — `NodeRef` helper view

`Data.name` no longer exists. Patch `NodeRef` so the Paths view still renders useful node labels, composing `path` + optional `selector`:

```sql
CREATE VIEW NodeRef AS
SELECT 'Sample' AS node_type, id AS node_id, name AS node_name FROM Sample
UNION ALL
SELECT 'Data', id,
       CASE WHEN selector IS NOT NULL
            THEN path || '#' || selector
            ELSE path
       END
FROM Data;
```

The Paths view itself needs no change (it only reads `NodeRef.node_name`).

### 4. `schemas/sql/seed_example.sql` — Data inserts

- Six `.raw` files: rename column `name` → `path` (values unchanged).
- Six result columns: split `"proteomics_result.csv#col=N"` into `path='proteomics_result.csv'` + `selector='col=N'` + `selector_format='https://datatracker.ietf.org/doc/html/rfc7111'`.

## Verification

```bash
sqlite3 :memory: ".read schemas/sql/001_core.sql" ".read schemas/sql/seed_example.sql" "SELECT * FROM Paths;"
```

Expected: 6 paths rendering identically to before, e.g.
`Base Culture -> Cultivation Flask RT -> Eppi RT 1 -> sample1.raw -> proteomics_result.csv#col=12`

The `#col=N` suffix now comes from the `NodeRef` CASE expression rather than `Data.name`.

## Out of Scope

No impact on decoration views, junction tables, other core tables, or the Paths recursive CTE. Only the Data storage model and the NodeRef display mapping change.
