# Project Context

<!-- One-liner: what is this project? -->
This repo contains the core data model specifications for the ARC ecosystem. It serves as a reference for the design and implementation of the data structures used in core tooling across nfdi4plants, including ARCtrl and related applications.

## Architecture

<!-- Where things live. The agent will grep/glob to explore, but this saves tokens and wrong turns. -->
This is not an implementation repo, it is where specs live.
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
│   │   ├── Material.md                    # Input/output materials (sources, samples)
│   │   ├── Data.md                        # Data files
│   │   ├── PropertyValue.md               # Extensible key-value-unit triples
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
│   │   │   ├── LabProcess.md              # Process → LabProcess
│   │   │   ├── LabProtocol.md             # Protocol → LabProtocol
│   │   │   ├── Sample.md                  # Material → Sample/Source
│   │   │   └── PropertyValues.md          # Parameter, Characteristic, Factor, Component
│   │   │
│   │   └── workflow-run/                  # Workflow Run decoration
│   │       ├── README.md                  # Overview + mapping table (core → WR)
│   │       ├── ArcWorkflow.md             # Dataset → ARC Workflow
│   │       ├── ArcRun.md                  # Dataset → ARC Run
│   │       ├── WorkflowProtocol.md        # Protocol → Workflow Protocol
│   │       ├── WorkflowInvocation.md      # Process → Workflow Invocation
│   │       ├── FormalParameter.md         # WR-specific entity
│   │       └── PropertyValues.md          # Workflow Input, Prefix, Position
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
    ├── arc_datamap_ro_crate.md            # ← roc-profiles/arc_datamap_ro_crate.md
    └── arc_wr_ro_crate.md                 # ← roc-profiles/arc_wr_ro_crate.md
```
<!-- Add key boundaries the agent must respect: -->
<!-- - "All database access goes through src/db/, never import the ORM directly in route handlers" -->
<!-- - "src/legacy/ is frozen — read but never modify" -->

## Tech Stack

<!-- Be specific about versions. Agents default to whatever was common in training data. -->

## Commands

<!-- Exact strings. Agents use these verbatim. -->

## Code Style

<!-- Only rules a linter can't enforce. If ruff/prettier/eslint handles it, don't repeat it here. -->

## Testing

## Git & Commits

- Conventional commits: `feat:`, `fix:`, `refactor:`, `docs:`, `test:`, `chore:`.
- One logical change per commit. Keep diffs reviewable.
- Never force-push to `main`.

## Prohibitions

<!-- Things the agent must never do. Be explicit — agents are eager to help. -->

- Do NOT add new production dependencies without asking first.

## Verification

<!-- What must pass before the agent considers a task complete. -->

Before marking work as done:

## Gotchas

<!-- Add real failure points as you discover them. This section is the highest-signal content. -->
<!-- Examples: -->
<!-- - "The ORM lazy-loads by default. Always use `selectinload()` in queries or you get N+1." -->
<!-- - "The CI runner has no network access. Mock all external API calls." -->
<!-- - "Environment variables are in .env.example, not .env. Copy first." -->