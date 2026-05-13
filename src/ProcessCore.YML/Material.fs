namespace ProcessCore.Yaml

open YAMLicious.YAMLiciousTypes
open ProcessCore
open Helpers

module Material =

    let private knownFields =
        Set.ofList [ "id"; "type"; "additionalType"; "name"; "additionalProperty" ]

    let private knownPropertyNames =
        Set.ofList [ "id"; "type"; "additionaltype"; "name"; "additionalproperty"; "inputof"; "outputof" ]

    let decoderWithPropertyResolver (processCoreOnly: bool) (resolvePropertyValue: string -> PropertyValue option) (value: YAMLElement) : Material =
        checkType processCoreOnly "Material" value
        let name           = tryGetField "name"           value |> Option.map decodeString |> Option.defaultValue ""
        let additionalType = tryGetField "additionalType" value |> Option.map decodeString

        let m = Material(name, ?additionalType = additionalType)

        tryGetField "additionalProperty" value
        |> Option.iter (fun v ->
            iterSequenceOrSingleton (fun elem ->
                match decodeRefOrInline (PropertyValue.decoder processCoreOnly) elem with
                | Choice2Of2 pv -> m.AddAdditionalProperty(pv)
                | Choice1Of2 id -> resolvePropertyValue id |> Option.iter m.AddAdditionalProperty) v)

        applyOverflow "Material" processCoreOnly knownFields m value
        m

    let decoder (processCoreOnly: bool) (value: YAMLElement) : Material =
        decoderWithPropertyResolver processCoreOnly (fun _ -> None) value

    let encoder (pvEncoder : PropertyValue -> YAMLElement) (m: Material) : YAMLElement =
        [
            yield "type", yamlValue "Material"
            yield "name", yamlValue m.Name
            match m.AdditionalType with
            | Some at -> yield "additionalType", yamlValue at
            | None    -> ()
            if m.AdditionalProperty.Count > 0 then
                yield "additionalProperty",
                      m.AdditionalProperty
                      |> Seq.map pvEncoder
                      |> Seq.toList
                      |> yamlSeq
            yield! emitOverflow knownPropertyNames m
        ]
        |> yamlMap

    let fromYamlString (processCoreOnly: bool) (s: string) : Material =
        YAMLicious.Reader.read s |> decoder processCoreOnly

    let toYamlString (whitespace: int option) (m: Material) : string =
        writeYaml whitespace (encoder PropertyValue.encoder m)
