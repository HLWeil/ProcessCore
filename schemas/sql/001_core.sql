-- ARC Data Model - SQLite Core Schema
-- Minimal core-only schema aligned with the current ProcessCore spec.

PRAGMA foreign_keys = ON;
PRAGMA journal_mode = WAL;

-- ============================================================
-- Core Entity Tables
-- ============================================================

CREATE TABLE DefinedTerm (
    id                  TEXT PRIMARY KEY,
    type                TEXT NOT NULL,
    name                TEXT NOT NULL,
    tan                 TEXT,
    in_defined_term_set TEXT
);

CREATE TABLE FormalParameter (
    id               TEXT PRIMARY KEY,
    type             TEXT NOT NULL,
    name             TEXT,
    name_tan         TEXT,
    default_value_id TEXT REFERENCES DefinedTerm(id)
);

CREATE TABLE PropertyValue (
    id               TEXT PRIMARY KEY,
    type             TEXT NOT NULL,
    additional_type  TEXT,
    name             TEXT NOT NULL,
    value            TEXT,
    unit             TEXT,
    name_tan         TEXT,
    value_tan        TEXT,
    unit_tan         TEXT,
    instance_of_id   TEXT REFERENCES FormalParameter(id)
);

CREATE TABLE LabProtocol (
    id               TEXT PRIMARY KEY,
    type             TEXT NOT NULL,
    additional_type  TEXT,
    name             TEXT,
    description      TEXT,
    intended_use_id  TEXT REFERENCES DefinedTerm(id),
    version          TEXT,
    url              TEXT
);

CREATE TABLE Material (
    id               TEXT PRIMARY KEY,
    type             TEXT NOT NULL,
    additional_type  TEXT,
    name             TEXT NOT NULL
);

CREATE TABLE Data (
    id               TEXT PRIMARY KEY,
    type             TEXT NOT NULL,
    additional_type  TEXT,
    path             TEXT NOT NULL,
    selector         TEXT,
    selector_format  TEXT,
    encoding_format  TEXT
);

CREATE TABLE Dataset (
    id               TEXT PRIMARY KEY,
    type             TEXT NOT NULL,
    additional_type  TEXT,
    identifier       TEXT NOT NULL,
    name             TEXT,
    description      TEXT
);

CREATE TABLE LabProcess (
    id                    TEXT PRIMARY KEY,
    type                  TEXT NOT NULL,
    additional_type       TEXT,
    name                  TEXT NOT NULL,
    executes_protocol_id  TEXT REFERENCES LabProtocol(id)
);

-- ============================================================
-- Relationship Tables
-- ============================================================

CREATE TABLE DatasetProcess (
    dataset_id      TEXT NOT NULL REFERENCES Dataset(id),
    lab_process_id  TEXT NOT NULL REFERENCES LabProcess(id),
    PRIMARY KEY (dataset_id, lab_process_id)
);

CREATE TABLE DatasetHasPartDataset (
    parent_dataset_id  TEXT NOT NULL REFERENCES Dataset(id),
    child_dataset_id   TEXT NOT NULL REFERENCES Dataset(id),
    PRIMARY KEY (parent_dataset_id, child_dataset_id),
    CHECK (parent_dataset_id <> child_dataset_id)
);

CREATE TABLE DatasetHasPartData (
    dataset_id  TEXT NOT NULL REFERENCES Dataset(id),
    data_id     TEXT NOT NULL REFERENCES Data(id),
    PRIMARY KEY (dataset_id, data_id)
);

CREATE TABLE DatasetAdditionalProperty (
    dataset_id        TEXT NOT NULL REFERENCES Dataset(id),
    property_value_id TEXT NOT NULL REFERENCES PropertyValue(id),
    PRIMARY KEY (dataset_id, property_value_id)
);

CREATE TABLE LabProtocolParameter (
    lab_protocol_id      TEXT NOT NULL REFERENCES LabProtocol(id),
    formal_parameter_id  TEXT NOT NULL REFERENCES FormalParameter(id),
    PRIMARY KEY (lab_protocol_id, formal_parameter_id)
);

CREATE TABLE LabProtocolAdditionalProperty (
    lab_protocol_id   TEXT NOT NULL REFERENCES LabProtocol(id),
    property_value_id TEXT NOT NULL REFERENCES PropertyValue(id),
    PRIMARY KEY (lab_protocol_id, property_value_id)
);

CREATE TABLE MaterialAdditionalProperty (
    material_id        TEXT NOT NULL REFERENCES Material(id),
    property_value_id  TEXT NOT NULL REFERENCES PropertyValue(id),
    PRIMARY KEY (material_id, property_value_id)
);

