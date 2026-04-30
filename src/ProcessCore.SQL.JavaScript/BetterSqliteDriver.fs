namespace ProcessCore.SQL.JavaScript

open System
open ProcessCore.SQL

#if FABLE_COMPILER_JAVASCRIPT
open Fable.Core
open Fable.Core.JsInterop

type private BetterSqliteStatement =
    abstract run : obj -> obj
    abstract all : obj -> obj[]
    abstract get : obj -> obj

type private BetterSqliteDatabase =
    abstract prepare : string -> BetterSqliteStatement
    abstract pragma : string -> obj
    abstract close : unit -> unit

[<ImportDefault("better-sqlite3")>]
let private betterSqliteDatabaseConstructor: obj = jsNative

let private normalizeParameterName (name: string) =
    if String.IsNullOrWhiteSpace name then
        invalidArg "name" "SQLite parameter names must not be empty."

    match name[0] with
    | '$'
    | '@'
    | ':' -> name.Substring 1
    | _ -> name

let private sqlValueToJs value =
    match value with
    | SqlValue.Null -> null
    | SqlValue.Text text -> text :> obj
    | SqlValue.Int number -> number :> obj

let private parametersToJsObject parameters =
    parameters
    |> List.map (fun (name, value) -> normalizeParameterName name ==> sqlValueToJs value)
    |> createObj

let inline private jsIsNullOrUndefined (value: obj) : bool =
    emitJsExpr value "($0 == null)"

let inline private jsTypeOf (value: obj) : string =
    emitJsExpr value "typeof $0"

let inline private jsToNumber (value: obj) : float =
    emitJsExpr value "Number($0)"

let inline private objectKeys (value: obj) : string[] =
    emitJsExpr value "Object.keys($0)"

let inline private propertyValue (value: obj) (key: string) : obj =
    emitJsExpr (value, key) "$0[$1]"

let private jsValueToSqlValue (value: obj) =
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

let private jsRowToSqlRow (row: obj) =
    objectKeys row
    |> Array.map (fun key -> key, propertyValue row key |> jsValueToSqlValue)
    |> Map.ofArray

type BetterSqliteDriver internal (database: BetterSqliteDatabase, ownsDatabase: bool) =

    interface ISqliteDriver with

        member _.Execute sql parameters =
            database.prepare(sql).run(parametersToJsObject parameters) |> ignore

        member _.Query sql parameters =
            database.prepare(sql).all(parametersToJsObject parameters)
            |> Array.map jsRowToSqlRow
            |> Array.toList

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

    let private enableForeignKeys (driver: BetterSqliteDriver) =
        (driver :> ISqliteDriver).Execute "PRAGMA foreign_keys = ON;" []
        driver

    let openDatabase path =
        createNew betterSqliteDatabaseConstructor path
        |> unbox<BetterSqliteDatabase>
        |> fun database -> new BetterSqliteDriver(database, true)
        |> enableForeignKeys

    let wrapDatabase database =
        new BetterSqliteDriver(unbox<BetterSqliteDatabase> database, false)
        |> enableForeignKeys

    let openFile path =
        openDatabase path

    let openInMemory () =
        openDatabase ":memory:"

#else

type BetterSqliteDriver() =

    let unavailable () =
        invalidOp "ProcessCore.SQL.JavaScript must be compiled with Fable for JavaScript and run with the better-sqlite3 npm package."

    interface ISqliteDriver with

        member _.Execute _ _ = unavailable ()

        member _.Query _ _ = unavailable ()

        member _.Scalar _ _ = unavailable ()

    interface IDisposable with

        member _.Dispose() = ()

[<RequireQualifiedAccess>]
module BetterSqlite =

    let openDatabase (_path: string) =
        invalidOp "ProcessCore.SQL.JavaScript must be compiled with Fable for JavaScript and run with the better-sqlite3 npm package."

    let wrapDatabase (_database: obj) =
        invalidOp "ProcessCore.SQL.JavaScript must be compiled with Fable for JavaScript and run with the better-sqlite3 npm package."

    let openFile path =
        openDatabase path

    let openInMemory () =
        openDatabase ":memory:"

#endif
