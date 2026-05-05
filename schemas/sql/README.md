# SQL Import Profile Artifacts

This directory contains an executable SQLite version of the SQL import profile described in [design.md](design.md).

## Files

- [001_core.sql](001_core.sql) - DDL for the core profile tables, constraints, indexes, and helper views.
- [seed_example.sql](seed_example.sql) - Small seeded process graph exercising datasets, materials, data files/fragments, protocols, parameters, and property values.
- `seeded_core.sqlite` - SQLite database built from the schema and seed files.

## Rebuild

```powershell
Remove-Item .\seeded_core.sqlite -ErrorAction SilentlyContinue
sqlite3 .\seeded_core.sqlite ".read 001_core.sql" ".read seed_example.sql"
sqlite3 .\seeded_core.sqlite "PRAGMA foreign_key_check;" "SELECT * FROM property_value_orphans;"
```

Both verification queries should return no rows.
