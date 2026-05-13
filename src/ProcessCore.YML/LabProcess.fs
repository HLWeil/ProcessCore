namespace ProcessCore.Yaml

open YAMLicious.YAMLiciousTypes
open ProcessCore
open Helpers

module LabProcess =

    let private knownFields =
        Set.ofList
            [ "id"; "type"; "additionalType"; "name"; "inputs"; "outputs"
              "executesProtocol"; "parameterValue" ]

    let private knownPropertyNames =
        Set.ofList
            [ "id"; "type"; "additionaltype"; "name"; "inputs"; "outputs"
              "executesprotocol"; "parametervalue"; "processof" ]

    /// Decode a single input/output YAML element into an IONode.
    /// Discriminates by the `type` field value; defaults to Material when absent.
    let private decodeIONodeWithPropertyResolver (processCoreOnly: bool) (resolvePropertyValue: string -> PropertyValue option) (value: YAMLElement) : IONode option =
        match tryDecodeIdReference value with
        | Some _ -> None  // id reference — leave unresolved
        | None ->
            let typeStr = tryGetField "type" value |> Option.map decodeString |> Option.defaultValue ""
            match typeStr with
            | "Data" ->
                Some (DataNode (Data.decoderWithPropertyResolver processCoreOnly resolvePropertyValue value))
            | "File" ->
                // Legacy alias: rewrite the type field to "Data" so checkType passes
                let rewritten =
                    getMappings value
                    |> List.map (fun (k, v) ->
                        if normalizeKey k = "type" then (k, yamlValue "Data") else (k, v))
                    |> yamlMap
                Some (DataNode (Data.decoderWithPropertyResolver processCoreOnly resolvePropertyValue rewritten))
            | _ ->
                Some (MaterialNode (Material.decoderWithPropertyResolver processCoreOnly resolvePropertyValue value))

    let private decodeIONode (processCoreOnly: bool) (value: YAMLElement) : IONode option =
        decodeIONodeWithPropertyResolver processCoreOnly (fun _ -> None) value

    let decoderWithResolvers (processCoreOnly: bool) (resolvePropertyValue: string -> PropertyValue option) (resolveProtocol: string -> LabProtocol option) (value: YAMLElement) : LabProcess =
        checkType processCoreOnly "LabProcess" value
        let name           = requireField "name"          value |> decodeString
        let additionalType = tryGetField "additionalType" value |> Option.map decodeString
        let executesProtocol =
            tryGetField "executesProtocol" value
            |> Option.bind (fun v ->
                match decodeRefOrInline (LabProtocol.decoderWithPropertyResolver processCoreOnly resolvePropertyValue) v with
                | Choice2Of2 proto -> Some proto
                | Choice1Of2 id    -> resolveProtocol id)

        let proc = LabProcess(name, ?additionalType = additionalType, ?executesProtocol = executesProtocol)

        let decodeIOSeq fieldName =
            tryGetField fieldName value
            |> Option.iter (fun v ->
                match tryDecodeSequence v with
                | Some elems ->
                    for elem in elems do
                        decodeIONodeWithPropertyResolver processCoreOnly resolvePropertyValue elem |> Option.iter (fun n ->
                            if fieldName = "inputs"  then proc.AddInput(n)
                            else                          proc.AddOutput(n))
                | None -> ())

        decodeIOSeq "inputs"
        decodeIOSeq "outputs"

        tryGetField "parameterValue" value
        |> Option.iter (fun v ->
            iterSequenceOrSingleton (fun elem ->
                match decodeRefOrInline (PropertyValue.decoder processCoreOnly) elem with
                | Choice2Of2 pv -> proc.AddParameterValue(pv)
                | Choice1Of2 id -> resolvePropertyValue id |> Option.iter proc.AddParameterValue) v)

        applyOverflow "LabProcess" processCoreOnly knownFields proc value
        proc

    let decoder (processCoreOnly: bool) (value: YAMLElement) : LabProcess =
        decoderWithResolvers processCoreOnly (fun _ -> None) (fun _ -> None) value

    let private encodeIONode (pvEncoder : PropertyValue -> YAMLElement) (node: IONode) : YAMLElement =
        match node with
        | MaterialNode m -> Material.encoder pvEncoder m
        | DataNode d     -> Data.encoder pvEncoder d

    let encoder (pvEncoder : PropertyValue -> YAMLElement) (protEncoder : LabProtocol -> YAMLElement) (proc: LabProcess) : YAMLElement =
        [
            yield "type", yamlValue "LabProcess"
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

    let fromYamlString (processCoreOnly: bool) (s: string) : LabProcess =
        YAMLicious.Reader.read s |> decoder processCoreOnly

    let toYamlString (whitespace: int option) (proc: LabProcess) : string =
        writeYaml whitespace (encoder PropertyValue.encoder (LabProtocol.encoder PropertyValue.encoder) proc)
