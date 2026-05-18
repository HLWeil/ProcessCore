(**
---
title: Querying Process Graphs
category: Core Implementation
categoryindex: 3
index: 5
---

# Querying Process Graphs

ProcessCore query methods let you ask questions from either the dataset or a specific material/data node.
This walkthrough loads the proteomics assay example and follows provenance from final data back to experimental conditions.
*)

(*** hide ***)
#r "../../src/ProcessCore/bin/Release/netstandard2.0/ProcessCore.dll"
#r "../../src/ProcessCore.YML/bin/Release/netstandard2.0/ProcessCore.YML.dll"
#r "nuget: DynamicObj"
#r "nuget: YAMLicious, 1.0.0-alpha.10"
open ProcessCore

let ymlString =
    System.IO.Path.Combine(__SOURCE_DIRECTORY__, "../../../examples/isa/assay_proteomics.yml")
    |> System.IO.File.ReadAllText

(*** hide ***)
let ymlCodeBlock =
    ymlString
    |> System.Net.WebUtility.HtmlEncode
    |> sprintf "<pre class=\"fssnip\"><code lang=\"yaml\" class=\"language-yaml\">%s</code></pre>"

(**
<details>
<summary>Show source YAML</summary>
*)

(*** hide ***)
ymlCodeBlock
(*** include-it-raw ***)

(**
</details>
*)

let myAssay = ProcessCore.Yaml.Dataset.fromYamlString false ymlString

(**
The example stores protocol references by id. For the protocol-name filter below, mirror the process name into the protocol name when the YAML did not provide one.
*)

for proc in myAssay.Processes do
    proc.ExecutesProtocol
    |> Option.iter (fun protocol ->
        if protocol.Name.IsNone then
            protocol.Name <- Some proc.Name)

(**
## Dataset-Level Discovery

Start by asking what is in the dataset. Dataset helpers include nested datasets through `AllProcesses`, `AllMaterials`, `AllData`, and `AllNodes`.
*)

let datasetOverview =
    [ "processes", myAssay.AllProcesses().Count
      "materials", myAssay.AllMaterials().Count
      "data", myAssay.AllData().Count
      "root nodes", myAssay.RootNodes().Count
      "final nodes", myAssay.FinalNodes().Count ]

datasetOverview
(*** include-it ***)

let rootNodes =
    myAssay.RootNodes()
    |> Seq.map (fun n -> n.Key())
    |> Seq.toList

rootNodes
(*** include-it ***)

let finalNodes =
    myAssay.FinalNodes()
    |> Seq.map (fun n -> n.Key())
    |> Seq.toList

finalNodes
(*** include-it ***)

(**
## Node-Centered Traversal

Pick one final result file and inspect the graph around it.
*)

let resultData =
    myAssay.AllData()
    |> Seq.find (fun d -> d.Path.Contains("proteomics_result.csv"))

let resultContext =
    [ "path", resultData.Path
      "upstream nodes", string (myAssay.NodesUpstreamOf(DataNode resultData).Count)
      "downstream nodes", string (myAssay.NodesDownstreamOf(DataNode resultData).Count)
      "paths through result", string (myAssay.PathsThrough(DataNode resultData).Count) ]

resultContext
(*** include-it ***)

let upstreamNodeKeys =
    myAssay.NodesUpstreamOf(DataNode resultData)
    |> Seq.map (fun n -> n.Key())
    |> Seq.toList

upstreamNodeKeys
(*** include-it ***)

(**
Property-value queries collect annotations from process parameters, input/output node properties, and protocol components.
*)

let upstreamPropertyValues =
    myAssay.UpstreamPropertyValuesForNode(DataNode resultData)
    |> Seq.map (fun pv -> pv.Name + "=" + pv.ValueWithUnitText)
    |> Seq.distinct
    |> Seq.toList

upstreamPropertyValues
(*** include-it ***)

(**
## Composable Queries

Plain F# sequence operations compose with graph traversal. This predicate selects the growth temperature condition used in the example.
*)

let is25Degrees (pv: PropertyValue) =
    pv.NameText = "temperature"
    && pv.ValueText = "25"
    && pv.UnitText = "degree Celsius"

let dataWith25DegreeHistory =
    myAssay.AllData()
    |> Seq.filter (fun data ->
        myAssay.UpstreamPropertyValuesForNode(DataNode data)
        |> Seq.exists is25Degrees)
    |> Seq.map (fun d -> d.Path)
    |> Seq.toList

dataWith25DegreeHistory
(*** include-it ***)

(**
Protocol-name filters narrow property collection to processes whose executed protocol has the given name.
*)

let resultPathsFrom25DegreeGrowth =
    myAssay.AllData()
    |> Seq.filter (fun data -> data.Path.Contains("proteomics_result.csv"))
    |> Seq.filter (fun data ->
        data.UpstreamPropertyValues(protocolName = "Growth", scope = myAssay.AllProcesses())
        |> Seq.exists is25Degrees)
    |> Seq.map (fun d -> d.Path)
    |> Seq.toList

resultPathsFrom25DegreeGrowth
(*** include-it ***)

(**
## What To Use When

| Task | API |
|------|-----|
| Count or list dataset contents | `AllProcesses`, `AllMaterials`, `AllData`, `AllNodes` |
| Find terminal sources and sinks | `RootNodes`, `FinalNodes` |
| Walk from a node | `UpstreamNodes`, `DownstreamNodes` |
| Collect annotations around a node | `UpstreamPropertyValues`, `DownstreamPropertyValues`, `PropertyValuesForNode` |
| Work inside one dataset only | Dataset-scoped helpers such as `NodesUpstreamOf` |
| Ask path questions | `PathsThrough`, `Path.TerminalInputs`, `Path.TerminalOutputs` |
*)
