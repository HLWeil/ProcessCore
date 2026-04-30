module ProcessCore.SQL.JavaScript.Tests.JavaScriptDriverTests

open Fable.Pyxpecto
open ProcessCore.SQL
open ProcessCore.SQL.JavaScript

#if FABLE_COMPILER_JAVASCRIPT
open Fable.Core

[<Import("readFileSync", "node:fs")>]
let private readFileSync (_path: string) (_encoding: string) : string = jsNative

let private readFixture relativePath =
    readFileSync relativePath "utf8"

let private createSeededDriver () =
    let driver = BetterSqlite.openInMemory ()
    let sql = driver :> ISqliteDriver
    sql.Execute (readFixture "schemas/sql/001_core.sql") []
    sql.Execute (readFixture "schemas/sql/seed_example.sql") []
    driver

let tests =
    testList
        "JavaScript SQLite driver"
        [
            testCase "executes schema and seed scripts with FK enforcement" (fun _ ->
                use driver = createSeededDriver ()
                let sql = driver :> ISqliteDriver

                let tableCount = sql.Scalar "SELECT count(*) FROM sqlite_master WHERE type = 'table';" []
                let fkViolations = sql.Query "PRAGMA foreign_key_check;" []
                let orphans = sql.Query "SELECT id FROM property_value_orphans;" []

                Expect.equal tableCount (SqlValue.Int 17) "Seeded schema should expose 17 tables."
                Expect.equal fkViolations.Length 0 "Seeded database should not have FK violations."
                Expect.equal orphans.Length 0 "Seeded database should not have orphan PropertyValues.")

            testCase "binds named parameters and maps row values" (fun _ ->
                use driver = BetterSqlite.openInMemory ()
                let sql = driver :> ISqliteDriver

                let row =
                    sql.Query
                        "SELECT $name AS name, $position AS position, $missing AS missing;"
                        [
                            "name", SqlValue.Text "sample"
                            "position", SqlValue.Int 2
                            "missing", SqlValue.Null
                        ]
                    |> List.exactlyOne

                Expect.equal row["name"] (SqlValue.Text "sample") "Text parameters should roundtrip."
                Expect.equal row["position"] (SqlValue.Int 2) "Int parameters should roundtrip."
                Expect.equal row["missing"] SqlValue.Null "Null parameters should roundtrip.")
        ]
#else
let tests =
    testList "JavaScript SQLite driver" []
#endif
