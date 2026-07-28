# Plan for the ARC workspace project file specification

Status: specification-planning document

Planned project-file location: `.arc/project.yml`

Target specification:
[`docs/spec/project_file.md`](../docs/spec/project_file.md)

Project-file handling plan:
[`plans/project_file_handling.md`](project_file_handling.md)

## 1. Summary

The project file maps ARC `Dataset` values to registered bidirectional codecs at
safe workspace-relative anchor paths. Rules may also declare named auxiliary
files relative to their anchor directories.

The language has one small rule shape:

```yaml
- id: study
  codec: isa.study.xlsx
  target:
    additionalType: Study
  path: "studies/{dataset.identifier}/isa.study.xlsx"
  files:
    - id: datamap
      path: isa.datamap.xlsx
```

A rule selects:

- the root Dataset;
- one exact direct child by `identifier`; or
- zero or more direct children by exact `additionalType`.

Each selected Dataset is one codec invocation. The Dataset may itself contain
deeper nested Datasets. Whether a codec serializes that nesting, splits Dataset
state across declared files, or uses only its primary document is codec
behavior.

Prepared non-root bindings identify direct children stored outside the root
anchor. The writer tells the root codec exactly which identifiers are
externalized, while every child binding still receives a complete Dataset.

## 2. Goals and boundaries

### 2.1 Goals

The specification should:

- keep `.arc/project.yml` as the single discoverable workspace configuration;
- make the Dataset the only project-level storage unit;
- require one root rule;
- select direct root children by exact identifier or exact `additionalType`;
- allow an exact identifier rule to relocate one Dataset from a general layout;
- infer requiredness and multiplicity from the target;
- use one safe path for both reading and writing;
- declare optional named codec files and project-managed empty files;
- select a bidirectional codec by exact registered ID;
- allow reusable local or URL-hosted workspace profiles;
- allow a project-local rule to wholly replace profile rules with the same
  target;
- reject ambiguous targets and anchor-path collisions deterministically; and
- represent the optional ISA-XLSX decoration scaffold and its five-rule
  Dataset-YAML counterpart.

### 2.2 Non-goals

The project file does not configure:

- storage of scientific payloads referenced by `Data`;
- project-level tree, shallow, contribution, overlay, or facet ownership;
- separate read and write formats;
- optional exact targets or explicit cardinality;
- nested project-level target selectors;
- arbitrary predicates, graph queries, globs, or expression languages;
- profile parameters, profile-to-profile overrides, field-level rule merging,
  codec options, or extension fields;
- package-registry profiles or dynamic codec loading;
- required auxiliary files, arbitrary inline file content, or standalone
  directory declarations;
- automatic stale-output deletion;
- a standardized recursive-YAML profile; or
- a persisted lockfile or source-provenance map.

Recursive YAML remains possible through a local rule or profile whose root codec
serializes the nested Dataset graph.

## 3. Mental model

```text
project/profile rules
        |
        v
root or direct-child Dataset selection
        |
        v
safe anchor path + named auxiliary files + exact bidirectional codec
        |
        v
codec-owned physical representation
```

Terms:

**Workspace root**
: The directory containing `.arc`.

**Project**
: The root configuration at `.arc/project.yml`.

**Workspace profile**
: A reusable, versioned declarative collection of storage rules.

**Workspace profile reference**
: A `file` or `url` reference to an `ArcWorkspaceProfile`.

**Rule**
: A mapping among rule identity, codec identity, Dataset target, and anchor
  path.

**Anchor path**
: The project-visible path used to discover and address one codec invocation.

**Auxiliary file**
: An optional named resource resolved relative to an anchor's directory. It is
  either codec-managed or a project-managed empty file.

**Exact target**
: An `identifier` target selecting one named direct child of the root.

**Type target**
: An `additionalType` target selecting all otherwise-unclaimed matching direct
  children.

## 4. Document shapes

### 4.1 Project

The project has no version field in this version:

```yaml
type: ArcWorkspaceProject

workspaceProfiles:
  - url: "https://example.org/arc/isa-xlsx-scaffold.yml"

rules: []
```

`workspaceProfiles` and `rules` are both optional, but their effective rule set
must contain exactly one root rule.

A workspace-profile reference contains exactly one confined local `file` or
absolute HTTP(S) `url`. The loaded YAML must be an `ArcWorkspaceProfile`.
Profiles are expanded in listed order. Before qualification and cross-rule
validation, every profile rule whose target equals a project-local rule target
is removed, and the local rules are appended. Root matches root; identifier and
additional-type targets match exact, case-sensitive values of the same target
kind. This is whole-rule replacement, not field inheritance or merging;
unrelated profile rules remain.

### 4.2 Workspace profile

```yaml
type: ArcWorkspaceProfile
id: org.example.layout
version: "1.0"
description: Example ARC storage layout

rules: []
```

Profile `id` and `version` identify the profile document. They are not repeated
on the reference. Profile IDs must be unique in one project.

