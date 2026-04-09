# Sample

ISA specialization of [Material](../../core/Material.md). Represents biological or chemical materials — including both sources (starting materials) and samples (derived materials).

**Schema.org type**: `bioschemas.org/Sample`

Reference: [ISA RO-Crate Profile — Sample](../../../references/isa_ro_crate.md)

## Source vs Sample

In ISA, the distinction between Source and Sample is contextual:
- **Source**: Starting material of a process (no `derivesFrom`)
- **Sample**: Material derived from a source or another sample via a process

Both use the same type (`bioschemas.org/Sample`); the graph position determines the role.

## Properties

Inherits all properties from [Material](../../core/Material.md). ISA-specific refinements:

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `additionalProperty` | [PropertyValue — Characteristic](PropertyValues.md), [PropertyValue — Factor](PropertyValues.md) | SHOULD | Material characteristics or experimental factors |
| `derivesFrom` | Sample | SHOULD | Source material(s) |
