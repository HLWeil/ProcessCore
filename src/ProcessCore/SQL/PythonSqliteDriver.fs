namespace ProcessCore.SQL.Python

open System
open Fable.Core
open ProcessCore.SQL

#if FABLE_COMPILER_PYTHON
open Fable.Core.PyInterop

/// <summary>Erased binding for a <c>sqlite3.Cursor</c> Python object.</summary>
[<AllowNullLiteral; Interface>]
type Cursor =
    abstract execute: sql: string * parameters: obj -> Cursor
    abstract fetchall: unit -> obj[]
    abstract fetchone: unit -> obj
    [<Emit("$0.description")>]
    abstract description: obj[]

/// <summary>Erased binding for a <c>sqlite3.Connection</c> Python object.</summary>
[<AllowNullLiteral; Interface>]
type Connection =
    abstract cursor: unit -> Cursor
    abstract commit: unit -> unit
    abstract close: unit -> unit
    abstract executescript: sql: string -> unit

[<AutoOpen>]
module private PythonSqliteInterop =

    [<Import("connect", "sqlite3")>]
    let connect (_path: string) : Connection = nativeOnly

    let normalizeParameterName (name: string) =
        if String.IsNullOrWhiteSpace name then
            invalidArg "name" "SQLite parameter names must not be empty."

        match name[0] with
        | '$'
        | '@'
        | ':' -> name.Substring 1
        | _ -> name

    [<Emit("int($0)")>]
    let intToPyInt (_value: int) : obj = nativeOnly

    let sqlValueToPy value =
        match value with
        | SqlValue.Null -> null
        | SqlValue.Text text -> text :> obj
        | SqlValue.Int number -> intToPyInt number

    let parametersToPyDict (parameters: SqlParameters) =
        parameters
        |> Array.map (fun (parameter: SqlParameter) -> normalizeParameterName parameter.Name ==> sqlValueToPy parameter.Value)
        |> createObj

    [<Emit("$0 is None")>]
    let pyIsNone (_value: obj) : bool = nativeOnly

    [<Emit("isinstance($0, str)")>]
    let pyIsString (_value: obj) : bool = nativeOnly

    [<Emit("isinstance($0, int)")>]
    let pyIsInt (_value: obj) : bool = nativeOnly

    [<Emit("int($0)")>]
    let pyToInt (_value: obj) : int = nativeOnly

    [<Emit("str($0)")>]
    let pyToString (_value: obj) : string = nativeOnly

    [<Emit("$0[$1]")>]
    let item (_value: obj) (_index: int) : obj = nativeOnly

    let pyValueToSqlValue value =
        if pyIsNone value then
            SqlValue.Null
        elif pyIsString value then
            SqlValue.Text(pyToString value)
        elif pyIsInt value then
            SqlValue.Int(pyToInt value)
        else
            SqlValue.Text(pyToString value)

    let columnName (descriptionItem: obj) =
        item descriptionItem 0 |> pyToString

    let pyRowToSqlRow (columns: string[]) (row: obj) =
        columns
        |> Array.mapi (fun index column -> column, item row index |> pyValueToSqlValue)
        |> Map.ofArray

