# Project Context

ARC Data Model is the specification and implementation workspace for the ARC process data model. It contains the normative markdown spec, derived SQL/YAML schema artifacts, examples, reference material, fsdocs documentation, and F# libraries for ProcessCore and the SQL profile.

## Architecture

```text
ARC-Data-Model/
├── docs/                         fsdocs documentation pages
│   ├── index.md
│   ├── _head.html                 fsdocs head injection, including Mermaid support
│   └── project/                   canonical project guides
│   └── spec/                      normative model specification
│       ├── core/                  ProcessCore entities
│       ├── decorations/           ISA, Workflow Run, and Datamap decorations
│       └── querying/              query use cases and graph traversal notes
├── spec/                         compatibility pointer to docs/spec
├── schemas/                      derived schema representations
│   ├── sql/                       executable SQLite profile and design notes
│   ├── yml/                       JSON Schema draft 2020-12 expressed in YAML
│   └── document-db/               placeholder for future document DB schemas
├── examples/                     concrete example documents
│   ├── core/                      schema-shaped core examples
│   ├── isa/                       legacy/profile-shaped ISA and Datamap examples
│   └── workflow-run/              placeholder for future Workflow Run examples
├── references/                   upstream profiles and preserved prior implementation notes
├── src/                          F# implementation projects
│   └── ProcessCore/              consolidated core, YAML, SQL, and Fable projects
├── tests/                        Pyxpecto tests
│   ├── ProcessCore.Tests/        consolidated core, YAML, and SQL tests
│   └── SpeedTest/
└── build/                        FAKE build project and task modules
```

## Current Vocabulary

- Core process/protocol entities are `Process` and `Recipe`.
- Core process I/O properties are `inputs`, `outputs`, and `executesProtocol`.
- Some upstream/profile-shaped examples use `object`, `result`, and `executesRecipe`; treat those as legacy/profile terminology unless the task explicitly says to preserve profile shape.
- Long-form project documentation belongs under `docs/project/`.
- Normative specification prose belongs under `docs/spec/`.
- Existing README files should stay short and link into `docs/`.

## Tech Stack

- F# / .NET projects in `src/`, currently centered on consolidated `ProcessCore` with Fable-specific project files beside it.
- FAKE build project under `build/`.
- fsdocs for generated documentation.
- Pyxpecto tests, with Fable transpilation paths for JavaScript and Python.
- JavaScript runtime tests use Node and `better-sqlite3`.
- Python runtime tests use `uv` and Python stdlib `sqlite3`.

## Commands

```powershell
.\build.cmd BuildSolution
.\build.cmd RunTests
.\build.cmd RunTestsAll
.\build.cmd TestJs
.\build.cmd TestPy
.\build.cmd BuildDocs
.\build.cmd WatchDocs
dotnet fsdocs watch
npm run test:js
```

## Git & Commits

- Conventional commits: `feat:`, `fix:`, `refactor:`, `docs:`, `test:`, `chore:`.
- One logical change per commit. Keep diffs reviewable.
- Never force-push to `main`.

## Prohibitions

- Do NOT add new production dependencies without asking first.
- Do NOT rewrite preserved upstream reference files unless explicitly asked; prefer documenting current behavior in `docs/project/`.

## Verification

Before marking docs plumbing work as done, run an fsdocs build or a targeted markdown/link check when practical. Before marking implementation work as done, run the relevant FAKE test target.
