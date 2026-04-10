-- ARC Data Model – SQLite Schema
-- ProcessCore tables, edge-native Process rows, decoration views,
-- and materialized lineage tables.

PRAGMA foreign_keys = ON;
PRAGMA journal_mode = WAL;

-- ============================================================
-- Core Entity Tables
-- ============================================================

CREATE TABLE DefinedTerm (
    id                  TEXT PRIMARY KEY,
    type                TEXT NOT NULL,
    name                TEXT NOT NULL,
    term_code           TEXT,
    in_defined_term_set TEXT
);

CREATE TABLE PropertyValue (
    id               TEXT PRIMARY KEY,
    type             TEXT NOT NULL DEFAULT 'PropertyValue',
    additional_type  TEXT,
    name             TEXT NOT NULL,
    value            TEXT,
    property_id      TEXT,
    unit_code        TEXT,
    unit_text        TEXT,
    value_reference  TEXT,
    example_of_work  TEXT
);

CREATE TABLE Protocol (
    id                    TEXT PRIMARY KEY,
    type                  TEXT NOT NULL,
    additional_type       TEXT,
    name                  TEXT,
    description           TEXT,
    intended_use_id       TEXT REFERENCES DefinedTerm(id),
    version               TEXT,
    url                   TEXT,
    programming_language  TEXT
);

CREATE TABLE Material (
    id              TEXT PRIMARY KEY,
    type            TEXT NOT NULL,
    name            TEXT NOT NULL,
    additional_type TEXT
);

CREATE TABLE Data (
    id              TEXT PRIMARY KEY,
    type            TEXT NOT NULL,
    additional_type TEXT,
    path            TEXT NOT NULL,
    selector        TEXT,
    selector_format TEXT,
    encoding_format TEXT
);

CREATE TABLE Dataset (
    id                      TEXT PRIMARY KEY,
    type                    TEXT NOT NULL DEFAULT 'Dataset',
    additional_type         TEXT NOT NULL,
    identifier              TEXT NOT NULL,
    name                    TEXT,
    description             TEXT,
    license                 TEXT,
    date_published          TEXT,
    date_created            TEXT,
    measurement_method      TEXT,
    measurement_technique   TEXT,
    variable_measured       TEXT,
    main_entity_id          TEXT REFERENCES Protocol(id),
    conforms_to             TEXT
);

CREATE TABLE Process (
    id                          TEXT PRIMARY KEY,
    type                        TEXT NOT NULL,
    name                        TEXT NOT NULL,
    additional_type             TEXT,
    executes_protocol_id        TEXT REFERENCES Protocol(id),
    input_type                  TEXT NOT NULL CHECK (input_type IN ('Material', 'Data')),
    input_id                    TEXT NOT NULL,
    output_type                 TEXT NOT NULL CHECK (output_type IN ('Material', 'Data')),
    output_id                   TEXT NOT NULL,
    end_time                    TEXT,
    disambiguating_description  TEXT,
    description                 TEXT
);

-- ============================================================
-- Relationship Tables
-- ============================================================

CREATE TABLE ProcessParameterValue (
    process_id       TEXT NOT NULL REFERENCES Process(id),
    propertyvalue_id TEXT NOT NULL REFERENCES PropertyValue(id),
    PRIMARY KEY (process_id, propertyvalue_id)
);

CREATE TABLE ProcessAdditionalProperty (
    process_id       TEXT NOT NULL REFERENCES Process(id),
    propertyvalue_id TEXT NOT NULL REFERENCES PropertyValue(id),
    PRIMARY KEY (process_id, propertyvalue_id)
);

CREATE TABLE DatasetAbout (
    dataset_id TEXT NOT NULL REFERENCES Dataset(id),
    process_id TEXT NOT NULL REFERENCES Process(id),
    PRIMARY KEY (dataset_id, process_id)
);

CREATE TABLE DatasetHasPartData (
    dataset_id TEXT NOT NULL REFERENCES Dataset(id),
    data_id    TEXT NOT NULL REFERENCES Data(id),
    PRIMARY KEY (dataset_id, data_id)
);

CREATE TABLE DatasetHasPartDataset (
    parent_id TEXT NOT NULL REFERENCES Dataset(id),
    child_id  TEXT NOT NULL REFERENCES Dataset(id),
    PRIMARY KEY (parent_id, child_id)
);

CREATE TABLE DataAdditionalProperty (
    data_id          TEXT NOT NULL REFERENCES Data(id),
    propertyvalue_id TEXT NOT NULL REFERENCES PropertyValue(id),
    PRIMARY KEY (data_id, propertyvalue_id)
);

CREATE TABLE DatasetAdditionalProperty (
    dataset_id       TEXT NOT NULL REFERENCES Dataset(id),
    propertyvalue_id TEXT NOT NULL REFERENCES PropertyValue(id),
    PRIMARY KEY (dataset_id, propertyvalue_id)
);

CREATE TABLE MaterialAdditionalProperty (
    material_id      TEXT NOT NULL REFERENCES Material(id),
    propertyvalue_id TEXT NOT NULL REFERENCES PropertyValue(id),
    PRIMARY KEY (material_id, propertyvalue_id)
);

CREATE TABLE ProtocolAdditionalProperty (
    protocol_id      TEXT NOT NULL REFERENCES Protocol(id),
    propertyvalue_id TEXT NOT NULL REFERENCES PropertyValue(id),
    PRIMARY KEY (protocol_id, propertyvalue_id)
);

CREATE TABLE DefinedTermAdditionalProperty (
    definedterm_id   TEXT NOT NULL REFERENCES DefinedTerm(id),
    propertyvalue_id TEXT NOT NULL REFERENCES PropertyValue(id),
    PRIMARY KEY (definedterm_id, propertyvalue_id)
);

