module ProcessCore.SQL.Tests.RowCodecTests

open Fable.Pyxpecto
open ProcessCore.SQL

let private parameter name parameters =
    parameters
    |> List.tryFind (fun (parameterName, _) -> parameterName = name)
    |> Option.map snd

let tests =
    testList
        "row codecs"
        [
            testCase "roundtrips dataset rows through SQL parameters" (fun _ ->
                let row: DatasetRow =
                    {
                        Id = "dataset-1"
                        Type = "Dataset"
                        AdditionalType = Some "Study"
                        Identifier = "DS-001"
                        Name = Some "Seed dataset"
                        Description = None
                    }

                let parameters = RowCodecs.Dataset.toParameters row

                Expect.equal (parameter "id" parameters) (Some(SqlValue.Text "dataset-1")) "id should be serialized."
                Expect.equal (parameter "description" parameters) (Some SqlValue.Null) "None should serialize as SQL NULL."
                Expect.equal (RowCodecs.Dataset.ofRow (Map.ofList parameters)) row "Dataset codec should roundtrip.")

            testCase "roundtrips process IO rows with direction literals" (fun _ ->
                let row: ProcessIoRow =
                    {
                        ProcessId = "process-1"
                        Direction = ProcessIoDirection.Input
                        Position = 0
                        MaterialId = Some "material-1"
                        DataId = None
                    }

                let parameters = RowCodecs.ProcessIo.toParameters row

                Expect.equal (parameter "direction" parameters) (Some(SqlValue.Text "input")) "Direction should use the SQL literal."
                Expect.equal (parameter "data_id" parameters) (Some SqlValue.Null) "Missing data target should serialize as NULL."
                Expect.equal (RowCodecs.ProcessIo.ofRow (Map.ofList parameters)) row "Process IO codec should roundtrip.")

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

                let actual = RowCodecs.DefinedTerm.ofRow row

                Expect.equal actual.Tan None "tan should parse from SQL NULL."
                Expect.equal actual.InDefinedTermSetId (Some "ontology-1") "text values should parse as Some.")
        ]
