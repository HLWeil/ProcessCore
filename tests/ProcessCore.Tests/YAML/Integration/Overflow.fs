module ProcessCore.Yaml.Tests.Integration.Overflow

open Fable.Pyxpecto
open DynamicObj
open ProcessCore
open ProcessCore.Yaml

let tests = testList "Overflow" [

    testCase "unknown field on DefinedTerm survives round-trip" <| fun _ ->
        let yaml = "type: DefinedTerm\nname: foo\nmyCustomField: bar\n"
        let dt   = Yaml.DefinedTerm.fromYamlString false yaml
        // accessible via DynamicObj overflow
        let v = dt.TryGetPropertyValue("myCustomField") |> Option.map string
        Expect.equal v (Some "bar") "overflow value accessible after decode"
        // survives re-encoding
        let yaml2 = Yaml.DefinedTerm.toYamlString None dt
        Expect.isTrue (yaml2.Contains("myCustomField")) "overflow key re-emitted"
        Expect.isTrue (yaml2.Contains("bar"))           "overflow value re-emitted"

    testCase "unknown field on Material survives round-trip" <| fun _ ->
        let yaml = "type: Material\nname: S1\nextraAnnotation: some-value\n"
        let m    = Yaml.Material.fromYamlString false yaml
        let v = m.TryGetPropertyValue("extraAnnotation") |> Option.map string
        Expect.equal v (Some "some-value") "overflow value accessible"
        let yaml2 = Yaml.Material.toYamlString None m
        Expect.isTrue (yaml2.Contains("extraAnnotation")) "overflow key re-emitted"

    testCase "unknown field on Material throws on round-trip for processcore only" <| fun _ ->
        let yaml = "type: Data\npath: data.csv\nunexpectedField: surprise\n"
        let f ()  = Yaml.Data.fromYamlString true yaml |> ignore
        Expect.throws f "unexpected field should raise in processCoreOnly mode"

    testCase "unknown field on Dataset survives round-trip" <| fun _ ->
        let yaml = "type: Dataset\nidentifier: DS-1\ncustomMeta: my-value\n"
        let ds   = Yaml.Dataset.fromYamlString false yaml
        let v = ds.TryGetPropertyValue("customMeta") |> Option.map string
        Expect.equal v (Some "my-value") "overflow value accessible"
        let yaml2 = Yaml.Dataset.toYamlString None ds
        Expect.isTrue (yaml2.Contains("customMeta")) "overflow key re-emitted"

    testCase "unknown nested object survives round-trip" <| fun _ ->
        let yaml = "type: DefinedTerm\nname: foo\nnested:\n  key: value\n  count: 42\n"
        let dt   = Yaml.DefinedTerm.fromYamlString false yaml
        // nested object stored as DynamicObj
        let v = dt.TryGetPropertyValue("nested") |> Option.map unbox<DynamicObj>
        Expect.isSome v "nested object in overflow"
        let nested = v.Value
        let count = nested.TryGetPropertyValue("count") |> Option.map unbox<int>
        Expect.equal count (Some 42) "nested integer value preserved"
        // survives re-encoding
        let yaml2 = Yaml.DefinedTerm.toYamlString None dt
        Expect.isTrue (yaml2.Contains("nested"))  "nested key re-emitted"
        Expect.isTrue (yaml2.Contains("key"))     "nested contents re-emitted"

    testCase "unknown sequence survives round-trip" <| fun _ ->
        let yaml = "type: Material\nname: S1\nmyList:\n  - alpha\n  - beta\n  - gamma\n"
        let m    = Yaml.Material.fromYamlString false yaml
        // sequence stored as ResizeArray<obj>
        let exists = m.GetProperties(true) |> Seq.exists (fun kv -> kv.Key = "myList")
        Expect.isTrue exists "sequence present in overflow"
        let yaml2 = Yaml.Material.toYamlString None m
        Expect.isTrue (yaml2.Contains("myList")) "sequence key re-emitted"
        Expect.isTrue (yaml2.Contains("alpha"))  "sequence content re-emitted"
        Expect.isTrue (yaml2.Contains("beta"))   "sequence item re-emitted"

    testCase "known fields not re-emitted as overflow" <| fun _ ->
        let dt = DefinedTerm("foo", tan = "T:1", inDefinedTermSet = "http://example.org/onto.owl")
        let yaml = Yaml.DefinedTerm.toYamlString None dt
        // each known field should appear exactly once
        let countOf (sub: string) (s: string) =
            s.Split([| sub |], System.StringSplitOptions.None).Length - 1
        Expect.equal (countOf "name:" yaml)             1 "name appears exactly once"
        Expect.equal (countOf "TAN:" yaml)              1 "TAN appears exactly once"
        Expect.equal (countOf "inDefinedTermSet:" yaml) 1 "inDefinedTermSet appears exactly once"
        Expect.equal (countOf "type:" yaml)             1 "type appears exactly once"

]
