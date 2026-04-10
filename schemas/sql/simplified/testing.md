# Testing the Simplified SQLite Schema

This directory contains the simplified ARC graph schema with:

- `Dataset`
- `Material`
- `Data`
- `Process`
- `PropertyValue`

`Process` is the edge list of the graph, and `Paths` / `PathSteps` are query views over that graph.

## Files

| File | Purpose |
|------|---------|
| `001_core.sql` | Simplified schema DDL and graph/query views |
| `seed_example.sql` | Proteomics example adapted to the simplified model |
| `seed.db` | Prebuilt SQLite database |

## Quick Check

```bash
sqlite3 schemas/sql/simplified/seed.db
```

Inside SQLite:

```sql
.headers on
.mode column
SELECT COUNT(*) FROM Paths;      -- expect 6
SELECT COUNT(*) FROM PathSteps;  -- expect 24
```

## Example Queries

### Temperature values along the path to a leaf data node

```sql
SELECT pv.name, pv.value, pv.unit, ps.step, ps.process_id
FROM Paths p
JOIN PathSteps ps
  ON ps.dataset_id = p.dataset_id
 AND ps.path_id    = p.path_id
JOIN PropertyValue pv
  ON pv.dataset_id = ps.dataset_id
 AND pv.owner_type = 'Process'
 AND pv.owner_id   = ps.process_id
WHERE p.dataset_id = '#Dataset_measurement1'
  AND p.leaf_type  = 'Data'
  AND p.leaf_id    = '#Data_result_col12'
  AND pv.name      = 'temperature'
ORDER BY ps.step;
```

Expected: one row with `25 degree Celsius`.

### Dataset parameters from a leaf data node

```sql
SELECT DISTINCT pv.name, pv.value, pv.unit
FROM Paths p
JOIN PropertyValue pv
  ON pv.dataset_id = p.dataset_id
 AND pv.owner_type = 'Dataset'
 AND pv.owner_id   = p.dataset_id
WHERE p.leaf_type = 'Data'
  AND p.leaf_id   = '#Data_result_col12';
```

Expected: dataset-level rows such as `variableMeasured = proteomics`.
