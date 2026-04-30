-- Seed data based on examples/isa/assay_proteomics.yml
-- Core-only example using the current ProcessCore entity and relation names.

-- ============================================================
-- Defined Terms
-- ============================================================

INSERT INTO DefinedTerm (id, type, name, tan, in_defined_term_set) VALUES
  ('https://example.org/terms/protocol/growth',                 'schema.org/DefinedTerm', 'Growth protocol',                            'growth-protocol',                 'https://example.org/terms/protocol'),
  ('https://example.org/terms/protocol/cell-lysis',            'schema.org/DefinedTerm', 'Cell lysis protocol',                        'cell-lysis-protocol',            'https://example.org/terms/protocol'),
  ('https://example.org/terms/protocol/ms-run',                'schema.org/DefinedTerm', 'Mass spectrometry run protocol',             'ms-run-protocol',                'https://example.org/terms/protocol'),
  ('https://example.org/terms/protocol/computational-analysis','schema.org/DefinedTerm', 'Computational proteome analysis protocol',  'computational-analysis-protocol','https://example.org/terms/protocol');

-- ============================================================
-- Protocols And Formal Parameters
-- ============================================================

INSERT INTO LabProtocol (id, type, name, intended_use_id) VALUES
  ('#Protocol_Growth',                          'bioschemas.org/LabProtocol', 'Growth',                           'https://example.org/terms/protocol/growth'),
  ('#Protocol_Cell_Lysis',                      'bioschemas.org/LabProtocol', 'Cell Lysis',                       'https://example.org/terms/protocol/cell-lysis'),
  ('#Protocol_MS_Run',                          'bioschemas.org/LabProtocol', 'MS Run',                           'https://example.org/terms/protocol/ms-run'),
  ('#Protocol_Computational_Proteome_Analysis', 'bioschemas.org/LabProtocol', 'Computational Proteome Analysis',  'https://example.org/terms/protocol/computational-analysis');

INSERT INTO FormalParameter (id, type, name, name_tan) VALUES
  ('#FP_sonicator',                'bioschemas.org/FormalParameter', 'sonicator',                 'https://bioregistry.io/OBI:0400114'),
  ('#FP_time',                     'bioschemas.org/FormalParameter', 'time',                      'https://bioregistry.io/PATO:0000165'),
  ('#FP_technical_replicate_group','bioschemas.org/FormalParameter', 'technical replicate group', 'https://bioregistry.io/DPBO:1000184'),
  ('#FP_software',                 'bioschemas.org/FormalParameter', 'software',                  'https://bioregistry.io/IAO_0000010');

INSERT INTO LabProtocolParameter (lab_protocol_id, formal_parameter_id) VALUES
  ('#Protocol_Cell_Lysis', '#FP_sonicator'),
  ('#Protocol_Cell_Lysis', '#FP_time'),
  ('#Protocol_Cell_Lysis', '#FP_technical_replicate_group'),
  ('#Protocol_Computational_Proteome_Analysis', '#FP_software');

-- ============================================================
-- Property Values
-- ============================================================

