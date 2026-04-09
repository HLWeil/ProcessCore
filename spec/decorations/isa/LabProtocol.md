# LabProtocol

ISA specialization of [Protocol](../../core/Protocol.md). Describes a planned experimental procedure in the laboratory.

**Schema.org type**: `bioschemas.org/LabProtocol`

Reference: [ISA RO-Crate Profile — LabProtocol](../../../references/isa_ro_crate.md)

## Additional Properties (beyond Protocol)

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `computationalTool` | DefinedTerm, [PropertyValue — Component](PropertyValues.md), SoftwareApplication | COULD | Software used |
| `labEquipment` | DefinedTerm, [PropertyValue — Component](PropertyValues.md), Text | COULD | Lab equipment used |
| `reagent` | BioChemEntity, DefinedTerm, [PropertyValue — Component](PropertyValues.md), Text | COULD | Reagents used |
| `comment` | Comment | COULD | Comments |
