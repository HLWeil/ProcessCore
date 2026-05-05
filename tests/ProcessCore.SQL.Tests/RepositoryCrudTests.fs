module ProcessCore.SQL.Tests.RepositoryCrudTests

open Fable.Pyxpecto
open ProcessCore.SQL
open ProcessCore.SQL.Tests.Fixtures

let private insertGraph (sql: ISqliteDriver) =
    DefinedTerm.insert (sql, DefinedTermRow("term:temperature", "DefinedTerm", "temperature"))
    LabProtocol.insert (sql, LabProtocolRow("protocol:extraction", "LabProtocol", Name = "Extraction"))
    FormalParameter.insert (sql, FormalParameterRow("parameter:temperature", "FormalParameter", Name = "temperature"))
    Dataset.insert (sql, DatasetRow("dataset:assay", "Dataset", "assay-001", AdditionalType = "Assay"))
    Material.insert (sql, MaterialRow("material:source", "Material", "leaf"))
    Data.insert (sql, DataRow("data:raw", "Data", "raw.dat"))
    LabProcess.insert (sql, LabProcessRow("process:extraction", "LabProcess", "extract", ExecutesProtocolId = "protocol:extraction"))
    PropertyValue.insert (sql, PropertyValueRow("pv:temperature", "PropertyValue", "temperature", Value = "20", Unit = "C", InstanceOfId = "parameter:temperature"))
    DatasetHasPart.insert (sql, DatasetHasPartRow("dataset:assay", 0, PartDataId = "data:raw"))
    DatasetProcess.insert (sql, DatasetProcessRow("dataset:assay", 0, "process:extraction"))
    DatasetAdditionalProperty.insert (sql, DatasetAdditionalPropertyRow("dataset:assay", 0, "pv:temperature"))
    ProtocolParameter.insert (sql, ProtocolParameterRow("protocol:extraction", 0, "parameter:temperature"))
    ProcessIo.insert (sql, ProcessIoRow("process:extraction", ProcessIoDirection.Input, 0, MaterialId = "material:source"))
    ProcessIo.insert (sql, ProcessIoRow("process:extraction", ProcessIoDirection.Output, 0, DataId = "data:raw"))
    ProcessParameterValue.insert (sql, ProcessParameterValueRow("process:extraction", 0, "pv:temperature"))
    ProtocolAdditionalProperty.insert (sql, ProtocolAdditionalPropertyRow("protocol:extraction", 0, "pv:temperature"))
    MaterialAdditionalProperty.insert (sql, MaterialAdditionalPropertyRow("material:source", 0, "pv:temperature"))
    DataAdditionalProperty.insert (sql, DataAdditionalPropertyRow("data:raw", 0, "pv:temperature"))

