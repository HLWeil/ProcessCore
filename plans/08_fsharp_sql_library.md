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
  ProcessCore/
    ProcessCore.fsproj
    ProcessCore.Javascript.fsproj
    ProcessCore.Python.fsproj
    SQL/
      Tables.fs
      RowCodecs.fs
      Repository.fs
      Sql.fs
      SqliteDriver.fs
      BetterSqliteDriver.fs
      PythonSqliteDriver.fs

tests/
  ProcessCore.Tests/                      # single Pyxpecto project, transpiled to JS/Python
    ProcessCore.Tests.fsproj              # conditional ProjectReferences per Fable target
    SQL/
      Fixtures.fs                         # conditional driver helpers (.NET / JS / Python)
    TableModelTests.fs
    RowCodecTests.fs
    DotNetDriverTests.fs                  # driver-agnostic; named for historical reasons
    RepositoryCrudTests.fs
    Tests.fs                              # SQL testList collected by ../Main.fs

build/
  Build.fs
  build.cmd
  build.sh

Directory.Packages.props
```

Notes:

- `ProcessCore.SQL` contains the shared Fable-compatible code.
- The consolidated `src/ProcessCore` project consumes `Fable.Core` and applies Fable attributes. Runtime-specific project variants select only the platform binding file needed for .NET, JavaScript, or Python.
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

Create one F# class per SQL table, decorated with `[<AttachMembers>]` so Fable emits members directly on the JS/TS/Python class. Names stay close to SQL table names but idiomatic enough for F#.

Entity tables:

- `DefinedTermRow`
- `PlanRow`
- `FormalParameterRow`
- `DatasetRow`
- `SampleRow`
- `DataRow`
- `ProcessRow`
- `AnnotationRow`

Association tables:

- `DatasetHasPartRow`
- `DatasetProcessRow`
- `DatasetAdditionalPropertyRow`
- `ProtocolParameterRow`
- `ProcessIoRow`
- `ProcessParameterValueRow`
- `ProtocolAdditionalPropertyRow`
- `SampleAdditionalPropertyRow`
- `DataAdditionalPropertyRow`

Two read-only view types:

- `ProcessEdgeRow`
- `AnnotationOrphanRow`

Pattern (illustrated for `DefinedTermRow`):

```fsharp
[<AttachMembers>]
type DefinedTermRow(Id: string, Type: string, Name: string,
                    ?Tan: string,
                    ?InDefinedTermSetId: string,
                    ?InDefinedTermSetName: string) =

    member val Id = Id with get, set
    member val Type = Type with get, set
    member val Name = Name with get, set
    member val Tan = Tan with get, set
    member val InDefinedTermSetId = InDefinedTermSetId with get, set
    member val InDefinedTermSetName = InDefinedTermSetName with get, set

    [<NamedParams>]
    static member create (Id, Type, Name, ?Tan, ?InDefinedTermSetId, ?InDefinedTermSetName) =
        DefinedTermRow(Id, Type, Name,
                       ?Tan = Tan,
                       ?InDefinedTermSetId = InDefinedTermSetId,
                       ?InDefinedTermSetName = InDefinedTermSetName)
```

Rules:

- Each row type is a class with `[<AttachMembers>]`, not an F# record. Records lower to a `Record` shim in JS and a `dataclass`-shaped helper in Python; classes give consumers a regular `new RowType(...)` constructor in every target.
- Provide a positional primary constructor and a `[<NamedParams>]` static `create` factory for the named-args (object-literal) variant in JS/TS/Python. Do **not** define overloads — Fable shadows them when `[<AttachMembers>]` is present.
- Required SQL columns become required ctor params; nullable SQL columns become `?Optional` params.
- Use `member val ... with get, set` so consumers can mutate row instances directly (matches the existing `ProcessCore` convention).
- Use `string` for `TEXT NOT NULL`, `string option` for nullable `TEXT`, `int` for positions.
- Use small discriminated unions only where they exactly reflect SQL constraints, and decorate them per the *I/O Boundary* rules (`[<Erase>]` for cases-with-data, `[<StringEnum>]` for case-only).
- Do not introduce nested/domain-shaped types yet.

## I/O Boundary

The shared library must not depend directly on `Microsoft.Data.Sqlite`, Node packages, Python `sqlite3`, or any other concrete connector. Define a small driver abstraction in shared code:

```fsharp
[<Erase>]
type SqlValue =
    | Null
    | Text of string
    | Int of int

