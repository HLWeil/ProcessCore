# ARC project file handling

Status: implementation-ready handling plan

Project-file language:
[project_file.md](project_file.md)

Target project: `ProcessCore`

Initial runtime targets: .NET and JavaScript

Out of scope for the first implementation: Python runtime support, arbitrary
scientific payload partitioning, remote profiles, and exact source-format
preservation

This document plans how the project-file language is compiled and how its
resources are read, merged, written, diagnosed, and tested.

## 1. Handling goals and non-goals

The first implementation must:

- validate every codec choice before resource I/O;
- reconstruct the same nested `Dataset` graph regardless of physical
  partitioning;
- reconcile shared `Sample`, `Data`, and `Recipe` entities deterministically;
- continue independent reads or writes after a resource-level failure and return
  structured diagnostics;
- protect unmanaged scientific data and unrelated files;
- work with Fable on .NET and Node.js without reflection-based plugin loading;
- leave all current `ARC` YAML and spreadsheet APIs unchanged; and
- provide pure planning functions that can be inspected and tested without
  touching the filesystem.

The first implementation does not preserve workbook formatting, YAML comments,
source ordering, or the exact original distribution of shared entity fields. It
does not replace or wrap the existing `ARC.Load`, `ARC.Write`, YAML, or
scaffold APIs, and it does not require Python support in the initial release.

## 2. Repository and runtime context

### 2.1 Current F# implementation

The core implementation lives under [`src/ProcessCore`](../src/ProcessCore).
Important starting points are:

- [`ARC.fs`](../src/ProcessCore/ARC.fs), where `ARC` specializes `Dataset` with a
  path, scaffold flag, samples, recipes, and fixed YAML/spreadsheet I/O;
- [`Graph.fs`](../src/ProcessCore/Graph.fs), which attaches graph members and
  maintains root-level canonical registries;
- [`ScaffoldReader.fs`](../src/ProcessCore/ScaffoldReader.fs), which discovers
  the fixed ISA-XLSX directory structure;
- [`YML/Dataset.fs`](../src/ProcessCore/YML/Dataset.fs), which serializes a
  recursively nested dataset;
- [`FragmentSelector.fs`](../src/ProcessCore/FragmentSelector.fs), which supplies
  the selector abstraction used by `Data`; and
- [`Helper/CrossAsync.fs`](../src/ProcessCore/Helper/CrossAsync.fs), which should
  be reused for cross-runtime asynchronous APIs.

The public usage documentation is illustrated in
[`arc.fsx`](../docs/core-implementation/arc.fsx),
[`creating-datasets.fsx`](../docs/core-implementation/creating-datasets.fsx), and
[`decorations.fsx`](../docs/core-implementation/decorations.fsx). Existing ARC
and YAML behavior is covered by
[`ARC.fs`](../tests/ProcessCore.Tests/ARC.fs) and
[`RoundTrip.fs`](../tests/ProcessCore.Tests/YAML/Integration/RoundTrip.fs).

### 2.2 Identity and canonicalization

The current graph uses root registries to canonicalize shared entities across
the dataset hierarchy:

- `Sample` by name;
- `Data` by path plus fragment selector; and
- `Recipe` by name plus version.

Datasets are identified by their identifier. Processes remain distinct even when
their values compare equal. Attaching graph members is preferable to mutating
collections directly because the graph code establishes back-edges and canonical
references.

At present, the first canonical object generally wins; later equal-key objects
do not contribute missing fields. Partitioned reading needs stronger behavior
because two independently parsed resources can legitimately describe compatible
parts of the same entity. The compatible-union merge is defined in section 8.

### 2.3 Extensibility and runtime constraints

Model types derive from `DynamicObj`, so unknown model properties can survive
YAML round trips. Storage-level extension handling must remain separate from
model-level dynamic properties.

The core library is compiled for .NET and through Fable. New public types and
algorithms should continue using the project's portable collection, path, and
async conventions. The implementation must not depend on reflection, runtime
assembly scanning, filesystem APIs unavailable to Node, or a new production
dependency without prior approval.

## 3. Handling terminology

