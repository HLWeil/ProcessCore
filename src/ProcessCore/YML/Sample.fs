namespace ProcessCore.Yaml

open YAMLicious.YAMLiciousTypes
open ProcessCore
open Helpers

module Sample =

    let private knownFields =
        Set.ofList [ "id"; "type"; "additionalType"; "name"; "additionalProperty" ]

    let private knownPropertyNames =
        Set.ofList [ "id"; "type"; "additionaltype"; "name"; "additionalproperty"; "inputof"; "outputof" ]

    let decoderWithPropertyResolver (processCoreOnly: bool) (resolveAnnotation: string -> Annotation option) (value: YAMLElement) : Sample =
        checkType processCoreOnly "Sample" value
        let name           = tryGetField "name"           value |> Option.map decodeString |> Option.defaultValue ""
        let additionalType = tryGetField "additionalType" value |> Option.map decodeString

        let m = Sample(name, ?additionalType = additionalType)

        tryGetField "additionalProperty" value
        |> Option.iter (fun v ->
            iterSequenceOrSingleton (fun elem ->
                match decodeRefOrInline (Annotation.decoder processCoreOnly) elem with
                | Choice2Of2 pv -> m.AddAdditionalProperty(pv)
                | Choice1Of2 id -> resolveAnnotation id |> Option.iter m.AddAdditionalProperty) v)

        applyOverflow "Sample" processCoreOnly knownFields m value
        m

    let decoder (processCoreOnly: bool) (value: YAMLElement) : Sample =
        decoderWithPropertyResolver processCoreOnly (fun _ -> None) value

    let encoder (pvEncoder : Annotation -> YAMLElement) (m: Sample) : YAMLElement =
        [
            yield "type", yamlValue "Sample"
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

    let fromYamlString (processCoreOnly: bool) (s: string) : Sample =
        YAMLicious.Reader.read s |> decoder processCoreOnly

    let toYamlString (whitespace: int option) (m: Sample) : string =
        writeYaml whitespace (encoder Annotation.encoder m)
