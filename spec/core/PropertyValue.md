# PropertyValue

Extensible key-value-unit triple. PropertyValues are the primary extension mechanism of ProcessCore. They can be attached through `additionalProperty` for cross-cutting metadata, or through dedicated relationships such as `parameterValue` when the host type already defines a more specific role.

**Schema.org type**: `schema.org/PropertyValue`

Decoration subtypes:
- ISA: ParameterValue, CharacteristicValue, FactorValue, Component
- Datamap: FragmentDescriptor
- Workflow Run: Workflow Input, Prefix, Position

## Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `id` | Text | MUST | Unique identifier |
| `type` | Text | MUST | `schema.org/PropertyValue` |
| `additionalType` | Text | SHOULD | Subtype discriminator |
| `name` | Text | MUST | Key name |
| `value` | Text, Number | SHOULD | The value |
| `unit` | Text | COULD | Unit ontology reference |
| `nameTAN` | URL | SHOULD | Key ontology reference |
| `valueTAN` | URL | COULD | Unit name |
| `unitTAN` | URL | COULD | Value ontology reference |

## Relationships

```mermaid
flowchart TD

    na@{ shape: stadium, label: "string" }
    va@{ shape: stadium, label: "string" }
    un@{ shape: stadium, label: "string" }
    nt@{ shape: stadium, label: "URL" }
    vt@{ shape: stadium, label: "URL" }
    ut@{ shape: stadium, label: "URL" }

    Dataset --additionalProperty--> PropertyValue
    Process --parameterValue--> PropertyValue
    Material --additionalProperty--> PropertyValue
    Data --additionalProperty--> PropertyValue
    Protocol --additionalProperty--> PropertyValue

    PropertyValue --name--> na
    PropertyValue --value--> va
    PropertyValue --unit--> un
    PropertyValue --nameTAN--> nt
    PropertyValue --valueTAN--> vt
    PropertyValue --unitTAN--> ut
    

```
