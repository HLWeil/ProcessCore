module ProcessCore.SQL.Tests.TableModelTests

open Fable.Pyxpecto
open ProcessCore.SQL
open ProcessCore.SQL.Platform

let tests =
    testList
        "table model"
        [
            testCase "tracks 8 entity tables and 9 association tables" (fun _ ->
                Expect.equal (Repository.EntityTables()).Length 8 "Entity table count should match the SQL profile."
                Expect.equal (Repository.AssociationTables()).Length 9 "Association table count should match the SQL profile.")

            testCase "keeps generated data.fragment_identity out of the public row model" (fun _ ->
                Expect.isFalse (Array.contains "fragment_identity" Repository.Data.Columns) "fragment_identity is a generated database detail.")

            testCase "maps process IO directions to SQL literals" (fun _ ->
                let directionParameter (row: ProcessIoRow) =
                    row.ToParameters()
                    |> Array.find (fun (parameter: SqlParameter) -> parameter.Name = "direction")
                    |> fun parameter -> parameter.Value

                let input =
                    Map.ofList
                        [
                            "process_id", SqlValue.Text "process-1"
                            "direction", SqlValue.Text "input"
                            "position", SqlValue.Int 0
                            "sample_id", SqlValue.Null
                            "data_id", SqlValue.Text "data-1"
                        ]
                    |> ProcessIoRow.ofRow

                let output =
                    Map.ofList
                        [
                            "process_id", SqlValue.Text "process-1"
                            "direction", SqlValue.Text "output"
                            "position", SqlValue.Int 1
                            "sample_id", SqlValue.Null
                            "data_id", SqlValue.Text "data-2"
                        ]
                    |> ProcessIoRow.ofRow

                Expect.equal (directionParameter input) (SqlValue.Text "input") "Input should map to the SQL literal."
                Expect.equal (directionParameter output) (SqlValue.Text "output") "Output should map to the SQL literal."
                Expect.equal input.Direction ProcessIoDirection.Input "input should parse."
                Expect.equal output.Direction ProcessIoDirection.Output "output should parse.")

            testCase "documents all planned connector targets" (fun _ ->
                let runtimes = Driver.connectorPlans |> Array.map (fun plan -> plan.Runtime)
                Expect.isTrue (Array.contains ".NET" runtimes) ".NET adapter should be planned."
                Expect.isTrue (Array.contains "JavaScript/Node" runtimes) "JavaScript adapter should be planned."
                Expect.isTrue (Array.contains "TypeScript/Node" runtimes) "TypeScript adapter should be planned."
                Expect.isTrue (Array.contains "Python" runtimes) "Python adapter should be planned.")
        ]