This plan uses the project-file terms defined in
[the project-file plan](project_file.md#5-project-file-terminology), plus:

- **compiled rule:** a validated, fully expanded rule with qualified identity;
- **binding:** the association among compiled rule, captures, target, and path
  discovered during a load or computed during a write;
- **managed output:** a file path matched by a currently enabled writable rule;
  and
- **workspace session:** in-memory state retaining the compiled plan, graph,
  bindings, outcomes, and diagnostics for an operation.

## 4. Conceptual architecture

```text
                         compile (pure)
 project.yml ─┐
              ├─> profiles + overrides + registry ─> CompiledStoragePlan
 local profile┘                                      │
                                                    │
                           ┌────────────────────────┴────────────────────────┐
                           │                                                 │
                       read plan                                         write plan
                           │                                                 │
                discover resource bindings                       select model bindings
                           │                                                 │
                    registered codecs                                registered codecs
                           │                                                 │
              parsed tree/dataset/overlay                      rendered temporary files
                           │                                                 │
               attach + compatible merge                       replace + safe stale prune
                           │                                                 │
                    WorkspaceSession                              WorkspaceSession
```

The compiler is shared by read and write. A codec never chooses its own paths,
discovers unrelated files, or determines model ownership. A rule never embeds
parser code. The executor never guesses a codec from a filename or media type.

## 5. Codec capabilities and registry

### 5.1 Explicit capability IDs

A rule names a codec by stable capability ID:

```yaml
codec: arc.yaml.tree.v1
```

The executor does not infer a codec from:

- extension;
- media type;
- file signature;
- worksheet names; or
- discovery order.

Format and media-type metadata may be exposed for diagnostics and validation,
but cannot override the explicit ID.

Suggested built-in IDs:

```text
arc.yaml.tree.v1
arc.yaml.dataset.v1
arc.isa.investigation.xlsx.v1
arc.isa.study.xlsx.v1
arc.isa.assay.xlsx.v1
arc.isa.workflow.xlsx.v1
arc.isa.run.xlsx.v1
arc.isa.datamap.xlsx.v1
```

The final names should be centralized constants. Renaming a capability is a
configuration compatibility change.

### 5.2 Registry

`CodecRegistry` is built explicitly by library code or the embedding
application. It maps capability IDs to codec implementations and rejects
duplicates. No reflection or project-file-directed loading is allowed.

The registry enables future codecs without coupling the planner to YAML or XLSX:

```fsharp
let registry =
    CodecRegistry.empty
    |> CodecRegistry.addTreeCodec yamlTreeCodec
    |> CodecRegistry.addDatasetCodec yamlDatasetCodec
    |> CodecRegistry.addDatasetCodec isaStudyCodec
    |> CodecRegistry.addOverlayCodec datamapCodec
```

The exact F# ergonomics may follow existing style, but registration must remain
explicit and testable on .NET and Node.

### 5.3 Descriptors

Every codec descriptor declares:

- stable capability ID;
- contribution kind;
- facet ID for an overlay;
- `CanRead` and `CanWrite`;
- owned facets for a tree or dataset representation;
- supported runtime targets;
- human-readable format/media-type metadata;
- codec-option validation; and
- whether it can determine `Omit` for an empty contribution.

Compilation verifies descriptor compatibility with every referencing rule.

### 5.4 Logical codec contracts

The implementation may use interfaces, records of functions, or discriminated
unions, but the logical contracts should be equivalent to:

```fsharp
type CodecId = CodecId of string
type FacetId = FacetId of string

type RenderedResource =
    | Content of bytes: byte array
    | Omit

type ITreeCodec =
    abstract Descriptor : TreeCodecDescriptor
    abstract Read :
        CodecReadContext -> CrossAsync<Result<Dataset, CodecDiagnostic list>>
    abstract Write :
        CodecWriteContext * Dataset ->
            CrossAsync<Result<RenderedResource, CodecDiagnostic list>>

type IDatasetCodec =
    abstract Descriptor : DatasetCodecDescriptor
    abstract Read :
        CodecReadContext -> CrossAsync<Result<Dataset, CodecDiagnostic list>>
    abstract Write :
        CodecWriteContext * Dataset ->
            CrossAsync<Result<RenderedResource, CodecDiagnostic list>>

type IOverlayCodec<'overlay> =
    abstract Descriptor : OverlayCodecDescriptor
    abstract Read :
        CodecReadContext -> CrossAsync<Result<'overlay, CodecDiagnostic list>>
    abstract Apply :
        Dataset * 'overlay -> Result<unit, CodecDiagnostic list>
    abstract Extract :
        Dataset -> Result<'overlay, CodecDiagnostic list>
    abstract Write :
        CodecWriteContext * 'overlay ->
            CrossAsync<Result<RenderedResource, CodecDiagnostic list>>
```

Because heterogeneous generic interfaces are awkward to store, the concrete
registry can erase overlay values behind an internal boxed representation while
keeping boxing out of public model APIs. The essential rule is transactional
overlay application: parse first, then apply once.

Codecs receive an already opened or safely resolved resource context. They do not
perform independent path discovery. They return diagnostics rather than printing
or throwing for expected format errors. Unexpected exceptions are caught at the
resource boundary and converted into an internal-codec diagnostic.

A base-only codec that encounters a non-empty field belonging to an unowned
standardized facet fails that resource rather than silently discarding the
field. An overlay codec parses a complete overlay value before applying it, so a
parse failure cannot partially mutate the dataset.

### 5.5 Existing codecs

Adapters may delegate to existing YAML and spreadsheet parsing/writing logic.
However:

- the current recursive YAML encoder is a tree codec;
- a dataset codec must suppress inline child serialization and decoding;
- scaffold workbook codecs should expose one workbook's semantic contribution;
- Datamap should be an overlay codec; and
- existing public APIs and their behavior remain untouched.

## 6. Compiled storage plan

### 6.1 Compilation pipeline

Compilation is pure except for loading explicitly referenced local profile
documents. It proceeds as follows:

1. parse and strictly validate `project.yml`;
2. resolve each exact built-in or confined local profile;
3. validate profile type, version, ID, parameters, and rules;
4. apply parameter defaults and supplied values;
5. qualify profile rule IDs as `<profile-id>#<rule-id>`;
6. apply restricted overrides;
7. qualify local rule IDs as `project#<rule-id>`;
8. remove disabled rules;
9. validate codecs and options against the registry;
10. compile path templates for discovery and rendering;
11. validate selectors, attachment dependencies, and direction capabilities;
12. calculate direction-specific base/facet ownership;
13. reject semantic ownership conflicts and static path collisions;
14. topologically order read dependencies; and
15. emit an immutable `CompiledStoragePlan`.

Errors in these phases are fatal. Best-effort behavior begins only after a valid
plan exists.

### 6.2 Static and concrete plan validation

Some conflicts can be rejected from declarations alone; others depend on
captured paths or the actual in-memory graph. Treat both as planning, not
resource execution:

- **static compilation** rejects obvious overlaps such as two root owners, a
  tree owner combined with descendant base rules, duplicate exact selectors,
  incompatible codecs, and identical literal outputs;
- **concrete read planning** performs discovery, creates provisional bindings
  from captures, and rejects two read base bindings for the same dataset or two
  read overlay bindings for the same `(dataset, facet)` before opening any
  metadata resource;
- **concrete write planning** selects actual model targets, renders every path,
  and rejects duplicate write owners and output collisions before calling a
  writer.

Selector domains that overlap but cannot be proven disjoint statically are
retained until concrete planning. They are not resolved by rule order. Discovery
itself may inspect directory entries and file names, but no codec is invoked and
no model or destination is mutated until concrete ownership validation succeeds.

For reads whose target identity cannot be known without parsing content, use a
two-step boundary: parse into detached contributions, validate the complete
binding set, and only then attach/apply them. A conflicting detached contribution
is not partially committed. Built-in repeated rules should capture
`{dataset.identifier}` so conflicts are normally detectable before parsing.

### 6.3 Planned public model

Names may be adjusted to match repository conventions, but implementation should
provide equivalents of:

```fsharp
type WorkspaceProject
type WorkspaceProfile
type StorageRule
type CompiledStorageRule
type CompiledStoragePlan
type CodecRegistry
type WorkspaceProfileRegistry
type WorkspaceSession
type ResourceBinding
type ResourceOutcome
type LoadResult
type WriteResult
type StorageDiagnostic
```

Recommended namespace:

```fsharp
namespace ProcessCore.Storage
```

Recommended public operations:

```fsharp
WorkspaceProject.parse
WorkspaceProfile.parse
StorageCompiler.compile
StoragePlanner.planRead
StoragePlanner.planWrite
Workspace.loadAsync
Workspace.writeAsync
WorkspaceSession.updateAsync
```

`planRead` and `planWrite` return inspectable paths, rule IDs, targets, captures,
dependencies, and prospective stale candidates without invoking a codec.

### 6.4 Session

A successful or partially successful load returns a `WorkspaceSession` containing:

- workspace root;
- parsed project;
- compiled plan;
- loaded `ARC` when a root could be constructed;
- successful resource bindings;
- failed/skipped resource outcomes;
- canonicalization report; and
- diagnostics.

`updateAsync` writes the session's current in-memory `ARC` using the session's
compiled plan. A separate `writeAsync` accepts a project/plan and an in-memory
`ARC` that was not loaded by this API. Neither operation requires persisted
source provenance.

The `ARC` class's `ArcPath` and `IsSpreadsheetScaffold` fields are not used as
the new representation selector. The session owns generalized storage state.

### 6.5 Path discovery, rendering, and safety

The read planner splits each template into literal and capture segments and
walks only the implied directory positions. It does not expose a general
recursive glob. Each matching file yields:

- normalized relative path;
- captured values;
- compiled rule ID;
- codec ID; and
- provisional semantic target.

Discovery results are sorted by normalized relative path using ordinal
comparison before parsing.

The write planner substitutes values from each selected dataset and parent.
Missing, empty, unsafe, or non-scalar values are planning failures. Two bindings
that render to the same normalized path are a fatal output-collision error.

Path safety should be implemented once in a shared helper and used during
compilation, discovery, write staging, replacement, and pruning. Confinement
must use normalized and resolved paths rather than string-prefix checks.

## 7. Read semantics

### 7.1 High-level algorithm

1. Locate and parse `.arc/project.yml`.
2. Compile profiles, overrides, rules, paths, codecs, dependencies, and ownership.
3. Discover read resources deterministically.
4. Read root-forming tree/dataset resources.
5. Read shallow dataset resources parent-before-child.
6. Attach successful datasets through the existing graph APIs.
7. Parse and apply overlays after their target dataset exists.
8. Merge shared canonical entities as each contribution is attached.
9. Aggregate success, absence, skip, warning, and failure outcomes.
10. Return an `ARC` plus session when the root exists; otherwise return no model
    with diagnostics.

### 7.2 Root construction

A valid read plan must provide exactly one enabled root-forming base rule:

- a tree rule targeting `root`; or
- a dataset rule targeting `root`.

A missing optional root resource still means no ARC can be constructed. Therefore
the compiler should warn when a root read is not marked `required`, and the
executor returns a root-missing error if no root materializes.

The parsed root `Dataset` is promoted or copied into an `ARC` using the same
model-preserving conventions as current loading. No source-layout boolean is set
to represent generalized storage.

### 7.3 Discovery and validation

For every discovered resource:

- captures are decoded as safe segments;
- the parsed dataset identifier must equal `{dataset.identifier}`, when present;
- the resolved parent must equal `{parent.identifier}`, when present;
- `additionalType` must satisfy the selector;
- exact identifiers must remain unique;
- a dataset contribution must not carry inline child datasets; and
- a tree contribution must not produce a root outside its declared target.

A mismatch fails that resource. It must not silently retarget or rename the
parsed object.

### 7.4 Dependencies and best effort

Independent resource failures do not cancel the entire load. Examples:

- one malformed assay does not prevent other assays from loading;
- an optional missing Datamap is recorded as absent;
- a Datamap parse failure leaves its base dataset intact;
- a failed study causes only resources whose parent/target depends on that study
  to be skipped.

Root absence/failure prevents a usable model and therefore skips all dependent
resources. Plan errors prevent all execution.

A missing or failed parent skips the dependent resource. It is never attached to
the root as a fallback.

Outcomes should distinguish at least:

- `Succeeded`;
- `Absent`;
- `Failed`;
- `SkippedDependency`;
- `SkippedNoTarget`; and
- `Omitted` where relevant to writing.

### 7.5 Attachment

Successful datasets are attached through established ProcessCore graph methods so
that:

- `HasPart` and `PartOf` agree;
- samples, data, and recipes are canonicalized at the root;
- process inputs and outputs point to canonical entities; and
- model invariants remain the same as for programmatically constructed graphs.

The storage layer should not maintain a parallel graph implementation.

## 8. Compatible-union merge of shared entities

### 8.1 Why merge is required

When metadata is partitioned, one sample may occur in multiple dataset resources.
The first resource might provide its name and material type; another might add a
description or annotation. Treating the later occurrence as an error loses valid
information, while blindly overwriting makes results discovery-order dependent.

For `Sample`, `Data`, and `Recipe` objects with the existing canonical key,
version 1 applies a deterministic compatible union.

### 8.2 Scalar fields

For each scalar property:

- canonical empty + incoming non-empty: copy incoming value;
- canonical non-empty + incoming empty: retain canonical value;
- equal non-empty values: retain one value, no warning;
- unequal non-empty values: emit a merge-conflict diagnostic and retain the
  canonical value.

“Empty” follows the property's model semantics: `None`, absent dynamic property,
or equivalent null representation. Empty strings are values unless that model
property already normalizes them as absent.

### 8.3 Collections

Collections are unioned recursively:

- retain existing canonical order;
- append previously unseen incoming items in incoming order;
- compare entity items by their established identity key;
- compare value objects by existing structural equality where safe;
- recursively merge equal-key entity items;
- avoid multiplying exact duplicates; and
- diagnose incompatible equal-key values while retaining the first.

This gives deterministic results because resource execution has a deterministic
rule/path order.

### 8.4 Dynamic properties

For `DynamicObj` overflow properties:

- a missing canonical key receives the incoming value;
- map-like values merge recursively;
- structurally equal opaque values are accepted;
- unequal opaque values produce a diagnostic and retain the canonical value; and
- arrays/lists use stable union when their values have meaningful equality,
  otherwise an unequal non-empty value is treated as a conflict.

Storage configuration fields are not inserted into model dynamic properties.

### 8.5 Conflict severity

Compatible-union conflicts are resource diagnostics, normally warnings, because
the model remains usable and deterministic. A caller may request a strict policy
that upgrades merge conflicts to errors in the returned result, but the canonical
first value remains unchanged. Strictness is an execution option, not a
project-file expression in version 1.

### 8.6 Writeback consequence

The system does not track which source supplied each field. When writing, the
fully merged canonical `Sample`, `Data`, or `Recipe` is serialized through every
owned dataset representation that references it.

This deliberately favors semantic consistency over exact source fidelity. A
read/write cycle can therefore enrich more than one resource with the merged
description.

## 9. Write semantics

### 9.1 High-level algorithm

1. Compile or reuse a valid storage plan.
2. Select all writable tree, dataset, and overlay targets from the in-memory ARC.
3. Render every output path and reject collisions before codec execution.
4. Extract contributions and ask each codec to render independently.
5. Write rendered content to a temporary sibling file.
6. Replace each destination only after its temporary file is complete.
7. Continue other independent outputs after a resource-level failure.
8. If and only if every planned output succeeds or is intentionally omitted,
   discover and delete stale managed files.
9. Return all written, omitted, failed, retained, and deleted paths plus
   diagnostics.

### 9.2 Canonical rewrite

Writing is a canonical rewrite, not an in-place patch of source syntax.
Consequences:

- YAML comments and formatting may change;
- workbook cell styling or unknown worksheets need not survive;
- collection ordering may become canonical;
- shared merged entities may be repeated in every referencing representation;
- a resource may move from its read path to a distinct canonical write path; and
- the second write of an unchanged model and plan should be byte-stable where
  the underlying codec supports deterministic bytes.

### 9.3 Per-resource atomicity

Each non-omitted resource is first written to a uniquely named temporary file in
the destination directory. After the codec and filesystem write succeed, the
temporary file replaces the destination using the strongest portable atomic
replace available.

Requirements:

- create parent directories only within the workspace;
- never truncate the destination before a complete temporary file exists;
- clean up the operation's own temporary file after failure where possible;
- retain the previous destination when rendering or replacement fails; and
- report when the platform cannot guarantee atomic replacement.

The write is not globally transactional across all files. Best effort means one
failed output does not prevent independent outputs from being attempted.

### 9.4 Empty overlays

When `omitWhenEmpty: true`, the overlay codec can return `Omit`. Omission counts
as a successful planned outcome. Its rendered path is excluded from the expected
output set and may be removed as stale only after the whole write phase succeeds.

A base dataset/tree codec should not normally omit its output.

Concrete write planning should warn when a selected dataset contains a non-empty
standardized facet with no writable owner. Partial exports are permitted, but
the unowned facet must be visible in the write report.

### 9.5 Stale managed outputs

Stale cleanup happens only if **all** planned writable resources either succeeded
or were intentionally omitted. If any render, staging, or replacement fails, no
stale file is deleted.

On a completely successful write:

1. use each currently enabled writable rule's compiled path template to discover
   existing matched regular files;
2. calculate the normalized set of expected non-omitted output paths;
3. subtract expected paths from matched paths;
4. revalidate every candidate's confinement and file type immediately before
   deletion; and
5. delete only those stale files, recording each outcome.

Never delete:

- a file not matched by a currently enabled writable rule;
- scientific payloads merely referenced by `Data`;
- directories;
- symbolic links or reparse points;
- `.arc/project.yml` or local profile files;
- a path owned only by a rule/profile that is no longer present; or
- any stale candidate after a partial write failure.

This rule means changing or removing a workspace profile does not authorize
cleanup of its old files. Such files become unmanaged and require an explicit
migration or user action.

### 9.6 Shared entity writeback

Every dataset codec receives the canonical graph view of its selected dataset.
If that dataset references a merged sample/data/recipe, the codec writes the
merged entity. This behavior is required even if the current session originally
loaded only a subset of those fields from that resource.

## 10. Diagnostics and result model

### 10.1 Diagnostic structure

Every diagnostic should carry as much of the following as applies:

```fsharp
type DiagnosticSeverity =
    | Info
    | Warning
    | Error

type StorageDiagnostic = {
    Code: string
    Severity: DiagnosticSeverity
    Message: string
    RuleId: string option
    CodecId: string option
    RelativePath: string option
    DatasetIdentifier: string option
    Facet: string option
    Cause: string option
}
```

Do not expose platform-specific exception objects as the only error information.
Unexpected exceptions may be retained as an optional cause/debug value on .NET,
but a stable code and message are required across runtimes.

### 10.2 Suggested diagnostic codes

The exact prefix may follow project convention. At minimum distinguish:

```text
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
MERGE_CONFLICT
STALE_DELETE
```

### 10.3 Load and write results

Recommended shapes:

```fsharp
type LoadResult = {
    Arc: ARC option
    Session: WorkspaceSession option
    Resources: ResourceOutcome list
    Diagnostics: StorageDiagnostic list
}

type WriteResult = {
    Written: string list
    Omitted: string list
    Failed: string list
    RetainedAfterFailure: string list
    DeletedStale: string list
    StaleDeleteFailures: string list
    Resources: ResourceOutcome list
    Diagnostics: StorageDiagnostic list
}
```

Callers determine success from fatal plan/root errors and resource outcomes, not
from absence of warnings. Stable helpers such as `HasErrors` are useful.

## 11. Public API compatibility and placement

The new implementation should be parallel to existing APIs:

```fsharp
let! loaded =
    Workspace.loadAsync
        workspaceRoot
        codecRegistry
        workspaceProfileRegistry
        LoadOptions.defaults

match loaded.Arc, loaded.Session with
| Some arc, Some session ->
    // manipulate the same ProcessCore ARC model
    let! written = WorkspaceSession.updateAsync session WriteOptions.defaults
    ()
| _ ->
    // inspect diagnostics
    ()
```

Existing calls such as fixed YAML loading, scaffold loading, and the
`IsSpreadsheetScaffold` switch must behave exactly as before. Do not initially
rewrite them as wrappers around the project-file system; doing so would enlarge
the compatibility surface and obscure regressions.

Suggested source organization:

```text
src/ProcessCore/Storage/
  Types.fs
  Diagnostics.fs
  ProjectSchema.fs
  ProfileSchema.fs
  PathTemplate.fs
  Registry.fs
  Compiler.fs
  Planner.fs
  Merge.fs
  Reader.fs
  Writer.fs
  Workspace.fs
  BuiltInProfiles.fs
  Codecs/
    YamlTree.fs
    YamlDataset.fs
    IsaSpreadsheet.fs
    DatamapSpreadsheet.fs
```

Actual compile order must be added carefully to the .NET and JavaScript project
files. Python project inclusion can be deferred, but shared files must not
accidentally introduce Python transpilation failures into unrelated builds.

## 12. Validation laws and invariants

### 12.1 Semantic round trip

For a valid writable plan `P` and compatible model `M`:

```text
read(P, write(P, M)) ≈ M
```

Here `≈` is semantic ARC graph equivalence:

- same dataset hierarchy and identifiers;
- same owned fields/facets;
- same process connections;
- same canonical shared entities after compatible union; and
- no requirement for source formatting or object reference identity.

### 12.2 Canonical write stability

For deterministic codecs:

```text
write(P, read(P, write(P, M))) = write(P, M)
```

at the byte level after the first canonical rewrite, excluding timestamps or
other explicitly documented nondeterministic codec output. Built-in codecs should
avoid nondeterministic output.

### 12.3 Ownership invariant

Every dataset base and every typed facet in the writable model has zero or one
owner. Zero means intentionally not persisted by this plan; more than one is a
compile error.

### 12.4 Path invariant

Every discovered, rendered, replaced, or deleted resource resolves to a regular
file path under the workspace root and belongs to the rule reported for it.

### 12.5 Failure invariant

A resource failure:

- does not partially apply an overlay;
- does not attach a partially parsed dataset;
- does not truncate its existing write target;
- does not trigger stale deletion; and
- affects independent resources only through explicit target/dependency
  relationships.

## 13. Implementation sequence

### Phase 1: schemas, types, and path compiler

Implement:

- strict project/profile YAML decoding;
- matching JSON Schema draft 2020-12 artifacts under `schemas/yml`;
- versioned public storage types;
- parameter validation and substitution;
- qualified rule IDs and restricted overrides;
- invertible path-template parsing/rendering/discovery representation;
- path confinement helpers;
- structured diagnostics; and
- codec/profile registries.

Tests should be pure wherever possible.

### Phase 2: compiler and planners

Implement:

- registry capability validation;
- selector and attachment validation;
- ownership analysis;
- dependency graph and cycle detection;
- deterministic compiled order;
- read discovery planning; and
- write target/path planning with collisions.

Expose the pure plan APIs before adding filesystem mutation.

### Phase 3: built-in codec adapters

Implement:

- recursive YAML tree adapter;
- shallow YAML dataset adapter;
- ISA investigation/study/assay/workflow/run dataset adapters; and
- Datamap overlay adapter.

Reuse existing parsing and writing logic. Do not change old public entry points.

### Phase 4: reader and merge

Implement:

- deterministic discovery and resource execution;
- root construction;
- parent-before-child attachment;
- overlay transactionality;
- compatible-union canonicalization;
- best-effort outcomes and dependency skips; and
- `WorkspaceSession`.

### Phase 5: writer and pruning

Implement:

- model target selection;
- codec extraction/rendering;
- sibling temporary files and replacement;
- full-write success accounting;
- empty-overlay omission;
- current-rule stale discovery; and
- guarded stale deletion.

### Phase 6: built-in profiles, examples, and documentation

Add:

- monolithic YAML and scaffold workspace profiles;
- the representative example projects from [the project-file plan](project_file.md#11-worked-configurations);
- public API usage documentation;
- schema reference documentation;
- migration guidance emphasizing canonical rewrite; and
- runtime-support notes.

## 14. Test plan and acceptance criteria

### 14.1 Project and profile schema

Test:

- minimal project;
- unknown `type` and unsupported `specVersion`;
- unknown fields outside `extensions`;
- structural examples accepted by both the runtime decoder and published schema;
- invalid structural fixtures rejected consistently by both validation paths;
- empty project after disabled rules;
- duplicate project/profile/rule IDs;
- exact built-in profile resolution;
- local profile path resolution relative to `.arc`;
- local profile escaping the workspace;
- profile ID/version mismatch;
- duplicate profile reference;
- missing/extra/wrong-type parameter;
- default and allowed-value handling;
- restricted override success;
- override of forbidden semantic fields;
- override of missing rule; and
- deterministic profile/project rule order.

### 14.2 Path templates and security

Test:

- literal paths;
- `{dataset.identifier}` and `{parent.identifier}` discovery/rendering;
- whole-segment capture enforcement;
- duplicate or unresolved captures;
- absolute, drive, UNC, URI, empty, `.`, and `..` rejection;
- slash normalization;
- unsafe captured identifiers and parameter values;
- symlink/reparse escape;
- output file/directory mismatch;
- Windows case-folded collision;
- two semantic bindings rendering the same output; and
- deterministic discovery ordering.

### 14.3 Ownership and dependencies

Test:

- one root tree;
- one root plus shallow children;
- nested shallow datasets;
- overlay with base owner;
- duplicate base owner;
- duplicate facet owner;
- tree/descendant overlap;
- static and rendered path collisions;
- missing attachment target;
- ambiguous parent;
- dependency cycle; and
- write-only rules that do not participate in read root construction.

### 14.4 Read execution

Test:

- monolithic YAML;
- current scaffold;
- mixed YAML/XLSX layout;
- required missing root;
- optional missing overlay;
- one corrupt child among valid siblings;
- failed parent and skipped descendants;
- parsed identifier/capture mismatch;
- additional-type mismatch;
- shallow dataset containing inline children;
- overlay parse failure with no partial mutation;
- deterministic diagnostic/resource ordering; and
- valid partial ARC returned when only independent children fail.

### 14.5 Compatible union

For `Sample`, `Data`, and `Recipe`, test:

- missing scalar filled;
- equal scalar accepted;
- unequal scalar diagnosed and first retained;
- stable collection union;
- equal-key nested entity merge;
- duplicate elimination;
- dynamic missing key copied;
- dynamic nested map union;
- dynamic opaque conflict diagnosed;
- resource-order determinism; and
- process edges reference the canonical merged objects.

### 14.6 Write execution

Test:

- target selection and deterministic path rendering;
- merged entity emitted in every referencing dataset;
- output written through temporary sibling and replacement;
- existing target retained after codec/render/replace failure;
- independent outputs continue after one failure;
- empty optional overlay omitted;
- no stale deletion after any write failure;
- stale file deleted after complete success;
- unmatched scientific payload retained;
- directory and symlink never deleted;
- configuration files never deleted;
- removed-profile outputs left unmanaged;
- import path not treated as stale write path;
- second canonical write is stable; and
- write/read semantic equivalence.

### 14.7 Runtime parity

Run storage tests on:

- .NET; and
- Fable JavaScript on Node.

The first implementation is accepted without Fable Python support, but must not
silently claim it. Add Python only after path, filesystem replacement, and
spreadsheet support are explicitly implemented and tested there.

### 14.8 Repository verification

At implementation completion, run at least:

```powershell
.\build.cmd BuildSolution
.\build.cmd RunTests
.\build.cmd TestJs
```

Use `.\build.cmd RunTestsAll` when Python inclusion is added or when shared
changes can affect existing Python output.

## 15. Migration and compatibility notes

- Existing repositories without `.arc/project.yml` continue using the old APIs;
  the new API should report a clear missing-project diagnostic rather than
  guessing.
- A project can opt into the existing physical formats through built-in
  workspace profiles.
- The new API does not set `IsSpreadsheetScaffold` to summarize a mixed layout.
- Canonical rewrite should be called out before the first update of an existing
  hand-formatted workspace.
- Users should review the pure write plan before migration when read and write
  paths differ.
- Because there is no provenance/lockfile, shared entity enrichment is written
  wherever that entity is referenced.
- Changing profiles does not clean files owned only by the old plan.
- Future project schema versions must reject rather than reinterpret unsupported
  semantics.

## 16. Repository references

Use these sources when implementing or reviewing this design:

### Normative and project documentation

- [ARC specification index](../docs/spec/index.md)
- [Process Core overview](../docs/spec/process_core/overview.md)
- [Dataset](../docs/spec/process_core/Dataset.md)
- [Process](../docs/spec/process_core/Process.md)
- [Recipe](../docs/spec/process_core/Recipe.md)
- [Sample](../docs/spec/process_core/Sample.md)
- [Data](../docs/spec/process_core/Data.md)
- [Datamap overview](../docs/spec/datamap/overview.md)
- [DataContext](../docs/spec/datamap/DataContext.md)
- [Administrative overview](../docs/spec/administrative/overview.md)
- [Decorations overview](../docs/spec/decorations/overview.md)
- [Implementation guide](../docs/project/implementation.md)
- [Existing prior-art survey](../docs/project/prior-art.md)
- [Core data-model implementation plan](core_datamodel.md)

### Implementation

- [`ARC.fs`](../src/ProcessCore/ARC.fs)
- [`Graph.fs`](../src/ProcessCore/Graph.fs)
- [`ScaffoldReader.fs`](../src/ProcessCore/ScaffoldReader.fs)
- [`YML/Dataset.fs`](../src/ProcessCore/YML/Dataset.fs)
- [`FragmentSelector.fs`](../src/ProcessCore/FragmentSelector.fs)
- [`CrossAsync.fs`](../src/ProcessCore/Helper/CrossAsync.fs)

### Usage and tests

- [`arc.fsx`](../docs/core-implementation/arc.fsx)
- [`creating-datasets.fsx`](../docs/core-implementation/creating-datasets.fsx)
- [`decorations.fsx`](../docs/core-implementation/decorations.fsx)
- [`ARC` tests](../tests/ProcessCore.Tests/ARC.fs)
- [YAML round-trip tests](../tests/ProcessCore.Tests/YAML/Integration/RoundTrip.fs)
- [Spreadsheet scaffold tests](../tests/ProcessCore.Tests/Spreadsheet/Scaffold.fs)

### Preserved profile references

- [ARC specification on the `projectfile`
  branch](https://github.com/HLWeil/ARC-Data-Model/blob/projectfile/references/ARC%20specification.md)
- [ISA-XLSX](../references/ISA-XLSX.md)
- [ISA RO-Crate](../references/isa_ro_crate.md)
- [Workflow Run RO-Crate](../references/arc_wr_ro_crate.md)
- [Datamap RO-Crate](../references/arc_datamap_ro_crate.md)

## 17. Final handling decisions

For avoidance of ambiguity, the implementation plan records these decisions
unless a later design change explicitly supersedes them:

1. Writes are canonical rewrites, not exact source-preserving edits.
2. New APIs are parallel to and do not replace the current ARC I/O APIs.
3. Resource execution is best effort, but project and plan errors are fatal.
4. Initial runtime support is .NET and Node.js; Python is deferred.
5. Equal-key shared entities merge by compatible union.
6. Canonical merged entities are written through every referencing dataset
   representation.
7. Sessions retain bindings only in memory.
8. Stale managed outputs are deleted only after every planned write succeeds.
9. Only files matched by current writable rules are eligible for stale deletion.
