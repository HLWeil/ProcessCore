namespace ProcessCore.Yaml

open YAMLicious.YAMLiciousTypes
open ProcessCore
open Helpers

module LabProtocol =

    let private knownFields =
        Set.ofList
            [ "id"; "type"; "additionalType"; "name"; "description"; "version"
              "url"; "intendedUse"; "parameters"; "labEquipment"; "additionalProperty" ]

    let private knownPropertyNames =
        Set.ofList
            [ "id"; "type"; "additionaltype"; "name"; "description"; "version"
              "url"; "intendeduse"; "parameters"; "labequipment"; "additionalproperty" ]

    let decoderWithPropertyResolver (processCoreOnly: bool) (resolvePropertyValue: string -> PropertyValue option) (value: YAMLElement) : LabProtocol =
        checkType processCoreOnly "LabProtocol" value
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
            LabProtocol(
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
        decodeSeq "labEquipment"       (PropertyValue.decoder processCoreOnly) resolvePropertyValue proto.AddLabEquipment
        decodeSeq "labEquipments"      (PropertyValue.decoder processCoreOnly) resolvePropertyValue proto.AddLabEquipment
        decodeSeq "additionalProperty" (PropertyValue.decoder processCoreOnly) resolvePropertyValue proto.AddAdditionalProperty

        applyOverflow "LabProtocol" processCoreOnly knownFields proto value
        proto

    let decoder (processCoreOnly: bool) (value: YAMLElement) : LabProtocol =
        decoderWithPropertyResolver processCoreOnly (fun _ -> None) value

    let encoder (proto: LabProtocol) : YAMLElement =
        let id =
            match proto.Url with
            | Some url  -> url
            | None      -> proto.Name |> Option.defaultValue ""
        [
            yield "id",   yamlValue id
            yield "type", yamlValue "LabProtocol"
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
                      |> Seq.map PropertyValue.encoder
                      |> Seq.toList
                      |> yamlSeq
            if proto.AdditionalProperty.Count > 0 then
                yield "additionalProperty",
                      proto.AdditionalProperty
                      |> Seq.map PropertyValue.encoder
                      |> Seq.toList
                      |> yamlSeq
            yield! emitOverflow knownPropertyNames proto
        ]
        |> yamlMap

    let fromYamlString (processCoreOnly: bool) (s: string) : LabProtocol =
        YAMLicious.Reader.read s |> decoder processCoreOnly

    let toYamlString (whitespace: int option) (proto: LabProtocol) : string =
        writeYaml whitespace (encoder proto)
