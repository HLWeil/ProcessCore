(**
---
title: Creating A Dataset
category: Core Implementation
categoryindex: 3
index: 3
---

# Creating A Dataset

This walkthrough builds a small process graph from F# objects. The goal is to show the model shape rather than every field in the specification.
*)

(*** hide ***)
#r "../../src/ProcessCore/bin/Release/netstandard2.1/ProcessCore.dll"
#r "nuget: DynamicObj"
open ProcessCore

(*** hide ***)
let mermaidBlock (text: string) =
    text.Trim()
    |> System.Net.WebUtility.HtmlEncode
    |> sprintf "<pre class=\"mermaid\">%s</pre>"

(**

#### Dataset

Start with a dataset. Administrative metadata is optional, but an identifier is the stable handle for the dataset.
*)

let dataset = Dataset("demo-dataset") // or ARC("demo-dataset") for an ARC package

let lab = Organization("Core Lab")
let curator = Agent("Ada", familyName = "Lovelace", email = "ada@example.org", affiliation = lab)
let citation = ScholarlyArticle("Minimal ProcessCore example", authors = [ curator ])

dataset.Title <- Some "Minimal ProcessCore example"
dataset.Description <- Some "One extraction process with nested quality control."
dataset.License <- Some "CC-BY-4.0"
dataset.DatePublished <- Some "2026-07-03"
dataset.DateCreated <- Some "2026-07-03"
dataset.DateModified <- Some "2026-07-03"
dataset.AddAgent(curator)
dataset.AddCitation(citation)

(**
The administrative metadata is attached to the package itself rather than the individual processes.
*)

(**
#### Recipe

A protocol describes the method. Formal parameters define expected knobs, for which values should be provided when the protocol is executed.
*)

let protocol = Recipe()
let temperature = FormalParameter("temperature")
protocol.Name <- Some "Extraction"
protocol.IntendedUse <- Some (DefinedTerm("sample extraction"))
protocol.AddParameter(temperature)

(**
Components are non-transformed entities in a protocol, such as machines or reagents.
*)

let centrifuge = Annotation(name = "centrifuge", value = "Eppendorf 5420")
let buffer = Annotation(name = "buffer", value = "PBS")

protocol.AddComponent(centrifuge)
protocol.AddComponent(buffer)

(**
#### Process

Processes are the core of the process graph. They are concrete executions of a protocol, with specific parameter values, and input and output entities.

First, we define input and output, i.e. `Sample` or `Data` nodes.
*)

let leaf = Sample("Leaf tissue")

let extractData = Data("raw/extract.csv")
extractData.EncodingFormat <- Some "text/csv"

(**
A `Process` connects those inputs to outputs. We also attach parameter values to the process, which should correspond to the protocol's formal parameters.
*)

let extraction = Process("Extraction")
let degrees25 = Annotation(name = "temperature", value = "25", unit = "degree Celsius", instanceOf = temperature)
extraction.ExecutesRecipe <- Some protocol
extraction.SetInputSample(leaf)
extraction.SetOutputData(extractData)
extraction.AddParameterValue(degrees25)

dataset.AddProcess(extraction)



(**

#### Nested Datasets

Datasets can contain child datasets. When a child dataset is added, its process nodes are re-canonicalized against the root dataset.
*)

let child = Dataset("qc-dataset")
child.Title <- Some "Quality control"

let qcReport = Data("qc/extract-report.tsv")

let qc = Process("Quality Control")
qc.SetInputData(extractData)
qc.SetOutputData(qcReport)
let threshold = FormalParameter("threshold")
let threshold95 = Annotation(name = "threshold", value = "0.95", instanceOf = threshold)
qc.AddParameterValue(threshold95)

child.AddProcess(qc)
dataset.AddPart(child)

(**
The parent process output and the child process input are the same logical `Data` node: same path, no selector. After `AddPart`, they are also the same object instance in the root dataset.
*)

let qcInputAfterAttach =
    match qc.Input.Value with
    | DataNode d -> d
    | SampleNode _ -> failwith "Expected data input"

let sharedDataIdentity =
    obj.ReferenceEquals(extractData, qcInputAfterAttach)

sharedDataIdentity
(*** include-it ***)

(**
The graph is now queryable from either the dataset or any node.
*)

let finalNodes =
    dataset.FinalNodes()
    |> Seq.map (fun n -> n.Key())
    |> Seq.toList

finalNodes
(*** include-it ***)

(**
## What To Use When

| Task | API |
|------|-----|
| Create a container | `Dataset(identifier)` or `ARC(identifier)` |
| Add package metadata | `dataset.Title`, `dataset.Description`, `dataset.License`, `dataset.DatePublished`, `dataset.AddAgent`, `dataset.AddCitation` |
| Add a process | `dataset.AddProcess(process)` |
| Add nested datasets | `dataset.AddPart(child)` |
| Connect samples or files | `process.SetInputSample`, `process.SetOutputData` |
| Attach process parameters | `process.AddParameterValue` |
| Attach characteristics/factors | `node.AddAdditionalProperty` |
| Attach protocol components | `protocol.AddComponent` |
*)
