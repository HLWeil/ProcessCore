# ProcessCore Test Plan

This document describes the test suite for the `src/ProcessCore` library. Tests live in `tests/ProcessCore.Tests/` and use [Fable.Pyxpecto](https://github.com/Freymaurer/Fable.Pyxpecto) (already present in the project).

## Goals

- Cover every type defined in the library.
- Cover every public member (property, CRUD method, query method).
- Cover the three invariants called out by the design plan:
  - **Back-edge consistency** — adding/removing I/O or a process updates back-edges atomically.
  - **Deduplication** — adding an identical object a second time is silently ignored.
  - **Object identity** — each type's `Equals` / `GetHashCode` follows the spec.
- Cover all graph traversal directions (undirected, upstream, downstream) and the optional `scope` parameter.
- Cover all four Annotation sources.
- Cover all three querying use-cases from the spec.

## Test Structure

Each logical group maps to one F# file inside `tests/ProcessCore.Tests/`:

```
tests/ProcessCore.Tests/
    Main.fs                  ← entry point (already exists)
    Synchronization.fs       ← (already exists, placeholder)
    Fixtures.fs              ← shared graph fixtures (must be first)
    Types/
        Annotation.fs
        DefinedTerm.fs
        FormalParameter.fs
        Sample.fs
        Data.fs
        Recipe.fs
        Process.fs
        Dataset.fs
        IONode.fs
    Graph/
        BackEdges.fs
        Deduplication.fs
        Traversal.fs
        AnnotationSources.fs
        DatasetQueries.fs
        ProcessGraphQueries.fs
        PathQueries.fs
    Table/
        CompositeTypes.fs
        TableAux.fs
        TableRead.fs
        TableWrite.fs
        TablesApi.fs
```

The `Main.fs` entry point collects and runs all test lists.

---

## Test Fixtures

Two shared fixtures should be defined in a `Fixtures.fs` module and reused across suites to keep tests focused on assertions rather than construction.

### Fixture A — Linear Chain

```
Source1 --[p1]--> Sample1 --[p2]--> Sample2 --[p3]--> rawData1.csv
```

- Three `Process` instances: `p1`, `p2`, `p3`
- `p1`: input `Sample("Source1", AdditionalType="Source")`, output `Sample("Sample1", AdditionalType="Sample")`; protocol `"extraction"` with `IntendedUse = DefinedTerm("cell growth")`; `ParameterValue` `[temperature=37°C (unit), rpm=200 (unitized)]`
- `p2`: input `Sample("Sample1")`, output `Sample("Sample2")`; protocol `"digestion"`; `ParameterValue` `[enzyme="Trypsin" (term, with TAN)]`
- `p3`: input `Sample("Sample2")`, output `Data("rawData1.csv")`; no protocol
- All three collected in a single `Dataset("DS-A")`

### Fixture B — Branching Graph

```
Source1 --[p1]--> Sample1 --[p2]--> SampleA
                          --[p3]--> SampleB
```

- Two output branches from `Sample1`
- All three processes in `Dataset("DS-B")`

### Fixture C — Merging Graph

```
Source1 --[p1]--> Sample1 \
                           [p3]--> FinalSample
Source2 --[p2]--> Sample2 /
```

- Two input paths converging into `p3`
- All three processes in `Dataset("DS-C")`

### Fixture D — Nested Datasets

```
Dataset("parent")
  ├─ Dataset("child1")  [p1: Source1 → Sample1]
  └─ Dataset("child2")  [p2: Sample1 → rawData1.csv]
```

- `Sample1` is shared across both child datasets (same object instance).

---

## 1 — Types

### 1.1 Annotation (`Types/Annotation.fs`)

| Test | Description |
|------|-------------|
| `construction with name only` | `Annotation("Temperature")` has `Name = "Temperature"`, all optional fields `None` |
| `construction with all fields` | Constructor overload or property setters populate `Value`, `Unit`, `NameTAN`, `ValueTAN`, `UnitTAN`, `AdditionalType`, `InstanceOf` |
| `equality same values` | Two PVs with same name/value/unit/nameTAN are equal |
| `equality ignores other fields` | Two PVs differing only in `ValueTAN`, `UnitTAN`, `AdditionalType` are still equal |
| `inequality different name` | Differing `Name` → not equal |
| `inequality different value` | Differing `Value` → not equal |
| `inequality different unit` | Differing `Unit` → not equal |
| `inequality different nameTAN` | Differing `NameTAN` → not equal |
| `hash consistency` | Equal objects have equal hash codes |
| `mutation` | Setting `Value` after construction is reflected on next read |

### 1.2 DefinedTerm (`Types/DefinedTerm.fs`)

| Test | Description |
|------|-------------|
| `construction with name` | All optional fields `None` |
| `equality all fields match` | Name + TAN + InDefinedTermSet all equal → equal |
| `inequality missing TAN` | One with TAN, one without → not equal |
| `default constructor` | `DefinedTerm()` has `Name = ""` |

### 1.3 FormalParameter (`Types/FormalParameter.fs`)

| Test | Description |
|------|-------------|
| `equality by name only` | Two FPs with same name (but different TAN/default) are equal |
| `inequality different name` | Different names → not equal |
| `DefaultValue field` | Setting `DefaultValue` to a `DefinedTerm` is readable |

### 1.4 Sample (`Types/Sample.fs`)

| Test | Description |
|------|-------------|
| `equality by name` | Two `Sample("Sample1")` are equal regardless of other fields |
| `inequality different name` | Different names → not equal |
| `AddAdditionalProperty deduplicates` | Adding identical PV twice → one entry |
| `RemoveAdditionalProperty` | PV is removed; non-existent remove is a no-op |
| `InputOf / OutputOf start empty` | Fresh sample has empty back-edge lists |

### 1.5 Data (`Types/Data.fs`)

| Test | Description |
|------|-------------|
| `equality by path + selector` | Same path + same selector → equal |
| `equality selector None vs Some ""` | Treated as different (spec: selector is exact) |
| `inequality different path` | Different path → not equal |
| `AddAdditionalProperty deduplicates` | Adding identical PV twice → one entry |
| `RemoveAdditionalProperty` | PV is removed |
| `InputOf / OutputOf start empty` | Fresh data node has empty back-edge lists |
| `EncodingFormat and AdditionalType fields` | Settable and readable |

### 1.6 Recipe (`Types/Recipe.fs`)

| Test | Description |
|------|-------------|
| `equality by name + version` | Name and version both must match |
| `inequality different version` | Same name, different version → not equal |
| `AddParameter deduplicates by name` | Adding FP with same name twice → one entry |
| `RemoveParameter` | FP removed; non-existent remove is no-op |
| `TryGetParameter found / not found` | Returns `Some` / `None` |
| `AddLabEquipment deduplicates` | Identical PV added twice → one entry |
| `RemoveLabEquipment` | PV removed |
| `AddAdditionalProperty deduplicates` | Identical PV added twice → one entry |
| `optional name constructor` | `Recipe()` has `Name = None` |

### 1.7 Process (`Types/Process.fs`)

| Test | Description |
|------|-------------|
| `equality by name` | Two processes with same name are equal |
| `AddInput deduplicates sample` | `AddInputSample` with identical sample twice → one input |
| `AddInput deduplicates data` | Same for Data nodes |
| `RemoveInput sample` | Input removed; back-edge cleared |
| `RemoveInput data` | Same for Data |
| `AddOutput deduplicates sample` | Identical sample twice → one output |
| `RemoveOutput sample` | Output removed; back-edge cleared |
| `AddParameterValue deduplicates` | Identical PV twice → one entry |
| `RemoveParameterValue` | PV removed |
| `TryGetParameterValue found / not found` | Returns `Some` / `None` |
| `GetParameterValue throws if missing` | Raises exception |
| `InputSamples / InputData filters correctly` | Mixed inputs → typed helpers return only the correct type |
| `OutputSamples / OutputData` | Same for outputs |
| `ProtocolParameters returns empty without protocol` | No protocol → empty |
| `ProtocolParameters delegates to protocol` | Returns protocol parameters |
| `AnnotationsByName — parameter source` | PV in `ParameterValue` with matching name returned |
| `AnnotationsByName — input node source` | PV in input sample `AdditionalProperty` returned |
| `AnnotationsByName — output node source` | PV in output sample `AdditionalProperty` returned |
| `AnnotationsByName — protocol component source` | PV in `LabEquipment` returned |
| `AnnotationsByName — no match returns empty` | Unknown name → empty |

### 1.8 Dataset (`Types/Dataset.fs`)

| Test | Description |
|------|-------------|
| `equality by identifier` | Same identifier → equal |
| `default constructor` | `Dataset()` has `Identifier = ""` |
| `AddProcess sets ProcessOf back-edge` | After `AddProcess`, `proc.ProcessOf = Some dataset` |
| `AddProcess deduplicates` | Adding same process twice → one entry |
| `RemoveProcess clears ProcessOf` | After `RemoveProcess`, `proc.ProcessOf = None` |
| `TryGetProcess found / not found` | Finds by name |
| `GetProcess found / not found` | Finds or raises |
| `AddPart sets PartOf back-edge` | After `AddPart`, `child.PartOf = Some parent` |
| `AddPart deduplicates` | Adding same child twice → one entry |
| `RemovePart clears PartOf` | After `RemovePart`, `child.PartOf = None` |
| `TryGetPart found / not found` | Finds child by identifier |
| `AddAdditionalProperty deduplicates` | Identical PV twice → one entry |

---

## 2 — Back-Edges (`Graph/BackEdges.fs`)

These tests verify the **eager consistency** contract.

| Test | Description |
|------|-------------|
| `AddInput — sample InputOf updated` | After `p.AddInputSample(m)`, `m.InputOf` contains `p` |
| `AddInput — data InputOf updated` | Same for Data nodes |
| `RemoveInput — sample InputOf cleared` | After removal, `m.InputOf` no longer contains `p` |
| `AddOutput — sample OutputOf updated` | After `p.AddOutputSample(m)`, `m.OutputOf` contains `p` |
| `RemoveOutput — sample OutputOf cleared` | After removal, `m.OutputOf` no longer contains `p` |
| `AddOutput — data OutputOf updated` | Same for Data |
| `RemoveOutput — data OutputOf cleared` | Same for Data |
| `two processes sharing a node` | Node's `InputOf` contains both processes |
| `AddProcess — ProcessOf set` | After `ds.AddProcess(p)`, `p.ProcessOf = Some ds` |
| `RemoveProcess — ProcessOf cleared` | After `ds.RemoveProcess(p)`, `p.ProcessOf = None` |
| `AddPart — PartOf set` | After `parent.AddPart(child)`, `child.PartOf = Some parent` |
| `RemovePart — PartOf cleared` | After `parent.RemovePart(child)`, `child.PartOf = None` |
| `re-adding after removal re-establishes back-edge` | Remove then add → back-edge is present again |

---

## 3 — Deduplication (`Graph/Deduplication.fs`)

| Test | Description |
|------|-------------|
| `AddInput: identical node not doubled` | Using Fixture A: adding `Sample1` again to `p2` inputs does not create a second entry |
| `AddOutput: identical node not doubled` | Adding `Sample1` again to `p1` outputs → still one entry |
| `shared node is same object instance` | After deduplication the returned element is `===` the original object (reference equality via F# `obj.ReferenceEquals`) |
| `AddProcess: duplicate ignored` | Adding `p1` to `DS-A` twice → `ds.Processes.Count` stays the same |
| `AddPart: duplicate child ignored` | Adding child dataset twice → `parent.HasPart.Count` stays the same |
| `AddParameterValue: duplicate ignored` | Identical PV twice → count unchanged |
| `AddParameter (protocol): duplicate ignored` | Same FP name twice → count unchanged |
| `AddLabEquipment: duplicate ignored` | Same PV twice → count unchanged |

---

## 4 — IONode (`Types/IONode.fs`)

| Test | Description |
|------|-------------|
| `SampleNode.Key()` | Returns `"M:<name>"` |
| `DataNode.Key() without selector` | Returns `"D:<path>"` |
| `DataNode.Key() with selector` | Returns `"D:<path><selector>"` |
| `EqualTo same sample` | `SampleNode(m1).EqualTo(SampleNode(m2))` where `m1 = m2` → true |
| `EqualTo different types` | `SampleNode` vs `DataNode` → false |
| `GetInputOf / GetOutputOf delegate` | Returns the underlying sample/data back-edge list |
| `IsRootNode: no predecessor in graph` | `Source1` in Fixture A → `IsRootNode` = true |
| `IsRootNode: has predecessor` | `Sample1` in Fixture A → `IsRootNode` = false |
| `IsFinalNode: no successor in graph` | `rawData1.csv` in Fixture A → `IsFinalNode` = true |
| `IsFinalNode: has successor` | `Sample1` in Fixture A → `IsFinalNode` = false |
| `IsRootNode: scoped to one dataset` | Fixture D: `Sample1` is root within `child2` but not in the full graph |

---

## 5 — Graph Traversal (`Graph/Traversal.fs`)

Using Fixture A (linear) and Fixture B (branching) and Fixture C (merging).

### 5.1 AllConnectedProcesses / AllConnectedNodes (undirected)

| Test | Description |
|------|-------------|
| `AllConnectedProcesses from root node` | `Source1` in A → all three processes `{p1, p2, p3}` |
| `AllConnectedProcesses from mid node` | `Sample1` in A → all three processes |
| `AllConnectedProcesses from leaf` | `rawData1.csv` in A → all three processes |
| `AllConnectedProcesses — branching` | `Source1` in B → all three processes including both branches |
| `AllConnectedNodes from root` | `Source1` in A → `{Sample1, Sample2, rawData1.csv}` (excludes self) |
| `AllConnectedProcesses with scope` | Scope to `{p1}` → only `{p1}` returned |
| `AllConnectedNodes with scope` | Scope to `{p1}` → only nodes connected through `p1` |

### 5.2 UpstreamProcesses / UpstreamNodes

| Test | Description |
|------|-------------|
| `UpstreamProcesses from leaf` | `rawData1.csv` in A → `{p3, p2, p1}` |
| `UpstreamProcesses from mid` | `Sample2` in A → `{p2, p1}` |
| `UpstreamProcesses from root` | `Source1` in A → empty |
| `UpstreamNodes from leaf` | `rawData1.csv` in A → `{Sample2, Sample1, Source1}` |
| `UpstreamNodes from mid` | `Sample1` in A → `{Source1}` |
| `UpstreamProcesses with scope` | Scope to `{p1, p2}` while querying from `rawData1.csv` → `{p2, p1}` |

### 5.3 DownstreamProcesses / DownstreamNodes

| Test | Description |
|------|-------------|
| `DownstreamProcesses from root` | `Source1` in A → `{p1, p2, p3}` |
| `DownstreamProcesses from mid` | `Sample1` in A → `{p2, p3}` |
| `DownstreamProcesses from leaf` | `rawData1.csv` → empty |
| `DownstreamNodes from root` | `Source1` in A → `{Sample1, Sample2, rawData1.csv}` |
| `DownstreamNodes — branching` | `Sample1` in B → `{SampleA, SampleB}` |

### 5.4 RootNodes / FinalNodes (on IONode)

| Test | Description |
|------|-------------|
| `RootNodes from leaf` | Walk upstream from `rawData1.csv` in A, filter → `{Source1}` |
| `FinalNodes from root` | Walk downstream from `Source1` in A, filter → `{rawData1.csv}` |
| `RootNodes in branching graph` | From either branch output in B → `{Source1}` |
| `FinalNodes in merging graph` | From `Source1` in C → `{FinalSample}` |

### 5.5 UpstreamSamples / DownstreamData etc.

| Test | Description |
|------|-------------|
| `UpstreamSamples from data leaf` | `rawData1.csv` in A → `{Sample2, Sample1, Source1}` |
| `DownstreamData from root` | `Source1` in A → `{rawData1.csv}` |
| `ConnectedSamples from mid` | `Sample1` in A → `{Source1, Sample2}` |
| `ConnectedData` | Returns only Data nodes |

---

## 6 — Annotation Sources (`Graph/AnnotationSources.fs`)

These tests verify that **all four sources** are collected when querying property values.

A dedicated fixture is needed: one process with PVs in all four positions simultaneously.

| Test | Description |
|------|-------------|
| `Process.AnnotationsByName — all 4 sources` | Create a process with `ParameterValue`, input node `AdditionalProperty`, output node `AdditionalProperty`, and protocol `LabEquipment` each having a PV with a unique name. Call `AnnotationsByName` for each name and verify it is found. |
| `IONode.AllAnnotations — all 4 sources` | Start from the input node of the above process; `AllAnnotations` includes entries from all four sources |
| `UpstreamAnnotations — filters to upstream only` | PV only on downstream process is not included |
| `DownstreamAnnotations — filters to downstream only` | PV only on upstream process is not included |
| `UpstreamAnnotations with protocolName filter` | Only collects from processes whose protocol name matches |
| `Deduplication across sources` | A PV with the same name/value/unit/nameTAN appearing in two sources is returned only once |
| `AllAnnotations on Path` | Same four-source fixture wrapped in a `Path` → identical set returned |

---

## 7 — Dataset Queries (`Graph/DatasetQueries.fs`)

Using Fixture A and Fixture D (nested).

| Test | Description |
|------|-------------|
| `AllProcesses — flat dataset` | `DS-A.AllProcesses()` → `{p1, p2, p3}` |
| `AllProcesses — nested datasets` | Fixture D: `parent.AllProcesses()` → processes from both children |
| `AllSamples deduplicates shared nodes` | Fixture D: `Sample1` appears in both children but is in result once |
| `AllData` | Fixture A: `rawData1.csv` in result |
| `AllNodes includes both types` | Fixture A: 4 nodes total |
| `RootNodes` | Fixture A: `{Source1}` |
| `FinalNodes` | Fixture A: `{rawData1.csv}` |
| `RootSamples / RootData / FinalSamples / FinalData` | Typed subsets of above |
| `AllAnnotations — no filter` | Returns PVs from all processes |
| `AllAnnotations — protocolName filter` | Only PVs from processes whose protocol name matches |
| `AnnotationsForNode — upstream + downstream` | Mid-graph node returns PVs from both directions |
| `UpstreamAnnotationsForNode` | Returns only upstream PVs |
| `DownstreamAnnotationsForNode` | Returns only downstream PVs |
| `FindProcessesByProtocolType` | Fixture A: `"cell growth"` → `{p1}` |
| `FindProcessesByProtocolType — no match` | Unknown type → empty |
| `FindProcessesByAnnotation — param source` | Finds process with matching parameter |
| `FindProcessesByAnnotation — input node source` | Finds process whose input node has matching `AdditionalProperty` |
| `FindProcessesByAnnotation — output node source` | Same for output |
| `FindProcessesByAnnotation — protocol component source` | PV in `LabEquipment` |
| `FindProcessesByPropertyName` | Returns process regardless of value |
| `SamplesResultingFromCondition — use-case 1` | Fixture A: query for samples resulting from `"cell growth"` at `temperature=37°C` → `{Sample1}` (first downstream terminal sample after `p1`) |
| `SamplesResultingFromCondition — no qualifying process` | Unknown protocol type → empty |
| `SamplesResultingFromCondition — branching downstream` | Fixture B: qualifying process has two terminal output samples → both returned |

---

## 8 — Path Queries (`Graph/PathQueries.fs`)

| Test | Description |
|------|-------------|
| `Path.Length` | Linear fixture → 3 |
| `Path.Head` | First process in the path |
| `Path.Last` | Last process in the path |
| `Path.Nodes() deduplicates shared nodes` | Shared node between two processes appears once |
| `Path.Samples()` | Only sample nodes |
| `Path.DataNodes()` | Only data nodes |
| `Path.ContainsNode — present` | Mid-graph node → true |
| `Path.ContainsNode — absent` | Node not in path → false |
| `Path.TerminalInputs` | Input nodes that are never outputs in the path |
| `Path.TerminalOutputs` | Output nodes that are never inputs in the path |
| `Path.AllAnnotations — all 4 sources` | See section 6 fixture |
| `Path.AnnotationsByName` | Returns only matching PVs |
| `Path.ProtocolParameters` | Returns FPs from all executed protocols, deduplicated |
| `empty Path` | `Head = None`, `Last = None`, `Length = 0`, `Nodes()` empty |

---

## 9 — ProcessGraph Queries (`Graph/ProcessGraphQueries.fs`)

| Test | Description |
|------|-------------|
| `TryGetProcess found / not found` | |
| `FindProcessesByProtocolType` | Same as Dataset test but via ProcessGraph |
| `FindProcessesByAnnotation` | Same as Dataset test |
| `FindProcessesByPropertyName` | Same |
| `ProcessesForNode — sample` | Returns both InputOf and OutputOf processes in scope |
| `ProcessesForNode — scoped to subset` | Processes outside scope not returned |
| `PathsThrough — single path` | Linear graph → one path covering all three processes |
| `PathsThrough — branching graph` | Node with two successors → two paths returned |
| `PathsThrough — node not in graph` | Empty result |
| `NodesDownstreamOf` | From root node → all downstream nodes |
| `NodesUpstreamOf` | From leaf node → all upstream nodes |
| `SamplesDownstreamOf / SamplesUpstreamOf` | Typed variants |
| `DataDownstreamOf / DataUpstreamOf` | Typed variants |
| `AllConnectedNodes via ProcessGraph` | Matches IONode.AllConnectedNodes for the same node |
| `ConnectedSamplesForNode / ConnectedDataForNode` | Typed variants |
| `AllAnnotationsForNode` | Collects across all paths through node |
| `ProtocolParametersForNode` | Collects FPs across all paths |
| `SamplesResultingFromCondition (name+value overload)` | Same as Dataset use-case 1 |
| `SamplesResultingFromCondition (predicate overload)` | Custom predicate selects qualifying processes |

---

## 10 — Table (`Table/`)

### 10.1 CompositeTypes (`Table/CompositeTypes.fs`)

| Test | Description |
|------|-------------|
| `IOType discriminated union — all cases` | Construct and pattern-match each case |
| `CompositeHeader — annotation headers carry term pair` | `Parameter("Temp", Some "TAN")` roundtrips |
| `CompositeHeader — protocol headers` | `ProtocolREF`, `ProtocolType`, etc. |
| `CompositeHeader — IO headers carry IOType` | `Input(IOType.Source)` |
| `CompositeCell.FreeText` | |
| `CompositeCell.Term` | |
| `CompositeCell.Unitized` | |
| `CompositeCell.Data` | Carries a `ProcessCore.Data` object |
| `CompositeColumn.ColumnCount` | Matches `Cells.Count` |

### 10.2 TableAux (`Table/TableAux.fs`)

| Test | Description |
|------|-------------|
| `SampleIOType — Source` | `AdditionalType = Some "Source"` → `IOType.Source` |
| `SampleIOType — Sample` | `AdditionalType = Some "Sample"` → `IOType.Sample` |
| `SampleIOType — None` | Default → `IOType.Sample` |
| `PVToCell — unitized PV` | PV with `Unit` set → `CompositeCell.Unitized` |
| `PVToCell — term PV` | PV with `ValueTAN` and no unit → `CompositeCell.Term` |
| `PVToCell — freetext PV` | PV with only `Value` → `CompositeCell.FreeText` |
| `PVToHeader — ParameterValue` | `AdditionalType = "ParameterValue"` → `CompositeHeader.Parameter` |
| `PVToHeader — FactorValue` | → `CompositeHeader.Factor` |
| `PVToHeader — CharacteristicValue` | → `CompositeHeader.Characteristic` |
| `PVToHeader — Component` | → `CompositeHeader.Component` |
| `PVToHeader — no AdditionalType` | Defaults to `CompositeHeader.Parameter` |
| `ApplyCellToPV — FreeText` | Sets `Value`, clears `Unit`/`ValueTAN`/`UnitTAN` |
| `ApplyCellToPV — Term` | Sets `Value` + `ValueTAN`, clears `Unit`/`UnitTAN` |
| `ApplyCellToPV — Unitized` | Sets all three fields |
| `ApplyCellToPV — Data cell is no-op` | PV unchanged |
| `MakePV roundtrip` | `MakePV(header, cell)` → same PV as manual construction |

### 10.3 Table Read API (`Table/TableRead.fs`)

Tests for `Decompose` and the derived read helpers.

| Test | Description |
|------|-------------|
| `empty process list → empty columns` | |
| `single process — input column first` | First column header is `Input(IOType.Source)` |
| `single process — protocol ref column present` | `ProtocolREF` column in result when process has a protocol |
| `single process — output column last` | Last column header is `Output(IOType.Sample)` or `Output(IOType.Data)` |
| `single process — parameter column present` | Process with one `ParameterValue` → one `Parameter` column |
| `single process — characteristic column present` | Input sample with `AdditionalProperty` typed as characteristic |
| `single process — factor column present` | Output sample with `AdditionalProperty` typed as factor |
| `single process — component column present` | Protocol `LabEquipment` → `Component` column |
| `column order is Input → Protocol → Char → Comp → Param → Factor → Output` | Verify index order in result |
| `multiple rows (multiple processes with same name)` | `Table` built from two `Process` objects with the same name → `Decompose` produces two rows worth of cells per annotation column |
| `data output — CompositeCell.Data in output column` | Output is `Data` node → cell carries `CompositeCell.Data` |
| `Headers derives from Decompose` | `table.Headers` matches `table.Decompose() |> map _.Header` |
| `ColumnCount / RowCount` | Correct counts for a known fixture |
| `GetColumn by index` | Returns the expected column; raises on out-of-range |
| `TryGetColumnByHeader — found` | Predicate matching first Parameter column returns it |
| `TryGetColumnByHeader — not found` | Returns `None` |
| `TryGetInputColumn / TryGetOutputColumn` | Convenience helpers return the correct typed columns |
| `GetComponentColumns` | Returns only `Component`-typed columns |
| `GetCellAt (col, row)` | Returns the expected cell value |
| `TryGetCellAt — in range` | Returns `Some cell` |
| `TryGetCellAt — out of range` | Returns `None` |
| `GetRow` | Returns one cell per column in correct order |

### 10.4 Table Write API (`Table/TableWrite.fs`)

Tests for column and row mutation methods.

#### Column write

| Test | Description |
|------|-------------|
| `AddColumn Parameter — PV added to each process` | After `table.AddColumn(CompositeHeader.Parameter("Temp", None), cells)`, each process has a new `ParameterValue` with the corresponding cell value |
| `AddColumn Characteristic — PV added to input node` | PV lands on input sample's `AdditionalProperty` |
| `AddColumn Factor — PV added to output node` | PV lands on output sample's `AdditionalProperty` |
| `AddColumn Component — PV added to protocol LabEquipment` | Requires process to have an `ExecutesProtocol` |
| `AddColumn non-annotation header is no-op` | `CompositeHeader.ProtocolREF` → nothing changes |
| `AddColumn with fewer cells than rows` | Missing cells are treated as `FreeText ""` |
| `RemoveColumn Parameter` | First matching PV removed from every process; column disappears in next `Decompose` |
| `RemoveColumn Characteristic` | Removed from input node |
| `RemoveColumn Factor` | Removed from output node |
| `RemoveColumn Component` | Removed from protocol `LabEquipment` |
| `RemoveColumn non-annotation is no-op` | `ProtocolREF` header → nothing changes |

#### Row write

| Test | Description |
|------|-------------|
| `AddRow — new Process created and registered in dataset` | After `table.AddRow(cells)`, `dataset.Processes` contains the new process |
| `AddRow — input cell sets input node name` | `FreeText "S1"` in Input column → process input is `Sample("S1")` |
| `AddRow — output cell sets output node name` | `FreeText "S2"` in Output column → process output is `Sample("S2")` |
| `AddRow — Data cell in Data-typed column` | `CompositeCell.Data d` → process input/output is `DataNode d` |
| `AddRow — ProtocolREF cell sets protocol name` | |
| `AddRow — ProtocolType cell sets protocol IntendedUse` | |
| `AddRow — Parameter cell creates ParameterValue` | |
| `AddRow — protocol cloned from first row` | New row inherits protocol name/description/etc. from first process |
| `AddRow at index — inserted at correct position` | `processes.[index]` is the new process |
| `AppendRow — equivalent to AddRow at end` | |
| `RemoveRow — process removed from table and dataset` | After `table.RemoveRow(0)`, `dataset.Processes.Count` decreases by 1 |
| `RemoveRow out-of-range is no-op` | |
| `UpdateRow — existing PV value updated in place` | `UpdateRow(0, cells)` with a new Unitized cell → the process's PV reflects the new value |
| `UpdateRow — input node name updated` | Cell in Input column changes sample name |
| `UpdateRow — new PV added if column did not have one` | If process had no PV for a column, one is created |

### 10.5 Tables (`Table/TablesApi.fs`)

Tests for the `Tables` aggregate type.

| Test | Description |
|------|-------------|
| `GetTables groups processes by name` | Dataset with `p1 ("table1")` and `p2 ("table1")` and `p3 ("table2")` → two tables |
| `TableCount` | Returns count of distinct process names |
| `TableNames` | Returns names in insertion order |
| `GetTableAt by index` | Returns correct table |
| `GetTable by name` | Returns correct table |
| `TryGetTable — found / not found` | Returns `Some` / `None` |
| `AddTable — new empty table` | Returns a `Table` with `RowCount = 0`; no processes yet |
| `AddTable — duplicate name raises` | `failwithf` when name already exists |
| `RemoveTable — all processes with that name removed from dataset` | After `tables.RemoveTable("table1")`, processes `p1` and `p2` are gone |
| `RemoveTable — unknown name is no-op` | |

---

## 11 — Integration / End-to-End

A small number of integration tests that assemble a realistic multi-step process graph (a subset of the proteomics example) and exercise the three querying use-cases from the spec end-to-end.

| Test | Description |
|------|-------------|
| `use-case 1 — growth temperature filter` | Build a multi-step dataset; use `Dataset.SamplesResultingFromCondition` to find samples downstream of a specific growth temperature |
| `use-case 2 — all parameters for a sample` | Use `Dataset.AnnotationsForNode` to retrieve all parameters connected to a mid-graph sample |
| `use-case 3 — all connected samples` | Use `IONode.AllConnectedNodes` scoped to the dataset to retrieve all samples connected to a given node |
| `ProcessGraph.PathsThrough — multi-path proteomics` | ProcessGraph over an investigation-level flat process list; `PathsThrough` returns the correct distinct paths |

---

## Implementation Notes

- **Test framework**: `Fable.Pyxpecto` (`testList`, `testCase`, `Expect.*`). Follows the existing pattern in `Synchronization.fs`.
- **Cross-platform**: All tests must compile and pass under both .NET and Fable (JavaScript/Python).
- **No serialization**: Tests may construct objects directly; no YAML/JSON parsing required.
- **Shared fixtures**: Define shared fixtures as `let`-bound values in a `Fixtures` module included early in the test project. Avoid re-constructing complex graphs in every test.
- **Reference equality check**: Use `obj.ReferenceEquals(a, b)` (or `Fable`-compatible equivalent) to verify that deduplication returns the original object instance.
- **Mutation safety**: Tests that mutate shared fixtures must create local copies or reconstruct from scratch to avoid test-order coupling.
- **`.fsproj` ordering**: F# files must be listed in `ProcessCore.Tests.fsproj` in dependency order. `Fixtures.fs` must come before all test files. Each sub-folder file (`Types/Annotation.fs`, etc.) is a flat entry in the project — there are no sub-folder project groupings, just ordered `<Compile Include="..." />` lines.
