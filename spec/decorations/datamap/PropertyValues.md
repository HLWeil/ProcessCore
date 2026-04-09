# Datamap PropertyValue Subtypes

Specializations of [PropertyValue](../../core/PropertyValue.md) used in the Datamap decoration.

Reference: [ARC Datamap RO-Crate Profile — Fragment Description](../../../references/arc_datamap_ro_crate.md)

## Fragment Descriptor (`FragmentDescriptor`)

PropertyValue carrying the contextual description of a [Data](../../core/Data.md) object or a selected fragment within one. It is typically attached to [Data](../../core/Data.md) via `additionalProperty` and represents the flexible, graph-native counterpart of [DataContext](../../core/DataContext.md).

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `additionalType` | Text | MUST | `FragmentDescriptor` |
| `name` | Text | SHOULD | Stable descriptor label, typically `FragmentDescriptor` |
| `subjectOf` | Text, URL | SHOULD | Identifier of the Data object or selected fragment being described |
| `selector` | Text | COULD | Fragment selector relative to the attached Data object when no explicit fragment Data node exists |
| `selectorFormat` | URL | COULD | Formal description of the selector syntax |
| `value` | Text | SHOULD | Explication of the fragment contents |
| `valueReference` | URL | COULD | Ontology reference for `value` |
| `alternateName` | Text | COULD | Short label such as a column header |
| `objectType` | Text | COULD | Expected value shape or entry type of the fragment |
| `objectTypeTAN` | URL | COULD | Ontology reference for `objectType` |
| `unitText` | Text | COULD | Human-readable unit |
| `unitCode` | URL | COULD | Unit ontology reference |
| `measurementMethod` | Text | COULD | Tool or method that generated the described data |
| `description` | Text | COULD | Additional free-text details |

## Mapping From `DataContext`

The datamap decoration preserves the DataContext authoring semantics with the following correspondence:

| DataContext field | Fragment Descriptor field |
|-------------------|---------------------------|
| `path` or `data` | attachment target Data object |
| `selector` | `selector` or explicit fragment `subjectOf` |
| `selectorFormat` | `selectorFormat` |
| `explication` | `value` |
| `explicationTAN` | `valueReference` |
| `label` | `alternateName` |
| `objectType` | `objectType` |
| `objectTypeTAN` | `objectTypeTAN` |
| `unit` | `unitText` |
| `unitTAN` | `unitCode` |
| `generatedBy` | `measurementMethod` |
| `description` | `description` |
