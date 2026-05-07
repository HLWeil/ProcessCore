module ProcessCore.Yaml.Tests.Main

open Fable.Pyxpecto

#if !FABLE_COMPILER_JAVASCRIPT && !FABLE_COMPILER_TYPESCRIPT
let inline (!!) value = value
#endif

#if FABLE_COMPILER_JAVASCRIPT || FABLE_COMPILER_TYPESCRIPT
open Fable.Core.JsInterop
#endif

let all =
    testList "ProcessCore.YAML" [
        Codecs.DefinedTerm.tests
        Codecs.FormalParameter.tests
        Codecs.PropertyValue.tests
        Codecs.Material.tests
        Codecs.Data.tests
        Codecs.LabProtocol.tests
        Codecs.LabProcess.tests
        Codecs.Dataset.tests
        Integration.RoundTrip.tests
        Integration.Overflow.tests
        Mode.StrictMode.tests
        Mode.LenientMode.tests
        Integration.Examples.tests
    ]

[<EntryPoint>]
let main _ =
    !!Pyxpecto.runTests [||] all
