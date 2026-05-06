module ProcessCore.Tests.Main

open Fable.Pyxpecto

#if !FABLE_COMPILER_JAVASCRIPT && !FABLE_COMPILER_TYPESCRIPT
let inline (!!) value = value
#endif

#if FABLE_COMPILER_JAVASCRIPT || FABLE_COMPILER_TYPESCRIPT
open Fable.Core.JsInterop
#endif

let all =
    testList
        "ProcessCore"
        [
            Synchronization.tests
            Types.PropertyValue.tests
            Types.DefinedTerm.tests
            Types.FormalParameter.tests
            Types.Material.tests
            Types.Data.tests
            Types.LabProtocol.tests
            Types.LabProcess.tests
            Types.Dataset.tests
            Types.IONode.tests
            Graph.BackEdges.tests
            Graph.Deduplication.tests
            Graph.Traversal.tests
            Graph.PropertyValueSources.tests
        ]

[<EntryPoint>]
let main _ =
    !!Pyxpecto.runTests [||] all
