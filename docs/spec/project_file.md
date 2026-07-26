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

The non-normative specification plan is in
[plans/project_file.md](../../plans/project_file.md). Processor and codec
behavior is specified separately in [Project File
Handling](project_file_handling.md).

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
resources managed by this specification. Such payloads are outside the set of
resources managed by a project file.

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

Shared `Sample`, `Data`, and `Recipe` objects form the reference closure required
by a base representation. They are reconciled by canonical identity as specified
in [Project File
Handling](project_file_handling.md#6-canonical-identity-and-compatible-union)
and are not independently assignable facets.

## 2. Document conformance

This specification defines two document conformance classes. Processor and
codec conformance are defined in
[Project File Handling](project_file_handling.md#1-conformance).

### 2.1 Project-document conformance

A conforming project document:

- is valid YAML;
- has `type: ArcWorkspaceProject`;
- has `specVersion: "1.0"`;
- satisfies the structural requirements in this specification; and
- is valid against the supplied profile and codec-capability registries,
  including all rule, path, ownership, and cross-document constraints.

Structural validity alone does not imply that all referenced resources exist.

### 2.2 Workspace-profile conformance

A conforming workspace profile:

- is valid YAML;
- has `type: ArcWorkspaceProfile`;
- has `specVersion: "1.0"`;
- declares an exact ID and version;
- satisfies its parameter and rule requirements; and
- is valid when referenced by a project with a compatible codec-capability
  registry.

## 3. Workspace and project location

### 3.1 Canonical location

Version 1.0 defines exactly one project file:

```text
<workspace-root>/.arc/project.yml
```

The workspace root is the parent of `.arc`.

Nested project files and mounted subprojects are not defined by version 1.0.

### 3.2 Path domain

All metadata resources and local profiles referenced by the project MUST be
inside the workspace root. All manifest paths use `/` as their separator,
regardless of the host operating system.

Only local filesystem resources are supported. HTTP, HTTPS, Git, package
registry, object-store, and other remote sources are not conforming version-1.0
project sources.

### 3.3 Configuration files

The project file and its local profile documents are configuration rather than
managed metadata outputs. They are never implicitly rewritten or treated as
stale outputs. Operational protection requirements are defined in [Project File
Handling](project_file_handling.md#2-project-resolution-and-path-processing).

## 4. YAML data model

### 4.1 General rules

Project and workspace-profile files MUST use YAML mappings, sequences, strings,
booleans, integers, and null where allowed by the field definition.

Field names are case-sensitive.

Unknown fields MUST be rejected except within:

- `extensions`; and
- `codecOptions`.

An `extensions` mapping has no version-1.0 semantics. Extension content is data,
not executable code.

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

The repository publishes JSON Schema draft 2020-12 representations for the
[project document](../../schemas/yml/arc-workspace-project.schema.yml) and
[workspace profile](../../schemas/yml/arc-workspace-profile.schema.yml).

Those schemas describe structural constraints. The semantic requirements in
this specification remain authoritative for registry lookup, path inversion,
ownership, selector, and cross-rule constraints.

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

`builtin` is an exact key in the supplied workspace-profile registry. `file` is
relative to `.arc` and MUST satisfy the path rules in section 10.

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
within the project. Their qualified IDs have the form:

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

Version 1.0 does not define a persisted lockfile, compiled-plan file, binding
file, or per-field provenance file. Such state is not a normative project
artifact.

## 6. Workspace profiles

### 6.1 Shape

```yaml
type: ArcWorkspaceProfile
specVersion: "1.0"
id: org.example.layout
version: "1.0"
description: Example reusable ARC metadata layout

parameters: {}
rules:
  - id: root
    contribution: tree
    codec: org.example.metadata.yml.v1
    target:
      selector: root
    directions: [read]
    read:
      path: metadata.yml
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

Parameter resolution and validation order are defined in
[Project File Handling](project_file_handling.md#23-parameter-resolution).

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

A dataset contribution with non-empty inline child datasets is non-conforming.
Hierarchy is established by `attachTo` or the target's parent reference.

A non-root readable dataset rule MUST identify exactly one parent for each
matched resource.

### 7.5 Overlay rules

An overlay rule maps one resource to the named `facet` of an existing target
dataset.

An overlay requires an existing base dataset. Transactional overlay application
is defined in [Project File
Handling](project_file_handling.md#36-facet-enforcement).

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
- zero matches means the resource is absent otherwise; and
- more than one match is always an error.

For `many`, zero matches is an error only when `required` is true. Every match is
an independent resource.

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
whether its typed contribution is empty. A codec returning `Omit` creates no output. Cleanup consequences are defined in
[Project File Handling](project_file_handling.md#75-empty-contribution-omission).

### 7.8 Codec options

`codecOptions` contains declarative values interpreted by the registered codec
capability. Unknown or invalid option names and values make the project
non-conforming.

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

Runtime parent resolution is defined in
[Project File Handling](project_file_handling.md#25-selector-and-parent-validation).

### 8.6 Unsupported selectors

Arbitrary predicates, graph-query expressions, JSONPath, regular expressions,
and executable selector code are not defined by version 1.0.

## 9. Parameters and path templates

Compilation, matching, rendering, and capture validation are defined in [Project
File Handling](project_file_handling.md#2-project-resolution-and-path-processing).

### 9.1 Template grammar

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

### 9.2 Unsupported syntax

Version 1.0 does not support:

- `*`, `**`, `?`, or character-class globs;
- regular expressions;
- environment-variable expansion;
- command or shell substitution;
- URI-template expansion;
- Jinja, Liquid, or other general template languages; or
- partial-segment captures.

## 10. Path safety

Every project, profile, read, and write path MUST:

- be relative to its specified base;
- reject absolute paths;
- reject drive-qualified and UNC paths;
- reject URI schemes;
- reject NUL;
- reject empty, `.`, and `..` segments;
- remain within the resolved workspace root after normalization;
- reject traversal through a symbolic link or reparse point that resolves
  outside the workspace.

Manifest separators are `/`. Host-specific separator conversion occurs only at
the filesystem boundary.

Operational confinement, filesystem collision checks, replacement, and deletion
requirements are defined in [Project File Handling](project_file_handling.md#24-filesystem-safety).

## 11. Codec capability IDs

### 11.1 Explicit capability selection

Every rule names one codec capability:

```yaml
codec: arc.yaml.dataset.v1
```

The capability is selected only by exact registry lookup of this ID. It MUST NOT
be inferred or replaced based on:

- filename extension;
- media type;
- file signature;
- workbook sheets;
- discovery order; or
- an unregistered value in `codecOptions`.

Media type and format information MAY be reported for validation or diagnostics,
but does not select a codec.

### 11.2 Standard capability IDs

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

Registry construction, descriptor validation, and codec execution contracts are
defined in [Project File
Handling](project_file_handling.md#3-codec-registry-and-contracts).

## 12. Built-in workspace profiles

Conforming implementations SHOULD provide these exact built-in profile IDs and
versions:

```text
org.arc.monolithic-yaml  1.0
org.arc.scaffold         1.0
```

### 12.1 Monolithic YAML

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

### 12.2 ARC scaffold

The scaffold profile is exactly:

```yaml
type: ArcWorkspaceProfile
specVersion: "1.0"
id: org.arc.scaffold
version: "1.0"
description: Established ISA-XLSX ARC scaffold

rules:
  - id: investigation
    contribution: dataset
    codec: arc.isa.investigation.xlsx.v1
    target:
      selector: root
    directions: [read, write]
    read:
      path: isa.investigation.xlsx
      required: true
      cardinality: one
    write:
      path: isa.investigation.xlsx

  - id: investigation-datamap
    contribution: overlay
    facet: arc.datamap
    codec: arc.isa.datamap.xlsx.v1
    target:
      selector: root
    directions: [read, write]
    read:
      path: isa.datamap.xlsx
      required: false
      cardinality: one
    write:
      path: isa.datamap.xlsx
      omitWhenEmpty: true

  - id: study
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
      cardinality: many
    write:
      path: "studies/{dataset.identifier}/isa.study.xlsx"

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
      path: "studies/{dataset.identifier}/isa.datamap.xlsx"
      cardinality: many
    write:
      path: "studies/{dataset.identifier}/isa.datamap.xlsx"
      omitWhenEmpty: true

  - id: assay
    contribution: dataset
    codec: arc.isa.assay.xlsx.v1
    target:
      selector: children
      parent: root
      additionalType: Assay
    attachTo: root
    directions: [read, write]
    read:
      path: "assays/{dataset.identifier}/isa.assay.xlsx"
      cardinality: many
    write:
      path: "assays/{dataset.identifier}/isa.assay.xlsx"

  - id: assay-datamap
    contribution: overlay
    facet: arc.datamap
    codec: arc.isa.datamap.xlsx.v1
    target:
      selector: children
      parent: root
      additionalType: Assay
    directions: [read, write]
    read:
      path: "assays/{dataset.identifier}/isa.datamap.xlsx"
      cardinality: many
    write:
      path: "assays/{dataset.identifier}/isa.datamap.xlsx"
      omitWhenEmpty: true

  - id: workflow
    contribution: dataset
    codec: arc.isa.workflow.xlsx.v1
    target:
      selector: children
      parent: root
      additionalType: Workflow
    attachTo: root
    directions: [read, write]
    read:
      path: "workflows/{dataset.identifier}/isa.workflow.xlsx"
      cardinality: many
    write:
      path: "workflows/{dataset.identifier}/isa.workflow.xlsx"

  - id: workflow-datamap
    contribution: overlay
    facet: arc.datamap
    codec: arc.isa.datamap.xlsx.v1
    target:
      selector: children
      parent: root
      additionalType: Workflow
    directions: [read, write]
    read:
      path: "workflows/{dataset.identifier}/isa.datamap.xlsx"
      cardinality: many
    write:
      path: "workflows/{dataset.identifier}/isa.datamap.xlsx"
      omitWhenEmpty: true

  - id: run
    contribution: dataset
    codec: arc.isa.run.xlsx.v1
    target:
      selector: children
      parent: root
      additionalType: Run
    attachTo: root
    directions: [read, write]
    read:
      path: "runs/{dataset.identifier}/isa.run.xlsx"
      cardinality: many
    write:
      path: "runs/{dataset.identifier}/isa.run.xlsx"

  - id: run-datamap
    contribution: overlay
    facet: arc.datamap
    codec: arc.isa.datamap.xlsx.v1
    target:
      selector: children
      parent: root
      additionalType: Run
    directions: [read, write]
    read:
      path: "runs/{dataset.identifier}/isa.datamap.xlsx"
      cardinality: many
    write:
      path: "runs/{dataset.identifier}/isa.datamap.xlsx"
      omitWhenEmpty: true
```

This profile reflects the established scaffold mapping: the investigation is the
root dataset; Study, Assay, Workflow, and Run datasets are immediate children;
and each base workbook may have an adjacent optional Datamap overlay.

## 13. Examples

Examples in this section are informative but are intended to be structurally
conforming.

### 13.1 Monolithic YAML project

```yaml
type: ArcWorkspaceProject
specVersion: "1.0"
workspaceProfiles:
  - id: org.arc.monolithic-yaml
    version: "1.0"
    builtin: org.arc.monolithic-yaml
```

### 13.2 Scaffold project

```yaml
type: ArcWorkspaceProject
specVersion: "1.0"
workspaceProfiles:
  - id: org.arc.scaffold
    version: "1.0"
    builtin: org.arc.scaffold
```

### 13.3 Profile with project overrides

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

### 13.4 Mixed explicit layout

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

### 13.5 Direction-specific migration

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
Files under `legacy` are outside the writable rule set.

### 13.6 Local workspace profile

`.arc/project.yml`:

```yaml
type: ArcWorkspaceProject
specVersion: "1.0"
workspaceProfiles:
  - id: org.example.lab-layout
    version: "1.0"
    file: profiles/lab-layout.yml
    parameters:
      studiesDirectory: experiments
```

`.arc/profiles/lab-layout.yml`:

```yaml
type: ArcWorkspaceProfile
specVersion: "1.0"
id: org.example.lab-layout
version: "1.0"
description: One shallow YAML document per study

parameters:
  studiesDirectory:
    type: path-segment
    default: studies

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
      cardinality: one
    write:
      path: metadata/root.yml

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
      path: "{studiesDirectory}/{dataset.identifier}/study.yml"
      cardinality: many
    write:
      path: "{studiesDirectory}/{dataset.identifier}/study.yml"
```

## 14. Migration and compatibility

The explicit `ARC` YAML and scaffold APIs remain unchanged and bypass project
discovery. The generic `ARC.load`, `Write`, and `Update` APIs automatically use
the project-file language when `.arc/project.yml` is present, as defined by
[Project File Handling](project_file_handling.md#10-processcore-arc-facade-integration).

For existing workspaces:

- use `org.arc.monolithic-yaml` to describe the current recursive `arc.yml`;
- use `org.arc.scaffold` to describe the current ISA-XLSX scaffold;
- use restricted overrides when only paths or safe rule settings change; and
- use separate read-only and write-only rules for controlled format or layout
  migration.

Without `.arc/project.yml`, generic ARC I/O retains its legacy behavior and does
not infer or create project configuration. A present project file is
authoritative, so an invalid project does not fall back to another
representation. Generic I/O never implicitly rewrites project or local-profile
documents. Removing or changing a profile does not authorize deletion of files
owned only by the former configuration.

## 15. Non-conforming examples

These examples are intentionally invalid.

### 15.1 Unknown field

```yaml
type: ArcWorkspaceProject
specVersion: "1.0"
rulse: []
```

`rulse` is not a defined field and is outside `extensions`.

### 15.2 Unsafe path

```yaml
type: ArcWorkspaceProject
specVersion: "1.0"
rules:
  - id: root
    contribution: tree
    codec: arc.yaml.tree.v1
    target:
      selector: root
    directions: [read]
    read:
      path: ../arc.yml
```

The path escapes the workspace root.

### 15.3 Semantic override

```yaml
type: ArcWorkspaceProject
specVersion: "1.0"
workspaceProfiles:
  - id: org.arc.scaffold
    version: "1.0"
    builtin: org.arc.scaffold
overrides:
  - rule: org.arc.scaffold#study
    codec: arc.yaml.dataset.v1
```

An override cannot change codec identity. The project must disable the inherited
rule and add a project-local rule.

### 15.4 Duplicate write ownership

```yaml
type: ArcWorkspaceProject
specVersion: "1.0"
rules:
  - id: first
    contribution: tree
    codec: arc.yaml.tree.v1
    target:
      selector: root
    directions: [write]
    write:
      path: first.yml

  - id: second
    contribution: dataset
    codec: arc.yaml.dataset.v1
    target:
      selector: root
    directions: [write]
    write:
      path: second.yml
```

Both rules own `arc.base` for the root in the write direction.

## 16. Version-1.0 exclusions

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
and direction-specific ownership. Deterministic planning and failure behavior are
defined in [Project File Handling](project_file_handling.md).
