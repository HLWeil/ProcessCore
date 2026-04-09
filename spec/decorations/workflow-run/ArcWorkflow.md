# ARC Workflow

Workflow Run specialization of [Dataset](../../core/Dataset.md). Container describing a workflow folder in an ARC with ISA-compliant metadata.

**`additionalType`**: `ARC Workflow`

Reference: [ARC WR RO-Crate Profile — ARC Workflow](../../../references/arc_wr_ro_crate.md)

## Additional Properties (beyond Dataset)

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `mainEntity` | [WorkflowProtocol](WorkflowProtocol.md) | MUST | The main workflow |
| `hasPart` | MediaObject, [WorkflowProtocol](WorkflowProtocol.md) | SHOULD | All data files and sub-workflows |
