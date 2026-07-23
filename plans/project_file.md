# ARC project file and generalized dataset partitioning

Status: implementation-ready design

Target project: `ProcessCore`

Proposed project-file path: `.arc/project.yml`

Initial runtime targets: .NET and JavaScript

Out of scope for the first implementation: Python runtime support, arbitrary scientific payload partitioning, remote profiles, and exact source-format preservation

## 1. Purpose

ARC's in-memory model is a graph of nestable `Dataset` objects. The current `ARC`
type adds filesystem I/O for two fixed physical representations:

1. one recursively nested `arc.yml`; or
2. the fixed ISA-XLSX scaffold described by the ARC specification.

That is sufficient while the physical layout and the parser are selected as one
indivisible choice. It does not cover workspaces in which different datasets or
facets of a dataset live in different files, use different supported formats, or
follow a reusable organization-specific convention.

This document specifies a generalized storage layer based on a root project file,
reusable **workspace profiles**, declarative storage rules, and explicitly
registered codecs. Its central abstraction is:

```text
physical metadata resource(s) ⇄ registered codec ⇄ Dataset contribution
```

A contribution is one of:

- a complete dataset tree;
- one shallow dataset; or
- one typed overlay on a dataset, such as its Datamap facet.

The same normalized rules drive both directions:

- **read:** discover resources, parse contributions, attach them to the correct
  dataset, and merge shared entities;
- **write:** select contributions from the in-memory model, render them through
  the correct codec, place them at deterministic paths, and prune stale managed
  outputs after a completely successful write.

The project file is not another serialization of the ARC model. It is a storage
plan for locating and materializing that model.

## 2. Goals

The first implementation must:

- allow metadata belonging to an ARC to be partitioned across arbitrary safe
  paths inside one local workspace;
- support both individually enumerated resources and reusable layout conventions;
- make every parser/writer choice explicit and validate it before I/O;
- express read-only, write-only, and bidirectional rules;
- reconstruct the same nested `Dataset` graph regardless of physical
  partitioning;
- define an unambiguous inverse mapping for writable layouts;
- allow one base representation plus independently stored typed overlays;
- reconcile shared `Sample`, `Data`, and `Recipe` entities deterministically;
- continue independent reads or writes after a resource-level failure and return
  structured diagnostics;
- protect unmanaged scientific data and unrelated files;
- work with Fable on .NET and Node.js without reflection-based plugin loading;
- leave all current `ARC` YAML and spreadsheet APIs unchanged;
- make workspace profiles usable as built-ins or as local declarative YAML files;
- permit project-local path and option overrides without silently changing a
  profile's semantic ownership; and
- provide pure planning functions that can be inspected and tested without
  touching the filesystem.

## 3. Non-goals

The first implementation deliberately does not:

- partition or rewrite scientific payload bytes such as CSV, TSV, images,
  Parquet, mzML, FASTQ, or arbitrary `Data` targets;
- fetch project files, profiles, schemas, or codecs over the network;
- execute scripts, templates, shell commands, or user-supplied code from YAML;
- dynamically load assemblies, npm packages, or arbitrary parser plugins named
  by a project file;
- preserve workbook formatting, YAML comments, source ordering, or the exact
  original distribution of shared entity fields;
- support field-by-field partitioning of arbitrary ARC objects;
- persist a lockfile or per-field provenance map;
- support nested project manifests or independently mounted subprojects;
- replace or wrap the existing `ARC.Load`, `ARC.Write`, YAML, or scaffold APIs;
- require Python support in the initial release; or
- define a general data-lake table format, content-addressed object store,
  archival package format, or validation-package mechanism.

Scientific payloads remain referenced through `Data` objects. The storage layer
manages ARC metadata resources only.

## 4. Repository and model context

### 4.1 Abstract model

The normative model is documented in [the ARC specification
index](../docs/spec/index.md). It combines three peer profiles into one runtime
model:

- [Process Core](../docs/spec/process_core/overview.md), which describes samples,
  data, processes, recipes, and datasets;
- [Datamap](../docs/spec/datamap/overview.md), which adds contextual descriptions
  of data fragments; and
- [Administrative](../docs/spec/administrative/overview.md), which adds agents,
  organizations, citations, and related descriptive metadata.

[Decorations](../docs/spec/decorations/overview.md), including ISA and Workflow
Run concepts, refine those shared types rather than creating separate object
universes. The relevant design principles are:

- **process-centric:** experiments are represented through processes connecting
  inputs and outputs and executing recipes;
- **unified:** profiles and decorations share the same runtime types;
- **extensible:** types can carry additional properties;
- **model-first:** YAML, spreadsheets, SQL, and future representations are
  projections of the model, not the model itself; and
- **representation-aware:** a profile may impose useful conventions without
  making the core graph depend on one physical syntax.

The main storage-relevant entities are:

- [`Dataset`](../docs/spec/process_core/Dataset.md): the nestable aggregate and
  ownership boundary;
- [`Process`](../docs/spec/process_core/Process.md): an experimental or
  computational step with model inputs and outputs;
- [`Recipe`](../docs/spec/process_core/Recipe.md): the protocol or recipe executed
  by a process;
- [`Sample`](../docs/spec/process_core/Sample.md): material participating in a
  process;
- [`Data`](../docs/spec/process_core/Data.md): a logical or physical data
  reference, potentially with fragment selection; and
- [`DataContext`](../docs/spec/datamap/DataContext.md): a Datamap description of
  the semantics of a data entity or fragment.

Administrative datasets and entities are described in [the Administrative
overview](../docs/spec/administrative/overview.md). The implementation overview
and schema mapping are in [implementation.md](../docs/project/implementation.md)
and the original implementation plan is in
[core_datamodel.md](core_datamodel.md).

### 4.2 Dataset nesting is the partitioning axis

`Dataset` is the natural unit of physical partitioning because it:

- can contain child datasets through `HasPart`/`PartOf`;
- owns processes and the entities used to describe them;
- can carry administrative and Datamap information;
- is already the aggregation unit used by YAML and spreadsheet I/O; and
- has a stable model identity through its identifier.

Version 1 therefore partitions at **Dataset plus typed overlay** granularity. It
does not allow arbitrary individual fields to be assigned to files. This keeps
ownership understandable and makes bidirectional rules tractable.

### 4.3 Current F# implementation

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

### 4.4 Existing physical layouts

The current YAML representation writes one recursive `arc.yml`. That resource
owns a complete dataset tree.

The current scaffold reader recognizes a fixed structure:

