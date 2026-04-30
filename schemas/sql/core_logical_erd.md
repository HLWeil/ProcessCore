# Core SQLite Logical ERD

This draft translates the current core spec into a SQLite-friendly logical model.

How to read it:

- Single-valued references become foreign key columns on the owning table, for example `LabProcess.executesProtocol` becomes `LabProcess.executes_protocol_id`.
- Multi-valued references become join tables, because SQLite does not have native array columns.
- Polymorphic multi-valued references such as `inputs` and `outputs` are split into separate `...Material` and `...Data` join tables so foreign keys can still be enforced.
- `pair_index` on process input/output tables preserves the spec idea that the Nth input corresponds to the Nth output.
- This draft assumes every persisted row has a primary key, even where the markdown spec marks an identifier as optional. That matters especially for `LabProtocol`, `LabProcess`, and `Data`.
- `Person` is still omitted because it is referenced from the core docs but is not currently specified as a core type.

## Overview

This overview keeps only the core tables and single-valued foreign keys. The multi-valued properties are detailed in the focused diagrams below.

```mermaid
erDiagram
    Dataset {
        TEXT id PK
        TEXT type
        TEXT additional_type
        TEXT identifier
        TEXT name
        TEXT description
    }

    LabProcess {
        TEXT id PK
        TEXT type
        TEXT additional_type
        TEXT name
        TEXT executes_protocol_id FK
    }

    LabProtocol {
        TEXT id PK
        TEXT type
        TEXT additional_type
        TEXT name
        TEXT description
        TEXT intended_use_id FK
        TEXT version
        TEXT url
    }

    Material {
        TEXT id PK
        TEXT type
        TEXT additional_type
        TEXT name
    }

    Data {
        TEXT id PK
        TEXT type
        TEXT additional_type
        TEXT path
        TEXT selector
        TEXT selector_format
        TEXT encoding_format
    }

    PropertyValue {
        TEXT id PK
        TEXT type
        TEXT additional_type
        TEXT name
        TEXT value
        TEXT unit
        TEXT name_tan
        TEXT value_tan
        TEXT unit_tan
        TEXT instance_of_id FK
    }

    FormalParameter {
        TEXT id PK
        TEXT type
        TEXT name
        TEXT name_tan
        TEXT default_value_id FK
    }

    DefinedTerm {
        TEXT id PK
        TEXT type
        TEXT name
        TEXT tan
        TEXT in_defined_term_set
    }

    LabProcess o|--|| LabProtocol : executes_protocol_id
    LabProtocol o|--|| DefinedTerm : intended_use_id
    PropertyValue o|--|| FormalParameter : instance_of_id
    FormalParameter o|--|| DefinedTerm : default_value_id
```

## Dataset Structure

This diagram expands the multi-valued dataset properties.

```mermaid
erDiagram
    Dataset {
        TEXT id PK
        TEXT type
        TEXT additional_type
        TEXT identifier
        TEXT name
        TEXT description
    }

    LabProcess {
        TEXT id PK
        TEXT type
        TEXT additional_type
        TEXT name
    }

    Data {
        TEXT id PK
        TEXT path
    }

    PropertyValue {
        TEXT id PK
        TEXT name
        TEXT value
    }

    DatasetProcess {
        TEXT dataset_id FK
        TEXT lab_process_id FK
    }

    DatasetHasPartDataset {
        TEXT parent_dataset_id FK
        TEXT child_dataset_id FK
    }

    DatasetHasPartData {
        TEXT dataset_id FK
        TEXT data_id FK
    }

    DatasetAdditionalProperty {
        TEXT dataset_id FK
        TEXT property_value_id FK
    }

    Dataset ||--o{ DatasetProcess : processes
    LabProcess ||--o{ DatasetProcess : processes

    Dataset ||--o{ DatasetHasPartDataset : parent_dataset_id
    Dataset ||--o{ DatasetHasPartDataset : child_dataset_id

    Dataset ||--o{ DatasetHasPartData : hasPart
    Data ||--o{ DatasetHasPartData : hasPart

    Dataset ||--o{ DatasetAdditionalProperty : additionalProperty
    PropertyValue ||--o{ DatasetAdditionalProperty : additionalProperty
```

## Protocol And Parameters

This diagram expands `LabProtocol.parameters`, `LabProtocol.additionalProperty`, `PropertyValue.instanceOf`, and `FormalParameter.defaultValue`.

