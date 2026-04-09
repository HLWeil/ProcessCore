-- Seed data based on examples/isa/assay_proteomics.yml
-- Populates core tables with the proteomics assay workflow:
--   Source -> Growth -> Sample -> Cell Lysis -> Sample -> MS Run -> File -> Computational Analysis -> File

-- ============================================================
-- Protocols
-- ============================================================

INSERT INTO Protocol (id, type, name) VALUES
  ('#Protocol_Growth',                         'LabProtocol', 'Growth'),
  ('#Protocol_Cell_Lysis',                     'LabProtocol', 'Cell Lysis'),
  ('#Protocol_MS_Run',                         'LabProtocol', 'MS Run'),
  ('#Protocol_Computational_Proteome_Analysis', 'LabProtocol', 'Computational Proteome Analysis');

-- ============================================================
-- PropertyValues
-- ============================================================

INSERT INTO PropertyValue (id, type, additional_type, name, value, property_id, unit_text, unit_code, value_reference) VALUES
  ('#PV_sonicator',       'PropertyValue', 'ParameterValue',     'sonicator',                'Fisherbrand Model 705 Sonic Dismembrator', 'https://bioregistry.io/OBI:0400114', NULL, NULL, 'https://bioregistry.io/OBI:5453453'),
  ('#PV_time_10min',      'PropertyValue', 'ParameterValue',     'time',                     '10',                                       'https://bioregistry.io/PATO:0000165', 'minute', 'https://bioregistry.io/UO:0000031', NULL),
  ('#PV_replicate_1',     'PropertyValue', 'ParameterValue',     'technical replicate group', '1',                                        'https://bioregistry.io/DPBO:1000184', NULL, NULL, NULL),
  ('#PV_replicate_2',     'PropertyValue', 'ParameterValue',     'technical replicate group', '2',                                        'https://bioregistry.io/DPBO:1000184', NULL, NULL, NULL),
  ('#PV_replicate_3',     'PropertyValue', 'ParameterValue',     'technical replicate group', '3',                                        'https://bioregistry.io/DPBO:1000184', NULL, NULL, NULL),
  ('#PV_software',        'PropertyValue', 'ParameterValue',     'software',                 'ProteomIQon',                              'https://bioregistry.io/IAO_0000010', NULL, NULL, NULL),
  ('#PV_organism',        'PropertyValue', 'CharacteristicValue','organism',                 'Arabidopsis thaliana',                     'https://bioregistry.io/SIO:010000', NULL, NULL, 'https://bioregistry.io/NCBITaxon:3702'),
  ('#PV_temp_25',         'PropertyValue', 'FactorValue',        'temperature',              '25',                                       'https://bioregistry.io/NCRO:0000029', 'degree Celsius', 'https://bioregistry.io/UO:0000027', NULL),
  ('#PV_temp_30',         'PropertyValue', 'FactorValue',        'temperature',              '30',                                       'https://bioregistry.io/NCRO:0000029', 'degree Celsius', 'https://bioregistry.io/UO:0000027', NULL),
  ('#PV_growth_env',      'PropertyValue', 'Component',          'growth environment',       'bioreactor',                               'https://bioregistry.io/OBI:0000997', NULL, NULL, 'https://bioregistry.io/OBI:0001046'),
  ('#PV_mass_spec',       'PropertyValue', 'Component',          'mass spectrometer',        'Q Exactive 9000',                         'https://bioregistry.io/OBI:0000049', NULL, NULL, NULL),
  ('#PV_var_measured',    'PropertyValue', NULL,                  'variableMeasured',         'proteomics',                               'https://schema.org/variableMeasured', NULL, NULL, 'https://bioregistry.io/MS:1003348');

-- ============================================================
-- Protocol Components
-- ============================================================

INSERT INTO ProtocolComponent (protocol_id, propertyvalue_id, role) VALUES
  ('#Protocol_Growth',   '#PV_growth_env', 'labEquipment'),
  ('#Protocol_MS_Run',   '#PV_mass_spec',  'labEquipment');

