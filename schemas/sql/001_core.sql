PRAGMA foreign_keys = ON;

DROP VIEW IF EXISTS annotation_orphans;
DROP VIEW IF EXISTS process_edges;

DROP TABLE IF EXISTS data_additional_property;
DROP TABLE IF EXISTS sample_additional_property;
DROP TABLE IF EXISTS protocol_additional_property;
DROP TABLE IF EXISTS process_parameter_value;
DROP TABLE IF EXISTS process_io;
DROP TABLE IF EXISTS protocol_parameter;
DROP TABLE IF EXISTS dataset_additional_property;
DROP TABLE IF EXISTS dataset_process;
DROP TABLE IF EXISTS dataset_has_part;
DROP TABLE IF EXISTS annotation;
DROP TABLE IF EXISTS process;
DROP TABLE IF EXISTS data;
DROP TABLE IF EXISTS sample;
DROP TABLE IF EXISTS dataset;
DROP TABLE IF EXISTS formal_parameter;
DROP TABLE IF EXISTS recipe;
DROP TABLE IF EXISTS defined_term;

CREATE TABLE defined_term (
  id TEXT PRIMARY KEY,
  type TEXT NOT NULL,
  name TEXT NOT NULL,
  tan TEXT,
  in_defined_term_set_id TEXT,
  in_defined_term_set_name TEXT
);

CREATE TABLE recipe (
  id TEXT PRIMARY KEY,
  type TEXT NOT NULL,
  additional_type TEXT,
  name TEXT,
  description TEXT,
  version TEXT,
  url TEXT,
  intended_use_id TEXT REFERENCES defined_term(id) ON DELETE RESTRICT,
  intended_use_text TEXT,
  CHECK (intended_use_id IS NULL OR intended_use_text IS NULL)
);

CREATE TABLE formal_parameter (
  id TEXT PRIMARY KEY,
  type TEXT NOT NULL,
  name TEXT,
  name_tan TEXT,
  default_value_id TEXT REFERENCES defined_term(id) ON DELETE RESTRICT
);

CREATE TABLE dataset (
  id TEXT PRIMARY KEY,
  type TEXT NOT NULL,
  additional_type TEXT,
  identifier TEXT NOT NULL,
  title TEXT,
  description TEXT
);

CREATE TABLE sample (
  id TEXT PRIMARY KEY,
  type TEXT NOT NULL,
  additional_type TEXT,
  name TEXT NOT NULL
);

CREATE TABLE data (
  id TEXT PRIMARY KEY,
  type TEXT NOT NULL,
  additional_type TEXT,
  path TEXT NOT NULL,
  selector TEXT,
  selector_format TEXT,
  encoding_format TEXT,
  fragment_identity TEXT GENERATED ALWAYS AS (
    CASE
      WHEN selector IS NULL THEN path
      ELSE path || char(31) || selector || char(31) || coalesce(selector_format, '')
    END
  ) STORED,
  UNIQUE (fragment_identity)
);

CREATE TABLE process (
  id TEXT PRIMARY KEY,
  type TEXT NOT NULL,
  additional_type TEXT,
  name TEXT NOT NULL,
  executes_protocol_id TEXT REFERENCES recipe(id) ON DELETE RESTRICT
);

CREATE TABLE annotation (
  id TEXT PRIMARY KEY,
  type TEXT NOT NULL,
  additional_type TEXT,
  name TEXT NOT NULL,
  value TEXT,
  unit TEXT,
  name_tan TEXT,
  value_tan TEXT,
  unit_tan TEXT,
  instance_of_id TEXT REFERENCES formal_parameter(id) ON DELETE RESTRICT
);

CREATE TABLE dataset_has_part (
  dataset_id TEXT NOT NULL REFERENCES dataset(id) ON DELETE CASCADE,
  position INTEGER NOT NULL CHECK (position >= 0),
  part_dataset_id TEXT REFERENCES dataset(id) ON DELETE RESTRICT,
  part_data_id TEXT REFERENCES data(id) ON DELETE RESTRICT,
  PRIMARY KEY (dataset_id, position),
  CHECK (
    (part_dataset_id IS NOT NULL AND part_data_id IS NULL)
    OR
    (part_dataset_id IS NULL AND part_data_id IS NOT NULL)
  )
);

CREATE TABLE dataset_process (
  dataset_id TEXT NOT NULL REFERENCES dataset(id) ON DELETE CASCADE,
  position INTEGER NOT NULL CHECK (position >= 0),
  process_id TEXT NOT NULL REFERENCES process(id) ON DELETE RESTRICT,
  PRIMARY KEY (dataset_id, position)
);

CREATE TABLE dataset_additional_property (
  dataset_id TEXT NOT NULL REFERENCES dataset(id) ON DELETE CASCADE,
  position INTEGER NOT NULL CHECK (position >= 0),
  annotation_id TEXT NOT NULL REFERENCES annotation(id) ON DELETE RESTRICT,
  PRIMARY KEY (dataset_id, position)
);

CREATE TABLE protocol_parameter (
  protocol_id TEXT NOT NULL REFERENCES recipe(id) ON DELETE CASCADE,
  position INTEGER NOT NULL CHECK (position >= 0),
  formal_parameter_id TEXT NOT NULL REFERENCES formal_parameter(id) ON DELETE RESTRICT,
  PRIMARY KEY (protocol_id, position)
);

