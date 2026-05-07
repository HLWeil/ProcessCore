namespace ProcessCore.Yaml

open YAMLicious.YAMLiciousTypes
open ProcessCore
open Helpers

module PropertyValue =

    let private knownFields =
        Set.ofList
            [ "id"; "type"; "additionalType"; "name"; "value"; "unit"
              "nameTAN"; "valueTAN"; "unitTAN"; "instanceOf" ]

    let private knownPropertyNames =
        Set.ofList
            [ "id"; "type"; "additionaltype"; "name"; "value"; "unit"
              "nametan"; "valuetan"; "unittan"; "instanceof" ]

    let decoder (processCoreOnly: bool) (value: YAMLElement) : PropertyValue =
        checkType processCoreOnly "PropertyValue" value
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
            PropertyValue(
                name,
                ?value          = pvValue,
                ?unit           = unit,
                ?nameTAN        = nameTAN,
                ?valueTAN       = valueTAN,
                ?unitTAN        = unitTAN,
                ?additionalType = additionalType,
                ?instanceOf     = instanceOf)
        applyOverflow knownFields pv value
        pv

    let encoder (pv: PropertyValue) : YAMLElement =
        [
            yield "id",   yamlValue (pv.NameTAN |> Option.defaultValue pv.Name)
            yield "type", yamlValue "PropertyValue"
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

    let fromYamlString (s: string) : PropertyValue =
        YAMLicious.Reader.read s |> decoder true

    let toYamlString (whitespace: int option) (pv: PropertyValue) : string =
        writeYaml whitespace (encoder pv)
