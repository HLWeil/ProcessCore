---
title: ARC Workspace Project File Handling
category: Specification
categoryindex: 3
index: 6
---

# ARC Workspace Project File Handling

## Status and scope

This document specifies how processors and codecs compile and execute the
project-file language defined in [ARC Workspace Project File](project_file.md).

It covers project resolution, path processing, codec contracts, compilation,
reading, canonical merging, writing, stale-output cleanup, diagnostics, and
round-trip behavior. Project and workspace-profile YAML syntax remains normative
in the project-file specification.

The key words **MUST**, **MUST NOT**, **REQUIRED**, **SHALL**, **SHALL NOT**,
**SHOULD**, **SHOULD NOT**, **RECOMMENDED**, **NOT RECOMMENDED**, **MAY**, and
**OPTIONAL** in this document are to be interpreted as described in
[BCP 14](https://www.rfc-editor.org/info/bcp14) when, and only when, they appear
in all capitals.

## 1. Conformance

### 1.1 Runtime terms

**Binding**
: The resolved association among a compiled rule, a resource path, path captures,
  and a semantic model target.

**Managed output**
: A regular file matched by a currently enabled writable rule.

**Workspace session**
: In-memory state retaining a compiled plan, ARC graph, bindings, outcomes, and
  diagnostics for a load/update operation.

### 1.2 Processor conformance

A conforming processor MUST:

- implement strict project and profile decoding;
- implement deterministic compilation and planning;
- enforce path confinement and direction-specific ownership;
- select codecs only by registered capability ID;
- implement the read, merge, write, failure, and stale-output behavior specified
  here; and
- return structured diagnostics.

A read-only processor MAY omit write execution but MUST reject projects requiring
unsupported write capabilities. A write-only processor has the corresponding
obligation for read capabilities.

### 1.3 Codec conformance

A conforming codec MUST publish a descriptor and MUST obey the contribution,
facet, direction, transactionality, and diagnostic contracts in section 3.

## 2. Project resolution and path processing

### 2.1 Project discovery

A caller MAY supply:

- the workspace root, in which case the processor resolves
  `.arc/project.yml`; or
- the exact project-file path, in which case the processor derives the root.

A processor MUST NOT search above an explicitly supplied workspace root.

The project file and local profile documents are configuration. A writer:

- MUST NOT rewrite them implicitly;
- MUST NOT treat them as stale outputs; and
- MUST NOT delete them.

### 2.2 Profile resolution

Built-in profiles are resolved by exact registry key. Local profile paths are
resolved relative to `.arc` and confined to the workspace. The loaded profile's
ID and version MUST exactly match the project reference.

### 2.3 Parameter resolution

The compiler:

1. rejects supplied parameter names not declared by the profile;
2. applies supplied values over defaults;
3. validates type and `allowedValues`;
4. rejects unresolved required values; and
5. substitutes resolved values before path-template compilation and codec-option
   validation.

### 2.4 Filesystem safety

Confinement MUST use normalized and resolved paths rather than raw string-prefix
checks. Safety MUST be checked again immediately before replacement or stale
deletion to reduce time-of-check/time-of-use risk.

On a case-insensitive filesystem, collision detection MUST use the filesystem's
effective comparison. On Windows, two outputs differing only by case conflict.

The workspace root and directories MUST NOT be replacement or deletion targets.
A replacement or deletion target MUST be a regular file and MUST NOT be a
symbolic link or reparse point.

### 2.5 Selector and parent validation

For a readable shallow dataset, the resolved parent reference MUST identify
exactly one successfully materialized dataset. A missing or failed parent skips
the dependent resource; it is not attached to the root as a fallback.

After parsing a dataset contribution:

- its identifier MUST equal `{dataset.identifier}` when captured;
- its parent MUST equal `{parent.identifier}` when captured;
- its `additionalType` MUST satisfy the target filter; and
- its hierarchy MUST satisfy `children` or `descendants`.

A mismatch fails the binding. A processor MUST NOT silently rename or retarget a
parsed dataset.

### 2.6 Processing order

A compiler processes a path in this order:

1. substitute declared `path-segment` profile parameters;
2. parse model captures;
3. validate literals and captures;
4. compile a read matcher and/or write renderer; and
5. normalize at the host-filesystem boundary.

### 2.7 Read matching

A read planner treats literal segments as exact names and capture segments as one
safe directory-entry segment. It walks only positions implied by the template;
it MUST NOT reinterpret the template as an unrestricted recursive glob.

Every match produces:

- a normalized workspace-relative path;
- captured values;
- qualified rule ID;
- codec capability ID; and
- provisional semantic target.

Matches are sorted by normalized relative path using ordinal comparison before
codec execution.

### 2.8 Write rendering

A write planner substitutes captures from the selected dataset and parent.
Missing, empty, or unsafe capture values are planning errors.

Two bindings in the same write plan MUST NOT render to the same normalized path.
The processor MUST detect all rendered collisions before invoking a writer.

## 3. Codec registry and contracts

### 3.1 Explicit capability selection

Every rule names one codec capability:

```yaml
codec: arc.yaml.dataset.v1
```

A processor MUST select the codec only by exact registry lookup of this ID. It
MUST NOT infer or replace the codec based on:

- filename extension;
- media type;
- file signature;
- workbook sheets;
- discovery order; or
- an unregistered value in `codecOptions`.

Media type and format information MAY be reported for validation or diagnostics,
but does not select a codec.

### 3.2 Registry

The embedding library or application constructs the codec registry explicitly.
Duplicate capability IDs MUST be rejected.

The project file MUST NOT cause dynamic assembly, package, module, or script
loading. A missing capability ID is a compile error.

### 3.3 Descriptor

Every codec descriptor declares:

- capability ID;
- contribution kind;
- overlay facet, when applicable;
- additional owned facets;
- `CanRead`;
- `CanWrite`;
- supported runtime targets;
- codec-option validator;
- human-readable format and media-type metadata; and
- whether the codec can return `Omit`.

The compiler MUST verify that the descriptor agrees with every rule that names
it.

### 3.4 Read contract

A codec receives a safely resolved resource context and validated options. It
MUST NOT discover additional project resources independently.

A tree codec returns one detached dataset tree.

A dataset codec returns one detached shallow dataset.

An overlay codec returns one detached typed overlay value. It MUST parse the
complete value before the processor applies it to a dataset.

Expected format failures MUST be returned as diagnostics. A processor MUST catch
an unexpected codec exception at the resource boundary and convert it to a
structured failure.

### 3.5 Write contract

A tree or dataset codec receives the selected canonical dataset view. An overlay
codec receives a typed facet extracted from that dataset.

A codec returns either:

- complete rendered content; or
- `Omit`, if its descriptor supports omission and the rule permits
  `omitWhenEmpty`.

A codec MUST NOT open, truncate, replace, or delete the final destination. The
workspace writer owns staging and replacement.

### 3.6 Facet enforcement

A codec MUST NOT apply or emit a detachable facet that the compiled rule does
not own. A base-only codec encountering non-empty data for an unowned
standardized facet MUST fail that resource rather than silently discard it.

An overlay codec MUST parse a complete overlay value before applying it. A parse
failure MUST leave the target dataset unchanged.

## 4. Compilation

### 4.1 Required pipeline

A processor MUST compile in this order:

1. parse and strictly validate the project;
2. resolve listed built-in and confined local profiles;
3. validate profile type, specification version, ID, version, parameters, and
   rules;
4. apply supplied parameter values over defaults;
5. qualify profile rule IDs;
6. apply restricted overrides;
7. qualify project-local rule IDs;
8. remove disabled rules;
9. resolve codec descriptors and validate options/directions;
10. compile path templates;
11. validate selectors and attachment dependencies;
12. calculate direction-specific base and facet ownership;
13. reject static ownership and path conflicts;
14. topologically order read dependencies; and
15. produce an immutable compiled storage plan.

Any error in these stages is fatal. No metadata codec may be invoked after a
failed compilation.

### 4.2 Static and concrete planning

Some conflicts are known from declarations, while others depend on discovered
captures or the actual graph.

Static compilation MUST reject at least:

- duplicate root owners in one direction;
- overlapping tree and descendant declarations in one direction when the overlap
  is provable;
- duplicate exact owners;
- duplicate literal output paths;
- missing or incompatible codecs;
- invalid attachment dependencies; and
- dependency cycles.

Concrete read planning MUST discover paths and create provisional bindings. It
MUST reject, before model mutation:

- two read base bindings for one dataset;
- two read overlay bindings for one `(dataset, facet)`; and
- ambiguous target or parent bindings.

Concrete write planning MUST select all model targets and render all paths. It
MUST reject, before codec execution:

- duplicate write owners;
- unresolved targets or captures; and
- normalized output collisions.

Selector domains that cannot be proven disjoint statically are not resolved by
rule order. They remain subject to concrete validation.

If semantic target identity can be known only after parsing, the processor MUST
parse into detached contributions, validate the complete ownership set, and only
then attach or apply those contributions.

### 4.3 Determinism

Compilation, discovery, target selection, merge, execution reporting, and
diagnostics MUST use deterministic ordering:

1. dependency order;
2. expanded profile/project rule order where dependencies are equal;
3. normalized path using ordinal comparison; and
4. dataset identifier using ordinal comparison.

## 5. Read processing

### 5.1 Root requirement

A readable plan MUST define exactly one root-forming read owner:

- a tree rule targeting `root`; or
- a dataset rule targeting `root`.

If the root resource is absent or fails, the result has no usable ARC. Dependent
resources are skipped and diagnostics are returned.

### 5.2 Processing order

A conforming reader:

1. compiles the project;
2. discovers and concretely validates read bindings;
3. parses the root-forming contribution;
4. parses shallow datasets in parent-before-child dependency order;
5. validates captures, selectors, and shallow/tree constraints;
6. attaches successful datasets using the ARC graph's established attachment
   operations;
7. parses and applies overlays only after their targets exist;
8. canonicalizes and merges shared entities;
9. records every success, absence, skip, warning, and failure; and
10. returns the ARC, session, outcomes, and diagnostics.

### 5.3 Graph attachment

The reader MUST use model attachment behavior that preserves:

- reciprocal `hasPart`/`partOf`;
- process ownership;
- process input/output canonical references;
- root-level sample, data, and recipe identity; and
- all existing ARC graph invariants.

A storage processor MUST NOT maintain a conflicting parallel graph identity
system.

### 5.4 Best-effort resource handling

After a valid plan exists, independent resource failures do not cancel the
entire read.

Examples:

- one malformed assay does not prevent sibling assays from loading;
- an optional missing Datamap is `Absent`;
- a Datamap failure leaves the base dataset unchanged; and
- a failed parent skips only resources depending on that parent.

Outcomes MUST distinguish at least:

```text
Succeeded
Absent
Failed
SkippedDependency
SkippedNoTarget
```

The processor MUST NOT attach a partially parsed dataset or apply a partially
parsed overlay.

## 6. Canonical identity and compatible union

### 6.1 Identity keys

When attaching independently parsed contributions, the processor reconciles
shared model entities using the ProcessCore identity keys:

| Entity | Canonical key |
|---|---|
| `Sample` | name |
| `Data` | path plus fragment selector |
| `Recipe` | name plus version |

Processes remain distinct model objects and are not merged merely because their
values are equal.

### 6.2 Scalars

For every scalar field on equal-key entities:

| Canonical value | Incoming value | Result |
|---|---|---|
| absent | present | copy incoming |
| present | absent | retain canonical |
| equal present | equal present | retain without warning |
| unequal present | unequal present | retain canonical and emit `MERGE_CONFLICT` |

Absence follows the property's model semantics. An empty string is a value unless
the model property already normalizes it to absence.

### 6.3 Collections

Collections are merged by stable union:

1. retain canonical items in their existing order;
2. identify entity items by their established key;
3. recursively merge equal-key entity items;
4. compare value items by structural equality where defined;
5. append unseen incoming items in incoming order; and
6. avoid exact duplicates.

Conflicting equal-key values retain the canonical value and emit a diagnostic.

### 6.4 Dynamic properties

For model `DynamicObj` overflow properties:

- copy an incoming value when the canonical key is absent;
- recursively merge map-like values;
- accept structurally equal opaque values;
- stable-union list values when meaningful equality exists; and
- otherwise retain the canonical value and emit `MERGE_CONFLICT`.

Project/profile configuration and storage bindings MUST NOT be inserted into
model dynamic properties.

### 6.5 Conflict policy

Merge conflicts are deterministic diagnostics. They SHOULD be warnings by
default. A caller MAY request strict execution behavior that treats them as
errors, but the merge result remains first-value-preserving.

### 6.6 Writeback

Version 1.0 tracks no per-field source provenance. On write, the fully merged
canonical shared entity MUST be serialized through every owned dataset
representation that references it.

## 7. Write processing

### 7.1 Canonical rewrite

Writing is a canonical model-to-resource rewrite, not an in-place syntax patch.
A writer is not required to preserve:

- YAML comments or formatting;
- unknown workbook formatting;
- worksheet layout not represented by the codec;
- original field provenance; or
- source collection ordering when the codec defines canonical order.

For deterministic codecs, a second write of an unchanged model and plan SHOULD
produce identical bytes.

### 7.2 Required pipeline

A conforming writer:

1. compiles or reuses a valid plan;
2. selects every writable tree, dataset, and overlay target;
3. validates direction-specific ownership;
4. renders every output path;
5. rejects all collisions before codec execution;
6. extracts and renders contributions independently;
7. stages each complete output in a sibling temporary file;
8. replaces each destination only after successful staging;
9. continues independent writes after resource-level failure;
10. performs stale-output cleanup only after a completely successful write; and
11. returns written, omitted, failed, retained, and deleted paths with
    diagnostics.

### 7.3 Per-resource replacement

The writer MUST:

- create parent directories only under the workspace root;
- use a uniquely named temporary sibling;
- avoid truncating the current destination before staging completes;
- replace the destination using the strongest portable atomic operation
  available;
- retain the previous destination if rendering, staging, or replacement fails;
- remove its own abandoned temporary resource when safely possible; and
- report when the host cannot guarantee atomic replacement.

Version 1.0 does not require a transaction spanning all outputs.

### 7.4 Best-effort writes

A resource-level write failure does not prevent independent outputs from being
attempted. It does, however, suppress all stale-output deletion for that write
operation.

The write result MUST distinguish at least:

```text
Written
Omitted
Failed
RetainedAfterFailure
DeletedStale
StaleDeleteFailed
```

### 7.5 Empty contribution omission

When `omitWhenEmpty` is true and the codec returns `Omit`:

- the binding counts as successfully processed;
- no output is staged;
- the path is excluded from expected outputs; and
- an existing currently managed file at that path may become stale.

A base tree or dataset codec SHOULD NOT return `Omit`.

### 7.6 Unowned non-empty facets

Concrete write planning SHOULD emit a warning when a dataset contains a non-empty
standardized facet with no writable owner. Zero ownership is permitted for
intentional partial exports. More than one owner in the same direction is an
error.

## 8. Stale managed outputs

### 8.1 Eligibility

Stale cleanup occurs only if every planned writable binding was either:

- written successfully; or
- intentionally omitted.

If any render, stage, or replacement fails, the processor MUST NOT delete any
stale candidate.

### 8.2 Candidate calculation

After complete write success, the writer:

1. discovers existing regular files matched by each currently enabled writable
   rule's compiled write template;
2. calculates the normalized expected non-omitted output set;
3. subtracts expected paths from matched paths;
4. revalidates every candidate immediately before deletion; and
5. deletes candidates independently, recording each outcome.

### 8.3 Prohibited deletion

The writer MUST NOT delete:

- a file not matched by a currently enabled writable rule;
- a scientific payload referenced by `Data`;
- a directory;
- a symbolic link or reparse point;
- `.arc/project.yml`;
- a local workspace-profile document;
- a path owned only by a profile or rule no longer in the project;
- a read-only/import path merely because the canonical write path differs; or
- any stale candidate after a partial write failure.

Removing or changing a workspace profile therefore does not authorize deletion
of files managed only by the former plan.

## 9. Diagnostics and results

### 9.1 Diagnostic fields

A diagnostic MUST contain:

- stable `code`;
- severity: `Info`, `Warning`, or `Error`;
- human-readable message; and
- available context.

Context SHOULD include:

- qualified rule ID;
- codec capability ID;
- workspace-relative path;
- dataset identifier;
- facet ID; and
- normalized cause text.

Expected parse or validation failures MUST NOT be represented solely by a
platform-specific exception.

### 9.2 Standard codes

Processors SHOULD use these stable codes:

```text
PROJECT_NOT_FOUND
PROJECT_PARSE
PROJECT_VERSION_UNSUPPORTED
PROFILE_NOT_FOUND
PROFILE_ID_MISMATCH
PROFILE_VERSION_MISMATCH
PARAMETER_INVALID
OVERRIDE_TARGET_UNKNOWN
RULE_DUPLICATE
RULE_INVALID
CODEC_NOT_REGISTERED
CODEC_DIRECTION_UNSUPPORTED
CODEC_OPTIONS_INVALID
PATH_UNSAFE
PATH_TEMPLATE_NOT_INVERTIBLE
PATH_COLLISION
OWNERSHIP_CONFLICT
DEPENDENCY_CYCLE
RESOURCE_REQUIRED_MISSING
RESOURCE_CARDINALITY
RESOURCE_PARSE
RESOURCE_RENDER
RESOURCE_REPLACE
RESOURCE_SKIPPED_DEPENDENCY
TARGET_NOT_FOUND
TARGET_AMBIGUOUS
CAPTURE_MISMATCH
DATASET_INLINE_CHILDREN
FACET_UNOWNED
MERGE_CONFLICT
STALE_DELETE
```

An implementation MAY add more specific codes but SHOULD retain these categories
for portable callers.

### 9.3 Load result

A load result contains:

- optional ARC graph;
- optional workspace session;
- ordered resource outcomes; and
- ordered diagnostics.

When no root is materialized, the ARC and session model value are absent.

### 9.4 Write result

A write result contains:

- written paths;
- omitted paths;
- failed paths;
- prior destinations retained after failure;
- deleted stale paths;
- stale deletion failures;
- ordered resource outcomes; and
- ordered diagnostics.

## 10. ProcessCore ARC facade integration

This section applies to the generic ProcessCore `ARC` I/O facade. Lower-level
processors MAY expose additional operations, registries, and options provided
that their project handling conforms to the preceding sections.

### 10.1 Generic load

`ARC.load` and `ARC.loadAsync` MUST test the exact supplied workspace root for
`.arc/project.yml` before applying legacy representation detection. They MUST NOT
search an ancestor workspace.

If the project file exists, the generic load MUST use it with the standard codec
and workspace-profile registries. The project file is authoritative: an invalid
project, invalid compiled plan, missing root, or resource error MUST NOT cause
fallback to `arc.yml` or a spreadsheet scaffold.

If the project file does not exist, generic loading MUST retain the legacy
behavior of preferring `arc.yml` and otherwise attempting the spreadsheet
scaffold. A processor MUST NOT create or infer project configuration during
loading.

### 10.2 Generic write and update

`Write` and `WriteAsync` MUST inspect the destination workspace. If
`.arc/project.yml` exists, they MUST compile and execute that project. If it does
not exist, they MUST retain the legacy `arc.yml` behavior. An invalid destination
project MUST NOT cause fallback to YAML.

`Update` and `UpdateAsync` without a different destination MUST reuse an attached
workspace session when present. They MUST NOT reread the project file merely
because it changed or was removed after the session was created.

When an explicit update destination differs from the attached session's
workspace, the destination project MUST govern the update. If no destination
project exists, the processor MUST return an `Error` diagnostic with code
`PROJECT_NOT_FOUND` before writing any resource. An ARC without a project session
MUST retain the legacy YAML/scaffold update behavior. Workspace equality MUST use
normalized, resolved paths.

Generic I/O MUST NOT implicitly create, rewrite, or delete `.arc/project.yml` or
local workspace-profile documents.

### 10.3 Session transitions

A project-aware load or a write whose destination plan compiles MUST attach its
workspace session to the ARC. A fatal destination-plan failure MUST leave the
previous session unchanged. A compiled destination session MUST remain attached
after partial resource execution so the caller can inspect diagnostics and retry.

A successful generic write using legacy YAML MUST clear an attached project
session and make YAML the active legacy representation.

The explicit `loadYML`, `loadYMLAsync`, `loadXLSX`, `loadXLSXAsync`, `WriteYML`,
`WriteYMLAsync`, `WriteXLSX`, and `WriteXLSXAsync` operations MUST bypass project
discovery. Explicit writes MUST clear an attached project session and select the
requested legacy representation.

### 10.4 Rich results and strict convenience methods

ProcessCore MUST provide result-returning synchronous and asynchronous variants
equivalent to:

```fsharp
ARC.loadWithResultAsync
ARC.loadWithResult
arc.WriteWithResultAsync
arc.WriteWithResult
arc.UpdateWithResultAsync
arc.UpdateWithResult
```

For project operations, these methods MUST return the load or write result
defined in section 9, including partial outcomes and diagnostics. Expected
project, planning, codec, and resource failures MUST remain in the result.

When no project file exists, a successful legacy operation MUST be adapted to a
result with no project session or project diagnostics. Legacy failures retain
their existing exception behavior.

The existing generic methods MUST preserve their current return types and act as
strict convenience wrappers. `Info` and `Warning` diagnostics MUST NOT make them
fail. If a result contains an `Error`, the wrapper MUST throw a
`StorageOperationException` only after independent work has completed. The
exception MUST carry either `LoadFailure` with the `LoadResult` or `WriteFailure`
with the `WriteResult`.

The lower-level `Workspace` API MUST remain available for callers requiring
custom registries or options.

## 11. Round-trip properties

### 11.1 Semantic read after write

For a compatible model `M` and a valid readable/writable plan `P`:

```text
read(P, write(P, M)) ≈ M
```

`≈` means semantic ARC graph equivalence:

- same dataset hierarchy and identifiers;
- same owned model fields and facets;
- same process connections;
- same canonical shared entities after compatible union; and
- no requirement for source formatting or runtime reference identity.

### 11.2 Canonical write stability

For deterministic codecs:

```text
write(P, read(P, write(P, M))) = write(P, M)
```

after the first canonical rewrite.

### 11.3 Failure safety

A failed resource operation MUST NOT:

- partially apply an overlay;
- attach a partially parsed dataset;
- truncate its current output destination;
- trigger stale cleanup; or
- cancel independent operations except through an explicit target dependency.
