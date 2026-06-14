namespace ProcessCore.SQL.JavaScript

open System
open Fable.Core
open ProcessCore.SQL

#if FABLE_COMPILER_JAVASCRIPT || FABLE_COMPILER_TYPESCRIPT || FABLE_COMPILER_TYPESCRIPT
open Fable.Core.JsInterop

/// <summary>Erased binding for a prepared statement of the <c>better-sqlite3</c> npm package.</summary>
type internal BetterSqliteStatement =
    abstract run : obj -> obj
    abstract all : obj -> obj[]
    abstract get : obj -> obj

/// <summary>Erased binding for a database handle of the <c>better-sqlite3</c> npm package.</summary>
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

/// <summary>
/// Node.js-side <see cref="ISqliteDriver"/> implementation backed by the
/// <a href="https://github.com/WiseLibs/better-sqlite3">better-sqlite3</a> npm package.
/// </summary>
/// <remarks>
/// <para>
/// Construction is private; obtain instances through the named-parameter factory members
/// (<c>create</c>, <c>createInMemory</c>, <c>wrapDatabase</c>) or the <see cref="BetterSqlite"/>
/// module. Each factory enables the <c>foreign_keys</c> pragma — better-sqlite3, like the C
/// library, leaves it off by default.
/// </para>
/// <para>
/// The driver tracks ownership of the underlying database handle. Handles opened by a factory are
/// owned and closed when the driver is disposed; handles passed to <c>wrapDatabase</c> are borrowed.
/// </para>
/// <para>
/// This file is dual-targeted: when compiled for the .NET runtime (no <c>FABLE_COMPILER_JAVASCRIPT</c>
/// symbol), the type degrades to a stub that raises on every call so consumers can still reference
/// the project from a .NET test harness without being able to use it.
/// </para>
/// </remarks>
[<AttachMembers>]
type BetterSqliteDriver internal (database: BetterSqliteDatabase, ownsDatabase: bool) =

    static member private enableForeignKeys (driver: BetterSqliteDriver) =
        (driver :> ISqliteDriver).Execute "PRAGMA foreign_keys = ON;" [||]
        driver

    /// <summary>
    /// Opens a new database handle by invoking the default-export <c>better-sqlite3</c>
    /// constructor with the given path. The resulting driver owns the handle.
    /// </summary>
    /// <param name="Path">A file-system path or <c>:memory:</c>.</param>
    [<NamedParams>]
    static member create (Path: string) =
        createNew betterSqliteDatabaseConstructor Path
        |> unbox<BetterSqliteDatabase>
        |> fun database -> new BetterSqliteDriver(database, true)
        |> BetterSqliteDriver.enableForeignKeys

    /// <summary>Opens a driver backed by an in-memory database.</summary>
    [<NamedParams>]
    static member createInMemory () =
        BetterSqliteDriver.create ":memory:"

    /// <summary>
    /// Wraps an existing <c>better-sqlite3</c> database handle. The driver does not own the
    /// handle; disposing the driver does <em>not</em> close it.
    /// </summary>
    /// <param name="Database">A <c>better-sqlite3</c> <c>Database</c> instance, passed as <c>obj</c> to keep the binding loose.</param>
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

/// <summary>
/// Convenience helpers that mirror the named-parameter factory members of
/// <see cref="BetterSqliteDriver"/> but expose plain F# functions.
/// </summary>
[<RequireQualifiedAccess>]
module BetterSqlite =

    /// <summary>Opens a driver against the given path. Use <c>:memory:</c> for an in-memory database.</summary>
    let openDatabase path =
        BetterSqliteDriver.create path

    /// <summary>Wraps an existing <c>better-sqlite3</c> database handle. The driver does not own the handle.</summary>
    let wrapDatabase database =
        BetterSqliteDriver.wrapDatabase database

    /// <summary>Alias for <c>openDatabase</c> with a file path. The driver owns the handle.</summary>
    let openFile path =
        openDatabase path

    /// <summary>Opens a driver backed by an in-memory database.</summary>
    let openInMemory () =
        openDatabase ":memory:"

#else

/// <summary>
/// .NET-only stub used when the project is compiled outside Fable. Every method raises
/// <see cref="System.InvalidOperationException"/>; the type exists only so that .NET test
/// harnesses can still link against the project.
/// </summary>
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

    /// <summary>Always raises — the driver is unavailable on .NET.</summary>
    [<NamedParams>]
    static member create (_Path: string) : BetterSqliteDriver =
        invalidOp "ProcessCore.SQL.JavaScript must be compiled with Fable for JavaScript and run with the better-sqlite3 npm package."

    /// <summary>Always raises — the driver is unavailable on .NET.</summary>
    [<NamedParams>]
    static member createInMemory () : BetterSqliteDriver =
        invalidOp "ProcessCore.SQL.JavaScript must be compiled with Fable for JavaScript and run with the better-sqlite3 npm package."

    /// <summary>Always raises — the driver is unavailable on .NET.</summary>
    [<NamedParams>]
    static member wrapDatabase (_Database: obj) : BetterSqliteDriver =
        invalidOp "ProcessCore.SQL.JavaScript must be compiled with Fable for JavaScript and run with the better-sqlite3 npm package."

/// <summary>.NET-only stub mirroring the Fable-side <c>BetterSqlite</c> module. Every helper raises.</summary>
[<RequireQualifiedAccess>]
module BetterSqlite =

    /// <summary>Always raises — the driver is unavailable on .NET.</summary>
    let openDatabase (_path: string) =
        BetterSqliteDriver.create _path

    /// <summary>Always raises — the driver is unavailable on .NET.</summary>
    let wrapDatabase (_database: obj) =
        BetterSqliteDriver.wrapDatabase _database

    /// <summary>Always raises — the driver is unavailable on .NET.</summary>
    let openFile path =
        openDatabase path

    /// <summary>Always raises — the driver is unavailable on .NET.</summary>
    let openInMemory () =
        BetterSqliteDriver.createInMemory ()

#endif
