namespace ProcessCore.Yaml

open YAMLicious.YAMLiciousTypes
open ProcessCore
open Helpers

module ScholarlyArticle =

    let private knownFields =
        Set.ofList
            [ "id"; "type"; "headline"; "identifier"; "creativeWorkStatus"
              "authors"; "additionalProperty" ]

    let private knownPropertyNames =
        Set.ofList
            [ "id"; "type"; "headline"; "identifier"; "creativeworkstatus"
              "authors"; "additionalproperty" ]

    let decoder (processCoreOnly: bool) (value: YAMLElement) : ScholarlyArticle =
        checkType processCoreOnly "ScholarlyArticle" value
        let id = tryGetField "id" value |> Option.map decodeString
        let headline =
            tryGetField "headline" value
            |> Option.map decodeString
            |> Option.defaultValue ""
        let identifier = tryGetField "identifier" value |> Option.map decodeString
        let creativeWorkStatus =
            tryGetField "creativeWorkStatus" value
            |> Option.bind (fun v ->
                match decodeRefOrInline (DefinedTerm.decoder processCoreOnly) v with
                | Choice2Of2 dt -> Some dt
                | Choice1Of2 _ -> None)

        let article =
            ScholarlyArticle(
                headline,
                ?id = id,
                ?identifier = identifier,
                ?creativeWorkStatus = creativeWorkStatus)

        let decodeAgentField fieldName =
            tryGetField fieldName value
            |> Option.iter (fun v ->
                iterSequenceOrSingleton (fun elem ->
                    match decodeRefOrInline (Agent.decoder processCoreOnly) elem with
                    | Choice2Of2 agent -> article.AddAuthor(agent)
                    | Choice1Of2 _ -> ()) v)

        decodeAgentField "authors"

        tryGetField "additionalProperty" value
        |> Option.iter (fun v ->
            iterSequenceOrSingleton (fun elem ->
                match decodeRefOrInline (Annotation.decoder processCoreOnly) elem with
                | Choice2Of2 pv -> article.AddAdditionalProperty(pv)
                | Choice1Of2 _ -> ()) v)

        applyOverflow "ScholarlyArticle" processCoreOnly knownFields article value
        article

    let encoder (article: ScholarlyArticle) : YAMLElement =
        [
            yield "type", yamlValue "ScholarlyArticle"
            match article.Id with
            | Some id -> yield "id", yamlValue id
            | None -> ()
            yield "headline", yamlValue article.Headline
            match article.Identifier with
            | Some v -> yield "identifier", yamlValue v
            | None -> ()
            match article.CreativeWorkStatus with
            | Some v -> yield "creativeWorkStatus", DefinedTerm.encoder v
            | None -> ()
            if article.Authors.Count > 0 then
                yield "authors",
                      article.Authors
                      |> Seq.map Agent.encoder
                      |> Seq.toList
                      |> yamlSeq
            if article.AdditionalProperty.Count > 0 then
                yield "additionalProperty",
                      article.AdditionalProperty
                      |> Seq.map Annotation.encoder
                      |> Seq.toList
                      |> yamlSeq
            yield! emitOverflow knownPropertyNames article
        ]
        |> yamlMap

    let fromYamlString (processCoreOnly: bool) (s: string) : ScholarlyArticle =
        YAMLicious.Reader.read s |> decoder processCoreOnly

    let toYamlString (whitespace: int option) (article: ScholarlyArticle) : string =
        writeYaml whitespace (encoder article)

