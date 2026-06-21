module ProcessCore.SQL.Tests.RowCodecTests

open Fable.Pyxpecto
open ProcessCore.SQL

let private parameter name (parameters: SqlParameters) =
    parameters
    |> Array.tryFind (fun (parameter: SqlParameter) -> parameter.Name = name)
    |> Option.map (fun parameter -> parameter.Value)

let private parametersToRow (parameters: SqlParameters) =
    parameters
    |> Array.map (fun (parameter: SqlParameter) -> parameter.Name, parameter.Value)
    |> Map.ofArray

let tests =
    testList
        "row codecs"
        [
            testCase "roundtrips dataset rows through SQL parameters" (fun _ ->
                let row = DatasetRow("dataset-1", "Dataset", "DS-001", AdditionalType = "Study", Title = "Seed dataset")

                let parameters = row.ToParameters()
                let actual = DatasetRow.ofRow (parametersToRow parameters)

                Expect.equal (parameter "id" parameters) (Some(SqlValue.Text "dataset-1")) "id should be serialized."
                Expect.equal (parameter "description" parameters) (Some SqlValue.Null) "None should serialize as SQL NULL."
                Expect.equal actual.Id row.Id "Dataset id should roundtrip."
                Expect.equal actual.Type row.Type "Dataset type should roundtrip."
                Expect.equal actual.Identifier row.Identifier "Dataset identifier should roundtrip."
                Expect.equal actual.AdditionalType row.AdditionalType "Dataset additional_type should roundtrip."
                Expect.equal actual.Title row.Title "Dataset title should roundtrip."
                Expect.equal actual.Description row.Description "Dataset description should roundtrip.")

            testCase "roundtrips process IO rows with direction literals" (fun _ ->
                let row = ProcessIoRow("process-1", ProcessIoDirection.Input, 0, SampleId = "sample-1")

                let parameters = row.ToParameters()
                let actual = ProcessIoRow.ofRow (parametersToRow parameters)

                Expect.equal (parameter "direction" parameters) (Some(SqlValue.Text "input")) "Direction should use the SQL literal."
                Expect.equal (parameter "data_id" parameters) (Some SqlValue.Null) "Missing data target should serialize as NULL."
                Expect.equal actual.ProcessId row.ProcessId "Process id should roundtrip."
                Expect.equal actual.Direction row.Direction "Direction should roundtrip."
                Expect.equal actual.Position row.Position "Position should roundtrip."
                Expect.equal actual.SampleId row.SampleId "Sample target should roundtrip."
                Expect.equal actual.DataId row.DataId "Data target should roundtrip.")

            testCase "reads nullable text columns" (fun _ ->
                let row =
                    Map.ofList
                        [
                            "id", SqlValue.Text "term-1"
                            "type", SqlValue.Text "DefinedTerm"
                            "name", SqlValue.Text "temperature"
                            "tan", SqlValue.Null
                            "in_defined_term_set_id", SqlValue.Text "ontology-1"
                            "in_defined_term_set_name", SqlValue.Null
                        ]

                let actual = DefinedTermRow.ofRow row

                Expect.equal actual.Tan None "tan should parse from SQL NULL."
                Expect.equal actual.InDefinedTermSetId (Some "ontology-1") "text values should parse as Some.")
        ]
