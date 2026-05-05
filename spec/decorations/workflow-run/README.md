# Workflow Run Decoration

Maps the ARC Workflow Run model onto ProcessCore. This decoration enables the representation of both prospective (planned) and retrospective (executed) provenance of computational workflows.

Reference: [ARC WR RO-Crate Profile](../../../references/arc_wr_ro_crate.md)

## Core → Workflow Run Mapping

| Core Type | WR Specialization | `additionalType` |
|-----------|-------------------|-------------------|
| [Dataset](../../core/Dataset.md) | [ARC Workflow](ArcWorkflow.md) | `ARC Workflow` |
| [Dataset](../../core/Dataset.md) | [ARC Run](ArcRun.md) | `ARC Run` |
| [LabProtocol](../../core/LabProtocol.md) | [Workflow Protocol](WorkflowProtocol.md) | `Workflow Protocol` |
| [LabProcess](../../core/LabProcess.md) | [Workflow Invocation](WorkflowInvocation.md) | `Workflow Invocation` |
| [PropertyValue](../../core/PropertyValue.md) | [PropertyValues](PropertyValues.md) | `Workflow Input` / `Prefix` / `Position` |
| _(WR-specific)_ | [FormalParameter](FormalParameter.md) | — |