INSERT INTO PropertyValue (
  id, type, additional_type, name, value, unit,
  name_tan, value_tan, unit_tan, instance_of_id
) VALUES
  ('#PV_sonicator',       'schema.org/PropertyValue', 'ParameterValue',      'sonicator',                 'Fisherbrand Model 705 Sonic Dismembrator', NULL,             'https://bioregistry.io/OBI:0400114', NULL,                                      NULL,                                     '#FP_sonicator'),
  ('#PV_time_10min',      'schema.org/PropertyValue', 'ParameterValue',      'time',                      '10',                                      'minute',         'https://bioregistry.io/PATO:0000165', NULL,                                      'https://bioregistry.io/UO:0000031',      '#FP_time'),
  ('#PV_replicate_1',     'schema.org/PropertyValue', 'ParameterValue',      'technical replicate group', '1',                                       NULL,             'https://bioregistry.io/DPBO:1000184', NULL,                                      NULL,                                     '#FP_technical_replicate_group'),
  ('#PV_replicate_2',     'schema.org/PropertyValue', 'ParameterValue',      'technical replicate group', '2',                                       NULL,             'https://bioregistry.io/DPBO:1000184', NULL,                                      NULL,                                     '#FP_technical_replicate_group'),
  ('#PV_replicate_3',     'schema.org/PropertyValue', 'ParameterValue',      'technical replicate group', '3',                                       NULL,             'https://bioregistry.io/DPBO:1000184', NULL,                                      NULL,                                     '#FP_technical_replicate_group'),
  ('#PV_software',        'schema.org/PropertyValue', 'ParameterValue',      'software',                  'ProteomIQon',                             NULL,             'https://bioregistry.io/IAO_0000010',  NULL,                                      NULL,                                     '#FP_software'),
  ('#PV_organism',        'schema.org/PropertyValue', 'CharacteristicValue', 'organism',                  'Arabidopsis thaliana',                    NULL,             'https://bioregistry.io/SIO:010000',   'https://bioregistry.io/NCBITaxon:3702', NULL,                                     NULL),
  ('#PV_temp_25',         'schema.org/PropertyValue', 'FactorValue',         'temperature',               '25',                                      'degree Celsius', 'https://bioregistry.io/NCRO:0000029', NULL,                                      'https://bioregistry.io/UO:0000027',      NULL),
  ('#PV_temp_30',         'schema.org/PropertyValue', 'FactorValue',         'temperature',               '30',                                      'degree Celsius', 'https://bioregistry.io/NCRO:0000029', NULL,                                      'https://bioregistry.io/UO:0000027',      NULL),
  ('#PV_growth_env',      'schema.org/PropertyValue', 'Component',           'growth environment',        'bioreactor',                              NULL,             'https://bioregistry.io/OBI:0000997',  'https://bioregistry.io/OBI:0001046',    NULL,                                     NULL),
  ('#PV_mass_spec',       'schema.org/PropertyValue', 'Component',           'mass spectrometer',         'Q Exactive 9000',                         NULL,             'https://bioregistry.io/OBI:0000049',  NULL,                                      NULL,                                     NULL),
  ('#PV_var_measured',    'schema.org/PropertyValue', NULL,                  'variableMeasured',          'proteomics',                              NULL,             'https://schema.org/variableMeasured', 'https://bioregistry.io/MS:1003348',     NULL,                                     NULL);

INSERT INTO LabProtocolAdditionalProperty (lab_protocol_id, property_value_id) VALUES
  ('#Protocol_Growth', '#PV_growth_env'),
  ('#Protocol_MS_Run', '#PV_mass_spec');

-- ============================================================
-- Materials
-- ============================================================

INSERT INTO Material (id, type, additional_type, name) VALUES
  ('#Mat_BaseCulture',        'Material', 'Source', 'Base Culture'),
  ('#Mat_CultivationFlaskRT', 'Material', 'Sample', 'Cultivation Flask RT'),
  ('#Mat_CultivationFlaskHT', 'Material', 'Sample', 'Cultivation Flask HT'),
  ('#Mat_EppiRT1',            'Material', 'Sample', 'Eppi RT 1'),
  ('#Mat_EppiRT2',            'Material', 'Sample', 'Eppi RT 2'),
  ('#Mat_EppiRT3',            'Material', 'Sample', 'Eppi RT 3'),
  ('#Mat_EppiHT1',            'Material', 'Sample', 'Eppi HT 1'),
  ('#Mat_EppiHT2',            'Material', 'Sample', 'Eppi HT 2'),
  ('#Mat_EppiHT3',            'Material', 'Sample', 'Eppi HT 3');

INSERT INTO MaterialAdditionalProperty (material_id, property_value_id) VALUES
  ('#Mat_BaseCulture',        '#PV_organism'),
  ('#Mat_CultivationFlaskRT', '#PV_temp_25'),
  ('#Mat_CultivationFlaskHT', '#PV_temp_30');

-- ============================================================
-- Data
-- ============================================================

INSERT INTO Data (id, type, path, encoding_format) VALUES
  ('#Data_sample1_raw', 'File', 'sample1.raw', NULL),
  ('#Data_sample2_raw', 'File', 'sample2.raw', NULL),
  ('#Data_sample3_raw', 'File', 'sample3.raw', NULL),
  ('#Data_sample4_raw', 'File', 'sample4.raw', NULL),
  ('#Data_sample5_raw', 'File', 'sample5.raw', NULL),
  ('#Data_sample6_raw', 'File', 'sample6.raw', NULL);

