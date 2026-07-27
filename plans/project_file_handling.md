# ARC project file handling

Status: implementation-planning document

Language plan:
[`plans/project_file.md`](project_file.md)

Normative handling specification:
[`docs/spec/project_file_handling.md`](../docs/spec/project_file_handling.md)

## 1. Summary

Project handling compiles the rules from `.arc/project.yml`, selects the root and
direct-child Datasets, resolves safe anchor paths, and invokes one registered
bidirectional codec per selected Dataset.

```text
root project + file/URL profiles + codec registry
                         |
                         v
                CompiledStoragePlan
                         |
                  +------+------+
                  |             |
               read plan      write plan
                  |             |
           anchor -> Dataset  Dataset -> anchor
                  |             |
                  +------v------+
                     ARC graph
```

The Dataset is the only project-visible storage unit. A codec decides whether
its Dataset representation is recursive, shallow, one file, or an anchor plus
opaque companion files.

## 2. Current implementation context

The relevant implementation is under `src/ProcessCore`:

- `ARC.fs` provides current explicit YAML/scaffold I/O and generic load/write
  behavior;
- `ScaffoldReader.fs` already derives `isa.datamap.xlsx` from an ISA anchor and
  enriches or writes the same Dataset;
- `YML/Dataset.fs` can preserve recursively nested Datasets; and
- `Graph.fs` attaches Datasets and canonicalizes shared graph entities.

The new storage layer should reuse these behaviors through adapters. Existing
explicit YAML and XLSX APIs remain independent of project discovery.

The implementation must remain portable across .NET and supported Fable
targets. It must not add production dependencies without approval.

## 3. Compiled model

The public model should provide equivalents of:

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
type CompiledStorageRule
type CompiledStoragePlan
type CodecRegistry
type WorkspaceSession
type ResourceBinding
type ResourceOutcome
type StorageDiagnostic
type LoadResult
type WriteResult
```

A compiled rule contains:

- qualified rule ID;
- exact codec ID and resolved codec;
- one target;
- compiled anchor matcher/renderer; and
- deterministic source order for reporting.

The compiled plan contains:

- exactly one root rule;
- a map of exact identifier rules;
- a map of additional-type rules;
- the reserved identifier set;
- compiled anchor-path collision data.

There are no read/write variants of a compiled rule.

## 4. Codec registry and contract

### 4.1 Registry

The embedding application constructs the registry explicitly. Capability IDs
are unique exact keys. Project configuration cannot dynamically load assemblies,
packages, scripts, or modules.

Every codec used by a project must be bidirectional and operate on one complete
Dataset per invocation.

Logical operations should be equivalent to:

```fsharp
readDatasetAsync:
    CodecContext -> anchorPath:string -> CrossAsync<Result<Dataset, CodecError>>

writeDatasetAsync:
    CodecContext -> anchorPath:string -> Dataset -> CrossAsync<Result<unit, CodecError>>
