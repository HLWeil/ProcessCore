namespace ProcessCore.Yaml

open YAMLicious.YAMLiciousTypes
open ProcessCore
open Helpers

module Data =

    let private knownFields =
        Set.ofList
            [ "type"; "additionalType"; "path"; "selector"
              "selectorFormat"; "usageInfo"; "encodingFormat"; "hasPart"; "additionalProperty" ; "name" ]

    let private knownPropertyNames =
        Set.ofList
            [ "type"; "additionaltype"; "path"; "selector"
              "selectorformat"; "usageinfo"; "encodingformat"; "haspart"; "additionalproperty"
              "inputof"; "outputof"; "name" ]

    let rec decoderWithPropertyResolver (processCoreOnly: bool) (resolveAnnotation: string -> Annotation option) (value: YAMLElement) : Data =
        checkType processCoreOnly "Data" value
        let path =
            tryGetField "path" value |> Option.bind tryDecodeString
            |> Option.defaultWith (fun () -> failwith "Data YAML object is missing required 'path' field.")

        let selector        = tryGetField "selector"        value |> Option.map decodeString
        let selectorFormat  =
            tryGetField "selectorFormat" value
            |> Option.orElse (tryGetField "usageInfo" value)
            |> Option.map decodeString
        let encodingFormat  = tryGetField "encodingFormat"  value |> Option.map decodeString
        let additionalType  = tryGetField "additionalType"  value |> Option.map decodeString
        let d = Data(path, ?selector = selector, ?selectorFormat = selectorFormat,
                     ?encodingFormat = encodingFormat, ?additionalType = additionalType)

        tryGetField "hasPart" value
        |> Option.iter (fun v ->
            iterSequenceOrSingleton (fun elem ->
                match decodeRefOrInline (decoderWithPropertyResolver processCoreOnly resolveAnnotation) elem with
                | Choice2Of2 child -> d.AddPart(child)
                | Choice1Of2 _ -> ()) v)

        tryGetField "additionalProperty" value
        |> Option.iter (fun v ->
            iterSequenceOrSingleton (fun elem ->
                match decodeRefOrInline (Annotation.decoder processCoreOnly) elem with
                | Choice2Of2 pv -> d.AddAdditionalProperty(pv)
                | Choice1Of2 id -> resolveAnnotation id |> Option.iter d.AddAdditionalProperty) v)

        applyOverflow "Data" processCoreOnly knownFields d value
        d

    let decoder (processCoreOnly: bool) (value: YAMLElement) : Data =
        decoderWithPropertyResolver processCoreOnly (fun _ -> None) value

    let rec encoder pvEncoder (d: Data) : YAMLElement =
        //let id =
        //    match d.Selector with
        //    | Some sel -> d.Path + "#" + sel
        //    | None     -> d.Path
        [
            yield "type", yamlValue "Data"
            yield "path", yamlValue d.Path
            match d.AdditionalType with
            | Some at -> yield "additionalType", yamlValue at
            | None    -> ()
            match d.Selector with
            | Some s -> yield "selector", yamlValue s
            | None   -> ()
            match d.SelectorFormat with
            | Some s -> yield "selectorFormat", yamlValue s
            | None   -> ()
            match d.EncodingFormat with
            | Some f -> yield "encodingFormat", yamlValue f
            | None   -> ()
            if d.HasPart.Count > 0 then
                yield "hasPart",
                      d.HasPart
                      |> Seq.map (encoder pvEncoder)
                      |> Seq.toList
                      |> yamlSeq
            if d.AdditionalProperty.Count > 0 then
                yield "additionalProperty",
                      d.AdditionalProperty
                      |> Seq.map pvEncoder
                      |> Seq.toList
                      |> yamlSeq
            yield! emitOverflow knownPropertyNames d
        ]
        |> yamlMap

    let fromYamlString (processCoreOnly: bool) (s: string) : Data =
        YAMLicious.Reader.read s |> decoder processCoreOnly

    let toYamlString (whitespace: int option) (d: Data) : string =
        writeYaml whitespace (encoder Annotation.encoder d)
