module ProcessCore.SQL.Tests.DotNetDriverTests

open System.IO
open Fable.Pyxpecto
open ProcessCore.SQL
open ProcessCore.SQL.DotNet

let private repoRoot =
    let rec findRoot directory =
        let schema = Path.Combine(directory, "schemas", "sql", "001_core.sql")

        if File.Exists schema then
            directory
        else
            let parent = Directory.GetParent directory

            if isNull parent then
                failwith "Could not find repository root containing schemas/sql/001_core.sql."
            else
                findRoot parent.FullName

    findRoot (Directory.GetCurrentDirectory())

let private readFixture relativePath =
    File.ReadAllText(Path.Combine(repoRoot, relativePath))

let private createSeededDriver () =
    let driver = Sqlite.openInMemory ()
    let sql = driver :> ISqliteDriver
    sql.Execute (readFixture "schemas/sql/001_core.sql") []
    sql.Execute (readFixture "schemas/sql/seed_example.sql") []
    driver

let tests =
    testList
        ".NET SQLite driver"
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

            testCase "binds named parameters and maps scalar values" (fun _ ->
                use driver = Sqlite.openInMemory ()
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

            testCase "reads seeded rows through shared codecs" (fun _ ->
                use driver = createSeededDriver ()
                let sql = driver :> ISqliteDriver

                let dataset =
                    sql.Query "SELECT id, type, additional_type, identifier, name, description FROM dataset WHERE id = $id;" [ "id", SqlValue.Text "dataset:proteomics-assay" ]
                    |> List.exactlyOne
                    |> RowCodecs.Dataset.ofRow

                Expect.equal dataset.Identifier "assay-proteomics-001" "Seeded dataset identifier should be readable."
                Expect.equal dataset.AdditionalType (Some "Assay") "Nullable text columns should map to options.")
        ]
