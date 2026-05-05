module ProcessCore.SQL.Tests.Main

open Fable.Pyxpecto

#if !FABLE_COMPILER_JAVASCRIPT && !FABLE_COMPILER_TYPESCRIPT
let inline (!!) value = value
#endif

#if FABLE_COMPILER_JAVASCRIPT || FABLE_COMPILER_TYPESCRIPT
open Fable.Core.JsInterop
#endif

let all =
    testList
        "ProcessCore.SQL"
        [
            TableModelTests.tests
            RowCodecTests.tests
            DotNetDriverTests.tests
            RepositoryCrudTests.tests
        ]

[<EntryPoint>]
let main _ =
    !!Pyxpecto.runTests [||] all