[<StringEnum>]
type ProcessIoDirection =
    | [<CompiledName("input")>] Input
    | [<CompiledName("output")>] Output

[<AttachMembers>]
type SqlParameter(Name: string, Value: SqlValue) =
    member val Name = Name with get, set
    member val Value = Value with get, set

    [<NamedParams>]
    static member create (Name, Value) = SqlParameter(Name, Value)

type SqlParameters = SqlParameter[]

type SqlRow = Map<string, SqlValue>

type ISqliteDriver =
    abstract Execute : sql: string -> parameters: SqlParameters -> unit
    abstract Query : sql: string -> parameters: SqlParameters -> SqlRow[]
    abstract Scalar : sql: string -> parameters: SqlParameters -> SqlValue
```

Fable-attribute rules:

- Unions whose cases carry data → `[<Erase>]`. JS/TS see plain values, Python sees the unwrapped payload, with no boxed DU shim.
- Unions with no payload → `[<StringEnum>]` plus `[<CompiledName(...)>]` to match SQL literals exactly. The string *is* the enum, so any `Sql`/`ofSql` helper goes away — `match dir with ProcessIoDirection.Input -> ...` still works in F#, and JS/TS receive the string directly.
- Drop `[<RequireQualifiedAccess>]` from these unions; it's redundant alongside the Fable attributes and lengthens the JS surface.

Collection rules:

- All public collection-typed members use `Array<'T>` (`'T[]`). F# `list` lowers to a linked-list shim in JS and a custom class in Python — neither is what consumers expect.
- Do not expose F# tuples on public surfaces. Tuples lower to plain JS arrays and are easy to confuse with array params. Use small `[<AttachMembers>]` classes (e.g. `SqlParameter`) instead.
- `Map<string, SqlValue>` for `SqlRow` stays — the Fable guidance allows string-keyed maps because they lower to native JS `Map` / Python `dict`.

Adapter wrapper convention:

- `ISqliteDriver` stays as an F# interface. Each platform adapter ships a small concrete class with `[<AttachMembers>]` that implements the interface and exposes a `[<NamedParams>]` static `create` factory. JS/TS/Python users instantiate `new DotNetSqliteDriver({...})` / `BetterSqliteDriver.create({...})` / `PythonSqliteDriver.create({...})` rather than fighting bare interface objects.

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

## Public-surface checklist

Anything appearing in a public type, member, or signature that crosses Fable to JS/TS/Python must follow these rules. They are not stylistic preferences — violating them produces non-ergonomic transpiled output (boxed DUs, linked-list shims, hidden methods).

| Forbidden in public surface | Use instead |
| --- | --- |
| F# records | `[<AttachMembers>]` class with positional ctor + `[<NamedParams>]` `static member create` |
| F# `'T list` | `'T[]` (`Array<'T>`) |
| F# tuples (`'a * 'b`) | small `[<AttachMembers>]` class, or `[<NamedParams>]` named args |
| Plain DU with cases that carry data | `[<Erase>]` |
| Plain DU with no payload | `[<StringEnum>]` (+ `[<CompiledName>]` if the string must match an external literal) |
| Overloaded methods on `[<AttachMembers>]` types | distinct names (`createFromPath`, `createFromConnection`) |
| Non-primitive dictionary keys | string/int keys only — non-primitives block native `Map`/`dict` lowering |

Internal-only helpers may keep `list`, tuples, or records freely; the rules apply only to the *exported* API surface.

## Row Codecs

Codecs attach to the row classes themselves, not to a free-standing module. Each row class exposes:

