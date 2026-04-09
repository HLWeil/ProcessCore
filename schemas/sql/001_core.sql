-- ARC Data Model – SQLite Schema
-- ProcessCore tables, junction tables, decoration views, and Paths view.

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
    id                          TEXT PRIMARY KEY,
    type                        TEXT NOT NULL,
    name                        TEXT NOT NULL,
    encoding_format             TEXT,
    disambiguating_description  TEXT
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
    end_time                    TEXT,
    disambiguating_description  TEXT,
    description                 TEXT
);

-- ============================================================
-- Junction / Relationship Tables
-- ============================================================

-- Process I/O (split by target type for proper FKs)

CREATE TABLE ProcessObjectMaterial (
    process_id  TEXT NOT NULL REFERENCES Process(id),
    material_id TEXT NOT NULL REFERENCES Material(id),
    PRIMARY KEY (process_id, material_id)
);

CREATE TABLE ProcessObjectData (
    process_id TEXT NOT NULL REFERENCES Process(id),
    data_id    TEXT NOT NULL REFERENCES Data(id),
    PRIMARY KEY (process_id, data_id)
);

CREATE TABLE ProcessResultMaterial (
    process_id  TEXT NOT NULL REFERENCES Process(id),
    material_id TEXT NOT NULL REFERENCES Material(id),
    PRIMARY KEY (process_id, material_id)
);

CREATE TABLE ProcessResultData (
    process_id TEXT NOT NULL REFERENCES Process(id),
    data_id    TEXT NOT NULL REFERENCES Data(id),
    PRIMARY KEY (process_id, data_id)
);

-- Process -> PropertyValue

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

-- Dataset relationships

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

-- AdditionalProperty per entity type

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

-- Other relationships

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

CREATE INDEX idx_process_executes_protocol     ON Process(executes_protocol_id);
CREATE INDEX idx_protocol_intended_use         ON Protocol(intended_use_id);
CREATE INDEX idx_dataset_main_entity           ON Dataset(main_entity_id);

-- ============================================================
-- Helper Views
-- ============================================================

-- Unified node reference (Material + Data)
CREATE VIEW NodeRef AS
SELECT 'Material' AS node_type, id AS node_id, name AS node_name FROM Material
UNION ALL
SELECT 'Data',                  id,             name             FROM Data;

-- All (object -> result) edges through processes
CREATE VIEW ProcessEdge AS
SELECT pom.process_id, p.name AS process_name,
       'Material' AS object_type, pom.material_id AS object_id,
       'Material' AS result_type, prm.material_id AS result_id
FROM ProcessObjectMaterial pom
JOIN ProcessResultMaterial prm ON pom.process_id = prm.process_id
JOIN Process p ON p.id = pom.process_id
UNION ALL
SELECT pom.process_id, p.name,
       'Material', pom.material_id,
       'Data',     prd.data_id
FROM ProcessObjectMaterial pom
JOIN ProcessResultData prd ON pom.process_id = prd.process_id
JOIN Process p ON p.id = pom.process_id
UNION ALL
SELECT pod.process_id, p.name,
       'Data', pod.data_id,
       'Material', prm.material_id
FROM ProcessObjectData pod
JOIN ProcessResultMaterial prm ON pod.process_id = prm.process_id
JOIN Process p ON p.id = pod.process_id
UNION ALL
SELECT pod.process_id, p.name,
       'Data', pod.data_id,
       'Data', prd.data_id
FROM ProcessObjectData pod
JOIN ProcessResultData prd ON pod.process_id = prd.process_id
JOIN Process p ON p.id = pod.process_id;

-- ============================================================
-- Paths View
-- ============================================================

-- Traces all maximal chains through the process graph.
-- A root node appears as an object but never as a result.
-- A leaf node appears as a result but never as an object.
-- Each row is one full path from root to leaf, rendered as
-- "NodeA -> NodeB -> NodeC -> ...".
CREATE VIEW Paths AS
WITH RECURSIVE
  -- Root nodes: appear as objects but never as results
  Roots AS (
    SELECT DISTINCT object_type AS node_type, object_id AS node_id
    FROM ProcessEdge
    WHERE NOT EXISTS (
      SELECT 1 FROM ProcessEdge e2
      WHERE e2.result_type = ProcessEdge.object_type
        AND e2.result_id   = ProcessEdge.object_id
    )
  ),
  PathTrace AS (
    -- Base case: start from each root
    SELECT
      r.node_type  AS current_type,
      r.node_id    AS current_id,
      nr.node_name AS path,
      0            AS depth
    FROM Roots r
    JOIN NodeRef nr ON nr.node_type = r.node_type AND nr.node_id = r.node_id

    UNION ALL

    -- Recursive step: follow edges
    SELECT
      pe.result_type,
      pe.result_id,
      pt.path || ' -> ' || nr.node_name,
      pt.depth + 1
    FROM PathTrace pt
    JOIN ProcessEdge pe
      ON pe.object_type = pt.current_type
     AND pe.object_id   = pt.current_id
    JOIN NodeRef nr
      ON nr.node_type = pe.result_type
     AND nr.node_id   = pe.result_id
    WHERE pt.depth < 100
  )
-- Keep only leaf paths (current node is never an object of any edge)
SELECT path, depth
FROM PathTrace pt
WHERE NOT EXISTS (
  SELECT 1 FROM ProcessEdge pe
  WHERE pe.object_type = pt.current_type
    AND pe.object_id   = pt.current_id
);

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
SELECT id, type, name, executes_protocol_id, end_time, description
FROM Process WHERE additional_type = 'Workflow Invocation';

CREATE VIEW WorkflowInput AS
SELECT id, type, name, value, example_of_work
FROM PropertyValue WHERE additional_type = 'Workflow Input';
