namespace ProcessCore.SQL

open Fable.Core

/// <summary>
/// SQLite values supported by the shared, Fable-compatible API surface.
/// </summary>
/// <remarks>
/// The shared API intentionally exposes only the three SQLite storage classes that map cleanly across .NET,
/// JavaScript and Python: <c>NULL</c>, <c>TEXT</c> and 32-bit signed <c>INTEGER</c>. Larger integers,
/// floating-point, blob and date/time values are not part of the cross-runtime contract — drivers either
/// reject them or coerce them into one of these three cases.
/// </remarks>
type SqlValue =
    /// <summary>Represents a SQL <c>NULL</c>.</summary>
    | Null
    /// <summary>Represents a SQL <c>TEXT</c> value.</summary>
    | Text of string
    /// <summary>Represents a SQL 32-bit signed <c>INTEGER</c> value. Drivers reject values outside <see cref="System.Int32"/>.</summary>
    | Int of int

/// <summary>
/// A single named parameter bound to an <see cref="SqlValue"/>, used to compose <see cref="SqlParameters"/>.
/// </summary>
/// <param name="Name">
/// Parameter name. May be passed with or without a leading sigil (<c>$</c>, <c>@</c>, <c>:</c>);
/// drivers normalize the name to the form their underlying engine expects.
/// </param>
/// <param name="Value">The bound value.</param>
[<AttachMembers>]
type SqlParameter(Name: string, Value: SqlValue) =

    /// <summary>The parameter name. Sigil-handling is the driver's responsibility.</summary>
    member val Name = Name with get, set
    /// <summary>The bound <see cref="SqlValue"/>.</summary>
    member val Value = Value with get, set

    /// <summary>
    /// Named-argument constructor exposed to JavaScript and Python callers via Fable's <c>NamedParams</c>.
    /// </summary>
    /// <param name="Name">Parameter name (with or without sigil).</param>
    /// <param name="Value">The bound value.</param>
    [<NamedParams>]
    static member create (Name: string, Value: SqlValue) =
        SqlParameter(Name, Value)

/// <summary>
/// An ordered collection of <see cref="SqlParameter"/> values bound to a single SQL command.
/// </summary>
type SqlParameters = SqlParameter[]

/// <summary>
/// A single result-set row, keyed by column name. Driver implementations populate this map from
/// the underlying engine's row representation (ADO.NET reader, better-sqlite3 object, Python tuple, …).
/// </summary>
type SqlRow = Map<string, SqlValue>

/// <summary>
/// Minimal driver contract implemented by runtime-specific SQLite adapters
/// (<c>ProcessCore.SQL.DotNet</c>, <c>ProcessCore.SQL.JavaScript</c>, <c>ProcessCore.SQL.Python</c>).
/// </summary>
/// <remarks>
/// The repository layer is written against this interface only — it has no knowledge of which
/// physical SQLite engine is in use. All members are synchronous; async semantics, if needed, are
/// the responsibility of the caller composing the driver.
/// </remarks>
type ISqliteDriver =
    /// <summary>Executes a non-query statement (INSERT/UPDATE/DELETE/DDL).</summary>
    /// <param name="sql">The SQL text. Use parameter placeholders rather than string interpolation.</param>
    /// <param name="parameters">Parameters bound to the placeholders in <paramref name="sql"/>.</param>
    abstract Execute : sql: string -> parameters: SqlParameters -> unit
    /// <summary>Executes a query and returns all rows.</summary>
    /// <param name="sql">The SELECT (or other row-producing) statement.</param>
    /// <param name="parameters">Parameters bound to the placeholders in <paramref name="sql"/>.</param>
    /// <returns>An array of rows, each keyed by column name.</returns>
    abstract Query : sql: string -> parameters: SqlParameters -> SqlRow[]
    /// <summary>Executes a query and returns the first column of the first row.</summary>
    /// <param name="sql">The query. The driver returns <see cref="SqlValue.Null"/> when no row is produced.</param>
    /// <param name="parameters">Parameters bound to the placeholders in <paramref name="sql"/>.</param>
    abstract Scalar : sql: string -> parameters: SqlParameters -> SqlValue

