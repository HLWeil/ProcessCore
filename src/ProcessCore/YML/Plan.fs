namespace ProcessCore.Yaml

open YAMLicious.YAMLiciousTypes
open ProcessCore
open Helpers

module Plan =

    let private knownFields =
        Set.ofList
            [ "id"; "type"; "additionalType"; "name"; "description"; "version"
              "url"; "intendedUse"; "parameters"; "labEquipment"; "additionalProperty" ]

    let private knownPropertyNames =
        Set.ofList
            [ "id"; "type"; "additionaltype"; "name"; "description"; "version"
              "url"; "intendeduse"; "parameters"; "labequipment"; "additionalproperty" ]

    let genID (proto: Plan) : string =
        match proto.TryGetPropertyValue("@id") with
        | Some (:? string as id) -> id
        | _ ->
            match proto.Url with
            | Some url -> url
            | None ->
                let name = proto.Name |> Option.map makeIdSlug |> Option.defaultValue "unnamed"
                "#Protocol_" + name


    let decoderWithPropertyResolver (processCoreOnly: bool) (resolveAnnotation: string -> Annotation option) (value: YAMLElement) : Plan =
        checkType processCoreOnly "Plan" value
        let name           = tryGetField "name"           value |> Option.map decodeString
        let description    = tryGetField "description"    value |> Option.map decodeString
        let version        = tryGetField "version"        value |> Option.map decodeString
        let url            = tryGetField "url"            value |> Option.map decodeString
        let additionalType = tryGetField "additionalType" value |> Option.map decodeString
        let intendedUse =
            tryGetField "intendedUse" value
            |> Option.bind (fun v ->
                match decodeRefOrInline (DefinedTerm.decoder processCoreOnly) v with
                | Choice2Of2 dt -> Some dt
                | Choice1Of2 _  -> None)

        let proto =
            Plan(
                ?name           = name,
                ?description    = description,
                ?version        = version,
                ?url            = url,
                ?additionalType = additionalType,
                ?intendedUse    = intendedUse)

        let decodeSeq fieldName (decoder: YAMLElement -> 'a) (resolve: string -> 'a option) (add: 'a -> unit) =
            tryGetField fieldName value
            |> Option.iter (fun v ->
                iterSequenceOrSingleton (fun elem ->
                    match decodeRefOrInline decoder elem with
                    | Choice2Of2 x -> add x
                    | Choice1Of2 id -> resolve id |> Option.iter add) v)

        decodeSeq "parameters"         (FormalParameter.decoder processCoreOnly) (fun _ -> None) proto.AddParameter
        decodeSeq "labEquipment"       (Annotation.decoder processCoreOnly) resolveAnnotation proto.AddLabEquipment
        decodeSeq "labEquipments"      (Annotation.decoder processCoreOnly) resolveAnnotation proto.AddLabEquipment
        decodeSeq "additionalProperty" (Annotation.decoder processCoreOnly) resolveAnnotation proto.AddAdditionalProperty

        applyOverflow "Plan" processCoreOnly knownFields proto value
        proto

    let decoder (processCoreOnly: bool) (value: YAMLElement) : Plan =
        decoderWithPropertyResolver processCoreOnly (fun _ -> None) value

    let encoder (pvEncoder : Annotation -> YAMLElement) (proto: Plan) : YAMLElement =
        [
            yield "type", yamlValue "Plan"
            match proto.AdditionalType with
            | Some at -> yield "additionalType", yamlValue at
            | None    -> ()
            match proto.Name with
            | Some n -> yield "name", yamlValue n
            | None   -> ()
            match proto.Description with
            | Some d -> yield "description", yamlValue d
            | None   -> ()
            match proto.Version with
            | Some v -> yield "version", yamlValue v
            | None   -> ()
            match proto.Url with
            | Some u -> yield "url", yamlValue u
            | None   -> ()
            match proto.IntendedUse with
            | Some dt -> yield "intendedUse", DefinedTerm.encoder dt
            | None    -> ()
            if proto.Parameters.Count > 0 then
                yield "parameters",
                      proto.Parameters
                      |> Seq.map FormalParameter.encoder
                      |> Seq.toList
                      |> yamlSeq
            if proto.LabEquipment.Count > 0 then
                yield "labEquipment",
                      proto.LabEquipment
                      |> Seq.map pvEncoder
                      |> Seq.toList
                      |> yamlSeq
            if proto.AdditionalProperty.Count > 0 then
                yield "additionalProperty",
                      proto.AdditionalProperty
                      |> Seq.map pvEncoder
                      |> Seq.toList
                      |> yamlSeq
            yield! emitOverflow knownPropertyNames proto
        ]
        |> yamlMap

    let fromYamlString (processCoreOnly: bool) (s: string) : Plan =
        YAMLicious.Reader.read s |> decoder processCoreOnly

    let toYamlString (whitespace: int option) (proto: Plan) : string =
        writeYaml whitespace (encoder Annotation.encoder proto)
