# Datamap Decoration

Maps datamap-style data context annotations onto ProcessCore. This decoration keeps file identity in [Data](../../core/Data.md) and carries fragment- or content-level semantics in specialized [PropertyValue](../../core/PropertyValue.md) objects attached through `additionalProperty`.

Reference: [ARC Datamap RO-Crate Profile](../../../references/arc_datamap_ro_crate.md)

## Core → Datamap Mapping

| Core Type | Datamap Specialization | `additionalType` |
|-----------|------------------------|------------------|
| [Data](../../core/Data.md) | Decorated Data carrying fragment metadata via `additionalProperty` | — |
| [PropertyValue](../../core/PropertyValue.md) | [PropertyValues](PropertyValues.md) | `FragmentDescriptor` |

## Modeling Notes

- The core-side [DataContext](../../core/DataContext.md) concept can be mapped into this decoration by attaching one `FragmentDescriptor` to the referenced [Data](../../core/Data.md) object.
- In this representation, `Data` keeps the file identity while `FragmentDescriptor` carries the fragment selector, semantic description, typing hints, and units.
- When a representation needs explicit fragment nodes, the same decoration can be combined with fragment-level [Data](../../core/Data.md) objects. The attached `FragmentDescriptor` then describes that fragment directly instead of carrying a selector relative to the parent file.
