module ProcessCore.Yaml.Tests.Integration.Overflow

open Fable.Pyxpecto
open DynamicObj
open ProcessCore
open ProcessCore.Spreadsheet
open ProcessCore.Yaml

let private overflowValue (dataset: Dataset) name =
    dataset.TryGetPropertyValue(name)

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

    testCase "scalar overflow values retain concrete primitive types" <| fun _ ->
        let yaml =
            """type: Dataset
identifier: scalar-overflow
stringValue: hello
integerValue: 42
booleanValue: true
decimalValue: 3.14
"""
        let dataset = Yaml.Dataset.fromYamlString false yaml

        Expect.isTrue (overflowValue dataset "stringValue" |> Option.exists (fun value -> value :? string)) "string overflow type"
        Expect.isTrue (overflowValue dataset "integerValue" |> Option.exists (fun value -> value :? int)) "integer overflow type"
        Expect.isTrue (overflowValue dataset "booleanValue" |> Option.exists (fun value -> value :? bool)) "boolean overflow type"
        Expect.isTrue (overflowValue dataset "decimalValue" |> Option.exists (fun value -> value :? decimal)) "decimal overflow type"

    testCase "type-tagged DefinedTerm overflow is statically typed" <| fun _ ->
        let yaml =
            """type: Dataset
identifier: typed-defined-term
term:
  type: DefinedTerm
  name: liquid chromatography
  TAN: OBI:0000470
"""
        let dataset = Yaml.Dataset.fromYamlString false yaml
        match overflowValue dataset "term" with
        | Some (:? DefinedTerm as term) ->
            Expect.equal term.Name "liquid chromatography" "typed term name"
            Expect.equal term.TAN (Some "OBI:0000470") "typed term TAN"
        | _ -> Expect.isTrue false "term should be a DefinedTerm"

    testCase "@type-tagged DefinedTerm overflow is statically typed" <| fun _ ->
        let yaml =
            """type: Dataset
identifier: at-type-defined-term
term:
  '@type': DefinedTerm
  name: mass spectrometry
  TAN: OBI:0000084
"""
        let dataset = Yaml.Dataset.fromYamlString false yaml
        match overflowValue dataset "term" with
        | Some (:? DefinedTerm as term) ->
            Expect.equal term.Name "mass spectrometry" "@type term name"
            Expect.equal term.TAN (Some "OBI:0000084") "@type term TAN"
        | _ -> Expect.isTrue false "@type term should be a DefinedTerm"

    testCase "nested FormalParameter and DefinedTerm overflow are typed" <| fun _ ->
        let yaml =
            """type: Dataset
identifier: typed-formal-parameter
parameter:
  type: FormalParameter
  name: temperature
  defaultValue:
    type: DefinedTerm
    name: 37 Celsius
"""
        let dataset = Yaml.Dataset.fromYamlString false yaml
        match overflowValue dataset "parameter" with
        | Some (:? FormalParameter as parameter) ->
            Expect.equal parameter.Name "temperature" "formal parameter name"
            Expect.isSome parameter.DefaultValue "formal parameter default value"
            Expect.equal parameter.DefaultValue.Value.Name "37 Celsius" "nested defined term name"
        | _ -> Expect.isTrue false "parameter should be a FormalParameter"

    testCase "nested Annotation and FormalParameter overflow are typed" <| fun _ ->
        let yaml =
            """type: Dataset
identifier: typed-annotation
annotation:
  type: Annotation
  name: temperature
  value: 37
  instanceOf:
    type: FormalParameter
    name: incubation temperature
"""
        let dataset = Yaml.Dataset.fromYamlString false yaml
        match overflowValue dataset "annotation" with
        | Some (:? Annotation as annotation) ->
            Expect.equal annotation.Name "temperature" "annotation name"
            Expect.isSome annotation.InstanceOf "annotation formal parameter"
            Expect.equal annotation.InstanceOf.Value.Name "incubation temperature" "nested parameter name"
        | _ -> Expect.isTrue false "annotation should be an Annotation"

    testCase "homogeneous tagged overflow sequences become concrete collections" <| fun _ ->
        let yaml =
            """type: Dataset
identifier: typed-term-sequence
terms:
  - type: DefinedTerm
    name: one
  - '@type': DefinedTerm
    name: two
"""
        let dataset = Yaml.Dataset.fromYamlString false yaml
        match overflowValue dataset "terms" with
        | Some (:? ResizeArray<DefinedTerm> as terms) -> Expect.equal (terms |> Seq.map (fun term -> term.Name) |> Seq.toList) [ "one"; "two" ] "typed term sequence"
        | _ -> Expect.isTrue false "terms should be ResizeArray<DefinedTerm>"

    testCase "homogeneous scalar overflow sequences become concrete collections" <| fun _ ->
        let yaml =
            """type: Dataset
identifier: typed-string-sequence
workflowIdentifiers:
  - workflow-1
  - workflow-2
"""
        let dataset = Yaml.Dataset.fromYamlString false yaml
        match overflowValue dataset "workflowIdentifiers" with
        | Some (:? ResizeArray<string> as values) -> Expect.equal (values |> Seq.toList) [ "workflow-1"; "workflow-2" ] "typed string sequence"
        | _ -> Expect.isTrue false "workflow identifiers should be ResizeArray<string>"

    testCase "untagged object overflow sequences become DynamicObj collections" <| fun _ ->
        let yaml =
            """type: Dataset
identifier: dynamic-object-sequence
comments:
  - name: registrationLedger
    value: registered
"""
        let dataset = Yaml.Dataset.fromYamlString false yaml
        match overflowValue dataset "comments" with
        | Some (:? ResizeArray<DynamicObj> as comments) ->
            Expect.equal (comments.[0].TryGetPropertyValue("name") |> Option.map string) (Some "registrationLedger") "dynamic object name"
        | _ -> Expect.isTrue false "comments should be ResizeArray<DynamicObj>"

    testCase "mixed or unknown tagged overflow sequences use generic fallback" <| fun _ ->
        let yaml =
            """type: Dataset
identifier: generic-sequence-fallback
values:
  - type: DefinedTerm
    name: known
  - type: UnknownDecoration
    value: unknown
"""
        let dataset = Yaml.Dataset.fromYamlString false yaml
        Expect.isTrue (overflowValue dataset "values" |> Option.exists (fun value -> value :? ResizeArray<obj>)) "mixed tagged sequence should remain generic"

    testCase "typed overflow re-encodes with its discriminator" <| fun _ ->
        let yaml =
            """type: Dataset
identifier: typed-reencode
term:
  '@type': DefinedTerm
  name: re-encoded
"""
        let dataset = Yaml.Dataset.fromYamlString false yaml
        let output = Yaml.Dataset.toYamlString None dataset
        Expect.isTrue (output.Contains("type: DefinedTerm")) "typed overflow discriminator is retained"

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

        match overflowValue decodedRun "WorkflowIdentifiers" with
        | Some (:? ResizeArray<string> as workflowIdentifiers) ->
            Expect.equal (workflowIdentifiers |> Seq.toList) [ "workflow-1"; "workflow-2" ]
                "workflow identifiers must retain their typed collection"
        | _ ->
            Expect.isTrue false "WorkflowIdentifiers must decode as ResizeArray<string>"

        match overflowValue decodedRun "MeasurementType" with
        | Some (:? DefinedTerm as measurementType) ->
            Expect.equal measurementType.Name "LC-MS" "measurement type name"
            Expect.equal measurementType.TAN (Some "OBI:0000470") "measurement type TAN"
        | _ ->
            Expect.isTrue false "MeasurementType must decode as DefinedTerm"

        match overflowValue decodedRun "TechnologyType" with
        | Some (:? DefinedTerm as technologyType) ->
            Expect.equal technologyType.Name "mass spectrometry" "technology type name"
            Expect.equal technologyType.TAN (Some "OBI:0000084") "technology type TAN"
        | _ ->
            Expect.isTrue false "TechnologyType must decode as DefinedTerm"

        match overflowValue decodedRun "Comments" with
        | Some (:? ResizeArray<DynamicObj> as comments) ->
            let commentName, commentValue = Comment.toString comments.[0]
            Expect.equal commentName (Some "registrationLedger") "comment name"
            Expect.equal commentValue (Some "registered-2026-01-01") "comment value"
        | _ ->
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
