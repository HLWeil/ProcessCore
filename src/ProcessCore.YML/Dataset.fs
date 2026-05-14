namespace ProcessCore.Yaml

open System
open System.Collections.Generic
open YAMLicious.YAMLiciousTypes
open ProcessCore
open Helpers

module Dataset =

    let knownFields =
        Set.ofList
            [ "type"; "additionalType"; "identifier"; "name"; "description"
              "processes"; "hasPart"; "additionalProperty" ]

    let knownPropertyNames =
        Set.ofList
            [ "type"; "additionaltype"; "identifier"; "name"; "description"
              "processes"; "haspart"; "additionalproperty"; "partof"
              "propertyvalues"; "labprotocols" ]

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

        let name           = tryGetField "name"           value |> Option.map decodeString
        let description    = tryGetField "description"    value |> Option.map decodeString
        let additionalType = tryGetField "additionalType" value |> Option.map decodeString

        let ds = Dataset(identifier, ?name = name, ?description = description, ?additionalType = additionalType)

        let propertyValues =
            addIndexedValues "propertyValues" (PropertyValue.decoder processCoreOnly) value

        let resolvePropertyValue id = tryFind propertyValues id

        let labProtocols =
            addIndexedValues "labProtocols" (LabProtocol.decoderWithPropertyResolver processCoreOnly resolvePropertyValue) value

        let resolveLabProtocol id = tryFind labProtocols id

        // processes
        tryGetField "processes" value
        |> Option.iter (fun v ->
            match tryDecodeSequence v with
            | Some elems ->
                for elem in elems do
                    match decodeRefOrInline (LabProcess.decoderWithResolvers processCoreOnly resolvePropertyValue resolveLabProtocol) elem with
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
                match decodeRefOrInline (PropertyValue.decoder processCoreOnly) elem with
                | Choice2Of2 pv -> ds.AddAdditionalProperty(pv)
                | Choice1Of2 id -> resolvePropertyValue id |> Option.iter ds.AddAdditionalProperty) v)

        applyOverflow "Dataset" processCoreOnly knownFields ds value
        ds

    // ── Encoders ───────────────────────────────────────────────────────────────

    let rec encoder (useIndexedMode: bool) (pvEncoder : (PropertyValue -> YAMLElement) option) (protEncoder : (LabProtocol -> YAMLElement) option) (ds: Dataset) : YAMLElement =
        
        // Build PV index from ALL processes (including hasPart children)
        let pvRegistry = Dictionary<string, PropertyValue>()
        let encodePV (pv : PropertyValue) =
            if useIndexedMode then
                let id = PropertyValue.genID pv
                pv.SetProperty("@id", id)
                if not <| pvRegistry.ContainsKey(id) then                
                    pvRegistry.[id] <- pv
                encodeRef id
            else 
                (Option.defaultValue PropertyValue.encoder pvEncoder) pv

        let protocolRegistry = Dictionary<string, LabProtocol>()
        let encodeProtocol (proto: LabProtocol) =
            if useIndexedMode then
                let id = LabProtocol.genID proto
                proto.SetProperty("@id", id)
                if not <| protocolRegistry.ContainsKey(id) then
                    protocolRegistry.[id] <- proto
                encodeRef id
            else
                (Option.defaultValue (LabProtocol.encoder encodePV) protEncoder) proto

        let processes =
            if ds.Processes.Count > 0 then
                ds.Processes
                |> Seq.map (LabProcess.encoder encodePV encodeProtocol)
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
            match ds.Name with
            | Some n -> yield "name", yamlValue n
            | None   -> ()
            match ds.Description with
            | Some d -> yield "description", yamlValue d
            | None   -> ()
            // Top-level index sections
            if protocolRegistry.Count > 0 then
                yield "labProtocols",
                    protocolRegistry.Values
                    |> Seq.map (fun kv -> LabProtocol.encoder encodePV kv)
                    |> Seq.toList
                    |> yamlSeq
            if pvRegistry.Count > 0 then
                yield "propertyValues",
                    pvRegistry.Values
                    |> Seq.map (fun kv -> PropertyValue.encoder kv)
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