INSERT INTO Data (id, type, path, selector, selector_format, encoding_format) VALUES
  ('#Data_result_col12', 'File', 'proteomics_result.csv', 'col=12', 'https://datatracker.ietf.org/doc/html/rfc7111', 'text/csv'),
  ('#Data_result_col13', 'File', 'proteomics_result.csv', 'col=13', 'https://datatracker.ietf.org/doc/html/rfc7111', 'text/csv'),
  ('#Data_result_col14', 'File', 'proteomics_result.csv', 'col=14', 'https://datatracker.ietf.org/doc/html/rfc7111', 'text/csv'),
  ('#Data_result_col15', 'File', 'proteomics_result.csv', 'col=15', 'https://datatracker.ietf.org/doc/html/rfc7111', 'text/csv'),
  ('#Data_result_col16', 'File', 'proteomics_result.csv', 'col=16', 'https://datatracker.ietf.org/doc/html/rfc7111', 'text/csv'),
  ('#Data_result_col17', 'File', 'proteomics_result.csv', 'col=17', 'https://datatracker.ietf.org/doc/html/rfc7111', 'text/csv');

-- ============================================================
-- Processes And Their Inputs/Outputs
-- ============================================================

INSERT INTO LabProcess (id, type, name, executes_protocol_id) VALUES
  ('#Proc_Growth_RT', 'bioschemas.org/LabProcess', 'Growth', '#Protocol_Growth'),
  ('#Proc_Growth_HT', 'bioschemas.org/LabProcess', 'Growth', '#Protocol_Growth'),
  ('#Proc_Lysis_RT1', 'bioschemas.org/LabProcess', 'Cell Lysis', '#Protocol_Cell_Lysis'),
  ('#Proc_Lysis_RT2', 'bioschemas.org/LabProcess', 'Cell Lysis', '#Protocol_Cell_Lysis'),
  ('#Proc_Lysis_RT3', 'bioschemas.org/LabProcess', 'Cell Lysis', '#Protocol_Cell_Lysis'),
  ('#Proc_Lysis_HT1', 'bioschemas.org/LabProcess', 'Cell Lysis', '#Protocol_Cell_Lysis'),
  ('#Proc_Lysis_HT2', 'bioschemas.org/LabProcess', 'Cell Lysis', '#Protocol_Cell_Lysis'),
  ('#Proc_Lysis_HT3', 'bioschemas.org/LabProcess', 'Cell Lysis', '#Protocol_Cell_Lysis'),
  ('#Proc_MS_RT1', 'bioschemas.org/LabProcess', 'MS Run', '#Protocol_MS_Run'),
  ('#Proc_MS_RT2', 'bioschemas.org/LabProcess', 'MS Run', '#Protocol_MS_Run'),
  ('#Proc_MS_RT3', 'bioschemas.org/LabProcess', 'MS Run', '#Protocol_MS_Run'),
  ('#Proc_MS_HT1', 'bioschemas.org/LabProcess', 'MS Run', '#Protocol_MS_Run'),
  ('#Proc_MS_HT2', 'bioschemas.org/LabProcess', 'MS Run', '#Protocol_MS_Run'),
  ('#Proc_MS_HT3', 'bioschemas.org/LabProcess', 'MS Run', '#Protocol_MS_Run'),
  ('#Proc_Comp_1', 'bioschemas.org/LabProcess', 'Computational Proteome Analysis', '#Protocol_Computational_Proteome_Analysis'),
  ('#Proc_Comp_2', 'bioschemas.org/LabProcess', 'Computational Proteome Analysis', '#Protocol_Computational_Proteome_Analysis'),
  ('#Proc_Comp_3', 'bioschemas.org/LabProcess', 'Computational Proteome Analysis', '#Protocol_Computational_Proteome_Analysis'),
  ('#Proc_Comp_4', 'bioschemas.org/LabProcess', 'Computational Proteome Analysis', '#Protocol_Computational_Proteome_Analysis'),
  ('#Proc_Comp_5', 'bioschemas.org/LabProcess', 'Computational Proteome Analysis', '#Protocol_Computational_Proteome_Analysis'),
  ('#Proc_Comp_6', 'bioschemas.org/LabProcess', 'Computational Proteome Analysis', '#Protocol_Computational_Proteome_Analysis');