/// <summary>
/// Helpers for constructing <see cref="SqlValue"/> instances from primitive values and for
/// extracting strongly-typed values back out, with descriptive errors when the underlying case
/// does not match the expected SQLite storage class.
/// </summary>
[<RequireQualifiedAccess>]
module SqlValue =

    /// <summary>
    /// Lifts a string option to an <see cref="SqlValue"/>, mapping <c>None</c> to <see cref="SqlValue.Null"/>.
    /// </summary>
    let ofTextOption value =
        match value with
        | Some text -> SqlValue.Text text
        | None -> SqlValue.Null

    /// <summary>Wraps an integer as <see cref="SqlValue.Int"/>.</summary>
    let ofInt value = SqlValue.Int value

    /// <summary>
    /// Extracts a required text value. Raises <see cref="System.ArgumentException"/> if the value
    /// is <see cref="SqlValue.Null"/> or non-textual; the <paramref name="column"/> name is included
    /// in the error so callers can identify the offending column.
    /// </summary>
    /// <param name="column">Logical column name used in error messages.</param>
    /// <param name="value">The value to project.</param>
    let asText column value =
        match value with
        | SqlValue.Text text -> text
        | SqlValue.Null -> invalidArg column $"Column '{column}' is NULL."
        | SqlValue.Int _ -> invalidArg column $"Column '{column}' is not TEXT."

    /// <summary>
    /// Extracts an optional text value, returning <c>None</c> for <see cref="SqlValue.Null"/>.
    /// Raises if the value is non-textual.
    /// </summary>
    /// <param name="column">Logical column name used in error messages.</param>
    /// <param name="value">The value to project.</param>
    let asTextOption column value =
        match value with
        | SqlValue.Text text -> Some text
        | SqlValue.Null -> None
        | SqlValue.Int _ -> invalidArg column $"Column '{column}' is not TEXT."

    /// <summary>
    /// Extracts a required integer value. Raises <see cref="System.ArgumentException"/> if the value
    /// is <see cref="SqlValue.Null"/> or non-integer.
    /// </summary>
    /// <param name="column">Logical column name used in error messages.</param>
    /// <param name="value">The value to project.</param>
    let asInt column value =
        match value with
        | SqlValue.Int number -> number
        | SqlValue.Null -> invalidArg column $"Column '{column}' is NULL."
        | SqlValue.Text _ -> invalidArg column $"Column '{column}' is not INTEGER."

/// <summary>
/// Helpers for extracting strongly-typed values from a <see cref="SqlRow"/>.
/// All helpers raise with a fully qualified <c>table.column</c> reference when the lookup fails
/// or the storage class is wrong.
/// </summary>
[<RequireQualifiedAccess>]
module SqlRow =

    let private value table column (row: SqlRow) =
        match Map.tryFind column row with
        | Some value -> value
        | None -> invalidArg table $"Missing column '{table}.{column}'."

    /// <summary>Reads a required <c>TEXT</c> column from <paramref name="row"/>.</summary>
    /// <param name="table">Logical table name (used in error messages).</param>
    /// <param name="column">Column name to read.</param>
    /// <param name="row">The row to read from.</param>
    let text table column row =
        value table column row |> SqlValue.asText $"{table}.{column}"

    /// <summary>Reads an optional <c>TEXT</c> column, returning <c>None</c> for SQL <c>NULL</c>.</summary>
    /// <param name="table">Logical table name (used in error messages).</param>
    /// <param name="column">Column name to read.</param>
    /// <param name="row">The row to read from.</param>
    let textOption table column row =
        value table column row |> SqlValue.asTextOption $"{table}.{column}"

    /// <summary>Reads a required <c>INTEGER</c> column from <paramref name="row"/>.</summary>
    /// <param name="table">Logical table name (used in error messages).</param>
    /// <param name="column">Column name to read.</param>
    /// <param name="row">The row to read from.</param>
    let int table column row =
        value table column row |> SqlValue.asInt $"{table}.{column}"
