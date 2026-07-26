# Plan for the ARC workspace project file specification

Status: specification-planning document

Planned project-file location: `.arc/project.yml`

Target specification:
[`docs/spec/project_file.md`](../docs/spec/project_file.md)

Companion handling plan:
[`plans/project_file_handling.md`](project_file_handling.md)

## Quick summary

An ARC can currently be stored either as one recursive `arc.yml` document or in
the established ISA-XLSX scaffold. The planned project file makes the physical
layout configurable.

The project file does not contain the ARC metadata itself. It says:

1. which metadata resources belong to the workspace;
2. which dataset or dataset facet each resource represents;
3. which registered format capability reads or writes it; and
4. where the resource is located relative to the workspace root.

The central idea is:

```text
project rule
  = model target
  + contribution kind
  + read/write direction
  + safe path template
  + explicit codec capability
```

A small project can select a built-in layout with a few lines. A specialized
project can list its rules directly. Reusable organizations can publish
declarative workspace profiles.

This document plans what the project-file specification needs to explain and
require. It intentionally leaves compilation, resource loading, graph merging,
writing, diagnostics, and stale-file cleanup to the
[handling plan](project_file_handling.md).

## 1. Why a project file is needed

ARC metadata is a graph of nested `Dataset` objects. A physical representation
does not always have the same shape as that graph:

- one YAML file can contain the complete recursive dataset tree;
- one workbook can represent one shallow dataset;
- a Datamap workbook can add one facet to a dataset described elsewhere; and
- different datasets in one ARC may use different supported formats.

Today, choosing a layout also effectively chooses its reader. That works for the
two established layouts, but it makes mixed or organization-specific layouts
difficult to describe.

The project file separates three concerns:

- the logical ARC graph;
- the physical placement of metadata files; and
- the registered codecs capable of representing those files.

For example, a study could be stored as ISA-XLSX, an assay as shallow YAML, and
the assay's Datamap as a separate workbook. The project file describes those
choices without changing the ARC model.

The project file is therefore a storage map, not:

- another serialization of an `ARC` or `Dataset`;
- a package manifest for scientific payload bytes;
- executable configuration;
- a mechanism for downloading plugins; or
- a replacement for the Process Core, Datamap, or Administrative model profiles.

## 2. Intended readers and use of this plan

The eventual specification should serve two audiences.

A workspace author should be able to answer:

- Where does the project file go?
- Can I use an existing layout?
- How do I describe a custom layout?
- Which files are metadata resources?
- Which datasets or facets do those files represent?
- Is a rule used for reading, writing, or both?

An implementer should be able to derive:

- the complete YAML document shape;
- all identifiers, enums, defaults, and cross-field constraints;
- the meaning of every selector and path capture;
- the ownership and conflict rules;
- the boundaries between project-file semantics and processor behavior; and
- matching JSON Schema artifacts and conformance examples.

The normative specification should remain readable in that order: introduce the
idea first, then the document structure, and only then the precise constraints.
Implementation algorithms should be referenced rather than embedded.

## 3. Goals and boundaries

### 3.1 Goals

The specification should define a project-file language that:

- describes arbitrary safe metadata paths within one local workspace;
- supports both built-in conventions and explicit project-local rules;
- allows reusable declarative workspace profiles;
- makes every format capability explicit;
- supports read-only, write-only, and bidirectional mappings;
- can describe complete trees, shallow datasets, and detachable facets;
- gives writable layouts an unambiguous inverse mapping;
- assigns storage ownership clearly and independently for reading and writing;
- supports deterministic interpretation across operating systems; and
- can represent the existing monolithic YAML and ISA-XLSX layouts.

### 3.2 Non-goals for version 1

The first specification should not define:

- storage or rewriting of scientific payloads referenced by `Data`;
- arbitrary field-by-field partitioning of ARC objects;
- remote project files or remote workspace profiles;
- dynamic assembly, package, module, or script loading;
- executable expressions, callbacks, shell syntax, or general templates;
- nested project manifests or mounted subprojects;
- multiple ARC roots;
- a persisted lockfile or per-field provenance map;
- exact source-format or workbook-format preservation;
- a transaction spanning every output file;
- profile-version ranges; or
- a general archival, object-store, or data-lake format.

These boundaries keep the first language small enough to specify precisely and
safe enough to implement consistently.

## 4. Mental model and vocabulary

The specification should introduce these concepts before presenting YAML.