CREATE TABLE MaterialDerivesFrom (
    material_id        TEXT NOT NULL REFERENCES Material(id),
    source_material_id TEXT NOT NULL REFERENCES Material(id),
    PRIMARY KEY (material_id, source_material_id)
);

CREATE TABLE ProtocolComponent (
    protocol_id      TEXT NOT NULL REFERENCES Protocol(id),
    propertyvalue_id TEXT NOT NULL REFERENCES PropertyValue(id),
    role             TEXT NOT NULL,
    PRIMARY KEY (protocol_id, propertyvalue_id)
);

-- ============================================================
-- Indexes
-- ============================================================

CREATE INDEX idx_propertyvalue_additional_type ON PropertyValue(additional_type);
CREATE INDEX idx_dataset_additional_type       ON Dataset(additional_type);
CREATE INDEX idx_process_additional_type       ON Process(additional_type);
CREATE INDEX idx_material_additional_type      ON Material(additional_type);
CREATE INDEX idx_protocol_additional_type      ON Protocol(additional_type);
CREATE INDEX idx_data_additional_type          ON Data(additional_type);

CREATE INDEX idx_process_executes_protocol     ON Process(executes_protocol_id);
CREATE INDEX idx_process_input                 ON Process(input_type, input_id);
CREATE INDEX idx_process_output                ON Process(output_type, output_id);
CREATE INDEX idx_protocol_intended_use         ON Protocol(intended_use_id);
CREATE INDEX idx_dataset_main_entity           ON Dataset(main_entity_id);

-- ============================================================
-- Helper Views
-- ============================================================

CREATE VIEW NodeRef AS
SELECT 'Material' AS node_type, id AS node_id, name AS node_name
FROM Material
UNION ALL
SELECT 'Data' AS node_type,
       id AS node_id,
       CASE
           WHEN selector IS NOT NULL THEN path || '#' || selector
           ELSE path
       END AS node_name
FROM Data;

-- ============================================================
-- Paths (closure table approach)
-- ============================================================
-- `Paths` holds one row per maximal path through the process graph.
-- `PathSteps` holds one row per (path, edge) — the process chain.
-- Both tables are empty after schema creation; run `refresh_paths.sql`
-- after seeding or after any change to `Process`, `Material.name`,
-- or `Data.path` / `Data.selector`.

CREATE TABLE Paths (
    path_id       TEXT PRIMARY KEY,
    length        INTEGER NOT NULL,
    root_type     TEXT NOT NULL,
    root_id       TEXT NOT NULL,
    leaf_type     TEXT NOT NULL,
    leaf_id       TEXT NOT NULL,
    path_rendered TEXT
);

CREATE TABLE PathSteps (
    path_id     TEXT NOT NULL REFERENCES Paths(path_id) ON DELETE CASCADE,
    step        INTEGER NOT NULL,
    process_id  TEXT NOT NULL REFERENCES Process(id),
    input_type  TEXT NOT NULL,
    input_id    TEXT NOT NULL,
    output_type TEXT NOT NULL,
    output_id   TEXT NOT NULL,
    PRIMARY KEY (path_id, step)
);

CREATE INDEX idx_pathsteps_process ON PathSteps(process_id);
CREATE INDEX idx_pathsteps_input   ON PathSteps(input_type, input_id);
CREATE INDEX idx_pathsteps_output  ON PathSteps(output_type, output_id);
CREATE INDEX idx_paths_leaf        ON Paths(leaf_type, leaf_id);
CREATE INDEX idx_paths_root        ON Paths(root_type, root_id);

-- ============================================================
-- ISA Decoration Views
-- ============================================================

CREATE VIEW Investigation AS
SELECT id, type, identifier, name, description,
       license, date_published, date_created
FROM Dataset WHERE additional_type = 'Investigation';

CREATE VIEW Study AS
SELECT id, type, identifier, name, description
FROM Dataset WHERE additional_type = 'Study';

CREATE VIEW Assay AS
SELECT id, type, identifier, name, description,
       measurement_method, measurement_technique, variable_measured
FROM Dataset WHERE additional_type = 'Assay';

CREATE VIEW ParameterValue AS
SELECT id, type, name, value, property_id, unit_code, unit_text, value_reference
FROM PropertyValue WHERE additional_type = 'ParameterValue';

CREATE VIEW CharacteristicValue AS
SELECT id, type, name, value, property_id, unit_code, unit_text, value_reference
FROM PropertyValue WHERE additional_type = 'CharacteristicValue';

CREATE VIEW FactorValue AS
SELECT id, type, name, value, property_id, unit_code, unit_text, value_reference
FROM PropertyValue WHERE additional_type = 'FactorValue';

CREATE VIEW Component AS
SELECT id, type, name, value, property_id, value_reference
FROM PropertyValue WHERE additional_type = 'Component';

-- ============================================================
-- Workflow Run Decoration Views
-- ============================================================

CREATE VIEW ArcWorkflow AS
SELECT id, type, identifier, name, description, main_entity_id
FROM Dataset WHERE additional_type = 'ARC Workflow';

CREATE VIEW ArcRun AS
SELECT id, type, identifier, name, description,
       conforms_to, measurement_method, measurement_technique, variable_measured
FROM Dataset WHERE additional_type = 'ARC Run';

CREATE VIEW WorkflowInvocation AS
SELECT id, type, name, executes_protocol_id,
       input_type, input_id, output_type, output_id,
       end_time, description
FROM Process WHERE additional_type = 'Workflow Invocation';

CREATE VIEW WorkflowInput AS
SELECT id, type, name, value, example_of_work
FROM PropertyValue WHERE additional_type = 'Workflow Input';
