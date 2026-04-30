namespace ProcessCore.SQL

/// SQLite values supported by the shared, Fable-compatible API surface.
[<RequireQualifiedAccess>]
type SqlValue =
    | Null
    | Text of string
    | Int of int

type SqlParameters = (string * SqlValue) list

type SqlRow = Map<string, SqlValue>

/// Minimal driver contract implemented by runtime-specific SQLite adapters.
type ISqliteDriver =
    abstract Execute : sql: string -> parameters: SqlParameters -> unit
    abstract Query : sql: string -> parameters: SqlParameters -> SqlRow list
    abstract Scalar : sql: string -> parameters: SqlParameters -> SqlValue

[<RequireQualifiedAccess>]
module SqlValue =

    let ofTextOption value =
        match value with
        | Some text -> SqlValue.Text text
        | None -> SqlValue.Null

    let ofInt value = SqlValue.Int value

    let asText column value =
        match value with
        | SqlValue.Text text -> text
        | SqlValue.Null -> invalidArg column $"Column '{column}' is NULL."
        | SqlValue.Int _ -> invalidArg column $"Column '{column}' is not TEXT."

    let asTextOption column value =
        match value with
        | SqlValue.Text text -> Some text
        | SqlValue.Null -> None
        | SqlValue.Int _ -> invalidArg column $"Column '{column}' is not TEXT."

    let asInt column value =
        match value with
        | SqlValue.Int number -> number
        | SqlValue.Null -> invalidArg column $"Column '{column}' is NULL."
        | SqlValue.Text _ -> invalidArg column $"Column '{column}' is not INTEGER."

[<RequireQualifiedAccess>]
module SqlRow =

    let private value table column (row: SqlRow) =
        match Map.tryFind column row with
        | Some value -> value
        | None -> invalidArg table $"Missing column '{table}.{column}'."

    let text table column row =
        value table column row |> SqlValue.asText $"{table}.{column}"

    let textOption table column row =
        value table column row |> SqlValue.asTextOption $"{table}.{column}"

    let int table column row =
        value table column row |> SqlValue.asInt $"{table}.{column}"
