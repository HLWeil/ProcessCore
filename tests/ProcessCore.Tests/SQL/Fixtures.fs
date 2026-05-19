module ProcessCore.SQL.Tests.Fixtures

open ProcessCore.SQL

#if FABLE_COMPILER_JAVASCRIPT
open Fable.Core
open ProcessCore.SQL.JavaScript

[<Import("readFileSync", "node:fs")>]
let private readFileSync (_path: string) (_encoding: string) : string = nativeOnly

let readFixture relativePath =
    readFileSync relativePath "utf8"

let createEmptyDriver () =
    let driver = BetterSqlite.openInMemory ()
    let sql = driver :> ISqliteDriver
    sql.Execute (readFixture "schemas/sql/001_core.sql") [||]
    driver :> ISqliteDriver

let createSeededDriver () =
    let sql = createEmptyDriver ()
    sql.Execute (readFixture "schemas/sql/seed_example.sql") [||]
    sql

#else
#if FABLE_COMPILER_PYTHON
open Fable.Core
open ProcessCore.SQL.Python

[<Emit("open($0, encoding='utf-8').read()")>]
let private readText (_path: string) : string = nativeOnly

let readFixture relativePath =
    readText relativePath

let createEmptyDriver () =
    let driver = PythonSqliteDriver.createInMemory ()
    let sql = driver :> ISqliteDriver
    sql.Execute (readFixture "schemas/sql/001_core.sql") [||]
    sql

let createSeededDriver () =
    let sql = createEmptyDriver ()
    sql.Execute (readFixture "schemas/sql/seed_example.sql") [||]
    sql

#else
open System.IO
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

let readFixture relativePath =
    File.ReadAllText(Path.Combine(repoRoot, relativePath))

let createEmptyDriver () =
    let driver = Sqlite.openInMemory ()
    let sql = driver :> ISqliteDriver
    sql.Execute (readFixture "schemas/sql/001_core.sql") [||]
    sql

let createSeededDriver () =
    let sql = createEmptyDriver ()
    sql.Execute (readFixture "schemas/sql/seed_example.sql") [||]
    sql
#endif
#endif
