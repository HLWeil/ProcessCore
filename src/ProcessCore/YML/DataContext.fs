namespace ProcessCore.Yaml

open YAMLicious.YAMLiciousTypes
open ProcessCore
open Helpers

module DataContext =

    let private knownFields =
        Set.ofList
            [ "id"; "type"; "data"; "explication"; "explicationTAN"; "objectType"
              "objectTypeTAN"; "unit"; "unitTAN"; "label"; "description"; "generatedBy" ]

    let private knownPropertyNames =
        Set.ofList
            [ "id"; "type"; "data"; "explication"; "explicationtan"; "objecttype"
              "objecttypetan"; "unit"; "unittan"; "label"; "description"; "generatedby" ]

    let private decodeDefinedTermField (processCoreOnly: bool) (nameField: string) (tanField: string) (value: YAMLElement) =
        match tryGetField nameField value with
        | Some fieldValue ->
            match decodeRefOrInline (DefinedTerm.decoder processCoreOnly) fieldValue with
            | Choice2Of2 term when term.Name <> "" -> Some term
            | Choice1Of2 id -> Some (DefinedTerm(id, ?tan = (tryGetField tanField value |> Option.map decodeString)))
            | _ ->
                tryDecodeString fieldValue
                |> Option.map (fun name -> DefinedTerm(name, ?tan = (tryGetField tanField value |> Option.map decodeString)))
        | None -> None

    let private emitDefinedTermFields nameField tanField (term: DefinedTerm option) =
        [
            match term with
            | Some dt ->
                yield nameField, yamlValue dt.Name
                match dt.TAN with
                | Some tan -> yield tanField, yamlValue tan
                | None -> ()
            | None -> ()
        ]

    let decoder (processCoreOnly: bool) (value: YAMLElement) : DataContext =
        checkType processCoreOnly "DataContext" value
        let data =
            tryGetField "data" value
            |> Option.map (Data.decoder processCoreOnly)
            |> Option.defaultWith (fun () -> failwith "DataContext YAML object is missing required 'data' field.")

        let dataContext =
            DataContext(
                data,
                ?explication = decodeDefinedTermField processCoreOnly "explication" "explicationTAN" value,
                ?objectType = decodeDefinedTermField processCoreOnly "objectType" "objectTypeTAN" value,
                ?unit = decodeDefinedTermField processCoreOnly "unit" "unitTAN" value,
                ?label = (tryGetField "label" value |> Option.map decodeString),
                ?description = (tryGetField "description" value |> Option.map decodeString),
                ?generatedBy = (tryGetField "generatedBy" value |> Option.map decodeString))

        applyOverflow "DataContext" processCoreOnly knownFields dataContext value
        dataContext

    let encoder (dataContext: DataContext) : YAMLElement =
        [
            yield "type", yamlValue "DataContext"
            yield "data", Data.encoder Annotation.encoder dataContext.Data
            yield! emitDefinedTermFields "explication" "explicationTAN" dataContext.Explication
            yield! emitDefinedTermFields "objectType" "objectTypeTAN" dataContext.ObjectType
            yield! emitDefinedTermFields "unit" "unitTAN" dataContext.Unit
            match dataContext.Label with
            | Some v -> yield "label", yamlValue v
            | None -> ()
            match dataContext.Description with
            | Some v -> yield "description", yamlValue v
            | None -> ()
            match dataContext.GeneratedBy with
            | Some v -> yield "generatedBy", yamlValue v
            | None -> ()
            yield! emitOverflow knownPropertyNames dataContext
        ]
        |> yamlMap

    let fromYamlString (processCoreOnly: bool) (s: string) : DataContext =
        YAMLicious.Reader.read s |> decoder processCoreOnly

    let toYamlString (whitespace: int option) (dataContext: DataContext) : string =
        writeYaml whitespace (encoder dataContext)

    let registerOverflowType () =
        Helpers.registerKnownTypeTyped "DataContext" decoder (fun value ->
            match value with
            | :? DataContext as typed -> Some (encoder typed)
            | _ -> None)

    do registerOverflowType ()
