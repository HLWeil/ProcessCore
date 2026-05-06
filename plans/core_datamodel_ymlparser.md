# YAML Parser Plan — ProcessCore.YML

Encoder and decoder for all `ProcessCore` types to and from YAML, following the schemas defined in [`schemas/yml/`](../schemas/yml/).

---

## References

| Document | Role |
|----------|------|
| [schemas/yml/](../schemas/yml/) | Normative YAML schemas (JSON Schema draft-2020-12) for all core types |
| [src/ProcessCore/](../src/ProcessCore/) | Core datamodel implementation — the types being serialized |
| [plans/yml_plan.md](yml_plan.md) | YAML schema design decisions (flat structure, `@id` cross-references, `additionalProperties`) |
| [plans/core_datamodel.md](core_datamodel.md) | Core datamodel design — mutability, back-edges, DynamicObj, Fable requirements |
| [references/YAMLParser/ARCtrl.Yaml.fsproj](../references/YAMLParser/ARCtrl.Yaml.fsproj) | Reference implementation project file — shows YAMLicious dependency and compilation order |
---

## Overview

The goal is a dedicated project `src/ProcessCore.YML/` that provides:

1. A **Helpers** module if necessary — low-level primitives like `references/YAMLParser/ROCrate/Helpers.fs`.
2. Per-type **codec modules** (encoder + decoder pair), one module per `ProcessCore` type (Might be overuled by namespace level recursion requirements)
3. A **generic overflow decoder** that parses unknown YAML fields and stores them as dynamic properties via `DynamicObj`.
4. Top-level `Encode` / `Decode` entry-point modules.

