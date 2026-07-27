---
title: ARC Workspace Project File Handling
category: Specification
categoryindex: 3
index: 6
---

# ARC Workspace Project File Handling

## Status and scope

This document specifies how processors and registered codecs execute the
project-file language defined in [ARC Workspace Project File](project_file.md).

It covers project resolution, compilation, Dataset target selection, anchor
processing, codec invocation, graph attachment, diagnostics, and public ARC
facade integration.

The key words **MUST**, **MUST NOT**, **REQUIRED**, **SHALL**, **SHALL NOT**,
**SHOULD**, **SHOULD NOT**, **RECOMMENDED**, **NOT RECOMMENDED**, **MAY**, and
**OPTIONAL** in this document are to be interpreted as described in
[BCP 14](https://www.rfc-editor.org/info/bcp14) when, and only when, they appear
in all capitals.

## 1. Conformance

### 1.1 Terms

**Compiled rule**
: A structurally and semantically validated rule with qualified identity,
  resolved bidirectional codec, target, and compiled anchor template.

**Binding**
: The association among a compiled rule, selected or discovered Dataset,
  captures, and normalized anchor.

**Reserved identifier**
: An identifier declared by an exact identifier target and therefore excluded
  from all additional-type bindings.

**Workspace session**
: In-memory state retaining the compiled plan, ARC graph, anchor bindings,
  outcomes, and diagnostics for an operation.

### 1.2 Processor conformance

A conforming processor MUST:

- strictly decode project and profile documents;
- resolve only explicitly referenced profile files and HTTP(S) URLs;
- select codecs by exact registered capability ID;
- require every selected codec to be bidirectional;
- implement deterministic target precedence and inferred multiplicity;
- validate and confine every project-visible anchor;
- reject anchor collisions before codec execution;
- attach project-selected children directly to the root;
- preserve deeper nesting returned by codecs;
- report structured outcomes and diagnostics; and
- perform no automatic stale-output deletion.

### 1.3 Codec conformance

A conforming codec MUST:

- be registered explicitly under one unique capability ID;
- read one complete Dataset from an anchor;
- write one complete Dataset to the same anchor convention;
- return expected failures in a form the processor can diagnose;
- keep all anchor and derived companion access inside the supplied workspace;
- avoid dynamic executable configuration from project data; and
- document any opaque companion-resource behavior.

Codec behavior beyond the project-visible anchor is subject to the trust
boundary in section 3.

## 2. Project and profile resolution

### 2.1 Project discovery

A caller MAY supply a workspace root or the exact `.arc/project.yml` path.

When given a workspace root, a processor MUST inspect exactly:

```text
<workspace-root>/.arc/project.yml
```

It MUST NOT search ancestor directories.

The root project and local referenced profiles are configuration. Project
handling MUST NOT create, rewrite, or delete them implicitly.

### 2.2 Workspace-profile resolution

A reference contains exactly one `file` or `url`.

A `file` is resolved relative to the root project's `.arc` directory and MUST
remain inside it. A `url` MUST be an absolute HTTP(S) URL. The loaded YAML MUST
conform as `ArcWorkspaceProfile`; a load, parse, or document-type failure is a
compilation error.

Profiles contribute rules in reference order, followed by project-local rules.
Profile IDs MUST be unique in the expanded profile set.

## 3. Codec contract

### 3.1 Registry

The embedding application constructs the codec registry explicitly. Duplicate
codec keys MUST be rejected. Profiles are loaded from declared `file` or `url`
references.

Project configuration MUST NOT load an assembly, package, module, plugin,
callback, expression, script, or shell command.

A missing codec capability is a fatal compilation error.

### 3.2 Bidirectional Dataset contract

Every rule maps one complete Dataset to one codec anchor. A codec interface MUST
provide logical operations equivalent to:

```text
read Dataset from anchor
write Dataset to anchor
```

The compiler MUST reject a registered capability that cannot perform both
operations.

A read returns one detached Dataset or one structured failure. A write receives
the selected canonical Dataset and returns success or one structured failure.

A codec MAY preserve or serialize nested Datasets contained within its Dataset.
The project processor MUST NOT impose shallow serialization.

The processor MUST catch unexpected codec exceptions at the invocation boundary
and convert them to structured anchor failures.

### 3.3 Opaque companion resources

The anchor is the only codec path visible to generic project planning.

A registered codec MAY derive additional paths from the anchor and MAY read,
write, replace, or omit those representation-specific companion resources. For
example, an ISA-XLSX codec may derive an adjacent `isa.datamap.xlsx`.

Generic handling:

- validates and reports only the anchor;
- does not bind or inspect companions;
- does not provide companion collision or bundle-atomicity guarantees; and
- does not delete stale anchors or companions.

The codec MUST confine every derived path to the workspace. It SHOULD avoid
truncating an existing resource until replacement content is complete, but this
specification does not guarantee a transaction across opaque resources.

Companion files are not scientific payloads merely because they are opaque to
the project plan. The codec remains responsible for distinguishing its metadata
representation from `Data` payloads.

### 3.4 Standard ISA-XLSX capabilities

A standard registry SHOULD provide:

```text
isa.investigation.xlsx
isa.study.xlsx
isa.assay.xlsx
isa.workflow.xlsx
isa.run.xlsx
```

Each capability reads and writes the complete corresponding Dataset
representation, including the ISA-XLSX Dataset/Datamap split where applicable.

No recursive-YAML capability ID is standardized here.

## 4. Compilation

### 4.1 Required pipeline

A processor MUST compile in this order:

1. parse and strictly validate the project;
2. load and strictly validate every referenced profile;
3. expand profile rules in reference order, followed by project-local rules;
4. qualify rule IDs and reject duplicate identities;
5. resolve every codec and verify bidirectionality;
6. parse and validate targets and anchor templates;
7. require exactly one root rule;
8. reserve exact identifiers and reject duplicate targets or static anchor
   collisions; and
9. produce an immutable compiled plan.

Any error is fatal. No codec may be invoked after failed compilation.

Rules in the project are qualified as `project#<rule-id>`. Profile rules use
`<profile-id>#<rule-id>`.

### 4.2 Target maps

The compiled plan MUST contain:

- one root rule;
- a unique map from declared identifier to exact rule;
- a unique map from declared `additionalType` to type rule; and
- the set of all exact identifiers.

An identifier target and an additional-type target are not conflicting target
domains. The reserved identifier set makes their concrete bindings disjoint.

### 4.3 Anchor compilation

The path compiler accepts only literal segments and at most one complete
`{dataset.identifier}` segment.

It MUST reject:

- absolute, drive-qualified, UNC, and URI paths;
- backslashes;
- empty, `.`, and `..` segments;
- NUL;
- unsupported or partial captures;
- repeated captures; and
- normalization outside the workspace.

An additional-type rule MUST contain the capture. Root and identifier rules MAY
use a literal path or the capture.

For read planning:

- a literal root or identifier anchor is checked directly;
- an identifier capture is rendered with the declared identifier and checked
  directly;
- a captured root template is discovered and MUST yield exactly one anchor; and
- an additional-type template discovers zero or more anchors.

For write planning, every capture is rendered from the selected Dataset
identifier.

### 4.4 Determinism

Compilation, discovery, selection, execution, outcomes, and diagnostics MUST use
this ordering:

1. root;
2. exact identifier by ordinal identifier comparison;
3. additional type by ordinal type comparison;
4. normalized anchor by ordinal comparison; and
5. qualified rule ID as a final tie-breaker.

Profile or rule declaration order MUST NOT affect target precedence.

## 5. Read processing

### 5.1 Binding discovery

A read planner MUST:

1. resolve exactly one mandatory root anchor;
2. resolve exactly one mandatory anchor for every identifier rule;
3. discover every additional-type anchor;
4. decode every capture as a safe path segment;
5. discard a type binding whose captured identifier is reserved;
6. reject duplicate bindings and normalized anchor collisions; and
7. produce the deterministic binding order from section 4.4.

Discovery of a reserved identifier through a general type path MUST NOT satisfy
the exact identifier rule. The exact rule's own anchor remains mandatory.

A type rule with no discovered anchors is valid.

### 5.2 Root

The processor invokes the root codec first.

The codec MUST return one Dataset. When the root template captured an
identifier, the returned Dataset identifier MUST equal it.

The returned Dataset is promoted or copied into an `ARC` without discarding
model fields or nested Datasets.

If the root anchor is absent, ambiguous, or fails, the load result has no usable
ARC. Other Dataset bindings are not attached.

### 5.3 Exact identifier resources

For every identifier rule:

The anchor is mandatory and its codec MUST return one Dataset whose identifier
equals the declared identifier and any path capture. A valid result is attached
directly to the root. Absence, ambiguity, or mismatch fails the binding without
allowing a type rule to claim the reserved identifier.

### 5.4 Additional-type resources

For every non-reserved discovered binding:

The codec MUST return one Dataset whose identifier equals the capture and whose
present `additionalType` exactly equals the rule value. A valid result is
attached directly to the root; a mismatch fails without renaming or retargeting
the Dataset.

### 5.5 Attachment and nested Datasets

Successful project-level children MUST be attached through the established graph
APIs so that reciprocal relations, process ownership, and canonical references
remain valid.

The processor MUST preserve deeper Datasets already nested inside a codec result.
It MUST NOT flatten them merely because project-level selectors address only
direct root children.

The processor MUST reject a graph attachment that would violate Dataset identity
or graph invariants.

### 5.6 Independent failures

After the root exists, failure of one child binding MUST NOT prevent independent
child bindings from being attempted.

A failed binding MUST NOT attach a partially parsed Dataset.

The load result MUST retain all successful and failed anchor outcomes. Exact
identifier failures are errors even when a usable partial ARC can be returned by
a result-oriented API.

## 6. Canonical identity and compatible union

Independently parsed Datasets may reference shared model entities. The processor
MUST reconcile them using these keys:

| Entity | Canonical key |
|---|---|
| `Sample` | name |
| `Data` | path plus fragment selector |
| `Recipe` | name plus version |

Processes remain distinct model objects.

For matching canonical and incoming entities:

- an absent canonical scalar receives a present incoming value;
- a present canonical scalar is retained when the incoming value is absent;
- equal values are retained without warning;
- unequal present values retain the canonical value and produce
  `MERGE_CONFLICT`;
- collections are stable-unioned by their established identity or equality;
- compatible map-like dynamic properties are recursively merged; and
- incompatible dynamic values retain the canonical value and produce
  `MERGE_CONFLICT`.

Merge conflicts SHOULD be warnings by default. A strict caller MAY treat them as
errors without changing the deterministic first-value-preserving result.

Project/profile configuration and storage bindings MUST NOT be inserted into
model dynamic properties.

## 7. Write processing

### 7.1 Target selection

A write planner MUST:

1. bind the root rule to the root Dataset;
2. resolve each exact identifier against direct children of the root;
3. report a target error for every missing exact child;
4. select direct children for each additional-type rule using exact,
   case-sensitive equality;
5. exclude every reserved identifier from additional-type bindings;
6. render each anchor;
7. reject unsafe anchors, duplicate Dataset bindings, and normalized anchor
   collisions before codec execution; and
8. order bindings as specified in section 4.4.

A direct child with no applicable exact or type rule is not independently
written by project handling.

### 7.2 Codec execution

For each valid binding, the processor passes the selected complete canonical
Dataset and normalized anchor to the rule's codec.

Independent binding failures MUST NOT prevent remaining valid bindings from
being attempted.

The result records one outcome per anchor. Opaque companion operations are part
of that anchor invocation and are not reported as generic bindings.

Writing is a canonical model-to-representation operation. Generic handling does
not require preservation of YAML comments, workbook styling, physical worksheet
layout, or other syntax not modeled by the codec.

### 7.3 No generic deletion or transaction

Project handling MUST NOT scan for or delete stale anchors or companions, and
MUST NOT claim transactional replacement across opaque codec resources.
Changing a profile or rule does not authorize deletion. A codec MAY update its
own representation-specific companions while writing the selected Dataset.

## 8. Outcomes and diagnostics

### 8.1 Resource outcomes

Processors MUST distinguish at least:

```text
Succeeded
Absent
Failed
SkippedNoRoot
SkippedTarget
```

`Absent` without an error applies only to an additional-type rule with zero
matches. Missing root and identifier resources are errors.

### 8.2 Diagnostic fields

A diagnostic MUST contain:

- stable code;
- `Info`, `Warning`, or `Error` severity;
- a human-readable message; and
- available structured context.

Context SHOULD include the qualified rule ID, codec ID, anchor, declared target,
Dataset identifier, and normalized cause.

Expected failures MUST NOT be represented solely by a platform-specific
exception.

### 8.3 Standard codes

Processors SHOULD use:

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

### 8.4 Results

A load result contains the optional ARC graph and workspace session, ordered
anchor outcomes, and ordered diagnostics. A write result contains the session,
ordered anchor outcomes, and ordered diagnostics.

Result-oriented APIs MAY return a partial ARC after child errors when the root
was successfully materialized.

## 9. Workspace sessions

A workspace session retains:

- workspace root;
- immutable compiled plan;
- loaded profile identities;
- ARC graph;
- current anchor bindings;
- outcomes; and
- diagnostics.

A session does not record per-field provenance or generic companion paths.

An update in the same workspace SHOULD reuse the attached compiled session. An
explicitly different destination is governed by that destination's project.

## 10. ProcessCore ARC facade integration

### 10.1 Generic load

`ARC.load` and `ARC.loadAsync` MUST check the exact workspace root for
`.arc/project.yml` before other representation detection.

When present:

- the project is authoritative;
- the standard codec registry and declared file/URL references are used;
- invalid compilation or root failure MUST NOT fall back to another
  representation; and
- a successful or partial result retains project diagnostics.

When absent, generic loading retains its non-project behavior.

### 10.2 Generic write and update

`Write` and `WriteAsync` MUST use a valid project found at the destination. An
invalid destination project MUST NOT cause another representation to be used.

An update in the attached project workspace SHOULD reuse its workspace session.
When an explicit destination differs, that destination's project governs the
write.

Generic project-aware writes MUST NOT create, rewrite, or delete project/profile
documents and MUST NOT perform stale resource deletion.

### 10.3 Explicit format APIs

Explicit YAML and spreadsheet load/write APIs MUST bypass project discovery and
retain their format-specific behavior.

### 10.4 Rich and strict APIs

ProcessCore SHOULD provide result-returning synchronous and asynchronous
load, write, and update operations. Convenience methods MAY keep their return
types and act as strict wrappers. A strict wrapper SHOULD throw only after
independent bindings complete and retain the structured result in the exception.

## 11. Round-trip properties

For a compatible model `M`, valid project `P`, and deterministic codecs:

```text
read(P, write(P, M)) ≈ selected(P, M)
```

`≈` means:

- equivalent selected Datasets, modeled fields, and process connections;
- equivalent codec-supported nested state; and
- equivalent canonical shared entities after compatible union.

Source formatting, object reference identity, and unmanaged children need not
be preserved.

A failed binding MUST NOT attach a partially parsed Dataset. This specification
does not promise rollback of writes already performed by an opaque codec.

## 12. Required scenarios

Conformance testing SHOULD cover:

- local and HTTP(S) profile loading, including load, parse, and type failures;
- duplicate profile IDs;
- one mandatory root;
- mandatory fixed and captured identifier targets;
- zero and many additional-type resources;
- exact identifier precedence over a general type rule independent of order;
- exclusion of reserved identifiers from general discovery;
- identifier, capture, and `additionalType` mismatches;
- unsafe paths and anchor collisions;
- direct-root attachment with deeper nesting preserved;
- bidirectional use of the same anchor;
- opaque ISA-XLSX Datamap companions;
- compatible-union behavior;
- independent child failures;
- no stale deletion; and
- generic project authority plus explicit-format bypass.
