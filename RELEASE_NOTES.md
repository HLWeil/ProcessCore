### 0.0.5+a302f6f (Released 2026-7-3)
* Additions:
    * [[#df3cd8a](https://github.com/HLWeil/ProcessCore/commit/df3cd8aedac00ca23b3f989c55fa5f464352ff7f)] extend ARC api
    * [[#7cee354](https://github.com/HLWeil/ProcessCore/commit/7cee354611ffbae4cd257e18646720e5d8008b69)] update script
* Bugfixes:
    * [[#a302f6f](https://github.com/HLWeil/ProcessCore/commit/a302f6f37e70dd4eb8391b28e72fa335b1f58df9)] fix annotation resolving in dataset parsing
    * [[#4274ed7](https://github.com/HLWeil/ProcessCore/commit/4274ed78d64f7455fbf09fd1b8d63649ab937d5b)] various spreadsheet parser fixes
    * [[#b17a32b](https://github.com/HLWeil/ProcessCore/commit/b17a32b6a1381fb5b6ca5ed0187df20960bc7e77)] yml parser fixes
    * [[#b76860b](https://github.com/HLWeil/ProcessCore/commit/b76860b845a845a2d9addb9eb5083625dbda67d4)] fix case of file reference in project file
    * [[#5235ad4](https://github.com/HLWeil/ProcessCore/commit/5235ad43ea47f352e0cf7e05bc78d49d6c9e3be6)] fix javascript transpilation

### 0.0.4+6a94245 (Released 2026-7-2)
* Bugfixes:
    * [[#6a94245](https://github.com/HLWeil/ProcessCore/commit/6a94245b3e28d96494279f6a6fac825ea00a29e2)] various fixes for ARC load from Scaffold

### 0.0.3+4669efd (Released 2026-7-2)
* Additions:
    * [[#815b119](https://github.com/HLWeil/ProcessCore/commit/815b1191a8076ccf00868cf179feb75088ee3eae)] rename some core types and properties
    * [[#3b4ed32](https://github.com/HLWeil/ProcessCore/commit/3b4ed324f87f824661cce741356f370bf2af03b8)] rename plan to recipe
    * [[#70ae751](https://github.com/HLWeil/ProcessCore/commit/70ae751153eec99341ec1221007102048489b7b1)] first round of changes for adding datamap and administrative metadata
    * [[#bbb0d48](https://github.com/HLWeil/ProcessCore/commit/bbb0d48b985681cff1bf9e4fcd5819fd24f654b3)] finish up first round of administrative/datamap integration
    * [[#d6ea4eb](https://github.com/HLWeil/ProcessCore/commit/d6ea4eb582de65a4a4edd2f36da0422d98455779)] adjust frontmatter in docs
    * [[#0d58bcf](https://github.com/HLWeil/ProcessCore/commit/0d58bcf08538a5d99cc2abe0a9bf6487d9dae05b)] finish up split into 3 parallel profiles
    * [[#b5ad576](https://github.com/HLWeil/ProcessCore/commit/b5ad5765fbe15eacc3912a1910136889156f3335)] make docs sidebar more compact
    * [[#6563320](https://github.com/HLWeil/ProcessCore/commit/65633200662f1a8c425cc199f47c4cfdbb22665b)] add some basic datacontext API and docs
    * [[#e5ebff4](https://github.com/HLWeil/ProcessCore/commit/e5ebff48f50c0143c698be13ff64f64120f003ba)] add filesystem access from arctrl
    * [[#28c342a](https://github.com/HLWeil/ProcessCore/commit/28c342a0ee59397da1e74d8dd5a42da289b590e2)] start adding spreadsheet read-in
    * [[#c4466a5](https://github.com/HLWeil/ProcessCore/commit/c4466a50b72bb0fd5b9e5f0bcc2bc8e2d517e52c)] finish up first version of scaffold reader
* Bugfixes:
    * [[#d182faa](https://github.com/HLWeil/ProcessCore/commit/d182faa6cabee169f4cb1ba2dd8b57ca021e91d6)] small fix to mermaid edges
    * [[#4669efd](https://github.com/HLWeil/ProcessCore/commit/4669efd717a25b89a901c203fd923b247820ab9b)] small fix

### 0.0.2+e46fea4 (Released 2026-7-2)
    * Add Dataset.CollapseProcesses() method to collapse linear chains of processes into single processes with the same overall inputs, outputs, and protocol.

### 0.0.1 (Released 2026-7-2)
    * Added the ProcessCore F# implementation with mutable core types for `Dataset`, `Process`, `Recipe`, `Sample`, `Data`, `Annotation`, `FormalParameter`, and `DefinedTerm`.
    * Added graph-maintenance behavior for process I/O back-edges, dataset nesting, node canonicalization, root/final node discovery, connected-node traversal, upstream/downstream traversal, and path queries.
    * Added fragment-aware `Data` support with `path`, optional `selector`, optional `selectorFormat`, optional `encodingFormat`, and pluggable fragment selector providers.
    * Added the live table projection layer over ProcessCore graphs, including composite headers/cells/columns, table decomposition, row and column mutation APIs, and dataset-level table grouping.
    * Added YAML codecs for the core model using YAMLicious, including strict and lenient type modes, dynamic overflow fields, round-trip helpers, and example-file coverage.
    * Added a SQLite SQL import profile with 8 entity tables, 9 association tables, graph-oriented indexes, `process_edges`, and `annotation_orphans`.
    * Added SQL row classes, row codecs, repository metadata, table CRUD facades, and view readers for the SQLite profile.
    * Added SQLite driver adapters for .NET (`Microsoft.Data.Sqlite`), JavaScript (`better-sqlite3` through Fable), and Python (`sqlite3` through Fable Python).
    * Added shared Pyxpecto coverage for the core model, graph traversal, table projection, YAML codecs, SQL row codecs, repository CRUD, and runtime-specific SQL drivers.
    * Added FAKE build targets for solution builds, .NET/JavaScript/Python tests, documentation builds, packaging, and release publishing.
    * Added fsdocs documentation pages for the core implementation, YAML parsing, querying, fragment selector providers, tables, and project/specification guides.
    * Added package metadata and multi-target project variants for .NET, JavaScript, and Python/Fable output.
    * Changed the release scope from the initial SQL scaffold into the consolidated ProcessCore implementation workspace.
    * Aligned current code and tests around the core vocabulary `Process`, `Recipe`, `inputs`, `outputs`, and `executesProtocol`.
    * Changed `RunTests` to execute the .NET, Python, and JavaScript Pyxpecto paths through the FAKE build.
    * Known gap: TypeScript packaging/testing is still not wired as a separate supported runtime.
    * Known gap: SQL transaction helpers are not yet exposed; callers can still issue explicit `BEGIN`, `COMMIT`, and `ROLLBACK` through the driver.
    * Known gap: ISA, Workflow Run, and Datamap decoration-specific runtime libraries remain future work on top of the core model.

