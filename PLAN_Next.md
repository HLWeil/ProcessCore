# Plan

## Recommended Modeling Direction

- Keep `DataContext` as a documented core authoring concept, because the repo already has `spec/core/DataContext.md` and a standalone datamap example in `examples/isa/datamap_proteomics.yml`.
- Add a second, more schema-native representation where the same semantics are expressed as a `PropertyValue` decoration attached to `Data` via `additionalProperty`.
- Treat the `PropertyValue`-based form as the flexible extension path, and describe the core `DataContext` form as a convenience model that can be mapped into it.
- Generalize `additionalProperty` as the standard extension hook for all ProcessCore entity types, not just `Dataset` and `Material`.

## Work Packages

### 1. Define `DataContext` in the core spec

- Expand [spec/core/DataContext.md](spec/core/DataContext.md) from a placeholder into a stub spec that explains:
  - what `DataContext` is for,
  - that it is represented as a root-level collection in the current examples,
  - that it describes the shape, content, and identity of a full data object or a selected fragment of one.
- Base the property list on `references/arc_datamap_ro_crate.md` and `examples/isa/datamap_proteomics.yml`, including at least:
  - data target (`path` or nested `data`),
  - selector,
  - selector format,
  - encoding format,
  - explication,
  - label,
  - object type,
  - unit,
  - description,
  - generator / provenance-like fields.
- Add explicit mapping notes showing which fields correspond to RO-Crate fragment concepts such as `about`, `subjectOf`, `usageInfo`, `pattern`, and fragment description `PropertyValue`s.

### 2. Add a datamap decoration based on `PropertyValue`

- Create a new decoration section for datamap metadata, most likely under `spec/decorations/datamap/`.
- Add a decoration overview that explains the alternate representation:
  - `Data` keeps file-level identity,
  - fragment- or content-level metadata is attached through `additionalProperty`,
  - the attached object is a specialized `PropertyValue` representing a data context / fragment descriptor.
- Add a stub for the datamap-specific `PropertyValue` subtype.
- Document how this decoration maps to the current datamap reference:
  - reference profile: fragment description is a `PropertyValue`,
  - ARC core variant: attach that `PropertyValue` directly to `Data`,
  - optional future refinement: represent selected sub-parts as separate `Data` nodes linked from the parent data object.

### 3. Generalize `additionalProperty` across ProcessCore

- Update the core type docs so every main ProcessCore entity can carry `additionalProperty`.
- Minimum files to touch:
  - [spec/core/Dataset.md](spec/core/Dataset.md) — already present, keep as baseline wording.
  - [spec/core/Material.md](spec/core/Material.md) — already present, keep as baseline wording.
  - [spec/core/Data.md](spec/core/Data.md) — add `additionalProperty` for file- and fragment-level annotations.
  - [spec/core/Process.md](spec/core/Process.md) — add `additionalProperty` for non-parameter annotations.
  - [spec/core/Protocol.md](spec/core/Protocol.md) — add `additionalProperty` for extensible protocol metadata.
  - [spec/core/Person.md](spec/core/Person.md) — add `additionalProperty` for auxiliary person metadata when needed.
  - [spec/core/DefinedTerm.md](spec/core/DefinedTerm.md) — add `additionalProperty` for extensible term annotations if the model wants a universal hook.
- Decide whether `PropertyValue` itself should remain the shared extension payload only, rather than also receiving an `additionalProperty` slot.

### 4. Align overview docs and diagrams

- Update [spec/core/README.md](spec/core/README.md) to:
  - list `DataContext` among the core types if it remains a first-class core concept,
  - show that `additionalProperty` is no longer limited to `Material` and `Dataset`.
- Update [spec/decorations/README.md](spec/decorations/README.md) so it explicitly covers:
  - type specialization through `additionalType`,
  - cross-cutting metadata attachment through `additionalProperty`.
- Update the repository-level [README.md](README.md) if the ProcessCore overview should mention `DataContext` or universal `additionalProperty`.

## Open Decisions To Resolve During Implementation

- Naming: whether the `PropertyValue` subtype should be called `DataContext`, `DataContextValue`, or `FragmentDescriptor`.
- Example shape: whether the canonical core example uses `path` directly or a nested `data` object.
- Scope: whether `DataContext` becomes a fully listed core type in `spec/core/README.md`, or remains a supplemental core-side model for datamap use cases only.
- Selector modeling: whether fragment identity is documented only as selector fields in `DataContext`, or whether the decoration should already introduce fragment `Data` nodes linked from a parent file via `hasPart`.
