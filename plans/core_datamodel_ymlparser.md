# YAML Parser Plan — ProcessCore.Yaml

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

The YAML implementation now lives inside `src/ProcessCore/` and provides:

1. A **Helpers** module if necessary — low-level primitives like `references/YAMLParser/ROCrate/Helpers.fs`.
2. Per-type **codec modules** (encoder + decoder pair), one module per `ProcessCore` type (Might be overuled by namespace level recursion requirements)
3. A **generic overflow decoder** that parses unknown YAML fields and stores them as dynamic properties via `DynamicObj`.
4. Top-level `Encode` / `Decode` entry-point modules.

The parser targets Fable compatibility (JS, Python, .NET) and uses [YAMLicious](https://github.com/nfdi4plants/YAMLicious) as the only YAML library dependency.

---

## Project Setup

**File:** `src/ProcessCore/ProcessCore.fsproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <Import Project="ProcessCore.Common.props" />

  <ItemGroup>
    <!-- Core model files first, then YAML codec files. Compilation order matters in F#. -->
    <Compile Include="DefinedTerm.fs" />
    <Compile Include="FormalParameter.fs" />
    <Compile Include="Annotation.fs" />
    <Compile Include="Administrative.fs" />
    <Compile Include="FragmentSelector.fs" />
    <Compile Include="Graph.fs" />
    <Compile Include="YML\Helpers.fs" />
    <Compile Include="YML\Decode.fs" />
    <Compile Include="YML\Encode.fs" />
    <Compile Include="YML\DefinedTerm.fs" />
    <Compile Include="YML\FormalParameter.fs" />
    <Compile Include="YML\Annotation.fs" />
    <Compile Include="YML\Sample.fs" />
    <Compile Include="YML\Data.fs" />
    <Compile Include="YML\Recipe.fs" />
    <Compile Include="YML\Process.fs" />
    <Compile Include="YML\Organization.fs" />
    <Compile Include="YML\Agent.fs" />
    <Compile Include="YML\ScholarlyArticle.fs" />
    <Compile Include="YML\DataContext.fs" />
    <Compile Include="YML\Dataset.fs" />
    <Compile Include="Table.fs" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Fable.Core" />
    <PackageReference Include="YAMLicious" />
    <PackageReference Include="DynamicObj" />
  </ItemGroup>
</Project>
```

---

## Generic Overflow Decoder

**File:** `src/ProcessCore/YML/Helpers.fs` (or a dedicated `GenericOverflow.fs` section)

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

## Indexed Object Sections

Reusable objects can be defined once in top-level index sections and referenced elsewhere by `@id`. This avoids repeating shared `Annotation` and `Recipe` objects in larger assay-style YAML files.

References use the same compact mapping shape as indexed objects:

```yaml
recipes:
  - "@id": "#Recipe_Cell_Lysis"
    type: Recipe
    components:
      - "@id": "#Component_centrifuge_Eppendorf_5420"

annotations:
  - "@id": "#ParameterValue_time_10_minute"
    type: Annotation
    additionalType: ParameterValue
    name: time
    value: 10
    unit: minute

processes:
  - type: Process
    name: Cell Lysis
    executesRecipe:
      "@id": "#Recipe_Cell_Lysis"
    parameterValue:
      - "@id": "#ParameterValue_time_10_minute"
```

Indexed sections currently cover:

- `annotations`: reusable `Annotation` objects, including parameter values, characteristics, factors, components, and additional properties.
- `recipes`: reusable `Recipe` objects that processes can reference through `executesRecipe`.

Inline objects remain valid wherever references are accepted. `@id` values should be stable within the document; fragment identifiers are preferred for local objects, while absolute URLs are suitable for externally identified objects.

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

### Annotation

**Known YAML fields:** `id`, `type`, `additionalType`, `name`, `value`, `unit`, `nameTAN`, `valueTAN`, `unitTAN`, `instanceOf`

**Decoder:**
- `name` → `Annotation(name)`
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

### Sample

**Known YAML fields:** `id`, `type`, `additionalType`, `name`, `additionalProperty`

**Decoder:**
- `name` → `Sample(name)`
- `additionalType` → optional
- `additionalProperty` → sequence decoded via `Annotation.decoder` per element (or string id skipped)
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
- `additionalProperty` → sequence decoded via `Annotation.decoder`
- Overflow → `.SetProperty`

**Encoder:**
- `id`: path (+ selector if present, joined with `#`)
- `type`: `"schema:MediaObject"`
- `path`, optional fields
- `additionalProperty` sequence (omit if empty)
- Overflow properties

> **Back-edges** (`InputOf`, `OutputOf`) are never serialized.

---

### Recipe

**Known YAML fields:** `id`, `type`, `additionalType`, `name`, `description`, `version`, `url`, `intendedUse`, `parameters`, `components`, `additionalProperty`

**Decoder:**
- All optional; construct `Recipe()` then assign
- `intendedUse` → `decodeRefOrInline DefinedTerm.decoder`
- `parameters` → sequence of `decodeRefOrInline FormalParameter.decoder`
- `components` → sequence of `decodeRefOrInline Annotation.decoder`, resolved through an optional annotation registry
- `additionalProperty` → sequence of `decodeRefOrInline Annotation.decoder`
- Overflow → `.SetProperty`

**Encoder:**
- `id` (optional, use `url` or `name`)
- `type`: `"bioschemas:LabProtocol"`
- All optional scalar fields
- `intendedUse` inline if present
- `parameters`, `components`, `additionalProperty` as sequences (omit if empty)
- Overflow properties

---

### Process

**Known YAML fields:** `id`, `type`, `additionalType`, `name`, `inputs`, `outputs`, `executesRecipe`, `parameterValue`

**Decoder:**
- `decoder`, `decoderWithResolvers`, and `fromYamlString` return `ResizeArray<Process>` because one YAML mapping may encode several in-memory edges.
- `name` is required and copied to every expanded `Process(name)`; `additionalType`, the resolved `executesRecipe`, `parameterValue`, and overflow properties are likewise copied.
- `inputs` and `outputs` are decoded into positional arrays of `IONode option`. Inline values discriminate `Sample` and `Data` by `type`; `File` remains a legacy alias for `Data`, and missing type defaults to `Sample`. An unresolved id reference preserves its position as `None`.
- Create `max(inputs.Count, outputs.Count, 1)` processes. Process `i` receives `inputs[i]` and `outputs[i]` when present, otherwise `None`. Thus equal arrays become paired edges, unequal arrays are padded, and an empty mapping becomes one metadata-only process.
- Clone mutable recipe, parameter, and term objects for each expanded process so editing one edge does not mutate its siblings.
- Apply overflow with the normal strict/lenient rules to every expanded process.

> **Back-edges** (`ProcessOf`, `InputOf`, and `OutputOf`) are never deserialized directly. The process decoder establishes endpoint back-edges locally; the `Dataset` decoder calls `AddProcess` for every expanded process to set ownership and canonicalize nodes through the root registry.

**Encoder:**
- `type`: `"Process"`; `name` is required and `additionalType` is emitted when present
- standalone `encoder` writes the singular endpoints as omitted or one-element `inputs`/`outputs` sequences (inline; omit back-edge fields)
- `encoderMany` writes a non-empty, already-equivalent process group as one mapping by appending singular endpoints to the plural arrays in encounter order
- `executesRecipe` inline if present
- `parameterValue` sequence (omit if empty)
- Overflow properties

---

### Dataset

**Known YAML fields:** `type`, `additionalType`, `identifier`, `title`, `description`, `license`, `datePublished`, `dateCreated`, `dateModified`, `processes`, `hasPart`, `dataFiles`, `agents`, `citations`, `dataContexts`, `additionalProperty`, `ArcPath`, and the indexed `annotations` section. Lenient documents may additionally carry `recipes` and other decoration fields through overflow handling.

**Decoder:**
- `identifier` (required) → `Dataset(identifier)`
- Decode the optional administrative scalars (`title`, `description`, `additionalType`, license, and publication/creation/modification dates) directly onto the dataset.
- `processes` → sequence of mappings or references. A mapping is passed to the collection-returning Process decoder, and every expanded result is flattened into the dataset through `AddProcess`; resolvable indexed annotations/recipes are supplied to the process decoder.
- `hasPart` → inline Dataset or Data values discriminated by `type`/`path`; datasets use `AddPart`, while data values enter `DataFiles`. Empty type defaults to Dataset. Unresolved string references are skipped.
- Decode the explicit `dataFiles`, `agents`, `citations`, and `dataContexts` collections with their corresponding codecs and add methods.
- `additionalProperty` → sequence of `decodeRefOrInline Annotation.decoder`
- Overflow → `.SetProperty`

**Encoder:**
- `identifier`: the dataset identifier (there is no generated dataset `id` field)
- `type`: `"Dataset"`
- All present optional administrative scalars
- Group `Processes` only at this boundary. Equivalence is the structural non-I/O encoding (`name`, `additionalType`, recipe, parameters, overflow) plus the exact endpoint-presence shape `(Input.IsSome, Output.IsSome)`. Preserve first-group order and process encounter order, then encode each group with `Process.encoderMany`.
- Keep both-sided, input-only, output-only, and endpoint-free processes in separate groups so omitted entries never shift positional pairing.
- In indexed mode, grouped process payloads still emit annotation and recipe references and populate the top-level registries.
- `hasPart` as sequence (inline)
- `dataFiles`, `agents`, `citations`, and `dataContexts` as sequences when non-empty
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
| `Annotation.fs` | `ProcessCore.Yaml.Annotation` | `decoder`, `encoder`, `fromYamlString`, `toYamlString` |
| `Sample.fs` | `ProcessCore.Yaml.Sample` | `decoder`, `encoder`, `fromYamlString`, `toYamlString` |
| `Data.fs` | `ProcessCore.Yaml.Data` | `decoder`, `encoder`, `fromYamlString`, `toYamlString` |
| `Recipe.fs` | `ProcessCore.Yaml.Recipe` | `decoder`, `encoder`, `fromYamlString`, `toYamlString` |
| `Process.fs` | `ProcessCore.Yaml.Process` | collection-returning `decoder`/`fromYamlString`, `decoderWithResolvers`, singular `encoder`, grouped `encoderMany`, `groupingKey`, `toYamlString` |
| `Organization.fs`, `Agent.fs`, `ScholarlyArticle.fs`, `DataContext.fs` | corresponding `ProcessCore.Yaml.*` modules | administrative type encoders/decoders and string helpers |
| `Dataset.fs` | `ProcessCore.Yaml.Dataset` | flattening `decoder`, grouped/index-aware `encoder`, `fromYamlString`, `toYamlString`, `toYamlStringIndexed` |

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
