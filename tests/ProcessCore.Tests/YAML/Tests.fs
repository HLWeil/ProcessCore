module ProcessCore.Yaml.Tests.All

open Fable.Pyxpecto

let all =
    testList "ProcessCore.YAML" [
        Codecs.DefinedTerm.tests
        Codecs.FormalParameter.tests
        Codecs.Annotation.tests
        Codecs.Sample.tests
        Codecs.Data.tests
        Codecs.Plan.tests
        Codecs.Process.tests
        Codecs.Dataset.tests
        Integration.RoundTrip.tests
        Integration.Overflow.tests
        Mode.StrictMode.tests
        Mode.LenientMode.tests
        Integration.Examples.tests
    ]
