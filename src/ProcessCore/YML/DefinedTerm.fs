namespace ProcessCore.Yaml

open YAMLicious.YAMLiciousTypes
open ProcessCore
open Helpers

module DefinedTerm =

    let private knownFields =
        Set.ofList [ "id"; "type"; "name"; "TAN"; "inDefinedTermSet" ]

    let private knownPropertyNames =
        Set.ofList [ "id"; "type"; "name"; "tan"; "indefinedtermset" ]

    let decoder (processCoreOnly: bool) (value: YAMLElement) : DefinedTerm =
        checkType processCoreOnly "DefinedTerm" value
        let name = tryGetField "name" value |> Option.map decodeString |> Option.defaultValue ""
        let tan  = tryGetField "TAN"  value |> Option.map decodeString
        let inDefinedTermSet =
            tryGetField "inDefinedTermSet" value
            |> Option.map (fun v ->
                // Accept string or inline object with an "id" field
                match tryDecodeString v with
                | Some s -> s
                | None   -> tryGetField "id" v |> Option.map decodeString |> Option.defaultValue "")

        let dt = DefinedTerm(name, ?tan = tan, ?inDefinedTermSet = inDefinedTermSet)
        applyOverflow "DefinedTerm" processCoreOnly knownFields dt value
        dt

    let encoder (dt: DefinedTerm) : YAMLElement =
        [
            yield "type", yamlValue "DefinedTerm"
            yield "name", yamlValue dt.Name
            match dt.TAN with
            | Some tan -> yield "TAN", yamlValue tan
            | None     -> ()
            match dt.InDefinedTermSet with
            | Some ids -> yield "inDefinedTermSet", yamlValue ids
            | None     -> ()
            yield! emitOverflow knownPropertyNames dt
        ]
        |> yamlMap

    let fromYamlString (processCoreOnly: bool) (s: string) : DefinedTerm =
        YAMLicious.Reader.read s |> decoder processCoreOnly

    let toYamlString (whitespace: int option) (dt: DefinedTerm) : string =
        writeYaml whitespace (encoder dt)