- a static factory: `static member ofRow (row: SqlRow) : RowType`
- an instance method: `member this.ToParameters () : SqlParameters`

Pattern:

```fsharp
[<AttachMembers>]
type DefinedTermRow(...) =
    // members from the *Table Types* section
    ...

    static member ofRow (row: SqlRow) : DefinedTermRow = ...
    member this.ToParameters () : SqlParameters = ...
```

This keeps `DefinedTermRow.ofRow(row)` and `instance.toParameters()` (the Fable-lowered name) ergonomic in JS/TS/Python.

Implementation notes:

- `RowCodecs.fs` stays as the home for the codec bodies. Use F# `type ... with` augmentations there if separating the type declaration in `Tables.fs` from the codec body keeps the files small.
- The existing `text` / `textOption` / `int` / `textParam` / `intParam` / `textOptionParam` helpers stay in a private module inside `RowCodecs.fs` — they must not appear on the public surface.
- Centralize nullable handling.
- Fail with table/column names in error messages.
- Keep generated column `data.fragment_identity` read-only and out of `DataRow` for now; it is an implementation detail of the SQLite database.
- View types (`ProcessEdgeRow`, `AnnotationOrphanRow`) only need `ofRow`; they have no `ToParameters`.

## Repository API

First pass API is generic and table-shaped, exposed as static members on per-table `[<AttachMembers>]` classes so JS/TS/Python see `Dataset.insert(driver, row)` etc.:

```fsharp
[<AttachMembers>]
type Dataset =
    static member insert (driver: ISqliteDriver, row: DatasetRow) : unit = ...
    static member update (driver: ISqliteDriver, row: DatasetRow) : unit = ...
    static member delete (driver: ISqliteDriver, id: string) : unit = ...
    static member get (driver: ISqliteDriver, id: string) : DatasetRow option = ...
    static member list (driver: ISqliteDriver) : DatasetRow[] = ...
```

A single `Repository` class collects the table-metadata accessors:

```fsharp
[<AttachMembers>]
type Repository =
    static member DefinedTerm : Table<DefinedTermRow> = ...
    static member Plan : Table<PlanRow> = ...
    // ... all 17 tables ...
    static member EntityTables : string[] = [| ... |]
    static member AssociationTables : string[] = [| ... |]
```

Repeat the per-table CRUD pattern for all 17 tables. Avoid clever generic metaprogramming until the duplication hurts in practice. Do not overload `insert` / `get` etc. — Fable would shadow them under `[<AttachMembers>]`. If a second arity is needed, give it a distinct name.

The metadata holder `Table<'row>` is internal to the implementation. It currently uses an F# record; convert to an `[<AttachMembers>]` class only if it ever appears on a public signature.

Current status: not implemented. `src/ProcessCore/SQL/Repository.fs` currently contains `Table<'row>` metadata for the 17 tables only. It does not yet expose `insert`, `update`, `delete`, `get`, `list`, view readers, or transaction helpers.

Views:

- `ProcessEdges.list`
- `AnnotationOrphans.list`

Transactions:

- Add a small transaction helper in the driver layer if all target connectors can support it consistently.
- Otherwise expose explicit `BEGIN`, `COMMIT`, `ROLLBACK` helpers as SQL commands.

## Fixtures

Use the existing seeded artifacts as fixtures:

- [schemas/sql/001_core.sql](../schemas/sql/001_core.sql)
- [schemas/sql/seed_example.sql](../schemas/sql/seed_example.sql)
- [schemas/sql/seeded_core.sqlite](../schemas/sql/seeded_core.sqlite)

For tests, prefer creating a fresh temporary database from `001_core.sql` and `seed_example.sql` per test suite. The committed `seeded_core.sqlite` is useful for smoke tests and manual inspection, but tests should not mutate it.

Fixture helpers (live in `tests/ProcessCore.Tests/SQL/Fixtures.fs`, one set per target via `#if FABLE_COMPILER_*`):

