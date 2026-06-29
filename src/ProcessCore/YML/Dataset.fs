namespace ProcessCore.Yaml

open System
open System.Collections.Generic
open YAMLicious.YAMLiciousTypes
open ProcessCore
open Helpers

module Dataset =

    let knownFields =
        Set.ofList
            [ "type"; "additionalType"; "identifier"; "title"; "description"
              "processes"; "hasPart"; "additionalProperty" ]

    let knownPropertyNames =
        Set.ofList
            [ "type"; "additionaltype"; "identifier"; "title"; "description"
              "processes"; "haspart"; "additionalproperty"; "partof"
              "propertyvalues"; "labprotocols"
              // Fable-compiled read-only instance properties — must not be re-emitted as overflow
              "noderegistrydirect"; "fragmentselectorprovidersdirect" ]

    let addIndexedValues fieldName decode value =
        let registry = Dictionary<string, 'a>()
        tryGetField fieldName value
        |> Option.iter (fun values ->
            iterSequenceOrSingleton (fun elem ->
                match tryGetField "@id" elem |> Option.bind tryDecodeString with
                | Some id -> registry.[normalizeId id] <- decode elem
                | None    -> ()) values)
        registry

    let tryFind (registry: Dictionary<string, 'a>) id =
        match registry.TryGetValue(normalizeId id) with
        | true, value -> Some value
        | false, _    -> None

    let rec decoder (processCoreOnly: bool) (value: YAMLElement) : Dataset =
        checkType processCoreOnly "Dataset" value
        let identifier =
            tryGetField "identifier" value |> Option.bind tryDecodeString
            |> Option.defaultWith (fun () -> failwith "Dataset YAML object is missing required 'identifier' field.")

        let title          = tryGetField "title"          value |> Option.map decodeString
        let description    = tryGetField "description"    value |> Option.map decodeString
        let additionalType = tryGetField "additionalType" value |> Option.map decodeString

        let ds = Dataset(identifier, ?title = title, ?description = description, ?additionalType = additionalType)

        let annotations =
            addIndexedValues "annotations" (Annotation.decoder processCoreOnly) value

        let resolveAnnotation id = tryFind annotations id

        let labProtocols =
            addIndexedValues "labProtocols" (Recipe.decoderWithPropertyResolver processCoreOnly resolveAnnotation) value

        let resolveRecipe id = tryFind labProtocols id

        // processes
        tryGetField "processes" value
        |> Option.iter (fun v ->
            match tryDecodeSequence v with
            | Some elems ->
                for elem in elems do
                    match decodeRefOrInline (Process.decoderWithResolvers processCoreOnly resolveAnnotation resolveRecipe) elem with
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
            iterSequenceOrSingleton (fun elem ->
                match decodeRefOrInline (Annotation.decoder processCoreOnly) elem with
                | Choice2Of2 pv -> ds.AddAdditionalProperty(pv)
                | Choice1Of2 id -> resolveAnnotation id |> Option.iter ds.AddAdditionalProperty) v)

        applyOverflow "Dataset" processCoreOnly knownFields ds value
        ds

    // ── Encoders ───────────────────────────────────────────────────────────────

    let rec encoder (useIndexedMode: bool) (pvEncoder : (Annotation -> YAMLElement) option) (protEncoder : (Recipe -> YAMLElement) option) (ds: Dataset) : YAMLElement =

        // Build PV index from ALL processes (including hasPart children)
        let pvRegistry = Dictionary<string, Annotation>()
        let encodePV (pv : Annotation) =
            if useIndexedMode then
                let id = Annotation.genID pv
                pv.SetProperty("@id", id)
                if not <| pvRegistry.ContainsKey(id) then
                    pvRegistry.[id] <- pv
                encodeRef id
            else
                (Option.defaultValue Annotation.encoder pvEncoder) pv

        let protocolRegistry = Dictionary<string, Recipe>()
        let encodeProtocol (proto: Recipe) =
            if useIndexedMode then
                let id = Recipe.genID proto
                proto.SetProperty("@id", id)
                if not <| protocolRegistry.ContainsKey(id) then
                    protocolRegistry.[id] <- proto
                encodeRef id
            else
                (Option.defaultValue (Recipe.encoder encodePV) protEncoder) proto

        let processes =
            if ds.Processes.Count > 0 then
                ds.Processes
                |> Seq.map (Process.encoder encodePV encodeProtocol)
                |> Seq.toList
                |> yamlSeq
                |> Some
            else
                None

        let hasParts =
            if ds.HasPart.Count > 0 then
                ds.HasPart
                |> Seq.map (encoder false (Some encodePV) (Some encodeProtocol))
                |> Seq.toList
                |> yamlSeq
                |> Some
            else
                None

        let additionalProperties =
            if ds.AdditionalProperty.Count > 0 then
                ds.AdditionalProperty
                |> Seq.map encodePV
                |> Seq.toList
                |> yamlSeq
                |> Some
            else
                None

        [
            yield "type",       yamlValue "Dataset"
            yield "identifier", yamlValue ds.Identifier
            match ds.AdditionalType with
            | Some at -> yield "additionalType", yamlValue at
            | None    -> ()
            match ds.Title with
            | Some title -> yield "title", yamlValue title
            | None   -> ()
            match ds.Description with
            | Some d -> yield "description", yamlValue d
            | None   -> ()
            // Top-level index sections
            if protocolRegistry.Count > 0 then
                yield "labProtocols",
                    protocolRegistry.Values
                    |> Seq.map (fun kv -> Recipe.encoder encodePV kv)
                    |> Seq.toList
                    |> yamlSeq
            if pvRegistry.Count > 0 then
                yield "annotations",
                    pvRegistry.Values
                    |> Seq.map (fun kv -> Annotation.encoder kv)
                    |> Seq.toList
                    |> yamlSeq
            // Processes using references
            if processes.IsSome then
                yield "processes", processes.Value

            // hasPart children use the same resolvers but don't emit their own index sections
            if hasParts.IsSome then
                yield "hasPart", hasParts.Value

            if additionalProperties.IsSome then
                yield "additionalProperty", additionalProperties.Value
            yield! emitOverflow knownPropertyNames ds
        ]
        |> yamlMap

    let fromYamlString (processCoreOnly : bool) (s: string) : Dataset =
        YAMLicious.Reader.read s |> decoder processCoreOnly

    let toYamlString (whitespace: int option) (ds: Dataset) : string =
        writeYaml whitespace (encoder false None None ds)

    let toYamlStringIndexed (whitespace: int option) (ds: Dataset) : string =
        writeYaml whitespace (encoder true None None ds)
