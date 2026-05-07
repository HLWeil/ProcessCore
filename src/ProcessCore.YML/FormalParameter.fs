namespace ProcessCore.Yaml

open YAMLicious.YAMLiciousTypes
open ProcessCore
open Helpers

module FormalParameter =

    let private knownFields =
        Set.ofList [ "id"; "type"; "name"; "nameTAN"; "defaultValue" ]

    let private knownPropertyNames =
        Set.ofList [ "id"; "type"; "name"; "nametan"; "defaultvalue" ]

    let decoder (processCoreOnly: bool) (value: YAMLElement) : FormalParameter =
        checkType processCoreOnly "FormalParameter" value
        let name    = tryGetField "name"    value |> Option.map decodeString |> Option.defaultValue ""
        let nameTAN = tryGetField "nameTAN" value |> Option.map decodeString
        let defaultValue =
            tryGetField "defaultValue" value
            |> Option.bind (fun v ->
                match decodeRefOrInline (DefinedTerm.decoder processCoreOnly) v with
                | Choice2Of2 dt -> Some dt
                | Choice1Of2 _  -> None)   // id references left unresolved

        let fp = FormalParameter(name, ?nameTAN = nameTAN, ?defaultValue = defaultValue)
        applyOverflow "FormalParameter" processCoreOnly knownFields fp value
        fp

    let encoder (fp: FormalParameter) : YAMLElement =
        [
            yield "id",   yamlValue fp.Name
            yield "type", yamlValue "FormalParameter"
            yield "name", yamlValue fp.Name
            match fp.NameTAN with
            | Some tan -> yield "nameTAN", yamlValue tan
            | None     -> ()
            match fp.DefaultValue with
            | Some dt -> yield "defaultValue", DefinedTerm.encoder dt
            | None    -> ()
            yield! emitOverflow knownPropertyNames fp
        ]
        |> yamlMap

    let fromYamlString (processCoreOnly: bool) (s: string) : FormalParameter =
        YAMLicious.Reader.read s |> decoder processCoreOnly

    let toYamlString (whitespace: int option) (fp: FormalParameter) : string =
        writeYaml whitespace (encoder fp)
