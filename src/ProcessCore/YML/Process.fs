namespace ProcessCore.Yaml

open YAMLicious.YAMLiciousTypes
open ProcessCore
open Helpers

module Process =

    let private knownFields =
        Set.ofList
            [ "id"; "type"; "additionalType"; "name"; "inputs"; "outputs"
              "executesProtocol"; "parameterValue" ]

    let private knownPropertyNames =
        Set.ofList
            [ "id"; "type"; "additionaltype"; "name"; "input"; "output"; "inputs"; "outputs"
              "executesprotocol"; "parametervalue"; "processof" ]

    let private cloneDefinedTerm (term: DefinedTerm) =
        DefinedTerm(term.Name, ?tan = term.TAN, ?inDefinedTermSet = term.InDefinedTermSet)

    let private cloneFormalParameter (parameter: FormalParameter) =
        FormalParameter(
            parameter.Name,
            ?nameTAN = parameter.NameTAN,
            ?defaultValue = (parameter.DefaultValue |> Option.map cloneDefinedTerm))

    let private cloneAnnotation (annotation: Annotation) =
        Annotation(
            annotation.Name,
            ?value = annotation.Value,
            ?unit = annotation.Unit,
            ?nameTAN = annotation.NameTAN,
            ?valueTAN = annotation.ValueTAN,
            ?unitTAN = annotation.UnitTAN,
            ?additionalType = annotation.AdditionalType,
            ?instanceOf = (annotation.InstanceOf |> Option.map cloneFormalParameter))

    let private cloneProtocol (protocol: Recipe) =
        Recipe(
            ?name = protocol.Name,
            ?description = protocol.Description,
            ?version = protocol.Version,
            ?url = protocol.Url,
            ?intendedUse = (protocol.IntendedUse |> Option.map cloneDefinedTerm),
            ?additionalType = protocol.AdditionalType,
            parameters = (protocol.Parameters |> Seq.map cloneFormalParameter),
            components = (protocol.Components |> Seq.map cloneAnnotation),
            additionalProperty = (protocol.AdditionalProperty |> Seq.map cloneAnnotation))

    /// Decode a single input/output YAML element into an IONode.
    /// Discriminates by the `type` field value; defaults to Sample when absent.
    let private decodeIONodeWithPropertyResolver (processCoreOnly: bool) (resolveAnnotation: string -> Annotation option) (value: YAMLElement) : IONode option =
        match tryDecodeIdReference value with
        | Some _ -> None  // id reference — leave unresolved
        | None ->
            let typeStr = tryGetField "type" value |> Option.map decodeString |> Option.defaultValue ""
            match typeStr with
            | "Data" ->
                Some (DataNode (Data.decoderWithPropertyResolver processCoreOnly resolveAnnotation value))
            | "File" ->
                // Legacy alias: rewrite the type field to "Data" so checkType passes
                let rewritten =
                    getMappings value
                    |> List.map (fun (k, v) ->
                        if normalizeKey k = "type" then (k, yamlValue "Data") else (k, v))
                    |> yamlMap
                Some (DataNode (Data.decoderWithPropertyResolver processCoreOnly resolveAnnotation rewritten))
            | _ ->
                Some (SampleNode (Sample.decoderWithPropertyResolver processCoreOnly resolveAnnotation value))

    let private decodeIONode (processCoreOnly: bool) (value: YAMLElement) : IONode option =
        decodeIONodeWithPropertyResolver processCoreOnly (fun _ -> None) value

    let decoderWithResolvers (processCoreOnly: bool) (resolveAnnotation: string -> Annotation option) (resolveProtocol: string -> Recipe option) (value: YAMLElement) : ResizeArray<Process> =
        checkType processCoreOnly "Process" value
        let name           = requireField "name"          value |> decodeString
        let additionalType = tryGetField "additionalType" value |> Option.map decodeString
        let executesProtocol =
            tryGetField "executesProtocol" value
            |> Option.bind (fun v ->
                match decodeRefOrInline (Recipe.decoderWithPropertyResolver processCoreOnly resolveAnnotation) v with
                | Choice2Of2 proto -> Some proto
                | Choice1Of2 id    -> resolveProtocol id)

        let decodeIOSeq fieldName =
            let nodes = ResizeArray<IONode option>()
            tryGetField fieldName value
            |> Option.iter (fun v ->
                match tryDecodeSequence v with
                | Some elems ->
                    for elem in elems do
                        nodes.Add(decodeIONodeWithPropertyResolver processCoreOnly resolveAnnotation elem)
                | None -> ())
            nodes

        let parameterValues = ResizeArray<Annotation>()
        tryGetField "parameterValue" value
        |> Option.iter (fun v ->
            iterSequenceOrSingleton (fun elem ->
                match decodeRefOrInline (Annotation.decoder processCoreOnly) elem with
                | Choice2Of2 pv -> parameterValues.Add(pv)
                | Choice1Of2 id -> resolveAnnotation id |> Option.iter parameterValues.Add) v)

        let inputs = decodeIOSeq "inputs"
        let outputs = decodeIOSeq "outputs"
        let count = max 1 (max inputs.Count outputs.Count)
        let processes = ResizeArray<Process>()
        for i in 0 .. count - 1 do
            let proc =
                Process(
                    name,
                    ?additionalType = additionalType,
                    ?executesProtocol = (executesProtocol |> Option.map cloneProtocol))
            if i < inputs.Count then inputs.[i] |> Option.iter proc.SetInput
            if i < outputs.Count then outputs.[i] |> Option.iter proc.SetOutput
            for parameterValue in parameterValues do
                proc.AddParameterValue(cloneAnnotation parameterValue)

            applyOverflow "Process" processCoreOnly knownFields proc value
            processes.Add(proc)
        processes

    let decoder (processCoreOnly: bool) (value: YAMLElement) : ResizeArray<Process> =
        decoderWithResolvers processCoreOnly (fun _ -> None) (fun _ -> None) value

    let private encodeIONode (pvEncoder : Annotation -> YAMLElement) (node: IONode) : YAMLElement =
        match node with
        | SampleNode m -> Sample.encoder pvEncoder m
        | DataNode d     -> Data.encoder pvEncoder d

    let encoderMany (pvEncoder : Annotation -> YAMLElement) (protEncoder : Recipe -> YAMLElement) (processes: seq<Process>) : YAMLElement =
        let processes = processes |> Seq.toArray
        let proc = processes.[0]
        [
            yield "type", yamlValue "Process"
            yield "name", yamlValue proc.Name
            match proc.AdditionalType with
            | Some at -> yield "additionalType", yamlValue at
            | None    -> ()
            let inputs = processes |> Seq.choose (fun p -> p.Input)
            if not (Seq.isEmpty inputs) then
                yield "inputs",
                      inputs |> Seq.map (encodeIONode pvEncoder) |> Seq.toList |> yamlSeq
            let outputs = processes |> Seq.choose (fun p -> p.Output)
            if not (Seq.isEmpty outputs) then
                yield "outputs",
                      outputs |> Seq.map (encodeIONode pvEncoder) |> Seq.toList |> yamlSeq
            match proc.ExecutesProtocol with
            | Some proto -> yield "executesProtocol", protEncoder proto
            | None       -> ()
            if proc.ParameterValue.Count > 0 then
                yield "parameterValue",
                      proc.ParameterValue
                      |> Seq.map pvEncoder
                      |> Seq.toList
                      |> yamlSeq
            yield! emitOverflow knownPropertyNames proc
        ]
        |> yamlMap

    let encoder (pvEncoder : Annotation -> YAMLElement) (protEncoder : Recipe -> YAMLElement) (proc: Process) : YAMLElement =
        encoderMany pvEncoder protEncoder [proc]

    /// Structural key for the non-I/O process state used only by dataset YAML grouping.
    let groupingKey (proc: Process) =
        encoder Annotation.encoder (Recipe.encoder Annotation.encoder) proc
        |> getMappings
        |> List.filter (fun (key, _) ->
            let key = normalizeKey key
            key <> "inputs" && key <> "outputs")
        |> yamlMap
        |> writeYaml None

    let fromYamlString (processCoreOnly: bool) (s: string) : ResizeArray<Process> =
        YAMLicious.Reader.read s |> decoder processCoreOnly

    let toYamlString (whitespace: int option) (proc: Process) : string =
        writeYaml whitespace (encoder Annotation.encoder (Recipe.encoder Annotation.encoder) proc)
