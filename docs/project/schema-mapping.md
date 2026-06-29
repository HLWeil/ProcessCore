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
| `Dataset` | Core | schema:Dataset | - |
|---|---|---|---|
| `additionalType` | Core | `schema:additionalType` | - |
| `identifier` | Core | `schema:identifier` | - |
| `title` | Core | `schema:name` | Renaming |
| `description` | Core | `schema:description` | - |
| `processes` | Core | `schema:about` | Renaming |
| `hasPart` | Core | `schema:hasPart` | - |
| `person` | Core (Suggested) | `schema:creator`; `schema:maintainer` ; `schema:maintainer` | Complex mapping (schema.org property is chosen based on role filed inside person) |
| `dataContexts` | Datamap | `schema:variableMeasured` | Renaming |
| `additionalProperty` | Core | `schema:additionalProperty` | Added Property |

## Process

| PC property | Location | Schema.org Property | Mapping |
|---|---|---|---|
| `Process` | Core | bioschemas:LabProcess | - |
|---|---|---|---|
| `additionalType` | Core | `schema:additionalType` | - |
| `name` | Core | `schema:name` | - |
| `inputs` | Core | `schema:object` | Renaming |
| `outputs` | Core | `schema:result` | Renaming |
| `executesProtocol` | Core | `bioschemas:executesRecipe` | Renaming |
| `parameterValue` | Core | `bioschemas:parameterValue` | - |

## Recipe

| PC property | Location | Schema.org Property | Mapping |
|---|---|---|---|
| `Recipe` | Core | bioschemas:LabProtocol | - |
|---|---|---|---|
| `additionalType` | Core | `schema:additionalType` | - |
| `name` | Core | `schema:name` | - |
| `description` | Core | `schema:description` | - |
| `parameters` | Core | `bioschemas:input` (?) | Renaming |
| `intendedUse` | Core | `bioschemas:intendedUse` | - |
| `labEquipment` | Core | `bioschemas:labEquipment` | - |
| `version` | Core | `schema:version` | - |
| `url` | Core | `schema:url` | - |
| `additionalProperty` | Core | `schema:additionalProperty` | Added Property |

## Sample

| PC property | Location | Schema.org Property | Mapping |
|---|---|---|---|
| `Sample` | Core | bioschemas:Sample | Renaming |
|---|---|---|---|
| `additionalType` | Core | `schema:additionalType` | - |
| `name` | Core | `schema:name` | - |
| `additionalProperty` | Core | `schema:additionalProperty` | - |

## Data

| PC property | Location | Schema.org Property | Mapping |
|---|---|---|---|
| `Data` | Core | schema:MediaObject | Renaming |
|---|---|---|---|
| `additionalType` | Core | `schema:additionalType` | - |
| `path` | Core | `@id` | Renaming and String conversion |
| `selector` | Core | `@id` | Renaming and String conversion |
| `selectorFormat` | Core | `schema:usageInfo` | Renaming |
| `encodingFormat` | Core | `schema:encodingFormat` | - |
| `additionalProperty` | Core | `schema:additionalProperty` | Added Property |

## Annotation

| PC property | Location | Schema.org Property | Mapping |
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

| PC property | Location | Schema.org Property | Mapping |
|---|---|---|---|
| `FormalParameter` | Core | bioschemas:FormalParameter | - |
|---|---|---|---|
| `name` | Core | `schema:name` | - |
| `nameTAN` | Core | `schema:url` | Renaming |
| `defaultValue` | Core | `bioschemas:defaultValue` | - |

## DefinedTerm

| PC property | Location | Schema.org Property | Mapping |
|---|---|---|---|
| `DefinedTerm` | Core | bioschemas:DefinedTerm | - |
|---|---|---|---|
| `name` | Core | `schema:name` | - |
| `TAN` | Core | `schema:termCode` | Renaming |
| `inDefinedTermSet` | Core | `schema:inDefinedTermSet` | - |

## Person

| PC property | Location | Schema.org Property | Mapping |
|---|---|---|---|
| `Person` | ISA | schema:Person | - |
|---|---|---|---|
| `givenName` | ISA | schema:givenName | - |
| `familyName` | ISA | schema:familyName | - |
| `email` | ISA | schema:email | - |
| `affiliation` | ISA | schema:affiliation | - |
| `identifier` | ISA | schema:identifier | - |
| `additionalProperty` | ISA | schema:additionalProperty | - |
| `jobTitle` | ISA | schema:jobTitle | - |

## DataContext

| PC property | Location | Schema.org Property | Mapping |
|---|---|---|---|
| `DataContext` | Datamap | schema:PropertyValue | - |
|---|---|---|---|
| `data` | Datamap | schema:subjectOf | Renaming |
| `explication` | Datamap | schema:value + schema:valueReference | Renaming plus merging strings into object (DefinedTerm) |
| `objectType` | Datamap | schema:pattern (on data) | Renaming plus moving into child |
| `unit` | Datamap | schema:unit + schema:unitCode | Renaming plus merging strings into object (DefinedTerm) |
| `label` | Datamap | schema:alternateName | Renaming |
| `description` | Datamap | schema:description | - |
| `generatedBy` | Datamap | schema:measurementMethod | Renaming |