```mermaid
erDiagram
    LabProtocol {
        TEXT id PK
        TEXT type
        TEXT additional_type
        TEXT name
        TEXT description
        TEXT intended_use_id FK
        TEXT version
        TEXT url
    }

    FormalParameter {
        TEXT id PK
        TEXT type
        TEXT name
        TEXT name_tan
        TEXT default_value_id FK
    }

    DefinedTerm {
        TEXT id PK
        TEXT type
        TEXT name
        TEXT tan
        TEXT in_defined_term_set
    }

    PropertyValue {
        TEXT id PK
        TEXT type
        TEXT additional_type
        TEXT name
        TEXT value
        TEXT unit
        TEXT instance_of_id FK
    }

    LabProtocolParameter {
        TEXT lab_protocol_id FK
        TEXT formal_parameter_id FK
    }

    LabProtocolAdditionalProperty {
        TEXT lab_protocol_id FK
        TEXT property_value_id FK
    }

    LabProtocol o|--|| DefinedTerm : intended_use_id
    FormalParameter o|--|| DefinedTerm : default_value_id
    PropertyValue o|--|| FormalParameter : instance_of_id

    LabProtocol ||--o{ LabProtocolParameter : parameters
    FormalParameter ||--o{ LabProtocolParameter : parameters

    LabProtocol ||--o{ LabProtocolAdditionalProperty : additionalProperty
    PropertyValue ||--o{ LabProtocolAdditionalProperty : additionalProperty
```

## Process Inputs, Outputs, And Parameter Values

This diagram expands `inputs`, `outputs`, and `parameterValue`.

```mermaid
erDiagram
    LabProcess {
        TEXT id PK
        TEXT type
        TEXT additional_type
        TEXT name
        TEXT executes_protocol_id FK
    }

    LabProtocol {
        TEXT id PK
        TEXT name
    }

    Material {
        TEXT id PK
        TEXT type
        TEXT name
    }

    Data {
        TEXT id PK
        TEXT path
        TEXT selector
    }

    PropertyValue {
        TEXT id PK
        TEXT name
        TEXT value
    }

    LabProcessParameterValue {
        TEXT lab_process_id FK
        TEXT property_value_id FK
    }

    LabProcessInputMaterial {
        TEXT lab_process_id FK
        TEXT material_id FK
        INTEGER pair_index
    }

    LabProcessInputData {
        TEXT lab_process_id FK
        TEXT data_id FK
        INTEGER pair_index
    }

    LabProcessOutputMaterial {
        TEXT lab_process_id FK
        TEXT material_id FK
        INTEGER pair_index
    }

    LabProcessOutputData {
        TEXT lab_process_id FK
        TEXT data_id FK
        INTEGER pair_index
    }

    LabProcess o|--|| LabProtocol : executes_protocol_id

    LabProcess ||--o{ LabProcessParameterValue : parameterValue
    PropertyValue ||--o{ LabProcessParameterValue : parameterValue

    LabProcess ||--o{ LabProcessInputMaterial : inputs
    Material ||--o{ LabProcessInputMaterial : inputs

    LabProcess ||--o{ LabProcessInputData : inputs
    Data ||--o{ LabProcessInputData : inputs

    LabProcess ||--o{ LabProcessOutputMaterial : outputs
    Material ||--o{ LabProcessOutputMaterial : outputs

    LabProcess ||--o{ LabProcessOutputData : outputs
    Data ||--o{ LabProcessOutputData : outputs
```

## AdditionalProperty Attachments

This diagram collects the repeated `additionalProperty` join-table pattern.

```mermaid
erDiagram
    Dataset {
        TEXT id PK
    }

    LabProtocol {
        TEXT id PK
    }

    Material {
        TEXT id PK
    }

    Data {
        TEXT id PK
    }

    PropertyValue {
        TEXT id PK
        TEXT name
        TEXT value
    }

    DatasetAdditionalProperty {
        TEXT dataset_id FK
        TEXT property_value_id FK
    }

    LabProtocolAdditionalProperty {
        TEXT lab_protocol_id FK
        TEXT property_value_id FK
    }

    MaterialAdditionalProperty {
        TEXT material_id FK
        TEXT property_value_id FK
    }

    DataAdditionalProperty {
        TEXT data_id FK
        TEXT property_value_id FK
    }

    Dataset ||--o{ DatasetAdditionalProperty : additionalProperty
    PropertyValue ||--o{ DatasetAdditionalProperty : additionalProperty

    LabProtocol ||--o{ LabProtocolAdditionalProperty : additionalProperty
    PropertyValue ||--o{ LabProtocolAdditionalProperty : additionalProperty

    Material ||--o{ MaterialAdditionalProperty : additionalProperty
    PropertyValue ||--o{ MaterialAdditionalProperty : additionalProperty

    Data ||--o{ DataAdditionalProperty : additionalProperty
    PropertyValue ||--o{ DataAdditionalProperty : additionalProperty
```
