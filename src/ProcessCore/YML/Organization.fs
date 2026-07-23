namespace ProcessCore.Yaml

open YAMLicious.YAMLiciousTypes
open ProcessCore
open Helpers

module Organization =

    let private knownFields =
        Set.ofList [ "id"; "type"; "name"; "url" ]

    let private knownPropertyNames =
        Set.ofList [ "id"; "type"; "name"; "url" ]

    let decoder (processCoreOnly: bool) (value: YAMLElement) : Organization =
        checkType processCoreOnly "Organization" value
        let id = tryGetField "id" value |> Option.map decodeString
        let name =
            tryGetField "name" value
            |> Option.map decodeString
            |> Option.defaultValue ""
        let url = tryGetField "url" value |> Option.map decodeString
        let org = Organization(name, ?id = id, ?url = url)
        applyOverflow "Organization" processCoreOnly knownFields org value
        org

    let encoder (org: Organization) : YAMLElement =
        [
            yield "type", yamlValue "Organization"
            match org.Id with
            | Some id -> yield "id", yamlValue id
            | None -> ()
            yield "name", yamlValue org.Name
            match org.Url with
            | Some url -> yield "url", yamlValue url
            | None -> ()
            yield! emitOverflow knownPropertyNames org
        ]
        |> yamlMap

    let fromYamlString (processCoreOnly: bool) (s: string) : Organization =
        YAMLicious.Reader.read s |> decoder processCoreOnly

    let toYamlString (whitespace: int option) (org: Organization) : string =
        writeYaml whitespace (encoder org)

    let registerOverflowType () =
        Helpers.registerKnownTypeTyped "Organization" decoder (fun value ->
            match value with
            | :? Organization as typed -> Some (encoder typed)
            | _ -> None)

    do registerOverflowType ()