-- ============================================================
-- Materials (Sources and Samples)
-- ============================================================

INSERT INTO Material (id, type, name, additional_type) VALUES
  ('#Mat_BaseCulture',          'Material', 'Base Culture',          'Source'),
  ('#Mat_CultivationFlaskRT',   'Material', 'Cultivation Flask RT',  'Sample'),
  ('#Mat_CultivationFlaskHT',   'Material', 'Cultivation Flask HT',  'Sample'),
  ('#Mat_EppiRT1',              'Material', 'Eppi RT 1',             'Sample'),
  ('#Mat_EppiRT2',              'Material', 'Eppi RT 2',             'Sample'),
  ('#Mat_EppiRT3',              'Material', 'Eppi RT 3',             'Sample'),
  ('#Mat_EppiHT1',              'Material', 'Eppi HT 1',             'Sample'),
  ('#Mat_EppiHT2',              'Material', 'Eppi HT 2',             'Sample'),
  ('#Mat_EppiHT3',              'Material', 'Eppi HT 3',             'Sample');

-- Material characteristics & factors
INSERT INTO MaterialAdditionalProperty (material_id, propertyvalue_id) VALUES
  ('#Mat_BaseCulture',        '#PV_organism'),
  ('#Mat_CultivationFlaskRT', '#PV_temp_25'),
  ('#Mat_CultivationFlaskHT', '#PV_temp_30');

-- ============================================================
-- Data (raw files and derived results)
-- ============================================================

INSERT INTO Data (id, type, name, encoding_format) VALUES
  ('#Data_sample1_raw', 'File', 'sample1.raw', NULL),
  ('#Data_sample2_raw', 'File', 'sample2.raw', NULL),
  ('#Data_sample3_raw', 'File', 'sample3.raw', NULL),
  ('#Data_sample4_raw', 'File', 'sample4.raw', NULL),
  ('#Data_sample5_raw', 'File', 'sample5.raw', NULL),
  ('#Data_sample6_raw', 'File', 'sample6.raw', NULL),
  ('#Data_result_col12', 'File', 'proteomics_result.csv#col=12', 'text/csv'),
  ('#Data_result_col13', 'File', 'proteomics_result.csv#col=13', 'text/csv'),
  ('#Data_result_col14', 'File', 'proteomics_result.csv#col=14', 'text/csv'),
  ('#Data_result_col15', 'File', 'proteomics_result.csv#col=15', 'text/csv'),
  ('#Data_result_col16', 'File', 'proteomics_result.csv#col=16', 'text/csv'),
  ('#Data_result_col17', 'File', 'proteomics_result.csv#col=17', 'text/csv');

-- ============================================================
-- Processes
-- ============================================================

-- Growth (2 processes: RT and HT)
INSERT INTO Process (id, type, name, executes_protocol_id) VALUES
  ('#Proc_Growth_RT', 'LabProcess', 'Growth', '#Protocol_Growth'),
  ('#Proc_Growth_HT', 'LabProcess', 'Growth', '#Protocol_Growth');

INSERT INTO ProcessObjectMaterial (process_id, material_id) VALUES
  ('#Proc_Growth_RT', '#Mat_BaseCulture'),
  ('#Proc_Growth_HT', '#Mat_BaseCulture');

INSERT INTO ProcessResultMaterial (process_id, material_id) VALUES
  ('#Proc_Growth_RT', '#Mat_CultivationFlaskRT'),
  ('#Proc_Growth_HT', '#Mat_CultivationFlaskHT');