let tests =
    testList
        "repository CRUD"
        [
            testCase "inserts and reads rows for every table" (fun _ ->
                let sql = createEmptyDriver ()

                insertGraph sql

                Expect.equal (DefinedTerm.get (sql, "term:temperature")).Value.Name "temperature" "DefinedTerm should read back."
                Expect.equal (LabProtocol.get (sql, "protocol:extraction")).Value.Name (Some "Extraction") "LabProtocol should read back."
                Expect.equal (FormalParameter.get (sql, "parameter:temperature")).Value.Name (Some "temperature") "FormalParameter should read back."
                Expect.equal (Dataset.get (sql, "dataset:assay")).Value.Identifier "assay-001" "Dataset should read back."
                Expect.equal (Material.get (sql, "material:source")).Value.Name "leaf" "Material should read back."
                Expect.equal (Data.get (sql, "data:raw")).Value.Path "raw.dat" "Data should read back."
                Expect.equal (LabProcess.get (sql, "process:extraction")).Value.ExecutesProtocolId (Some "protocol:extraction") "LabProcess should read back."
                Expect.equal (PropertyValue.get (sql, "pv:temperature")).Value.Value (Some "20") "PropertyValue should read back."
                Expect.equal (DatasetHasPart.get (sql, "dataset:assay", 0)).Value.PartDataId (Some "data:raw") "DatasetHasPart should read back."
                Expect.equal (DatasetProcess.get (sql, "dataset:assay", 0)).Value.ProcessId "process:extraction" "DatasetProcess should read back."
                Expect.equal (DatasetAdditionalProperty.get (sql, "dataset:assay", 0)).Value.PropertyValueId "pv:temperature" "DatasetAdditionalProperty should read back."
                Expect.equal (ProtocolParameter.get (sql, "protocol:extraction", 0)).Value.FormalParameterId "parameter:temperature" "ProtocolParameter should read back."
                Expect.equal (ProcessIo.get (sql, "process:extraction", ProcessIoDirection.Input, 0)).Value.MaterialId (Some "material:source") "ProcessIo input should read back."
                Expect.equal (ProcessParameterValue.get (sql, "process:extraction", 0)).Value.PropertyValueId "pv:temperature" "ProcessParameterValue should read back."
                Expect.equal (ProtocolAdditionalProperty.get (sql, "protocol:extraction", 0)).Value.PropertyValueId "pv:temperature" "ProtocolAdditionalProperty should read back."
                Expect.equal (MaterialAdditionalProperty.get (sql, "material:source", 0)).Value.PropertyValueId "pv:temperature" "MaterialAdditionalProperty should read back."
                Expect.equal (DataAdditionalProperty.get (sql, "data:raw", 0)).Value.PropertyValueId "pv:temperature" "DataAdditionalProperty should read back."
                Expect.equal (ProcessIo.list sql).Length 2 "ProcessIo list should include both input and output rows.")

            testCase "updates entity and association rows" (fun _ ->
                let sql = createEmptyDriver ()

                insertGraph sql
                Dataset.update (sql, DatasetRow("dataset:assay", "Dataset", "assay-002", AdditionalType = "Assay", Name = "Updated"))
                Data.insert (sql, DataRow("data:processed", "Data", "processed.dat"))
                DatasetHasPart.update (sql, DatasetHasPartRow("dataset:assay", 0, PartDataId = "data:processed"))

                Expect.equal (Dataset.get (sql, "dataset:assay")).Value.Identifier "assay-002" "Entity update should persist."
                Expect.equal (Dataset.get (sql, "dataset:assay")).Value.Name (Some "Updated") "Nullable entity update should persist."
                Expect.equal (DatasetHasPart.get (sql, "dataset:assay", 0)).Value.PartDataId (Some "data:processed") "Association update should persist.")

            testCase "deletes entity and association rows" (fun _ ->
                let sql = createEmptyDriver ()

                insertGraph sql
                Data.insert (sql, DataRow("data:extra", "Data", "extra.dat"))
                DatasetHasPart.insert (sql, DatasetHasPartRow("dataset:assay", 1, PartDataId = "data:extra"))

                DatasetHasPart.delete (sql, "dataset:assay", 1)
                Data.delete (sql, "data:extra")

                Expect.equal (DatasetHasPart.get (sql, "dataset:assay", 1)) None "Association delete should remove the row."
                Expect.equal (Data.get (sql, "data:extra")) None "Entity delete should remove the row.")

            testCase "lists process edges and property value orphans" (fun _ ->
                let sql = createEmptyDriver ()

                insertGraph sql
                PropertyValue.insert (sql, PropertyValueRow("pv:orphan", "PropertyValue", "orphan"))

                let edge = ProcessEdges.list sql |> Array.exactlyOne
                let orphan = PropertyValueOrphans.list sql |> Array.exactlyOne

                Expect.equal edge.ProcessId "process:extraction" "Process edge should expose the process id."
                Expect.equal edge.InputId "material:source" "Process edge should expose the input id."
                Expect.equal edge.OutputId "data:raw" "Process edge should expose the output id."
                Expect.equal orphan.Id "pv:orphan" "Orphan view should expose unowned property values.")
        ]