CREATE TABLE process_io (
  process_id TEXT NOT NULL REFERENCES process(id) ON DELETE CASCADE,
  direction TEXT NOT NULL CHECK (direction IN ('input', 'output')),
  position INTEGER NOT NULL CHECK (position >= 0),
  sample_id TEXT REFERENCES sample(id) ON DELETE RESTRICT,
  data_id TEXT REFERENCES data(id) ON DELETE RESTRICT,
  PRIMARY KEY (process_id, direction, position),
  CHECK (
    (sample_id IS NOT NULL AND data_id IS NULL)
    OR
    (sample_id IS NULL AND data_id IS NOT NULL)
  )
);

CREATE TABLE process_parameter_value (
  process_id TEXT NOT NULL REFERENCES process(id) ON DELETE CASCADE,
  position INTEGER NOT NULL CHECK (position >= 0),
  annotation_id TEXT NOT NULL REFERENCES annotation(id) ON DELETE RESTRICT,
  PRIMARY KEY (process_id, position)
);

CREATE TABLE protocol_additional_property (
  protocol_id TEXT NOT NULL REFERENCES recipe(id) ON DELETE CASCADE,
  position INTEGER NOT NULL CHECK (position >= 0),
  annotation_id TEXT NOT NULL REFERENCES annotation(id) ON DELETE RESTRICT,
  PRIMARY KEY (protocol_id, position)
);

CREATE TABLE sample_additional_property (
  sample_id TEXT NOT NULL REFERENCES sample(id) ON DELETE CASCADE,
  position INTEGER NOT NULL CHECK (position >= 0),
  annotation_id TEXT NOT NULL REFERENCES annotation(id) ON DELETE RESTRICT,
  PRIMARY KEY (sample_id, position)
);

CREATE TABLE data_additional_property (
  data_id TEXT NOT NULL REFERENCES data(id) ON DELETE CASCADE,
  position INTEGER NOT NULL CHECK (position >= 0),
  annotation_id TEXT NOT NULL REFERENCES annotation(id) ON DELETE RESTRICT,
  PRIMARY KEY (data_id, position)
);

CREATE INDEX idx_process_io_input_sample
  ON process_io(sample_id, process_id)
  WHERE direction = 'input' AND sample_id IS NOT NULL;

CREATE INDEX idx_process_io_output_sample
  ON process_io(sample_id, process_id)
  WHERE direction = 'output' AND sample_id IS NOT NULL;

CREATE INDEX idx_process_io_input_data
  ON process_io(data_id, process_id)
  WHERE direction = 'input' AND data_id IS NOT NULL;

CREATE INDEX idx_process_io_output_data
  ON process_io(data_id, process_id)
  WHERE direction = 'output' AND data_id IS NOT NULL;

CREATE INDEX idx_dataset_process_process
  ON dataset_process(process_id);

CREATE INDEX idx_process_protocol
  ON process(executes_protocol_id);

CREATE INDEX idx_recipe_intended_use
  ON recipe(intended_use_id);

CREATE INDEX idx_process_parameter_value_property
  ON process_parameter_value(annotation_id);

CREATE INDEX idx_annotation_instance_of
  ON annotation(instance_of_id);

CREATE INDEX idx_annotation_name_value
  ON annotation(name_tan, value);

CREATE INDEX idx_dataset_additional_annotation
  ON dataset_additional_property(annotation_id);

CREATE INDEX idx_protocol_additional_annotation
  ON protocol_additional_property(annotation_id);

CREATE INDEX idx_sample_additional_annotation
  ON sample_additional_property(annotation_id);

CREATE INDEX idx_data_additional_annotation
  ON data_additional_property(annotation_id);

CREATE VIEW process_edges AS
SELECT
  consumed.process_id,
  consumed.position AS input_position,
  produced.position AS output_position,
  CASE WHEN consumed.sample_id IS NOT NULL THEN 'sample' ELSE 'data' END AS input_kind,
  coalesce(consumed.sample_id, consumed.data_id) AS input_id,
  CASE WHEN produced.sample_id IS NOT NULL THEN 'sample' ELSE 'data' END AS output_kind,
  coalesce(produced.sample_id, produced.data_id) AS output_id
FROM process_io AS consumed
JOIN process_io AS produced
  ON produced.process_id = consumed.process_id
 AND produced.direction = 'output'
WHERE consumed.direction = 'input';

CREATE VIEW annotation_orphans AS
SELECT pv.id
FROM annotation AS pv
WHERE NOT EXISTS (
  SELECT 1 FROM dataset_additional_property AS a WHERE a.annotation_id = pv.id
)
AND NOT EXISTS (
  SELECT 1 FROM protocol_additional_property AS a WHERE a.annotation_id = pv.id
)
AND NOT EXISTS (
  SELECT 1 FROM sample_additional_property AS a WHERE a.annotation_id = pv.id
)
AND NOT EXISTS (
  SELECT 1 FROM data_additional_property AS a WHERE a.annotation_id = pv.id
)
AND NOT EXISTS (
  SELECT 1 FROM process_parameter_value AS a WHERE a.annotation_id = pv.id
);