**Workspace root**
: The directory that contains `.arc`.

**Workspace project**
: The root storage configuration at `.arc/project.yml`.

**Workspace profile**
: A reusable declarative collection of storage rules. This is different from an
  ARC model profile such as Process Core or Datamap.

**Rule**
: One mapping between a model target and a physical metadata resource.

**Target**
: The dataset or dataset group selected by a rule.

**Contribution**
: The portion of the ARC graph represented by one resource.

**Codec capability**
: A stable registered identifier for a supported physical representation. The
  project names capabilities but does not load code.

**Facet**
: A named unit of storage ownership within a dataset.

**Path template**
: A restricted relative path that can match resources when reading and render
  paths when writing.

The specification should distinguish three contribution kinds:

| Kind | Meaning | Typical example |
|---|---|---|
| `tree` | One resource contains a dataset and its recursive descendants | monolithic `arc.yml` |
| `dataset` | One resource contains exactly one shallow dataset | one study workbook |
| `overlay` | One resource contains a named facet of an existing dataset | one Datamap workbook |

Version 1 should define at least these facets:

- `arc.base`: dataset identity, descriptive and administrative metadata,
  processes, data references, and other non-detached fields;
- `arc.datamap`: the dataset's `DataContext` state.

`Sample`, `Data`, and `Recipe` objects required by a base representation are
shared graph references, not independently assignable facets. Their
canonicalization belongs to project-file handling.

## 5. Requirements the specification must preserve

### 5.1 One discoverable root document

Version 1 should use exactly one canonical project file:

```text
<workspace-root>/.arc/project.yml
```

The parent of `.arc` is the workspace root. Local profiles and metadata resources
remain inside that root. Nested projects are deferred.

### 5.2 Declarative and strict YAML

The language should use ordinary YAML mappings, sequences, and scalar values. It
should be strict enough that misspelled instructions are errors.

The specification needs to define:

- exact document type and specification-version fields;
- case sensitivity;
- duplicate-key behavior;
- allowed identifiers and version tokens;
- defaults and required fields;
- the treatment of unknown fields;
- inert `extensions` data; and
- declarative `codecOptions`.

Neither `extensions` nor `codecOptions` should contain executable behavior.

### 5.3 Explicit and direction-specific ownership

For any direction, a dataset base or facet should have at most one owner.

This means:

- a tree owner cannot overlap a dataset owner in its subtree;
- two overlays cannot own the same facet of the same dataset in one direction;
- two writable rules cannot produce the same normalized path; and
- rule order cannot be used to choose between conflicting owners.

Ownership is direction-specific. A read-only legacy rule and a write-only
canonical rule may intentionally describe the same target through different
formats or paths.

### 5.4 Safe and invertible paths

All paths should be relative to a defined base and remain inside the workspace.
The language should reject absolute paths, drive or UNC paths, URI schemes,
empty segments, `.` and `..`, NUL, and traversal outside the workspace.

Writable templates need a unique rendering for every selected target. The
language should avoid unrestricted globs, regular expressions, partial-segment
captures, environment expansion, and general-purpose template engines.

### 5.5 Explicit format capabilities

A rule should identify a codec by an exact stable capability ID, for example:

```text
arc.yaml.tree.v1
arc.yaml.dataset.v1
arc.isa.study.xlsx.v1
arc.isa.datamap.xlsx.v1
```

File extensions, media types, signatures, workbook sheets, and discovery order
must not silently select another codec. Missing or incompatible capabilities are
project validation errors, while registry construction and codec execution
belong in the handling specification.

### 5.6 Compatibility with the ARC model

The project file should describe storage without adding storage configuration to
model dynamic properties. It should preserve the distinction between:

- the logical dataset hierarchy;
- storage rules and bindings; and
- scientific payload references.

The existing monolithic YAML and scaffold layouts should be expressible as
built-in workspace profiles. Generic ARC I/O should discover and honor
`.arc/project.yml` when present, while explicit YAML and spreadsheet APIs remain
independent and generic operations retain their legacy behavior when it is
absent. The companion handling plan defines the API integration details.

## 6. Step-by-step plan for the normative specification

The normative document should be written in the following sequence.

### Step 1: Explain scope, model, and conformance

Start with the short mental model from this plan. State that the project file
maps metadata resources to ARC datasets and facets, while scientific payloads
remain outside its management scope.

Define separate conformance classes for:

