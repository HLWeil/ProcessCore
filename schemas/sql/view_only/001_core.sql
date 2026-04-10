-- ARC Data Model – SQLite Schema
-- ProcessCore tables, edge-native Process rows, decoration views,
-- and recursive lineage views.

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
-- Paths (view-only approach)
-- ============================================================
-- `_LeafWalks` is an internal helper view that runs the recursive
-- graph walk directly over `Process`, carrying
-- (path_id, path_rendered, steps_json, leaf_*, length)
-- for each maximal walk. Both `PathSteps` and `Paths` project
-- from it, so the recursive CTE is written once.

CREATE VIEW _LeafWalks AS
WITH RECURSIVE walk(
    path_id, path_rendered, steps_json, current_type, current_id, depth
) AS (
    SELECT
        p.input_type || ':' || p.input_id || '|' || p.output_type || ':' || p.output_id,
        in_nr.node_name || ' -> ' || out_nr.node_name,
        json_array(json_object(
            'step',        0,
            'process_id',  p.id,
            'input_type',  p.input_type,  'input_id',  p.input_id,
            'output_type', p.output_type, 'output_id', p.output_id
        )),
        p.output_type, p.output_id,
        0
    FROM Process p
    JOIN NodeRef in_nr
      ON in_nr.node_type = p.input_type
     AND in_nr.node_id   = p.input_id
    JOIN NodeRef out_nr
      ON out_nr.node_type = p.output_type
     AND out_nr.node_id   = p.output_id
    WHERE NOT EXISTS (
        SELECT 1 FROM Process prev
        WHERE prev.output_type = p.input_type
          AND prev.output_id   = p.input_id
    )

    UNION ALL

    SELECT
        w.path_id || '|' || p.output_type || ':' || p.output_id,
        w.path_rendered || ' -> ' || out_nr.node_name,
        json_insert(w.steps_json, '$[#]', json_object(
            'step',        w.depth + 1,
            'process_id',  p.id,
            'input_type',  p.input_type,  'input_id',  p.input_id,
            'output_type', p.output_type, 'output_id', p.output_id
        )),
        p.output_type, p.output_id,
        w.depth + 1
    FROM walk w
    JOIN Process p
      ON p.input_type = w.current_type
     AND p.input_id   = w.current_id
    JOIN NodeRef out_nr
      ON out_nr.node_type = p.output_type
     AND out_nr.node_id   = p.output_id
    WHERE w.depth < 100
)
SELECT
    w.path_id,
    w.path_rendered,
    w.steps_json,
    w.current_type AS leaf_type,
    w.current_id   AS leaf_id,
    w.depth + 1    AS length
FROM walk w
WHERE NOT EXISTS (
    SELECT 1 FROM Process p
    WHERE p.input_type = w.current_type
      AND p.input_id   = w.current_id
);

CREATE VIEW PathSteps AS
SELECT
    lw.path_id,
    CAST(json_extract(s.value, '$.step')        AS INTEGER) AS step,
    json_extract(s.value, '$.process_id')  AS process_id,
    json_extract(s.value, '$.input_type')  AS input_type,
    json_extract(s.value, '$.input_id')    AS input_id,
    json_extract(s.value, '$.output_type') AS output_type,
    json_extract(s.value, '$.output_id')   AS output_id
FROM _LeafWalks lw, json_each(lw.steps_json) s;

CREATE VIEW Paths AS
SELECT
    path_id,
    length,
    json_extract(steps_json, '$[0].input_type') AS root_type,
    json_extract(steps_json, '$[0].input_id')   AS root_id,
    leaf_type,
    leaf_id,
    path_rendered
FROM _LeafWalks;

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
