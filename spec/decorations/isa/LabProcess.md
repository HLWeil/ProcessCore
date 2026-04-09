# LabProcess

ISA specialization of [Process](../../core/Process.md). Represents an executed experimental process in a laboratory workflow.

**Schema.org type**: `bioschemas.org/LabProcess`

Reference: [ISA RO-Crate Profile — LabProcess](../../../references/isa_ro_crate.md)

## Properties

Inherits all properties from [Process](../../core/Process.md). ISA-specific refinements:

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `object` | [Sample](Sample.md), Data | SHOULD | Input materials/files (sorted for correspondence with results) |
| `result` | [Sample](Sample.md), Data | SHOULD | Output materials/files (sorted for correspondence with objects) |
| `executesLabProtocol` | [LabProtocol](LabProtocol.md) | SHOULD | Protocol executed |
| `parameterValue` | [PropertyValue — Parameter](PropertyValues.md) | SHOULD | Process parameter values |
| `disambiguatingDescription` | Text | COULD | Comments |
