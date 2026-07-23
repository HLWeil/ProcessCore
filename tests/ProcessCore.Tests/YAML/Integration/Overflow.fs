module ProcessCore.Yaml.Tests.Integration.Overflow

open Fable.Pyxpecto
open DynamicObj
open ProcessCore
open ProcessCore.Spreadsheet
open ProcessCore.Yaml

let private tryTypedProperty<'T when 'T :> obj> (dataset: Dataset) name =
    match dataset.TryGetPropertyValue(name) with
    | Some (:? 'T as value) -> Some value
    | _ -> None

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

    testCase "unknown field on Sample survives round-trip" <| fun _ ->
        let yaml = "type: Sample\nname: S1\nextraAnnotation: some-value\n"
        let m    = Yaml.Sample.fromYamlString false yaml
        let v = m.TryGetPropertyValue("extraAnnotation") |> Option.map string
        Expect.equal v (Some "some-value") "overflow value accessible"
        let yaml2 = Yaml.Sample.toYamlString None m
        Expect.isTrue (yaml2.Contains("extraAnnotation")) "overflow key re-emitted"

    testCase "unknown field on Sample throws on round-trip for processcore only" <| fun _ ->
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
        let yaml = "type: Sample\nname: S1\nmyList:\n  - alpha\n  - beta\n  - gamma\n"
        let m    = Yaml.Sample.fromYamlString false yaml
        // sequence stored as ResizeArray<obj>
        let exists = m.GetProperties(true) |> Seq.exists (fun kv -> kv.Key = "myList")
        Expect.isTrue exists "sequence present in overflow"
        let yaml2 = Yaml.Sample.toYamlString None m
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

    testCase "typed Run overflow survives YAML for spreadsheet writers" <| fun _ ->
        let run = Dataset("run-1", additionalType = "Run")
        run.SetProperty("WorkflowIdentifiers", ResizeArray [ "workflow-1"; "workflow-2" ])
        run.SetProperty("MeasurementType", DefinedTerm("LC-MS", tan = "OBI:0000470"))
        run.SetProperty("TechnologyType", DefinedTerm("mass spectrometry", tan = "OBI:0000084"))
        run.SetProperty("TechnologyPlatform", "Orbitrap")
        run.SetProperty(
            "Comments",
            ResizeArray [ Comment.fromString "registrationLedger" "registered-2026-01-01" ])

        let arc = ARC("typed-run-overflow", hasPart = [ run ])
        let decodedArc = arc.toYamlString(2) |> ARC.fromYamlString
        let decodedRun = decodedArc.HasPart |> Seq.exactlyOne

        match tryTypedProperty<ResizeArray<string>> decodedRun "WorkflowIdentifiers" with
        | Some workflowIdentifiers ->
            Expect.equal (workflowIdentifiers |> Seq.toList) [ "workflow-1"; "workflow-2" ]
                "workflow identifiers must retain their typed collection"
        | None ->
            Expect.isTrue false "WorkflowIdentifiers must decode as ResizeArray<string>"

        match tryTypedProperty<DefinedTerm> decodedRun "MeasurementType" with
        | Some measurementType ->
            Expect.equal measurementType.Name "LC-MS" "measurement type name"
            Expect.equal measurementType.TAN (Some "OBI:0000470") "measurement type TAN"
        | None ->
            Expect.isTrue false "MeasurementType must decode as DefinedTerm"

        match tryTypedProperty<DefinedTerm> decodedRun "TechnologyType" with
        | Some technologyType ->
            Expect.equal technologyType.Name "mass spectrometry" "technology type name"
            Expect.equal technologyType.TAN (Some "OBI:0000084") "technology type TAN"
        | None ->
            Expect.isTrue false "TechnologyType must decode as DefinedTerm"

        match tryTypedProperty<ResizeArray<DynamicObj>> decodedRun "Comments" with
        | Some comments ->
            let commentName, commentValue = Comment.toString comments.[0]
            Expect.equal commentName (Some "registrationLedger") "comment name"
            Expect.equal commentValue (Some "registered-2026-01-01") "comment value"
        | None ->
            Expect.isTrue false "Comments must decode as ResizeArray<DynamicObj>"

        let sparse = Run.toSparseTable decodedRun
        if sparse.Matrix.ContainsKey((Run.workflowIdentifiersLabel, 1)) then
            Expect.equal (sparse.Matrix[(Run.workflowIdentifiersLabel, 1)]) "workflow-1;workflow-2"
                "Run writer must receive typed workflow identifiers"
        else
            Expect.isTrue false "Run writer must receive workflow identifiers"
        if sparse.Matrix.ContainsKey((Run.measurementTypeLabel, 1)) then
            Expect.equal (sparse.Matrix[(Run.measurementTypeLabel, 1)]) "LC-MS"
                "Run writer must receive typed measurement type"
        else
            Expect.isTrue false "Run writer must receive measurement type"
        if sparse.Matrix.ContainsKey(("registrationLedger", 1)) then
            Expect.equal (sparse.Matrix[("registrationLedger", 1)]) "registered-2026-01-01"
                "Run writer must receive typed comments"
        else
            Expect.isTrue false "Run writer must receive comments"

]
