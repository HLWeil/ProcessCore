PRAGMA foreign_keys = ON;

BEGIN;

INSERT INTO defined_term (id, type, name, tan, in_defined_term_set_id, in_defined_term_set_name)
VALUES
  ('obo:OBI_0000070', 'schema:DefinedTerm', 'assay', 'OBI:0000070', 'http://purl.obolibrary.org/obo/obi.owl', 'Ontology for Biomedical Investigations'),
  ('obo:NCIT_C16681', 'schema:DefinedTerm', 'temperature', 'NCIT:C16681', 'https://ncithesaurus.nci.nih.gov', 'NCI Thesaurus'),
  ('obo:UO_0000027', 'schema:DefinedTerm', 'degree Celsius', 'UO:0000027', 'http://purl.obolibrary.org/obo/uo.owl', 'Units of measurement ontology');

INSERT INTO lab_protocol (id, type, additional_type, name, description, version, url, intended_use_id, intended_use_text)
VALUES
  ('protocol:growth', 'bioschemas:LabProtocol', 'LabProtocol', 'Plant growth', 'Grow source material under controlled temperature.', '1.0', NULL, 'obo:OBI_0000070', NULL),
  ('protocol:proteomics', 'bioschemas:LabProtocol', 'LabProtocol', 'Proteomics measurement', 'Measure protein abundance by mass spectrometry.', '1.0', NULL, NULL, 'proteomics assay');

INSERT INTO formal_parameter (id, type, name, name_tan, default_value_id)
VALUES
  ('param:growth-temperature', 'bioschemas:FormalParameter', 'growth temperature', 'obo:NCIT_C16681', NULL);

INSERT INTO protocol_parameter (protocol_id, position, formal_parameter_id)
VALUES
  ('protocol:growth', 0, 'param:growth-temperature');

INSERT INTO dataset (id, type, additional_type, identifier, name, description)
VALUES
  ('dataset:proteomics-assay', 'schema:Dataset', 'Assay', 'assay-proteomics-001', 'Proteomics assay example', 'Seed dataset for the SQL import profile.');

INSERT INTO material (id, type, additional_type, name)
VALUES
  ('material:source-1', 'bioschemas:Sample', 'Source', 'Arabidopsis source 1'),
  ('material:sample-1', 'bioschemas:Sample', 'Sample', 'Arabidopsis sample 1');

INSERT INTO data (id, type, additional_type, path, selector, selector_format, encoding_format)
VALUES
  ('data:raw-spectrum', 'File', 'Raw Data', 'assays/proteomics/raw/sample-1.mzML', NULL, NULL, 'application/mzml+xml'),
  ('data:protein-table', 'File', 'Processed Data', 'assays/proteomics/processed/proteins.csv', NULL, NULL, 'text/csv'),
  ('data:protein-table#abundance', 'File', 'Data Fragment', 'assays/proteomics/processed/proteins.csv', 'col=abundance', 'https://www.rfc-editor.org/rfc/rfc7111', 'text/csv');

INSERT INTO lab_process (id, type, additional_type, name, executes_protocol_id)
VALUES
  ('process:growth-1', 'bioschemas:LabProcess', 'LabProcess', 'Grow source 1', 'protocol:growth'),
  ('process:measure-1', 'bioschemas:LabProcess', 'LabProcess', 'Measure sample 1', 'protocol:proteomics');

INSERT INTO property_value (id, type, additional_type, name, value, unit, name_tan, value_tan, unit_tan, instance_of_id)
VALUES
  ('pv:growth-temperature-22c', 'schema:PropertyValue', 'ParameterValue', 'growth temperature', '22', 'degree Celsius', 'obo:NCIT_C16681', NULL, 'obo:UO_0000027', 'param:growth-temperature'),
  ('pv:dataset-organism', 'schema:PropertyValue', 'CharacteristicValue', 'organism', 'Arabidopsis thaliana', NULL, NULL, NULL, NULL, NULL),
  ('pv:source-genotype', 'schema:PropertyValue', 'CharacteristicValue', 'genotype', 'Col-0', NULL, NULL, NULL, NULL, NULL),
  ('pv:raw-format', 'schema:PropertyValue', 'CharacteristicValue', 'file role', 'raw spectrum', NULL, NULL, NULL, NULL, NULL),
  ('pv:protocol-instrument', 'schema:PropertyValue', 'Component', 'instrument', 'Q Exactive', NULL, NULL, NULL, NULL, NULL);

INSERT INTO dataset_has_part (dataset_id, position, part_dataset_id, part_data_id)
VALUES
  ('dataset:proteomics-assay', 0, NULL, 'data:raw-spectrum'),
  ('dataset:proteomics-assay', 1, NULL, 'data:protein-table');

INSERT INTO dataset_process (dataset_id, position, process_id)
VALUES
  ('dataset:proteomics-assay', 0, 'process:growth-1'),
  ('dataset:proteomics-assay', 1, 'process:measure-1');

INSERT INTO dataset_additional_property (dataset_id, position, property_value_id)
VALUES
  ('dataset:proteomics-assay', 0, 'pv:dataset-organism');

INSERT INTO material_additional_property (material_id, position, property_value_id)
VALUES
  ('material:source-1', 0, 'pv:source-genotype');

INSERT INTO data_additional_property (data_id, position, property_value_id)
VALUES
  ('data:raw-spectrum', 0, 'pv:raw-format');

INSERT INTO protocol_additional_property (protocol_id, position, property_value_id)
VALUES
  ('protocol:proteomics', 0, 'pv:protocol-instrument');

INSERT INTO process_parameter_value (process_id, position, property_value_id)
VALUES
  ('process:growth-1', 0, 'pv:growth-temperature-22c');

INSERT INTO process_io (process_id, direction, position, material_id, data_id)
VALUES
  ('process:growth-1', 'input', 0, 'material:source-1', NULL),
  ('process:growth-1', 'output', 0, 'material:sample-1', NULL),
  ('process:measure-1', 'input', 0, 'material:sample-1', NULL),
  ('process:measure-1', 'output', 0, NULL, 'data:raw-spectrum'),
  ('process:measure-1', 'output', 1, NULL, 'data:protein-table');

COMMIT;