- project documents;
- workspace-profile documents; and
- processors or codecs only by reference to their companion contracts.

This prevents reader and writer algorithms from obscuring the file format.

### Step 2: Define location and document identity

Specify the canonical path, workspace-root derivation, local-only scope, and the
root YAML discriminator:

```yaml
type: ArcWorkspaceProject
specVersion: "1.0"
```

The specification should say whether a field is required, optional, or has a
default, but should avoid implementation data structures.

### Step 3: Define the top-level project shape

The planned top-level shape is:

```yaml
type: ArcWorkspaceProject
specVersion: "1.0"

workspaceProfiles: []
overrides: []
rules: []
extensions: {}
```

The specification should explain these fields in this order:

1. `workspaceProfiles` imports reusable declarative layouts;
2. `overrides` makes limited project-specific adjustments;
3. `rules` adds project-local mappings; and
4. `extensions` reserves inert namespaced data for future work.

After profile expansion, overrides, and removal of disabled rules, at least one
rule should remain enabled.

### Step 4: Define workspace profiles

A profile is itself a YAML document with an exact ID and version:

```yaml
type: ArcWorkspaceProfile
specVersion: "1.0"
id: org.example.layout
version: "1.0"

parameters: {}
rules: []
extensions: {}
```

The specification should cover:

- built-in profile references;
- confined local profile references relative to `.arc`;
- exact ID and version matching;
- profile parameter declarations and supplied values;
- deterministic expansion order; and
- restricted overrides.

Version 1 parameters only need scalar types that can be validated safely.
A dedicated `path-segment` type should be used for values inserted into paths.
General strings should not become path fragments.

Overrides may adjust operationally safe fields such as paths, optionality,
cardinality, omission behavior, codec options, or enabled state. They should not
silently change contribution kind, target, facet, direction, or codec identity.
A semantic change should require disabling the inherited rule and adding a new
project-local rule.

### Step 5: Define storage rules

Introduce one representative rule before describing every field:

```yaml
- id: studies
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
  codecOptions: {}
```

The specification should then define:

- stable rule identity and qualification;
- enabled state;
- contribution kind;
- optional overlay facet;
- exact codec capability;
- target selector;
- optional parent attachment;
- directions;
- read settings;
- write settings;
- codec options; and
- extensions.

Project-local and profile rule IDs need unambiguous qualified forms for reports
and references, even though the runtime representation belongs elsewhere.

### Step 6: Define contributions and facets

Explain `tree`, `dataset`, and `overlay` using diagrams or short examples before
giving conflict rules.

The specification should make these points explicit:

- a tree owns the selected root and represented descendants;
- a dataset contribution is shallow and does not embed child datasets;
- hierarchy for shallow datasets comes from selectors and `attachTo`;
- an overlay requires an existing base dataset;
- an overlay names exactly one facet; and
- a codec cannot claim or emit a facet outside the rule's ownership.

The `arc.base` and `arc.datamap` facets should be standardized here. Future facets
require stable identifiers and registered compatible codecs.

### Step 7: Define target and parent selectors

Version 1 only needs a small selector vocabulary:

- `root`;
- `exact`, with a dataset identifier;
- `children`, beneath a named parent;
- `descendants`, beneath a named parent.

`children` and `descendants` may use an exact `additionalType` filter. Parent
references may use `root`, an exact identifier, or a value captured from the
resource path.

The specification should define uniqueness and matching requirements, but leave
the order of parsing and graph mutation to the handling document. Arbitrary
predicates, JSONPath, graph queries, regular expressions, and executable selector
code remain out of scope.

### Step 8: Define the path-template language

The language should consist of slash-separated whole segments:

- literal segments;
- substituted `path-segment` parameters;
- `{dataset.identifier}`; and
- `{parent.identifier}`.

A valid example is:

```text
studies/{dataset.identifier}/isa.study.xlsx
```

A partial capture such as this should be invalid:

```text
studies/study-{dataset.identifier}.xlsx
```

The specification needs to define matching, rendering, normalization, collision
identity, and case behavior at the semantic level. Filesystem walking,
time-of-check/time-of-use protection, staging, and deletion belong in the
handling plan.

### Step 9: Define read and write declarations

Directions should be explicit:

```yaml
directions: [read, write]
```

The allowed combinations are read/write, read-only, and write-only. Direction
order has no meaning.

Read settings should cover:

- a path template;
- whether a resource is required; and
- `one` or `many` cardinality.

