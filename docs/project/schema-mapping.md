---
title: Schema.org mapping
category: Project
categoryindex: 2
index: 6
---

# Schema.org mapping

The ProcessCore is designed to be compatible with RO-Crate, and therefore it's model structure closely follows Schema.org. Here we track the mapping between ProcessCore and Schema.org, and note any deviations or extensions.

## Dataset

| PC property | Location | Schema.org Property | Mapping |
|---|---|---|---|
| `id` | `@id` | - | - |
| `type` | `@type` | - | - |
| `additionalType` | `schema:` | `schema:additionalType` | - |
| `identifier` | `schema:` | `schema:identifier` | - |
| `name` | `schema:` | `schema:name` | - |
| `description` | `schema:` | `schema:description` | - |
| `processes` | PC extension | `schema:about` | Renaming |
| `hasPart` | `schema:` | `schema:hasPart` | - |
| `additionalProperty` | `schema:` | `schema:additionalProperty` | - |

## LabProcess   

| PC property | Location | Schema.org Property | Mapping |
|---|---|---|---|
| `id` | `@id` | - | - |
| `type` | `@type` | - | - |
| `additionalType` | `schema:` | `schema:additionalType` | - |
| `name` | `schema:` | `schema:name` | - |
| `inputs` | `bioschemas:` | `bioschemas:inputs` | - |
| `outputs` | `bioschemas:` | `bioschemas:outputs` | - |
| `executesProtocol` | `bioschemas:` | `bioschemas:executesProtocol` | - |
| `parameterValue` | `bioschemas:` | `bioschemas:parameterValue` | - |

## LabProtocol

| PC property | Location | Schema.org Property | Mapping |
|---|---|---|---|
| `id` | `@id` | - | - |
| `type` | `@type` | - | - |
| `additionalType` | `schema:` | `schema:additionalType` | - |
| `name` | `schema:` | `schema:name` | - |
| `description` | `schema:` | `schema:description` | - |
| `parameters` | `bioschemas:` | `bioschemas:input` | Renaming |
| `intendedUse` | `bioschemas:` | `bioschemas:intendedUse` | - |
| `additionalProperty` | `schema:` | `schema:additionalProperty` | - |
| `labEquipment` | `bioschemas:` | `bioschemas:labEquipment` | - |
| `version` | `schema:` | `schema:version` | - |
| `url` | `schema:` | `schema:url` | - |

## Material

| PC property | Location | Schema.org Property | Mapping |
|---|---|---|---|
| `id` | `@id` | - | - |
| `type` | `@type` | - | - |
| `additionalType` | `schema:` | `schema:additionalType` | - |
| `name` | `schema:` | `schema:name` | - |
| `additionalProperty` | `schema:` | `schema:additionalProperty` | - |

## Data

| PC property | Location | Schema.org Property | Mapping |
|---|---|---|---|
| `id` | `@id` | - | - |
| `type` | `@type` | - | - |
| `additionalType` | `schema:` | `schema:additionalType` | - |
| `path` | PC extension | `schema:contentUrl` | Renaming |
| `selector` | PC extension | - | - |
| `selectorFormat` | PC extension | - | - |
| `encodingFormat` | `schema:` | `schema:encodingFormat` | - |
| `additionalProperty` | `schema:` | `schema:additionalProperty` | - |

## PropertyValue

| PC property | Location | Schema.org Property | Mapping |
|---|---|---|---|
| `id` | `@id` | - | - |
| `type` | `@type` | - | - |
| `additionalType` | `schema:` | `schema:additionalType` | - |
| `name` | `schema:` | `schema:name` | - |
| `value` | `schema:` | `schema:value` | - |
| `unit` | `schema:` | `schema:unitCode` | - |
| `nameTAN` | PC extension | - | - |
| `valueTAN` | PC extension | - | - |
| `unitTAN` | PC extension | - | - |
| `instanceOf` | PC extension | - | - |

## FormalParameter

| PC property | Location | Schema.org Property | Mapping |
|---|---|---|---|
| `id` | `@id` | - | - |
| `type` | `@type` | - | - |
| `name` | `schema:` | `schema:name` | - |
| `nameTAN` | PC extension | - | - |
| `defaultValue` | `bioschemas:` | `bioschemas:defaultValue` | - |

## DefinedTerm

| PC property | Location | Schema.org Property | Mapping |
|---|---|---|---|
| `id` | `@id` | - | - |
| `type` | `@type` | - | - |
| `name` | `schema:` | `schema:name` | - |
| `TAN` | PC extension | - | - |
| `inDefinedTermSet` | `schema:` | `schema:inDefinedTermSet` | - |