-- Cell Lysis (6 processes: 3 RT replicates + 3 HT replicates)
INSERT INTO Process (id, type, name, executes_protocol_id) VALUES
  ('#Proc_Lysis_RT1', 'LabProcess', 'Cell Lysis', '#Protocol_Cell_Lysis'),
  ('#Proc_Lysis_RT2', 'LabProcess', 'Cell Lysis', '#Protocol_Cell_Lysis'),
  ('#Proc_Lysis_RT3', 'LabProcess', 'Cell Lysis', '#Protocol_Cell_Lysis'),
  ('#Proc_Lysis_HT1', 'LabProcess', 'Cell Lysis', '#Protocol_Cell_Lysis'),
  ('#Proc_Lysis_HT2', 'LabProcess', 'Cell Lysis', '#Protocol_Cell_Lysis'),
  ('#Proc_Lysis_HT3', 'LabProcess', 'Cell Lysis', '#Protocol_Cell_Lysis');

INSERT INTO ProcessObjectMaterial (process_id, material_id) VALUES
  ('#Proc_Lysis_RT1', '#Mat_CultivationFlaskRT'),
  ('#Proc_Lysis_RT2', '#Mat_CultivationFlaskRT'),
  ('#Proc_Lysis_RT3', '#Mat_CultivationFlaskRT'),
  ('#Proc_Lysis_HT1', '#Mat_CultivationFlaskHT'),
  ('#Proc_Lysis_HT2', '#Mat_CultivationFlaskHT'),
  ('#Proc_Lysis_HT3', '#Mat_CultivationFlaskHT');

INSERT INTO ProcessResultMaterial (process_id, material_id) VALUES
  ('#Proc_Lysis_RT1', '#Mat_EppiRT1'),
  ('#Proc_Lysis_RT2', '#Mat_EppiRT2'),
  ('#Proc_Lysis_RT3', '#Mat_EppiRT3'),
  ('#Proc_Lysis_HT1', '#Mat_EppiHT1'),
  ('#Proc_Lysis_HT2', '#Mat_EppiHT2'),
  ('#Proc_Lysis_HT3', '#Mat_EppiHT3');

INSERT INTO ProcessParameterValue (process_id, propertyvalue_id) VALUES
  ('#Proc_Lysis_RT1', '#PV_time_10min'), ('#Proc_Lysis_RT1', '#PV_sonicator'), ('#Proc_Lysis_RT1', '#PV_replicate_1'),
  ('#Proc_Lysis_RT2', '#PV_time_10min'), ('#Proc_Lysis_RT2', '#PV_sonicator'), ('#Proc_Lysis_RT2', '#PV_replicate_2'),
  ('#Proc_Lysis_RT3', '#PV_time_10min'), ('#Proc_Lysis_RT3', '#PV_sonicator'), ('#Proc_Lysis_RT3', '#PV_replicate_3'),
  ('#Proc_Lysis_HT1', '#PV_time_10min'), ('#Proc_Lysis_HT1', '#PV_sonicator'), ('#Proc_Lysis_HT1', '#PV_replicate_1'),
  ('#Proc_Lysis_HT2', '#PV_time_10min'), ('#Proc_Lysis_HT2', '#PV_sonicator'), ('#Proc_Lysis_HT2', '#PV_replicate_2'),
  ('#Proc_Lysis_HT3', '#PV_time_10min'), ('#Proc_Lysis_HT3', '#PV_sonicator'), ('#Proc_Lysis_HT3', '#PV_replicate_3');

-- MS Run (6 processes: one per eppi sample -> raw file)
INSERT INTO Process (id, type, name, executes_protocol_id) VALUES
  ('#Proc_MS_RT1', 'LabProcess', 'MS Run', '#Protocol_MS_Run'),
  ('#Proc_MS_RT2', 'LabProcess', 'MS Run', '#Protocol_MS_Run'),
  ('#Proc_MS_RT3', 'LabProcess', 'MS Run', '#Protocol_MS_Run'),
  ('#Proc_MS_HT1', 'LabProcess', 'MS Run', '#Protocol_MS_Run'),
  ('#Proc_MS_HT2', 'LabProcess', 'MS Run', '#Protocol_MS_Run'),
  ('#Proc_MS_HT3', 'LabProcess', 'MS Run', '#Protocol_MS_Run');

