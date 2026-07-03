module ProcessCore.Yaml.Tests.Codecs.DefinedTerm

open Fable.Pyxpecto
open ProcessCore
open ProcessCore.Yaml

let tests = testList "DefinedTerm" [

    testCase "encode name only" <| fun _ ->
        let dt   = DefinedTerm("foo")
        let yaml = Yaml.DefinedTerm.toYamlString None dt
        Expect.isTrue (yaml.Contains("name: foo"))         "name key"
        Expect.isTrue (yaml.Contains("type: DefinedTerm")) "type key"

    testCase "encode all fields" <| fun _ ->
        let dt   = DefinedTerm("cell growth", tan = "GO:0016049", inDefinedTermSet = "http://purl.obolibrary.org/obo/go.owl")
        let yaml = Yaml.DefinedTerm.toYamlString None dt
        Expect.isTrue (yaml.Contains("name: cell growth"))                          "name"
        Expect.isTrue (yaml.Contains("TAN: GO:0016049"))                            "TAN"
        Expect.isTrue (yaml.Contains("inDefinedTermSet: http://purl.obolibrary.org/obo/go.owl")) "inDefinedTermSet"

    testCase "decode name only" <| fun _ ->
        let yaml = "type: DefinedTerm\nname: bar\n"
        let dt   = Yaml.DefinedTerm.fromYamlString true yaml
        Expect.equal dt.Name "bar" "name"
        Expect.equal dt.TAN  None  "no TAN"

    testCase "decode all fields" <| fun _ ->
        let yaml = "type: DefinedTerm\nname: cell growth\nTAN: GO:0016049\ninDefinedTermSet: http://purl.obolibrary.org/obo/go.owl\n"
        let dt   = Yaml.DefinedTerm.fromYamlString true yaml
        Expect.equal dt.Name              "cell growth"                               "name"
        Expect.equal dt.TAN               (Some "GO:0016049")                          "TAN"
        Expect.equal dt.InDefinedTermSet  (Some "http://purl.obolibrary.org/obo/go.owl") "inDefinedTermSet"

    testCase "decode inDefinedTermSet as inline object" <| fun _ ->
        let yaml = "type: DefinedTerm\nname: foo\ninDefinedTermSet:\n  id: http://example.org/onto.owl\n"
        let dt   = Yaml.DefinedTerm.fromYamlString true yaml
        Expect.equal dt.InDefinedTermSet (Some "http://example.org/onto.owl") "inDefinedTermSet from inline object"

    testCase "round-trip name only" <| fun _ ->
        let original = DefinedTerm("alpha")
        let yaml     = Yaml.DefinedTerm.toYamlString None original
        let decoded  = Yaml.DefinedTerm.fromYamlString true yaml
        Expect.equal decoded.Name original.Name "name"
        Expect.equal decoded.TAN  original.TAN  "TAN"

    testCase "round-trip all fields" <| fun _ ->
        let original = DefinedTerm("cell growth", tan = "GO:0016049", inDefinedTermSet = "http://purl.obolibrary.org/obo/go.owl")
        let yaml     = Yaml.DefinedTerm.toYamlString None original
        let decoded  = Yaml.DefinedTerm.fromYamlString true yaml
        Expect.equal decoded original "round-trip equality"

    testCase "fromYamlString" <| fun _ ->
        let yaml = "type: DefinedTerm\nname: baz\nTAN: MS:1000031\n"
        let dt   = Yaml.DefinedTerm.fromYamlString true yaml
        Expect.equal dt.Name "baz"              "name"
        Expect.equal dt.TAN  (Some "MS:1000031") "TAN"

    testCase "toYamlString default whitespace" <| fun _ ->
        let dt   = DefinedTerm("a", tan = "T:1")
        let yaml = Yaml.DefinedTerm.toYamlString None dt
        // With 2-space indent the mapping block uses 2 spaces before nested items.
        // For a flat object there is no nesting, but the output should be non-empty valid YAML.
        Expect.isTrue (yaml.Length > 0) "non-empty output"
        Expect.isTrue (yaml.Contains("name: a")) "name present"

    testCase "toYamlString custom whitespace" <| fun _ ->
        let dt     = DefinedTerm("a")
        let yaml4  = Yaml.DefinedTerm.toYamlString (Some 4) dt
        let yaml2  = Yaml.DefinedTerm.toYamlString (Some 2) dt
        // Both should contain the same keys; this test simply verifies the call doesn't throw.
        Expect.isTrue (yaml4.Contains("name: a")) "name present with 4-space indent"
        Expect.isTrue (yaml2.Contains("name: a")) "name present with 2-space indent"

    testCase "missing name field" <| fun _ ->
        // When the YAML has no 'name' field the decoder defaults to empty string.
        let yaml = "type: DefinedTerm\nTAN: GO:0001\n"
        let dt   = Yaml.DefinedTerm.fromYamlString true yaml
        Expect.equal dt.Name "" "name defaults to empty string"

    testList "Special Characters" [
        
        testCase "contains colon+space" <| fun _ ->
            let dt   = DefinedTerm(name = "cell: growth")
            let yaml = Yaml.DefinedTerm.toYamlString None dt
            let dt' = Yaml.DefinedTerm.fromYamlString true yaml
            Expect.equal dt.Name dt'.Name "name with colon round-trip"

        testCase "contains hashtag" <| fun _ ->
            let dt   = DefinedTerm(name = "cell#growth")
            let yaml = Yaml.DefinedTerm.toYamlString None dt
            let dt' = Yaml.DefinedTerm.fromYamlString true yaml
            Expect.equal dt.Name dt'.Name "name with hashtag round-trip"

        testCase "starts with asterisk" <| fun _ ->
            let dt   = DefinedTerm(name = "*cell growth")
            let yaml = Yaml.DefinedTerm.toYamlString None dt
            let dt' = Yaml.DefinedTerm.fromYamlString true yaml
            Expect.equal dt.Name dt'.Name "name starts with asterisk"
    ]
]
