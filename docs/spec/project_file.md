---
title: ARC Workspace Project File
category: Specification
categoryindex: 3
index: 5
---

# ARC Workspace Project File

## Status and scope

This document specifies version 1.0 of the ARC workspace project file and its
workspace-profile language.

The project file describes how ARC metadata resources on disk map to the unified
ARC Data Model. It is not a serialization of an `ARC` or `Dataset`. Instead, it
selects datasets and typed facets, assigns their storage ownership, locates their
physical resources, and names the registered codecs that read or write those
resources.

The key words **MUST**, **MUST NOT**, **REQUIRED**, **SHALL**, **SHALL NOT**,
**SHOULD**, **SHOULD NOT**, **RECOMMENDED**, **NOT RECOMMENDED**, **MAY**, and
**OPTIONAL** in this document are to be interpreted as described in
[BCP 14](https://www.rfc-editor.org/info/bcp14) when, and only when, they appear
in all capitals.

The non-normative design background and implementation plan are in
[plans/project_file.md](../../plans/project_file.md).

## 1. Model

An ARC workspace is modeled as:

```text
physical metadata resources
        <=>
registered codecs
        <=>
dataset trees, shallow datasets, and typed overlays
        <=>
one unified in-memory ARC graph
```

The project file applies to metadata represented by the [ARC Data
Model](index.md), including [Process Core](process_core/overview.md),
[Datamap](datamap/overview.md), [Administrative](administrative/overview.md), and
their [decorations](decorations/overview.md).

Scientific payloads referenced by [`Data`](process_core/Data.md), such as CSV,
Parquet, image, mass-spectrometry, or sequencing files, are not metadata
resources managed by this specification. A project processor MUST NOT rewrite or
delete such payloads merely because they are referenced by the ARC graph.

### 1.1 Terms

**Workspace root**
: The local directory containing the `.arc` directory.

**Project file**
: The root storage configuration at `.arc/project.yml`.

**Workspace profile**
: A reusable declarative set of storage rules. A workspace profile is distinct
  from an ARC model profile such as Process Core or Datamap.

**Rule**
: A declarative mapping between model targets, contribution kind, direction,
  path template, and codec capability.

**Codec**
: A processor capability registered by the embedding application that converts
  between one physical metadata resource and one typed contribution.

**Tree contribution**
: One resource containing a dataset and a recursively embedded dataset subtree.

**Dataset contribution**
: One resource containing exactly one shallow dataset. Parent-child attachment is
  defined by the rule and not by an embedded `hasPart` subtree.

**Overlay contribution**
: One resource containing a named detachable facet of an existing dataset.

**Facet**
: A named unit of storage ownership within a dataset.

**Binding**
: The resolved association among a compiled rule, a resource path, path captures,
  and a semantic model target.

**Managed output**
: A regular file matched by a currently enabled writable rule.

**Workspace session**
: In-memory state retaining a compiled plan, ARC graph, bindings, outcomes, and
  diagnostics for a load/update operation.

### 1.2 Standard facets

Version 1.0 defines these facets:

| Facet | Meaning |
|---|---|
| `arc.base` | Dataset identity, descriptive and administrative fields, processes, data references, and decoration fields not assigned to another detachable facet |
| `arc.datamap` | The dataset's `DataContext` state |

A tree or dataset rule always owns `arc.base` for its selected target in each
declared direction. A codec MAY declare additional facets that its physical
representation owns. An overlay rule owns exactly the facet named by its
`facet` field.

A codec MUST NOT apply or emit a detachable facet that the compiled rule does not
own. A base-only codec encountering non-empty data for an unowned standardized
facet MUST fail that resource rather than silently discard it.

Shared `Sample`, `Data`, and `Recipe` objects form the reference closure required
by a base representation. They are reconciled by canonical identity as specified
in section 14 and are not independently assignable facets.

## 2. Conformance

This specification defines four conformance classes.

### 2.1 Project-document conformance

A conforming project document:

- is valid YAML;
- has `type: ArcWorkspaceProject`;
- has `specVersion: "1.0"`;
- satisfies the structural requirements in this specification; and
- compiles without project, profile, rule, path, ownership, or codec errors
  against the registries supplied to the processor.

Structural validity alone does not imply that all referenced resources exist.

### 2.2 Workspace-profile conformance

A conforming workspace profile:

- is valid YAML;
- has `type: ArcWorkspaceProfile`;
- has `specVersion: "1.0"`;
- declares an exact ID and version;
- satisfies its parameter and rule requirements; and
- compiles without errors when referenced by a project and supplied with a
  compatible codec registry.

### 2.3 Processor conformance

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

### 2.4 Codec conformance

A conforming codec MUST publish a descriptor and MUST obey the contribution,
facet, direction, transactionality, and diagnostic contracts in section 11.

## 3. Workspace and project discovery

### 3.1 Canonical location

Version 1.0 defines exactly one project file:

```text
<workspace-root>/.arc/project.yml
```

The workspace root is the parent of `.arc`.

A caller MAY supply:

- the workspace root, in which case the processor resolves
  `.arc/project.yml`; or
- the exact project-file path, in which case the processor derives the root.

A processor MUST NOT search above an explicitly supplied workspace root. Nested
project files and mounted subprojects are not defined by version 1.0.

### 3.2 Path domain

All metadata resources and local profiles referenced by the project MUST be
inside the workspace root. All manifest paths use `/` as their separator,
regardless of the host operating system.

Only local filesystem resources are supported. HTTP, HTTPS, Git, package
registry, object-store, and other remote sources are not conforming version-1.0
project sources.

### 3.3 Configuration ownership

The project file and its local profile documents are configuration, not managed
metadata outputs. A storage writer:

- MUST NOT rewrite them implicitly;
- MUST NOT treat them as stale outputs; and
- MUST NOT delete them.

## 4. YAML data model

### 4.1 General rules

Project and workspace-profile files MUST use YAML mappings, sequences, strings,
booleans, integers, and null where allowed by the field definition.

Field names are case-sensitive.

Unknown fields MUST be rejected except within:

- `extensions`; and
- `codecOptions`.

An `extensions` mapping is retained by the decoder but has no version-1.0
execution semantics. A processor MUST NOT execute extension content as code.

Duplicate YAML mapping keys MUST be rejected.

### 4.2 Identifiers

Project-local rule IDs, profile IDs, profile rule IDs, and parameter names are
ASCII strings.

Profile and rule IDs:

```text
[A-Za-z][A-Za-z0-9._-]*
```

They:

- MUST contain at most 128 characters;
- MUST NOT contain `#`; and
- MUST be unique in their declared scope.

Parameter names:

```text
[A-Za-z][A-Za-z0-9_-]*
```

Dots are reserved in capture names and are not allowed in parameter names.

### 4.3 Versions

`specVersion` is a specification discriminator and MUST be exactly the quoted
string `"1.0"`.

A workspace-profile `version` is an opaque exact-match token:

```text
[A-Za-z0-9][A-Za-z0-9._+-]*
```

It MUST contain at most 64 ASCII characters. Version 1.0 does not define version
ranges, ordering, compatibility resolution, or network lookup.

### 4.4 Schema artifacts

The repository SHOULD publish JSON Schema draft 2020-12 representations at:

```text
schemas/yml/arc-workspace-project.schema.yml
schemas/yml/arc-workspace-profile.schema.yml
```

Those schemas describe structural constraints. Runtime compilation remains
authoritative for registry lookup, path inversion, ownership, selector, and
cross-rule constraints.

## 5. Project document

### 5.1 Shape

```yaml
type: ArcWorkspaceProject
specVersion: "1.0"

workspaceProfiles: []
overrides: []
rules: []
extensions: {}
```

Fields:

| Field | Type | Required | Default |
|---|---|---:|---|
| `type` | string | yes | none |
| `specVersion` | string | yes | none |
| `workspaceProfiles` | sequence of profile references | no | empty |
| `overrides` | sequence of rule overrides | no | empty |
| `rules` | sequence of storage rules | no | empty |
| `extensions` | mapping | no | empty |

After profile expansion, overrides, and disabled-rule removal, at least one rule
MUST remain enabled.

### 5.2 Profile references

A profile reference has this common form:

```yaml
- id: org.arc.scaffold
  version: "1.0"
  builtin: org.arc.scaffold
  parameters: {}
```

or:

```yaml
- id: org.example.layout
  version: "2.1"
  file: profiles/layout.yml
  parameters: {}
```

Fields:

| Field | Type | Required | Meaning |
|---|---|---:|---|
| `id` | profile ID | yes | Expected profile ID |
| `version` | version token | yes | Expected exact profile version |
| `builtin` | profile ID | conditional | Built-in registry key |
| `file` | safe relative path | conditional | Local profile path relative to `.arc` |
| `parameters` | mapping | no | Values supplied to declared profile parameters |
| `extensions` | mapping | no | Inactive extension data |

Exactly one of `builtin` or `file` MUST be present.

For a built-in reference, the processor MUST resolve `builtin` through the
supplied workspace-profile registry. For a local reference, the processor MUST
resolve `file` relative to `.arc` and enforce the path rules in section 10.

The loaded profile's `id` and `version` MUST exactly equal the reference's `id`
and `version`. The same profile ID MUST NOT be referenced more than once.

Profile references are expanded in sequence order.

### 5.3 Rule overrides

An override identifies an expanded profile rule by qualified ID:

```yaml
- rule: org.arc.scaffold#study
  enabled: true
  read:
    path: "experiments/{dataset.identifier}/isa.study.xlsx"
    required: false
    cardinality: many
  write:
    path: "experiments/{dataset.identifier}/isa.study.xlsx"
    omitWhenEmpty: false
  codecOptions:
    strict: true
```

Allowed fields:

| Field | Type |
|---|---|
| `rule` | qualified rule ID |
| `enabled` | boolean |
| `read.path` | path template |
| `read.required` | boolean |
| `read.cardinality` | `one` or `many` |
| `write.path` | path template |
| `write.omitWhenEmpty` | boolean |
| `codecOptions` | mapping |
| `extensions` | mapping |

An override MUST NOT change:

- rule identity;
- contribution kind;
- facet;
- codec capability ID;
- directions;
- target selector; or
- attachment selector.

To change those semantics, a project MUST disable the profile rule and add a
project-local rule.

An override referencing no rule or more than one rule is a compile error.
Overrides are fieldwise patches: an omitted nested field retains the expanded
profile value rather than resetting its containing block. Overrides are applied
in listed order. A later override MAY replace a property set by an earlier
override for the same rule.

### 5.4 Project-local rules

Project-local rules are defined by the `rules` sequence. Their IDs are unique
within the project. During compilation, the processor qualifies each ID as:

```text
project#<rule-id>
```

Profile rule IDs are qualified as:

```text
<profile-id>#<rule-id>
```

Qualified IDs are the stable identifiers reported in plans, bindings, outcomes,
and diagnostics.

### 5.5 No lockfile

Version 1.0 does not define a persisted lockfile or per-field provenance file.
An implementation MAY retain compiled rules and bindings in a workspace session,
but MUST NOT serialize such state as a normative project artifact without an
explicit future specification.

## 6. Workspace profiles

### 6.1 Shape

```yaml
type: ArcWorkspaceProfile
specVersion: "1.0"
id: org.example.layout
version: "1.0"
description: Example reusable ARC metadata layout

parameters: {}
rules: []
extensions: {}
```

Fields:

| Field | Type | Required | Default |
|---|---|---:|---|
| `type` | string | yes | none |
| `specVersion` | string | yes | none |
| `id` | profile ID | yes | none |
| `version` | version token | yes | none |
| `description` | string | no | none |
| `parameters` | mapping of parameter declarations | no | empty |
| `rules` | sequence of storage rules | yes | none |
| `extensions` | mapping | no | empty |

A workspace profile MUST contain at least one rule before project overrides are
applied.

### 6.2 Parameter declarations

Example:

```yaml
parameters:
  studiesDirectory:
    type: path-segment
    default: studies

  strictWorkbooks:
    type: boolean
    default: true

  dialect:
    type: string
    required: true
    allowedValues: [standard, legacy]
```

Parameter declaration fields:

| Field | Type | Required | Meaning |
|---|---|---:|---|
| `type` | `string`, `path-segment`, `boolean`, or `integer` | yes | Value type |
| `default` | matching scalar | no | Default value |
| `required` | boolean | no | Defaults to `true` when no default exists |
| `allowedValues` | sequence of matching scalars | no | Finite accepted values |
| `description` | string | no | Informational text |
| `extensions` | mapping | no | Inactive extension data |

A declaration MUST NOT set `required: true` and `default: null`.

The compiler:

1. rejects supplied parameter names not declared by the profile;
2. applies supplied values over defaults;
3. validates type and `allowedValues`;
4. rejects unresolved required values; and
5. substitutes resolved values before path-template compilation and codec-option
   validation.

Parameters MUST NOT refer to other parameters.

A `path-segment` value MUST be one non-empty safe path segment. It MUST NOT
contain `/`, `\`, NUL, a URI scheme, or a drive prefix, and MUST NOT equal `.`
or `..`.

A `string` parameter MAY appear in codec options but MUST NOT be substituted into
a path template. Only `path-segment` parameters can form path segments.

## 7. Storage rules

### 7.1 Shape

```yaml
- id: study
  enabled: true
  contribution: dataset
  codec: arc.isa.study.xlsx.v1
  target:
    selector: children
    parent: root
    additionalType: Study
  attachTo: root
  directions: [read, write]
  read:
    path: "studies/{dataset.identifier}/isa.study.xlsx"
    required: false
    cardinality: many
  write:
    path: "studies/{dataset.identifier}/isa.study.xlsx"
    omitWhenEmpty: false
  codecOptions: {}
  extensions: {}
```

Common fields:

| Field | Type | Required | Default |
|---|---|---:|---|
| `id` | rule ID | yes | none |
| `enabled` | boolean | no | `true` |
| `contribution` | `tree`, `dataset`, or `overlay` | yes | none |
| `facet` | facet ID | overlay only | none |
| `codec` | codec capability ID | yes | none |
| `target` | target selector | yes | none |
| `attachTo` | parent reference | conditional | none |
| `directions` | non-empty unique sequence of `read` and/or `write` | yes | none |
| `read` | read settings | when readable | none |
| `write` | write settings | when writable | none |
| `codecOptions` | mapping | no | empty |
| `extensions` | mapping | no | empty |

`facet` is REQUIRED for an overlay and MUST NOT be present for tree or dataset
contributions.

`read` MUST be present if and only if `directions` contains `read`. `write` MUST
be present if and only if `directions` contains `write`.

### 7.2 Directionality

A rule is:

- bidirectional with `directions: [read, write]`;
- import-only with `directions: [read]`; or
- export-only with `directions: [write]`.

Sequence order does not change semantics.

A rule MAY use distinct read and write paths. An import-only owner and an
export-only owner MAY target the same dataset or facet through different codecs,
because ownership is direction-specific.

### 7.3 Tree rules

A tree rule maps one resource to a selected dataset and its recursively embedded
descendants.

A tree rule owns `arc.base` and every additional descriptor-declared facet
throughout the selected subtree in each declared direction.

In a given direction, a tree owner MUST NOT overlap:

- another tree owner;
- a dataset owner for its root; or
- a dataset owner for any descendant in its owned subtree.

An overlay MAY coexist only for a facet not owned by the tree codec in that
direction.

### 7.4 Dataset rules

A dataset rule maps one resource to exactly one shallow dataset.

On read, a dataset codec MUST reject a contribution with non-empty inline child
datasets. Hierarchy is established by `attachTo` or the target's parent
reference.

A non-root readable dataset rule MUST identify exactly one parent for each
binding.

### 7.5 Overlay rules

An overlay rule maps one resource to the named `facet` of an existing target
dataset.

The base dataset MUST exist before the overlay can be applied. An overlay parse
failure MUST leave the target dataset unchanged.

The version-1 standard overlay facet is:

```yaml
facet: arc.datamap
```

Other facets require an explicitly registered codec descriptor and stable facet
ID.

### 7.6 Read settings

```yaml
read:
  path: "studies/{dataset.identifier}/isa.study.xlsx"
  required: false
  cardinality: many
```

Fields:

| Field | Type | Required | Default |
|---|---|---:|---|
| `path` | path template | yes | none |
| `required` | boolean | no | `false` |
| `cardinality` | `one` or `many` | no | inferred as below |
| `extensions` | mapping | no | empty |

When omitted, `cardinality` defaults to:

- `one` when the path has no model capture; and
- `many` when the path has a model capture.

For `one`:

- zero matches is an error when `required` is true;
- zero matches is an `Absent` outcome otherwise; and
- more than one match is always an error.

For `many`, zero matches is an error only when `required` is true. Every match is
an independent binding and resource outcome.

### 7.7 Write settings

```yaml
write:
  path: "studies/{dataset.identifier}/isa.study.xlsx"
  omitWhenEmpty: false
```

Fields:

| Field | Type | Required | Default |
|---|---|---:|---|
| `path` | path template | yes | none |
| `omitWhenEmpty` | boolean | no | `false` |
| `extensions` | mapping | no | empty |

The path MUST render exactly one output for every selected target.

`omitWhenEmpty` SHOULD be used only for optional overlays. The codec determines
whether its typed contribution is empty. A codec returning `Omit` creates no
output and makes an existing matching output a stale candidate after a fully
successful write.

### 7.8 Codec options

`codecOptions` contains declarative values interpreted by the registered codec.
The codec MUST strictly validate its option names and values during compilation.

Codec options:

- MUST NOT define another resource-discovery mechanism;
- MUST NOT contain executable scripts, expressions, callbacks, or shell
  commands; and
- MUST NOT change a rule's contribution kind, target, facet, or ownership.

Semantically incompatible format versions SHOULD use distinct capability IDs
rather than an unconstrained option value.

## 8. Target and parent selectors

### 8.1 Root selector

```yaml
target:
  selector: root
```

`root` selects the workspace ARC's root dataset.

### 8.2 Exact selector

```yaml
target:
  selector: exact
  identifier: experiment-1
```

`exact` selects the dataset with the exact `identifier`. The identifier MUST be
globally unique in the resulting dataset tree.

### 8.3 Children selector

```yaml
target:
  selector: children
  parent: root
  additionalType: Study
```

`children` selects immediate children of the specified parent, optionally
filtered by exact case-sensitive `additionalType`.

### 8.4 Descendants selector

```yaml
target:
  selector: descendants
  parent:
    identifier: investigation-1
  additionalType: Assay
```

`descendants` selects every descendant of the specified parent, optionally
filtered by exact case-sensitive `additionalType`.

Selection order is parent-before-child and then by identifier using ordinal
comparison.

### 8.5 Parent references

A parent reference is one of:

```yaml
parent: root
```

```yaml
parent:
  identifier: investigation-1
```

```yaml
parent:
  capture: parent.identifier
```

The capture form is valid only when the rule's path contains
`{parent.identifier}`.

`attachTo` uses the same parent-reference form:

```yaml
attachTo:
  capture: parent.identifier
```

For a readable shallow dataset, the resolved parent reference MUST identify
exactly one successfully materialized dataset. A missing or failed parent causes
the dependent resource to be skipped; the processor MUST NOT attach it to the
root as a fallback.

### 8.6 Selector validation

After parsing a dataset contribution:

- its identifier MUST equal `{dataset.identifier}` when that capture is present;
- its parent MUST equal `{parent.identifier}` when that capture is present;
- its `additionalType` MUST satisfy the target filter when present; and
- its hierarchy MUST satisfy `children` or `descendants`.

A mismatch fails the binding. A processor MUST NOT silently rename or retarget
the parsed dataset.

Arbitrary predicates, graph-query expressions, JSONPath, regular expressions,
and executable selector code are not defined by version 1.0.

## 9. Parameters and path templates

### 9.1 Processing order

A compiler processes a path in this order:

1. substitute declared `path-segment` profile parameters;
2. parse model captures;
3. validate literals and captures;
4. compile a read matcher and/or write renderer; and
5. normalize at the host-filesystem boundary.

### 9.2 Template grammar

A path template is a `/`-separated sequence of whole segments. Each segment is
one of:

- a literal;
- one profile parameter reference such as `{studiesDirectory}`; or
- one model capture.

Version 1.0 defines:

```text
{dataset.identifier}
{parent.identifier}
```

After parameter substitution, a capture MUST occupy its entire segment:

```text
studies/{dataset.identifier}/isa.study.xlsx
```

This is not conforming:

```text
studies/study-{dataset.identifier}.xlsx
```

Each capture MAY occur at most once in one template.

### 9.3 Read matching

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

### 9.4 Write rendering

A write planner substitutes captures from the selected dataset and parent.
Missing, empty, or unsafe capture values are planning errors.

Two bindings in the same write plan MUST NOT render to the same normalized path.
The processor MUST detect all rendered collisions before invoking a writer.

### 9.5 Unsupported syntax

Version 1.0 does not support:

- `*`, `**`, `?`, or character-class globs;
- regular expressions;
- environment-variable expansion;
- command or shell substitution;
- URI-template expansion;
- Jinja, Liquid, or other general template languages; or
- partial-segment captures.

## 10. Path safety

Every project, profile, read, write, temporary, replacement, and deletion path
MUST:

- be relative to its specified base;
- reject absolute paths;
- reject drive-qualified and UNC paths;
- reject URI schemes;
- reject NUL;
- reject empty, `.`, and `..` segments;
- remain within the resolved workspace root after normalization;
- reject traversal through a symbolic link or reparse point that resolves
  outside the workspace; and
- identify a regular file whenever replacement or deletion is attempted.

Manifest separators are `/`. A processor MAY convert separators only at the
filesystem boundary.

Confinement MUST be based on normalized/resolved paths, not a raw string prefix.
Path safety MUST be rechecked immediately before replacement and stale deletion
to reduce time-of-check/time-of-use risk.

On case-insensitive filesystems, output collision detection MUST use the
filesystem's effective case comparison. On Windows, the processor MUST reject
two outputs that differ only by case.

The workspace root and directories MUST NOT be replacement or deletion targets.

## 11. Codec registry and contracts

### 11.1 Explicit capability selection

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

### 11.2 Registry

The embedding library or application constructs the codec registry explicitly.
Duplicate capability IDs MUST be rejected.

The project file MUST NOT cause dynamic assembly, package, module, or script
loading. A missing capability ID is a compile error.

### 11.3 Descriptor

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

### 11.4 Read contract

A codec receives a safely resolved resource context and validated options. It
MUST NOT discover additional project resources independently.

A tree codec returns one detached dataset tree.

A dataset codec returns one detached shallow dataset.

An overlay codec returns one detached typed overlay value. It MUST parse the
complete value before the processor applies it to a dataset.

Expected format failures MUST be returned as diagnostics. A processor MUST catch
an unexpected codec exception at the resource boundary and convert it to a
structured failure.

### 11.5 Write contract

A tree or dataset codec receives the selected canonical dataset view. An overlay
codec receives a typed facet extracted from that dataset.

A codec returns either:

- complete rendered content; or
- `Omit`, if its descriptor supports omission and the rule permits
  `omitWhenEmpty`.

A codec MUST NOT open, truncate, replace, or delete the final destination. The
workspace writer owns staging and replacement.

### 11.6 Standard capability IDs

Implementations SHOULD use these IDs for the corresponding built-in
capabilities:

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

`arc.yaml.tree.v1` represents the complete recursively nested YAML form and owns
all standardized facets it serializes. `arc.yaml.dataset.v1` is shallow and
base-only.

## 12. Compilation

### 12.1 Required pipeline

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

### 12.2 Static and concrete planning

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

### 12.3 Determinism

Compilation, discovery, target selection, merge, execution reporting, and
diagnostics MUST use deterministic ordering:

1. dependency order;
2. expanded profile/project rule order where dependencies are equal;
3. normalized path using ordinal comparison; and
4. dataset identifier using ordinal comparison.

## 13. Read processing

### 13.1 Root requirement

A readable plan MUST define exactly one root-forming read owner:

- a tree rule targeting `root`; or
- a dataset rule targeting `root`.

If the root resource is absent or fails, the result has no usable ARC. Dependent
resources are skipped and diagnostics are returned.

### 13.2 Processing order

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

### 13.3 Graph attachment

The reader MUST use model attachment behavior that preserves:

- reciprocal `hasPart`/`partOf`;
- process ownership;
- process input/output canonical references;
- root-level sample, data, and recipe identity; and
- all existing ARC graph invariants.

A storage processor MUST NOT maintain a conflicting parallel graph identity
system.

### 13.4 Best-effort resource handling

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

## 14. Canonical identity and compatible union

### 14.1 Identity keys

When attaching independently parsed contributions, the processor reconciles
shared model entities using the ProcessCore identity keys:

| Entity | Canonical key |
|---|---|
| `Sample` | name |
| `Data` | path plus fragment selector |
| `Recipe` | name plus version |

Processes remain distinct model objects and are not merged merely because their
values are equal.

### 14.2 Scalars

For every scalar field on equal-key entities:

| Canonical value | Incoming value | Result |
|---|---|---|
| absent | present | copy incoming |
| present | absent | retain canonical |
| equal present | equal present | retain without warning |
| unequal present | unequal present | retain canonical and emit `MERGE_CONFLICT` |

Absence follows the property's model semantics. An empty string is a value unless
the model property already normalizes it to absence.

### 14.3 Collections

Collections are merged by stable union:

1. retain canonical items in their existing order;
2. identify entity items by their established key;
3. recursively merge equal-key entity items;
4. compare value items by structural equality where defined;
5. append unseen incoming items in incoming order; and
6. avoid exact duplicates.

Conflicting equal-key values retain the canonical value and emit a diagnostic.

### 14.4 Dynamic properties

For model `DynamicObj` overflow properties:

- copy an incoming value when the canonical key is absent;
- recursively merge map-like values;
- accept structurally equal opaque values;
- stable-union list values when meaningful equality exists; and
- otherwise retain the canonical value and emit `MERGE_CONFLICT`.

Project/profile configuration and storage bindings MUST NOT be inserted into
model dynamic properties.

### 14.5 Conflict policy

Merge conflicts are deterministic diagnostics. They SHOULD be warnings by
default. A caller MAY request strict execution behavior that treats them as
errors, but the merge result remains first-value-preserving.

### 14.6 Writeback

Version 1.0 tracks no per-field source provenance. On write, the fully merged
canonical shared entity MUST be serialized through every owned dataset
representation that references it.

## 15. Write processing

### 15.1 Canonical rewrite

Writing is a canonical model-to-resource rewrite, not an in-place syntax patch.
A writer is not required to preserve:

- YAML comments or formatting;
- unknown workbook formatting;
- worksheet layout not represented by the codec;
- original field provenance; or
- source collection ordering when the codec defines canonical order.

For deterministic codecs, a second write of an unchanged model and plan SHOULD
produce identical bytes.

### 15.2 Required pipeline

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

### 15.3 Per-resource replacement

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

### 15.4 Best-effort writes

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

### 15.5 Empty contribution omission

When `omitWhenEmpty` is true and the codec returns `Omit`:

- the binding counts as successfully processed;
- no output is staged;
- the path is excluded from expected outputs; and
- an existing currently managed file at that path may become stale.

A base tree or dataset codec SHOULD NOT return `Omit`.

### 15.6 Unowned non-empty facets

Concrete write planning SHOULD emit a warning when a dataset contains a non-empty
standardized facet with no writable owner. Zero ownership is permitted for
intentional partial exports. More than one owner in the same direction is an
error.

## 16. Stale managed outputs

### 16.1 Eligibility

Stale cleanup occurs only if every planned writable binding was either:

- written successfully; or
- intentionally omitted.

If any render, stage, or replacement fails, the processor MUST NOT delete any
stale candidate.

### 16.2 Candidate calculation

After complete write success, the writer:

1. discovers existing regular files matched by each currently enabled writable
   rule's compiled write template;
2. calculates the normalized expected non-omitted output set;
3. subtracts expected paths from matched paths;
4. revalidates every candidate immediately before deletion; and
5. deletes candidates independently, recording each outcome.

### 16.3 Prohibited deletion

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

## 17. Diagnostics and results

### 17.1 Diagnostic fields

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

### 17.2 Standard codes

Processors SHOULD use these stable codes:

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
FACET_UNOWNED
MERGE_CONFLICT
STALE_DELETE
```

An implementation MAY add more specific codes but SHOULD retain these categories
for portable callers.

### 17.3 Load result

A load result contains:

- optional ARC graph;
- optional workspace session;
- ordered resource outcomes; and
- ordered diagnostics.

When no root is materialized, the ARC and session model value are absent.

### 17.4 Write result

A write result contains:

- written paths;
- omitted paths;
- failed paths;
- prior destinations retained after failure;
- deleted stale paths;
- stale deletion failures;
- ordered resource outcomes; and
- ordered diagnostics.

## 18. Round-trip properties

### 18.1 Semantic read after write

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

### 18.2 Canonical write stability

For deterministic codecs:

```text
write(P, read(P, write(P, M))) = write(P, M)
```

after the first canonical rewrite.

### 18.3 Failure safety

A failed resource operation MUST NOT:

- partially apply an overlay;
- attach a partially parsed dataset;
- truncate its current output destination;
- trigger stale cleanup; or
- cancel independent operations except through an explicit target dependency.

## 19. Built-in workspace profiles

An implementation SHOULD provide these exact built-in profile IDs and versions:

```text
org.arc.monolithic-yaml  1.0
org.arc.scaffold         1.0
```

### 19.1 Monolithic YAML

The monolithic profile is equivalent to:

```yaml
type: ArcWorkspaceProfile
specVersion: "1.0"
id: org.arc.monolithic-yaml
version: "1.0"
description: Complete recursive ARC YAML document
rules:
  - id: arc-yaml
    contribution: tree
    codec: arc.yaml.tree.v1
    target:
      selector: root
    directions: [read, write]
    read:
      path: arc.yml
      required: true
      cardinality: one
    write:
      path: arc.yml
```

### 19.2 ARC scaffold

The scaffold profile describes the established ISA-XLSX paths:

```text
isa.investigation.xlsx
studies/{dataset.identifier}/isa.study.xlsx
assays/{dataset.identifier}/isa.assay.xlsx
workflows/{dataset.identifier}/isa.workflow.xlsx
runs/{dataset.identifier}/isa.run.xlsx
```

and optional adjacent:

```text
isa.datamap.xlsx
```

It MUST use dataset contributions for investigation, study, assay, workflow, and
run workbooks, and `arc.datamap` overlay contributions for Datamap workbooks.
Its selectors and decoration discriminators MUST match the ARC scaffold model
mapping.

## 20. Examples

Examples in this section are informative but are intended to be structurally
conforming.

### 20.1 Monolithic YAML project

```yaml
type: ArcWorkspaceProject
specVersion: "1.0"
workspaceProfiles:
  - id: org.arc.monolithic-yaml
    version: "1.0"
    builtin: org.arc.monolithic-yaml
```

### 20.2 Scaffold project

```yaml
type: ArcWorkspaceProject
specVersion: "1.0"
workspaceProfiles:
  - id: org.arc.scaffold
    version: "1.0"
    builtin: org.arc.scaffold
```

### 20.3 Profile with project overrides

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

### 20.4 Mixed explicit layout

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
    attachTo: root
    directions: [read, write]
    read:
      path: "metadata/studies/{dataset.identifier}/study.yml"
      cardinality: many
    write:
      path: "metadata/studies/{dataset.identifier}/study.yml"

  - id: assays
    contribution: dataset
    codec: arc.isa.assay.xlsx.v1
    target:
      selector: children
      parent: root
      additionalType: Assay
    attachTo: root
    directions: [read, write]
    read:
      path: "workbooks/{dataset.identifier}/assay.xlsx"
      cardinality: many
    write:
      path: "workbooks/{dataset.identifier}/assay.xlsx"

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
    write:
      path: "contexts/{dataset.identifier}/datamap.xlsx"
      omitWhenEmpty: true
```

### 20.5 Direction-specific migration

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
      path: metadata/root.yml
      required: true
    write:
      path: metadata/root.yml

  - id: import-legacy-studies
    contribution: dataset
    codec: arc.isa.study.xlsx.v1
    target:
      selector: children
      parent: root
      additionalType: Study
    attachTo: root
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

The two study rules do not conflict because each direction has one base owner.
Files under `legacy` are not stale write outputs.

## 21. Version-1.0 exclusions

The following are outside this version:

- arbitrary field-by-field model partitioning;
- arbitrary scientific-payload packaging;
- remote workspace profiles;
- dynamic codec loading;
- general glob or expression languages;
- nested project manifests;
- multiple ARC roots;
- persisted source provenance or lockfiles;
- exact source-format preservation;
- multi-file transactional commit;
- profile-version ranges; and
- automatic cleanup of files owned only by a former project configuration.

Future versions MUST preserve safe path confinement, explicit codec selection,
direction-specific ownership, deterministic planning, and the distinction
between fatal plan errors and best-effort resource errors.
