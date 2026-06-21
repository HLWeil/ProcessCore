namespace ProcessCore.Yaml

open YAMLicious.YAMLiciousTypes
open ProcessCore
open Helpers

module Annotation =

    let genID (pv: Annotation) =
        match pv.TryGetPropertyValue("@id") with
        | Some (:? string as id) -> id
        | _ ->
            let prefix = pv.AdditionalType |> Option.defaultValue "Annotation" |> makeIdSlug
            let name   = makeIdSlug pv.Name
            let parts  = [
                yield prefix
                yield name
                match pv.Value with Some v when v <> "" -> yield makeIdSlug v | _ -> ()
                match pv.Unit  with Some u when u <> "" -> yield makeIdSlug u | _ -> ()
            ]
            "#" + String.concat "_" parts

    let private knownFields =
        Set.ofList
            [ "id"; "type"; "additionalType"; "name"; "value"; "unit"
              "nameTAN"; "valueTAN"; "unitTAN"; "instanceOf"; "nameText"; "valueText"; "unitText"; "valueWithUnitText" ]

    let private knownPropertyNames =
        Set.ofList
            [ "id"; "type"; "additionaltype"; "name"; "value"; "unit"
              "nametan"; "valuetan"; "unittan"; "instanceof"; "nametext"; "valuetext"; "unittext"; "valuewithunittext"]

    let decoder (processCoreOnly: bool) (value: YAMLElement) : Annotation =
        checkType processCoreOnly "Annotation" value
        let name           = tryGetField "name"           value |> Option.map decodeString |> Option.defaultValue ""
        let pvValue        = tryGetField "value"          value |> Option.map decodeString
        let unit           = tryGetField "unit"           value |> Option.map decodeString
        let nameTAN        = tryGetField "nameTAN"        value |> Option.map decodeString
        let valueTAN       = tryGetField "valueTAN"       value |> Option.map decodeString
        let unitTAN        = tryGetField "unitTAN"        value |> Option.map decodeString
        let additionalType = tryGetField "additionalType" value |> Option.map decodeString
        let instanceOf =
            tryGetField "instanceOf" value
            |> Option.bind (fun v ->
                match decodeRefOrInline (FormalParameter.decoder processCoreOnly) v with
                | Choice2Of2 fp -> Some fp
                | Choice1Of2 _  -> None)   // id references left unresolved

        let pv =
            Annotation(
                name,
                ?value          = pvValue,
                ?unit           = unit,
                ?nameTAN        = nameTAN,
                ?valueTAN       = valueTAN,
                ?unitTAN        = unitTAN,
                ?additionalType = additionalType,
                ?instanceOf     = instanceOf)
        applyOverflow "Annotation" processCoreOnly knownFields pv value
        pv

    let encoder (pv: Annotation) : YAMLElement =
        [
            yield "type", yamlValue "Annotation"
            yield "name", yamlValue pv.Name
            match pv.AdditionalType with
            | Some at -> yield "additionalType", yamlValue at
            | None    -> ()
            match pv.Value with
            | Some v -> yield "value", yamlValue v
            | None   -> ()
            match pv.Unit with
            | Some u -> yield "unit", yamlValue u
            | None   -> ()
            match pv.NameTAN with
            | Some t -> yield "nameTAN", yamlValue t
            | None   -> ()
            match pv.ValueTAN with
            | Some t -> yield "valueTAN", yamlValue t
            | None   -> ()
            match pv.UnitTAN with
            | Some t -> yield "unitTAN", yamlValue t
            | None   -> ()
            match pv.InstanceOf with
            | Some fp -> yield "instanceOf", FormalParameter.encoder fp
            | None    -> ()
            yield! emitOverflow knownPropertyNames pv
        ]
        |> yamlMap



    let fromYamlString (processCoreOnly: bool) (s: string) : Annotation =
        YAMLicious.Reader.read s |> decoder processCoreOnly

    let toYamlString (whitespace: int option) (pv: Annotation) : string =
        writeYaml whitespace (encoder pv)
