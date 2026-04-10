# Testing the SQLite Schema

This directory contains the ARC Data Model SQLite schema, a seed example based on the proteomics assay, and a prebuilt database for quick exploration.

## Files

| File | Purpose |
|------|---------|
| `001_core.sql` | Schema DDL: core tables, junction tables, decoration views, `Paths` view |
| `seed_example.sql` | Seed data mirroring [`examples/isa/assay_proteomics.yml`](../../examples/isa/assay_proteomics.yml) |
| `seed.db` | Prebuilt SQLite database (schema + seed already applied) |

## Prerequisites

- `sqlite3` CLI (ships with most systems; on Windows use the binaries from https://www.sqlite.org/download.html)
- Optional: [DB Browser for SQLite](https://sqlitebrowser.org/) or DBeaver for GUI inspection

## Quick Start — Use the Prebuilt DB

```bash
sqlite3 schemas/sql/seed.db
```

Inside the SQLite shell:

```sql
.headers on
.mode column
SELECT * FROM Paths;
```

Expected output: 6 rows tracing the full process graph from `Base Culture` to each column of `proteomics_result.csv`.

## Rebuild the DB From Scratch

```bash
rm schemas/sql/seed.db
sqlite3 schemas/sql/seed.db ".read schemas/sql/001_core.sql" ".read schemas/sql/seed_example.sql"
```

Or build in memory (no file written):

```bash
sqlite3 :memory: ".read schemas/sql/001_core.sql" ".read schemas/sql/seed_example.sql" "SELECT * FROM Paths;"
```

## Smoke Tests

### 1. Core tables are populated

```sql
SELECT 'Protocol' AS tbl, COUNT(*) AS n FROM Protocol
UNION ALL SELECT 'PropertyValue', COUNT(*) FROM PropertyValue
UNION ALL SELECT 'Material',      COUNT(*) FROM Material
UNION ALL SELECT 'Data',          COUNT(*) FROM Data
UNION ALL SELECT 'Process',       COUNT(*) FROM Process
UNION ALL SELECT 'Dataset',       COUNT(*) FROM Dataset;
```

Expected: Protocol 4, PropertyValue 12, Material 9, Data 12, Process 20, Dataset 1.

### 2. Paths view — end-to-end process graph

```sql
SELECT * FROM Paths ORDER BY path;
```

Expected: 6 paths, each of depth 4, e.g.
`Base Culture -> Cultivation Flask RT -> Eppi RT 1 -> sample1.raw -> proteomics_result.csv#col=12`

Note: the `#col=12` suffix is composed by the `NodeRef` view from `Data.path` + `Data.selector` — it is not stored as a single string.

### 3. ISA decoration views

```sql
SELECT * FROM Assay;
SELECT COUNT(*) FROM ParameterValue;       -- expect 6
SELECT COUNT(*) FROM CharacteristicValue;  -- expect 1
SELECT COUNT(*) FROM FactorValue;          -- expect 2
SELECT COUNT(*) FROM Component;            -- expect 2
```

### 4. Workflow Run decoration views (should return 0 rows with ISA seed)

```sql
SELECT COUNT(*) FROM ArcWorkflow;          -- expect 0
SELECT COUNT(*) FROM ArcRun;               -- expect 0
SELECT COUNT(*) FROM WorkflowInvocation;   -- expect 0
SELECT COUNT(*) FROM WorkflowInput;        -- expect 0
```

### 5. Foreign key enforcement

```sql
PRAGMA foreign_keys;  -- expect 1

-- This should fail with a FK violation:
INSERT INTO ProcessParameterValue (process_id, propertyvalue_id)
VALUES ('#does_not_exist', '#PV_software');
```

## Exploratory Queries

### Materials with their factor values (temperature)

```sql
SELECT m.name AS material, pv.name AS factor, pv.value, pv.unit_text
FROM Material m
JOIN MaterialAdditionalProperty map ON map.material_id = m.id
JOIN PropertyValue pv ON pv.id = map.propertyvalue_id
WHERE pv.additional_type = 'FactorValue';
```

### Which processes use the sonicator parameter?

```sql
SELECT DISTINCT p.name, p.id
FROM Process p
JOIN ProcessParameterValue ppv ON ppv.process_id = p.id
JOIN PropertyValue pv ON pv.id = ppv.propertyvalue_id
WHERE pv.name = 'sonicator';
```

### All data files produced in the assay

```sql
SELECT d.path, d.selector, d.encoding_format
FROM Data d
JOIN ProcessResultData prd ON prd.data_id = d.id;
```

### Query from the use-cases doc: "Samples where growth temperature = 25"

```sql
SELECT m.name
FROM Material m
JOIN ProcessResultMaterial prm ON prm.material_id = m.id
JOIN Process p ON p.id = prm.process_id
JOIN Protocol pr ON pr.id = p.executes_protocol_id
JOIN MaterialAdditionalProperty map ON map.material_id = m.id
JOIN PropertyValue pv ON pv.id = map.propertyvalue_id
WHERE pr.name = 'Growth'
  AND pv.additional_type = 'FactorValue'
  AND pv.name  = 'temperature'
  AND pv.value = '25';
```

## Troubleshooting

- **"no such table: Data" etc.** — schema not loaded. Run `.read schemas/sql/001_core.sql` first, or open the prebuilt `seed.db`.
- **Empty Paths view** — seed data not loaded. Re-run the rebuild command above.
- **Journal files next to `seed.db`** — SQLite sets `journal_mode = WAL`. To consolidate into a single file, run `sqlite3 seed.db "PRAGMA wal_checkpoint(TRUNCATE);"`.
