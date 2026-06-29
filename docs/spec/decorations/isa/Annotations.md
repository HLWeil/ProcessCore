---
title: ISA Annotation Subtypes
category: ISA Decoration
categoryindex: 5
index: 10
---

# ISA Annotation Subtypes

Specializations of [Annotation](../../core/Annotation.md) used in the ISA decoration. All share the same base structure and differ by `additionalType`.

Reference: [ISA RO-Crate Profile — Annotation](../../../../references/isa_ro_crate.md)

## Parameter (`ParameterValue`)

Key-value-unit triple representing a process parameter. Attached to Process via `parameterValue`.

Same properties as Annotation, with `additionalType` = `ParameterValue`.

## Characteristic (`CharacteristicValue`)

Key-value-unit triple representing a sample characteristic. Attached to Sample via `additionalProperty`.

Same properties as Annotation, with `additionalType` = `CharacteristicValue`.

## Factor (`FactorValue`)

Key-value-unit triple representing an experimental factor. Attached to Sample via `additionalProperty`.

Same properties as Annotation, with `additionalType` = `FactorValue`.

## Component (`Component`)

Key-value pair representing a protocol component (equipment, reagent, software). Attached to Recipe via `labEquipment`, `reagent`, or `computationalTool`.

Same properties as Annotation, with `additionalType` = `Component`.