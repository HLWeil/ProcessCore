module ProcessCore.SQL.Tests.TableModelTests

open Fable.Pyxpecto
open ProcessCore.SQL
open ProcessCore.SQL.Platform

let tests =
    testList
        "table model"
        [
            testCase "tracks 8 entity tables and 9 association tables" (fun _ ->
                Expect.equal Repository.entityTables.Length 8 "Entity table count should match the SQL profile."
                Expect.equal Repository.associationTables.Length 9 "Association table count should match the SQL profile.")

            testCase "keeps generated data.fragment_identity out of the public row model" (fun _ ->
                Expect.isFalse (List.contains "fragment_identity" Repository.data.Columns) "fragment_identity is a generated database detail.")

            testCase "maps process IO directions to SQL literals" (fun _ ->
                Expect.equal ProcessIoDirection.Input.Sql "input" "Input should map to the SQL literal."
                Expect.equal ProcessIoDirection.Output.Sql "output" "Output should map to the SQL literal."
                Expect.equal (ProcessIoDirection.ofSql "input") ProcessIoDirection.Input "input should parse."
                Expect.equal (ProcessIoDirection.ofSql "output") ProcessIoDirection.Output "output should parse.")

            testCase "documents all planned connector targets" (fun _ ->
                let runtimes = Driver.connectorPlans |> List.map _.Runtime
                Expect.isTrue (List.contains ".NET" runtimes) ".NET adapter should be planned."
                Expect.isTrue (List.contains "JavaScript/Node" runtimes) "JavaScript adapter should be planned."
                Expect.isTrue (List.contains "TypeScript/Node" runtimes) "TypeScript adapter should be planned."
                Expect.isTrue (List.contains "Python" runtimes) "Python adapter should be planned.")
        ]
