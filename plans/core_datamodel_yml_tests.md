# ProcessCore.YAML Test Plan

This document describes the test suite for the YAML support built into the `src/ProcessCore` library.
Tests live in `tests/ProcessCore.YAML.Tests/` and use [Fable.Pyxpecto](https://github.com/Freymaurer/Fable.Pyxpecto), matching the pattern of `tests/ProcessCore.Tests/`.

## Goals

- Round-trip every type: encode a constructed object → YAML string → decode → compare field by field.
- Verify that each decoder reads back what the encoder writes (encode ∘ decode = identity for the field set).
- Verify lenient mode (`processCoreOnly = false`) accepts unknown or mismatched `type` values.
- Verify strict mode (`processCoreOnly = true`) rejects mismatched `type` values.
- Verify overflow/extension fields survive a round-trip via `DynamicObj`.
- Verify the `fromYamlString` / `toYamlString` convenience helpers on each module.
- Verify the top-level `Decode.fromYamlString` / `Encode.toYamlString` entry points.
- Cover the real-world example files in `examples/isa/`.

## Running the Tests

```
dotnet .\tests\ProcessCore.YAML.Tests\bin\Debug\net10.0\ProcessCore.YAML.Tests.dll
```

## Test Structure

```
tests/ProcessCore.YAML.Tests/
    Main.fs                        ← entry point, collects all test lists
    Fixtures.fs                    ← shared YAML strings and pre-built objects
    Codecs/
        DefinedTerm.fs
        FormalParameter.fs
        PropertyValue.fs
        Material.fs
        Data.fs
        LabProtocol.fs
        LabProcess.fs
        Dataset.fs
    Integration/
        RoundTrip.fs               ← full graph encode → decode
        Examples.fs                ← parses files from examples/isa/
        Overflow.fs                ← DynamicObj extension field tests
    Mode/
        StrictMode.fs              ← processCoreOnly = true rejection tests
        LenientMode.fs             ← processCoreOnly = false acceptance tests
```

---

## Test Fixtures (`Fixtures.fs`)

### Object fixtures — reuse the same graph shapes as ProcessCore.Tests where possible

**Fixture PV** — a `PropertyValue` with all optional fields populated:
```fsharp
PropertyValue("Temperature", value = "37", unit = "°C",
              nameTAN = "PATO:0000146", valueTAN = "...", unitTAN = "UO:0000027",
              additionalType = "Parameter")
```

**Fixture DT** — a `DefinedTerm` with TAN and `inDefinedTermSet`:
```fsharp
DefinedTerm("cell growth", tan = "GO:0016049", inDefinedTermSet = "http://purl.obolibrary.org/obo/go.owl")
```

**Fixture FP** — a `FormalParameter` with `nameTAN` and `defaultValue`:
```fsharp
FormalParameter("temperature", nameTAN = "PATO:0000146", defaultValue = DefinedTerm("37°C"))
```

**Fixture Material** — `Material("Sample1", additionalType = "Sample")` with two `additionalProperty` entries.

**Fixture Data** — `Data("rawData1.csv", selector = "Sheet1", selectorFormat = "excel", encodingFormat = "text/csv")` with one `additionalProperty`.

**Fixture LabProtocol** — protocol with `name`, `description`, `version`, `url`, `intendedUse`, one parameter, one labEquipment, one additionalProperty.

**Fixture LabProcess** — process with `name`, one material input, one data output, `executesProtocol`, two `parameterValue` entries.

**Fixture Dataset** — `Dataset("DS-1")` containing the fixture LabProcess and one nested child `Dataset("DS-1/assay")`.

### YAML string fixtures

Inline YAML strings for each type used in decoder-only tests (decoding without encoding first).
These should match what the encoder produces so they can also serve as encoder reference outputs.

---

## 1 — DefinedTerm Codec (`Codecs/DefinedTerm.fs`)

| Test | Description |
|------|-------------|
| `encode name only` | Object with no TAN/set → YAML has `id = name`, `type = "DefinedTerm"`, `name`, no other keys |
| `encode all fields` | TAN and `inDefinedTermSet` appear in output |
| `decode name only` | YAML with only `name` → `DefinedTerm` with correct name, `TAN = None` |
| `decode all fields` | All fields decoded correctly |
| `decode inDefinedTermSet as inline object` | Nested object with `id` field → extracts the id string |
| `round-trip name only` | encode → string → decode → same name |
| `round-trip all fields` | encode → string → decode → all fields match |
| `fromYamlString` | Convenience function works end-to-end |
| `toYamlString default whitespace` | Produces valid, non-empty YAML |
| `toYamlString custom whitespace` | Indentation width is respected |
| `missing name field` | YAML without `name` → defaults to `""` |

---

## 2 — FormalParameter Codec (`Codecs/FormalParameter.fs`)

| Test | Description |
|------|-------------|
| `encode name only` | `id = name`, `type = "FormalParameter"` |
| `encode with nameTAN` | `nameTAN` key present |
| `encode with defaultValue` | Nested `DefinedTerm` object inline |
| `decode name only` | Name decoded, optionals `None` |
| `decode with defaultValue as inline object` | Nested DefinedTerm decoded |
| `decode with defaultValue as id-reference` | String value → `defaultValue = None` (ref left unresolved) |
| `round-trip name only` | |
| `round-trip all fields` | |

---

## 3 — PropertyValue Codec (`Codecs/PropertyValue.fs`)

| Test | Description |
|------|-------------|
| `encode name only` | `id = name`, `type = "PropertyValue"`, no optional keys |
| `encode all fields` | All 8 optional keys present |
| `encode instanceOf as inline FormalParameter` | Nested FP object |
| `decode name only` | All optionals `None` |
| `decode all fields` | All optional fields decoded |
| `decode value field as string even when YAML stores number` | `value: 37` → `Value = Some "37"` |
| `decode instanceOf as id-reference` | String ref → `instanceOf = None` |
| `round-trip all fields` | |

---

## 4 — Material Codec (`Codecs/Material.fs`)

| Test | Description |
|------|-------------|
| `encode name only` | `id = name`, `type = "Material"`, no `additionalProperty` key |
| `encode with additionalProperty` | Sequence of PropertyValue objects |
| `encode with additionalType` | `additionalType` key present |
| `decode name only` | `additionalProperty` empty, back-edges empty |
| `decode with additionalProperty` | PVs decoded and added |
| `decode with additionalProperty as id-references` | String refs skipped |
| `back-edges not in output` | Encoded YAML contains no `inputOf`, `outputOf` keys |
| `round-trip name only` | |
| `round-trip with additionalProperty` | |

---

## 5 — Data Codec (`Codecs/Data.fs`)

| Test | Description |
|------|-------------|
| `encode path only` | `id = path`, `type = "Data"`, no optional keys |
| `encode with selector` | `id = path + "#" + selector`, `selector` key present |
| `encode all fields` | All optional keys present |
| `decode path only` | `Path` decoded, all optionals `None` |
| `decode with selector and selectorFormat` | Both decoded |
| `id field goes to overflow` | YAML with `id` but no `path` → exception (path required); YAML with both `id` and `path` → `id` lands in overflow, `path` decoded |
| `decode with additionalProperty` | PVs decoded |
| `back-edges not in output` | No `inputOf`, `outputOf` keys |
| `round-trip path only` | |
| `round-trip all fields` | |

---

## 6 — LabProtocol Codec (`Codecs/LabProtocol.fs`)

| Test | Description |
|------|-------------|
| `encode minimal` | No name → `id = ""`, `type = "LabProtocol"` |
| `encode with name and url` | `id = url`, all provided fields present |
| `encode with parameters sequence` | Nested FP objects in `parameters` array |
| `encode with labEquipment sequence` | Nested PV objects |
| `encode with additionalProperty sequence` | |
| `encode with intendedUse` | Nested DefinedTerm inline |
| `decode minimal` | All optionals `None`, sequences empty |
| `decode all fields` | All fields decoded |
| `decode parameters as id-references` | Refs skipped |
| `decode intendedUse as id-reference` | Ref → `intendedUse = None` |
| `round-trip minimal` | |
| `round-trip all fields` | |

---

## 7 — LabProcess Codec (`Codecs/LabProcess.fs`)

| Test | Description |
|------|-------------|
| `encode name only` | `id = name`, `type = "LabProcess"`, no inputs/outputs/protocol |
| `encode with material input` | Material inline in `inputs` array |
| `encode with data output` | Data inline in `outputs` array |
| `encode with executesProtocol` | Nested LabProtocol inline |
| `encode with parameterValues` | Sequence of PV objects |
| `decode name only` | Inputs/outputs empty, protocol `None` |
| `decode material input` | Material decoded and added to inputs |
| `decode data output` | Data decoded and added to outputs |
| `decode data by "File" legacy type alias` | `type: File` → decoded as `Data` |
| `decode io as id-references` | String refs produce no IONode entries |
| `decode executesProtocol as inline object` | LabProtocol decoded |
| `decode executesProtocol as id-reference` | Ref → `executesProtocol = None` |
| `decode parameterValues` | PVs decoded |
| `back-edges not in output` | No `processOf` key |
| `round-trip name only` | |
| `round-trip with inputs and outputs` | |
| `round-trip with protocol and parameters` | |

---

## 8 — Dataset Codec (`Codecs/Dataset.fs`)

| Test | Description |
|------|-------------|
| `encode minimal` | `id = identifier`, `type = "Dataset"`, no sequences |
| `encode with processes` | Nested LabProcess objects in `processes` array |
| `encode with hasPart` | Nested child Dataset in `hasPart` array |
| `encode with additionalProperty` | PV sequence |
| `decode minimal` | Identifier decoded, all sequences empty |
| `id field goes to overflow` | YAML with `id` but no `identifier` → exception (identifier required); YAML with both `id` and `identifier` → `id` lands in overflow, `identifier` decoded |
| `decode with processes` | Processes decoded, `ProcessOf` back-edge set on each |
| `decode with hasPart as child datasets` | Child decoded, `PartOf` back-edge set |
| `decode hasPart with empty type defaults to Dataset` | Item without `type` field decoded as child Dataset |
| `decode with additionalProperty` | PVs decoded |
| `decode processes as id-references` | String refs skipped |
| `ProcessOf back-edge after decode` | `proc.ProcessOf = Some ds` after decoding |
| `PartOf back-edge after decode` | `child.PartOf = Some parent` after decoding |
| `back-edges not in output` | No `partOf` or `processOf` keys in YAML |
| `round-trip minimal` | |
| `round-trip with processes` | Processes survive encode → decode |
| `round-trip nested hasPart` | Nested datasets survive encode → decode |

---

## 9 — Round-Trip Integration (`Integration/RoundTrip.fs`)

These tests encode a fully-wired graph, decode it, and compare structure rather than reference equality.

| Test | Description |
|------|-------------|
| `linear graph round-trip` | Fixture graph from ProcessCore.Tests Fixture A: encode `DS-A` → YAML string → decode → same process names, same input/output names |
| `nested dataset round-trip` | Fixture D: parent with two child datasets → encode → decode → child identifier and process names intact |
| `parameterValues round-trip` | PVs with all optional fields survive encoding and decoding |
| `protocol round-trip` | LabProtocol with parameters, labEquipment, intendedUse all survive |
| `whitespace option` | `toYamlString (Some 4)` produces YAML with 4-space indentation that `fromYamlString` can parse back |
| `Decode.fromYamlString entry point` | Top-level `Decode.fromYamlString Dataset.decoder` works equivalently to `Dataset.fromYamlString` |
| `Encode.toYamlString entry point` | Top-level `Encode.toYamlString` works equivalently to per-module helper |

---

## 10 — Overflow / Extension Fields (`Integration/Overflow.fs`)

| Test | Description |
|------|-------------|
| `unknown field on DefinedTerm survives round-trip` | Set `dt.SetProperty("customTag", "value")` → encode → decode → `GetProperty("customTag") = "value"` |
| `unknown field on Material survives round-trip` | Same for `Material` |
| `unknown field on Dataset survives round-trip` | Same for `Dataset` |
| `unknown nested object survives round-trip` | Overflow value is a nested YAML object → decoded as `DynamicObj` |
| `unknown sequence survives round-trip` | Overflow value is a YAML sequence → decoded as `ResizeArray<obj>` |
| `known fields not re-emitted as overflow` | After decode, `GetProperties(true)` on decoded object does not double-emit known fields |

---

## 11 — Strict Mode (`Mode/StrictMode.fs`)

These tests call `decoder true` (i.e. `processCoreOnly = true`) directly.

| Test | Description |
|------|-------------|
| `correct type passes` | YAML with `type: DefinedTerm` → no exception |
| `wrong type on DefinedTerm raises` | `type: WrongType` → `failwithf` message contains both expected and actual type names |
| `wrong type on Material raises` | Same |
| `wrong type on Data raises` | Same |
| `wrong type on LabProtocol raises` | Same |
| `wrong type on LabProcess raises` | Same |
| `wrong type on Dataset raises` | Same |
| `missing type field passes` | YAML without a `type` key → no exception (absent is allowed) |

---

## 12 — Lenient Mode (`Mode/LenientMode.fs`)

These tests call `decoder false` (i.e. `processCoreOnly = false`).

| Test | Description |
|------|-------------|
| `decorated type on DefinedTerm accepted` | `type: schema:DefinedTerm` → decodes normally without error |
| `decorated type on Material accepted` | `type: bioschemas:Sample` → decodes as `Material` |
| `decorated type on Data accepted` | `type: schema:MediaObject` → decodes as `Data` |
| `decorated type on LabProtocol accepted` | `type: bioschemas:LabProtocol` |
| `decorated type on LabProcess accepted` | `type: bioschemas:LabProcess` |
| `decorated type on Dataset accepted` | `type: schema:Dataset` |
| `completely absent type accepted` | YAML with no `type` field → decodes normally |
| `unknown arbitrary type accepted` | `type: custom:Foo` → decodes, field goes into overflow |
| `field values still decoded` | Even in lenient mode, `name`, `identifier`, etc. are decoded correctly |

---

## 13 — Real-World Examples (`Integration/Examples.fs`)

### Spec compliance review

The three example files were reviewed against the YML schemas in `schemas/yml/`. The deviations are listed below. These files are **ISA-decorated** and use field names and type values that differ from the ProcessCore core schema — they must be parsed in lenient mode (`processCoreOnly = false`). Many field names currently used do not exist in the schemas and will land in `DynamicObj` overflow; a future ISA-decoration layer would map them.

#### `examples/isa/investigation.yml`

| Field / issue | Schema expectation | File value | Verdict |
|---|---|---|---|
| `type` | `const: "Dataset"` | `"Dataset"` | ✅ |
| `additionalType` | optional string | `"Investigation"` | ✅ |
| `id` | Overflow only | absent | ✅ Not mapped to any property; would flow to overflow if present |
| `identifier` | Required | `"ara_prot_2023"` | ✅ |
| `name` | Schema field | present | ✅ |
| `additionalProperty` (singular) | Schema uses `additionalProperty` | present | ✅ PVs decoded into typed list |
| `creators` | Not in core Dataset schema | present | ❌ Overflow |
| PropertyValue `type` values | `const: "PropertyValue"` | `"PropertyValue"` | ✅ |

#### `examples/isa/assay_proteomics.yml`

| Field / issue | Schema expectation | File value | Verdict |
|---|---|---|---|
| `type` | `const: "Dataset"` | `"Dataset"` | ✅ |
| `additionalType` | optional string | `"Assay"` | ✅ |
| `id` | Overflow only | absent | ✅ Not mapped to any property; would flow to overflow if present |
| `identifier` | Required | `"measurement1"` | ✅ |
| `creators` | Not in core schema | present | ❌ Overflow |
| `labProtocols` | Not in schema (no top-level protocol list) | present | ❌ Overflow |
| `propertyValues` | Not in schema (no top-level propertyValues) | present | ❌ Overflow |
| `processes[*].inputs` | Schema uses `inputs` | `inputs` | ✅ |
| `processes[*].outputs` | Schema uses `outputs` | `outputs` | ✅ |
| `processes[*].executesProtocol` | Schema uses `executesProtocol` | `executesProtocol` | ✅ |
| `processes[*].parameterValue` (singular) | Schema uses `parameterValue` | `parameterValue` | ✅ |
| `Material.additionalProperty` (singular) | Schema uses `additionalProperty` | `additionalProperty` | ✅ |
| `type: Material` + `additionalType: Source` | Schema uses `type: Material` with `additionalType` | `type: Material`, `additionalType: Source` | ✅ |
| `type: Data` on data outputs | Schema uses `type: Data` | `"Data"` | ✅ |
| Data `path` field | Schema uses `path` | `path: sample1.raw` | ✅ |

#### `examples/isa/datamap_proteomics.yml`

Skip for now, as this file is still a WIP

| Field / issue | Schema expectation | File value | Verdict |
|---|---|---|---|
| Top-level key `datacontexts` | No matching type in core schema | present | ❌ This is a Datamap decoration — not a `Dataset` at all |
| `type: DataContext` | Not a ProcessCore type | present | ❌ No decoder |
| Fields `explication`, `explicationTAN`, `objectType`, etc. | Not in any schema | present | ❌ All overflow / future decoration fields |

> **Conclusion:** `datamap_proteomics.yml` cannot be decoded as any current ProcessCore type. It is a pure Datamap decoration. The example tests for this file should only assert that the file can be loaded and its top-level structure inspected via raw YAML, not decoded into a typed object.

---

### Fixtures from example files

These fixtures are intended for use as **inline string literals** in the test project. They should be exact copies of the YAML content in the example files. As both files contain decorations, of course the parser result in lenient mode MUST contain he proper overflown fields, the parser in strict mode is expected to throw an exception.

---

### Tests

| Test | Description |
|------|-------------|
| `parse investigation fixture (spec-conformant)` | Inline fixture string → `Dataset.fromYamlString` succeeds; `Identifier = "ara_prot_2023"` |
| `investigation name field` | `Name = Some "Validation of Proteins in Arabidopsis thaliana"` |
| `investigation additionalType` | `AdditionalType = Some "Investigation"` |
| `investigation additionalProperty count` | Three `PropertyValue` entries decoded |
| `investigation PV names` | Names are `"latitude"`, `"longitude"`, `"aim"` |
| `parse assay fixture (spec-conformant)` | Inline fixture string → `Dataset.fromYamlString` succeeds; `Identifier = "measurement1"` |
| `assay process count` | One `LabProcess` decoded |
| `assay process name` | `"Growth"` |
| `assay process input name` | `"Base Culture"` with `AdditionalType = Some "Source"` |
| `assay process output name` | `"Cultivation Flask RT"` |
| `assay output additionalProperty` | One PV with `Name = "temperature"`, `Value = Some "25"` |
| `original investigation.yml lenient parse` | Load file content; parse with `Dataset.decoder false`; no exception; `identifier = "ara_prot_2023"`, `name` and `additionalProperty` decoded; `creators` in overflow |
| `original assay_proteomics.yml lenient parse` | Load file content; parse with `Dataset.decoder false`; no exception; `identifier = "measurement1"`; all 20 processes decoded with typed inputs/outputs; `creators`, `labProtocols`, `propertyValues` in overflow |
| `original datamap_proteomics.yml raw YAML load` | File can be read by `YAMLicious.Reader.read` without exception; top-level key `datacontexts` accessible as overflow on a bare `DynamicObj` |

