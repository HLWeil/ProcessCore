---
title: Property Values And Annotation Slots
category: Core Implementation
categoryindex: 3
index: 7
---

# Property Values And Annotation Slots

`PropertyValue` is the main extension and annotation shape in ProcessCore. It carries a name, optional value, optional unit, optional ontology references, and an optional `AdditionalType` discriminator.

The same type is used in several slots. The slot explains what the value annotates.

| Slot | Meaning | Typical `AdditionalType` |
|------|---------|--------------------------|
| `Dataset.AdditionalProperty` | Dataset-level metadata | Profile-specific |
| `LabProcess.ParameterValue` | Runtime parameter value on a concrete process | `ParameterValue` |
| `Material.AdditionalProperty` on an input | Characteristic of the input material | `CharacteristicValue` |
| `Data.AdditionalProperty` on an input | Characteristic of the input data object | `CharacteristicValue` |
| `Material.AdditionalProperty` on an output | Factor or observed output annotation | `FactorValue` |
| `Data.AdditionalProperty` on an output | Factor or observed output annotation | `FactorValue` |
| `LabProtocol.LabEquipment` | Equipment, reagent, software, or component used by the protocol | `Component` |
| `LabProtocol.AdditionalProperty` | Protocol-level metadata | Profile-specific |

## AdditionalType

`AdditionalType` is deliberately lightweight. It is a discriminator used by table projection and query examples, not a separate class hierarchy.

Common values:

- `ParameterValue`: value measured or chosen for one process run.
- `CharacteristicValue`: annotation of an input node.
- `FactorValue`: annotation of an output node.
- `Component`: protocol equipment, reagent, software, or instrument.

## Query Behavior

Property-value traversal gathers values from multiple slots:

- Process parameter values.
- Input and output node additional properties.
- Protocol lab equipment/components.

That means `UpstreamPropertyValues` and `DownstreamPropertyValues` answer provenance questions such as "which conditions and components are connected to this result?" rather than only "which fields are stored on this exact node?"

The [querying walkthrough](querying.fsx) shows this behavior on a profile-shaped assay. The [table walkthrough](tables.fsx) shows how table columns map back into these slots.

## What To Use When

| Task | API |
|------|-----|
| Store process parameters | `LabProcess.AddParameterValue` |
| Store input characteristics | `Material.AddAdditionalProperty`, `Data.AddAdditionalProperty` |
| Store output factors | `Material.AddAdditionalProperty`, `Data.AddAdditionalProperty` |
| Store protocol components | `LabProtocol.AddLabEquipment` |
| Convert property values to table cells | `ProcessCore.Table.TableAux.PVToCell` |
| Query all connected values | `AllPropertyValues`, `UpstreamPropertyValues`, `DownstreamPropertyValues` |