```

The context supplies a confined workspace boundary and diagnostic facilities.
The precise F# surface can follow existing repository conventions.

### 4.2 Anchor and companions

The rule path is the project-visible anchor. Generic planning and outcomes know
only that anchor.

A registered codec may privately derive companion paths. ISA-XLSX adapters can
reuse the current behavior that maps an ISA anchor to adjacent
`isa.datamap.xlsx`.

Because companions are opaque:

- the generic planner does not enumerate them;
- generic collision detection covers anchors only;
- result paths and standard resource diagnostics report anchors only;
- the workspace layer does not stage or replace a multi-file codec bundle;
- project handling does not delete stale anchors or companions; and
- the codec is responsible for confining companion access to the workspace and
  for avoiding destructive partial behavior where practical.

This is an intentional trust boundary for registered codecs. It must be stated
clearly rather than implying generic guarantees that the processor cannot
enforce.

### 4.3 Standard codecs

The default registry should provide:

```text
isa.investigation.xlsx
isa.study.xlsx
isa.assay.xlsx
isa.workflow.xlsx
isa.run.xlsx
```

Each spreadsheet adapter reads and writes the Dataset, including its Datamap
state according to the ISA-XLSX representation.

Recursive YAML can be registered under an implementation-chosen stable ID and
used by local rules. No recursive-YAML profile or codec ID is standardized by
the project-file specification.

## 5. Compilation

Compilation:

1. strictly validates the project and every referenced profile;
2. loads `file` profiles from `.arc` and `url` profiles over HTTP(S);
3. expands profile rules in reference order, followed by project-local rules;
4. qualifies rule IDs and rejects duplicate identities;
5. resolves every codec and verifies that it is bidirectional;
6. validates targets, paths, and the single-root requirement;
7. reserves exact identifiers and rejects duplicate targets or static anchor
   conflicts; and
8. emits an immutable compiled plan.

Compilation errors are fatal and prevent codec execution.

Identifier/additional-type overlap is not a conflict. The compiled reserved set
makes their concrete selection domains disjoint.

### 5.1 Profile loading

A `file` is relative to `.arc` and must remain inside it. A `url` is an absolute
HTTP(S) URL. The loaded YAML must be an `ArcWorkspaceProfile`; resolution,
fetch, parse, and document-type failures stop compilation.

## 6. Path processing

The path compiler supports:

- literal `/`-separated segments; and
- `{dataset.identifier}` as one complete segment, at most once.

It rejects other brace syntax, partial captures, globs, unsafe segments, absolute
forms, backslashes, traversal, and resolution outside the workspace.

Target rules compile as follows:

- a literal root anchor is checked directly;
- a captured root anchor is discovered and must match exactly once;
- a literal identifier anchor is checked directly;
- a captured identifier anchor is rendered using the declared identifier and
  checked directly;
- an additional-type template is a discovery matcher and must include the
  capture.

Every discovered capture is decoded as a safe path segment. Parsed identifiers
must equal their declared or captured values.

Write planning renders anchors from selected Dataset identifiers and rejects all
normalized anchor collisions before invoking a codec.

## 7. Read planning and execution

### 7.1 Plan

Read planning:

1. resolves the mandatory root anchor;
2. resolves every mandatory exact-identifier anchor;
3. discovers zero or more additional-type anchors;
4. excludes type bindings whose captured identifier is reserved by an exact
   rule;
5. rejects duplicate captured identifiers and normalized anchor collisions; and
6. orders root, exact identifiers, then type bindings deterministically.

A general path containing a reserved identifier does not satisfy the mandatory
exact rule.

### 7.2 Execute

Execution:

1. invokes the root codec;
2. verifies its capture, when present;
3. promotes or copies the returned Dataset into an `ARC`;
4. invokes exact-identifier codecs and verifies declared/captured identities;
5. invokes type codecs and verifies captured identity and exact
   `additionalType`;
6. attaches each successful project-level child directly to the root through
   established graph APIs;
7. preserves any nested Datasets already contained in the returned child;
8. canonicalizes and compatibly merges shared Samples, Data, and Recipes; and
9. records ordered outcomes and diagnostics.

Root absence or failure yields no usable ARC. Exact-identifier absence or failure
is an error but need not cancel independent sibling resource attempts after the
root exists.

Type rules may discover no resources without error.

The processor never flattens deeper nesting and never uses a type-rule resource
as a substitute for a missing exact target.

## 8. Write planning and execution

### 8.1 Select targets

Write planning:

1. binds the root rule to the root Dataset;
2. resolves every exact identifier against direct root children;
3. reports each missing exact identifier as a target error;
4. selects direct children for additional-type rules by exact case-sensitive
   value;
5. excludes all reserved identifiers from type selection;
6. renders every anchor;
7. rejects unsafe anchors, duplicate bindings, and normalized collisions; and
8. orders root, exact, then type bindings deterministically.

Unmatched direct children have no independent project binding. They may still be
part of another selected Dataset's codec representation.

### 8.2 Execute

For every valid binding, the writer passes the selected complete Dataset and
anchor to its codec. Independent binding failures do not prevent remaining
bindings from being attempted.

Writing is a canonical model-to-representation operation. Format fidelity such
as YAML comments or unknown workbook styling is outside the generic contract.

Project handling:

- does not provide a cross-file transaction;
- does not promise atomic replacement for opaque codec resources;
- does not infer or prune stale anchors;
- does not ask codecs to remove representations for Datasets no longer selected;
  and
- reports success or failure at the anchor binding.

## 9. Canonical identity and merge

Independently parsed Dataset representations may reference the same model
entities. Retain compatible-union behavior using established identity keys:

| Entity | Key |
|---|---|
| `Sample` | name |
| `Data` | path plus fragment selector |
| `Recipe` | name plus version |

For matching entities:

- fill absent scalar values from present incoming values;
- retain equal values;
- retain the first unequal value and report a merge conflict;
- stable-union collections;
- recursively merge compatible dynamic maps; and
- preserve graph back-edges and canonical references through existing attachment
  APIs.

Processes remain distinct objects. Storage configuration is not inserted into
model dynamic properties.

## 10. Results and diagnostics

Resource outcomes should be limited to:

```text
Succeeded
Absent
Failed
SkippedNoRoot
SkippedTarget
```

`Absent` is normal only for a type rule with no matches. Missing root and exact
resources produce errors.

A diagnostic should contain:

- stable code;
- severity;
- message;
- qualified rule ID when applicable;
- codec ID when applicable;
- anchor path when applicable;
- target or Dataset identifier when applicable; and
- normalized cause text.

Core diagnostic categories:

```text
PROJECT_NOT_FOUND
PROJECT_PARSE
PROFILE_LOAD
PROFILE_INVALID
PROFILE_DUPLICATE
RULE_DUPLICATE
RULE_INVALID
ROOT_RULE_COUNT
TARGET_DUPLICATE
TARGET_NOT_FOUND
CODEC_NOT_REGISTERED
CODEC_NOT_BIDIRECTIONAL
PATH_UNSAFE
PATH_TEMPLATE_INVALID
PATH_COLLISION
RESOURCE_REQUIRED_MISSING
RESOURCE_PARSE
RESOURCE_WRITE
IDENTIFIER_MISMATCH
ADDITIONAL_TYPE_MISMATCH
MERGE_CONFLICT
```

## 11. Public API integration

Generic ARC loading and writing should check the exact supplied workspace for
`.arc/project.yml`.

When present:

- the project is authoritative;
- compilation or root failure does not fall back to another representation;
- result-returning APIs expose the compiled session, anchor outcomes, and
  diagnostics; and
- strict convenience APIs preserve their current return types and throw a
  structured storage exception after independent work completes.

When absent, existing non-project I/O behavior remains.

Explicit YAML and XLSX methods bypass project discovery and retain their existing
behavior.

Recommended operations remain equivalent to:

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

## 12. Implementation sequence

### Phase 1: syntax and paths

- simplified F# document types;
- strict YAML decoding;
- matching JSON Schemas;
- target and anchor-template parsing;
- confined file and HTTP(S) profile loading; and
- diagnostics.

### Phase 2: registries and compilation

- file/URL profile expansion;
- duplicate-profile detection;
- profile-qualified rule IDs;
- bidirectional codec validation;
- root and target uniqueness;
- identifier reservation and precedence; and
- static anchor validation.

### Phase 3: planners

- mandatory root/exact anchor resolution;
- type discovery;
- capture validation;
- write selection and precedence; and
- concrete anchor-collision checks.

### Phase 4: codec adapters and execution

- five ISA-XLSX Dataset adapters;
- opaque Datamap companion behavior;
- optional local recursive-YAML adapter;
- root construction and child attachment;
- compatible-union canonicalization; and
- best-effort anchor outcomes.

### Phase 5: facade and documentation

- generic ARC facade integration;
- sessions and rich results;
- explicit-format bypass;
- standard scaffold file/URL examples;
- usage examples; and
- .NET, JavaScript, and Python parity checks.

## 13. Test plan

Test:

- strict project and profile schema acceptance/rejection;
- local and URL references with exactly one source field;
- profile load, parse, type, and duplicate-ID failures;
- missing and duplicate root rules;
- duplicate exact and type targets;
- identifier precedence independent of rule order;
- fixed and captured exact paths;
- required root and exact resources;
- zero/many type resources;
- capture, identifier, and `additionalType` mismatches;
- unsafe paths and anchor collisions;
- direct-root attachment with deeper nesting preserved;
- shared entity compatible union;
- independent parse/write failure behavior;
- opaque ISA/Datamap companion integration;
- confirmation that project writes delete no stale files;
- project-aware generic facade behavior;
- explicit-format bypass; and
- .NET/Fable runtime parity.
