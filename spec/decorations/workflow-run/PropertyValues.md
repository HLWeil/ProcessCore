# Workflow Run PropertyValue Subtypes

Specializations of [PropertyValue](../../core/PropertyValue.md) used in the Workflow Run decoration.

Reference: [ARC WR RO-Crate Profile — PropertyValue](../../../references/arc_wr_ro_crate.md)

## Workflow Input (`Workflow Input`)

Realized value for a workflow input in a Workflow Invocation. Links to a FormalParameter to distinguish from process parameters.

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `additionalType` | Text | MUST | `Workflow Input` |
| `exampleOfWork` | IRI | MUST | References FormalParameter `input` being realized |
| `name` | Text | SHOULD | Input name |
| `value` | Boolean, Number, Text | MUST | Realized value |

## Prefix (`Prefix`)

Describes the CLI prefix of a workflow input.

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `name` | Text | MUST | `Prefix` |
| `value` | Text | MUST | CLI prefix string |

## Position (`Position`)

Describes the positional index of a workflow input.

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `name` | Text | MUST | `Position` |
| `value` | Number | MUST | Position index |