- `readFixture relativePath` — returns the schema/seed file content as a string. Each target uses its native filesystem API (`System.IO.File.ReadAllText` for .NET, `node:fs.readFileSync` for JS, Python `open(...).read()` via `[<Emit>]` for Python).
- `createEmptyDriver ()` — opens an in-memory database (`Sqlite.openInMemory` / `BetterSqlite.openInMemory` / `PythonSqliteDriver.createInMemory`) and applies `001_core.sql`. Returns an `ISqliteDriver`.
- `createSeededDriver ()` — extends `createEmptyDriver` with `seed_example.sql`.

These three helpers are the entire test surface — every test module receives an `ISqliteDriver` from one of them and stays driver-agnostic. Target-specific temp-file creation, when needed, belongs in `Fixtures.fs`, not in row codecs.

## Tests

A **single transpilable test project** at `tests/ProcessCore.Tests` runs the same Pyxpecto suite against every target. SQL test modules know nothing about which driver is in use — they take an `ISqliteDriver` from `SQL/Fixtures.fs` and exercise the public shared API. Target selection happens in two places only:

1. `Fixtures.fs` — `#if FABLE_COMPILER_JAVASCRIPT` / `FABLE_COMPILER_PYTHON` / else, picking the matching adapter and filesystem helper.
2. `ProcessCore.Tests.fsproj` — `ProjectReference`s gated by the same Fable compiler symbols, so each transpilation pulls in the matching ProcessCore project variant:

   ```xml
   <ProjectReference Include="..\..\src\ProcessCore\ProcessCore.fsproj"            Condition="'$(FABLE_COMPILER)' != 'true'" />
   <ProjectReference Include="..\..\src\ProcessCore\ProcessCore.Javascript.fsproj" Condition="'$(FABLE_COMPILER_JAVASCRIPT)' == 'true' Or '$(FABLE_COMPILER_TYPESCRIPT)' == 'true'" />
   <ProjectReference Include="..\..\src\ProcessCore\ProcessCore.Python.fsproj"     Condition="'$(FABLE_COMPILER_PYTHON)' == 'true'" />
   ```

`Main.fs` builds one `testList "ProcessCore.SQL"` from all module-level `tests` values and runs it via `Pyxpecto.runTests`. The `!!` operator from `Fable.Core.JsInterop` only opens under JS/TS targets so the same `Main.fs` compiles unchanged everywhere.

Adding a new test:

- Add a new `*Tests.fs` module exposing a `let tests = testList "..." [ ... ]`.
- Append it to `ProcessCore.Tests.fsproj` *before* `Main.fs` (compile order matters).
- Append the module's `tests` to the `all` list in `Main.fs`.
- The test runs under .NET, JS, and Python without further changes — provided it only uses public types and the fixture helpers.

Use Fable.Pyxpecto for portable tests. Its README documents Expecto-like `testList`, `testCase`, and `testCaseAsync`, and runners for .NET, JavaScript, TypeScript, and Python.

Test groups:

1. **Schema Smoke**
   - `001_core.sql` creates 17 tables.
   - `seed_example.sql` loads without FK errors.
   - `PRAGMA foreign_key_check` returns no rows.
   - `annotation_orphans` returns no rows for the seed.

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
   - FK `RESTRICT` blocks deleting referenced `annotation`.
   - owner delete cascades to owner association rows.

5. **Views**
   - `process_edges` returns the growth and measurement edges.
   - `annotation_orphans` surfaces an intentionally inserted orphan PV.

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
- Added SQL support under the consolidated `src/ProcessCore` project.
- Added shared SQL primitives, one-to-one table row records, row codec modules, table metadata, and explicit runtime connector planning stubs.
- Added a BuildProject-template-based FAKE build project under `build/` with wrapper scripts and the planned target names.
- Added SQL coverage to the unified `ProcessCore.Tests` executable using Fable.Pyxpecto.
- JavaScript Fable transpilation/test targets are wired. TypeScript and Python targets still exist as explicit pending targets.

