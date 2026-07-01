module ProcessCore.Tests.Types.DefinedTerm

open Fable.Pyxpecto
open ProcessCore

let tests = testList "DefinedTerm" [

    testCase "construction with name" <| fun _ ->
        let dt = DefinedTerm("cell growth")
        Expect.equal dt.Name "cell growth" "Name should be set"
        Expect.isNone dt.TAN             "TAN should be None"
        Expect.isNone dt.InDefinedTermSet "InDefinedTermSet should be None"

    testCase "equality all fields match" <| fun _ ->
        let dt1 = DefinedTerm("cell growth", tan = "http://purl.obolibrary.org/obo/GO_0016049", inDefinedTermSet = "http://purl.obolibrary.org/obo/go.owl")
        let dt2 = DefinedTerm("cell growth", tan = "http://purl.obolibrary.org/obo/GO_0016049", inDefinedTermSet = "http://purl.obolibrary.org/obo/go.owl")
        Expect.equal dt1 dt2 "Matching Name + TAN + InDefinedTermSet → equal"

    testCase "inequality missing TAN" <| fun _ ->
        let dt1 = DefinedTerm("cell growth", tan = "http://purl.obolibrary.org/obo/GO_0016049")
        let dt2 = DefinedTerm("cell growth")
        Expect.notEqual dt1 dt2 "One with TAN, one without → not equal"

    testCase "SemanticallyEquals prefers matching TANs" <| fun _ ->
        let left = DefinedTerm("LFQ intensity", tan = "http://purl.obolibrary.org/obo/MS_1001902")
        let right = DefinedTerm("label-free quantification intensity", tan = "http://purl.obolibrary.org/obo/MS_1001902")
        Expect.isTrue (left.SemanticallyEquals(right)) "matching TANs should be semantic equals"

    testCase "SemanticallyEquals falls back to exact term equality" <| fun _ ->
        let left = DefinedTerm("protein abundance")
        let same = DefinedTerm("protein abundance")
        let different = DefinedTerm("protein intensity")
        Expect.isTrue (left.SemanticallyEquals(same)) "same term without TAN should match"
        Expect.isFalse (left.SemanticallyEquals(different)) "different term without TAN should not match"

]