Write settings should cover:

- a path template; and
- optional `omitWhenEmpty`, mainly for detachable overlays.

This section should define what each value means for the project language.
Detailed outcomes, best-effort behavior, and file replacement remain in the
handling plan.

### Step 10: Define validation and conflict rules

Collect cross-cutting validity requirements in one chapter so readers do not
have to infer them from field descriptions.

At minimum, the specification should reject:

- duplicate project, profile, or rule identifiers;
- profile ID or version mismatches;
- unknown override targets;
- invalid parameter values;
- missing or incompatible codec capabilities;
- unsafe or non-invertible paths;
- unresolved selectors or parents;
- direction-specific ownership conflicts;
- overlapping tree and dataset ownership;
- duplicate rendered outputs; and
- dependency cycles.

The normative document should distinguish structural schema validation from
semantic validation that requires profiles, a codec registry, or concrete model
targets.

### Step 11: Define built-in profiles and examples

Version 1 should define two built-in profiles:

- `org.arc.monolithic-yaml` version `1.0`;
- `org.arc.scaffold` version `1.0`.

The specification should include their exact declarative rules, not merely refer
to hard-coded behavior. This proves that the new language can describe both
established layouts.

Examples should be informative and cover:

- monolithic YAML;
- the standard scaffold;
- a profile with safe path overrides;
- a fully explicit mixed layout;
- a local workspace profile; and
- read-old/write-new migration rules.

### Step 12: Close with versioning and exclusions

The final chapter should summarize version-1 exclusions and reserve future work
without implying current semantics.

Likely future extensions include:

- persisted provenance or lock information;
- remote or packaged profiles;
- additional typed facets;
- document-database resources;
- content-addressed resources;
- nested projects;
- richer selectors; and
- explicit migration manifests.

Future versions should retain safe path confinement, explicit codec selection,
deterministic ownership, and the separation between project syntax and handling.

## 7. Representative user stories

These stories should guide examples and explanatory prose in the specification.

### 7.1 Use the monolithic YAML layout

A user wants the complete ARC in `arc.yml`. Their project only needs to select
the built-in profile:

```yaml
type: ArcWorkspaceProject
specVersion: "1.0"
workspaceProfiles:
  - id: org.arc.monolithic-yaml
    version: "1.0"
    builtin: org.arc.monolithic-yaml
```

This is the simplest introduction because it shows that the project file can be
small even when the expanded storage plan is precise.

### 7.2 Use the established scaffold

A user wants investigation, study, assay, workflow, run, and Datamap workbooks
at their established locations:

```yaml
type: ArcWorkspaceProject
specVersion: "1.0"
workspaceProfiles:
  - id: org.arc.scaffold
    version: "1.0"
    builtin: org.arc.scaffold
```

The built-in profile owns the detailed rules. The project does not repeat them.

### 7.3 Relocate a profile safely

An organization uses the scaffold semantics but stores study workbooks under
`metadata/studies`. A restricted override changes read and write paths while
leaving target, contribution kind, facet, direction, and codec unchanged.

This example should explain why restricted overrides are convenient and why
semantic overrides are intentionally prohibited.

### 7.4 Mix representations

A project stores:

- its root dataset in shallow YAML;
- studies in study workbooks;
- assays in assay workbooks; and
- assay Datamaps as overlays under a separate `contexts` directory.

This example should demonstrate multiple rules, explicit codecs, selectors,
attachment, facets, and distinct path templates without becoming the first
example a newcomer sees.

### 7.5 Read an old layout and write a new one

A project reads legacy study files from one directory and writes canonical
shallow YAML to another. Two direction-specific rules own the same logical
targets without conflicting.

This example should make direction-specific ownership understandable without
explaining the writer algorithm.

## 8. Specification deliverables

Rewriting the specification from this plan should produce:

1. normative prose at
   [`docs/spec/project_file.md`](../docs/spec/project_file.md);
2. JSON Schema draft 2020-12 artifacts at:
   - `schemas/yml/arc-workspace-project.schema.yml`;
   - `schemas/yml/arc-workspace-profile.schema.yml`;
3. exact declarative definitions for both built-in workspace profiles;
4. conforming and non-conforming YAML examples;
5. a glossary aligned with the ARC model vocabulary;
6. links to the separate handling contracts; and
7. a short migration note for existing YAML and scaffold users.

A review of the finished specification should be able to answer these questions
without reading implementation code:

