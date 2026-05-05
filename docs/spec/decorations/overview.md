---
title: Decorations
category: Specification
categoryindex: 8
index: 3
---

# Decorations

Decorations are domain-specific extensions of ProcessCore. They specialize core types through `additionalType`, define decoration-specific properties, and add entities only where the domain needs concepts that do not have a direct ProcessCore counterpart.

## Available Decorations

| Decoration | Domain |
|------------|--------|
| [ISA](isa/overview.md) | Investigation, Study, Assay, Source, Sample, and ISA property-value roles |
| [Workflow Run](workflow-run/overview.md) | Workflow and Run datasets, workflow protocols, and workflow invocations |
| [Datamap](datamap/overview.md) | Datamap datasets and DataContext annotations for file fragments |

## Extension Rules

- Do not modify ProcessCore entities for a decoration.
- Use `additionalType` to discriminate specializations.
- Prefer `additionalProperty` for extensible metadata that can compose across decorations.
- Add decoration-specific entities only when the concept does not fit a core type.