INSERT INTO LabProcessInputMaterial (lab_process_id, material_id, pair_index) VALUES
  ('#Proc_Growth_RT', '#Mat_BaseCulture', 0),
  ('#Proc_Growth_HT', '#Mat_BaseCulture', 0),
  ('#Proc_Lysis_RT1', '#Mat_CultivationFlaskRT', 0),
  ('#Proc_Lysis_RT2', '#Mat_CultivationFlaskRT', 0),
  ('#Proc_Lysis_RT3', '#Mat_CultivationFlaskRT', 0),
  ('#Proc_Lysis_HT1', '#Mat_CultivationFlaskHT', 0),
  ('#Proc_Lysis_HT2', '#Mat_CultivationFlaskHT', 0),
  ('#Proc_Lysis_HT3', '#Mat_CultivationFlaskHT', 0),
  ('#Proc_MS_RT1', '#Mat_EppiRT1', 0),
  ('#Proc_MS_RT2', '#Mat_EppiRT2', 0),
  ('#Proc_MS_RT3', '#Mat_EppiRT3', 0),
  ('#Proc_MS_HT1', '#Mat_EppiHT1', 0),
  ('#Proc_MS_HT2', '#Mat_EppiHT2', 0),
  ('#Proc_MS_HT3', '#Mat_EppiHT3', 0);

INSERT INTO LabProcessInputData (lab_process_id, data_id, pair_index) VALUES
  ('#Proc_Comp_1', '#Data_sample1_raw', 0),
  ('#Proc_Comp_2', '#Data_sample2_raw', 0),
  ('#Proc_Comp_3', '#Data_sample3_raw', 0),
  ('#Proc_Comp_4', '#Data_sample4_raw', 0),
  ('#Proc_Comp_5', '#Data_sample5_raw', 0),
  ('#Proc_Comp_6', '#Data_sample6_raw', 0);

INSERT INTO LabProcessOutputMaterial (lab_process_id, material_id, pair_index) VALUES
  ('#Proc_Growth_RT', '#Mat_CultivationFlaskRT', 0),
  ('#Proc_Growth_HT', '#Mat_CultivationFlaskHT', 0),
  ('#Proc_Lysis_RT1', '#Mat_EppiRT1', 0),
  ('#Proc_Lysis_RT2', '#Mat_EppiRT2', 0),
  ('#Proc_Lysis_RT3', '#Mat_EppiRT3', 0),
  ('#Proc_Lysis_HT1', '#Mat_EppiHT1', 0),
  ('#Proc_Lysis_HT2', '#Mat_EppiHT2', 0),
  ('#Proc_Lysis_HT3', '#Mat_EppiHT3', 0);

INSERT INTO LabProcessOutputData (lab_process_id, data_id, pair_index) VALUES
  ('#Proc_MS_RT1', '#Data_sample1_raw', 0),
  ('#Proc_MS_RT2', '#Data_sample2_raw', 0),
  ('#Proc_MS_RT3', '#Data_sample3_raw', 0),
  ('#Proc_MS_HT1', '#Data_sample4_raw', 0),
  ('#Proc_MS_HT2', '#Data_sample5_raw', 0),
  ('#Proc_MS_HT3', '#Data_sample6_raw', 0),
  ('#Proc_Comp_1', '#Data_result_col12', 0),
  ('#Proc_Comp_2', '#Data_result_col13', 0),
  ('#Proc_Comp_3', '#Data_result_col14', 0),
  ('#Proc_Comp_4', '#Data_result_col15', 0),
  ('#Proc_Comp_5', '#Data_result_col16', 0),
  ('#Proc_Comp_6', '#Data_result_col17', 0);

