-- ARC Data Model – Simplified SQLite Schema
-- Minimal graph-first schema:
-- Dataset, Material, Data, Process, PropertyValue

PRAGMA foreign_keys = ON;
PRAGMA journal_mode = WAL;

-- ============================================================
-- Core Tables
-- ============================================================

CREATE TABLE Dataset (
    id          TEXT PRIMARY KEY,
    name        TEXT NOT NULL,
    description TEXT
);

CREATE TABLE Material (
    id          TEXT PRIMARY KEY,
    dataset_id  TEXT NOT NULL REFERENCES Dataset(id),
    name        TEXT NOT NULL,
    kind        TEXT
);

CREATE TABLE Data (
    id              TEXT PRIMARY KEY,
    dataset_id      TEXT NOT NULL REFERENCES Dataset(id),
    path            TEXT NOT NULL,
    selector        TEXT,
    encoding_format TEXT
);

CREATE TABLE Process (
    id           TEXT PRIMARY KEY,
    dataset_id   TEXT NOT NULL REFERENCES Dataset(id),
    name         TEXT NOT NULL,
    input_type   TEXT NOT NULL CHECK (input_type IN ('Material', 'Data')),
    input_id     TEXT NOT NULL,
    output_type  TEXT NOT NULL CHECK (output_type IN ('Material', 'Data')),
    output_id    TEXT NOT NULL
);

CREATE TABLE PropertyValue (
    id          TEXT PRIMARY KEY,
    dataset_id  TEXT NOT NULL REFERENCES Dataset(id),
    owner_type  TEXT NOT NULL CHECK (owner_type IN ('Dataset', 'Process')),
    owner_id    TEXT NOT NULL,
    name        TEXT NOT NULL,
    value       TEXT,
    unit        TEXT
);

-- ============================================================
-- Indexes
-- ============================================================

CREATE INDEX idx_material_dataset ON Material(dataset_id);
CREATE INDEX idx_data_dataset     ON Data(dataset_id);
CREATE INDEX idx_process_dataset  ON Process(dataset_id);
CREATE INDEX idx_process_input    ON Process(dataset_id, input_type, input_id);
CREATE INDEX idx_process_output   ON Process(dataset_id, output_type, output_id);
CREATE INDEX idx_property_dataset ON PropertyValue(dataset_id);
CREATE INDEX idx_property_owner   ON PropertyValue(dataset_id, owner_type, owner_id);
CREATE INDEX idx_property_name    ON PropertyValue(dataset_id, name);

-- ============================================================
-- Helper Views
-- ============================================================

CREATE VIEW NodeRef AS
SELECT
    dataset_id,
    'Material' AS node_type,
    id AS node_id,
    name AS node_name
FROM Material
UNION ALL
SELECT
    dataset_id,
    'Data' AS node_type,
    id AS node_id,
    CASE
        WHEN selector IS NOT NULL THEN path || '#' || selector
        ELSE path
    END AS node_name
FROM Data;

-- ============================================================
-- Path Views
-- ============================================================

CREATE VIEW _LeafWalks AS
WITH RECURSIVE walk(
    dataset_id,
    path_id,
    path_rendered,
    steps_json,
    root_type,
    root_id,
    current_type,
    current_id,
    depth
) AS (
    SELECT
        p.dataset_id,
        p.input_type || ':' || p.input_id || '|' || p.output_type || ':' || p.output_id,
        in_nr.node_name || ' -> ' || out_nr.node_name,
        json_array(json_object(
            'step',        0,
            'process_id',  p.id,
            'input_type',  p.input_type,
            'input_id',    p.input_id,
            'output_type', p.output_type,
            'output_id',   p.output_id
        )),
        p.input_type,
        p.input_id,
        p.output_type,
        p.output_id,
        1
    FROM Process p
    JOIN NodeRef in_nr
      ON in_nr.dataset_id = p.dataset_id
     AND in_nr.node_type  = p.input_type
     AND in_nr.node_id    = p.input_id
    JOIN NodeRef out_nr
      ON out_nr.dataset_id = p.dataset_id
     AND out_nr.node_type  = p.output_type
     AND out_nr.node_id    = p.output_id
    WHERE NOT EXISTS (
        SELECT 1
        FROM Process prev
        WHERE prev.dataset_id  = p.dataset_id
          AND prev.output_type = p.input_type
          AND prev.output_id   = p.input_id
    )

    UNION ALL

    SELECT
        w.dataset_id,
        w.path_id || '|' || p.output_type || ':' || p.output_id,
        w.path_rendered || ' -> ' || out_nr.node_name,
        json_insert(w.steps_json, '$[#]', json_object(
            'step',        w.depth,
            'process_id',  p.id,
            'input_type',  p.input_type,
            'input_id',    p.input_id,
            'output_type', p.output_type,
            'output_id',   p.output_id
        )),
        w.root_type,
        w.root_id,
        p.output_type,
        p.output_id,
        w.depth + 1
    FROM walk w
    JOIN Process p
      ON p.dataset_id = w.dataset_id
     AND p.input_type = w.current_type
     AND p.input_id   = w.current_id
    JOIN NodeRef out_nr
      ON out_nr.dataset_id = p.dataset_id
     AND out_nr.node_type  = p.output_type
     AND out_nr.node_id    = p.output_id
    WHERE w.depth < 100
)
SELECT
    dataset_id,
    path_id,
    path_rendered,
    steps_json,
    root_type,
    root_id,
    current_type AS leaf_type,
    current_id   AS leaf_id,
    depth        AS length
FROM walk
WHERE NOT EXISTS (
    SELECT 1
    FROM Process next
    WHERE next.dataset_id = walk.dataset_id
      AND next.input_type = walk.current_type
      AND next.input_id   = walk.current_id
);

CREATE VIEW PathSteps AS
SELECT
    lw.dataset_id,
    lw.path_id,
    CAST(json_extract(step.value, '$.step')        AS INTEGER) AS step,
    json_extract(step.value, '$.process_id')  AS process_id,
    json_extract(step.value, '$.input_type')  AS input_type,
    json_extract(step.value, '$.input_id')    AS input_id,
    json_extract(step.value, '$.output_type') AS output_type,
    json_extract(step.value, '$.output_id')   AS output_id
FROM _LeafWalks lw, json_each(lw.steps_json) step;

CREATE VIEW Paths AS
SELECT
    dataset_id,
    path_id,
    length,
    root_type,
    root_id,
    leaf_type,
    leaf_id,
    path_rendered
FROM _LeafWalks;