CREATE TABLE DataAdditionalProperty (
    data_id            TEXT NOT NULL REFERENCES Data(id),
    property_value_id  TEXT NOT NULL REFERENCES PropertyValue(id),
    PRIMARY KEY (data_id, property_value_id)
);

CREATE TABLE LabProcessParameterValue (
    lab_process_id     TEXT NOT NULL REFERENCES LabProcess(id),
    property_value_id  TEXT NOT NULL REFERENCES PropertyValue(id),
    PRIMARY KEY (lab_process_id, property_value_id)
);

CREATE TABLE LabProcessInputMaterial (
    lab_process_id  TEXT NOT NULL REFERENCES LabProcess(id),
    material_id     TEXT NOT NULL REFERENCES Material(id),
    pair_index      INTEGER NOT NULL CHECK (pair_index >= 0),
    PRIMARY KEY (lab_process_id, material_id, pair_index)
);

CREATE TABLE LabProcessInputData (
    lab_process_id  TEXT NOT NULL REFERENCES LabProcess(id),
    data_id         TEXT NOT NULL REFERENCES Data(id),
    pair_index      INTEGER NOT NULL CHECK (pair_index >= 0),
    PRIMARY KEY (lab_process_id, data_id, pair_index)
);

CREATE TABLE LabProcessOutputMaterial (
    lab_process_id  TEXT NOT NULL REFERENCES LabProcess(id),
    material_id     TEXT NOT NULL REFERENCES Material(id),
    pair_index      INTEGER NOT NULL CHECK (pair_index >= 0),
    PRIMARY KEY (lab_process_id, material_id, pair_index)
);

CREATE TABLE LabProcessOutputData (
    lab_process_id  TEXT NOT NULL REFERENCES LabProcess(id),
    data_id         TEXT NOT NULL REFERENCES Data(id),
    pair_index      INTEGER NOT NULL CHECK (pair_index >= 0),
    PRIMARY KEY (lab_process_id, data_id, pair_index)
);

-- ============================================================
-- Indexes
-- ============================================================

CREATE INDEX idx_dataset_additional_type             ON Dataset(additional_type);
CREATE INDEX idx_labprocess_additional_type          ON LabProcess(additional_type);
CREATE INDEX idx_labprotocol_additional_type         ON LabProtocol(additional_type);
CREATE INDEX idx_material_additional_type            ON Material(additional_type);
CREATE INDEX idx_data_additional_type                ON Data(additional_type);
CREATE INDEX idx_propertyvalue_additional_type       ON PropertyValue(additional_type);

CREATE INDEX idx_labprocess_executes_protocol        ON LabProcess(executes_protocol_id);
CREATE INDEX idx_labprotocol_intended_use            ON LabProtocol(intended_use_id);
CREATE INDEX idx_propertyvalue_instance_of           ON PropertyValue(instance_of_id);
CREATE INDEX idx_formalparameter_default_value       ON FormalParameter(default_value_id);

CREATE INDEX idx_datasetprocess_process              ON DatasetProcess(lab_process_id);
CREATE INDEX idx_datasethaspartdataset_child         ON DatasetHasPartDataset(child_dataset_id);
CREATE INDEX idx_datasethaspartdata_data             ON DatasetHasPartData(data_id);
CREATE INDEX idx_datasetadditionalproperty_property  ON DatasetAdditionalProperty(property_value_id);
CREATE INDEX idx_labprotocolparameter_parameter      ON LabProtocolParameter(formal_parameter_id);
CREATE INDEX idx_labprotocoladditionalproperty_prop  ON LabProtocolAdditionalProperty(property_value_id);
CREATE INDEX idx_materialadditionalproperty_prop     ON MaterialAdditionalProperty(property_value_id);
CREATE INDEX idx_dataadditionalproperty_prop         ON DataAdditionalProperty(property_value_id);
CREATE INDEX idx_labprocessparametervalue_prop       ON LabProcessParameterValue(property_value_id);

CREATE INDEX idx_labprocessinputmaterial_pair        ON LabProcessInputMaterial(lab_process_id, pair_index);
CREATE INDEX idx_labprocessinputdata_pair            ON LabProcessInputData(lab_process_id, pair_index);
CREATE INDEX idx_labprocessoutputmaterial_pair       ON LabProcessOutputMaterial(lab_process_id, pair_index);
CREATE INDEX idx_labprocessoutputdata_pair           ON LabProcessOutputData(lab_process_id, pair_index);
