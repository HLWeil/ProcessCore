# ISA PropertyValue Subtypes

Specializations of [PropertyValue](../../core/PropertyValue.md) used in the ISA decoration. All share the same base structure and differ by `additionalType`.

Reference: [ISA RO-Crate Profile — PropertyValue](../../../references/isa_ro_crate.md)

## Parameter (`ParameterValue`)

Key-value-unit triple representing a process parameter. Attached to LabProcess via `parameterValue`.

Same properties as PropertyValue, with `additionalType` = `ParameterValue`.

## Characteristic (`CharacteristicValue`)

Key-value-unit triple representing a material characteristic. Attached to Sample via `additionalProperty`.

Same properties as PropertyValue, with `additionalType` = `CharacteristicValue`.

## Factor (`FactorValue`)

Key-value-unit triple representing an experimental factor. Attached to Sample via `additionalProperty`.

Same properties as PropertyValue, with `additionalType` = `FactorValue`.

## Component (`Component`)

Key-value pair representing a protocol component (equipment, reagent, software). Attached to LabProtocol via `labEquipment`, `reagent`, or `computationalTool`.

Same properties as PropertyValue, with `additionalType` = `Component`.