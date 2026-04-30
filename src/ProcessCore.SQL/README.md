# ProcessCore.SQL

Fable-friendly F# skeleton for the ProcessCore SQLite import profile in `schemas/sql`.

The project currently contains only shared code:

- `Sql.fs` defines the portable SQLite value/row shape and `ISqliteDriver`.
- `Tables.fs` defines one F# record per SQL table plus rows for the current views.
- `RowCodecs.fs` maps `SqlRow` values to records and records to named SQL parameters.
- `Repository.fs` records table metadata for later CRUD modules.
- `Platform/Driver.fs` documents the runtime adapter seam for .NET, JavaScript/TypeScript, and Python.

Concrete SQLite connector packages are intentionally kept out of this shared project. Runtime-specific code lives behind the `ISqliteDriver` boundary so the table model and codecs remain transpilable with Fable.

The .NET adapter is in `src/ProcessCore.SQL.DotNet` and implements `ISqliteDriver` with `Microsoft.Data.Sqlite`.
