module ProcessCore.SQL.Python.Tests.PythonDriverTests

open Fable.Pyxpecto
open ProcessCore.SQL
open ProcessCore.SQL.Python

#if FABLE_COMPILER_PYTHON
open Fable.Core

[<Emit("open($0, encoding='utf-8').read()")>]
let private readText (_path: string) : string = nativeOnly

let private readFixture relativePath =
    readText relativePath

let private createSeededDriver () =
    let driver = PythonSqliteDriver.createInMemory ()
    let sql = driver :> ISqliteDriver
    sql.Execute (readFixture "schemas/sql/001_core.sql") [||]
    sql.Execute (readFixture "schemas/sql/seed_example.sql") [||]
    driver

let tests =
    testList
        "Python SQLite driver"
        [
            testCase "executes schema and seed scripts with FK enforcement" (fun _ ->
                use driver = createSeededDriver ()
                let sql = driver :> ISqliteDriver

                let tableCount = sql.Scalar "SELECT count(*) FROM sqlite_master WHERE type = 'table';" [||]
                let fkViolations = sql.Query "PRAGMA foreign_key_check;" [||]
                let orphans = sql.Query "SELECT id FROM property_value_orphans;" [||]

                Expect.equal tableCount (SqlValue.Int 17) "Seeded schema should expose 17 tables."
                Expect.equal fkViolations.Length 0 "Seeded database should not have FK violations."
                Expect.equal orphans.Length 0 "Seeded database should not have orphan PropertyValues.")

            testCase "binds named parameters and maps row values" (fun _ ->
                use driver = PythonSqliteDriver.createInMemory ()
                let sql = driver :> ISqliteDriver

                let row =
                    sql.Query
                        "SELECT $name AS name, $position AS position, $missing AS missing;"
                        [|
                            SqlParameter("name", SqlValue.Text "sample")
                            SqlParameter("position", SqlValue.Int 2)
                            SqlParameter("missing", SqlValue.Null)
                        |]
                    |> Array.exactlyOne

                Expect.equal row["name"] (SqlValue.Text "sample") "Text parameters should roundtrip."
                Expect.equal row["position"] (SqlValue.Int 2) "Int parameters should roundtrip."
                Expect.equal row["missing"] SqlValue.Null "Null parameters should roundtrip.")

            testCase "reads seeded rows through shared codecs" (fun _ ->
                use driver = createSeededDriver ()
                let sql = driver :> ISqliteDriver

                let dataset =
                    sql.Query
                        "SELECT id, type, additional_type, identifier, name, description FROM dataset WHERE id = $id;"
                        [| SqlParameter("id", SqlValue.Text "dataset:proteomics-assay") |]
                    |> Array.exactlyOne
                    |> DatasetRow.ofRow

                Expect.equal dataset.Identifier "assay-proteomics-001" "Seeded dataset identifier should be readable."
                Expect.equal dataset.AdditionalType (Some "Assay") "Nullable text columns should map to options.")
        ]
#else
let tests =
    testList "Python SQLite driver" []
#endif
