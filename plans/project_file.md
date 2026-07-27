# Plan for the ARC workspace project file specification

Status: specification-planning document

Planned project-file location: `.arc/project.yml`

Target specification:
[`docs/spec/project_file.md`](../docs/spec/project_file.md)

Companion handling plan:
[`plans/project_file_handling.md`](project_file_handling.md)

## 1. Summary

The project file maps complete ARC `Dataset` values to registered bidirectional
codecs at safe workspace-relative anchor paths.

The language has one small rule shape:

```yaml
- id: study
  codec: isa.study.xlsx
  target:
    additionalType: Study
  path: "studies/{dataset.identifier}/isa.study.xlsx"
```

A rule selects:

- the root Dataset;
- one exact direct child by `identifier`; or
- zero or more direct children by exact `additionalType`.

Each selected Dataset is one codec invocation. The Dataset may itself contain
deeper nested Datasets. Whether a codec serializes that nesting, splits Dataset
state across related files, or uses one physical document is codec behavior and
not project-file syntax.

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
- select a bidirectional codec by exact registered ID;
- allow reusable local or URL-hosted workspace profiles;
- reject ambiguous targets and anchor-path collisions deterministically; and
- represent the established ISA-XLSX scaffold with five rules.

### 2.2 Non-goals

The project file does not configure:

- storage of scientific payloads referenced by `Data`;
- project-level tree, shallow, contribution, overlay, or facet ownership;
- separate read and write formats;
- optional exact targets or explicit cardinality;
- nested project-level target selectors;
- arbitrary predicates, graph queries, globs, or expression languages;
- profile parameters, rule overrides, codec options, or extension fields;
- package-registry profiles or dynamic codec loading;
- generic management, collision analysis, or reporting of codec companion files;
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
safe anchor path + exact bidirectional codec
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
: A reusable, versioned declarative collection of the same four-field rules.

**Workspace profile reference**
: A `file` or `url` reference to an `ArcWorkspaceProfile`.

**Rule**
: A mapping among rule identity, codec identity, Dataset target, and anchor
  path.

**Anchor path**
: The project-visible path used to discover and address one codec invocation.
  A codec may privately derive companion resources from it.

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

`workspaceProfiles` and `rules` are both optional, but their expanded rule set
must contain exactly one root rule.

A workspace-profile reference contains exactly one confined local `file` or
absolute HTTP(S) `url`. The loaded YAML must be an `ArcWorkspaceProfile`.
Profiles are expanded in listed order, followed by project-local rules.

### 4.2 Workspace profile

```yaml
type: ArcWorkspaceProfile
id: arc.isa.xlsx.scaffold
version: "1.0"
description: Established ISA-XLSX ARC scaffold

rules: []
```

Profile `id` and `version` identify the profile document. They are not repeated
on the reference. Profile IDs must be unique in one project.

Profiles have no parameters or extension points. Projects have no overrides.

### 4.3 Rule

```yaml
- id: study
  codec: isa.study.xlsx
  target:
    additionalType: Study
  path: "studies/{dataset.identifier}/isa.study.xlsx"
```

All four fields are required. Unknown fields are errors.

Rule IDs are unique within their declaring project or profile. Expanded rule
IDs are qualified as `<profile-id>#<rule-id>` and `project#<rule-id>` for
planning and diagnostics.

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

## 7. Codec boundary

Every rule names one exact registered capability ID. Filename extensions,
content sniffing, media types, and rule order do not select another codec.

Every registered codec used by this language must read and write a complete
Dataset through the same anchor path.

The project-visible anchor is not necessarily the codec's only physical file.
For example, an ISA-XLSX codec may derive an adjacent `isa.datamap.xlsx`, enrich
the Dataset while reading, and emit or update it while writing. Such companions:

- are not separate project targets or facets;
- are not included in generic project collision analysis;
- are not listed as separate generic resource outcomes;
- are not automatically deleted by project handling; and
- remain the registered codec's safety and consistency responsibility.

## 8. Standard ISA-XLSX profile

The standard ISA-XLSX profile definition is:

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

Datamap resources are part of these codec representations and are intentionally
absent from the profile rules. The profile may be stored locally or published
at an HTTP(S) URL.

## 9. Validation and examples

The normative specification and schemas should reject:

- missing or duplicate root rules after expansion;
- duplicate identifier or additional-type targets;
- missing or incompatible codecs;
- unknown project, profile, reference, target, or rule fields;
- type paths without `{dataset.identifier}`;
- repeated or partial-segment captures;
- unsafe paths and anchor collisions;
- missing root or identifier resources;
- parsed identifier/capture mismatches; and
- parsed `additionalType` mismatches.

Examples should cover:

- the standard scaffold loaded from a file or URL;
- local and URL profile references plus project-local rules;
- repeated-source and duplicate-profile rejection;
- an exact identifier at a literal path;
- an exact identifier using the capture;
- identifier precedence over a general type rule; and
- recursive YAML through a local root rule.

## 10. Deliverables

The rewrite must keep these artifacts synchronized:

1. this non-normative project-file plan;
2. the normative project-file specification;
3. the companion handling plan;
4. the project JSON Schema expressed as YAML; and
5. the workspace-profile JSON Schema expressed as YAML.

The schema captures structural validity. Profile resolution, exact target
uniqueness, inferred multiplicity, codec lookup, path rendering, resource
existence, and concrete collision checks remain semantic validation.
