namespace ProcessCore.Yaml

open YAMLicious.YAMLiciousTypes
open ProcessCore
open Helpers

module Dataset =

    let private knownFields =
        Set.ofList
            [ "type"; "additionalType"; "identifier"; "name"; "description"
              "processes"; "hasPart"; "additionalProperty" ]

    let private knownPropertyNames =
        Set.ofList
            [ "type"; "additionaltype"; "identifier"; "name"; "description"
              "processes"; "haspart"; "additionalproperty"; "partof" ]

    let rec decoder (processCoreOnly: bool) (value: YAMLElement) : Dataset =
        checkType processCoreOnly "Dataset" value
        let identifier =
            tryGetField "identifier" value |> Option.bind tryDecodeString
            |> Option.defaultWith (fun () -> failwith "Dataset YAML object is missing required 'identifier' field.")

        let name           = tryGetField "name"           value |> Option.map decodeString
        let description    = tryGetField "description"    value |> Option.map decodeString
        let additionalType = tryGetField "additionalType" value |> Option.map decodeString

        let ds = Dataset(identifier, ?name = name, ?description = description, ?additionalType = additionalType)

        // processes
        tryGetField "processes" value
        |> Option.iter (fun v ->
            match tryDecodeSequence v with
            | Some elems ->
                for elem in elems do
                    match decodeRefOrInline (LabProcess.decoder processCoreOnly) elem with
                    | Choice2Of2 proc -> ds.AddProcess(proc)
                    | Choice1Of2 _    -> ()
            | None -> ())

        // hasPart — each item is either an inline Dataset or an inline Data; discriminate by 'type'
        tryGetField "hasPart" value
        |> Option.iter (fun v ->
            match tryDecodeSequence v with
            | Some elems ->
                for elem in elems do
                    match tryDecodeString elem with
                    | Some _ -> ()  // id reference — leave unresolved
                    | None ->
                        let typeStr = tryGetField "type" elem |> Option.map decodeString |> Option.defaultValue ""
                        if typeStr = "Dataset" || typeStr = "" then
                            // Try to decode as Dataset (nested); empty type defaults to Dataset
                            let child = decoder processCoreOnly elem
                            ds.AddPart(child)
                        // Data nodes in hasPart are not directly representable as Dataset children;
                        // callers should handle this via processes instead.
            | None -> ())

        // additionalProperty
        tryGetField "additionalProperty" value
        |> Option.iter (fun v ->
            match tryDecodeSequence v with
            | Some elems ->
                for elem in elems do
                    match decodeRefOrInline (PropertyValue.decoder processCoreOnly) elem with
                    | Choice2Of2 pv -> ds.AddAdditionalProperty(pv)
                    | Choice1Of2 _  -> ()
            | None -> ())

        applyOverflow knownFields ds value
        ds

    let rec encoder (ds: Dataset) : YAMLElement =
        [
            yield "id",         yamlValue ds.Identifier
            yield "type",       yamlValue "Dataset"
            yield "identifier", yamlValue ds.Identifier
            match ds.AdditionalType with
            | Some at -> yield "additionalType", yamlValue at
            | None    -> ()
            match ds.Name with
            | Some n -> yield "name", yamlValue n
            | None   -> ()
            match ds.Description with
            | Some d -> yield "description", yamlValue d
            | None   -> ()
            if ds.Processes.Count > 0 then
                yield "processes",
                      ds.Processes
                      |> Seq.map LabProcess.encoder
                      |> Seq.toList
                      |> yamlSeq
            if ds.HasPart.Count > 0 then
                yield "hasPart",
                      ds.HasPart
                      |> Seq.map encoder
                      |> Seq.toList
                      |> yamlSeq
            if ds.AdditionalProperty.Count > 0 then
                yield "additionalProperty",
                      ds.AdditionalProperty
                      |> Seq.map PropertyValue.encoder
                      |> Seq.toList
                      |> yamlSeq
            yield! emitOverflow knownPropertyNames ds
        ]
        |> yamlMap

    let fromYamlString (processCoreOnly : bool) (s: string) : Dataset =
        YAMLicious.Reader.read s |> decoder processCoreOnly

    let toYamlString (whitespace: int option) (ds: Dataset) : string =
        writeYaml whitespace (encoder ds)
