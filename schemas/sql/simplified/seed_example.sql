-- Seed data for the simplified schema
-- One dataset, edge-native Process rows, and generic PropertyValue rows

-- ============================================================
-- Dataset
-- ============================================================

INSERT INTO Dataset (id, name, description) VALUES
  ('#Dataset_measurement1', 'Proteomics Assay', 'Simplified graph-first proteomics assay');

-- ============================================================
-- Materials
-- ============================================================

INSERT INTO Material (id, dataset_id, name, kind) VALUES
  ('#Mat_BaseCulture',        '#Dataset_measurement1', 'Base Culture',         'Source'),
  ('#Mat_CultivationFlaskRT', '#Dataset_measurement1', 'Cultivation Flask RT', 'Sample'),
  ('#Mat_CultivationFlaskHT', '#Dataset_measurement1', 'Cultivation Flask HT', 'Sample'),
  ('#Mat_EppiRT1',            '#Dataset_measurement1', 'Eppi RT 1',            'Sample'),
  ('#Mat_EppiRT2',            '#Dataset_measurement1', 'Eppi RT 2',            'Sample'),
  ('#Mat_EppiRT3',            '#Dataset_measurement1', 'Eppi RT 3',            'Sample'),
  ('#Mat_EppiHT1',            '#Dataset_measurement1', 'Eppi HT 1',            'Sample'),
  ('#Mat_EppiHT2',            '#Dataset_measurement1', 'Eppi HT 2',            'Sample'),
  ('#Mat_EppiHT3',            '#Dataset_measurement1', 'Eppi HT 3',            'Sample');

-- ============================================================
-- Data
-- ============================================================

INSERT INTO Data (id, dataset_id, path, encoding_format) VALUES
  ('#Data_sample1_raw', '#Dataset_measurement1', 'sample1.raw', NULL),
  ('#Data_sample2_raw', '#Dataset_measurement1', 'sample2.raw', NULL),
  ('#Data_sample3_raw', '#Dataset_measurement1', 'sample3.raw', NULL),
  ('#Data_sample4_raw', '#Dataset_measurement1', 'sample4.raw', NULL),
  ('#Data_sample5_raw', '#Dataset_measurement1', 'sample5.raw', NULL),
  ('#Data_sample6_raw', '#Dataset_measurement1', 'sample6.raw', NULL);

INSERT INTO Data (id, dataset_id, path, selector, encoding_format) VALUES
  ('#Data_result_col12', '#Dataset_measurement1', 'proteomics_result.csv', 'col=12', 'text/csv'),
  ('#Data_result_col13', '#Dataset_measurement1', 'proteomics_result.csv', 'col=13', 'text/csv'),
  ('#Data_result_col14', '#Dataset_measurement1', 'proteomics_result.csv', 'col=14', 'text/csv'),
  ('#Data_result_col15', '#Dataset_measurement1', 'proteomics_result.csv', 'col=15', 'text/csv'),
  ('#Data_result_col16', '#Dataset_measurement1', 'proteomics_result.csv', 'col=16', 'text/csv'),
  ('#Data_result_col17', '#Dataset_measurement1', 'proteomics_result.csv', 'col=17', 'text/csv');

-- ============================================================
-- Processes
-- ============================================================

