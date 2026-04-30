# Plan: SQL Design Refinement

Iteration on [schemas/sql/design.md](../schemas/sql/design.md) to close gaps surfaced by cross-checking against the core spec MDs and YAML schemas.

## Goal

Resolve the 13 original issues identified in design.md, plus the framing issue **#0**, so that it accurately reflects the YAML schemas, the spec MDs, and the querying use cases — without changing the table set. The most important is **#0** (profile vs spec framing), which determines how several others are answered.

## Issue summary

| # | Severity | Issue | Phase |
|---|----------|-------|-------|
| 0 | meta | design.md blurs "faithful YAML representation" with "stricter SQL profile"; framing must come first | 0 (D0) |
| 1 | high | "Round-trip" claim is overstated; `type` column policy understates which tables actually have a `const` type in YAML | 0 (D1) + 1 (E6) |
| 2 | high | `oneOf [string @id, inline object]` resolution rule is implicit; mixed-target lists need multi-table lookup policy; inline objects can lack `id` | 1 (E1) |
| 3 | high | `LabProtocol.intendedUse` string is ambiguous (free text vs `@id`) | 1 (E1) |
| 4 | high | `process_io` symmetry policy contradicts the spec; design.md prose ("spec does not require") is factually wrong | 0 (D2) + 1 (E5) |
| 5 | high | No index strategy for documented graph-traversal queries; composites and `process_parameter_value` lookup are missing | 1 (E2) |
| 6 | medium | `Data.id` vs `Data.path` relationship unspecified; naive `id := path` collides on fragment-level data | 1 (E3) |
| 7 | medium | Orphan `PropertyValue` policy undefined | 0 (D3) |
| 8 | medium | Numeric `PropertyValue.value` has no canonical text format | 0 (D5) |
| 9 | medium | FK `ON DELETE` semantics unspecified | 0 (D4) |
| 10 | low | Stale `core/ERD.md` link | done |
| 11 | low | `Dataset.hasPart` type column gap in spec MD | 2 (S1) |
| 12 | low | `FormalParameter.workExample` and `Dataset.creator → Person` are spec-internal discrepancies | 2 (S2, S3) |
| 13 | low | Per-owner `additional_property` tables choice should be promoted into Design Decisions | 1 (E4) |

## Decisions taken

