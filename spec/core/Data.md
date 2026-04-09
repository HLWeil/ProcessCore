# Data

Data files produced or consumed by processes.

**Schema.org type**: `schema.org/MediaObject` or `File`

## Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `@id` | Text | MUST | Path pointing to the file |
| `@type` | Text | MUST | `File` or `MediaObject` |
| `name` | Text | MUST | File name |
| `encodingFormat` | Text | COULD | MIME type |
| `disambiguatingDescription` | Text | COULD | Data type (e.g., "Raw Data File", "Derived Data File") |

## Relationships

```mermaid
flowchart TD
    Process --object--> Data
    Process --result--> Data
    Dataset --hasPart--> Data
```
