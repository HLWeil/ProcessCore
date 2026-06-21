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
            [ "id"; "type"; "additionaltype"; "name"; "inputs"; "outputs"
              "executesprotocol"; "parametervalue"; "processof" ]

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

    let decoderWithResolvers (processCoreOnly: bool) (resolveAnnotation: string -> Annotation option) (resolveProtocol: string -> Plan option) (value: YAMLElement) : Process =
        checkType processCoreOnly "Process" value
        let name           = requireField "name"          value |> decodeString
        let additionalType = tryGetField "additionalType" value |> Option.map decodeString
        let executesProtocol =
            tryGetField "executesProtocol" value
            |> Option.bind (fun v ->
                match decodeRefOrInline (Plan.decoderWithPropertyResolver processCoreOnly resolveAnnotation) v with
                | Choice2Of2 proto -> Some proto
                | Choice1Of2 id    -> resolveProtocol id)

        let proc = Process(name, ?additionalType = additionalType, ?executesProtocol = executesProtocol)

        let decodeIOSeq fieldName =
            tryGetField fieldName value
            |> Option.iter (fun v ->
                match tryDecodeSequence v with
                | Some elems ->
                    for elem in elems do
                        decodeIONodeWithPropertyResolver processCoreOnly resolveAnnotation elem |> Option.iter (fun n ->
                            if fieldName = "inputs"  then proc.AddInput(n)
                            else                          proc.AddOutput(n))
                | None -> ())

        decodeIOSeq "inputs"
        decodeIOSeq "outputs"

        tryGetField "parameterValue" value
        |> Option.iter (fun v ->
            iterSequenceOrSingleton (fun elem ->
                match decodeRefOrInline (Annotation.decoder processCoreOnly) elem with
                | Choice2Of2 pv -> proc.AddParameterValue(pv)
                | Choice1Of2 id -> resolveAnnotation id |> Option.iter proc.AddParameterValue) v)

        applyOverflow "Process" processCoreOnly knownFields proc value
        proc

    let decoder (processCoreOnly: bool) (value: YAMLElement) : Process =
        decoderWithResolvers processCoreOnly (fun _ -> None) (fun _ -> None) value

    let private encodeIONode (pvEncoder : Annotation -> YAMLElement) (node: IONode) : YAMLElement =
        match node with
        | SampleNode m -> Sample.encoder pvEncoder m
        | DataNode d     -> Data.encoder pvEncoder d

    let encoder (pvEncoder : Annotation -> YAMLElement) (protEncoder : Plan -> YAMLElement) (proc: Process) : YAMLElement =
        [
            yield "type", yamlValue "Process"
            yield "name", yamlValue proc.Name
            match proc.AdditionalType with
            | Some at -> yield "additionalType", yamlValue at
            | None    -> ()
            if proc.Inputs.Count > 0 then
                yield "inputs",
                      proc.Inputs |> Seq.map (encodeIONode pvEncoder) |> Seq.toList |> yamlSeq
            if proc.Outputs.Count > 0 then
                yield "outputs",
                      proc.Outputs |> Seq.map (encodeIONode pvEncoder) |> Seq.toList |> yamlSeq
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

    let fromYamlString (processCoreOnly: bool) (s: string) : Process =
        YAMLicious.Reader.read s |> decoder processCoreOnly

    let toYamlString (whitespace: int option) (proc: Process) : string =
        writeYaml whitespace (encoder Annotation.encoder (Plan.encoder Annotation.encoder) proc)
