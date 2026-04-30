# Plan: ProcessCore.SQL Library

Create a F# library for reading and writing the SQL import profile in [schemas/sql/](../schemas/sql/). The first implementation is intentionally boring: a one-to-one representation of the tables as F# types plus simple CRUD/read APIs. Higher-level graph/domain APIs come later.

## Goals

- Provide F# record types matching the current SQL tables in [schemas/sql/001_core.sql](../schemas/sql/001_core.sql).
- Keep the core model and row-mapping code transpilable with Fable to JavaScript, TypeScript, and Python.
- Make database I/O explicitly platform-dependent, because SQLite connectors are different in .NET, JS/TS, and Python.
- Build with FAKE using the BuildProjects.NET style from <https://github.com/kMutagene/BuildProjects.NET>.
- Test with Fable.Pyxpecto from <https://github.com/Freymaurer/Fable.Pyxpecto>, so tests can run under .NET and transpiled targets.

## Non-Goals

- No rich ProcessCore domain model yet.
- No graph traversal API beyond table/view access.
- No ORM.
- No automatic YAML import/export yet.
- No browser-first SQLite target yet; start with runtime connectors for .NET, Node JS/TS, and Python.

## Project Shape

Proposed repository layout:

```text
src/
  ProcessCore.SQL/
    ProcessCore.SQL.fsproj
    Tables.fs
    RowCodecs.fs
    Commands.fs
    Repository.fs
  ProcessCore.SQL.DotNet/
    ProcessCore.SQL.DotNet.fsproj
    SqliteDriver.fs
  ProcessCore.SQL.JavaScript/
    ProcessCore.SQL.JavaScript.fsproj
    BetterSqliteDriver.fs
  ProcessCore.SQL.TypeScript/
    ProcessCore.SQL.TypeScript.fsproj
  ProcessCore.SQL.Python/
    ProcessCore.SQL.Python.fsproj

tests/
  ProcessCore.SQL.Tests/
    ProcessCore.SQL.Tests.fsproj
    Main.fs
    Fixtures.fs
    TableRoundtripTests.fs
    ConstraintTests.fs
    ViewTests.fs

build/
  Build.fs
  build.cmd
  build.sh

Directory.Packages.props
```

Notes:

- `ProcessCore.SQL` contains the shared Fable-compatible code.
- Runtime adapter projects are the only layer allowed to bind to concrete SQLite connectors.
- Package boundaries are intentional: publish `ProcessCore.SQL` as the shared package, then publish adapter packages per ecosystem/runtime (`ProcessCore.SQL.DotNet` on NuGet, JavaScript/TypeScript output to npm, Python output to PyPI).
- Tests compile against the public API, not private helper functions.
- The existing SQL fixtures stay in `schemas/sql/`; tests copy/read those files instead of duplicating schema text.
- Current implementation keeps the BuildProjects.NET wrapper scripts under `build/`, not at repository root.

## Build System

Use BuildProjects.NET/FAKE as the build foundation.

Implementation steps:

1. Inspect <https://github.com/kMutagene/BuildProjects.NET> locally or from a fetched template before scaffolding.
2. Add a FAKE build project under `build/`.
3. Add targets:
   - `Clean`
   - `Restore`
   - `Build`
   - `TestDotNet`
   - `TranspileJs`
   - `TestJs`
   - `TranspileTs`
   - `TestTs`
   - `TranspilePy`
   - `TestPy`
   - `RunTests`
4. Keep `RunTests` as the default aggregate once all target runtimes are wired.

Build commands should be simple:

```powershell
.\build.cmd RunTests
.\build.cmd TestDotNet
.\build.cmd TestJs
.\build.cmd TestTs
.\build.cmd TestPy
```

## Table Types

Create one F# record type per SQL table. Keep names close to SQL table names but idiomatic enough for F#.

Entity tables:

- `DefinedTermRow`
- `LabProtocolRow`
- `FormalParameterRow`
- `DatasetRow`
- `MaterialRow`
- `DataRow`
- `LabProcessRow`
- `PropertyValueRow`

Association tables:

- `DatasetHasPartRow`
- `DatasetProcessRow`
- `DatasetAdditionalPropertyRow`
- `ProtocolParameterRow`
- `ProcessIoRow`
- `ProcessParameterValueRow`
- `ProtocolAdditionalPropertyRow`
- `MaterialAdditionalPropertyRow`
- `DataAdditionalPropertyRow`

Rules:

