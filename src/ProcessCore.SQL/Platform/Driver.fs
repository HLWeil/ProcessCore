namespace ProcessCore.SQL.Platform

open ProcessCore.SQL

/// Describes a runtime adapter without taking a dependency on its connector package yet.
type ConnectorPlan =
    {
        Runtime: string
        CandidatePackage: string
        Notes: string list
    }

[<RequireQualifiedAccess>]
module Driver =

    let dotNet =
        {
            Runtime = ".NET"
            CandidatePackage = "Microsoft.Data.Sqlite"
            Notes =
                [
                    "Implement ISqliteDriver over a synchronous connection wrapper."
                    "Keep the adapter outside the shared table/codecs layer."
                ]
        }

    let javaScript =
        {
            Runtime = "JavaScript/Node"
            CandidatePackage = "better-sqlite3"
            Notes =
                [
                    "Prefer a synchronous connector if repository APIs stay synchronous."
                    "Use Fable extern bindings in this platform module only."
                ]
        }

    let typeScript =
        {
            Runtime = "TypeScript/Node"
            CandidatePackage = "better-sqlite3"
            Notes =
                [
                    "Share the JavaScript binding if Fable output and packaging permit it."
                    "Add typed externs before exposing the adapter as a public TypeScript package."
                ]
        }

    let python =
        {
            Runtime = "Python"
            CandidatePackage = "sqlite3"
            Notes =
                [
                    "Use Python stdlib sqlite3 through Fable Python bindings."
                    "Map Python row tuples/dicts to SqlRow at the adapter boundary."
                ]
        }

    let connectorPlans =
        [
            dotNet
            javaScript
            typeScript
            python
        ]

    let notConfigured runtime : ISqliteDriver =
        invalidOp $"No SQLite connector adapter has been configured for {runtime} yet."
