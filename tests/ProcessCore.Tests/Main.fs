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
            Integration.tests
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
            Graph.DatasetQueries.tests
            Graph.PathQueries.tests
            Graph.ProcessGraphQueries.tests
            Table.CompositeTypes.tests
            Table.TableAux.tests
            Table.TableRead.tests
            Table.TableWrite.tests
            Table.TablesApi.tests
        ]

[<EntryPoint>]
let main _ =
    !!Pyxpecto.runTests [||] all
