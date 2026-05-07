namespace ProcessCore.Yaml

open YAMLicious.YAMLiciousTypes
open ProcessCore
open Helpers

module Data =

    let private knownFields =
        Set.ofList
            [ "type"; "additionalType"; "path"; "selector"
              "selectorFormat"; "encodingFormat"; "additionalProperty" ]

    let private knownPropertyNames =
        Set.ofList
            [ "type"; "additionaltype"; "path"; "selector"
              "selectorformat"; "encodingformat"; "additionalproperty"
              "inputof"; "outputof" ]

    let decoder (processCoreOnly: bool) (value: YAMLElement) : Data =
        checkType processCoreOnly "Data" value
        let path =
            tryGetField "path" value |> Option.bind tryDecodeString
            |> Option.defaultWith (fun () -> failwith "Data YAML object is missing required 'path' field.")

        let selector        = tryGetField "selector"        value |> Option.map decodeString
        let selectorFormat  = tryGetField "selectorFormat"  value |> Option.map decodeString
        let encodingFormat  = tryGetField "encodingFormat"  value |> Option.map decodeString
        let additionalType  = tryGetField "additionalType"  value |> Option.map decodeString

        let d = Data(path, ?selector = selector, ?selectorFormat = selectorFormat,
                     ?encodingFormat = encodingFormat, ?additionalType = additionalType)

        tryGetField "additionalProperty" value
        |> Option.iter (fun v ->
            match tryDecodeSequence v with
            | Some elems ->
                for elem in elems do
                    match decodeRefOrInline (PropertyValue.decoder processCoreOnly) elem with
                    | Choice2Of2 pv -> d.AddAdditionalProperty(pv)
                    | Choice1Of2 _  -> ()
            | None -> ())

        applyOverflow "Data" processCoreOnly knownFields d value
        d

    let encoder (d: Data) : YAMLElement =
        //let id =
        //    match d.Selector with
        //    | Some sel -> d.Path + "#" + sel
        //    | None     -> d.Path
        [
            //yield "id",   yamlValue id
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
            if d.AdditionalProperty.Count > 0 then
                yield "additionalProperty",
                      d.AdditionalProperty
                      |> Seq.map PropertyValue.encoder
                      |> Seq.toList
                      |> yamlSeq
            yield! emitOverflow knownPropertyNames d
        ]
        |> yamlMap

    let fromYamlString (processCoreOnly: bool) (s: string) : Data =
        YAMLicious.Reader.read s |> decoder processCoreOnly

    let toYamlString (whitespace: int option) (d: Data) : string =
        writeYaml whitespace (encoder d)