INSERT INTO Process (id, dataset_id, name, input_type, input_id, output_type, output_id) VALUES
  ('#Proc_Growth_RT', '#Dataset_measurement1', 'Growth', 'Material', '#Mat_BaseCulture',        'Material', '#Mat_CultivationFlaskRT'),
  ('#Proc_Growth_HT', '#Dataset_measurement1', 'Growth', 'Material', '#Mat_BaseCulture',        'Material', '#Mat_CultivationFlaskHT'),
  ('#Proc_Lysis_RT1', '#Dataset_measurement1', 'Cell Lysis', 'Material', '#Mat_CultivationFlaskRT', 'Material', '#Mat_EppiRT1'),
  ('#Proc_Lysis_RT2', '#Dataset_measurement1', 'Cell Lysis', 'Material', '#Mat_CultivationFlaskRT', 'Material', '#Mat_EppiRT2'),
  ('#Proc_Lysis_RT3', '#Dataset_measurement1', 'Cell Lysis', 'Material', '#Mat_CultivationFlaskRT', 'Material', '#Mat_EppiRT3'),
  ('#Proc_Lysis_HT1', '#Dataset_measurement1', 'Cell Lysis', 'Material', '#Mat_CultivationFlaskHT', 'Material', '#Mat_EppiHT1'),
  ('#Proc_Lysis_HT2', '#Dataset_measurement1', 'Cell Lysis', 'Material', '#Mat_CultivationFlaskHT', 'Material', '#Mat_EppiHT2'),
  ('#Proc_Lysis_HT3', '#Dataset_measurement1', 'Cell Lysis', 'Material', '#Mat_CultivationFlaskHT', 'Material', '#Mat_EppiHT3'),
  ('#Proc_MS_RT1',    '#Dataset_measurement1', 'MS Run', 'Material', '#Mat_EppiRT1', 'Data', '#Data_sample1_raw'),
  ('#Proc_MS_RT2',    '#Dataset_measurement1', 'MS Run', 'Material', '#Mat_EppiRT2', 'Data', '#Data_sample2_raw'),
  ('#Proc_MS_RT3',    '#Dataset_measurement1', 'MS Run', 'Material', '#Mat_EppiRT3', 'Data', '#Data_sample3_raw'),
  ('#Proc_MS_HT1',    '#Dataset_measurement1', 'MS Run', 'Material', '#Mat_EppiHT1', 'Data', '#Data_sample4_raw'),
  ('#Proc_MS_HT2',    '#Dataset_measurement1', 'MS Run', 'Material', '#Mat_EppiHT2', 'Data', '#Data_sample5_raw'),
  ('#Proc_MS_HT3',    '#Dataset_measurement1', 'MS Run', 'Material', '#Mat_EppiHT3', 'Data', '#Data_sample6_raw'),
  ('#Proc_Comp_1',    '#Dataset_measurement1', 'Computational Proteome Analysis', 'Data', '#Data_sample1_raw', 'Data', '#Data_result_col12'),
  ('#Proc_Comp_2',    '#Dataset_measurement1', 'Computational Proteome Analysis', 'Data', '#Data_sample2_raw', 'Data', '#Data_result_col13'),
  ('#Proc_Comp_3',    '#Dataset_measurement1', 'Computational Proteome Analysis', 'Data', '#Data_sample3_raw', 'Data', '#Data_result_col14'),
  ('#Proc_Comp_4',    '#Dataset_measurement1', 'Computational Proteome Analysis', 'Data', '#Data_sample4_raw', 'Data', '#Data_result_col15'),
  ('#Proc_Comp_5',    '#Dataset_measurement1', 'Computational Proteome Analysis', 'Data', '#Data_sample5_raw', 'Data', '#Data_result_col16'),
  ('#Proc_Comp_6',    '#Dataset_measurement1', 'Computational Proteome Analysis', 'Data', '#Data_sample6_raw', 'Data', '#Data_result_col17');

-- ============================================================
-- PropertyValues
-- ============================================================

-- Dataset-level properties
INSERT INTO PropertyValue (id, dataset_id, owner_type, owner_id, name, value, unit) VALUES
  ('#PV_dataset_variableMeasured', '#Dataset_measurement1', 'Dataset', '#Dataset_measurement1', 'variableMeasured', 'proteomics', NULL),
  ('#PV_dataset_instrumentPlatform', '#Dataset_measurement1', 'Dataset', '#Dataset_measurement1', 'measurementTechnique', 'mass spectrometry', NULL);