INSERT INTO LabProcessParameterValue (lab_process_id, property_value_id) VALUES
  ('#Proc_Lysis_RT1', '#PV_time_10min'), ('#Proc_Lysis_RT1', '#PV_sonicator'), ('#Proc_Lysis_RT1', '#PV_replicate_1'),
  ('#Proc_Lysis_RT2', '#PV_time_10min'), ('#Proc_Lysis_RT2', '#PV_sonicator'), ('#Proc_Lysis_RT2', '#PV_replicate_2'),
  ('#Proc_Lysis_RT3', '#PV_time_10min'), ('#Proc_Lysis_RT3', '#PV_sonicator'), ('#Proc_Lysis_RT3', '#PV_replicate_3'),
  ('#Proc_Lysis_HT1', '#PV_time_10min'), ('#Proc_Lysis_HT1', '#PV_sonicator'), ('#Proc_Lysis_HT1', '#PV_replicate_1'),
  ('#Proc_Lysis_HT2', '#PV_time_10min'), ('#Proc_Lysis_HT2', '#PV_sonicator'), ('#Proc_Lysis_HT2', '#PV_replicate_2'),
  ('#Proc_Lysis_HT3', '#PV_time_10min'), ('#Proc_Lysis_HT3', '#PV_sonicator'), ('#Proc_Lysis_HT3', '#PV_replicate_3'),
  ('#Proc_Comp_1', '#PV_software'),
  ('#Proc_Comp_2', '#PV_software'),
  ('#Proc_Comp_3', '#PV_software'),
  ('#Proc_Comp_4', '#PV_software'),
  ('#Proc_Comp_5', '#PV_software'),
  ('#Proc_Comp_6', '#PV_software');

-- ============================================================
-- Dataset
-- ============================================================

INSERT INTO Dataset (id, type, additional_type, identifier, name) VALUES
  ('#Dataset_measurement1', 'schema.org/Dataset', 'Assay', 'measurement1', 'Proteomics Assay');

INSERT INTO DatasetAdditionalProperty (dataset_id, property_value_id) VALUES
  ('#Dataset_measurement1', '#PV_var_measured');

INSERT INTO DatasetHasPartData (dataset_id, data_id) VALUES
  ('#Dataset_measurement1', '#Data_sample1_raw'),
  ('#Dataset_measurement1', '#Data_sample2_raw'),
  ('#Dataset_measurement1', '#Data_sample3_raw'),
  ('#Dataset_measurement1', '#Data_sample4_raw'),
  ('#Dataset_measurement1', '#Data_sample5_raw'),
  ('#Dataset_measurement1', '#Data_sample6_raw'),
  ('#Dataset_measurement1', '#Data_result_col12'),
  ('#Dataset_measurement1', '#Data_result_col13'),
  ('#Dataset_measurement1', '#Data_result_col14'),
  ('#Dataset_measurement1', '#Data_result_col15'),
  ('#Dataset_measurement1', '#Data_result_col16'),
  ('#Dataset_measurement1', '#Data_result_col17');

INSERT INTO DatasetProcess (dataset_id, lab_process_id) VALUES
  ('#Dataset_measurement1', '#Proc_Growth_RT'),
  ('#Dataset_measurement1', '#Proc_Growth_HT'),
  ('#Dataset_measurement1', '#Proc_Lysis_RT1'),
  ('#Dataset_measurement1', '#Proc_Lysis_RT2'),
  ('#Dataset_measurement1', '#Proc_Lysis_RT3'),
  ('#Dataset_measurement1', '#Proc_Lysis_HT1'),
  ('#Dataset_measurement1', '#Proc_Lysis_HT2'),
  ('#Dataset_measurement1', '#Proc_Lysis_HT3'),
  ('#Dataset_measurement1', '#Proc_MS_RT1'),
  ('#Dataset_measurement1', '#Proc_MS_RT2'),
  ('#Dataset_measurement1', '#Proc_MS_RT3'),
  ('#Dataset_measurement1', '#Proc_MS_HT1'),
  ('#Dataset_measurement1', '#Proc_MS_HT2'),
  ('#Dataset_measurement1', '#Proc_MS_HT3'),
  ('#Dataset_measurement1', '#Proc_Comp_1'),
  ('#Dataset_measurement1', '#Proc_Comp_2'),
  ('#Dataset_measurement1', '#Proc_Comp_3'),
  ('#Dataset_measurement1', '#Proc_Comp_4'),
  ('#Dataset_measurement1', '#Proc_Comp_5'),
  ('#Dataset_measurement1', '#Proc_Comp_6');
