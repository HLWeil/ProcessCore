# ARC project file handling

Status: implementation-planning document

Language plan:
[`plans/project_file.md`](project_file.md)

The intended manifestation of this plan is the ProcessCore software
implementation. The normative project-file language remains in
[`docs/spec/project_file.md`](../docs/spec/project_file.md).

## 1. Summary

Project handling adds one configuration-driven path through the existing ARC
load and write APIs:

```text
.arc/project.yml + referenced profiles + codec registry
                         |
                         v
              load, validate, resolve
                         |
              +----------+----------+
              |                     |
       resolve resources       select Datasets
              |                     |
         codec -> Dataset      Dataset -> codec
              |                     |
              +----------+----------+
                         |
                      ARC graph
```

Each rule maps one complete Dataset to one safe workspace-relative anchor and
optional named auxiliary files through one exact bidirectional codec ID.
Project-level targets address only the root and its direct children; codecs may
preserve deeper Dataset nesting.

The first implementation is deliberately small:

- resolved forms are internal implementation details;
- operations return the existing `ARC` or `unit` shapes;
- failures use one structured project error and fail the operation;
- project updates re-read and resolve the project instead of retaining a
  workspace session; and
- graph attachment reuses existing canonicalization without adding a separate
  merge policy.

It does not introduce partial ARC results, per-resource outcome objects, a
diagnostic framework, strict/rich API variants, or generic transaction and
stale-file management.

## 2. Implementation surface

The implementation should add equivalents of:

```fsharp
type WorkspaceProject
type WorkspaceProfile
type WorkspaceProfileReference =
    | File of string
    | Url of string
type StorageTarget =
    | Root
    | Identifier of string
    | AdditionalType of string
type StorageRule
type StorageFile
type StorageFileCreation = Empty
type CodecInput
type CodecOutput
type DatasetCodec
type ProjectError
exception ProjectException of ProjectError
```

`WorkspaceProject`, `WorkspaceProfile`, and `StorageRule` are the strict decoded
document model. A small internal resolved-project representation holds resolved
rules containing the qualified rule ID, codec, target, and prepared path
template. No public planner, binding, execution-plan, session, or result types
are needed.

`ProjectError` should distinguish configuration, profile loading, codec lookup,
path, target, and resource failures and carry a human-readable message plus
available rule, codec, anchor, and cause context. The implementation does not
need a stable catalog of individual diagnostic codes in its first version.
Internal project operations return `Result`; existing exception-based ARC
methods raise `ProjectException` containing the structured error.

### 2.1 Codec contract

The embedding application constructs an exact-ID registry of bidirectional
Dataset codecs. Duplicate IDs are rejected. Project data cannot dynamically
load packages, assemblies, modules, scripts, callbacks, or commands.

The logical codec contract is:

```fsharp
readDatasetAsync:
    CodecContext -> CodecInput -> CrossAsync<Result<Dataset, ProjectError>>

writeDatasetAsync:
    CodecContext -> Dataset -> CrossAsync<Result<CodecOutput, ProjectError>>
```

`CodecInput` contains required primary bytes and existing auxiliary bytes keyed
by declared file ID. `CodecOutput` contains primary bytes and codec-managed
auxiliary bytes. The runtime resolves, confines, reads, validates, and writes
all paths; codecs never derive paths or access the filesystem. Expected failures
use `ProjectError`; unexpected exceptions are caught at the codec boundary.

The standard registry provides:

```text
isa.investigation.xlsx
isa.study.xlsx
isa.assay.xlsx
isa.workflow.xlsx
isa.run.xlsx
```

These IDs can share one adapter mechanism over the existing `ScaffoldReader`
functions. ISA-XLSX adapters consume and produce the declared `datamap` bytes.
Generic project handling creates `create: empty` resources and never deletes
stale anchors or auxiliary files.

No recursive-YAML codec ID is standardized. A local application may register a
bidirectional Dataset YAML adapter under its own stable ID.

## 3. Project resolution and operation preparation

Given a workspace root, project discovery checks exactly
`.arc/project.yml` and does not search ancestors.

Resolution:

1. strictly decodes the project;
2. loads `file` profiles relative to `.arc` and `url` profiles over HTTP(S);
3. strictly decodes every profile;
4. expands profile rules in reference order, followed by local rules;
5. qualifies rule IDs;
6. resolves every exact codec ID;
7. resolves targets and prepares path templates;
8. requires exactly one root rule;
9. rejects duplicate profile IDs, qualified rule IDs, identifier targets, and
   additional-type targets;
10. resolves auxiliary declarations relative to anchor directories;
11. rejects statically identical anchor and auxiliary templates; and
12. records the exact-identifier reservation set.

Any project resolution error prevents codec invocation.

### 3.1 Paths

The path-template resolver supports literal `/`-separated segments and at most
one whole `{dataset.identifier}` segment. It rejects unsupported captures,
partial captures, backslashes, empty or traversal segments, absolute or
URI-like forms, and paths escaping the configured base.

Resolved paths must remain confined after normalization and symlink or reparse
point resolution. Collision comparison uses the host filesystem's effective
case behavior.

Target-specific behavior is:

- a literal root anchor is checked directly;
- a captured root anchor is discovered and must match exactly once;
- a literal identifier anchor is checked directly;
- a captured identifier anchor is rendered from its declared identifier; and
- an additional-type path discovers zero or more anchors and therefore must
  contain the identifier capture.

