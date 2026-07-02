module ProcessCore.Tests.Types.Administrative

open Fable.Pyxpecto
open ProcessCore

let tests = testList "Administrative" [

    testCase "Organization equality uses id when present" <| fun _ ->
        let a = Organization("Example Lab", id = "org:1")
        let b = Organization("Renamed Lab", id = "org:1")
        Expect.equal a b "same id should be equal"

    testCase "Agent carries affiliation, identifier, job title, and properties" <| fun _ ->
        let org = Organization("Example Lab")
        let role = DefinedTerm("data steward")
        let pv = Annotation("orcid", value = "0000-0000-0000-0001")
        let agent =
            Agent(
                "Ada",
                familyName = "Lovelace",
                email = "ada@example.org",
                affiliation = org,
                identifier = "ORCID:0000-0000-0000-0001",
                jobTitles = [ role ],
                additionalProperty = [ pv ])

        Expect.equal agent.Affiliation (Some org) "affiliation should be retained"
        Expect.equal agent.JobTitles.Count 1 "job titles should be retained"
        Expect.equal agent.Identifier (Some "ORCID:0000-0000-0000-0001") "identifier should be retained"
        Expect.equal agent.AdditionalProperty.Count 1 "additional property should be retained"

    testCase "ScholarlyArticle carries authors and status" <| fun _ ->
        let agent = Agent("Ada", familyName = "Lovelace")
        let status = DefinedTerm("published")
        let article =
            ScholarlyArticle(
                "A compact methods citation",
                identifier = "doi:10.0000/example",
                creativeWorkStatus = status,
                authors = [ agent ])

        Expect.equal article.Authors.Count 1 "author should be retained"
        Expect.equal article.CreativeWorkStatus (Some status) "status should be retained"
        Expect.equal article.Identifier (Some "doi:10.0000/example") "identifier should be retained"
]

