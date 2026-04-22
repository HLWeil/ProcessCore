# Assay

ISA specialization of [Dataset](../../core/Dataset.md). Represents a specific analytical measurement or experimental assay.

**`additionalType`**: `Assay`

Reference: [ISA RO-Crate Profile — Assay](../../../references/isa_ro_crate.md)

## Additional Properties (beyond Dataset)

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `about` | [LabProcess](../../core/LabProcess.md) | SHOULD | Experimental processes in this assay |
| `measurementMethod` | URL, DefinedTerm | SHOULD | Measurement type (e.g., Proteomics) |
| `measurementTechnique` | URL, DefinedTerm | SHOULD | Technology used (e.g., mass spectrometry) |
| `variableMeasured` | Text, PropertyValue | COULD | Target variable being measured |