/// <summary>
/// Python-side <see cref="ISqliteDriver"/> implementation backed by the Python standard-library
/// <c>sqlite3</c> module.
/// </summary>
/// <remarks>
/// <para>
/// Construction is private; obtain instances through the named-parameter factory members
/// (<c>create</c>, <c>createInMemory</c>, <c>wrapConnection</c>). Each factory enables the
/// <c>foreign_keys</c> pragma — Python's <c>sqlite3</c>, like the underlying C library, leaves
/// it off by default.
/// </para>
/// <para>
/// Parameter-less <c>Execute</c> calls are routed through <c>executescript</c> so that
/// multi-statement DDL works in a single round-trip; parameterised calls go through a fresh
/// cursor with <c>execute</c>. Both paths call <c>commit</c> after the operation, matching the
/// auto-commit semantics expected by the rest of the repository layer.
/// </para>
/// <para>
/// This file is dual-targeted: when compiled outside Fable Python, the type degrades to a stub
/// whose members raise <see cref="System.InvalidOperationException"/>.
/// </para>
/// </remarks>
[<AttachMembers>]
type PythonSqliteDriver internal (connection: Connection, ownsConnection: bool) =

    /// <summary>The underlying <c>sqlite3.Connection</c>, exposed for advanced scenarios such as transactions or backup.</summary>
    member _.Connection = connection

    static member private enableForeignKeys (driver: PythonSqliteDriver) =
        (driver :> ISqliteDriver).Execute "PRAGMA foreign_keys = ON;" [||]
        driver

    /// <summary>
    /// Opens a new connection via <c>sqlite3.connect</c>. The resulting driver owns the
    /// connection and will close it on dispose.
    /// </summary>
    /// <param name="Path">A file-system path or <c>:memory:</c>.</param>
    [<NamedParams>]
    static member create (Path: string) =
        new PythonSqliteDriver(connect Path, true)
        |> PythonSqliteDriver.enableForeignKeys

    /// <summary>Opens a driver backed by an in-memory database.</summary>
    [<NamedParams>]
    static member createInMemory () =
        PythonSqliteDriver.create ":memory:"

    /// <summary>
    /// Wraps an existing <c>sqlite3.Connection</c>. The driver does not own the connection;
    /// disposing the driver does <em>not</em> close it.
    /// </summary>
    /// <param name="Connection">The connection to wrap.</param>
    [<NamedParams>]
    static member wrapConnection (Connection: Connection) =
        new PythonSqliteDriver(Connection, false)
        |> PythonSqliteDriver.enableForeignKeys

    interface ISqliteDriver with

        member _.Execute sql parameters =
            if parameters.Length = 0 then
                connection.executescript sql
            else
                let cursor = connection.cursor ()
                cursor.execute (sql, parametersToPyDict parameters) |> ignore

            connection.commit ()

        member _.Query sql parameters =
            let cursor = connection.cursor ()
            cursor.execute (sql, parametersToPyDict parameters) |> ignore
            let columns = cursor.description |> Array.map columnName

            cursor.fetchall ()
            |> Array.map (pyRowToSqlRow columns)

        member this.Scalar sql parameters =
            let rows = (this :> ISqliteDriver).Query sql parameters

            if rows.Length = 0 then
                SqlValue.Null
            else
                let values = rows[0] |> Map.toArray

                if values.Length = 0 then
                    SqlValue.Null
                else
                    values[0] |> snd

    interface IDisposable with

        member _.Dispose() =
            if ownsConnection then
                connection.close ()

#else

module private PythonSqliteUnavailable =

    let unavailable () =
        invalidOp "ProcessCore.SQL.Python must be compiled with Fable for Python and run with the Python stdlib sqlite3 module."

/// <summary>
/// .NET-only stub used when the project is compiled outside Fable Python. Every method raises
/// <see cref="System.InvalidOperationException"/>; the type exists only so that .NET test
/// harnesses can still link against the project.
/// </summary>
[<AttachMembers>]
type PythonSqliteDriver() =

    interface ISqliteDriver with

        member _.Execute _ _ = PythonSqliteUnavailable.unavailable ()

        member _.Query _ _ = PythonSqliteUnavailable.unavailable ()

        member _.Scalar _ _ = PythonSqliteUnavailable.unavailable ()

    interface IDisposable with

        member _.Dispose() = ()

    /// <summary>Always raises — the driver is unavailable on .NET.</summary>
    [<NamedParams>]
    static member create (_Path: string) : PythonSqliteDriver =
        PythonSqliteUnavailable.unavailable ()

    /// <summary>Always raises — the driver is unavailable on .NET.</summary>
    [<NamedParams>]
    static member createInMemory () : PythonSqliteDriver =
        PythonSqliteUnavailable.unavailable ()

    /// <summary>Always raises — the driver is unavailable on .NET.</summary>
    [<NamedParams>]
    static member wrapConnection (_Connection: obj) : PythonSqliteDriver =
        PythonSqliteUnavailable.unavailable ()

#endif