- Use `string` for `TEXT NOT NULL`.
- Use `string option` for nullable `TEXT`.
- Use `int` for positions.
- Use small discriminated unions only where they exactly reflect SQL constraints and are Fable-safe, e.g. `ProcessIoDirection = Input | Output`.
- Do not introduce nested/domain-shaped records yet.

## I/O Boundary

The shared library must not depend directly on `Microsoft.Data.Sqlite`, Node packages, Python `sqlite3`, or any other concrete connector. Define a small driver abstraction in shared code:

```fsharp
type SqlValue =
    | SqlNull
    | SqlText of string
    | SqlInt of int

type SqlRow = Map<string, SqlValue>

type ISqliteDriver =
    abstract Execute : sql: string -> parameters: (string * SqlValue) list -> unit
    abstract Query : sql: string -> parameters: (string * SqlValue) list -> SqlRow list
    abstract Scalar : sql: string -> parameters: (string * SqlValue) list -> SqlValue
```

Platform adapters implement that shape:

- .NET: likely `Microsoft.Data.Sqlite`.
- JavaScript/TypeScript on Node: choose one connector, likely `better-sqlite3` for synchronous behavior or `sqlite`/`sqlite3` if async becomes necessary.
- Python: stdlib `sqlite3`.

Selection can use conditional compilation:

```fsharp
#if FABLE_COMPILER_PYTHON
// Python binding
#elif FABLE_COMPILER_JAVASCRIPT
// JS binding
#elif FABLE_COMPILER_TYPESCRIPT
// TS binding
#else
// .NET binding
#endif
```

If async-only connectors are chosen for JS/TS, split the abstraction into sync and async before implementation. Do not fake sync over async in shared code.

## Row Codecs

Each table gets two functions:

- `ofRow : SqlRow -> TableRow`
- `toParameters : TableRow -> (string * SqlValue) list`

Codec rules:

- Centralize nullable handling.
- Fail with table/column names in error messages.
- Keep generated column `data.fragment_identity` read-only and out of `DataRow` for now; it is an implementation detail of the SQLite database.

## Repository API

First pass API is generic and table-shaped:

```fsharp
module Dataset =
    val insert : ISqliteDriver -> DatasetRow -> unit
    val update : ISqliteDriver -> DatasetRow -> unit
    val delete : ISqliteDriver -> id: string -> unit
    val get : ISqliteDriver -> id: string -> DatasetRow option
    val list : ISqliteDriver -> DatasetRow list
```

Repeat this pattern for all 17 tables. Avoid clever generic metaprogramming until the duplication hurts in practice.

Current status: not implemented. `src/ProcessCore.SQL/Repository.fs` currently contains `Table<'row>` metadata for the 17 tables only. It does not yet expose `insert`, `update`, `delete`, `get`, `list`, view readers, or transaction helpers.

Views:

- `ProcessEdges.list`
- `PropertyValueOrphans.list`

Transactions:

- Add a small transaction helper in the driver layer if all target connectors can support it consistently.
- Otherwise expose explicit `BEGIN`, `COMMIT`, `ROLLBACK` helpers as SQL commands.

## Fixtures

Use the existing seeded artifacts as fixtures:

- [schemas/sql/001_core.sql](../schemas/sql/001_core.sql)
- [schemas/sql/seed_example.sql](../schemas/sql/seed_example.sql)
- [schemas/sql/seeded_core.sqlite](../schemas/sql/seeded_core.sqlite)

For tests, prefer creating a fresh temporary database from `001_core.sql` and `seed_example.sql` per test suite. The committed `seeded_core.sqlite` is useful for smoke tests and manual inspection, but tests should not mutate it.

Fixture helpers:

- `createEmptyDatabase`
- `createSeededDatabase`
- `readSchemaSql`
- `readSeedSql`
- `copySeededDatabase`

Target-specific temp-file creation belongs in test platform helpers, not in row codecs.

## Tests

Use Fable.Pyxpecto for portable tests. Its README documents Expecto-like `testList`, `testCase`, and `testCaseAsync`, and runners for .NET, JavaScript, TypeScript, and Python.

Test groups:

1. **Schema Smoke**
   - `001_core.sql` creates 17 tables.
   - `seed_example.sql` loads without FK errors.
   - `PRAGMA foreign_key_check` returns no rows.
   - `property_value_orphans` returns no rows for the seed.

2. **Table Reads**
   - Read each table from the seeded database.
   - Assert expected row counts and a few representative values.

3. **Roundtrip Inserts**
   - Insert one row into each entity table where FK dependencies allow it.
   - Read it back and compare record equality.
   - Insert association rows and verify positions are preserved.

