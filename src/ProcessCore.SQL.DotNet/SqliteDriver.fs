namespace ProcessCore.SQL.DotNet

open System
open System.Data
open Microsoft.Data.Sqlite
open ProcessCore.SQL

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

    let addParameters (command: SqliteCommand) parameters =
        parameters
        |> List.iter (fun (name, value) ->
            let parameter = command.CreateParameter()
            parameter.ParameterName <- normalizeParameterName name
            parameter.Value <- parameterValue value
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

    member _.Connection = connection

    interface ISqliteDriver with

        member _.Execute sql parameters =
            use command = createCommand sql parameters
            command.ExecuteNonQuery() |> ignore

        member _.Query sql parameters =
            use command = createCommand sql parameters
            use reader = command.ExecuteReader()

            [
                while reader.Read() do
                    [
                        for index in 0 .. reader.FieldCount - 1 do
                            reader.GetName index, sqliteValueToSqlValue(reader.GetValue index)
                    ]
                    |> Map.ofList
            ]

        member _.Scalar sql parameters =
            use command = createCommand sql parameters
            command.ExecuteScalar() |> sqliteValueToSqlValue

    interface IDisposable with

        member _.Dispose() =
            if ownsConnection then
                connection.Dispose()

[<RequireQualifiedAccess>]
module Sqlite =

    let private enableForeignKeys (driver: SqliteDriver) =
        (driver :> ISqliteDriver).Execute "PRAGMA foreign_keys = ON;" []
        driver

    let openConnectionString (connectionString: string) =
        let connection = new SqliteConnection(connectionString)
        connection.Open()
        new SqliteDriver(connection, true)
        |> enableForeignKeys

    let wrapConnection (connection: SqliteConnection) =
        if connection.State <> ConnectionState.Open then
            connection.Open()

        new SqliteDriver(connection, false)
        |> enableForeignKeys

    let openFile path =
        let builder = SqliteConnectionStringBuilder()
        builder.DataSource <- path
        openConnectionString (builder.ToString())

    let openInMemory () =
        openConnectionString "Data Source=:memory:"
