module ProcessCore.Tests.Types.FormalParameter

open Fable.Pyxpecto
open ProcessCore

let tests = testList "FormalParameter" [

    testCase "equality by name only" <| fun _ ->
        let fp1 = FormalParameter("temperature", nameTAN = "http://example.org/a")
        let fp2 = FormalParameter("temperature")
        Expect.equal fp1 fp2 "Same name regardless of TAN → equal"

    testCase "inequality different name" <| fun _ ->
        let fp1 = FormalParameter("temperature")
        let fp2 = FormalParameter("rpm")
        Expect.notEqual fp1 fp2 "Different names → not equal"

    testCase "DefaultValue field" <| fun _ ->
        let fp = FormalParameter("temperature")
        Expect.isNone fp.DefaultValue "DefaultValue starts as None"
        let dt = DefinedTerm("room temperature")
        fp.DefaultValue <- Some dt
        Expect.isSome fp.DefaultValue           "DefaultValue should be Some after setting"
        Expect.equal fp.DefaultValue.Value dt   "DefaultValue should hold the assigned DefinedTerm"

]
