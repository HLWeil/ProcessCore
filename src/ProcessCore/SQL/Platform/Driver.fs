namespace ProcessCore.SQL.Platform

open Fable.Core
open ProcessCore.SQL

/// <summary>
/// Describes a runtime adapter without taking a dependency on its connector package yet.
/// </summary>
/// <remarks>
/// Plans are static metadata: they declare the runtime, the candidate connector library and a
/// short list of free-form integration notes. They do not contain executable connection logic —
/// the actual <see cref="ISqliteDriver"/> implementations live in the runtime-specific projects
/// (<c>ProcessCore.SQL.DotNet</c>, <c>ProcessCore.SQL.JavaScript</c>, <c>ProcessCore.SQL.Python</c>).
/// </remarks>
/// <param name="Runtime">Display name of the target runtime (e.g. <c>".NET"</c>, <c>"Python"</c>).</param>
/// <param name="CandidatePackage">Connector package proposed for the runtime.</param>
/// <param name="Notes">Free-form integration notes used as guidance when wiring up the adapter.</param>
[<AttachMembers>]
type ConnectorPlan(Runtime: string, CandidatePackage: string, Notes: string[]) =

    /// <summary>Display name of the target runtime.</summary>
    member val Runtime = Runtime with get, set
    /// <summary>Proposed connector package.</summary>
    member val CandidatePackage = CandidatePackage with get, set
    /// <summary>Free-form integration notes.</summary>
    member val Notes = Notes with get, set

    /// <summary>Named-argument constructor exposed to JavaScript and Python callers.</summary>
    [<NamedParams>]
    static member create(Runtime: string, CandidatePackage: string, Notes: string[]) =
        ConnectorPlan(Runtime, CandidatePackage, Notes)

/// <summary>
/// Catalogue of <see cref="ConnectorPlan"/> values, one per supported runtime, plus a fallback
/// helper for runtimes that do not yet have an adapter wired up.
/// </summary>
[<RequireQualifiedAccess>]
module Driver =

    /// <summary>Plan for the .NET runtime backed by <c>Microsoft.Data.Sqlite</c>.</summary>
    let dotNet =
        ConnectorPlan(
            ".NET",
            "Microsoft.Data.Sqlite",
            [|
                "Implement ISqliteDriver over a synchronous connection wrapper."
                "Keep the adapter outside the shared table/codecs layer."
            |]
        )

    /// <summary>Plan for the Node.js runtime backed by the <c>better-sqlite3</c> npm package.</summary>
    let javaScript =
        ConnectorPlan(
            "JavaScript/Node",
            "better-sqlite3",
            [|
                "Prefer a synchronous connector if repository APIs stay synchronous."
                "Use Fable extern bindings in this platform module only."
            |]
        )

    /// <summary>Plan for a TypeScript-on-Node consumer that re-uses the JavaScript adapter.</summary>
    let typeScript =
        ConnectorPlan(
            "TypeScript/Node",
            "better-sqlite3",
            [|
                "Share the JavaScript binding if Fable output and packaging permit it."
                "Add typed externs before exposing the adapter as a public TypeScript package."
            |]
        )

    /// <summary>Plan for the Python runtime backed by the stdlib <c>sqlite3</c> module.</summary>
    let python =
        ConnectorPlan(
            "Python",
            "sqlite3",
            [|
                "Use Python stdlib sqlite3 through Fable Python bindings."
                "Map Python row tuples/dicts to SqlRow at the adapter boundary."
            |]
        )

    /// <summary>All known plans, in declaration order.</summary>
    let connectorPlans =
        [|
            dotNet
            javaScript
            typeScript
            python
        |]

    /// <summary>
    /// Returns an <see cref="ISqliteDriver"/> placeholder that always raises, used as the default
    /// driver for runtimes that have not been wired up yet.
    /// </summary>
    /// <param name="runtime">Display name of the runtime, embedded in the error message.</param>
    let notConfigured runtime : ISqliteDriver =
        invalidOp $"No SQLite connector adapter has been configured for {runtime} yet."
