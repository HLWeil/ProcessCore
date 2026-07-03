namespace ProcessCore.Yaml

open YAMLicious.YAMLiciousTypes
open ProcessCore
open Helpers

module Agent =

    let private knownFields =
        Set.ofList
            [ "id"; "type"; "givenName"; "familyName"; "email"; "affiliation"; "identifier"
              "jobTitles"; "additionalName"; "address"; "telephone"; "additionalProperty" ]

    let private knownPropertyNames =
        Set.ofList
            [ "id"; "type"; "givenname"; "familyname"; "email"; "affiliation"; "identifier"
              "jobtitles"; "additionalname"; "address"; "telephone"; "additionalproperty" ]

    let decoder (processCoreOnly: bool) (value: YAMLElement) : Agent =
        checkType processCoreOnly "Agent" value
        let id = tryGetField "id" value |> Option.map decodeString
        let givenName =
            tryGetField "givenName" value
            |> Option.map decodeString
            |> Option.defaultValue ""
        let familyName = tryGetField "familyName" value |> Option.map decodeString
        let email = tryGetField "email" value |> Option.map decodeString
        let affiliation =
            tryGetField "affiliation" value
            |> Option.bind (fun v ->
                match decodeRefOrInline (Organization.decoder processCoreOnly) v with
                | Choice2Of2 org -> Some org
                | Choice1Of2 _ -> None)
        let identifier = tryGetField "identifier" value |> Option.map decodeString
        let jobTitles =
            tryGetField "jobTitles" value
            |> Option.map (fun v ->
                match tryDecodeSequence v with
                | Some elems ->
                    elems
                    |> Seq.choose (fun elem ->
                        match decodeRefOrInline (DefinedTerm.decoder processCoreOnly) elem with
                        | Choice2Of2 dt -> Some dt
                        | Choice1Of2 _ -> None
                    )
                | None -> Seq.empty
            )

        let additionalName = tryGetField "additionalName" value |> Option.map decodeString
        let address = tryGetField "address" value |> Option.map decodeString
        let telephone = tryGetField "telephone" value |> Option.map decodeString

        let agent =
            Agent(
                givenName,
                ?id = id,
                ?familyName = familyName,
                ?email = email,
                ?affiliation = affiliation,
                ?identifier = identifier,
                ?jobTitles = jobTitles,
                ?additionalName = additionalName,
                ?address = address,
                ?telephone = telephone)

        tryGetField "additionalProperty" value
        |> Option.iter (fun v ->
            iterSequenceOrSingleton (fun elem ->
                match decodeRefOrInline (Annotation.decoder processCoreOnly) elem with
                | Choice2Of2 pv -> agent.AddAdditionalProperty(pv)
                | Choice1Of2 _ -> ()) v)

        applyOverflow "Agent" processCoreOnly knownFields agent value
        agent

    let encoder (agent: Agent) : YAMLElement =
        [
            yield "type", yamlValue "Agent"
            match agent.Id with
            | Some id -> yield "id", yamlValue id
            | None -> ()
            yield "givenName", yamlValue agent.GivenName
            match agent.FamilyName with
            | Some v -> yield "familyName", yamlValue v
            | None -> ()
            match agent.Email with
            | Some v -> yield "email", yamlValue v
            | None -> ()
            match agent.Affiliation with
            | Some org -> yield "affiliation", Organization.encoder org
            | None -> ()
            match agent.Identifier with
            | Some v -> yield "identifier", yamlValue v
            | None -> ()
            if agent.JobTitles.Count > 0 then 
                yield "jobTitles", yamlSeq (Seq.map DefinedTerm.encoder agent.JobTitles |> Seq.toList)
            match agent.AdditionalName with
            | Some v -> yield "additionalName", yamlValue v
            | None -> ()
            match agent.Address with
            | Some v -> yield "address", yamlValue v
            | None -> ()
            match agent.Telephone with
            | Some v -> yield "telephone", yamlValue v
            | None -> ()
            if agent.AdditionalProperty.Count > 0 then
                yield "additionalProperty",
                      agent.AdditionalProperty
                      |> Seq.map Annotation.encoder
                      |> Seq.toList
                      |> yamlSeq
            yield! emitOverflow knownPropertyNames agent
        ]
        |> yamlMap

    let fromYamlString (processCoreOnly: bool) (s: string) : Agent =
        YAMLicious.Reader.read s |> decoder processCoreOnly

    let toYamlString (whitespace: int option) (agent: Agent) : string =
        writeYaml whitespace (encoder agent)

