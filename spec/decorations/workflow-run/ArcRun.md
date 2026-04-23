# ARC Run

Workflow Run specialization of [Dataset](../../core/Dataset.md). Container describing a run folder in an ARC, documenting the execution of workflows.

**`additionalType`**: `ARC Run`

Reference: [ARC WR RO-Crate Profile — ARC Run](../../../references/arc_wr_ro_crate.md)

## Additional Properties (beyond Dataset)

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `processes` | [WorkflowInvocation](WorkflowInvocation.md) | SHOULD | Workflow invocations (MUST equal `mentions`) |
| `mentions` | [WorkflowInvocation](WorkflowInvocation.md) | SHOULD | Workflow invocations (MUST equal `processes`) |
| `conformsTo` | CreativeWork | SHOULD | Versioned WR profile permalink |
| `measurementMethod` | URL, DefinedTerm | SHOULD | Technology used |
| `measurementTechnique` | URL, DefinedTerm | SHOULD | Software/tool used |
| `variableMeasured` | Text, PropertyValue | COULD | Endpoint being computed |
