(**
---
title: Creating A Dataset
category: Core Implementation
categoryindex: 3
index: 2
---

# Creating A Dataset

This walkthrough builds a small process graph from F# objects. The goal is to show the model shape rather than every field in the specification.
*)

(*** hide ***)
#r "../../src/ProcessCore/bin/Release/netstandard2.0/ProcessCore.dll"
#r "nuget: DynamicObj"
open ProcessCore

let pv name value additionalType =
    let p = PropertyValue(name)
    p.Value <- Some value
    p.AdditionalType <- Some additionalType
    p

let nodeLabel = function
    | MaterialNode m -> "Material: " + m.Name
    | DataNode d -> "Data: " + d.Path + (d.Selector |> Option.defaultValue "")

(**
Start with a dataset. Administrative metadata is optional, but an identifier is the stable handle for the dataset.
*)

let dataset = Dataset("demo-dataset")
dataset.Name <- Some "Minimal ProcessCore example"
dataset.Description <- Some "One extraction process with nested quality control."

(**
A protocol describes the method. Formal parameters define expected knobs; process parameter values record what happened in a concrete process.
*)

let protocol = LabProtocol()
protocol.Name <- Some "Extraction"
protocol.IntendedUse <- Some (DefinedTerm("sample extraction"))
protocol.AddParameter(FormalParameter("buffer"))
protocol.AddLabEquipment(pv "centrifuge" "Eppendorf 5420" "Component")

let buffer = pv "buffer" "PBS" "ParameterValue"

(**
Inputs and outputs are `Material` or `Data` nodes. Node-level property values are useful for characteristics and factors.
*)

let leaf = Material("Leaf tissue")
leaf.AdditionalType <- Some "Source"
leaf.AddAdditionalProperty(pv "organism" "Arabidopsis thaliana" "CharacteristicValue")

let extract = Data("raw/extract.tsv")
extract.EncodingFormat <- Some "text/tab-separated-values"

(**
A `LabProcess` connects inputs to outputs. Adding the process to the dataset also sets the `ProcessOf` back-edge and canonicalizes its nodes against the dataset registry.
*)

let extraction = LabProcess("Extraction")
extraction.ExecutesProtocol <- Some protocol
extraction.AddInputMaterial(leaf)
extraction.AddOutputData(extract)
extraction.AddParameterValue(buffer)

dataset.AddProcess(extraction)

let firstShape =
    [ "processes", dataset.Processes.Count
      "materials", dataset.AllMaterials().Count
      "data", dataset.AllData().Count
      "parameters on process", extraction.ParameterValue.Count
      "protocol components", protocol.LabEquipment.Count ]

firstShape
(*** include-it ***)

(**
Datasets can contain child datasets. When a child dataset is added, its process nodes are re-canonicalized against the root dataset.
*)

let child = Dataset("qc-dataset")
child.Name <- Some "Quality control"

let qcInput = Data("raw/extract.tsv")
let qcReport = Data("qc/extract-report.tsv")

let qc = LabProcess("Quality Control")
qc.AddInputData(qcInput)
qc.AddOutputData(qcReport)
qc.AddParameterValue(pv "threshold" "0.95" "ParameterValue")

child.AddProcess(qc)
dataset.AddPart(child)

let nestedShape =
    [ "direct processes", dataset.Processes.Count
      "child datasets", dataset.HasPart.Count
      "all processes", dataset.AllProcesses().Count
      "all data nodes", dataset.AllData().Count ]

nestedShape
(*** include-it ***)

(**
The parent process output and the child process input are the same logical `Data` node: same path, no selector. After `AddPart`, they are also the same object instance in the root dataset.
*)

let qcInputAfterAttach =
    match qc.Inputs.[0] with
    | DataNode d -> d
    | MaterialNode _ -> failwith "Expected data input"

let sharedDataIdentity =
    obj.ReferenceEquals(extract, qcInputAfterAttach)

sharedDataIdentity
(*** include-it ***)

(**
The graph is now queryable from either the dataset or any node.
*)

let finalNodes =
    dataset.FinalNodes()
    |> Seq.map nodeLabel
    |> Seq.toList

finalNodes
(*** include-it ***)

(**
## What To Use When

| Task | API |
|------|-----|
| Create a container | `Dataset(identifier)` |
| Add a process | `dataset.AddProcess(process)` |
| Add nested datasets | `dataset.AddPart(child)` |
| Connect materials or files | `process.AddInputMaterial`, `process.AddOutputData` |
| Attach process parameters | `process.AddParameterValue` |
| Attach characteristics/factors | `node.AddAdditionalProperty` |
| Attach protocol components | `protocol.AddLabEquipment` |
*)
