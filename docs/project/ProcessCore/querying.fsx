(**
---
title: Querying Use Cases
category: Use Cases
index: 1
---

# Querying ARC process graphs

This use case shows how to find data produced under a given condition.
*)

(*** hide ***)
#r "../../../src/ProcessCore/bin/Release/netstandard2.0/ProcessCore.dll"
#r "../../../src/ProcessCore.YML/bin/Release/netstandard2.0/ProcessCore.YML.dll"
#r "nuget: DynamicObj"
#r "nuget: YAMLicious, 1.0.0-alpha.10"
open ProcessCore
open ProcessCore.Yaml

(**
First we construct or load a small dataset.
*)

let ymlString = 
    (System.IO.Path.Combine(__SOURCE_DIRECTORY__, "../../../examples/isa/assay_proteomics.yml"))
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

let myAssay = Dataset.fromYamlString false ymlString



(**
We can filter the data for those entities, which have an upstream property value with the name "temperature" and the value "25".
*)

let is25Degrees (pv: PropertyValue) =
    pv.NameText = "temperature" && pv.ValueText = "25" && pv.UnitText = "degree Celsius"

let matches : seq<Data> =
    myAssay.AllData()
    |> Seq.filter (fun data ->
        data.UpstreamPropertyValues()
        |> Seq.exists is25Degrees)

matches
|> Seq.map (fun d -> d.Path)

(*** include-it ***)
