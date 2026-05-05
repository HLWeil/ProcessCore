namespace ProcessCore.SQL.Platform

open Fable.Core
open ProcessCore.SQL

/// Describes a runtime adapter without taking a dependency on its connector package yet.
[<AttachMembers>]
type ConnectorPlan(Runtime: string, CandidatePackage: string, Notes: string[]) =

    member val Runtime = Runtime with get, set
    member val CandidatePackage = CandidatePackage with get, set
    member val Notes = Notes with get, set

    [<NamedParams>]
    static member create(Runtime: string, CandidatePackage: string, Notes: string[]) =
        ConnectorPlan(Runtime, CandidatePackage, Notes)

[<RequireQualifiedAccess>]
module Driver =

    let dotNet =
        ConnectorPlan(
            ".NET",
            "Microsoft.Data.Sqlite",
            [|
                "Implement ISqliteDriver over a synchronous connection wrapper."
                "Keep the adapter outside the shared table/codecs layer."
            |]
        )

    let javaScript =
        ConnectorPlan(
            "JavaScript/Node",
            "better-sqlite3",
            [|
                "Prefer a synchronous connector if repository APIs stay synchronous."
                "Use Fable extern bindings in this platform module only."
            |]
        )

    let typeScript =
        ConnectorPlan(
            "TypeScript/Node",
            "better-sqlite3",
            [|
                "Share the JavaScript binding if Fable output and packaging permit it."
                "Add typed externs before exposing the adapter as a public TypeScript package."
            |]
        )

    let python =
        ConnectorPlan(
            "Python",
            "sqlite3",
            [|
                "Use Python stdlib sqlite3 through Fable Python bindings."
                "Map Python row tuples/dicts to SqlRow at the adapter boundary."
            |]
        )

    let connectorPlans =
        [|
            dotNet
            javaScript
            typeScript
            python
        |]

    let notConfigured runtime : ISqliteDriver =
        invalidOp $"No SQLite connector adapter has been configured for {runtime} yet."
