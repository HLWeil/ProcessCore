namespace ProcessCore.Yaml

#nowarn "3261" // DynamicObj returns obj|null; we handle null-safety at runtime via pattern matching

open System
open System.Globalization
open YAMLicious.YAMLiciousTypes
open DynamicObj

module Helpers =

    // ── Key normalization ──────────────────────────────────────────────────────

    let normalizeKey (key: string) =
        key.Trim().Trim('"').Trim('\'')

    // ── Structural unwrapping ──────────────────────────────────────────────────

    let unwrapSingleObject = function
        | YAMLElement.Object [single] ->
            match single with
            | YAMLElement.Mapping _ -> YAMLElement.Object [single]
            | _ -> single
        | other -> other

    // ── Mapping extraction ─────────────────────────────────────────────────────

    let tryGetMappings = function
        | YAMLElement.Object elements ->
            elements
            |> List.choose (function
                | YAMLElement.Mapping (k, v) -> Some (normalizeKey k.Value, v)
                | _ -> None)
            |> Some
        | _ -> None

    let getMappings element =
        element
        |> unwrapSingleObject
        |> tryGetMappings
        |> Option.defaultValue []

    let tryGetField name element =
        getMappings element
        |> List.tryPick (fun (k, v) -> if normalizeKey k = name then Some v else None)

    let requireField name element =
        match tryGetField name element with
        | Some v -> v
        | None   -> failwithf "Required YAML field '%s' is missing." name

    // ── Scalar decoding ────────────────────────────────────────────────────────

    let tryDecodeString = function
        | YAMLElement.Value v                   -> Some v.Value
        | YAMLElement.Object [YAMLElement.Value v] -> Some v.Value
        | _ -> None

    let decodeString element =
        match tryDecodeString element with
        | Some s -> s
        | None   -> failwithf "Expected string-like YAML value but got %A" element

    /// Verify the 'type' field, if present, equals the expected value.
    /// When processCoreOnly is false the check is skipped and the generic field-by-field
    /// decoding path is taken instead (lenient mode for decorated YAML).
    let checkType (processCoreOnly: bool) (expected: string) (element: YAMLElement) =
        if processCoreOnly then
            match tryGetField "type" element |> Option.bind tryDecodeString with
            | Some actual when actual <> expected ->
                failwithf "Expected YAML type '%s' but found '%s'." expected actual
            | _ -> ()

    let tryDecodeStringResizeArray element =
        match unwrapSingleObject element with
        | YAMLElement.Sequence elements ->
            elements |> List.map decodeString |> ResizeArray |> Some
        | _ ->
            tryDecodeString element |> Option.map (fun s -> ResizeArray [s])

    let tryDecodeSequence = function
        | YAMLElement.Sequence elements                   -> Some elements
        | YAMLElement.Object [YAMLElement.Sequence elements] -> Some elements
        | _ -> None

    let parseScalarToObj (value: string) : obj =
        let text = value.Trim()
        if text.Equals("null", StringComparison.OrdinalIgnoreCase) || text = "~" then
            null
        else
            let mutable boolVal    = false
            let mutable intVal     = 0
            let mutable decimalVal = 0M
            if   Boolean.TryParse(text, &boolVal) then box boolVal
            elif Int32.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, &intVal) then box intVal
            elif Decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, &decimalVal) then box decimalVal
            else box value

    // ── YAMLElement constructors ───────────────────────────────────────────────

    let yamlValue (s: string) =
        let s = if s.Contains("#") then $"\"{s}\"" else s
        YAMLElement.Value (YAMLContent.create(s, style = ScalarStyle.Plain))

    let yamlMap (pairs: (string * YAMLElement) list) =
        pairs
        |> List.map (fun (k, v) -> YAMLElement.Mapping (YAMLContent.create(k, style = ScalarStyle.Plain), v))
        |> YAMLElement.Object

    let yamlSeq (items: YAMLElement list) =
        YAMLElement.Sequence items

    let objToScalarYaml (value: obj) =
        match value with
        | null                -> yamlValue "null"
        | :? string  as s    -> yamlValue s
        | :? int     as i    -> yamlValue (string i)
        | :? bool    as b    -> yamlValue (if b then "true" else "false")
        | :? float   as f    -> yamlValue (f.ToString(CultureInfo.InvariantCulture))
        | :? decimal as d    -> yamlValue (d.ToString(CultureInfo.InvariantCulture))
        | :? DateTime as dt  -> yamlValue (dt.ToString("O", CultureInfo.InvariantCulture))
        | _ -> failwithf "Unsupported scalar type for YAML encoding: %s" (value.GetType().FullName)

    // ── Generic overflow codec ─────────────────────────────────────────────────

    /// Decode any YAML element into an obj for storage in DynamicObj.
    /// Scalars → primitives, sequences → ResizeArray<obj>, objects → DynamicObj.
    let rec genericDecodeToObj (value: YAMLElement) : obj =
        match unwrapSingleObject value with
        | YAMLElement.Value v ->
            parseScalarToObj v.Value
        | YAMLElement.Sequence elements ->
            elements
            |> List.map genericDecodeToObj
            |> ResizeArray
            |> box
        | YAMLElement.Object _ ->
            let dynObj = DynamicObj()
            for (k, v) in getMappings value do
                dynObj.SetProperty(k, genericDecodeToObj v)
            box dynObj
        | other ->
            failwithf "Unsupported YAML structure for generic overflow: %A" other

    /// Encode a DynamicObj overflow value back to YAMLElement.
    let rec genericEncodeFromObj (value: obj) : YAMLElement =
        match value with
        | null                -> yamlValue "null"
        | :? string  as s    -> yamlValue s
        | :? int     as i    -> yamlValue (string i)
        | :? bool    as b    -> yamlValue (if b then "true" else "false")
        | :? float   as f    -> yamlValue (f.ToString(CultureInfo.InvariantCulture))
        | :? decimal as d    -> yamlValue (d.ToString(CultureInfo.InvariantCulture))
        | :? DateTime as dt  -> yamlValue (dt.ToString("O", CultureInfo.InvariantCulture))
        | :? DynamicObj as dynObj ->
            dynObj.GetProperties(true)
            |> Seq.map (fun kv -> kv.Key, genericEncodeFromObj kv.Value)
            |> Seq.toList
            |> yamlMap
        | :? System.Collections.IEnumerable as l ->
            l |> Seq.cast<obj> |> Seq.map genericEncodeFromObj |> Seq.toList |> yamlSeq
        | _ -> failwithf "Cannot encode overflow value of type %s: %A" (value.GetType().FullName) value

    // ── Ref-or-inline helper ───────────────────────────────────────────────────

    /// Decode a field that is either a plain string id reference or a full inline object.
    /// Returns Choice1Of2(id) for references, Choice2Of2(decoded) for inline objects.
    let decodeRefOrInline (inlineDecoder: YAMLElement -> 'a) (value: YAMLElement) : Choice<string, 'a> =
        match tryDecodeString value with
        | Some id -> Choice1Of2 id
        | None    -> Choice2Of2 (inlineDecoder value)

    // ── Overflow helpers ───────────────────────────────────────────────────────

    /// Store all YAML fields whose keys are not in knownFields as dynamic properties on obj.
    let applyOverflow (objectType : string) (processCoreOnly : bool) (knownFields: Set<string>) (obj: DynamicObj) (value: YAMLElement) =
        for (key, yamlVal) in getMappings value do
            if not (knownFields |> Set.contains key) then
                if processCoreOnly then
                    failwithf $"Unknown field '{key}' found in YAML for object of type '{objectType}'. Strict mode is enabled, so this is not allowed. Field value: '{yamlVal}'"
                else
                    obj.SetProperty(key, genericDecodeToObj yamlVal)
             


    /// Emit all dynamic properties whose lowercase key is not in knownPropertyNames.
    let emitOverflow (knownPropertyNames: Set<string>) (obj: DynamicObj) : (string * YAMLElement) list =
        obj.GetProperties(true)
        |> Seq.choose (fun kv ->
            if not (knownPropertyNames |> Set.contains (kv.Key.ToLowerInvariant())) then
                Some (kv.Key, genericEncodeFromObj kv.Value)
            else
                None)
        |> Seq.toList

    // ── Writer helper ──────────────────────────────────────────────────────────

    /// Render a YAMLElement to a YAML string with the given indentation width (default 2).
    let writeYaml (whitespace: int option) (element: YAMLElement) : string =
        YAMLicious.Writer.write element (Some (fun c -> { c with Whitespace = defaultArg whitespace 2 }))
