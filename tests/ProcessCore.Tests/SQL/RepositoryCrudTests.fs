module ProcessCore.SQL.Tests.RepositoryCrudTests

open Fable.Pyxpecto
open ProcessCore.SQL
open ProcessCore.SQL.Tests.Fixtures

let private insertGraph (sql: ISqliteDriver) =
    DefinedTerm.insert (sql, DefinedTermRow("term:temperature", "DefinedTerm", "temperature"))
    Plan.insert (sql, PlanRow("protocol:extraction", "Plan", Name = "Extraction"))
    FormalParameter.insert (sql, FormalParameterRow("parameter:temperature", "FormalParameter", Name = "temperature"))
    Dataset.insert (sql, DatasetRow("dataset:assay", "Dataset", "assay-001", AdditionalType = "Assay"))
    Sample.insert (sql, SampleRow("sample:source", "Sample", "leaf"))
    Data.insert (sql, DataRow("data:raw", "Data", "raw.dat"))
    Process.insert (sql, ProcessRow("process:extraction", "Process", "extract", ExecutesProtocolId = "protocol:extraction"))
    Annotation.insert (sql, AnnotationRow("pv:temperature", "Annotation", "temperature", Value = "20", Unit = "C", InstanceOfId = "parameter:temperature"))
    DatasetHasPart.insert (sql, DatasetHasPartRow("dataset:assay", 0, PartDataId = "data:raw"))
    DatasetProcess.insert (sql, DatasetProcessRow("dataset:assay", 0, "process:extraction"))
    DatasetAdditionalProperty.insert (sql, DatasetAdditionalPropertyRow("dataset:assay", 0, "pv:temperature"))
    ProtocolParameter.insert (sql, ProtocolParameterRow("protocol:extraction", 0, "parameter:temperature"))
    ProcessIo.insert (sql, ProcessIoRow("process:extraction", ProcessIoDirection.Input, 0, SampleId = "sample:source"))
    ProcessIo.insert (sql, ProcessIoRow("process:extraction", ProcessIoDirection.Output, 0, DataId = "data:raw"))
    ProcessParameterValue.insert (sql, ProcessParameterValueRow("process:extraction", 0, "pv:temperature"))
    ProtocolAdditionalProperty.insert (sql, ProtocolAdditionalPropertyRow("protocol:extraction", 0, "pv:temperature"))
    SampleAdditionalProperty.insert (sql, SampleAdditionalPropertyRow("sample:source", 0, "pv:temperature"))
    DataAdditionalProperty.insert (sql, DataAdditionalPropertyRow("data:raw", 0, "pv:temperature"))

let tests =
    testList
        "repository CRUD"
        [
            testCase "inserts and reads rows for every table" (fun _ ->
                let sql = createEmptyDriver ()

                insertGraph sql

                Expect.equal (DefinedTerm.get (sql, "term:temperature")).Value.Name "temperature" "DefinedTerm should read back."
                Expect.equal (Plan.get (sql, "protocol:extraction")).Value.Name (Some "Extraction") "Plan should read back."
                Expect.equal (FormalParameter.get (sql, "parameter:temperature")).Value.Name (Some "temperature") "FormalParameter should read back."
                Expect.equal (Dataset.get (sql, "dataset:assay")).Value.Identifier "assay-001" "Dataset should read back."
                Expect.equal (Sample.get (sql, "sample:source")).Value.Name "leaf" "Sample should read back."
                Expect.equal (Data.get (sql, "data:raw")).Value.Path "raw.dat" "Data should read back."
                Expect.equal (Process.get (sql, "process:extraction")).Value.ExecutesProtocolId (Some "protocol:extraction") "Process should read back."
                Expect.equal (Annotation.get (sql, "pv:temperature")).Value.Value (Some "20") "Annotation should read back."
                Expect.equal (DatasetHasPart.get (sql, "dataset:assay", 0)).Value.PartDataId (Some "data:raw") "DatasetHasPart should read back."
                Expect.equal (DatasetProcess.get (sql, "dataset:assay", 0)).Value.ProcessId "process:extraction" "DatasetProcess should read back."
                Expect.equal (DatasetAdditionalProperty.get (sql, "dataset:assay", 0)).Value.AnnotationId "pv:temperature" "DatasetAdditionalProperty should read back."
                Expect.equal (ProtocolParameter.get (sql, "protocol:extraction", 0)).Value.FormalParameterId "parameter:temperature" "ProtocolParameter should read back."
                Expect.equal (ProcessIo.get (sql, "process:extraction", ProcessIoDirection.Input, 0)).Value.SampleId (Some "sample:source") "ProcessIo input should read back."
                Expect.equal (ProcessParameterValue.get (sql, "process:extraction", 0)).Value.AnnotationId "pv:temperature" "ProcessParameterValue should read back."
                Expect.equal (ProtocolAdditionalProperty.get (sql, "protocol:extraction", 0)).Value.AnnotationId "pv:temperature" "ProtocolAdditionalProperty should read back."
                Expect.equal (SampleAdditionalProperty.get (sql, "sample:source", 0)).Value.AnnotationId "pv:temperature" "SampleAdditionalProperty should read back."
                Expect.equal (DataAdditionalProperty.get (sql, "data:raw", 0)).Value.AnnotationId "pv:temperature" "DataAdditionalProperty should read back."
                Expect.equal (ProcessIo.list sql).Length 2 "ProcessIo list should include both input and output rows.")

            testCase "updates entity and association rows" (fun _ ->
                let sql = createEmptyDriver ()

                insertGraph sql
                Dataset.update (sql, DatasetRow("dataset:assay", "Dataset", "assay-002", AdditionalType = "Assay", Title = "Updated"))
                Data.insert (sql, DataRow("data:processed", "Data", "processed.dat"))
                DatasetHasPart.update (sql, DatasetHasPartRow("dataset:assay", 0, PartDataId = "data:processed"))

                Expect.equal (Dataset.get (sql, "dataset:assay")).Value.Identifier "assay-002" "Entity update should persist."
                Expect.equal (Dataset.get (sql, "dataset:assay")).Value.Title (Some "Updated") "Nullable entity update should persist."
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
                Annotation.insert (sql, AnnotationRow("pv:orphan", "Annotation", "orphan"))

                let edge = ProcessEdges.list sql |> Array.exactlyOne
                let orphan = AnnotationOrphans.list sql |> Array.exactlyOne

                Expect.equal edge.ProcessId "process:extraction" "Process edge should expose the process id."
                Expect.equal edge.InputId "sample:source" "Process edge should expose the input id."
                Expect.equal edge.OutputId "data:raw" "Process edge should expose the output id."
                Expect.equal orphan.Id "pv:orphan" "Orphan view should expose unowned property values.")
        ]
