module ProcessCore.Yaml.Tests.All

open Fable.Pyxpecto

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