| # | Choice | Notes |
|---|---|---|
| D0 | **(a) Profile** | Rename design.md as "SQL Import Profile of the ProcessCore model." Round-trip is to/from the profile; SQL ↔ in-memory model mapping handles deviations. |
| D1 | **add `type TEXT` for now** | Add `type TEXT` (no CHECK, no canonicalization) to `dataset`, `lab_process`, `lab_protocol`, `material`, `property_value`, `formal_parameter`, `defined_term`. Drop the "type omitted when constant" Design Decisions bullet. Local issue filed: rename `LabProcess` → `Process`, `LabProtocol` → `Protocol`. |
| D2 | **keep symmetry non-mandatory** | No CHECK or import validation on `inputs.length == outputs.length`. Fix the false prose at [design.md:444](../schemas/sql/design.md#L444) to acknowledge spec SHOULD. Local issue filed: future enforcement. |
| D3 | **(a) closed-document invariant** | At end of import transaction: every PV must be referenced by ≥1 row in the five owner association tables. `instance_of_id` does not count. |
| D4 | **(a) CASCADE owner→assoc, RESTRICT on entity refs** | Deleting a `property_value` is *blocked* if any association row still references it. Deleting an owner (e.g. `dataset`) cascades to its association rows. Forces explicit cleanup; surfaces accidents. |
| D5 | **values as pure strings** | Remove `property_value.value_type` column and its CHECKs. `value` stays TEXT nullable. Numeric/text validation moves to the in-memory model layer, driven by ontology context. |

## Phase 0 — Decision rationale

Choices are recorded in [Decisions taken](#decisions-taken). The notes below capture the rationale and downstream edit implications.

### D0. Profile vs spec

Decision: treat design.md as a SQL import profile, not a pure faithful SQL rendering of arbitrary YAML.

design.md currently blurs two activities: (a) faithfully represent the authoritative YAML/spec in SQL, and (b) define a SQL-side contract that tightens some of YAML's looser fields. The chosen framing is (b): the SQL profile is allowed to reject unresolved references, require exact-one-target FKs, disallow orphan PVs at commit time, and generate stable local IDs where the source omits IDs. Round-trip claims are therefore to/from the profile, not arbitrary YAML.

### D1. `type` column policy

Decision: add `type TEXT` columns to every entity table for now, with no `CHECK` and no canonicalization. This keeps the SQL profile close to the current spec tables while leaving the naming problem (`LabProcess`/`LabProtocol` versus `Process`/`Protocol`) to a separate local issue.

Concretely, `data.type` already exists; add `type TEXT` to:

- `dataset`
- `lab_process`
- `lab_protocol`
- `material`
- `property_value`
- `formal_parameter`
- `defined_term`

Remove the current Design Decisions bullet that says `type` columns are omitted when constant.

### D2. `process_io` symmetry

The spec ([LabProcess.md:41](../spec/core/LabProcess.md#L41)) says inputs/outputs "should be of the same length" — a SHOULD. design.md currently asserts the opposite at [line 444](../schemas/sql/design.md#L444): "the spec does not require the two lists to have equal length." That prose is factually wrong and must be fixed regardless of which option below is chosen.

Decision: physically allow asymmetry and do not add an import-time length validation yet. The "spec does not require" prose is rewritten to: *"the spec recommends equal-length lists; the profile permits asymmetric storage and does not currently enforce length equality."*

Whether this SHOULD should become a MUST, or whether a profile-level warning should be emitted, is tracked as a separate local issue.

### D3. Orphan `PropertyValue` policy

Validation level matters: per-row at insert vs closed-document at transaction commit. Per-row breaks staging and forward references; closed-document achieves the same end state without that cost.

**Definition.** A PV is *orphan* if no row references its `id` from any of the five owner association tables:

- `dataset_additional_property`
- `protocol_additional_property`
- `material_additional_property`
- `data_additional_property`
- `process_parameter_value`

`PropertyValue.instance_of_id → FormalParameter` is *outbound* from the PV and does **not** count as ownership: a PV that points at a FormalParameter but is referenced by no owner is still orphan.

Decision: closed-document invariant, no orphan PVs at commit time. PVs may exist transiently mid-import (staging, forward refs); the constraint is checked once at the end of the import transaction.

### D4. FK `ON DELETE` behavior

Decision: use `CASCADE` for owner → association rows, because those rows have no independent meaning, and `RESTRICT` for references to first-class entities.

This means deleting a `property_value` row is blocked while any association row still references it; the caller must delete or move those associations explicitly. Deleting an owner row, such as a `dataset`, cascades to that owner's association rows.

### D5. `PropertyValue.value` storage

Decision: store values as pure strings in SQL. Remove `property_value.value_type` and its consistency CHECKs. `property_value.value` remains nullable `TEXT`.

The SQL profile does not preserve the source scalar kind: YAML `42` and `"42"` both become `value = '42'`. Semantic validation, including numeric parsing based on ontology context, belongs in the SQL ↔ in-memory model layer or a separate validator.

## Phase 1 — Mechanical edits to design.md

Applied in one editing pass after Phase 0.

### E1. Add "Reference resolution" subsection (issues #2, #3)

New bullet under Design Decisions, covering five cases:

> **References resolve to FKs.** YAML schemas express references as `oneOf [string @id, inline object]`. On import:
>
> 1. **Single-target string `@id`.** Resolve against the unique target table by `id`. Unknown `@id` → error.
> 2. **Mixed-target string `@id`** (`hasPart` → Dataset|Data; `inputs`/`outputs` → Material|Data). Look up across all permitted target tables. Zero hits → error. Hits in multiple tables → error (cross-type `id` collision is a data-quality bug the user must resolve).
> 3. **Inline object with `id`** on a single-target list. Upsert by `id` into the target table before recording the FK.
> 4. **Inline object on a mixed-target list** (`hasPart`, `inputs`, `outputs`). Validate the object against each permitted target schema (`Dataset.yml`/`Data.yml` for `hasPart`; `Material.yml`/`Data.yml` for `inputs`/`outputs`). Zero matches → error. Multiple matches → error (the YAML schemas are mutually exclusive in practice; ambiguity means malformed input).
> 5. **Inline object without `id`** (permitted by YAML for `LabProcess`, `LabProtocol`, `Data`, where `id` is COULD). Generate a stable local identifier per the rule already in design.md Design Decisions, persist the row, then record the FK.
>
> This rule applies to `executesProtocol`, `defaultValue`, `instanceOf`, `parameters`, `processes`, `hasPart`, `inputs`, `outputs`, `parameterValue`, and every `additionalProperty`. **`intendedUse` is excluded** — see the disambiguation rule below — because a bare string there can mean free text rather than an unresolved `@id`.

Plus an `intendedUse`-specific clause (the one field where a bare string is overloaded):

> **`intendedUse` string disambiguation.** When the source value is a string, look it up against `defined_term.id`. Hit → set `intended_use_id`. Miss → set `intended_use_text`. URI shape is *not* used as a heuristic: a free-text intended use that happens to look like a URI is treated as text unless it also matches a known `defined_term.id`. External-term resolution (auto-creating `defined_term` stubs from URI-shaped misses) is a separate feature, not part of this disambiguation.

Mirror the cross-referencing wording already in [schemas/README.md:28-34](../schemas/README.md).

### E2. Add "Indexes" section (issue #5)

After "Validation Rules". Index shapes are tied to the documented query patterns; node-leading order beats direction-leading because traversal in [use-cases.md](../spec/querying/use-cases.md) starts from a specific node, not from a global "all inputs" set. SQLite supports partial indexes natively; prefer them where they let us drop a column from the key.

**Graph traversal indexes (process_io):**

- Partial indexes per direction, node-leading:
  - `process_io(material_id, process_id) WHERE direction = 'input'`
  - `process_io(material_id, process_id) WHERE direction = 'output'`
  - `process_io(data_id, process_id) WHERE direction = 'input'`
  - `process_io(data_id, process_id) WHERE direction = 'output'`
- `process_io(process_id, direction)` — already implied by PK; explicit for clarity.

**Lookup-side indexes for the temperature/protocol query:**

- `lab_protocol(intended_use_id)` — find protocols by ontology term (step 1 of the [use-cases.md:37](../spec/querying/use-cases.md#L37) command chain).
- `process_parameter_value(property_value_id)` — find processes whose parameter values match (step 2).
- `property_value` lookup indexes depend on the canonical query shape; candidates: `(name_tan, value)` for ontology-keyed value-equality, `(instance_of_id)` for FormalParameter-keyed lookups. Add as the query workload solidifies; do not over-index speculatively.

**Other:**

- `dataset_process(process_id)` — locate owning dataset for a given process.
- `<owner>_additional_property(property_value_id)` — find owners of a PV (supports D3 closed-document check; one index per owner table).
- `lab_process(executes_protocol_id)` — find executions of a protocol.
- Short `WITH RECURSIVE` example for the Path query from [use-cases.md](../spec/querying/use-cases.md), keyed off `process_io`.

### E3. Add `Data.id`/`Data.path` import rule (issue #6)

The naive `id := path` collides on fragment-level data. `selectorFormat` matters too: the same `selector` string interpreted under different formats (e.g. RFC 7111 vs JSONPath) addresses different fragments, so two rows with the same `(path, selector)` but different `selectorFormat` are *different* fragments and must get different ids.

In the `data` table section:

> When the source omits `id`, generate it deterministically from the fragment-identity triple `(path, selector, selectorFormat)`:
>
> - `selector` absent → `id := path` (`selectorFormat` is meaningless without a selector and ignored).
> - `selector` present → `id := canonicalize(path, selector, selectorFormat)`.
>
> The exact canonicalization is a profile decision (e.g. structured concatenation, URI-fragment encoding, or content hash) — pick one and apply uniformly. Whatever is chosen, `selectorFormat` MUST participate when `selector` is present. When the source provides `id`, store it verbatim alongside `path`/`selector`/`selectorFormat` — the model permits divergence (e.g. when `id` is a globally unique URN).

Uniqueness is best enforced importer-side by deterministic ID generation (the function above is injective on the fragment-identity triple). If the profile additionally wants a database-level invariant, declare uniqueness over the normalized triple — for example a generated column `data.fragment_identity = path || char(31) || coalesce(selector, '') || char(31) || coalesce(selectorFormat, '')` with a unique index on it. Avoid clever partial indexes: SQLite treats NULLs as distinct in unique indexes, so naive partial schemes silently fail to dedupe the no-selector case.

### E4. Promote per-owner association table choice (issue #13)
New bullet under Design Decisions:

> **Per-owner `additional_property` tables, not a polymorphic `(owner_table, owner_id)` table.** Four near-identical tables is the cost of preserving FK enforcement; a single polymorphic table cannot constrain the owner side. The duplication is structural and accepted.

### E5. Apply Phase 0 decisions

- D0 → retitle the document as "SQL Import Profile" and rewrite intro framing
- D1 → add `type TEXT` to `dataset`, `lab_process`, `lab_protocol`, `material`, `property_value`, `formal_parameter`, `defined_term`; remove the "type omitted when constant" Design Decisions bullet
- D2 → rewrite I/O symmetry prose at [design.md:444](../schemas/sql/design.md#L444); replace the false "spec does not require" claim with the SHOULD + non-enforcement framing
- D3 → add closed-document "no orphan PVs at commit time" invariant under Validation Rules
- D4 → add `ON DELETE` clauses to every FK definition: `CASCADE` for owner → association rows, `RESTRICT` for refs to first-class entities
- D5 → remove `property_value.value_type` and its CHECKs; document that `value` is nullable `TEXT` and semantic validation belongs above SQL

### E6. Reframe round-trip claim in intro (issue #1 spillover)

The current claim ("round-trip the current core YAML schemas without silently dropping core semantics") is true for the *profile* but not for arbitrary YAML.

Rewrite as: *"This SQL import profile round-trips YAML documents that conform to it. The profile narrows the open YAML surface where SQL needs a concrete contract — for example no orphan PropertyValues at commit time, exact-one-target foreign keys for mixed-target lists, deterministic generated IDs for fragment-level Data, and unresolved references as import errors except for `intendedUse` free text."*

## Phase 2 — Spec-doc follow-ups (separate from design.md)

Inconsistencies between spec MDs and YAML — raise as a separate cleanup, not papered over in design.md.

### S1. `Dataset.hasPart` type column gap (issue #11)
[spec/core/Dataset.md:23](../spec/core/Dataset.md) lists the target as `Dataset` only; YAML and prose both allow `Dataset | Data`. Fix the property table.

### S2. `FormalParameter.workExample` (issue #12a)
Drawn in [spec/core/FormalParameter.md:26](../spec/core/FormalParameter.md) Mermaid but absent from the property table and YAML. Either remove from the diagram or add to the table + YAML.

### S3. `Dataset.creator → Person` (issue #12b)
Drawn in [spec/core/Dataset.md:38](../spec/core/Dataset.md) Mermaid but no `Person` core type exists. Either remove from the diagram or add `Person` to the core spec set.

## Execution order

1. Use the decisions recorded above as fixed inputs.
2. Apply Phase 1 (E1–E6) in one editing pass to [schemas/sql/design.md](../schemas/sql/design.md).
3. Keep the D1/D2 follow-ups in [issues.md](issues.md) for manual filing.
4. Decide Phase 2 scope — same branch, separate branch, or punt to spec authors.

## Out of scope

- Changes to the table set in design.md (no new entities, no new association tables).
- ISA / Workflow Run / Datamap decoration tables (already documented out of scope in design.md).
- Closure tables or materialized path views (deferred until query workload demands).
- The `Path` type from the querying spec — derived via `WITH RECURSIVE`, not stored.