The parser targets Fable compatibility (JS, Python, .NET) and uses [YAMLicious](https://github.com/nfdi4plants/YAMLicious) as the only YAML library dependency.

---

## Project Setup

**File:** `src/ProcessCore.YML/ProcessCore.YML.fsproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <!-- Compilation order matters in F# -->
    <Compile Include="Helpers.fs" />
    <Compile Include="DefinedTerm.fs" />
    <Compile Include="FormalParameter.fs" />
    <Compile Include="PropertyValue.fs" />
    <Compile Include="Material.fs" />
    <Compile Include="Data.fs" />
    <Compile Include="LabProtocol.fs" />
    <Compile Include="LabProcess.fs" />
    <Compile Include="Dataset.fs" />
    <Compile Include="Encode.fs" />
    <Compile Include="Decode.fs" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="YAMLicious" Version="*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../ProcessCore/ProcessCore.fsproj" />
  </ItemGroup>
</Project>
```

---

## Generic Overflow Decoder

**File:** `src/ProcessCore.YML/Helpers.fs` (or a dedicated `GenericOverflow.fs` section)

Any YAML fields that do not match a known property name for a given type are captured and stored on the object via its `DynamicObj` base class. This preserves round-trip fidelity for decoration-specific fields (e.g. `ISA` or `WorkflowRun` fields written by another tool) without the core parser needing to know about them.

```fsharp
open DynamicObj

/// Decode any YAML element into an obj suitable for DynamicObj.SetProperty.
/// Supports scalars, sequences (→ ResizeArray<obj>), and nested maps (→ Dictionary<string, obj>).
let rec genericDecodeToObj (value: YAMLElement) : obj =
    match unwrapSingleObject value with
    | YAMLElement.Value v ->
        parseScalarToObj v.Value
    | YAMLElement.Sequence elements ->
        elements
        |> List.map genericDecodeToObj
        |> ResizeArray<obj>
        |> box
    | YAMLElement.Object _ ->
        // Decode nested map as DynamicObj
        let dynObj = DynamicObj()
        for (k, v) in getMappings value do
            dynObj.SetProperty(k, genericDecodeToObj v)
        box dynObj
    | other ->
        failwithf "Unsupported YAML structure for generic overflow: %A" other
```

```fsharp
/// Encode a DynamicObj overflow value (scalar, ResizeArray, or Dictionary) back to YAMLElement.
let rec genericEncodeFromObj (value: obj) : YAMLElement =
    match value with
    | null                                    -> yamlValue "null"
    | :? string   as s                        -> yamlValue s
    | :? int      as i                        -> yamlValue (string i)
    | :? bool     as b                        -> yamlValue (if b then "true" else "false")
    | :? float    as f                        -> yamlValue (f.ToString(CultureInfo.InvariantCulture))
    | :? decimal  as d                        -> yamlValue (d.ToString(CultureInfo.InvariantCulture))
    | :? System.Collections.IEnumerable as l  ->
        l |> Seq.cast<obj> |> Seq.map genericEncodeFromObj |> Seq.toList |> yamlSeq
    | :? DynamicObj as dynObj ->
        dynObj.GetProperties(true)
        |> Seq.map (fun kv -> kv.Key, genericEncodeFromObj kv.Value)
        |> Seq.toList
        |> yamlMap
    | _ -> failwithf "Cannot encode overflow value %A" value
```

**Overflow application in a decoder** (pattern used in every type module):

```fsharp
// After all known fields are decoded, iterate remaining mappings:
for (key, yamlVal) in getMappings value do
    if not (knownFields |> Set.contains key) then
        let decoded = Helpers.genericDecodeToObj yamlVal
        obj.SetProperty(key, decoded)
```

**Overflow emission in an encoder** (pattern used in every type module):

```fsharp
// After all known fields are encoded, iterate DynamicObj properties:
for kv in (obj.GetProperties(true)) do
    let l = kv.Key.ToLower()
    if not (knownPropertyNames |> Set.contains l) then
        yield kv.Key, Helpers.genericEncodeFromObj kv.Value
```

The set `knownFields` / `knownPropertyNames` is a module-level constant in each codec module.

---

## Reference / Inline cross-cutting pattern

Every relational field in the YAML schema accepts either a **string `id` reference** or an **inline object**. A shared helper handles this throughout all decoders:

```fsharp
/// Decode a field that is either a string @id reference or an inline object.
/// Returns Choice1Of2(id) for references and Choice2Of2(decoded) for inline objects.
let decodeRefOrInline (inlineDecoder: YAMLElement -> 'a) (value: YAMLElement) : Choice<string, 'a> =
    match tryDecodeString value with
    | Some id -> Choice1Of2 id
    | None    -> Choice2Of2 (inlineDecoder value)
```

Callers resolve `Choice1Of2 id` lazily (pass through, leave as string, or look up in a registry passed from the outer document decoder) or immediately when the document is self-contained.

---

## Per-Type Codec Modules

Each file follows the same structure:

```
namespace ProcessCore.Yaml

module <TypeName> =

    open YAMLicious.YAMLiciousTypes
    open ProcessCore
    open Helpers

    let private knownFields = Set.ofList [ ... ]

    let decoder  (value : YAMLElement) : <TypeName> = ...
    let encoder  (obj   : <TypeName>)  : YAMLElement = ...

    let fromYamlString (s: string)                       : <TypeName> = ...
    let toYamlString   (whitespace: int option) (obj: <TypeName>) : string = ...
```

### DefinedTerm

**Known YAML fields:** `id`, `type`, `name`, `TAN`, `inDefinedTermSet`

**Decoder:**
- `name` → `DefinedTerm(name)`
- `TAN` → optional `.TAN`
- `inDefinedTermSet` → optional `.InDefinedTermSet` (string or inline object; take `.id` if object)
- Overflow → `.SetProperty`

**Encoder:**
- `id` field: if `TAN` is present use `TAN` as `id`, else use `name`
- `type` → constant `"schema:DefinedTerm"`
- `name`, `TAN`, `inDefinedTermSet` (optional)
- Overflow properties

---

### FormalParameter

**Known YAML fields:** `id`, `type`, `name`, `nameTAN`, `defaultValue`

**Decoder:**
- `name` → `FormalParameter(name)`
- `nameTAN` → optional `.NameTAN`
- `defaultValue` → `decodeRefOrInline DefinedTerm.decoder` → store if inline (id references are left unresolved at this level)
- Overflow → `.SetProperty`

**Encoder:**
- `id`: use `name`
- `type`: `"bioschemas:FormalParameter"`
- `name`, `nameTAN` (optional), `defaultValue` (optional, inline)
- Overflow properties

---

### PropertyValue

**Known YAML fields:** `id`, `type`, `additionalType`, `name`, `value`, `unit`, `nameTAN`, `valueTAN`, `unitTAN`, `instanceOf`

**Decoder:**
- `name` → `PropertyValue(name)`
- `value` → optional `.Value` (decoded as string even if YAML stores numeric)
- `unit`, `nameTAN`, `valueTAN`, `unitTAN`, `additionalType` → optional scalars
- `instanceOf` → `decodeRefOrInline FormalParameter.decoder` (inline only; id reference left unresolved)
- Overflow → `.SetProperty`

**Encoder:**
- `id`: if `NameTAN` is present use it, else use `name`
- `type`: `"schema:PropertyValue"`
- All optional scalar fields (omit if `None`)
- `instanceOf` if present (inline)
- Overflow properties

---

### Material

**Known YAML fields:** `id`, `type`, `additionalType`, `name`, `additionalProperty`

**Decoder:**
- `name` → `Material(name)`
- `additionalType` → optional
- `additionalProperty` → sequence decoded via `PropertyValue.decoder` per element (or string id skipped)
- Overflow → `.SetProperty`

**Encoder:**
- `id`: `name`
- `type`: `"bioschemas:Sample"`
- `additionalType` (optional)
- `additionalProperty` as sequence (inline, omit if empty)
- Overflow properties

> **Back-edges** (`InputOf`, `OutputOf`) are runtime graph state and are **never** serialized.

---

### Data

**Known YAML fields:** `id`, `type`, `additionalType`, `path`, `selector`, `selectorFormat`, `encodingFormat`, `additionalProperty`

**Decoder:**
- `path` (or `id` when `path` is absent) → `Data(path)`
- `selector`, `selectorFormat`, `encodingFormat`, `additionalType` → optional
- `additionalProperty` → sequence decoded via `PropertyValue.decoder`
- Overflow → `.SetProperty`

**Encoder:**
- `id`: path (+ selector if present, joined with `#`)
- `type`: `"schema:MediaObject"`
- `path`, optional fields
- `additionalProperty` sequence (omit if empty)
- Overflow properties

> **Back-edges** (`InputOf`, `OutputOf`) are never serialized.

---

### LabProtocol

**Known YAML fields:** `id`, `type`, `additionalType`, `name`, `description`, `version`, `url`, `intendedUse`, `parameters`, `labEquipment`, `additionalProperty`

**Decoder:**
- All optional; construct `LabProtocol()` then assign
- `intendedUse` → `decodeRefOrInline DefinedTerm.decoder`
- `parameters` → sequence of `decodeRefOrInline FormalParameter.decoder`
- `labEquipment` → sequence of `decodeRefOrInline PropertyValue.decoder`
- `additionalProperty` → sequence of `decodeRefOrInline PropertyValue.decoder`
- Overflow → `.SetProperty`

**Encoder:**
- `id` (optional, use `url` or `name`)
- `type`: `"bioschemas:LabProtocol"`
- All optional scalar fields
- `intendedUse` inline if present
- `parameters`, `labEquipment`, `additionalProperty` as sequences (omit if empty)
- Overflow properties

---

### LabProcess

**Known YAML fields:** `id`, `type`, `additionalType`, `name`, `inputs`, `outputs`, `executesProtocol`, `parameterValue`

**Decoder:**
- `name` (required) → `LabProcess(name)`
- `additionalType` → optional
- `inputs` → sequence; each element is `decodeRefOrInline` of either `Material.decoder` or `Data.decoder`; discriminate by `type` field value (`"bioschemas:Sample"` → Material, `"schema:MediaObject"` / `"File"` → Data); string ids are kept as unresolved references
- `outputs` → same pattern as `inputs`
- `executesProtocol` → `decodeRefOrInline LabProtocol.decoder`
- `parameterValue` → sequence of `decodeRefOrInline PropertyValue.decoder`
- Overflow → `.SetProperty`

> **Back-edges** (`ProcessOf`) are never deserialized directly; they are wired up by the `Dataset` decoder after constructing the full process list.

**Encoder:**
- `id`: `name`
- `type`: `"bioschemas:LabProcess"`
- `inputs`, `outputs` as sequences (inline; omit back-edge fields)
- `executesProtocol` inline if present
- `parameterValue` sequence (omit if empty)
- Overflow properties

---

### Dataset

**Known YAML fields:** `id`, `type`, `additionalType`, `identifier`, `name`, `description`, `processes`, `hasPart`, `additionalProperty`

**Decoder:**
- `identifier` (required) → `Dataset(identifier)`
- Other optional scalars
- `processes` → sequence of `decodeRefOrInline LabProcess.decoder`; after constructing all processes, wire back-edges (`process.ProcessOf <- dataset`)
- `hasPart` → sequence of `decodeRefOrInline` of either `Dataset.decoder` or `Data.decoder` (discriminate by `type`)
- `additionalProperty` → sequence of `decodeRefOrInline PropertyValue.decoder`
- Overflow → `.SetProperty`

**Encoder:**
- `id`: `identifier`
- `type`: `"schema:Dataset"`
- All optional scalars
- `processes` as sequence (inline)
- `hasPart` as sequence (inline)
- `additionalProperty` as sequence (omit if empty)
- Overflow properties

> **Back-edges** (`PartOf`) are never serialized; the parent knows its children via `HasPart`.

---

## Document-level reference resolution

When a YAML document uses `id` references instead of inline objects (e.g. a `processes` item that is just a string `id`), the `Dataset` decoder must resolve those references by first building an entity registry from all inline objects in the document, then substituting references.

**Two-pass strategy** (optional, activated when the top-level document contains a `registry` mapping):

```
registry:           # optional flat list of reusable entities
  - id: pv-001
    type: schema:PropertyValue
    name: Temperature
    value: "25"
    unit: degree Celsius

processes:
  - id: process-001
    ...
    parameterValue:
      - pv-001        # id reference resolved from registry
```

If no `registry` section is present, all references that cannot be resolved inline are left as `Choice1Of2 id` strings and are not wired up (the caller is responsible for resolution at a higher level).

---

## Entry-Point Modules

### `Encode.fs`

```fsharp
namespace ProcessCore.Yaml

module Encode =
    open YAMLicious
    open YAMLicious.YAMLiciousTypes
    open YAMLicious.Writer

    let DefaultWhitespace = 2
    let defaultWhitespace spaces = defaultArg spaces DefaultWhitespace

    let inline toYamlString whitespace (element: YAMLElement) =
        write element (Some (fun c -> { c with Whitespace = whitespace }))
```

### `Decode.fs`

```fsharp
namespace ProcessCore.Yaml

module Decode =
    open YAMLicious
    open YAMLicious.YAMLiciousTypes
    open YAMLicious.Reader

    let inline fromYamlString (decoder: YAMLElement -> 'a) (s: string) : 'a =
        read s |> decoder
```

---

## Codec Module Listing

| File | Namespace | Top-level functions exposed |
|------|-----------|----------------------------|
| `Helpers.fs` | `ProcessCore.Yaml.Helpers` | `getMappings`, `requireField`, `tryGetField`, `decodeString`, `yamlValue`, `yamlMap`, `yamlSeq`, `genericDecodeToObj`, `genericEncodeFromObj`, `decodeRefOrInline` |
| `DefinedTerm.fs` | `ProcessCore.Yaml.DefinedTerm` | `decoder`, `encoder`, `fromYamlString`, `toYamlString` |
| `FormalParameter.fs` | `ProcessCore.Yaml.FormalParameter` | `decoder`, `encoder`, `fromYamlString`, `toYamlString` |
| `PropertyValue.fs` | `ProcessCore.Yaml.PropertyValue` | `decoder`, `encoder`, `fromYamlString`, `toYamlString` |
| `Material.fs` | `ProcessCore.Yaml.Material` | `decoder`, `encoder`, `fromYamlString`, `toYamlString` |
| `Data.fs` | `ProcessCore.Yaml.Data` | `decoder`, `encoder`, `fromYamlString`, `toYamlString` |
| `LabProtocol.fs` | `ProcessCore.Yaml.LabProtocol` | `decoder`, `encoder`, `fromYamlString`, `toYamlString` |
| `LabProcess.fs` | `ProcessCore.Yaml.LabProcess` | `decoder`, `encoder`, `fromYamlString`, `toYamlString` |
| `Dataset.fs` | `ProcessCore.Yaml.Dataset` | `decoder`, `encoder`, `fromYamlString`, `toYamlString` |

---

## Overflow / DynamicObj Consistency Rules

1. The set of "known" field names for each type is defined as a module-level `Set<string>` constant. Any YAML key **not** in this set is passed to `genericDecodeToObj` and stored via `DynamicObj.SetProperty(key, value)`.

2. During encoding, `DynamicObj.GetProperties(true)` is iterated **after** all known properties are emitted. Keys that are lowercase shadows of known typed properties (e.g. `"name"`, `"value"`) are skipped to avoid duplication.

3. Because `DynamicObj.GetProperties` is reflection-based on .NET and property-bag-based on Fable, the Fable implementation of this pattern must be verified against the DynamicObj Fable adapter. Specifically, the `#if !FABLE_COMPILER` guard used in the ROCrate reference for `SomeObj` pattern matching must be applied here too if `option<obj>` values end up in the property bag.

4. **No back-edge fields** (`InputOf`, `OutputOf`, `ProcessOf`, `PartOf`) are ever stored in the dynamic property bag or serialized. These are purely runtime-managed.

---

## Fable Compatibility Notes

- Use `ResizeArray` for all decoded sequences (not `list` or `array`).
- Avoid `System.Reflection` at runtime; the `#if !FABLE_COMPILER` guard on `SomeObj` from the reference implementation should be carried over.
- `System.Collections.Generic.Dictionary<string, obj>` is acceptable in the generic overflow helper; on Fable/JS it compiles to a plain object.
- Float formatting must use `CultureInfo.InvariantCulture` for cross-platform consistency.
- `YAMLicious.Reader.read` and `YAMLicious.Writer.write` are the only library entry points needed.

---

## Out of Scope

- Validation against the JSON Schema files in `schemas/yml/` — that is a separate validation layer.
- ISA / WorkflowRun decoration-specific codec logic — decoration codecs will live in their own projects and reuse these base codecs.
- YAML multi-document streams (`---` separator) — only single-document input is supported (same restriction as the ROCrate reference).
- Schema version negotiation.
