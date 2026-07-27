---
title: ARC Workspace Project File
category: Specification
categoryindex: 3
index: 5
---

# ARC Workspace Project File

## Status and scope

This document specifies the ARC workspace project file and its workspace-profile
language.

The project file maps complete ARC `Dataset` values to registered bidirectional
codecs at workspace-relative anchor paths. It is storage configuration, not an
ARC model serialization.

The key words **MUST**, **MUST NOT**, **REQUIRED**, **SHALL**, **SHALL NOT**,
**SHOULD**, **SHOULD NOT**, **RECOMMENDED**, **NOT RECOMMENDED**, **MAY**, and
**OPTIONAL** in this document are to be interpreted as described in
[BCP 14](https://www.rfc-editor.org/info/bcp14) when, and only when, they appear
in all capitals.

The non-normative design plan is
[`plans/project_file.md`](../../plans/project_file.md). Processor and codec
behavior is specified in [Project File Handling](project_file_handling.md).

## 1. Model

```text
project/profile rule
        =
Dataset target + exact codec ID + anchor path
```

A rule selects one of:

- the root Dataset;
- one direct child of the root with an exact identifier; or
- direct children of the root with an exact `additionalType`.

One codec invocation reads or writes each selected Dataset. A selected Dataset
MAY contain nested Datasets. Project selectors do not select or constrain that
deeper nesting.

Scientific payloads referenced by [`Data`](process_core/Data.md), such as CSV,
Parquet, images, or instrument files, are not managed resources under this
specification.

### 1.1 Terms

**Workspace root**
: The local directory containing `.arc`.

**Project file**
: The storage configuration at `.arc/project.yml`.

**Workspace profile**
: A reusable declarative collection of storage rules.

**Rule**
: A mapping among a rule ID, codec ID, Dataset target, and anchor path.

**Anchor**
: The project-visible resource path passed to a codec. A codec MAY derive
  representation-specific companion resources from it.

**Exact target**
: An `identifier` target selecting one named direct child of the root.

**Type target**
: An `additionalType` target selecting otherwise-unclaimed matching direct
  children.

## 2. Conformance

### 2.1 Project document

A conforming project document:

- is valid YAML;
- has `type: ArcWorkspaceProject`;
- satisfies the structural requirements in this document;
- expands its referenced workspace profiles to exactly one root rule;
- satisfies all profile, target, codec, path, and collision requirements; and
- contains no unknown fields.

An `ArcWorkspaceProject` has no document version field in this specification.

### 2.2 Workspace-profile document

A conforming workspace profile:

- is valid YAML;
- has `type: ArcWorkspaceProfile`;
- declares an exact `id` and `version`;
- contains at least one conforming rule; and
- contains no unknown fields.

Profile `version` identifies the profile release. This specification does not
define a separate `specVersion`.

A referenced YAML document MUST conform as a workspace-profile document.

### 2.3 Processor and codec conformance

Processor and codec conformance is defined in
[Project File Handling](project_file_handling.md#1-conformance).

## 3. Workspace and path domain

The canonical project path is:

```text
<workspace-root>/.arc/project.yml
```

The parent of `.arc` is the workspace root. Referenced profile documents do not
create nested workspace roots or additional project entry points.

Metadata anchors MUST be inside the workspace root. Local referenced profiles
MUST be inside `.arc`. Manifest filesystem paths use `/` regardless of the host
operating system.

The root project is local. Referenced profiles MAY be local files or HTTP(S)
resources. Project and profile documents are configuration and MUST NOT be
implicitly rewritten or deleted by project handling.

## 4. YAML data model

Field names and identifier comparisons are case-sensitive. Duplicate YAML
mapping keys and unknown fields MUST be rejected.

YAML aliases MAY be resolved by a parser, but validation applies to the expanded
value. YAML tags MUST NOT cause executable behavior or dynamic type loading.

Unless a field says otherwise, strings MUST NOT be implicitly coerced from
numbers, booleans, or null.

Project, profile, rule, and codec IDs MUST:

- begin with an ASCII letter;
- contain only ASCII letters, digits, `.`, `_`, or `-`; and
- contain no more than 128 characters.

Profile versions MUST be quoted strings that begin with an ASCII letter or
digit, contain only ASCII letters, digits, `.`, `_`, `+`, or `-`, and contain no
more than 64 characters.

The JSON Schema draft 2020-12 representations are:

- [project schema](../../schemas/yml/arc-workspace-project.schema.yml); and
- [workspace-profile schema](../../schemas/yml/arc-workspace-profile.schema.yml).

The schemas describe structural constraints. This document remains authoritative
for cross-document, registry, target, resource, and concrete path constraints.

## 5. Project document

### 5.1 Shape

```yaml
type: ArcWorkspaceProject

workspaceProfiles:
  - url: "https://example.org/arc/isa-xlsx-scaffold.yml"

rules: []
```

Fields:

| Field | Type | Required | Default |
|---|---|---:|---|
| `type` | string | yes | none |
| `workspaceProfiles` | sequence of profile references | no | empty |
| `rules` | sequence of rules | no | empty |

At least one profile reference or project-local rule MUST be declared.

After profile expansion, the combined rules MUST contain exactly one root rule.

### 5.2 Workspace-profile references

```yaml
workspaceProfiles:
  - file: profiles/local-layout.yml
  - url: "https://example.org/arc/remote-layout.yml"
```

Each entry MUST contain exactly one `file` or `url`. `file` is relative to
`.arc` and MUST remain inside it. `url` MUST be an absolute HTTP(S) URL.

The referenced YAML MUST be an `ArcWorkspaceProfile`. Its declared `id` MUST be
unique in the project. Profiles contribute rules in listed order, followed by
project-local rules. Order does not resolve target or path conflicts.

### 5.3 Rule qualification

Project-local rule IDs MUST be unique within `rules`. Their qualified IDs are:

```text
project#<rule-id>
```

Profile rules are qualified as:

```text
<profile-id>#<rule-id>
```

Qualified IDs are used in compiled plans, outcomes, and diagnostics.

## 6. Workspace profiles

### 6.1 Shape

```yaml
type: ArcWorkspaceProfile
id: org.example.layout
version: "1.0"
description: Example layout

rules:
  - id: root
    codec: example.yml
    target: root
    path: metadata.yml
```

Fields:

| Field | Type | Required |
|---|---|---:|
| `type` | string | yes |
| `id` | profile ID | yes |
| `version` | profile version | yes |
| `description` | string | no |
| `rules` | non-empty sequence of rules | yes |

Rule IDs MUST be unique within the profile.

Profiles do not have parameters, overrides, codec options, enabled flags, or
extension fields.

## 7. Rules

### 7.1 Shape

```yaml
- id: study
  codec: isa.study.xlsx
  target:
    additionalType: Study
  path: "studies/{dataset.identifier}/isa.study.xlsx"
```

Fields:

| Field | Type | Required |
|---|---|---:|
| `id` | rule ID | yes |
| `codec` | codec ID | yes |
| `target` | target | yes |
| `path` | path template | yes |

Every field is required. No other field is allowed.

Every rule is bidirectional. Its codec and path are used for both reading and
writing.

## 8. Targets

A target is exactly one of the three forms in this section.

### 8.1 Root

```yaml
target: root
```

The root target selects the ARC root Dataset. The expanded project MUST contain
exactly one root rule.

On read, the root rule MUST resolve to exactly one anchor and that anchor MUST
successfully produce one Dataset for a usable ARC result.

On write, the rule selects the one root Dataset.

### 8.2 Identifier

```yaml
target:
  identifier: special-study
```

An identifier target selects exactly one direct child of the root whose
identifier is equal to the declared value.

On read:

- the rule MUST resolve to exactly one anchor;
- the anchor is required;
- the parsed Dataset identifier MUST equal the declared value; and
- the Dataset is attached directly to the root.

On write, absence of the named direct child is a target error.

Two expanded identifier rules MUST NOT declare the same identifier.

### 8.3 Additional type

```yaml
target:
  additionalType: Study
```

An additional-type target selects zero or more direct children of the root whose
`additionalType` is present and exactly equals the declared case-sensitive
value.

Every parsed Dataset MUST have the declared `additionalType`. A mismatch fails
that resource.

Two expanded additional-type rules MUST NOT declare the same value.

### 8.4 Identifier precedence

All expanded identifier targets are reserved before additional-type selection.

If a direct child has an identifier target and also matches an additional-type
target:

- the identifier rule selects it;
- the additional-type rule MUST exclude it; and
- no duplicate representation is created.

This precedence is independent of rule and profile order.

During read discovery, an additional-type binding whose captured identifier is
reserved by an identifier rule MUST be excluded from that type rule. A resource
at the exact rule's anchor remains required; discovery through the general path
does not satisfy the exact rule.

### 8.5 Dataset depth

These targets select only the root and its direct project-level children. A
selected Dataset MAY contain deeper nested Datasets. A processor MUST preserve
such nesting returned by the selected Dataset's codec and MUST NOT flatten it
because of project target semantics.

## 9. Path templates

### 9.1 Grammar

A path template is a `/`-separated sequence of whole segments. A segment is:

- a non-empty literal; or
- exactly `{dataset.identifier}`.

The capture MAY occur at most once.

Valid:

```text
studies/{dataset.identifier}/isa.study.xlsx
```

Invalid:

```text
studies/study-{dataset.identifier}.xlsx
```

Version 1 does not support `{parent.identifier}`, parameter references, globs,
regular expressions, environment expansion, command substitution, URI
templates, or general template languages.

### 9.2 Target-specific requirements

- A root path MAY be literal or contain `{dataset.identifier}`.
- An identifier-target path MAY be literal or contain
  `{dataset.identifier}`.
- An additional-type path MUST contain `{dataset.identifier}` exactly once.

Root and identifier paths resolve to exactly one anchor. Additional-type paths
may match zero or more anchors.

For a root or identifier rule with a capture, discovery of more than one anchor
is an error even if one resource later fails parsing.

### 9.3 Matching and rendering

On read, literal segments match exactly. The capture matches one safe, non-empty
path segment and yields the candidate Dataset identifier.

When a capture is present, the parsed Dataset identifier MUST equal the captured
value. An identifier target additionally requires equality with its declared
identifier.

On write, `{dataset.identifier}` renders the selected Dataset identifier. A
rendered identifier MUST be a safe path segment.

### 9.4 Safety

Every anchor and local referenced-document path MUST:

- be relative to its defined base;
- use `/` separators in the document;
- reject absolute, drive-qualified, UNC, and URI forms;
- reject backslashes, NUL, empty, `.`, and `..` segments;
- remain within its base after normalization; and
- reject traversal through a symlink or reparse point outside its base.

Collision identity uses normalized resolved paths and the host filesystem's
effective case comparison.

## 10. Codecs and anchors

### 10.1 Exact codec lookup

`codec` is an exact registered capability ID. A processor MUST NOT infer or
replace it based on extension, media type, content, workbook sheets, discovery
order, or another rule.

A missing codec is a project compilation error.

Every codec used by a rule MUST support both reading and writing a complete
Dataset at the same anchor.

### 10.2 Standard ISA-XLSX codec IDs

The standard scaffold uses:

```text
isa.investigation.xlsx
isa.study.xlsx
isa.assay.xlsx
isa.workflow.xlsx
isa.run.xlsx
```

### 10.3 Opaque companion resources

The anchor is the only path described by a rule. A registered codec MAY derive
and manage format-specific companion resources.

For example, an ISA-XLSX codec MAY read or write an adjacent
`isa.datamap.xlsx` as part of the selected Dataset representation.

Companion resources:

- are not separate project targets;
- are not subject to generic project collision planning;
- are not separate generic outcomes or diagnostics paths; and
- are not automatically deleted by project handling.

Their confinement, consistency, and failure behavior are codec responsibilities
defined in [Project File Handling](project_file_handling.md#3-codec-contract).

## 11. Cross-rule validation

After profile expansion, a conforming project MUST reject:

- zero or more than one root rule;
- duplicate identifier target values;
- duplicate additional-type target values;
- duplicate qualified rule IDs;
- missing or incompatible codecs;
- invalid target/path combinations;
- unsafe templates or rendered anchors; and
- statically identical or concretely colliding anchors.

An identifier target and an additional-type target are not a target conflict.
Identifier precedence makes their selection domains disjoint.

All anchor collisions MUST be detected before invoking a codec for the affected
operation. Rule order MUST NOT choose a winner.

## 12. Standard scaffold profile

The standard ISA-XLSX profile document is:

```yaml
type: ArcWorkspaceProfile
id: arc.isa.xlsx.scaffold
version: "1.0"
description: Established ISA-XLSX ARC scaffold

rules:
  - id: investigation
    codec: isa.investigation.xlsx
    target: root
    path: isa.investigation.xlsx

  - id: study
    codec: isa.study.xlsx
    target:
      additionalType: Study
    path: "studies/{dataset.identifier}/isa.study.xlsx"

  - id: assay
    codec: isa.assay.xlsx
    target:
      additionalType: Assay
    path: "assays/{dataset.identifier}/isa.assay.xlsx"

  - id: workflow
    codec: isa.workflow.xlsx
    target:
      additionalType: Workflow
    path: "workflows/{dataset.identifier}/isa.workflow.xlsx"

  - id: run
    codec: isa.run.xlsx
    target:
      additionalType: Run
    path: "runs/{dataset.identifier}/isa.run.xlsx"
```

The standard profile intentionally contains no Datamap rules. Each ISA-XLSX
codec is responsible for the Dataset/Datamap physical split.

The profile MAY be referenced by `file` or `url`.

## 13. Examples

### 13.1 URL-hosted scaffold project

```yaml
type: ArcWorkspaceProject
workspaceProfiles:
  - url: "https://example.org/arc/isa-xlsx-scaffold.yml"
```

The URL is illustrative; this specification does not assign the standard
profile a canonical URL.

### 13.2 Referenced profile plus local exact target

```yaml
type: ArcWorkspaceProject
workspaceProfiles:
  - file: profiles/base.yml

rules:
  - id: fixed-study
    codec: isa.study.xlsx
    target:
      identifier: calibration-study
    path: special/calibration/isa.study.xlsx
```

If `profiles/base.yml` contributes a general Study rule, `calibration-study` is
excluded from that rule and handled at the fixed path.

### 13.3 Exact target with identifier capture

```yaml
- id: fixed-assay
  codec: isa.assay.xlsx
  target:
    identifier: assay-42
  path: "special/{dataset.identifier}/isa.assay.xlsx"
```

The path must resolve exactly once, and its capture and parsed identifier must
both equal `assay-42`.

### 13.4 Local recursive YAML profile

```yaml
type: ArcWorkspaceProfile
id: org.example.recursive-yaml
version: "1.0"
rules:
  - id: arc
    codec: org.example.recursive-yaml
    target: root
    path: arc.yml
```

Whether the codec serializes nested Datasets recursively is outside the
project-file syntax.

## 14. Invalid examples

### 14.1 Type target without capture

```yaml
- id: study
  codec: isa.study.xlsx
  target:
    additionalType: Study
  path: studies/isa.study.xlsx
```

A type target may select multiple Datasets and therefore requires the identifier
capture.

### 14.2 Duplicate exact targets

Two expanded rules with:

```yaml
target:
  identifier: study-1
```

are invalid even when their codec or path differs.

### 14.3 Partial capture

```yaml
path: "studies/study-{dataset.identifier}.xlsx"
```

Captures must occupy a complete segment.
