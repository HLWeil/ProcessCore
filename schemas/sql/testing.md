# Testing The SQLite Core Schema

This directory now contains a trimmed core-only SQLite example aligned with the current ProcessCore tables and relations.

## Files

| File | Purpose |
|------|---------|
| `001_core.sql` | Core table DDL plus join tables for multi-valued relations |
| `seed_example.sql` | Seed data mirroring the proteomics assay example using the current core entity names |
| `seed_core.db` | Prebuilt SQLite database for the trimmed core-only schema (schema + seed already applied) |

## Quick Start

```bash
sqlite3 schemas/sql/seed_core.db
```

Inside SQLite:

```sql
.tables
SELECT COUNT(*) FROM LabProcess;
```

## Rebuild The DB

```bash
rm schemas/sql/seed_core.db
sqlite3 schemas/sql/seed_core.db ".read schemas/sql/001_core.sql" ".read schemas/sql/seed_example.sql"
```

## Smoke Tests

### 1. Core entities are populated

```sql
SELECT 'DefinedTerm' AS tbl, COUNT(*) AS n FROM DefinedTerm
UNION ALL SELECT 'FormalParameter', COUNT(*) FROM FormalParameter
UNION ALL SELECT 'PropertyValue', COUNT(*) FROM PropertyValue
UNION ALL SELECT 'LabProtocol', COUNT(*) FROM LabProtocol
UNION ALL SELECT 'Material', COUNT(*) FROM Material
UNION ALL SELECT 'Data', COUNT(*) FROM Data
UNION ALL SELECT 'Dataset', COUNT(*) FROM Dataset
UNION ALL SELECT 'LabProcess', COUNT(*) FROM LabProcess;
```

Expected: 4, 4, 12, 4, 9, 12, 1, 20.

### 2. Process parameters are linked through formal parameters

```sql
SELECT pv.id, pv.name, pv.value, fp.name AS formal_parameter
FROM PropertyValue pv
LEFT JOIN FormalParameter fp ON fp.id = pv.instance_of_id
WHERE pv.additional_type = 'ParameterValue'
ORDER BY pv.id;
```

### 3. Process inputs and outputs can be inspected via join tables

```sql
SELECT p.name,
       im.material_id AS input_material,
       idt.data_id    AS input_data,
       om.material_id AS output_material,
       od.data_id     AS output_data
FROM LabProcess p
LEFT JOIN LabProcessInputMaterial im
  ON im.lab_process_id = p.id AND im.pair_index = 0
LEFT JOIN LabProcessInputData idt
  ON idt.lab_process_id = p.id AND idt.pair_index = 0
LEFT JOIN LabProcessOutputMaterial om
  ON om.lab_process_id = p.id AND om.pair_index = 0
LEFT JOIN LabProcessOutputData od
  ON od.lab_process_id = p.id AND od.pair_index = 0
ORDER BY p.id;
```

### 4. Dataset relations are populated

```sql
SELECT COUNT(*) FROM DatasetProcess;              -- expect 20
SELECT COUNT(*) FROM DatasetHasPartData;          -- expect 12
SELECT COUNT(*) FROM DatasetAdditionalProperty;   -- expect 1
```

### 5. Foreign key enforcement

```sql
PRAGMA foreign_keys;  -- expect 1

INSERT INTO LabProcessParameterValue (lab_process_id, property_value_id)
VALUES ('#does_not_exist', '#PV_software');
```

The insert above should fail with a foreign key violation.