4. **Constraint Behavior**
   - `process_io` exact-one-target check rejects both-null and both-set targets.
   - `intended_use_id` / `intended_use_text` mutual exclusion rejects both-set values.
   - FK `RESTRICT` blocks deleting referenced `property_value`.
   - owner delete cascades to owner association rows.

5. **Views**
   - `process_edges` returns the growth and measurement edges.
   - `property_value_orphans` surfaces an intentionally inserted orphan PV.

6. **Cross-Target Tests**
   - Same public tests should run under .NET, JS, TS, and Python once drivers are wired.
   - Mark target-specific gaps as pending tests, not omitted tests.

## Dependency Plan

Do not add production dependencies blindly. Before implementation, confirm connector choices:

- .NET SQLite connector package.
- Node JS/TS SQLite package.
- Python binding strategy, ideally stdlib `sqlite3`.

Expected package categories:

- Fable and Fable SDK/tooling.
- FAKE / BuildProjects.NET build dependencies.
- Fable.Pyxpecto for tests.
- SQLite connector packages per runtime adapter.

## Implementation Phases

### Phase 1 — Scaffold

- Status: complete for the initial skeleton.
- Added a compileable `src/ProcessCore.SQL` project targeting `net10.0`.
- Added shared SQL primitives, one-to-one table row records, row codec modules, table metadata, and explicit runtime connector planning stubs.
- Added a BuildProject-template-based FAKE build project under `build/` with wrapper scripts and the planned target names.
- Added a `ProcessCore.SQL.Tests` executable using Fable.Pyxpecto.
- JavaScript Fable transpilation/test targets are wired. TypeScript and Python targets still exist as explicit pending targets.

### Phase 2 — Shared Table Model

- Status: complete for table-shaped shared code.
- Done: added the 17 row records.
- Done: added `SqlValue`, `SqlRow`, and `ISqliteDriver`.
- Done: added row codecs for all tables.
- Done: added .NET tests for table metadata and representative row codec roundtrips.
- Not part of this phase: CRUD/repository operations. Those remain in Phase 3.

### Phase 3 — .NET Driver and Repository

- Status: partially complete.
- Done: implemented the .NET SQLite driver adapter with `Microsoft.Data.Sqlite`.
- Done: added .NET tests that create an in-memory database from `001_core.sql` and `seed_example.sql`, check FK health, bind parameters, and read seeded rows through shared codecs.
- Pending: implement CRUD modules for the 17 tables.
- Pending: add CRUD tests for insert/get/list/update/delete across representative entity and association tables.
- Pending: add view readers for `process_edges` and `property_value_orphans`.
- Pending: add transaction helpers or document explicit `BEGIN` / `COMMIT` / `ROLLBACK` usage.

### Phase 4 — Python Driver

- Add Fable Python binding for stdlib `sqlite3`.
- Transpile tests with Fable.
- Run Pyxpecto tests under Python.

### Phase 5 — JS/TS Drivers

- Status: started; JavaScript adapter project, `better-sqlite3` binding, Node tooling, JS Pyxpecto test project, and `TestJs` build target are wired and passing. TypeScript remains pending.
- Done: chose `better-sqlite3` as the Node SQLite connector for the synchronous driver boundary.
- Done: introduced Node tooling with `package.json`, `package-lock.json`, npm scripts, generated output ignores, and `better-sqlite3` dependency management.
- Done: wired Fable JavaScript transpilation and `TestJs` into the BuildProject pipeline.
- Done: added JavaScript binding and JS Pyxpecto tests for schema/seed execution plus parameter/row mapping.
- Pending: decide whether TypeScript gets a distinct binding or reuses the JavaScript binding output.
- Pending: wire TypeScript transpilation and tests.

### Next Implementation Step — Repository CRUD

- Implement small shared SQL command helpers for `INSERT`, `UPDATE`, `DELETE`, primary-key predicates, and ordered `SELECT`.
- Add table-specific modules for all 17 tables, starting with the 8 entity tables before association tables.
- Keep the public API table-shaped and synchronous through `ISqliteDriver`.
- Add .NET CRUD tests first, then reuse the same public tests under JavaScript once the shape is stable.

### Phase 6 — Polish

- Document public API.
- Document connector support matrix.
- Add examples for opening the seeded database and listing rows.
- Decide whether to package/publish the F# library.

## Open Questions

- Which exact JS/TS SQLite connector should be the default?
- Should JS and TS share one binding implementation, or should TS get typed externs?
- Should repository functions be sync-only for the first version, or should the abstraction be async from day one?
- Should the library own schema creation/migration, or only read/write databases already created from `001_core.sql`?
- Should generated `data.fragment_identity` ever be exposed in a view/read model?