Profiles have no parameters or extension points and cannot override one
another. Projects have only the exact-target, whole-rule replacement mechanism
described above.

### 4.3 Rule

```yaml
- id: study
  codec: isa.study.xlsx
  target:
    additionalType: Study
  path: "studies/{dataset.identifier}/isa.study.xlsx"
  files:
    - id: datamap
      path: isa.datamap.xlsx
```

`id`, `codec`, `target`, and `path` are required. `files` is optional. Unknown
fields are errors.

Rule IDs are unique within their declaring project or profile. After local
replacement, effective rule IDs are qualified as
`<profile-id>#<rule-id>` and `project#<rule-id>` for planning and diagnostics.

## 5. Target language

### 5.1 Root

```yaml
target: root
```

The root target selects the workspace ARC root Dataset. Exactly one expanded
root rule must exist. Its anchor must resolve to exactly one resource on read
and one output on write.

### 5.2 Exact identifier

```yaml
target:
  identifier: special-study
```

An identifier target selects exactly one direct child of the root. The resource
is mandatory on read. The selected or parsed Dataset identifier must equal the
declared identifier.

Two identifier rules must not declare the same value.

### 5.3 Additional type

```yaml
target:
  additionalType: Study
```

An additional-type target selects zero or more direct children of the root by
exact, case-sensitive equality. A child with no matching `additionalType` is not
selected by that rule.

Two additional-type rules must not declare the same value.

### 5.4 Precedence

Identifier targets are reserved before additional-type selection. If a direct
child matches both:

1. the identifier rule selects it;
2. the additional-type rule excludes it; and
3. rule order has no effect.

This permits a general Study layout plus a fixed location for one named Study.
Applying both rules to the same Dataset is not allowed.

Target selectors govern only project-level bindings. Deeper Datasets embedded
inside a selected Dataset remain part of that codec input or result.

## 6. Paths

Paths are workspace-relative `/`-separated templates made from literal whole
segments and the single capture:

```text
{dataset.identifier}
```

Root and identifier targets may use a literal path or the capture. An
additional-type target must contain the capture exactly once so every selected
Dataset has a distinct anchor.

The capture must occupy its whole segment. This is valid:

```text
studies/{dataset.identifier}/isa.study.xlsx
```

This is invalid:

```text
studies/study-{dataset.identifier}.xlsx
```

Paths reject absolute, drive-qualified, UNC, URI, empty, `.`, `..`, NUL, and
backslash-containing forms. Resolved anchors must remain inside the workspace.

On read, a captured identifier must equal the parsed Dataset identifier. On
write, the selected Dataset identifier renders the capture. Normalized anchor
collisions are errors before codec execution.

Auxiliary paths contain only literal safe segments and are relative to the
resolved anchor's directory. Auxiliary IDs are unique within a rule. Resolved
auxiliary paths participate in confinement and collision checks alongside
anchors.

## 7. Codec boundary

Every rule names one exact registered capability ID. Filename extensions,
content sniffing, media types, and rule order do not select another codec.

Every registered codec used by this language must read and write a complete
Dataset through one primary resource plus its declared auxiliary resource map.
The filesystem layer supplies existing auxiliary resources by logical ID and
writes only declared codec outputs. Auxiliary files are optional on read.
Undeclared codec outputs are errors.

The codec context carries the identifiers of direct root children represented by
prepared non-root bindings. Root codecs must avoid writing competing complete
inline copies of those children. The standard `dataset.yml` codec filters them
from the root document's top-level `hasPart` without mutating the in-memory
Dataset. It writes child Datasets completely, including inline `dataContexts`.

`CodecRegistry.standard` is the built-in capability set, not a declaration
that an ISA layout is the standard ARC representation. The most basic storage
profile is a single root `dataset.yml` rule writing the complete ARC to
`arc.yml`.

On read, an external child replaces an inline root child with the same
identifier as a whole Dataset; no fields are merged. Two external resources
with the same Dataset identifier are rejected.

An auxiliary declaration with `create: empty` is project-managed and is emitted
as a zero-byte file after a successful codec write. Generic project handling
does not delete stale anchors or auxiliary files.

## 8. Profile examples

Profiles select a storage layout and optional decorations. The basic profile is
single-file Dataset YAML; ISA-XLSX and ISA Dataset-YAML are two optional
profiles for ISA-decorated ARCs.

### 8.1 Basic single-file ARC YAML

```yaml
type: ArcWorkspaceProfile
id: arc.yml
version: "1.0"
description: Basic single-file ARC Dataset YAML

rules:
  - id: arc
    codec: dataset.yml
    target: root
    path: arc.yml
```

With no child bindings, the complete nested Dataset graph and its
`dataContexts` remain in `arc.yml`.

### 8.2 ISA-XLSX decoration scaffold

One possible profile for an ISA-decorated ARC is:

