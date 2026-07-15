---
title: Schema.org mapping
category: Project
categoryindex: 2
index: 6
---

# Schema.org mapping

The ARC Core data model is designed to be compatible with RO-Crate, and therefore its model structure closely follows Schema.org. Here we track the mapping between ARC Core and Schema.org, and note any deviations or extensions.

## Dataset

| ARC property | Location | Schema.org Property | Mapping |
|---|---|---|---|
| `Dataset` | Core | schema:Dataset | - |
|---|---|---|---|
| `additionalType` | Core | `schema:additionalType` | - |
| `identifier` | Core | `schema:identifier` | - |
| `title` | Administrative | `schema:name` | Renaming |
| `description` | Administrative | `schema:description` | - |
| `license` | Administrative | `schema:license` | Added Property |
| `datePublished` | Administrative | `schema:datePublished` | Added Property |
| `dateCreated` | Administrative | `schema:dateCreated` | Added Property |
| `dateModified` | Administrative | `schema:dateModified` | Added Property |
| `processes` | Process Core | `schema:about` | Renaming |
| `hasPart` | Process Core / Datamap | `schema:hasPart` | Sub-datasets and data-file membership |
| `dataFiles` | Datamap | `schema:hasPart` | Added Property |
| `agents` | Administrative | `schema:creator`; `schema:contributor`; `schema:maintainer` | Complex mapping (Schema.org property is chosen based on role metadata inside Agent) |
| `citations` | Administrative | `schema:citation` | Added Property |
| `dataContexts` | Datamap | `schema:variableMeasured` | Renaming |
| `additionalProperty` | Core | `schema:additionalProperty` | Added Property |

## Process

| ARC property | Location | Schema.org Property | Mapping |
|---|---|---|---|
| `Process` | Process Core | bioschemas:LabProcess | - |
|---|---|---|---|
| `additionalType` | Process Core | `schema:additionalType` | - |
| `name` | Process Core | `schema:name` | - |
| `inputs` | Process Core | `schema:object` | Renaming |
| `outputs` | Process Core | `schema:result` | Renaming |
| `executesRecipe` | Process Core | `bioschemas:executesRecipe` | - |
| `parameterValue` | Process Core | `bioschemas:parameterValue` | - |

## Recipe

| ARC property | Location | Schema.org Property | Mapping |
|---|---|---|---|
| `Recipe` | Process Core | bioschemas:LabProtocol | - |
|---|---|---|---|
| `additionalType` | Process Core | `schema:additionalType` | - |
| `name` | Process Core | `schema:name` | - |
| `description` | Process Core | `schema:description` | - |
| `parameters` | Process Core | `bioschemas:input` (?) | Renaming |
| `intendedUse` | Process Core | `bioschemas:intendedUse` | - |
| `components` | Process Core | `bioschemas:labEquipment`; `bioschemas:computationalTool`; `bioschemas:reagent` | Unified Process Core property for protocol components |
| `version` | Process Core | `schema:version` | - |
| `url` | Process Core | `schema:url` | - |
| `additionalProperty` | Process Core | `schema:additionalProperty` | Added Property |

## Sample

| ARC property | Location | Schema.org Property | Mapping |
|---|---|---|---|
| `Sample` | Process Core | bioschemas:Sample | Renaming |
|---|---|---|---|
| `additionalType` | Process Core | `schema:additionalType` | - |
| `name` | Process Core | `schema:name` | - |
| `additionalProperty` | Process Core | `schema:additionalProperty` | - |

## Data

| ARC property | Location | Schema.org Property | Mapping |
|---|---|---|---|
| `Data` | Core | schema:MediaObject | Renaming |
|---|---|---|---|
| `additionalType` | Core | `schema:additionalType` | - |
| `path` | Core | `@id` | Renaming and String conversion |
| `selector` | Core | `@id` | Renaming and String conversion |
| `selectorFormat` | Core | `schema:usageInfo` | Renaming |
| `encodingFormat` | Core | `schema:encodingFormat` | - |
| `hasPart` | Core | `schema:hasPart` | Data fragments |
| `additionalProperty` | Core | `schema:additionalProperty` | Added Property |

