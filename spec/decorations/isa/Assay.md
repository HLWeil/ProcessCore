# Assay

ISA specialization of [Dataset](../../core/Dataset.md). Represents a specific analytical measurement or experimental assay.

**`additionalType`**: `Assay`

Reference: [ISA RO-Crate Profile — Assay](../../../references/isa_ro_crate.md)

## Additional Properties (beyond Dataset)

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `additionalType` | string | MUST | `Assay` |
| `about` | [LabProcess](../../core/LabProcess.md) | SHOULD | Experimental processes in this assay |
| `perfomers` | [Person](Person.md) | COULD | Assay performers and contributors |

## Relationships

```mermaid
flowchart TD

    at@{ shape: stadium, label: "\"Assay\"" }

    Investigation --hasPart--> Assay
    Study -.assays.-> Assay
    Assay --about--> LabProcess
    Assay --performers--> Person
    Assay --additionalType--> at

```

## AdditionalProperties

The `identifier` property can be used to link to external identifiers for the article, such as a DOI or PubMedID. 


| `measurementMethod` |  DefinedTerm | SHOULD | Measurement type (e.g., Proteomics) |
| `measurementTechnique` | DefinedTerm | SHOULD | Technology used (e.g., mass spectrometry) |
| `variableMeasured` | PropertyValue | COULD | Target variable being measured |
