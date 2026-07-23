namespace ProcessCore.Yaml

#nowarn "3261" // DynamicObj returns obj|null; we handle null-safety at runtime via pattern matching

open System
open System.Collections.Generic
open System.Globalization
open YAMLicious.YAMLiciousTypes
open DynamicObj

module Helpers =

    let specialCharacters = Set [| ':'; '-'; '{'; '}'; '['; ']'; ','; '&'; '*'; '#'; '?'; '|'; '>'; '%'; '@'; '`' |]

    // ── Key normalization ──────────────────────────────────────────────────────

    let normalizeKey (key: string) =
        key.Trim().Trim('"').Trim('\'')

    let normalizeId (id: string) =
        id.Trim().Trim('"').Trim('\'')

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

    let tryGetTypeName element =
        getMappings element
        |> List.tryPick (fun (key, value) ->
            let normalized = normalizeKey key
            if normalized.Equals("type", StringComparison.OrdinalIgnoreCase) ||
               normalized.Equals("@type", StringComparison.OrdinalIgnoreCase) then
                tryDecodeString value
            else
                None)

    let decodeString element =
        match tryDecodeString element with
        | Some s -> s
        | None   -> failwithf "Expected string-like YAML value but got %A" element

    let tryDecodeIdReference value =
        match tryDecodeString value with
        | Some id -> Some (normalizeId id)
        | None ->
            match getMappings value with
            | [key, idValue] when key = "@id" || key = "id" -> tryDecodeString idValue |> Option.map normalizeId
            | _ -> None

    /// Verify the 'type' field, if present, equals the expected value.
    /// When processCoreOnly is false the check is skipped and the generic field-by-field
    /// decoding path is taken instead (lenient mode for decorated YAML).
    let checkType (processCoreOnly: bool) (expected: string) (element: YAMLElement) =
        if processCoreOnly then
            match tryGetTypeName element with
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

    let iterSequenceOrSingleton (handler: YAMLElement -> unit) value =
        match tryDecodeSequence value with
        | Some elems -> elems |> List.iter handler
        | None       -> handler value

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

    // Values and keys are created without an explicit scalar style so that YAMLicious
    // auto-resolves a safe style on write: plain when safe, a double-quoted scalar when
    // the content would otherwise break the YAML (e.g. '#', ": ", leading '*'/'@'/'-'),
    // and a block scalar for multiline content.
    let yamlValue (s: string) =
        YAMLElement.Value (YAMLContent.create(s))

    let yamlMap (pairs: (string * YAMLElement) list) =
        pairs
        |> List.map (fun (k, v) -> YAMLElement.Mapping (YAMLContent.create(k), v))
        |> YAMLElement.Object

    let yamlSeq (items: YAMLElement list) =
        YAMLElement.Sequence items

    // Replace non-alphanumeric characters with underscores to create a slug suitable for @id values.
    let makeIdSlug (s: string) : string =
        s |> String.map (fun c -> if Char.IsLetterOrDigit c || c = '-' then c else '_')

    /// Encode an @id reference: { "@id": "<id>" }
    let encodeRef (id: string) : YAMLElement =
        [ "@id", yamlValue id ] |> yamlMap

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
    type private KnownTypeRegistration =
        { Decode: bool -> YAMLElement -> obj
          DecodeMany: bool -> YAMLElement list -> obj
          Encode: obj -> YAMLElement option }

    let private knownTypes = Dictionary<string, KnownTypeRegistration>()

    let registerKnownType
        (name: string)
        (decoder: bool -> YAMLElement -> obj)
        (decoderMany: bool -> YAMLElement list -> obj)
        (encoder: obj -> YAMLElement option) =
        let registration =
            { Decode = decoder
              DecodeMany = decoderMany
              Encode = encoder }
        knownTypes.[name.ToLowerInvariant()] <- registration

    let registerKnownTypeTyped<'T>
        (name: string)
        (decoder: bool -> YAMLElement -> 'T)
        (encoder: obj -> YAMLElement option) =
        registerKnownType name
            (fun processCoreOnly value -> box (decoder processCoreOnly value))
            (fun processCoreOnly values -> values |> List.map (decoder processCoreOnly) |> ResizeArray |> box)
            encoder

    let private tryKnownTypeRegistration (name: string) =
        match knownTypes.TryGetValue(name.ToLowerInvariant()) with
        | true, registration -> Some registration
        | false, _ -> None

    let private tryDecodeKnownObject (value: YAMLElement) =
        tryGetTypeName value
        |> Option.bind (fun name ->
            tryKnownTypeRegistration name
            |> Option.map (fun registration -> registration.Decode false value))

    let private tryDecodeKnownCollection (values: YAMLElement list) =
        let typeNames = values |> List.map tryGetTypeName
        match typeNames with
        | first :: rest when first.IsSome && rest |> List.forall ((=) first) ->
            first
            |> Option.bind (fun name ->
                tryKnownTypeRegistration name
                |> Option.map (fun registration -> registration.DecodeMany false values))
        | _ -> None

    let private tryStringCollection (values: obj list) =
        if not values.IsEmpty && values |> List.forall (fun value -> value :? string) then
            values |> List.map (fun value -> value :?> string) |> ResizeArray |> box |> Some
        else None

    let private tryBoolCollection (values: obj list) =
        if not values.IsEmpty && values |> List.forall (fun value -> value :? bool) then
            values |> List.map (fun value -> value :?> bool) |> ResizeArray |> box |> Some
        else None

    let private tryIntCollection (values: obj list) =
        if not values.IsEmpty && values |> List.forall (fun value -> value :? int) then
            values |> List.map (fun value -> value :?> int) |> ResizeArray |> box |> Some
        else None

    let private tryDecimalCollection (values: obj list) =
        if not values.IsEmpty && values |> List.forall (fun value -> value :? decimal) then
            values |> List.map (fun value -> value :?> decimal) |> ResizeArray |> box |> Some
        else None

    let private tryDynamicObjCollection (values: obj list) =
        if not values.IsEmpty && values |> List.forall (fun value -> value :? DynamicObj) then
            values |> List.map (fun value -> value :?> DynamicObj) |> ResizeArray |> box |> Some
        else None

    let rec genericDecodeToObj (value: YAMLElement) : obj =
        match unwrapSingleObject value with
        | YAMLElement.Value v ->
            parseScalarToObj v.Value
        | YAMLElement.Sequence elements ->
            let hasTypeTag = elements |> List.exists (tryGetTypeName >> Option.isSome)
            match tryDecodeKnownCollection elements with
            | Some typed -> typed
            | None ->
                let values = elements |> List.map genericDecodeToObj
                if hasTypeTag then
                    box (ResizeArray<obj>(values))
                else
                match tryStringCollection values with
                | Some typed -> typed
                | None ->
                    match tryBoolCollection values with
                    | Some typed -> typed
                    | None ->
                        match tryIntCollection values with
                        | Some typed -> typed
                        | None ->
                            match tryDecimalCollection values with
                            | Some typed -> typed
                            | None ->
                                match tryDynamicObjCollection values with
                                    | Some typed -> typed
                                    | None -> box (ResizeArray<obj>(values))
        | YAMLElement.Object _ ->
            match tryDecodeKnownObject value with
            | Some typed -> typed
            | None ->
                let dynObj = DynamicObj()
                for (k, v) in getMappings value do
                    dynObj.SetProperty(k, genericDecodeToObj v)
                box dynObj
        | other ->
            failwithf "Unsupported YAML structure for generic overflow: %A" other

    #if !FABLE_COMPILER
    let (|SomeObj|_|) =
        // create generalized option type
        let ty = typedefof<option<_>>
        fun (a:obj) ->
            // Check for nulls otherwise 'a.GetType()' would fail
            if isNull a 
            then 
                None 
            else
                let aty = a.GetType()
                // Get option'.Value
                let v = aty.GetProperty("Value")
                if aty.IsGenericType && aty.GetGenericTypeDefinition() = ty then
                    // return value if existing
                    Some(v.GetValue(a, [| |]))
                else 
                    None
    #endif

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
        | _ ->
            match knownTypes.Values |> Seq.tryPick (fun registration -> registration.Encode value) with
            | Some encoded -> encoded
            | None ->
                match value with
        #if !FABLE_COMPILER
                | SomeObj o -> genericEncodeFromObj o
        #endif
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
        match tryDecodeIdReference value with
        | Some id -> Choice1Of2 id
        | None    -> Choice2Of2 (inlineDecoder value)

    // ── Overflow helpers ───────────────────────────────────────────────────────

    /// Store all YAML fields whose keys are not in knownFields as dynamic properties on obj.
    let applyOverflow (objectType : string) (processCoreOnly : bool) (knownFields: Set<string>) (obj: DynamicObj) (value: YAMLElement) =
        for (key, yamlVal) in getMappings value do
            let isTypeAlias = normalizeKey key = "@type" && knownFields |> Set.contains "type"
            if not (knownFields |> Set.contains key || isTypeAlias) then
                if processCoreOnly then
                    failwithf $"Unknown field '{key}' found in YAML for object of type '{objectType}'. Strict mode is enabled, so this is not allowed. Field value: '{yamlVal}'"
                else
                    obj.SetProperty(key, genericDecodeToObj yamlVal)
             


    /// Emit all dynamic properties whose lowercase key is not in knownPropertyNames.
    let emitOverflow (knownPropertyNames: Set<string>) (obj: DynamicObj) : (string * YAMLElement) list =
        obj.GetProperties(true)
        |> Seq.choose (fun kv ->
            let isKnown = knownPropertyNames |> Set.contains (kv.Key.ToLowerInvariant())
            let preceedingUnderscore = kv.Key.StartsWith("_")
            let isFableInternal = kv.Key.Length > 4 && kv.Key.StartsWith("init@")
            if not (isKnown || preceedingUnderscore || isFableInternal) then
                Some (kv.Key, genericEncodeFromObj kv.Value)
            else
                None)
        |> Seq.toList

    // ── Writer helper ──────────────────────────────────────────────────────────

    /// Render a YAMLElement to a YAML string with the given indentation width (default 2).
    let writeYaml (whitespace: int option) (element: YAMLElement) : string =
        YAMLicious.Writer.write element (Some (fun c -> { c with Whitespace = defaultArg whitespace 2 }))