- What does the project file control?
- What remains part of the ARC model?
- Which resources are managed metadata rather than scientific payloads?
- How are reusable profiles imported and safely customized?
- How does a rule select a dataset or facet?
- How is hierarchy reconstructed declaratively?
- How are read and write ownership distinguished?
- Which paths are valid and invertible?
- How is a codec selected?
- Which conflicts make a project invalid?
- Which features are deliberately deferred?

## 9. Reference map

### 9.1 ARC model and project documentation

Use these sources to keep the project-file language aligned with the current
model:

- [ARC specification index](../docs/spec/index.md)
- [Process Core overview](../docs/spec/process_core/overview.md)
- [Dataset](../docs/spec/process_core/Dataset.md)
- [Data](../docs/spec/process_core/Data.md)
- [Datamap overview](../docs/spec/datamap/overview.md)
- [DataContext](../docs/spec/datamap/DataContext.md)
- [Administrative overview](../docs/spec/administrative/overview.md)
- [Decorations overview](../docs/spec/decorations/overview.md)
- [Implementation guide](../docs/project/implementation.md)
- [Core data-model plan](core_datamodel.md)
- [Project-file handling plan](project_file_handling.md)

### 9.2 Existing layouts and preserved references

These sources describe the representations the first specification must cover:

- [preserved ARC project-file specification reference](https://github.com/HLWeil/ARC-Data-Model/blob/projectfile/references/ARC%20specification.md)
- [ISA-XLSX](../references/ISA-XLSX.md)
- [ISA RO-Crate](../references/isa_ro_crate.md)
- [Workflow Run RO-Crate](../references/arc_wr_ro_crate.md)
- [Datamap RO-Crate](../references/arc_datamap_ro_crate.md)

### 9.3 Selected design precedents

These are design references, not formats that ARC should copy wholesale:

- [Frictionless Data Package](https://specs.frictionlessdata.io/data-package/)
  demonstrates a small declarative resource catalog with profiles.
- [RO-Crate 1.2](https://www.researchobject.org/ro-crate/specification/1.2/)
  distinguishes a logical metadata graph from referenced physical resources.
- [BagIt, RFC 8493](https://datatracker.ietf.org/doc/html/rfc8493) clearly
  separates managed metadata from payload content.
- [W3C Profiles Vocabulary](https://www.w3.org/TR/dx-prof/) provides useful
  language for named reusable conformance profiles.
- [Cargo manifests](https://doc.rust-lang.org/cargo/reference/manifest.html)
  illustrate strict declarative project configuration with stable identifiers.
- [OCI image descriptors](https://github.com/opencontainers/image-spec/blob/main/descriptor.md)
  motivate explicit capability and media metadata instead of format guessing.
- [Intake catalogs](https://intake.readthedocs.io/en/stable/catalog.html) show
  explicit source-to-driver mappings.
- [Apache Iceberg](https://iceberg.apache.org/spec/) provides useful precedent
  for separating logical data from physical partition layout.
- [Oxford Common File Layout 1.1](https://ocfl.io/1.1/spec/) is relevant to safe,
  deterministic file management.
- [Boomerang](https://www.cs.cornell.edu/~jnfoster/papers/boomerang.pdf)
  motivates the need for an invertible read/write mapping.

The final specification should cite a precedent only where it clarifies a design
choice. It should not become a general prior-art survey.

## 10. Decisions the specification should make explicit

The rewrite should preserve these settled design decisions:

1. The project file is `.arc/project.yml`.
2. It describes ARC metadata storage, not scientific payload storage.
3. The physical contribution unit is a dataset tree, a shallow dataset, or a
   typed overlay.
4. `arc.base` and `arc.datamap` are the initial standardized facets.
5. Workspace profiles are built-in or confined local declarative files.
6. Profiles use exact versions in version 1.
7. Project overrides are intentionally restricted.
8. Every rule names an explicit codec capability.
9. Rules can be read/write, read-only, or write-only.
10. Ownership is exclusive per direction, dataset, and facet.
11. Paths use a restricted whole-segment capture language.
12. Version 1 has one workspace root and no nested projects.
13. Version 1 has no persisted lockfile or field provenance.
14. Existing monolithic YAML and scaffold layouts are built-in profiles.
15. Project syntax and resource-handling behavior remain separate documents.

If one of these decisions changes, this plan, the normative specification, the
schemas, examples, and handling plan should be updated together.