-- Process-level properties
INSERT INTO PropertyValue (id, dataset_id, owner_type, owner_id, name, value, unit) VALUES
  ('#PV_growth_rt_temp', '#Dataset_measurement1', 'Process', '#Proc_Growth_RT', 'temperature', '25', 'degree Celsius'),
  ('#PV_growth_ht_temp', '#Dataset_measurement1', 'Process', '#Proc_Growth_HT', 'temperature', '30', 'degree Celsius'),
  ('#PV_lysis_rt1_time', '#Dataset_measurement1', 'Process', '#Proc_Lysis_RT1', 'time', '10', 'minute'),
  ('#PV_lysis_rt2_time', '#Dataset_measurement1', 'Process', '#Proc_Lysis_RT2', 'time', '10', 'minute'),
  ('#PV_lysis_rt3_time', '#Dataset_measurement1', 'Process', '#Proc_Lysis_RT3', 'time', '10', 'minute'),
  ('#PV_lysis_ht1_time', '#Dataset_measurement1', 'Process', '#Proc_Lysis_HT1', 'time', '10', 'minute'),
  ('#PV_lysis_ht2_time', '#Dataset_measurement1', 'Process', '#Proc_Lysis_HT2', 'time', '10', 'minute'),
  ('#PV_lysis_ht3_time', '#Dataset_measurement1', 'Process', '#Proc_Lysis_HT3', 'time', '10', 'minute'),
  ('#PV_lysis_rt1_sonicator', '#Dataset_measurement1', 'Process', '#Proc_Lysis_RT1', 'sonicator', 'Fisherbrand Model 705 Sonic Dismembrator', NULL),
  ('#PV_lysis_rt2_sonicator', '#Dataset_measurement1', 'Process', '#Proc_Lysis_RT2', 'sonicator', 'Fisherbrand Model 705 Sonic Dismembrator', NULL),
  ('#PV_lysis_rt3_sonicator', '#Dataset_measurement1', 'Process', '#Proc_Lysis_RT3', 'sonicator', 'Fisherbrand Model 705 Sonic Dismembrator', NULL),
  ('#PV_lysis_ht1_sonicator', '#Dataset_measurement1', 'Process', '#Proc_Lysis_HT1', 'sonicator', 'Fisherbrand Model 705 Sonic Dismembrator', NULL),
  ('#PV_lysis_ht2_sonicator', '#Dataset_measurement1', 'Process', '#Proc_Lysis_HT2', 'sonicator', 'Fisherbrand Model 705 Sonic Dismembrator', NULL),
  ('#PV_lysis_ht3_sonicator', '#Dataset_measurement1', 'Process', '#Proc_Lysis_HT3', 'sonicator', 'Fisherbrand Model 705 Sonic Dismembrator', NULL),
  ('#PV_lysis_rt1_repl', '#Dataset_measurement1', 'Process', '#Proc_Lysis_RT1', 'technical replicate group', '1', NULL),
  ('#PV_lysis_rt2_repl', '#Dataset_measurement1', 'Process', '#Proc_Lysis_RT2', 'technical replicate group', '2', NULL),
  ('#PV_lysis_rt3_repl', '#Dataset_measurement1', 'Process', '#Proc_Lysis_RT3', 'technical replicate group', '3', NULL),
  ('#PV_lysis_ht1_repl', '#Dataset_measurement1', 'Process', '#Proc_Lysis_HT1', 'technical replicate group', '1', NULL),
  ('#PV_lysis_ht2_repl', '#Dataset_measurement1', 'Process', '#Proc_Lysis_HT2', 'technical replicate group', '2', NULL),
  ('#PV_lysis_ht3_repl', '#Dataset_measurement1', 'Process', '#Proc_Lysis_HT3', 'technical replicate group', '3', NULL),
  ('#PV_comp_1_software', '#Dataset_measurement1', 'Process', '#Proc_Comp_1', 'software', 'ProteomIQon', NULL),
  ('#PV_comp_2_software', '#Dataset_measurement1', 'Process', '#Proc_Comp_2', 'software', 'ProteomIQon', NULL),
  ('#PV_comp_3_software', '#Dataset_measurement1', 'Process', '#Proc_Comp_3', 'software', 'ProteomIQon', NULL),
  ('#PV_comp_4_software', '#Dataset_measurement1', 'Process', '#Proc_Comp_4', 'software', 'ProteomIQon', NULL),
  ('#PV_comp_5_software', '#Dataset_measurement1', 'Process', '#Proc_Comp_5', 'software', 'ProteomIQon', NULL),
  ('#PV_comp_6_software', '#Dataset_measurement1', 'Process', '#Proc_Comp_6', 'software', 'ProteomIQon', NULL);
