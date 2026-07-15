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
              "license"; "datePublished"; "dateCreated"; "dateModified"
              "processes"; "hasPart"; "dataFiles"; "agents"; "citations"
              "dataContexts"; "additionalProperty"; "ArcPath"; "IsSpreadsheetScaffold"
              "annotations" ]

    let knownPropertyNames =
        Set.ofList
            [ "type"; "additionaltype"; "identifier"; "title"; "description"
              "license"; "datepublished"; "datecreated"; "datemodified"
              "processes"; "haspart"; "datafiles"; "agents"
              "citations"; "datacontexts"; "additionalproperty"; "partof"
              "propertyvalues"; "annotations"; "samples"; "recipes"; "labprotocols"
              // Fable-compiled read-only instance properties — must not be re-emitted as overflow
              "noderegistrydirect"; "nodepinsdirect"; "reciperegistrydirect"; "recipepinsdirect"
              "fragmentselectorprovidersdirect"; "arcpath"; "isspreadsheetscaffold" ]

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


    /// <summary>
    /// Decodes a Dataset or subclass from a YAMLElement. YAML allows annotations and recipes to be defined in a top-level index section, with each containing an "@id" field. Other objects can then reference these by their "@id" instead of being defined inline. This decoder supports that pattern by first decoding the annotations and recipes into registries, and then resolving references to them when decoding processes and other properties. If resolvers are provided, they will be used to resolve references to annotations and recipes. If not, the decoder will only resolve references that were defined in the same YAML document.
    /// </summary>
    /// <param name="createF">Factory function to create an instance of 'A given the identifier</param>
    /// <param name="annotationResolver">Resolver for annotations</param>
    /// <param name="recipeResolver">Resolver for recipes</param>
    /// <param name="processCoreOnly">Flag indicating if only process core properties should be decoded. If set to true, the decoder fails when encountering objects not defined in the core spec.</param>
    /// <param name="value">The YAMLElement to decode</param>
    let rec private decoderGenericCore<'A when 'A :> Dataset>
        (createF : string -> 'A)
        (annotationResolver : (string -> Annotation option) option) 
        (recipeResolver : (string -> Recipe option) option)
        (storeSample : ('A -> Sample -> Sample) option)
        (storeRecipe : ('A -> Recipe -> Recipe) option)
        (processCoreOnly: bool) 
        (value: YAMLElement) : 'A =
        checkType processCoreOnly "Dataset" value
        let identifier =
            tryGetField "identifier" value |> Option.bind tryDecodeString
            |> Option.defaultWith (fun () -> failwith "Dataset YAML object is missing required 'identifier' field.")

        let title          = tryGetField "title"          value |> Option.map decodeString
        let description    = tryGetField "description"    value |> Option.map decodeString
        let additionalType = tryGetField "additionalType" value |> Option.map decodeString
        let license        = tryGetField "license"        value |> Option.map decodeString
        let datePublished  = tryGetField "datePublished"  value |> Option.map decodeString
        let dateCreated    = tryGetField "dateCreated"    value |> Option.map decodeString
        let dateModified   = tryGetField "dateModified"   value |> Option.map decodeString

        let ds =
            createF identifier

        ds.Title <- title
        ds.Description <- description
        ds.AdditionalType <- additionalType
        ds.License <- license
        ds.DatePublished <- datePublished
        ds.DateCreated <- dateCreated
        ds.DateModified <- dateModified

        let resolveAnnotation = 
            match annotationResolver with
            | Some resolver -> resolver
            | None ->
                let annotations =
                    addIndexedValues "annotations" (Annotation.decoder processCoreOnly) value
                tryFind annotations

        let resolveRecipe =
            match recipeResolver with
            | Some resolver -> resolver
            | None ->
                let recipes = Dictionary<string, Recipe>()
                (tryGetField "recipes" value
                 |> Option.orElseWith (fun () -> tryGetField "labProtocols" value))
                |> Option.iter (fun values ->
                    iterSequenceOrSingleton (fun elem ->
                        let decoded = Recipe.decoderWithPropertyResolver processCoreOnly resolveAnnotation elem
                        let canonical =
                            storeRecipe
                            |> Option.map (fun store -> store ds decoded)
                            |> Option.defaultValue decoded
                        match tryGetField "@id" elem |> Option.bind tryDecodeString with
                        | Some id -> recipes.[normalizeId id] <- canonical
                        | None -> ()) values)
                tryFind recipes

        let decodeSeq fieldName (decoder: YAMLElement -> 'a) (resolve: string -> 'a option) (add: 'a -> unit) =
            tryGetField fieldName value
            |> Option.iter (fun v ->
                iterSequenceOrSingleton (fun elem ->
                    match decodeRefOrInline decoder elem with
                    | Choice2Of2 x -> add x
                    | Choice1Of2 id -> resolve id |> Option.iter add) v)

        // ARC-owned objects and Dataset data files are loaded before processes so
        // they win first-writer canonicalization.
        storeSample
        |> Option.iter (fun store ->
            decodeSeq
                "samples"
                (Sample.decoderWithPropertyResolver processCoreOnly resolveAnnotation)
                (fun _ -> None)
                (fun sample -> store ds sample |> ignore))

        decodeSeq "dataFiles" (Data.decoderWithPropertyResolver processCoreOnly resolveAnnotation) (fun _ -> None) ds.AddDataFile

        // processes
        tryGetField "processes" value
        |> Option.iter (fun v ->
            match tryDecodeSequence v with
            | Some elems ->
                for elem in elems do
                    match tryDecodeString elem with
                    | Some _ -> ()
                    | None ->
                        for proc in Process.decoderWithResolvers processCoreOnly resolveAnnotation resolveRecipe elem do
                            ds.AddProcess(proc)
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
                        let hasPath = tryGetField "path" elem |> Option.isSome
                        if typeStr = "Data" || typeStr = "MediaObject" || typeStr = "File" || hasPath then
                            let data = Data.decoderWithPropertyResolver processCoreOnly resolveAnnotation elem
                            ds.AddDataFile(data)
                        elif typeStr = "Dataset" || typeStr = "" then
                            // Try to decode as Dataset (nested); empty type defaults to Dataset
                            let child =
                                decoderGenericCore<Dataset>
                                    (fun i -> Dataset(i))
                                    (Some resolveAnnotation)
                                    (Some resolveRecipe)
                                    None
                                    None
                                    processCoreOnly
                                    elem
                            ds.AddPart(child)
            | None -> ())

        decodeSeq "agents" (Agent.decoder processCoreOnly) (fun _ -> None) ds.AddAgent
        decodeSeq "citations" (ScholarlyArticle.decoder processCoreOnly) (fun _ -> None) ds.AddCitation
        decodeSeq "dataContexts" (DataContext.decoder processCoreOnly) (fun _ -> None) ds.AddDataContext

        // additionalProperty
        tryGetField "additionalProperty" value
        |> Option.iter (fun v ->
            iterSequenceOrSingleton (fun elem ->
                match decodeRefOrInline (Annotation.decoder processCoreOnly) elem with
                | Choice2Of2 pv -> ds.AddAdditionalProperty(pv)
                | Choice1Of2 id -> resolveAnnotation id |> Option.iter ds.AddAdditionalProperty) v)

        let fields =
            if storeSample.IsSome || storeRecipe.IsSome then
                knownFields
                |> Set.add "samples"
                |> Set.add "recipes"
                |> Set.add "labProtocols"
            else
                knownFields
        applyOverflow "Dataset" processCoreOnly fields ds value
        ds

    let decoderGeneric<'A when 'A :> Dataset>
        (createF : string -> 'A)
        (annotationResolver : (string -> Annotation option) option)
        (recipeResolver : (string -> Recipe option) option)
        (processCoreOnly: bool)
        (value: YAMLElement) : 'A =
        decoderGenericCore createF annotationResolver recipeResolver None None processCoreOnly value

    let decoderGenericWithStores<'A when 'A :> Dataset>
        (createF : string -> 'A)
        (storeSample : 'A -> Sample -> Sample)
        (storeRecipe : 'A -> Recipe -> Recipe)
        (processCoreOnly: bool)
        (value: YAMLElement) : 'A =
        decoderGenericCore createF None None (Some storeSample) (Some storeRecipe) processCoreOnly value

    let decoder (processCoreOnly: bool) (value: YAMLElement) : Dataset =
        decoderGeneric<Dataset> (fun i -> Dataset(i)) None None processCoreOnly value

    // ── Encoders ───────────────────────────────────────────────────────────────

    let rec private encoderCore
        (useIndexedMode: bool)
        (pvEncoder : (Annotation -> YAMLElement) option)
        (recipeEncoder : (Recipe -> YAMLElement) option)
        (storedSamples : seq<Sample> option)
        (storedRecipes : seq<Recipe> option)
        (ds: Dataset) : YAMLElement =

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

        let recipeRegistry = Dictionary<string, Recipe>()
        let encodeRecipe (proto: Recipe) =
            if useIndexedMode then
                let id = Recipe.genID proto
                proto.SetProperty("@id", id)
                if not <| recipeRegistry.ContainsKey(id) then
                    recipeRegistry.[id] <- proto
                encodeRef id
            else
                (Option.defaultValue (Recipe.encoder encodePV) recipeEncoder) proto

        storedRecipes
        |> Option.iter (Seq.iter (fun recipe -> encodeRecipe recipe |> ignore))

        let sameProcessState (a: Process) (b: Process) =
            let shape (p: Process) = p.Input.IsSome, p.Output.IsSome
            shape a = shape b && Process.groupingKey a = Process.groupingKey b

        let processes =
            if ds.Processes.Count > 0 then
                let groups = ResizeArray<ResizeArray<Process>>()
                for proc in ds.Processes do
                    match groups |> Seq.tryFind (fun group -> sameProcessState group.[0] proc) with
                    | Some group -> group.Add(proc)
                    | None -> groups.Add(ResizeArray([proc]))
                groups |> Seq.map (Process.encoderMany encodePV encodeRecipe)
                |> Seq.toList
                |> yamlSeq
                |> Some
            else
                None

        let hasParts =
            if ds.HasPart.Count > 0 then
                ds.HasPart
                |> Seq.map (encoderCore false (Some encodePV) (Some encodeRecipe) None None)
                |> Seq.toList
                |> yamlSeq
                |> Some
            else
                None

        let dataFiles =
            if ds.DataFiles.Count > 0 then
                ds.DataFiles
                |> Seq.map (Data.encoder encodePV)
                |> Seq.toList
                |> yamlSeq
                |> Some
            else
                None

        let agents =
            if ds.Agents.Count > 0 then
                ds.Agents
                |> Seq.map Agent.encoder
                |> Seq.toList
                |> yamlSeq
                |> Some
            else
                None

        let citations =
            if ds.Citations.Count > 0 then
                ds.Citations
                |> Seq.map ScholarlyArticle.encoder
                |> Seq.toList
                |> yamlSeq
                |> Some
            else
                None

        let dataContexts =
            if ds.DataContexts.Count > 0 then
                ds.DataContexts
                |> Seq.map DataContext.encoder
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

        let samples =
            storedSamples
            |> Option.bind (fun values ->
                let encoded = values |> Seq.map (Sample.encoder encodePV) |> Seq.toList
                if encoded.IsEmpty then None else Some (yamlSeq encoded))

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
            match ds.License with
            | Some license -> yield "license", yamlValue license
            | None -> ()
            match ds.DatePublished with
            | Some date -> yield "datePublished", yamlValue date
            | None -> ()
            match ds.DateCreated with
            | Some date -> yield "dateCreated", yamlValue date
            | None -> ()
            match ds.DateModified with
            | Some date -> yield "dateModified", yamlValue date
            | None -> ()
            // Top-level index sections
            if recipeRegistry.Count > 0 then
                yield "recipes",
                    recipeRegistry.Values
                    |> Seq.map (fun kv -> Recipe.encoder encodePV kv)
                    |> Seq.toList
                    |> yamlSeq
            if pvRegistry.Count > 0 then
                yield "annotations",
                    pvRegistry.Values
                    |> Seq.map (fun kv -> Annotation.encoder kv)
                    |> Seq.toList
                    |> yamlSeq
            if samples.IsSome then
                yield "samples", samples.Value
            // Processes using references
            if processes.IsSome then
                yield "processes", processes.Value

            // hasPart children use the same resolvers but don't emit their own index sections
            if hasParts.IsSome then
                yield "hasPart", hasParts.Value

            if dataFiles.IsSome then
                yield "dataFiles", dataFiles.Value

            if agents.IsSome then
                yield "agents", agents.Value

            if citations.IsSome then
                yield "citations", citations.Value

            if dataContexts.IsSome then
                yield "dataContexts", dataContexts.Value

            if additionalProperties.IsSome then
                yield "additionalProperty", additionalProperties.Value
            yield! emitOverflow knownPropertyNames ds
        ]
        |> yamlMap

    let encoder
        (useIndexedMode: bool)
        (pvEncoder : (Annotation -> YAMLElement) option)
        (recipeEncoder : (Recipe -> YAMLElement) option)
        (ds: Dataset) : YAMLElement =
        encoderCore useIndexedMode pvEncoder recipeEncoder None None ds

    let encoderWithStores (samples: seq<Sample>) (recipes: seq<Recipe>) (ds: Dataset) : YAMLElement =
        encoderCore true None None (Some samples) (Some recipes) ds

    let fromYamlString (processCoreOnly : bool) (s: string) : Dataset =
        YAMLicious.Reader.read s |> decoder processCoreOnly

    let toYamlString (whitespace: int option) (ds: Dataset) : string =
        writeYaml whitespace (encoder false None None ds)

    let toYamlStringIndexed (whitespace: int option) (ds: Dataset) : string =
        writeYaml whitespace (encoder true None None ds)

    let toYamlStringIndexedWithStores
        (whitespace: int option)
        (samples: seq<Sample>)
        (recipes: seq<Recipe>)
        (ds: Dataset) : string =
        writeYaml whitespace (encoderWithStores samples recipes ds)