## Annotation

| ARC property | Location | Schema.org Property | Mapping |
|---|---|---|---|
| `Annotation` | Core | schema:PropertyValue | - |
|---|---|---|---|
| `additionalType` | Core | `schema:additionalType` | - |
| `name` | Core | `schema:name` | - |
| `value` | Core | `schema:value` | - |
| `unit` | Core | `schema:unitText` | Renaming |
| `nameTAN` | Core | `schema:propertyID` | Renaming |
| `valueTAN` | Core | `schema:valueReference` | Renaming |
| `unitTAN` | Core | `schema:unitCode` | Renaming |
| `instanceOf` | Core | `schema:exampleOfWork` | Renaming |

## FormalParameter

| ARC property | Location | Schema.org Property | Mapping |
|---|---|---|---|
| `FormalParameter` | Process Core | bioschemas:FormalParameter | - |
|---|---|---|---|
| `name` | Process Core | `schema:name` | - |
| `nameTAN` | Process Core | `schema:url` | Renaming |
| `defaultValue` | Process Core | `bioschemas:defaultValue` | - |

## DefinedTerm

| ARC property | Location | Schema.org Property | Mapping |
|---|---|---|---|
| `DefinedTerm` | Core | bioschemas:DefinedTerm | - |
|---|---|---|---|
| `name` | Core | `schema:name` | - |
| `TAN` | Core | `schema:termCode` | Renaming |
| `inDefinedTermSet` | Core | `schema:inDefinedTermSet` | - |

## Agent

| ARC property | Location | Schema.org Property | Mapping |
|---|---|---|---|
| `Agent` | Administrative | schema:Agent | Renaming from Person |
|---|---|---|---|
| `givenName` | Administrative | schema:givenName | - |
| `familyName` | Administrative | schema:familyName | - |
| `email` | Administrative | schema:email | - |
| `affiliation` | Administrative | schema:affiliation | - |
| `identifier` | Administrative | schema:identifier | - |
| `additionalProperty` | Administrative | schema:additionalProperty | - |
| `jobTitle` | Administrative | schema:jobTitle | - |

## Organization

| ARC property | Location | Schema.org Property | Mapping |
|---|---|---|---|
| `Organization` | Administrative | schema:Organization | - |
|---|---|---|---|
| `name` | Administrative | schema:name | - |
| `url` | Administrative | schema:url | - |

## ScholarlyArticle

| ARC property | Location | Schema.org Property | Mapping |
|---|---|---|---|
| `ScholarlyArticle` | Administrative | schema:ScholarlyArticle | - |
|---|---|---|---|
| `headline` | Administrative | schema:headline | - |
| `identifier` | Administrative | schema:identifier | - |
| `authors` | Administrative | schema:author | Renaming |
| `creativeWorkStatus` | Administrative | schema:creativeWorkStatus | - |
| `additionalProperty` | Administrative | schema:additionalProperty | - |

## DataContext

| ARC property | Location | Schema.org Property | Mapping |
|---|---|---|---|
| `DataContext` | Datamap | schema:PropertyValue | - |
|---|---|---|---|
| `data` | Datamap | schema:subjectOf | Renaming |
| `explication` | Datamap | schema:value | Renaming |
| `explicationTAN` | Datamap | schema:valueReference | Renaming |
| `objectType` | Datamap | schema:pattern | Renaming |
| `objectTypeTAN` | Datamap | schema:valueReference | Renaming |
| `unit` | Datamap | schema:unitText | Renaming |
| `unitTAN` | Datamap | schema:unitCode | Renaming |
| `label` | Datamap | schema:alternateName | Renaming |
| `description` | Datamap | schema:description | - |
| `generatedBy` | Datamap | schema:measurementMethod | Renaming |