Auxiliary paths contain literal safe segments, are resolved from each concrete
anchor directory, and do not repeat the Dataset capture. IDs are unique within
their rule. Every auxiliary path follows the same confinement and reparse-point
checks as an anchor.

### 3.2 Operation preparation

Before the first codec invocation, a read operation resolves all root,
identifier, and additional-type anchors plus their auxiliary files, excludes
type anchors reserved by exact identifier rules, and rejects every concrete
collision.

Before the first codec invocation, a write operation selects all target
Datasets, renders every anchor and auxiliary path, verifies mandatory exact
targets, and rejects unsafe paths, duplicate Dataset bindings, and concrete
collisions.

Bindings use a stable platform-independent order: root, exact identifiers,
additional types, normalized anchor, then qualified rule ID. Identifier
reservation, not declaration order, determines precedence.

## 4. Read and write execution

### 4.1 Read

After successful preflight:

1. read each binding's required primary bytes and existing optional auxiliary
   bytes;
2. decode the mandatory root Dataset;
3. verify its captured identifier when applicable;
4. construct the `ARC` root without discarding nested state;
5. decode each mandatory exact-identifier Dataset and verify its declared and
   captured identifier;
6. decode each discovered additional-type Dataset and verify its captured
   identifier and exact case-sensitive `additionalType`; and
7. attach each direct child with the established `Dataset.AddPart` graph API.

Additional-type rules may have no matching anchors. Root and exact-identifier
resources are mandatory.

Any codec, identity, type, or attachment failure fails the load and no partial
ARC is returned. A failed Dataset is never attached. Deeper Datasets returned by
a codec remain nested inside their selected Dataset.

`Dataset.AddPart` already establishes parentage and canonicalizes graph nodes and
recipes against the root. Project handling must use that behavior as-is. It does
not merge scalar fields, recursively merge dynamic properties, select a
first-value winner, or introduce project-specific merge conflicts.

### 4.2 Write

Write selection binds:

- the root rule to the root Dataset;
- each identifier rule to its mandatory direct child; and
- each additional-type rule to matching direct children not reserved by an
  identifier rule.

Unmatched direct children have no independent project output, although they may
remain nested inside another selected Dataset's codec representation.

After successful preflight, each codec receives one complete selected Dataset
and returns primary plus named auxiliary bytes. Before writing an invocation,
generic handling rejects undeclared IDs and codec output for project-managed
files. It then writes the primary and returned files and emits every
`create: empty` declaration as zero bytes. The first codec or resource failure
fails the operation. Outputs already written by earlier codecs are not rolled
back, and generic handling does not delete stale resources.

## 5. ARC integration

The relevant existing code is in `ARC.fs`, `ScaffoldReader.fs`,
`YML/Dataset.fs`, and `Graph.fs`.

Generic `ARC.loadAsync` checks the supplied workspace for `.arc/project.yml`
before existing YAML or spreadsheet detection. When present, the project is
authoritative: a project failure does not fall back to another representation.
When absent, current detection remains unchanged.

Generic `WriteAsync` checks the destination for a project and uses it when
present. `UpdateAsync` resolves its destination from the explicit path or
`ArcPath` and re-resolves that destination's project on every operation. Existing
synchronous methods remain wrappers around the asynchronous implementation.

Explicit YAML and XLSX load/write methods bypass project discovery. Project
handling never creates, rewrites, or deletes `.arc/project.yml` or referenced
profile documents.

The implementation must remain compatible with .NET and supported Fable
targets. Existing platform abstractions should be reused. No production
dependency is added without prior approval.

## 6. Implementation sequence

### Phase 1: documents and resolution

- document types and strict YAML decoding;
- local and HTTP(S) profile resolution;
- target and path-template resolution;
- exact codec registry; and
- cross-rule and path validation.

### Phase 2: execution

- read and write preflight;
- Dataset codec interface;
- shared ISA-XLSX adapter mechanism;
- root construction and `Dataset.AddPart` attachment; and
- fail-fast project errors.

### Phase 3: facade and parity

- generic ARC discovery and authority;
- update-by-resolution;
- explicit-format bypass;
- examples and usage documentation; and
- .NET, JavaScript, and Python verification.

## 7. Test plan

Test:

- strict project/profile decoding, file and URL resolution, and duplicate
  rejection;
- missing or duplicate root, identifier, type, rule, and codec cases;
- literal and captured root/exact paths plus zero/many type discoveries;
- path traversal, symlink escape, host-case collisions, unsafe captured
  identifiers, auxiliary path safety, and duplicate auxiliary IDs;
- identifier reservation and precedence independent of declaration order;
- required-resource, identifier, capture, and `additionalType` failures;
- confirmation that anchor/auxiliary collisions are detected before codec
  execution;
- direct-root attachment through `Dataset.AddPart` with deeper nesting and
  existing graph canonicalization preserved;
- bidirectional ISA-XLSX Dataset/Datamap adapter behavior, missing optional
  Datamaps, and conditional Datamap output;
- unconditional project-managed empty-file output and rejection of undeclared
  codec outputs;
- fail-fast codec and attachment errors without a returned partial ARC;
- confirmation that failed writes do not trigger rollback or stale deletion;
- generic project authority and explicit-format bypass;
- update resolution at the selected destination;
- selected-Dataset write/read equivalence for deterministic codecs; and
- .NET/Fable runtime parity.
