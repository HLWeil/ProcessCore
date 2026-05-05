namespace ProcessCore.SQL.Python

open System
open Fable.Core
open ProcessCore.SQL

#if FABLE_COMPILER_PYTHON
open Fable.Core.PyInterop

[<AllowNullLiteral; Interface>]
type Cursor =
    abstract execute: sql: string * parameters: obj -> Cursor
    abstract fetchall: unit -> obj[]
    abstract fetchone: unit -> obj
    [<Emit("$0.description")>]
    abstract description: obj[]

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

[<AttachMembers>]
type PythonSqliteDriver internal (connection: Connection, ownsConnection: bool) =

    member _.Connection = connection

    static member private enableForeignKeys (driver: PythonSqliteDriver) =
        (driver :> ISqliteDriver).Execute "PRAGMA foreign_keys = ON;" [||]
        driver

    [<NamedParams>]
    static member create (Path: string) =
        new PythonSqliteDriver(connect Path, true)
        |> PythonSqliteDriver.enableForeignKeys

    [<NamedParams>]
    static member createInMemory () =
        PythonSqliteDriver.create ":memory:"

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

[<AttachMembers>]
type PythonSqliteDriver() =

    interface ISqliteDriver with

        member _.Execute _ _ = PythonSqliteUnavailable.unavailable ()

        member _.Query _ _ = PythonSqliteUnavailable.unavailable ()

        member _.Scalar _ _ = PythonSqliteUnavailable.unavailable ()

    interface IDisposable with

        member _.Dispose() = ()

    [<NamedParams>]
    static member create (_Path: string) : PythonSqliteDriver =
        PythonSqliteUnavailable.unavailable ()

    [<NamedParams>]
    static member createInMemory () : PythonSqliteDriver =
        PythonSqliteUnavailable.unavailable ()

    [<NamedParams>]
    static member wrapConnection (_Connection: obj) : PythonSqliteDriver =
        PythonSqliteUnavailable.unavailable ()

#endif
