module ProcessCore.Yaml.Tests.Codecs.Administrative

open Fable.Pyxpecto
open ProcessCore
open ProcessCore.Yaml

let tests = testList "Administrative" [

    testCase "Organization roundtrip" <| fun _ ->
        let original = Organization("Example Lab", id = "org:lab", url = "https://example.org")
        let yaml = Yaml.Organization.toYamlString None original
        let decoded = Yaml.Organization.fromYamlString true yaml

        Expect.equal decoded.Id original.Id "id should roundtrip"
        Expect.equal decoded.Name original.Name "name should roundtrip"
        Expect.equal decoded.Url original.Url "url should roundtrip"

    testCase "Agent roundtrip with affiliation and job title" <| fun _ ->
        let original =
            Agent(
                "Ada",
                familyName = "Lovelace",
                email = "ada@example.org",
                affiliation = Organization("Example Lab"),
                identifier = "ORCID:0000-0000-0000-0001",
                jobTitle = DefinedTerm("data steward"))

        let yaml = Yaml.Agent.toYamlString None original
        let decoded = Yaml.Agent.fromYamlString true yaml

        Expect.equal decoded.GivenName original.GivenName "givenName should roundtrip"
        Expect.equal decoded.FamilyName original.FamilyName "familyName should roundtrip"
        Expect.equal decoded.Affiliation.Value.Name "Example Lab" "affiliation should roundtrip"
        Expect.equal decoded.JobTitle.Value.Name "data steward" "jobTitle should roundtrip"

    testCase "ScholarlyArticle roundtrip with author" <| fun _ ->
        let original =
            ScholarlyArticle(
                "Example methods",
                identifier = "doi:10.0000/example",
                creativeWorkStatus = DefinedTerm("published"),
                authors = [ Agent("Ada", familyName = "Lovelace") ])

        let yaml = Yaml.ScholarlyArticle.toYamlString None original
        let decoded = Yaml.ScholarlyArticle.fromYamlString true yaml

        Expect.equal decoded.Headline original.Headline "headline should roundtrip"
        Expect.equal decoded.Identifier original.Identifier "identifier should roundtrip"
        Expect.equal decoded.Authors.Count 1 "author should roundtrip"
]

