# Workflow Invocation

Workflow Run specialization of [LabProcess](../../core/LabProcess.md). Represents the execution of a Workflow Protocol, combining computational and laboratory workflow execution.

**`additionalType`**: `Workflow Invocation`

**Multi-type**: CreateAction + LabProcess

Reference: [ARC WR RO-Crate Profile — Workflow Invocation](../../../references/arc_wr_ro_crate.md)

## Properties

Inherits all properties from [LabProcess](../../core/LabProcess.md). WR-specific refinements:

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `executesLabProtocol` | [WorkflowProtocol](WorkflowProtocol.md) | MUST | Executed workflow (MUST equal `instrument`) |
| `inputs` | MediaObject, Dataset, PropertyValue | MUST | Input files consumed |
| `outputs` | MediaObject, Dataset, PropertyValue | MUST | Output files created/modified |
| `parameterValue` | [PropertyValue](PropertyValues.md) | COULD | Workflow parameter values |
| `description` | Text | COULD | Execution details (CLI args, settings) |