### Phase 2 — Shared Table Model

- Status: complete for the Fable-compat migration.
- Done: converted the 17 table row records and two view records to `[<AttachMembers>]` classes with positional constructors, mutable `member val` properties, and `[<NamedParams>] create factories.
- Done: re-decorated `SqlValue` with `[<Erase>]` and `ProcessIoDirection` with `[<StringEnum>]`; removed the public `Sql` / `ofSql` helpers from the enum surface.
- Done: replaced tuple/list SQL parameters with `SqlParameter[]`; `ISqliteDriver.Query` now returns `SqlRow[]`.
- Done: moved codecs onto row classes as `RowType.ofRow` and `row.ToParameters()`.
- Done: replaced the `Repository` module with a `[<AttachMembers>]` `Repository` class and array-based table metadata.
- Done: updated .NET and JavaScript adapters/tests to the new public shape.
- Verified: `.\build.cmd RunTests` and `.\build.cmd TestJs` passed after migration.
- Legacy context: the original phase landed table-shaped shared code, but predates the *Public-surface checklist* above. The completed migration covered:
  - Convert the 17 row records (and the two view records) to `[<AttachMembers>]` classes per the *Table Types* pattern.
  - Re-decorate `SqlValue` with `[<Erase>]` and `ProcessIoDirection` with `[<StringEnum>]`; delete the now-unused `ProcessIoDirection.Sql`/`ofSql` helpers.
  - Replace the tuple-based `SqlParameters = (string * SqlValue) list` with the `SqlParameter[]` shape from *I/O Boundary*; update `ISqliteDriver` to match and to return `SqlRow[]`.
  - Move codec free functions from `module RowCodecs` onto each row class as `static member ofRow` + `member ToParameters` (per the *Row Codecs* section).
  - Replace the `Repository` module with the `Repository` class.
- Not part of this phase: CRUD/repository operations. Those remain in Phase 3.

### Phase 3 — .NET Driver and Repository

- Status: partially complete.
- Done: implemented the .NET SQLite driver adapter with `Microsoft.Data.Sqlite`.
- Done: added .NET tests that create an in-memory database from `001_core.sql` and `seed_example.sql`, check FK health, bind parameters, and read seeded rows through shared codecs.
- Done: implemented table-shaped CRUD classes for all 17 tables (`insert`, `update`, `delete`, `get`, `list`), with composite-key accessors for association tables.
- Done: added view readers for `process_edges` and `annotation_orphans`.
- Done: moved the driver and CRUD tests into the shared `tests/ProcessCore.Tests` project with target-specific driver fixtures for .NET, JavaScript, and Python.
- Pending: add transaction helpers or document explicit `BEGIN` / `COMMIT` / `ROLLBACK` usage.

### Phase 4 — Python Driver

- Status: started; Python adapter, uv-based runtime dependency, and `TranspilePy` / `TestPy` build targets are wired against the shared SQL test project.
- Done: added the Python driver to `src/ProcessCore/SQL/` and the `ProcessCore.Python.fsproj` target variant.
- Done: implemented `PythonSqliteDriver` over Python stdlib `sqlite3` via Fable Python interop.
- Done: Python now reuses `tests/ProcessCore.Tests/SQL` via Fable Python; only the fixture selects `PythonSqliteDriver`.
- Done: added root `pyproject.toml` and `uv.lock` for Python tooling; `fable-library==5.0.0` provides the Python Fable runtime.
- Done: wired `TranspilePy` and `TestPy`; `TestPy` runs `uv run python build/out/py-tests/main.py --fail-on-focused-tests`.
- Done: added `RunTestsAll` to run .NET, JavaScript, then Python test execution in sequence.
- Pending: verify the expanded shared Python suite after the fixture consolidation.
- Pending: decide whether to append `TestPy` to the default `RunTests` aggregate now, or keep it explicit behind `RunTestsAll` until Python support is less experimental.

#### Prerequisite

The Phase 2 Fable-compat migration must complete first. Without `[<AttachMembers>]` / `Array<'T>` / `[<Erase>]` / `[<StringEnum>]` migration, Fable Python output is awkward (boxed DUs, linked-list helpers, no kw-only `create`).

