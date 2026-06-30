---
title: Decorations
category: Specification
categoryindex: 8
index: 3
---

# Decorations

Decorations are domain-specific extensions of ARC Core. They specialize core types through `additionalType`, define decoration-specific properties, and add entities only where the domain needs concepts that do not have a direct ARC Core counterpart.

## Available Decorations

| Decoration | Domain |
|------------|--------|
| [ISA](isa/overview.md) | Investigation, Study, Assay, Source, Sample, and ISA property-value roles |
| [Workflow Run](workflow-run/overview.md) | Workflow and Run datasets, workflow protocols, and workflow invocations |
| [Datamap](../datamap/overview.md) | Promoted to a sibling profile for data files, fragments, and DataContext descriptors |

## Extension Rules

- Do not modify ARC Core entities for a decoration.
- Use `additionalType` to discriminate specializations.
- Prefer `additionalProperty` for extensible metadata that can compose across decorations.
- Add decoration-specific entities only when the concept does not fit a core type.

