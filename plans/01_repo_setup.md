# Project Layout Plan: ARC Data Model Specification Repository

## Context

This repository will become the specification home for the ARC Data Model. The model is built on a **ProcessCore** abstraction — a graph connecting sources to data via processes, extensible via Annotations. Two "decoration" proof-of-concepts (ISA and WorkflowRun) demonstrate how domain-specific models map onto ProcessCore. The specs must also be representable as SQL and document-DB schemas.

Currently, the repo has a flat structure with a few markdown files, YAML examples, and RO-Crate profile references — all at the root or in `roc-profiles/`. This plan reorganizes it into a clean, scalable layout.

## Proposed Structure

```
ARC-Data-Model/
├── README.md                              # Project overview, principles, links
├── AGENTS.md                              # Agent instructions
│
├── spec/
│   ├── README.md                          # Spec reading guide, design principles
│   │
│   ├── core/                              # ProcessCore specification
│   │   ├── README.md                      # Core model overview + process graph diagram
│   │   ├── Dataset.md                     # Container/context for processes
│   │   ├── Process.md                     # Core transformation node (inputs → outputs)
│   │   ├── Protocol.md                    # Planned procedure description
│   │   ├── Sample.md                    # Input/output samples (sources, samples)
│   │   ├── Data.md                        # Data files
│   │   ├── Annotation.md               # Extensible key-value-unit triples
│   │   ├── Person.md                      # Contributors
│   │   └── DefinedTerm.md                 # Ontology annotations
│   │
│   ├── decorations/
│   │   ├── README.md                      # What decorations are, extension mechanism
│   │   │
│   │   ├── isa/                           # ISA decoration
│   │   │   ├── README.md                  # Overview + mapping table (core → ISA)
│   │   │   ├── Investigation.md           # Dataset → Investigation
│   │   │   ├── Study.md                   # Dataset → Study
│   │   │   ├── Assay.md                   # Dataset → Assay
│   │   │   ├── Process.md              # Process → Process
│   │   │   ├── Plan.md             # Protocol → Plan
│   │   │   ├── Sample.md                  # Sample → Sample/Source
│   │   │   └── Annotations.md          # Parameter, Characteristic, Factor, Component
│   │   │
│   │   └── workflow-run/                  # Workflow Run decoration
│   │       ├── README.md                  # Overview + mapping table (core → WR)
│   │       ├── ArcWorkflow.md             # Dataset → ARC Workflow
│   │       ├── ArcRun.md                  # Dataset → ARC Run
│   │       ├── WorkflowProtocol.md        # Protocol → Workflow Protocol
│   │       ├── WorkflowInvocation.md      # Process → Workflow Invocation
│   │       ├── FormalParameter.md         # WR-specific entity
│   │       └── Annotations.md          # Workflow Input, Prefix, Position
│   │
│   └── querying/
│       └── use-cases.md                   # Query patterns on the process graph
│
├── schemas/
│   ├── README.md                          # How schemas relate to the spec
│   ├── sql/                               # SQL schema representations (future)
│   └── document-db/                       # Document DB representations (future)
│
├── examples/
│   ├── README.md                          # Index of examples
│   ├── isa/
│   │   ├── investigation.yml              # ← isa.investigation.yml
│   │   ├── assay_proteomics.yml           # ← isa.assay_proteomics-example.yml
│   │   └── datamap_proteomics.yml         # ← isa.datamap_proteomics.yml
│   └── workflow-run/                      # (future WR examples)
│
└── references/
    ├── README.md                          # What these reference specs are
    ├── isa_ro_crate.md                    # ← roc-profiles/isa_ro_crate.md
    └── arc_wr_ro_crate.md                 # ← roc-profiles/arc_wr_ro_crate.md
```

## Key Design Decisions

1. **`spec/` as the normative home** — separates the specification from supporting sample (examples, schemas, references). Everything under `spec/` is "what the model IS."

2. **One file per core type** — each ProcessCore entity gets its own markdown file with properties, relationships, and constraints. Enables direct cross-linking from decoration specs (e.g., ISA `Investigation.md` references `../../core/Dataset.md`).

3. **Decorations as subdirectories** — each decoration is a self-contained folder under `spec/decorations/`. Adding a future decoration (e.g., Galaxy, CWL) means adding a new folder — no changes to core or other decorations.

4. **Annotation subtypes grouped per decoration** — Parameter/Characteristic/Factor/Component share the same base structure and differ only by `additionalType`. One file per decoration keeps them together rather than creating many tiny files.

5. **`references/` replaces `roc-profiles/`** — more general name, accommodates future non-RO-Crate reference material. Files preserved unchanged as upstream specs.

6. **`schemas/` at root level** — SQL and document-DB schemas are *derived representations* of the spec, not the spec itself. Kept separate to make this clear. Initially empty with READMEs.

7. **`examples/` organized by decoration** — existing YAML examples are ISA-specific, so they go under `examples/isa/`. Cleaner filenames (drop `isa.` prefix and `-example` suffix).

8. **Naming conventions**: PascalCase for type spec files (matches schema.org/Bioschemas names), lowercase-with-hyphens for directories, lowercase-with-underscores for example files.

## Migration Steps

1. Create directory structure: `spec/core/`, `spec/decorations/isa/`, `spec/decorations/workflow-run/`, `spec/querying/`, `schemas/sql/`, `schemas/document-db/`, `examples/isa/`, `examples/workflow-run/`, `references/`
2. Move files:
   - `roc-profiles/*.md` → `references/`
   - `isa.investigation.yml` → `examples/isa/investigation.yml`
   - `isa.assay_proteomics-example.yml` → `examples/isa/assay_proteomics.yml`
   - `isa.datamap_proteomics.yml` → `examples/isa/datamap_proteomics.yml`
   - `ARC_ProcessCore_Querying.md` → `spec/querying/use-cases.md`
3. Create README.md stubs for each directory (short description + purpose)
4. Create core type spec stubs under `spec/core/` (entity name, one-line description, empty property table template)
5. Create decoration spec stubs under `spec/decorations/isa/` and `spec/decorations/workflow-run/` (reference to core type + upstream profile)
6. Update root `README.md` to reflect the new structure and project principles
7. Remove empty `roc-profiles/` directory

## What is NOT included (intentional)

- No `src/` or `lib/` — this is a spec repo, not code
- No `docs/` — the spec IS the documentation
- No version folders (`v1/`, `v2/`) — use git tags/branches instead
- No build/site-gen config — can be added later if needed
- Spec file content beyond stubs — that's a separate task after layout is approved

## Verification

- All existing files are preserved (moved, not deleted)
- Directory tree matches the proposed structure
- All README.md files exist with at least a title and one-line description
- Links between spec files resolve correctly (relative paths)
- `git status` shows moves (not delete+create) where possible
