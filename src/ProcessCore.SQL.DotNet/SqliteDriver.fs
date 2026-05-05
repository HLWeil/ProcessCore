namespace ProcessCore.SQL.DotNet

open System
open System.Data
open Fable.Core
open Microsoft.Data.Sqlite
open ProcessCore.SQL

/// <summary>
/// .NET-side <see cref="ISqliteDriver"/> implementation backed by <c>Microsoft.Data.Sqlite</c>.
/// </summary>
/// <remarks>
/// <para>
/// Construction is private; obtain instances through the named-parameter factory members
/// (<c>create</c>, <c>createFromFile</c>, <c>createInMemory</c>, <c>wrapConnection</c>) or via the
/// convenience <see cref="Sqlite"/> module. The factories all open the underlying
/// <see cref="SqliteConnection"/> if it is not already open and enable the
/// <c>foreign_keys</c> pragma — SQLite does not enforce foreign keys by default.
/// </para>
/// <para>
/// The driver tracks whether it owns the connection. Connections opened by a factory are owned
/// and disposed when the driver is disposed; connections passed to <c>wrapConnection</c> are
/// borrowed and outlive the driver.
/// </para>
/// </remarks>
[<AttachMembers>]
type SqliteDriver internal (connection: SqliteConnection, ownsConnection: bool) =

    let normalizeParameterName (name: string) =
        if String.IsNullOrWhiteSpace name then
            invalidArg "name" "SQLite parameter names must not be empty."

        match name[0] with
        | '$'
        | '@'
        | ':' -> name
        | _ -> "$" + name

    let parameterValue value =
        match value with
        | SqlValue.Null -> DBNull.Value :> obj
        | SqlValue.Text text -> text :> obj
        | SqlValue.Int number -> number :> obj

    let addParameters (command: SqliteCommand) (parameters: SqlParameters) =
        parameters
        |> Array.iter (fun (sqlParameter: SqlParameter) ->
            let parameter = command.CreateParameter()
            parameter.ParameterName <- normalizeParameterName sqlParameter.Name
            parameter.Value <- parameterValue sqlParameter.Value
            command.Parameters.Add(parameter) |> ignore)

    let sqliteValueToSqlValue (value: obj) =
        match value with
        | null -> SqlValue.Null
        | :? DBNull -> SqlValue.Null
        | :? string as text -> SqlValue.Text text
        | :? int as number -> SqlValue.Int number
        | :? int64 as number ->
            if number > int64 Int32.MaxValue || number < int64 Int32.MinValue then
                invalidOp $"SQLite integer value '{number}' is outside the supported Int32 range."
            else
                SqlValue.Int(int number)
        | :? int16 as number -> SqlValue.Int(int number)
        | :? byte as number -> SqlValue.Int(int number)
        | other -> SqlValue.Text(string other)

    let createCommand sql parameters =
        let command = connection.CreateCommand()
        command.CommandText <- sql
        addParameters command parameters
        command

    /// <summary>The underlying <see cref="SqliteConnection"/>, exposed for advanced scenarios such as transactions or backup.</summary>
    member _.Connection = connection

    static member private enableForeignKeys (driver: SqliteDriver) =
        (driver :> ISqliteDriver).Execute "PRAGMA foreign_keys = ON;" [||]
        driver

    /// <summary>
    /// Creates a driver from a raw ADO.NET connection string and opens the connection. The
    /// resulting driver owns the connection.
    /// </summary>
    /// <param name="ConnectionString">An ADO.NET connection string for <c>Microsoft.Data.Sqlite</c>.</param>
    [<NamedParams>]
    static member create (ConnectionString: string) =
        let connection = new SqliteConnection(ConnectionString)
        connection.Open()

        new SqliteDriver(connection, true)
        |> SqliteDriver.enableForeignKeys

    /// <summary>
    /// Creates a driver pointing at a database file. Equivalent to <c>create</c> with a
    /// connection string of <c>Data Source={Path}</c>.
    /// </summary>
    /// <param name="Path">File-system path to the SQLite database file.</param>
    [<NamedParams>]
    static member createFromFile (Path: string) =
        let builder = SqliteConnectionStringBuilder()
        builder.DataSource <- Path
        SqliteDriver.create(builder.ToString())

    /// <summary>Creates a driver backed by an in-memory database (<c>Data Source=:memory:</c>).</summary>
    [<NamedParams>]
    static member createInMemory () =
        SqliteDriver.create "Data Source=:memory:"

    /// <summary>
    /// Wraps an existing <see cref="SqliteConnection"/>. Opens the connection if it is not already
    /// open. The resulting driver does <em>not</em> own the connection — disposing the driver will
    /// not close it.
    /// </summary>
    /// <param name="Connection">The connection to wrap.</param>
    [<NamedParams>]
    static member wrapConnection (Connection: SqliteConnection) =
        if Connection.State <> ConnectionState.Open then
            Connection.Open()

        new SqliteDriver(Connection, false)
        |> SqliteDriver.enableForeignKeys

    interface ISqliteDriver with

        member _.Execute sql parameters =
            use command = createCommand sql parameters
            command.ExecuteNonQuery() |> ignore

        member _.Query sql parameters =
            use command = createCommand sql parameters
            use reader = command.ExecuteReader()

            [|
                while reader.Read() do
                    [|
                        for index in 0 .. reader.FieldCount - 1 do
                            reader.GetName index, sqliteValueToSqlValue(reader.GetValue index)
                    |]
                    |> Map.ofArray
            |]

        member _.Scalar sql parameters =
            use command = createCommand sql parameters
            command.ExecuteScalar() |> sqliteValueToSqlValue

    interface IDisposable with

        member _.Dispose() =
            if ownsConnection then
                connection.Dispose()

/// <summary>
/// Convenience helpers that mirror the named-parameter factory members of <see cref="SqliteDriver"/>
/// but expose plain F# functions rather than static members. Prefer these from F# call sites.
/// </summary>
[<RequireQualifiedAccess>]
module Sqlite =

    /// <summary>Opens a driver from an ADO.NET connection string. The driver owns the connection.</summary>
    let openConnectionString (connectionString: string) =
        SqliteDriver.create connectionString

    /// <summary>Wraps an existing <see cref="SqliteConnection"/>. The driver does not own the connection.</summary>
    let wrapConnection (connection: SqliteConnection) =
        SqliteDriver.wrapConnection connection

    /// <summary>Opens a driver pointing at a SQLite database file.</summary>
    let openFile path =
        SqliteDriver.createFromFile path

    /// <summary>Opens a driver backed by an in-memory database.</summary>
    let openInMemory () =
        SqliteDriver.createInMemory ()
