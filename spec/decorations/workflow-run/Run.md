# Run

Workflow Run specialization of [Dataset](../../core/Dataset.md). Container describing a run folder in an ARC, documenting the execution of workflows.

**`additionalType`**: `Run`

Reference: [ARC WR RO-Crate Profile — ARC Run](../../../references/arc_wr_ro_crate.md)

## Additional Properties (beyond Dataset)

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `additionalType` | string | MUST | `Run` |
| `processes` | [WorkflowInvocation](WorkflowInvocation.md) | SHOULD | Workflow invocations|
| `performers` | [Person](Person.md) | COULD | Run performers and contributors |