```text
isa.investigation.xlsx
studies/<identifier>/isa.study.xlsx
assays/<identifier>/isa.assay.xlsx
workflows/<identifier>/isa.workflow.xlsx
runs/<identifier>/isa.run.xlsx
.../isa.datamap.xlsx
```

The investigation is read first. Study, assay, workflow, and run workbooks become
child datasets. Adjacent Datamap workbooks act as overlays by adding
`DataContext` information to an already identified dataset. The preserved
[ARC specification](https://github.com/HLWeil/ARC-Data-Model/blob/projectfile/references/ARC%20specification.md)
from the repository's `projectfile` branch and
[ISA-XLSX notes](../references/ISA-XLSX.md) document this profile-shaped layout.

The generalized layer must be able to describe both current layouts without
making either one the default ontology of storage.

### 4.5 Identity and canonicalization

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
parts of the same entity. Section 14 defines the required merge.

### 4.6 Extensibility and runtime constraints

Model types derive from `DynamicObj`, so unknown model properties can survive
YAML round trips. Storage-level extension handling must remain separate from
model-level dynamic properties.

The core library is compiled for .NET and through Fable. New public types and
algorithms should continue using the project's portable collection, path, and
async conventions. The implementation must not depend on reflection, runtime
assembly scanning, filesystem APIs unavailable to Node, or a new production
dependency without prior approval.

## 5. Terminology

The word “profile” is overloaded in this domain. This design uses the following
terms consistently:

- **model profile:** Process Core, Datamap, or Administrative;
- **decoration:** ISA, Workflow Run, or another refinement of model types;
- **workspace project:** the root `.arc/project.yml` storage configuration;
- **workspace profile:** a reusable, declarative set of storage rules;
- **codec:** registered code that converts between a physical resource and one
  contribution kind;
- **rule:** one declarative ownership, selection, path, direction, and codec
  mapping;
- **compiled rule:** a validated, fully expanded rule with qualified identity;
- **resource:** one physical metadata file consumed or produced by a rule;
- **target:** the dataset or overlay selected in the model;
- **binding:** the association among compiled rule, captures, target, and path
  discovered during a load or computed during a write;
- **tree contribution:** a full dataset subtree;
- **dataset contribution:** exactly one shallow dataset without inline child
  datasets;
- **overlay contribution:** one named facet applied to an existing dataset;
- **managed output:** a file path matched by a currently enabled writable rule;
  and
- **payload:** a scientific data file referenced by the ARC model but not managed
  as a metadata resource by this design.

## 6. Conceptual architecture

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

## 7. Contribution kinds and ownership

### 7.1 Facet model

Every dataset has an implicit `arc.base` facet containing its identity,
descriptive/administrative properties, processes, data references, and decoration
fields not assigned to another standardized detachable facet. `arc.datamap`
contains `DataContext` state. A contribution's codec descriptor declares the
facet set it can faithfully read and write.

- a dataset rule always claims `arc.base` for its selected dataset and may claim
  additional facets declared by its codec;
- an overlay rule claims exactly its named detachable facet;
- a tree rule claims `arc.base` throughout its subtree plus every additional
  facet declared by its codec throughout that subtree; and
- shared sample/data/recipe closure is serialized as required by a base codec
  but is reconciled by canonical identity rather than treated as a separately
  assignable facet.

A codec must not read into or write a detachable facet the compiled rule does not
own. Consequently, `arc.yaml.tree.v1` can represent the current complete
recursive YAML document and declares `arc.datamap` ownership, while the planned
`arc.yaml.dataset.v1` is shallow and base-only. A future all-facets shallow YAML
capability would need a distinct descriptor/ID.

If a base-only representation contains a non-empty field belonging to an
unowned standardized facet, its codec must fail that resource with an
ownership/facet diagnostic rather than silently discard it. On write it omits
that facet by contract.

Concrete write planning should warn when a selected dataset contains a non-empty
standardized facet with no writable owner. This is permitted because projects
may intentionally export only part of a model, but it must be visible in the
write report.

### 7.2 Tree contribution

A tree codec reads or writes one resource that recursively owns a selected
dataset and every descendant encoded inside it.

Use cases:

- the existing monolithic `arc.yml`;
- another future recursive document format.

A tree rule excludes base dataset rules for that root and all descendants in its
owned subtree. An overlay rule may coexist only when its facet is not already
owned by the tree codec's declared facet set. For version 1, the built-in
monolithic YAML tree codec should conservatively declare ownership of all
metadata facets it can serialize.

Those exclusions apply per direction. A read-only monolithic tree can coexist
with write-only shallow dataset rules to perform a controlled repartitioning.

### 7.3 Dataset contribution

A dataset codec reads or writes one **shallow** dataset. `HasPart` is not encoded
as recursively embedded child objects. Parent/child relationships are established
by the rule's target and `attachTo` semantics.

Use cases:

- one YAML document per study or assay;
- one ISA-XLSX workbook per decorated dataset;
- a mixed layout in which different datasets use different codecs.

A new shallow YAML codec is required if YAML is to be used per dataset. The
existing recursive YAML codec must not be silently reused with altered semantics.

### 7.4 Overlay contribution

An overlay codec reads or writes one named facet of a selected dataset without
owning the base dataset.

The first standardized facet is:

```yaml
facet: arc.datamap
```

Use case: `isa.datamap.xlsx` adjacent to an assay workbook.

Future facets require:

1. a stable facet identifier;
2. a typed overlay representation or patch contract;
3. explicit codec registration; and
4. documented ownership and merge behavior.

An overlay codec must parse a complete overlay value before applying it. A parse
failure must never partially mutate the dataset.

### 7.5 Exclusive ownership

After profile expansion and overrides, the compiler must prove:

- at most one base tree/dataset owner for each `(direction, dataset)`;
- at most one owner for each `(direction, dataset, facet)`;
- no tree owner overlaps a dataset owner below it in the same direction;
- no two writable bindings resolve to the same normalized output path; and
- every writable target has an invertible output path.

These are plan errors and are fatal before any resource I/O.

Ownership is direction-specific. One import-only rule and one export-only rule
may intentionally own the same dataset/facet through different codecs. A
bidirectional rule claims both directions. Rule order never resolves two owners
in the same direction.

Shared `Sample`, `Data`, and `Recipe` references are the intentional exception to
exclusive physical occurrence. They are canonical graph entities, not separately
owned facets. Their compatible-union behavior is specified in section 14.

## 8. Project file

### 8.1 Location and root

There is exactly one project file in version 1:

```text
<workspace-root>/.arc/project.yml
```

The workspace root is the parent directory of `.arc`. All resource paths and
all local profile files must remain within this root. Nested manifests are not
searched or composed.

Callers may either:

- pass the workspace root and let the API locate `.arc/project.yml`; or
- pass the exact project-file path, from which the root is derived.

The loader must not walk above an explicitly supplied root.

### 8.2 Project schema

The normative version-1 shape is:

```yaml
type: ArcWorkspaceProject
specVersion: "1.0"

workspaceProfiles:
  - id: org.arc.scaffold
    version: "1.0"
    builtin: org.arc.scaffold
    parameters:
      studiesDirectory: studies

  - id: org.example.local-layout
    version: "2.1"
    file: profiles/local-layout.yml
    parameters:
      metadataDirectory: metadata

overrides:
  - rule: org.arc.scaffold#study
    read:
      path: "experiments/{dataset.identifier}/isa.study.xlsx"
    write:
      path: "experiments/{dataset.identifier}/isa.study.xlsx"

  - rule: org.arc.scaffold#run
    enabled: false

rules:
  - id: root-metadata
    contribution: dataset
    codec: arc.yaml.dataset.v1
    target:
      selector: root
    directions: [read, write]
    read:
      path: metadata/root.yml
      required: true
    write:
      path: metadata/root.yml

extensions: {}
```

Top-level fields:

| Field | Required | Meaning |
|---|---:|---|
| `type` | yes | Must be exactly `ArcWorkspaceProject`. |
| `specVersion` | yes | Project schema version; version 1 requires `"1.0"`. |
| `workspaceProfiles` | no | Ordered built-in or local profile references. |
| `overrides` | no | Restricted changes to expanded profile rules. |
| `rules` | no | Project-local rules, ordered as written. |
| `extensions` | no | Explicit storage-schema extension bag. |

At least one enabled rule must remain after compilation.

Unknown fields outside `extensions` are errors. This is intentionally stricter
than model-object `DynamicObj` behavior: misspelled storage instructions must not
be ignored. Extensions are retained by the project parser but are semantically
inactive unless a registered extension handler explicitly understands them.
No extension handler mechanism is required in version 1.

The implementation must publish matching JSON Schema draft 2020-12 artifacts,
expressed in YAML alongside the existing schemas:

```text
schemas/yml/arc-workspace-project.schema.yml
schemas/yml/arc-workspace-profile.schema.yml
```

These schemas cover document structure, required fields, enums, basic scalar
constraints, and `additionalProperties: false` outside explicit extension and
codec-option maps. Runtime compilation remains authoritative for registry,
ownership, path inversion, selector, and cross-rule checks that JSON Schema
cannot express. Keep runtime decoders and schema artifacts under parity tests;
do not add a production schema-validation dependency merely to implement this
design.

### 8.3 Profile references

A profile reference chooses exactly one source:

```yaml
- id: org.arc.scaffold
  version: "1.0"
  builtin: org.arc.scaffold
```

or:

```yaml
- id: org.example.layout
  version: "2.1"
  file: profiles/layout.yml
```

Rules:

- `id` and `version` are required and exact; version ranges are not supported;
- IDs match `[A-Za-z][A-Za-z0-9._-]*`, are at most 128 ASCII
  characters, and must not contain `#`;
- versions are opaque exact-match tokens matching
  `[A-Za-z0-9][A-Za-z0-9._+-]*` with a maximum of 64 ASCII characters;
  version 1 does not order versions or interpret compatibility ranges;
- `builtin` is resolved only through `WorkspaceProfileRegistry`;
- `file` is relative to `.arc`, normalized, and confined to the workspace;
- `builtin` and `file` are mutually exclusive;
- HTTP(S), registry, Git, package-manager, and environment-variable sources are
  invalid;
- the loaded profile's declared ID and version must exactly match the reference;
- duplicate references to the same profile ID are invalid; and
- profile references are expanded in listed order.

### 8.4 Project-local rules

Project rule IDs must be unique within the project and match:

```text
[A-Za-z][A-Za-z0-9._-]*
```

They are limited to 128 ASCII characters.

Their compiled IDs are prefixed with `project#`, for example
`project#root-metadata`. This prevents collisions with profile rules.

### 8.5 No lockfile

Version 1 has no persisted lockfile. Exact profile versions and explicit codec
capability IDs make the configuration reproducible within a known library
version. A `WorkspaceSession` retains the actual compiled plan and load bindings
in memory for update/write operations, but it is not serialized automatically.

## 9. Workspace profiles

### 9.1 Purpose

A workspace profile is analogous to a software project convention or packaging
descriptor: it names a reusable set of rules, parameters, and defaults. It does
not add ARC model types and must not be confused with Process Core, Datamap, or
Administrative model profiles.

### 9.2 Profile schema

```yaml
type: ArcWorkspaceProfile
specVersion: "1.0"
id: org.arc.scaffold
version: "1.0"
description: ARC ISA-XLSX scaffold with adjacent Datamap workbooks

parameters:
  studiesDirectory:
    type: path-segment
    default: studies
  assaysDirectory:
    type: path-segment
    default: assays

rules:
  - id: investigation
    contribution: dataset
    codec: arc.isa.investigation.xlsx.v1
    target:
      selector: root
      additionalType: Investigation
    directions: [read, write]
    read:
      path: isa.investigation.xlsx
      required: true
    write:
      path: isa.investigation.xlsx

  - id: study
    contribution: dataset
    codec: arc.isa.study.xlsx.v1
    target:
      selector: children
      parent: root
      additionalType: Study
    attachTo:
      selector: root
    directions: [read, write]
    read:
      path: "{studiesDirectory}/{dataset.identifier}/isa.study.xlsx"
      cardinality: many
    write:
      path: "{studiesDirectory}/{dataset.identifier}/isa.study.xlsx"

  - id: study-datamap
    contribution: overlay
    facet: arc.datamap
    codec: arc.isa.datamap.xlsx.v1
    target:
      selector: children
      parent: root
      additionalType: Study
    directions: [read, write]
    read:
      path: "{studiesDirectory}/{dataset.identifier}/isa.datamap.xlsx"
      required: false
      cardinality: many
    write:
      path: "{studiesDirectory}/{dataset.identifier}/isa.datamap.xlsx"
      omitWhenEmpty: true

extensions: {}
```

Profile fields follow the same strict unknown-field rule as the project file.
`description` is informational and has no execution effect.

### 9.3 Parameters

Parameters provide simple substitution, not a programming language. A declaration
has:

- `type`: `string`, `path-segment`, `boolean`, or `integer`;
- optional `default`;
- optional finite `allowedValues`; and
- optional `required`, defaulting to `true` when no default exists.

The project supplies parameter values in its profile reference. Compilation:

1. validates supplied parameter names;
2. applies supplied values over defaults;
3. checks types and allowed values;
4. rejects unresolved parameters; and
5. substitutes values before path-template validation.

`path-segment` values must be one non-empty relative segment and must not contain
`/`, `\`, `.` as the entire value, `..`, a drive prefix, a URI scheme, or a NUL
character. General `string` parameters may be used only in codec options, not
directly as path fragments. Parameters cannot refer to other parameters.

### 9.4 Built-in profiles

The first implementation should register at least:

- `org.arc.monolithic-yaml` version `1.0`; and
- `org.arc.scaffold` version `1.0`.

The monolithic profile contains one tree rule for `arc.yml`.

The scaffold profile models the current investigation/study/assay/workflow/run
workbooks plus optional adjacent Datamap overlays. Its actual selectors and
codec capabilities must match the current spreadsheet implementation, including
any profile-specific decoration names. The declarative profile should replace
hardcoded *discovery in the new storage API*, but the old `ScaffoldReader` remains
unchanged for compatibility.

### 9.5 Restricted overrides

An override identifies an expanded rule by qualified ID:

```yaml
overrides:
  - rule: org.arc.scaffold#study
    enabled: true
    read:
      path: "experiments/{dataset.identifier}/study.xlsx"
    write:
      path: "experiments/{dataset.identifier}/study.xlsx"
    codecOptions:
      strict: true
```

Only these properties may be overridden:

- `enabled`;
- `read.path`, `read.required`, and `read.cardinality`;
- `write.path` and `write.omitWhenEmpty`;
- profile parameter values at the profile reference; and
- `codecOptions`.

An override cannot change:

- contribution kind;
- facet;
- codec capability ID;
- target or attachment selector; or
- the rule's stable identity.

To change those semantics, disable the profile rule and add a project-local rule.
Overrides referencing missing or duplicate qualified IDs are fatal errors.

This restriction keeps profile upgrades reviewable and preserves their ownership
claims.

## 10. Storage rules

### 10.1 Common shape

```yaml
- id: assay
  enabled: true
  contribution: dataset
  codec: arc.isa.assay.xlsx.v1
  target:
    selector: descendants
    parent: root
    additionalType: Assay
  attachTo:
    selector: root
  directions: [read, write]
  read:
    path: "metadata/assays/{dataset.identifier}/assay.xlsx"
    required: false
    cardinality: many
  write:
    path: "metadata/assays/{dataset.identifier}/assay.xlsx"
    omitWhenEmpty: false
  codecOptions: {}
  extensions: {}
```

Required common fields:

- `id`;
- `contribution`: `tree`, `dataset`, or `overlay`;
- `codec`;
- `target`;
- `directions`: a non-empty set containing `read`, `write`, or both.

Conditional fields:

- `facet` is required only for `overlay` and forbidden otherwise;
- `attachTo` is required for a read-capable non-root dataset rule unless the
  target supplies an unambiguous parent relationship;
- `read` is required when `directions` contains `read`;
- `write` is required when `directions` contains `write`.

`enabled` defaults to `true`. `codecOptions` defaults to an empty object and is
validated by the selected codec. `extensions` is retained but inactive.

### 10.2 Directionality

Rules may be:

- `directions: [read, write]` — bidirectional;
- `directions: [read]` — import-only; or
- `directions: [write]` — export-only.

Read and write paths are deliberately separate because migration layouts can
import from one location and export canonically to another. A bidirectional rule
does not require textual equality of the two path templates, but both mappings
must identify the same semantic targets.

### 10.3 Target selectors

Version 1 selectors are intentionally limited:

```yaml
target:
  selector: root
```

```yaml
target:
  selector: exact
  identifier: experiment-1
```

```yaml
target:
  selector: children
  parent: root
  additionalType: Study
```

```yaml
target:
  selector: descendants
  parent:
    identifier: investigation-1
  additionalType: Assay
```

Supported selector values:

- `root`;
- `exact`;
- `children`; and
- `descendants`.

Rules:

- an `exact` identifier must be globally unique in the resulting dataset tree;
- `children` selects immediate children of its parent;
- `descendants` selects all descendants of its parent;
- `additionalType`, when present, filters datasets by the stable type/decorated
  type representation already used by ProcessCore;
- a read capture such as `{dataset.identifier}` is used to construct or validate
  the selected dataset;
- a parsed dataset's identifier and additional type must agree with captures and
  selector constraints; and
- selection order is deterministic: parent-before-child, then identifier using
  ordinal comparison.

Version 1 does not define arbitrary predicates, JSONPath, graph queries, regex
selectors, or user code.

### 10.4 Attachment

`attachTo` is meaningful when reading a shallow dataset contribution. It uses the
same limited selector vocabulary but must resolve to exactly one existing parent
for each resource binding.

The root dataset has no `attachTo`. For repeated nested layouts, a path may capture
both parent and child identity:

```yaml
read:
  path: >-
    investigations/{parent.identifier}/studies/{dataset.identifier}/study.yml
```

The compiler verifies that the rule's `attachTo` and template captures can
identify one parent. During execution, a missing or failed parent causes the
dependent resource to be skipped with a diagnostic; it is not attached to the
root as a fallback.

### 10.5 Read settings

```yaml
read:
  path: "studies/{dataset.identifier}/isa.study.xlsx"
  required: false
  cardinality: many
```

- `path` is a safe invertible template;
- `required` defaults to `false`, except a root-forming rule should normally set
  it to `true`;
- `cardinality` is `one` or `many`; it defaults to `one` when the template has no
  model capture and `many` when it does.

For `cardinality: one`, zero matches are an error when required and a recorded
absence otherwise; more than one is always an error. For `many`, zero matches are
allowed unless required; each match becomes an independent resource outcome.

### 10.6 Write settings

```yaml
write:
  path: "studies/{dataset.identifier}/isa.study.xlsx"
  omitWhenEmpty: false
```

`path` must render exactly one output for each selected target. `omitWhenEmpty`
defaults to `false` and is useful primarily for optional overlays. The codec,
not the rule engine, determines whether a typed contribution is empty. If the
codec reports `Omit`, the otherwise expected path becomes a stale candidate.

### 10.7 Codec options

Codec options are declarative scalar/list/map values. Each registered codec
publishes its option validator. Invalid and unknown options are compile errors.
Options cannot contain executable expressions, filesystem callbacks, or a second
path-discovery mechanism.

Examples may include strictness, a worksheet naming convention, or a format
version when those are genuine codec capabilities. A codec must use a stable
capability ID for semantically incompatible versions instead of interpreting an
unbounded `format` string.

## 11. Path-template language

### 11.1 Design constraints

The same rule must support discovery and rendering. Therefore version 1 uses a
small invertible path language, not arbitrary globbing or string interpolation.

A template is a slash-separated sequence of:

- literal path segments;
- already substituted `path-segment` profile parameters; and
- model captures.

Standard captures:

- `{dataset.identifier}`;
- `{parent.identifier}`.

Additional captures must not be added without a well-defined read-to-model and
model-to-write inverse.

### 11.2 Capture grammar

After parameter substitution, each capture occupies a whole path segment:

```text
studies/{dataset.identifier}/isa.study.xlsx
```

The following is invalid in version 1:

```text
studies/study-{dataset.identifier}.xlsx
```

Whole-segment captures avoid ambiguous parsing and escaping. A template may
contain each capture at most once. Captured values must pass the same safe-segment
validation as `path-segment` parameters.

Literal segments are matched exactly. Implementations must document whether the
host filesystem is case-sensitive, but ownership and collision checks should
also compute a case-folded key on Windows and reject two outputs that differ only
by case there.

### 11.3 Discovery

The read planner splits the template into literal and capture segments and walks
only the implied directory positions. It does not expose a general recursive
glob. Each matching file yields:

- normalized relative path;
- captured values;
- compiled rule ID;
- codec ID; and
- provisional semantic target.

Discovery results are sorted by normalized relative path using ordinal
comparison before parsing.

### 11.4 Rendering

The write planner substitutes values from each selected dataset and parent.
Missing, empty, unsafe, or non-scalar values are planning failures. Two bindings
that render to the same normalized path are a fatal output-collision error.

### 11.5 Path safety

All project, profile, read, write, temporary, and deletion paths must:

- be relative to the workspace root;
- use `/` as the manifest separator and be converted at the filesystem boundary;
- reject absolute paths, drive-qualified paths, UNC paths, URI schemes, NULs,
  empty segments, `.` segments, and `..` segments;
- remain under the resolved workspace root after normalization;
- reject traversal through a symbolic link or reparse point that resolves outside
  the workspace;
- treat the manifest and profile files themselves as unmanaged configuration;
- never target the workspace root or a directory for replacement/deletion; and
- identify regular files only for managed-output deletion.

Implement path safety once in a shared helper and use it during compile,
discovery, write staging, replacement, and pruning. Do not rely solely on string
prefix checks.

Unconstrained `*`, `**`, `?`, regexes, environment expansion, command
substitution, Jinja/Liquid templates, and shell syntax are invalid.

## 12. Codec capabilities and registry

### 12.1 Explicit capability IDs

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

### 12.2 Registry

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

### 12.3 Descriptors

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

### 12.4 Logical codec contracts

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

### 12.5 Existing codecs

Adapters may delegate to existing YAML and spreadsheet parsing/writing logic.
However:

- the current recursive YAML encoder is a tree codec;
- a dataset codec must suppress inline child serialization and decoding;
- scaffold workbook codecs should expose one workbook's semantic contribution;
- Datamap should be an overlay codec; and
- existing public APIs and their behavior remain untouched.

## 13. Compiled storage plan

### 13.1 Compilation pipeline

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

### 13.2 Static and concrete plan validation

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

### 13.3 Planned public model

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

### 13.4 Session

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

## 14. Read semantics

### 14.1 High-level algorithm

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

### 14.2 Root construction

A valid read plan must provide exactly one enabled root-forming base rule:

- a tree rule targeting `root`; or
- a dataset rule targeting `root`.

A missing optional root resource still means no ARC can be constructed. Therefore
the compiler should warn when a root read is not marked `required`, and the
executor returns a root-missing error if no root materializes.

The parsed root `Dataset` is promoted or copied into an `ARC` using the same
model-preserving conventions as current loading. No source-layout boolean is set
to represent generalized storage.

### 14.3 Discovery and validation

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

### 14.4 Dependencies and best effort

Independent resource failures do not cancel the entire load. Examples:

- one malformed assay does not prevent other assays from loading;
- an optional missing Datamap is recorded as absent;
- a Datamap parse failure leaves its base dataset intact;
- a failed study causes only resources whose parent/target depends on that study
  to be skipped.

Root absence/failure prevents a usable model and therefore skips all dependent
resources. Plan errors prevent all execution.

Outcomes should distinguish at least:

- `Succeeded`;
- `Absent`;
- `Failed`;
- `SkippedDependency`;
- `SkippedNoTarget`; and
- `Omitted` where relevant to writing.

### 14.5 Attachment

Successful datasets are attached through established ProcessCore graph methods so
that:

- `HasPart` and `PartOf` agree;
- samples, data, and recipes are canonicalized at the root;
- process inputs and outputs point to canonical entities; and
- model invariants remain the same as for programmatically constructed graphs.

The storage layer should not maintain a parallel graph implementation.

## 15. Compatible-union merge of shared entities

### 15.1 Why merge is required

When metadata is partitioned, one sample may occur in multiple dataset resources.
The first resource might provide its name and material type; another might add a
description or annotation. Treating the later occurrence as an error loses valid
information, while blindly overwriting makes results discovery-order dependent.

For `Sample`, `Data`, and `Recipe` objects with the existing canonical key,
version 1 applies a deterministic compatible union.

### 15.2 Scalar fields

For each scalar property:

- canonical empty + incoming non-empty: copy incoming value;
- canonical non-empty + incoming empty: retain canonical value;
- equal non-empty values: retain one value, no warning;
- unequal non-empty values: emit a merge-conflict diagnostic and retain the
  canonical value.

“Empty” follows the property's model semantics: `None`, absent dynamic property,
or equivalent null representation. Empty strings are values unless that model
property already normalizes them as absent.

### 15.3 Collections

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

### 15.4 Dynamic properties

For `DynamicObj` overflow properties:

- a missing canonical key receives the incoming value;
- map-like values merge recursively;
- structurally equal opaque values are accepted;
- unequal opaque values produce a diagnostic and retain the canonical value; and
- arrays/lists use stable union when their values have meaningful equality,
  otherwise an unequal non-empty value is treated as a conflict.

Storage configuration fields are not inserted into model dynamic properties.

### 15.5 Conflict severity

Compatible-union conflicts are resource diagnostics, normally warnings, because
the model remains usable and deterministic. A caller may request a strict policy
that upgrades merge conflicts to errors in the returned result, but the canonical
first value remains unchanged. Strictness is an execution option, not a
project-file expression in version 1.

### 15.6 Writeback consequence

The system does not track which source supplied each field. When writing, the
fully merged canonical `Sample`, `Data`, or `Recipe` is serialized through every
owned dataset representation that references it.

This deliberately favors semantic consistency over exact source fidelity. A
read/write cycle can therefore enrich more than one resource with the merged
description.

## 16. Write semantics

### 16.1 High-level algorithm

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

### 16.2 Canonical rewrite

Writing is a canonical rewrite, not an in-place patch of source syntax.
Consequences:

- YAML comments and formatting may change;
- workbook cell styling or unknown worksheets need not survive;
- collection ordering may become canonical;
- shared merged entities may be repeated in every referencing representation;
- a resource may move from its read path to a distinct canonical write path; and
- the second write of an unchanged model and plan should be byte-stable where
  the underlying codec supports deterministic bytes.

### 16.3 Per-resource atomicity

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

### 16.4 Empty overlays

When `omitWhenEmpty: true`, the overlay codec can return `Omit`. Omission counts
as a successful planned outcome. Its rendered path is excluded from the expected
output set and may be removed as stale only after the whole write phase succeeds.

A base dataset/tree codec should not normally omit its output.

### 16.5 Stale managed outputs

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

### 16.6 Shared entity writeback

Every dataset codec receives the canonical graph view of its selected dataset.
If that dataset references a merged sample/data/recipe, the codec writes the
merged entity. This behavior is required even if the current session originally
loaded only a subset of those fields from that resource.

## 17. Diagnostics and result model

### 17.1 Diagnostic structure

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

### 17.2 Suggested diagnostic codes

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

### 17.3 Load and write results

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

## 18. Public API compatibility and placement

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

## 19. Worked configurations

### 19.1 Existing monolithic `arc.yml`

```yaml
type: ArcWorkspaceProject
specVersion: "1.0"
workspaceProfiles:
  - id: org.arc.monolithic-yaml
    version: "1.0"
    builtin: org.arc.monolithic-yaml
```

Conceptual built-in rule:

```yaml
- id: arc-yaml
  contribution: tree
  codec: arc.yaml.tree.v1
  target:
    selector: root
  directions: [read, write]
  read:
    path: arc.yml
    required: true
  write:
    path: arc.yml
```

This is a tree owner. Adding a per-study base rule under it is an ownership
conflict.

### 19.2 Existing ISA-XLSX scaffold

```yaml
type: ArcWorkspaceProject
specVersion: "1.0"
workspaceProfiles:
  - id: org.arc.scaffold
    version: "1.0"
    builtin: org.arc.scaffold
```

The built-in profile expands to one root investigation dataset rule, repeated
study/assay/workflow/run dataset rules, and optional Datamap overlay rules.
Paths correspond to the current scaffold convention in
[ARC specification.md](https://github.com/HLWeil/ARC-Data-Model/blob/projectfile/references/ARC%20specification.md).

### 19.3 Relocated scaffold through safe overrides

```yaml
type: ArcWorkspaceProject
specVersion: "1.0"
workspaceProfiles:
  - id: org.arc.scaffold
    version: "1.0"
    builtin: org.arc.scaffold

overrides:
  - rule: org.arc.scaffold#investigation
    read:
      path: metadata/investigation.xlsx
    write:
      path: metadata/investigation.xlsx

  - rule: org.arc.scaffold#study
    read:
      path: "metadata/studies/{dataset.identifier}/study.xlsx"
    write:
      path: "metadata/studies/{dataset.identifier}/study.xlsx"
```

The contribution kinds, targets, and codecs remain those of the profile.

### 19.4 Mixed arbitrary layout

```yaml
type: ArcWorkspaceProject
specVersion: "1.0"

rules:
  - id: root
    contribution: dataset
    codec: arc.yaml.dataset.v1
    target:
      selector: root
    directions: [read, write]
    read:
      path: metadata/project.yml
      required: true
    write:
      path: metadata/project.yml

  - id: studies
    contribution: dataset
    codec: arc.yaml.dataset.v1
    target:
      selector: children
      parent: root
      additionalType: Study
    attachTo:
      selector: root
    directions: [read, write]
    read:
      path: "metadata/study-records/{dataset.identifier}/study.yml"
      cardinality: many
    write:
      path: "metadata/study-records/{dataset.identifier}/study.yml"

  - id: assays
    contribution: dataset
    codec: arc.isa.assay.xlsx.v1
    target:
      selector: children
      parent: root
      additionalType: Assay
    attachTo:
      selector: root
    directions: [read, write]
    read:
      path: "lab-workbooks/{dataset.identifier}/assay.xlsx"
      cardinality: many
    write:
      path: "lab-workbooks/{dataset.identifier}/assay.xlsx"

  - id: assay-datamaps
    contribution: overlay
    facet: arc.datamap
    codec: arc.isa.datamap.xlsx.v1
    target:
      selector: children
      parent: root
      additionalType: Assay
    directions: [read, write]
    read:
      path: "contexts/{dataset.identifier}/datamap.xlsx"
      cardinality: many
      required: false
    write:
      path: "contexts/{dataset.identifier}/datamap.xlsx"
      omitWhenEmpty: true
```

This example demonstrates arbitrary local placement, two dataset codecs, a typed
overlay, and one unified in-memory graph.

### 19.5 Import old, write new

```yaml
- id: migrated-studies
  contribution: dataset
  codec: arc.yaml.dataset.v1
  target:
    selector: children
    parent: root
    additionalType: Study
  attachTo:
    selector: root
  directions: [read, write]
  read:
    path: "legacy/{dataset.identifier}/metadata.yml"
    cardinality: many
  write:
    path: "metadata/studies/{dataset.identifier}/study.yml"
```

After a successful write, legacy paths are not stale because stale discovery is
based on current **write** templates. Migration cleanup is separate.

When the format also changes, use two direction-specific owners:

```yaml
- id: import-legacy-studies
  contribution: dataset
  codec: arc.isa.study.xlsx.v1
  target:
    selector: children
    parent: root
    additionalType: Study
  attachTo:
    selector: root
  directions: [read]
  read:
    path: "legacy/{dataset.identifier}/isa.study.xlsx"
    cardinality: many

- id: export-canonical-studies
  contribution: dataset
  codec: arc.yaml.dataset.v1
  target:
    selector: children
    parent: root
    additionalType: Study
  directions: [write]
  write:
    path: "metadata/studies/{dataset.identifier}/study.yml"
```

These rules do not conflict because each direction still has exactly one owner.

### 19.6 Local workspace profile

Project:

```yaml
type: ArcWorkspaceProject
specVersion: "1.0"
workspaceProfiles:
  - id: org.example.instrument-layout
    version: "1.3"
    file: profiles/instrument-layout.yml
    parameters:
      metadataDirectory: instrument-metadata
```

Profile path:

```text
.arc/profiles/instrument-layout.yml
```

The profile remains declarative and can use only codecs already registered by
the embedding application.

## 20. Validation laws and invariants

### 20.1 Semantic round trip

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

### 20.2 Canonical write stability

For deterministic codecs:

```text
write(P, read(P, write(P, M))) = write(P, M)
```

at the byte level after the first canonical rewrite, excluding timestamps or
other explicitly documented nondeterministic codec output. Built-in codecs should
avoid nondeterministic output.

### 20.3 Ownership invariant

Every dataset base and every typed facet in the writable model has zero or one
owner. Zero means intentionally not persisted by this plan; more than one is a
compile error.

### 20.4 Path invariant

Every discovered, rendered, replaced, or deleted resource resolves to a regular
file path under the workspace root and belongs to the rule reported for it.

### 20.5 Failure invariant

A resource failure:

- does not partially apply an overlay;
- does not attach a partially parsed dataset;
- does not truncate its existing write target;
- does not trigger stale deletion; and
- affects independent resources only through explicit target/dependency
  relationships.

## 21. Implementation sequence

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
- the three representative example projects from section 19;
- public API usage documentation;
- schema reference documentation;
- migration guidance emphasizing canonical rewrite; and
- runtime-support notes.

## 22. Test plan and acceptance criteria

### 22.1 Project and profile schema

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

### 22.2 Path templates and security

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

### 22.3 Ownership and dependencies

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

### 22.4 Read execution

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

### 22.5 Compatible union

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

### 22.6 Write execution

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

### 22.7 Runtime parity

Run storage tests on:

- .NET; and
- Fable JavaScript on Node.

The first implementation is accepted without Fable Python support, but must not
silently claim it. Add Python only after path, filesystem replacement, and
spreadsheet support are explicitly implemented and tested there.

### 22.8 Repository verification

At implementation completion, run at least:

```powershell
.\build.cmd BuildSolution
.\build.cmd RunTests
.\build.cmd TestJs
```

Use `.\build.cmd RunTestsAll` when Python inclusion is added or when shared
changes can affect existing Python output.

## 23. Migration and compatibility notes

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

## 24. Deferred extensions

These can be layered on later without changing the version-1 core:

- a persisted lock/provenance file for exact source attribution and safer profile
  migration cleanup;
- remote, signed, or package-distributed workspace profiles;
- content hashes and integrity manifests;
- multiple roots or mounted subprojects;
- richer selectors based on a constrained graph-query language;
- read-only wildcard/glob rules that explicitly give up invertible writing;
- transactional multi-file commit through a generation directory;
- additional typed overlays;
- explicit scientific-payload packaging rules separate from metadata storage;
- additional codecs such as RO-Crate or database-backed resources;
- Python runtime support; and
- generated editor completions and language-server integration based on the
  version-1 JSON Schema artifacts.

Each extension should preserve explicit codec selection, confinement, ownership,
determinism, and the distinction between plan errors and resource errors.

## 25. Prior art and design rationale

No single existing standard solves ARC's full bidirectional graph-partitioning
problem. The design combines ideas from several families while avoiding their
scope mismatches.

### 25.1 Data and research packaging

- [Frictionless Data Package](https://specs.frictionlessdata.io/data-package/)
  demonstrates a small root descriptor listing resources.
- [Frictionless Data Resource](https://specs.frictionlessdata.io/data-resource/)
  separates resource location, media/format information, and schema metadata.
- [Frictionless Profiles](https://specs.frictionlessdata.io/profiles/) and
  [Patterns](https://specs.frictionlessdata.io/patterns/) show how a base
  descriptor can be constrained and reused. ARC borrows the manifest/profile
  split but uses explicit registered codec capabilities and model targets.
- [RO-Crate 1.2](https://www.researchobject.org/ro-crate/specification/1.2/)
  models a research object as a root data entity plus contextual entities.
  [RO-Crate data entities](https://www.researchobject.org/ro-crate/specification/1.2/data-entities.html)
  are useful precedent for distinguishing described payload from metadata, and
  [RO-Crate profiles](https://www.researchobject.org/ro-crate/specification/1.2/profiles.html)
  inform reusable conformance conventions.
- Repository-local mappings in [ISA RO-Crate](../references/isa_ro_crate.md),
  [Workflow Run RO-Crate](../references/arc_wr_ro_crate.md), and
  [Datamap RO-Crate](../references/arc_datamap_ro_crate.md) are relevant future
  codec/profile inputs.
- [BagIt, RFC 8493](https://datatracker.ietf.org/doc/html/rfc8493) separates
  payload, tag metadata, manifests, and integrity. Its safety/inventory ideas are
  useful, but BagIt describes transfer packages rather than bidirectional model
  partitioning.
- [METS overview](https://www.loc.gov/standards/mets/METSOverview.v3_en.html) and
  [METS schema documentation](https://www.loc.gov/standards/mets/docs/mets.v1-9.html)
  demonstrate structural maps connecting logical divisions with physical files.
  ARC similarly needs an explicit logical-to-physical map, but with a much
  smaller, domain-specific rule language.

### 25.2 Profile and conformance mechanisms

- [W3C Profiles Vocabulary](https://www.w3.org/TR/dx-prof/) supplies vocabulary
  for resources conforming to profiles and profile artifacts.
- [RFC 6906, the `profile` link relation](https://datatracker.ietf.org/doc/html/rfc6906)
  distinguishes a profile from a new media type. This supports keeping workspace
  convention, model profile, and codec format as separate concerns.

Version 1 intentionally uses exact local/built-in profile references instead of
general web profile negotiation.

### 25.3 Software project and packaging rules

- [Git attributes](https://git-scm.com/docs/gitattributes) illustrates ordered,
  path-scoped rules with well-defined precedence. ARC borrows stable rule order
  but avoids negation-heavy glob semantics because writable mappings must invert.
- [MSBuild items](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-items)
  and [MSBuild include/exclude
  behavior](https://learn.microsoft.com/en-us/visualstudio/msbuild/how-to-exclude-files-from-the-build)
  show declarative file sets and project-local overrides.
- [Maven Assembly descriptors](https://maven.apache.org/plugins/maven-assembly-plugin/assembly.html)
  and the [Maven Assembly Plugin
  introduction](https://maven.apache.org/plugins/maven-assembly-plugin/)
  show reusable packaging layouts, file sets, and output mappings.
- [Cargo manifests](https://doc.rust-lang.org/cargo/reference/manifest.html)
  combine a conventional project file with explicit targets and include/exclude
  controls. They are useful precedent for one root project descriptor with
  versioned, strictly named configuration.
- [npm `package.json`](https://docs.npmjs.com/cli/v11/configuring-npm/package-json)
  illustrates conventional project discovery, package-local paths, workspaces,
  and a declarative boundary between metadata and executable tooling.
- [Kustomize's `kustomization`
  file](https://kubectl.docs.kubernetes.io/references/kustomize/kustomization/)
  demonstrates reusable bases plus local overlays. ARC adopts the
  profile-plus-restricted-override shape, while intentionally avoiding general
  structural patching of rules.
- The [OCI image descriptor](https://github.com/opencontainers/image-spec/blob/main/descriptor.md)
  and [OCI image manifest](https://github.com/opencontainers/image-spec/blob/main/manifest.md)
  demonstrate stable media/capability identifiers and descriptors that refer to
  separately stored content. ARC does not adopt OCI's immutable/content-addressed
  assumptions, but benefits from explicit resource capabilities.

### 25.4 Data catalogs, parser registries, and format adapters

- [Intake catalogs](https://intake.readthedocs.io/en/stable/catalog.html) map
  named data sources to driver/plugin configuration.
- [Intake plugin
  authoring](https://intake.readthedocs.io/en/latest/making-plugins.html) is useful
  precedent for separating declarative catalogs from executable drivers.
- [Apache Tika parser interfaces](https://tika.apache.org/3.2.3/parser.html)
  demonstrate a registry of parsers with supported media types. ARC uses a
  registry but refuses parser inference to keep project behavior deterministic.
- [Common Workflow Language 1.2](https://www.commonwl.org/v1.2/) separates
  declarative portable documents from registered tool/runtime behavior.
- [CSV on the Web metadata](https://www.w3.org/TR/tabular-metadata/) shows how
  external metadata can describe tabular resources and dialects.
- [Filesystem Spec](https://filesystem-spec.readthedocs.io/en/latest/) shows a
  portable interface between higher-level data tooling and storage backends.
  Version 1 remains local-only, but should similarly keep path planning above
  runtime-specific filesystem operations.

### 25.5 Partitioned dataset/table systems

- [Apache Arrow Dataset](https://arrow.apache.org/docs/python/dataset.html)
  combines multiple physical fragments into one logical dataset and uses
  partition information during discovery.
- [Apache Iceberg specification](https://iceberg.apache.org/spec/) and
  [Iceberg partitioning](https://iceberg.apache.org/docs/latest/partitioning/)
  separate logical tables from physical partition layout and metadata evolution.
- [STAC specification](https://stacspec.org/en/about/specification/) demonstrates
  catalogs, collections, items, and linked assets across a hierarchy.
- [DVC metafiles](https://dvc.org/doc/user-guide/project-structure/dvc-files)
  demonstrate small project-controlled descriptors that connect logical data
  artifacts to external or generated file trees. ARC differs by materializing a
  typed metadata graph rather than tracking payload hashes.

These systems validate the logical/physical separation and deterministic
partition discovery, but they generally partition homogeneous data fragments,
not heterogeneous ARC dataset/overlay contributions.

### 25.6 Digital repositories and resource maps

- [OAI-ORE Primer](https://www.openarchives.org/ore/1.0/primer) describes Resource
  Maps that enumerate and relate the resources making up an aggregation. This is
  a close metadata analogue to a root storage manifest, although it does not
  prescribe ARC-specific parsers or inverse writes.
- [DCAT 3](https://www.w3.org/TR/vocab-dcat-3/) separates a logical dataset from
  one or more distributions. ARC's codec-bound resources are more operational
  than DCAT distributions, but the logical-versus-representation distinction is
  the same.
- [Oxford Common File Layout 1.1](https://ocfl.io/1.1/spec/) provides strong
  inventory, path, integrity, and versioning rules for repository objects. Its
  safe-inventory principles inform future lock/integrity extensions; adopting
  OCFL's preservation/version store is outside this proposal.

### 25.7 Bidirectional transformations

- [Boomerang](https://www.cs.cornell.edu/~jnfoster/papers/boomerang.pdf) and
  [invertible syntax
  descriptions](https://www.informatik.uni-marburg.de/~rendel/unparse/) motivate
  using one restricted mapping that can both parse paths and render them.
- [Contract lenses: reasoning about bidirectional programs via
  calculation](https://www.cambridge.org/core/journals/journal-of-functional-programming/article/contract-lenses-reasoning-about-bidirectional-programs-via-calculation/43F612938DAA399A9D35193FB6278F56)
  gives the broader theoretical context for round-trip laws and explicit
  reconciliation policies.

The project-file language does not attempt to implement general lenses. It uses
their practical lesson: arbitrary read globs plus unrelated write templates do
not constitute a reliable bidirectional mapping.

### 25.8 Selectors and fragments

- [RFC 7111](https://datatracker.ietf.org/doc/html/rfc7111) is relevant to the
  repository's existing fragment-selector rationale, but selectors inside
  scientific payloads remain model data rather than storage-resource routing.

## 26. Repository references

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

## 27. Final decisions recorded by this plan

For avoidance of ambiguity, implementation must use these decisions unless a
later design change explicitly supersedes them:

1. The physical partition unit is a dataset, a dataset tree, or a typed overlay;
   arbitrary field partitioning is deferred.
2. Writes are canonical rewrites, not exact source-preserving edits.
3. Only ARC metadata resources are managed; scientific payload bytes are
   referenced but not partitioned.
4. Workspace profiles expand first; project overrides and rules are normalized
   afterward.
5. Dataset base and facet ownership are exclusive per direction; one read owner
   and a different write owner may coexist.
6. Version 1 has one root `.arc/project.yml`.
7. Workspace profiles are built-in or confined local declarative files, never
   network resources.
8. New APIs are parallel to and do not replace the current ARC I/O APIs.
9. Rules can be read/write, import-only, or export-only.
10. Resource execution is best effort, but project/plan errors are fatal.
11. Paths are safe relative paths inside the local workspace.
12. YAML rules target technical users and are strictly schema validated.
13. Initial runtime support is .NET and Node.js; Python is deferred.
14. Equal-key shared entities merge by compatible union.
15. Canonical merged entities are written through every referencing dataset
    representation.
16. Reusable rule sets are called workspace profiles.
17. There is no version-1 lockfile; sessions retain bindings only in memory.
18. Codec capability IDs are explicit; format/media metadata never selects a
    parser.
19. Stale managed outputs are deleted only after every planned write succeeds.
20. Only files matched by current writable rules are eligible for stale deletion.
