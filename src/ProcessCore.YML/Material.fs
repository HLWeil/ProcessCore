namespace ProcessCore.Yaml

open YAMLicious.YAMLiciousTypes
open ProcessCore
open Helpers

module Material =

    let private knownFields =
        Set.ofList [ "id"; "type"; "additionalType"; "name"; "additionalProperty" ]

    let private knownPropertyNames =
        Set.ofList [ "id"; "type"; "additionaltype"; "name"; "additionalproperty"; "inputof"; "outputof" ]

    let decoder (processCoreOnly: bool) (value: YAMLElement) : Material =
        checkType processCoreOnly "Material" value
        let name           = tryGetField "name"           value |> Option.map decodeString |> Option.defaultValue ""
        let additionalType = tryGetField "additionalType" value |> Option.map decodeString

        let m = Material(name, ?additionalType = additionalType)

        tryGetField "additionalProperty" value
        |> Option.iter (fun v ->
            match tryDecodeSequence v with
            | Some elems ->
                for elem in elems do
                    match decodeRefOrInline (PropertyValue.decoder processCoreOnly) elem with
                    | Choice2Of2 pv -> m.AddAdditionalProperty(pv)
                    | Choice1Of2 _  -> ()   // id references left unresolved
            | None -> ())

        applyOverflow "Material" processCoreOnly knownFields m value
        m

    let encoder (m: Material) : YAMLElement =
        [
            yield "id",   yamlValue m.Name
            yield "type", yamlValue "Material"
            yield "name", yamlValue m.Name
            match m.AdditionalType with
            | Some at -> yield "additionalType", yamlValue at
            | None    -> ()
            if m.AdditionalProperty.Count > 0 then
                yield "additionalProperty",
                      m.AdditionalProperty
                      |> Seq.map PropertyValue.encoder
                      |> Seq.toList
                      |> yamlSeq
            yield! emitOverflow knownPropertyNames m
        ]
        |> yamlMap

    let fromYamlString (processCoreOnly: bool) (s: string) : Material =
        YAMLicious.Reader.read s |> decoder processCoreOnly

    let toYamlString (whitespace: int option) (m: Material) : string =
        writeYaml whitespace (encoder m)
