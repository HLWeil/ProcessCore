# ISA PropertyValue Subtypes

Specializations of [PropertyValue](../../core/PropertyValue.md) used in the ISA decoration. All share the same base structure and differ by `additionalType`.

Reference: [ISA RO-Crate Profile — PropertyValue](../../../references/isa_ro_crate.md)

## Parameter (`ParameterValue`)

Key-value-unit triple representing a process parameter. Attached to LabProcess via `parameterValue`.

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `additionalType` | Text | MUST | `ParameterValue` |
| `name` | Text | MUST | Parameter key name |
| `value` | Text, Number | SHOULD | Parameter value |
| `propertyID` | URL | SHOULD | Key ontology reference |
| `unitCode` | URL | COULD | Unit ontology reference |
| `unitText` | Text | COULD | Unit name |
| `valueReference` | URL | COULD | Value ontology reference |

## Characteristic (`CharacteristicValue`)

Key-value-unit triple representing a material characteristic. Attached to Sample via `additionalProperty`.

Same properties as Parameter, with `additionalType` = `CharacteristicValue`.

## Factor (`FactorValue`)

Key-value-unit triple representing an experimental factor. Attached to Sample via `additionalProperty`.

Same properties as Parameter, with `additionalType` = `FactorValue`.

## Component (`Component`)

Key-value pair representing a protocol component (equipment, reagent, software). Attached to LabProtocol via `labEquipment`, `reagent`, or `computationalTool`.

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `additionalType` | Text | MUST | `Component` |
| `name` | Text | MUST | Component key name |
| `value` | Text, Number | SHOULD | Component value |
| `propertyID` | URL | SHOULD | Key ontology reference |
| `valueReference` | URL | COULD | Value ontology reference |
