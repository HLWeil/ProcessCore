# Release Notes


## 0.0.2

- Add Dataset.CollapseProcesses() method to collapse linear chains of processes into single processes with the same overall inputs, outputs, and protocol.

## 0.0.1

- Added the ProcessCore F# implementation with mutable core types for `Dataset`, `Process`, `Plan`, `Sample`, `Data`, `Annotation`, `FormalParameter`, and `DefinedTerm`.
- Added graph-maintenance behavior for process I/O back-edges, dataset nesting, node canonicalization, root/final node discovery, connected-node traversal, upstream/downstream traversal, and path queries.
- Added fragment-aware `Data` support with `path`, optional `selector`, optional `selectorFormat`, optional `encodingFormat`, and pluggable fragment selector providers.
- Added the live table projection layer over ProcessCore graphs, including composite headers/cells/columns, table decomposition, row and column mutation APIs, and dataset-level table grouping.
- Added YAML codecs for the core model using YAMLicious, including strict and lenient type modes, dynamic overflow fields, round-trip helpers, and example-file coverage.
- Added a SQLite SQL import profile with 8 entity tables, 9 association tables, graph-oriented indexes, `process_edges`, and `annotation_orphans`.
- Added SQL row classes, row codecs, repository metadata, table CRUD facades, and view readers for the SQLite profile.
- Added SQLite driver adapters for .NET (`Microsoft.Data.Sqlite`), JavaScript (`better-sqlite3` through Fable), and Python (`sqlite3` through Fable Python).
- Added shared Pyxpecto coverage for the core model, graph traversal, table projection, YAML codecs, SQL row codecs, repository CRUD, and runtime-specific SQL drivers.
- Added FAKE build targets for solution builds, .NET/JavaScript/Python tests, documentation builds, packaging, and release publishing.
- Added fsdocs documentation pages for the core implementation, YAML parsing, querying, fragment selector providers, tables, and project/specification guides.
- Added package metadata and multi-target project variants for .NET, JavaScript, and Python/Fable output.
- Changed the release scope from the initial SQL scaffold into the consolidated ProcessCore implementation workspace.
- Aligned current code and tests around the core vocabulary `Process`, `Plan`, `inputs`, `outputs`, and `executesProtocol`.
- Changed `RunTests` to execute the .NET, Python, and JavaScript Pyxpecto paths through the FAKE build.
- Known gap: TypeScript packaging/testing is still not wired as a separate supported runtime.
- Known gap: SQL transaction helpers are not yet exposed; callers can still issue explicit `BEGIN`, `COMMIT`, and `ROLLBACK` through the driver.
- Known gap: ISA, Workflow Run, and Datamap decoration-specific runtime libraries remain future work on top of the core model.