INSERT INTO ProcessObjectMaterial (process_id, material_id) VALUES
  ('#Proc_MS_RT1', '#Mat_EppiRT1'),
  ('#Proc_MS_RT2', '#Mat_EppiRT2'),
  ('#Proc_MS_RT3', '#Mat_EppiRT3'),
  ('#Proc_MS_HT1', '#Mat_EppiHT1'),
  ('#Proc_MS_HT2', '#Mat_EppiHT2'),
  ('#Proc_MS_HT3', '#Mat_EppiHT3');

INSERT INTO ProcessResultData (process_id, data_id) VALUES
  ('#Proc_MS_RT1', '#Data_sample1_raw'),
  ('#Proc_MS_RT2', '#Data_sample2_raw'),
  ('#Proc_MS_RT3', '#Data_sample3_raw'),
  ('#Proc_MS_HT1', '#Data_sample4_raw'),
  ('#Proc_MS_HT2', '#Data_sample5_raw'),
  ('#Proc_MS_HT3', '#Data_sample6_raw');

-- Computational Proteome Analysis (6 processes: raw file -> result column)
INSERT INTO Process (id, type, name, executes_protocol_id) VALUES
  ('#Proc_Comp_1', 'LabProcess', 'Computational Proteome Analysis', '#Protocol_Computational_Proteome_Analysis'),
  ('#Proc_Comp_2', 'LabProcess', 'Computational Proteome Analysis', '#Protocol_Computational_Proteome_Analysis'),
  ('#Proc_Comp_3', 'LabProcess', 'Computational Proteome Analysis', '#Protocol_Computational_Proteome_Analysis'),
  ('#Proc_Comp_4', 'LabProcess', 'Computational Proteome Analysis', '#Protocol_Computational_Proteome_Analysis'),
  ('#Proc_Comp_5', 'LabProcess', 'Computational Proteome Analysis', '#Protocol_Computational_Proteome_Analysis'),
  ('#Proc_Comp_6', 'LabProcess', 'Computational Proteome Analysis', '#Protocol_Computational_Proteome_Analysis');

INSERT INTO ProcessObjectData (process_id, data_id) VALUES
  ('#Proc_Comp_1', '#Data_sample1_raw'),
  ('#Proc_Comp_2', '#Data_sample2_raw'),
  ('#Proc_Comp_3', '#Data_sample3_raw'),
  ('#Proc_Comp_4', '#Data_sample4_raw'),
  ('#Proc_Comp_5', '#Data_sample5_raw'),
  ('#Proc_Comp_6', '#Data_sample6_raw');

INSERT INTO ProcessResultData (process_id, data_id) VALUES
  ('#Proc_Comp_1', '#Data_result_col12'),
  ('#Proc_Comp_2', '#Data_result_col13'),
  ('#Proc_Comp_3', '#Data_result_col14'),
  ('#Proc_Comp_4', '#Data_result_col15'),
  ('#Proc_Comp_5', '#Data_result_col16'),
  ('#Proc_Comp_6', '#Data_result_col17');

INSERT INTO ProcessParameterValue (process_id, propertyvalue_id) VALUES
  ('#Proc_Comp_1', '#PV_software'),
  ('#Proc_Comp_2', '#PV_software'),
  ('#Proc_Comp_3', '#PV_software'),
  ('#Proc_Comp_4', '#PV_software'),
  ('#Proc_Comp_5', '#PV_software'),
  ('#Proc_Comp_6', '#PV_software');

-- ============================================================
-- Dataset (Assay container)
-- ============================================================

INSERT INTO Dataset (id, type, additional_type, identifier, name) VALUES
  ('#Dataset_measurement1', 'Dataset', 'Assay', 'measurement1', 'Proteomics Assay');

INSERT INTO DatasetAdditionalProperty (dataset_id, propertyvalue_id) VALUES
  ('#Dataset_measurement1', '#PV_var_measured');

-- Link all processes to the assay dataset
INSERT INTO DatasetAbout (dataset_id, process_id) VALUES
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