#### Project layout additions

```text
src/ProcessCore/
  ProcessCore.Python.fsproj              # Fable F# project compiled to Python
  SQL/
    PythonSqliteDriver.fs                # ISqliteDriver impl using Python stdlib sqlite3
    PythonInterop.fs                     # Fable [<Import>]/[<Emit>] bindings for sqlite3

tests/ProcessCore.Tests/
  ProcessCore.Tests.fsproj               # Shared Pyxpecto test project for .NET / JS / Python
  Main.fs
  SQL/
    Fixtures.fs                          # Conditional driver fixture selected by Fable target

build/output/python/                     # Fable transpilation output (gitignored)
  process_core_sql/                      # transpiled shared lib
  process_core_sql_python/               # transpiled adapter
  tests/                                 # transpiled tests

pyproject.toml                           # at repo root for Python tooling/deps
```

Current implementation note: JavaScript and Python both transpile `tests/ProcessCore.Tests/ProcessCore.Tests.fsproj`; Fable output still goes to `build/out/js-tests/` and `build/out/py-tests/`.

#### Tooling decisions

- **Python version**: pin to `>=3.11` (matches Fable Python's tested baseline).
- **Package manager**: `uv`.
- **Test runner**: Fable.Pyxpecto's Python runner executes directly via `uv run python build/out/py-tests/main.py --fail-on-focused-tests`; it is not a pytest suite.
- **Connector**: stdlib `sqlite3` only; no third-party SQLite package.

#### Fable Python bindings for `sqlite3`

`PythonInterop.fs` carries the thin Fable bindings — minimal, only what `PythonSqliteDriver.fs` consumes:

```fsharp
namespace ProcessCore.SQL.Platform.Python

open Fable.Core
open Fable.Core.PyInterop

[<AllowNullLiteral; Interface>]
type Cursor =
    abstract execute : sql: string * parameters: obj -> Cursor
    abstract fetchall : unit -> obj[]
    abstract fetchone : unit -> obj
    [<Emit("$0.description")>] abstract description : obj[]

and [<AllowNullLiteral; Interface>] Connection =
    abstract cursor : unit -> Cursor
    abstract commit : unit -> unit
    abstract close : unit -> unit
    abstract executescript : sql: string -> unit

[<Import("connect", from = "sqlite3")>]
let connect (path: string) : Connection = nativeOnly
```

#### `PythonSqliteDriver` implementation

```fsharp
[<AttachMembers>]
type PythonSqliteDriver(connection: Connection) =

    interface ISqliteDriver with
        member _.Execute sql parameters =
            let cursor = connection.cursor ()
            cursor.execute (sql, toPyDict parameters) |> ignore
            connection.commit ()

        member _.Query sql parameters =
            let cursor = connection.cursor ()
            cursor.execute (sql, toPyDict parameters) |> ignore
            let columns = cursor.description |> Array.map (fun col -> col?(0) :?> string)
            cursor.fetchall () |> Array.map (fun row -> rowToSqlRow columns row)

        member this.Scalar sql parameters =
            ((this :> ISqliteDriver).Query sql parameters)
            |> Array.head
            |> Map.toArray
            |> Array.head
            |> snd

    [<NamedParams>]
    static member create (Path: string) =
        PythonSqliteDriver (connect Path)

    [<NamedParams>]
    static member createInMemory () =
        PythonSqliteDriver (connect ":memory:")
```

Notes:

- `parameters` are converted to a Python dict inside `toPyDict` / `parametersToPyDict`. The implementation strips `$` / `@` / `:` prefixes from parameter names because Python `sqlite3` accepts `$name` placeholders in SQL but expects `name` keys in the parameter dict.
- Map `cursor.description` columns + row tuples to `SqlRow` (`Map<string, SqlValue>`) at the adapter boundary, so the shared codec layer receives the same shape as on .NET / JS.
- `Execute` uses `connection.executescript` when parameters are empty so schema/seed scripts with multiple statements work. Parameterized statements use `cursor.execute`.
- Fable `int32` values must be converted to native Python `int` before binding to sqlite3.
- `commit` per `Execute`: keep the simple write semantics for now; a transaction helper comes in Phase 6.

#### Build pipeline wiring

Add to `build/Build.fs`:

- `TranspilePy` target — runs Fable against `tests/ProcessCore.Tests/ProcessCore.Tests.fsproj`; conditional project references bring in the shared library and Python adapter output.
- `TestPy` target — runs `uv run python build/out/py-tests/main.py --fail-on-focused-tests`; depends on `TranspilePy`.
- `RunTestsAll` aggregate — runs .NET Pyxpecto tests, JavaScript transpile/test, and Python transpile/test sequentially.
- `RunTests` aggregate — append `TestPy` once green on a developer machine, or keep `RunTestsAll` as the explicit cross-runtime target.

Wrapper-script additions: `.\build.cmd TranspilePy`, `.\build.cmd TestPy`.

#### Python tests

Reuse shared test modules unchanged — they're written against the public `ISqliteDriver` and shared codecs. `Fixtures.fs` is conditional rather than Python-only:

- .NET uses `Sqlite.openInMemory`.
- JavaScript uses `BetterSqlite.openInMemory`.
- Python uses `PythonSqliteDriver.createInMemory`.
- All three read `001_core.sql` and `seed_example.sql` from the repo root and expose `createEmptyDriver` / `createSeededDriver`.

The shared test list from `tests/ProcessCore.Tests/SQL/` compiles under Python without test-body changes once the fixture selects the Python adapter.

#### Verification

1. `.\build.cmd TranspilePy` runs cleanly and produces `build/out/py-tests/`.
2. Spot-check one transpiled file under `build/out/py-tests/src/ProcessCore/SQL/` to confirm `DefinedTermRow` is a regular Python class with kw-only `create`, no boxed DU helpers around `SqlValue`, and `Array` parameters lower to `list[T]`.
3. `.\build.cmd TestPy` exits 0 on the shared SQL suite.
4. Manual smoke: `python -c "from process_core_sql_python.python_sqlite_driver import PythonSqliteDriver; d = PythonSqliteDriver.create_in_memory(); ..."` confirms the named-arg `create` and mutable members carry into Python.

### Phase 5 — JS/TS Drivers

- Status: started; JavaScript adapter project, `better-sqlite3` binding, Node tooling, shared SQL Pyxpecto test project, and `TestJs` build target are wired. TypeScript remains pending.
- Done: chose `better-sqlite3` as the Node SQLite connector for the synchronous driver boundary.
- Done: introduced Node tooling with `package.json`, `package-lock.json`, npm scripts, generated output ignores, and `better-sqlite3` dependency management.
- Done: wired Fable JavaScript transpilation and `TestJs` into the BuildProject pipeline.
- Done: JavaScript now reuses `tests/ProcessCore.Tests/SQL` via Fable JavaScript; only the fixture selects `BetterSqlite`.
- Pending: verify the expanded shared JavaScript suite after the fixture consolidation.
- Pending: decide whether TypeScript gets a distinct binding or reuses the JavaScript binding output.
- Pending: wire TypeScript transpilation and tests.

### Next Implementation Step — Repository Coverage and Transactions

- Verify `RunTestsAll` after the shared fixture consolidation.
- Add transaction helpers or document explicit `BEGIN` / `COMMIT` / `ROLLBACK` usage.
- Decide whether `TestPy` should join the default `RunTests` aggregate now that the uv-backed smoke suite is green.

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
- Does `Map<string, SqlValue>` lower to a native `dict[str, SqlValue]` in Fable Python, or to a custom map class?
- Should `TestPy` be included in the default `RunTests` aggregate now that the uv-backed smoke tests pass?
