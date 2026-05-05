namespace ProcessCore.SQL.JavaScript

open System
open Fable.Core
open ProcessCore.SQL

#if FABLE_COMPILER_JAVASCRIPT
open Fable.Core.JsInterop

type internal BetterSqliteStatement =
    abstract run : obj -> obj
    abstract all : obj -> obj[]
    abstract get : obj -> obj

type internal BetterSqliteDatabase =
    abstract prepare : string -> BetterSqliteStatement
    abstract exec : string -> obj
    abstract pragma : string -> obj
    abstract close : unit -> unit

[<AutoOpen>]
module private BetterSqliteInterop =

    [<ImportDefault("better-sqlite3")>]
    let betterSqliteDatabaseConstructor: obj = jsNative

    let normalizeParameterName (name: string) =
        if String.IsNullOrWhiteSpace name then
            invalidArg "name" "SQLite parameter names must not be empty."

        match name[0] with
        | '$'
        | '@'
        | ':' -> name.Substring 1
        | _ -> name

    let sqlValueToJs value =
        match value with
        | SqlValue.Null -> null
        | SqlValue.Text text -> text :> obj
        | SqlValue.Int number -> number :> obj

    let parametersToJsObject (parameters: SqlParameters) =
        parameters
        |> Array.map (fun (parameter: SqlParameter) -> normalizeParameterName parameter.Name ==> sqlValueToJs parameter.Value)
        |> createObj

    let inline jsIsNullOrUndefined (value: obj) : bool =
        emitJsExpr value "($0 == null)"

    let inline jsTypeOf (value: obj) : string =
        emitJsExpr value "typeof $0"

    let inline jsToNumber (value: obj) : float =
        emitJsExpr value "Number($0)"

    let inline objectKeys (value: obj) : string[] =
        emitJsExpr value "Object.keys($0)"

    let inline propertyValue (value: obj) (key: string) : obj =
        emitJsExpr (value, key) "$0[$1]"

    let jsValueToSqlValue (value: obj) =
        if jsIsNullOrUndefined value then
            SqlValue.Null
        else
            match jsTypeOf value with
            | "string" -> SqlValue.Text(unbox<string> value)
            | "number"
            | "bigint" ->
                let number = jsToNumber value

                if number > float Int32.MaxValue || number < float Int32.MinValue then
                    invalidOp $"SQLite integer value '{number}' is outside the supported Int32 range."
                else
                    SqlValue.Int(int number)
            | _ -> SqlValue.Text(string value)

    let jsRowToSqlRow (row: obj) =
        objectKeys row
        |> Array.map (fun key -> key, propertyValue row key |> jsValueToSqlValue)
        |> Map.ofArray

[<AttachMembers>]
type BetterSqliteDriver internal (database: BetterSqliteDatabase, ownsDatabase: bool) =

    static member private enableForeignKeys (driver: BetterSqliteDriver) =
        (driver :> ISqliteDriver).Execute "PRAGMA foreign_keys = ON;" [||]
        driver

    [<NamedParams>]
    static member create (Path: string) =
        createNew betterSqliteDatabaseConstructor Path
        |> unbox<BetterSqliteDatabase>
        |> fun database -> new BetterSqliteDriver(database, true)
        |> BetterSqliteDriver.enableForeignKeys

    [<NamedParams>]
    static member createInMemory () =
        BetterSqliteDriver.create ":memory:"

    [<NamedParams>]
    static member wrapDatabase (Database: obj) =
        new BetterSqliteDriver(unbox<BetterSqliteDatabase> Database, false)
        |> BetterSqliteDriver.enableForeignKeys

    interface ISqliteDriver with

        member _.Execute sql parameters =
            match parameters with
            | [||] -> database.exec(sql) |> ignore
            | _ -> database.prepare(sql).run(parametersToJsObject parameters) |> ignore

        member _.Query sql parameters =
            database.prepare(sql).all(parametersToJsObject parameters)
            |> Array.map jsRowToSqlRow

        member _.Scalar sql parameters =
            let row = database.prepare(sql).get(parametersToJsObject parameters)

            if jsIsNullOrUndefined row then
                SqlValue.Null
            else
                let keys = objectKeys row

                if keys.Length = 0 then
                    SqlValue.Null
                else
                    propertyValue row keys[0] |> jsValueToSqlValue

    interface IDisposable with

        member _.Dispose() =
            if ownsDatabase then
                database.close()

[<RequireQualifiedAccess>]
module BetterSqlite =

    let openDatabase path =
        BetterSqliteDriver.create path

    let wrapDatabase database =
        BetterSqliteDriver.wrapDatabase database

    let openFile path =
        openDatabase path

    let openInMemory () =
        openDatabase ":memory:"

#else

[<AttachMembers>]
type BetterSqliteDriver() =

    let unavailable () =
        invalidOp "ProcessCore.SQL.JavaScript must be compiled with Fable for JavaScript and run with the better-sqlite3 npm package."

    interface ISqliteDriver with

        member _.Execute _ _ = unavailable ()

        member _.Query _ _ = unavailable ()

        member _.Scalar _ _ = unavailable ()

    interface IDisposable with

        member _.Dispose() = ()

    [<NamedParams>]
    static member create (_Path: string) : BetterSqliteDriver =
        invalidOp "ProcessCore.SQL.JavaScript must be compiled with Fable for JavaScript and run with the better-sqlite3 npm package."

    [<NamedParams>]
    static member createInMemory () : BetterSqliteDriver =
        invalidOp "ProcessCore.SQL.JavaScript must be compiled with Fable for JavaScript and run with the better-sqlite3 npm package."

    [<NamedParams>]
    static member wrapDatabase (_Database: obj) : BetterSqliteDriver =
        invalidOp "ProcessCore.SQL.JavaScript must be compiled with Fable for JavaScript and run with the better-sqlite3 npm package."

[<RequireQualifiedAccess>]
module BetterSqlite =

    let openDatabase (_path: string) =
        BetterSqliteDriver.create _path

    let wrapDatabase (_database: obj) =
        BetterSqliteDriver.wrapDatabase _database

    let openFile path =
        openDatabase path

    let openInMemory () =
        BetterSqliteDriver.createInMemory ()

#endif
