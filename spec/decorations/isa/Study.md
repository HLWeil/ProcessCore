# Study

ISA specialization of [Dataset](../../core/Dataset.md). Represents a unit of research with associated experimental processes at the study level.

**`additionalType`**: `Study`

Reference: [ISA RO-Crate Profile — Study](../../../references/isa_ro_crate.md)

## Additional Properties (beyond Dataset)

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `about` | [LabProcess](../../core/LabProcess.md) | SHOULD | Experimental processes in this study |
| `hasPart` | [Assay](Assay.md), Data | SHOULD | Contained assays or data files |
| `citation` | ScholarlyArticle | COULD | Associated publications |
