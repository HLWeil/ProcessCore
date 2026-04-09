# ISA Decoration

Maps the ISA (Investigation/Study/Assay) model onto ProcessCore. This decoration enables the representation of experimental metadata following the ISA framework.

Reference: [ISA RO-Crate Profile](../../../references/isa_ro_crate.md)

## Core → ISA Mapping

| Core Type | ISA Specialization | `additionalType` |
|-----------|--------------------|-------------------|
| [Dataset](../../core/Dataset.md) | [Investigation](Investigation.md) | `Investigation` |
| [Dataset](../../core/Dataset.md) | [Study](Study.md) | `Study` |
| [Dataset](../../core/Dataset.md) | [Assay](Assay.md) | `Assay` |
| [Process](../../core/Process.md) | [LabProcess](LabProcess.md) | — |
| [Protocol](../../core/Protocol.md) | [LabProtocol](LabProtocol.md) | — |
| [Material](../../core/Material.md) | [Sample](Sample.md) | `Sample` / `Source` |
| [PropertyValue](../../core/PropertyValue.md) | [PropertyValues](PropertyValues.md) | `ParameterValue` / `CharacteristicValue` / `FactorValue` / `Component` |

## Examples

- [investigation.yml](../../../examples/isa/investigation.yml)
- [assay_proteomics.yml](../../../examples/isa/assay_proteomics.yml)
- [datamap_proteomics.yml](../../../examples/isa/datamap_proteomics.yml)