```yaml
type: ArcWorkspaceProfile
id: arc.isa.xlsx.scaffold
version: "1.0"
description: ISA-XLSX decoration scaffold

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
    files:
      - id: datamap
        path: isa.datamap.xlsx
      - id: resources-placeholder
        path: resources/.gitkeep
        create: empty
      - id: protocols-placeholder
        path: protocols/.gitkeep
        create: empty

  - id: assay
    codec: isa.assay.xlsx
    target:
      additionalType: Assay
    path: "assays/{dataset.identifier}/isa.assay.xlsx"
    files:
      - id: datamap
        path: isa.datamap.xlsx
      - id: dataset-placeholder
        path: dataset/.gitkeep
        create: empty
      - id: protocols-placeholder
        path: protocols/.gitkeep
        create: empty

  - id: workflow
    codec: isa.workflow.xlsx
    target:
      additionalType: Workflow
    path: "workflows/{dataset.identifier}/isa.workflow.xlsx"
    files:
      - id: datamap
        path: isa.datamap.xlsx
      - id: protocols-placeholder
        path: protocols/.gitkeep
        create: empty

  - id: run
    codec: isa.run.xlsx
    target:
      additionalType: Run
    path: "runs/{dataset.identifier}/isa.run.xlsx"
    files:
      - id: datamap
        path: isa.datamap.xlsx
      - id: dataset-placeholder
        path: dataset/.gitkeep
        create: empty
      - id: protocols-placeholder
        path: protocols/.gitkeep
        create: empty
```

Datamap resources are optional codec-managed files. Dataset, resources, and
protocols placeholders are project-managed empty files. The profile may be
stored locally or published at an HTTP(S) URL.

### 8.3 ISA Dataset-YAML decoration scaffold

The same optional ISA decoration layout can use Dataset YAML instead of
workbooks:

```yaml
type: ArcWorkspaceProfile
id: arc.isa.yml.scaffold
version: "1.0"
description: ISA Dataset-YAML decoration scaffold

rules:
  - id: investigation
    codec: dataset.yml
    target: root
    path: isa.investigation.yml

  - id: study
    codec: dataset.yml
    target:
      additionalType: Study
    path: "studies/{dataset.identifier}/isa.study.yml"
    files:
      - id: resources-placeholder
        path: resources/.gitkeep
        create: empty
      - id: protocols-placeholder
        path: protocols/.gitkeep
        create: empty

  - id: assay
    codec: dataset.yml
    target:
      additionalType: Assay
    path: "assays/{dataset.identifier}/isa.assay.yml"
    files:
      - id: dataset-placeholder
        path: dataset/.gitkeep
        create: empty
      - id: protocols-placeholder
        path: protocols/.gitkeep
        create: empty

  - id: workflow
    codec: dataset.yml
    target:
      additionalType: Workflow
    path: "workflows/{dataset.identifier}/isa.workflow.yml"
    files:
      - id: protocols-placeholder
        path: protocols/.gitkeep
        create: empty

  - id: run
    codec: dataset.yml
    target:
      additionalType: Run
    path: "runs/{dataset.identifier}/isa.run.yml"
    files:
      - id: dataset-placeholder
        path: dataset/.gitkeep
        create: empty
      - id: protocols-placeholder
        path: protocols/.gitkeep
        create: empty
```

The codec stores Datamap entries directly as Dataset `dataContexts`, so the
profile needs no `datamap` auxiliary declarations. Project-managed placeholders
remain identical to the ISA-XLSX layout.

## 9. Validation and examples

The normative specification and schemas should reject:

- missing or duplicate root rules after profile expansion and local
  replacement;
- duplicate identifier or additional-type targets;
- missing or incompatible codecs;
- unknown project, profile, reference, target, or rule fields;
- type paths without `{dataset.identifier}`;
- repeated or partial-segment captures;
- unsafe paths and anchor collisions;
- duplicate auxiliary IDs, unsafe auxiliary paths, and anchor/auxiliary
  collisions;
- undeclared codec outputs;
- duplicate Dataset identifiers across decoded non-root resources;
- missing root or identifier resources;
- parsed identifier/capture mismatches; and
- parsed `additionalType` mismatches.

Examples should cover:

- the basic single-file `arc.yml` profile;
- an ISA decoration scaffold loaded from a file or URL;
- the ISA Dataset-YAML scaffold with inline `dataContexts`;
- local and URL profile references plus project-local rules;
- whole-rule replacement by local target while unrelated profile rules remain;
- repeated-source and duplicate-profile rejection;
- an exact identifier at a literal path;
- an exact identifier using the capture;
- identifier precedence over a general type rule; and
- recursive YAML through the basic single-file `arc.yml` rule; and
- external-child replacement of a duplicate inline root child.

## 10. Deliverables

The rewrite must keep these artifacts synchronized:

1. this non-normative project-file plan;
2. the normative project-file specification;
3. the project-file handling plan;
4. the project JSON Schema expressed as YAML; and
5. the workspace-profile JSON Schema expressed as YAML.

The schema captures structural validity. Profile resolution, exact target
uniqueness, inferred multiplicity, codec lookup, path rendering, resource
existence, and concrete collision checks remain semantic validation.
