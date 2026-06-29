(**
---
title: Tabular Views
category: Core Implementation
categoryindex: 3
index: 7
---

# Tabular Views Over Process Graphs

`ProcessCore.Table` exposes ISA-like table views over a process graph. A table groups processes by name, and each row is backed by a live `Process`.
*)

(*** hide ***)
#r "../../src/ProcessCore/bin/Release/netstandard2.0/ProcessCore.dll"
#r "nuget: DynamicObj"
open ProcessCore
open ProcessCore.Table

let sample name =
    Sample(name, additionalType = "Sample")

let source name =
    Sample(name, additionalType = "Source")

let growthProcess inputName outputName temperature =
    let protocol = Recipe()
    protocol.Name <- Some "Growth"
    protocol.AddLabEquipment(Annotation("growth chamber", value = "chamber-1", additionalType = "Component"))

    let output = sample outputName
    output.AddAdditionalProperty(Annotation("temperature", value = temperature, unit = "degree Celsius", additionalType = "FactorValue"))

    let p = Process("Growth")
    p.ExecutesProtocol <- Some protocol
    p.AddInputSample(source inputName)
    p.AddOutputSample(output)
    p.AddParameterValue(Annotation("duration", value = "7", unit = "day", additionalType = "ParameterValue"))
    p

let dataset = Dataset("table-demo")
dataset.AddProcess(growthProcess "Base culture" "Leaf sample RT" "25")
dataset.AddProcess(growthProcess "Base culture" "Leaf sample HT" "30")

(**
`dataset.Tables` groups processes with the same name. Here both processes become rows in one `Growth` table.
*)

let tableNames =
    dataset.Tables.TableNames
    |> Seq.toList

tableNames
(*** include-it ***)

let growth = dataset.Tables.GetTable("Growth")

let initialShape =
    [ "rows", growth.RowCount
      "columns", growth.ColumnCount ]

initialShape
(*** include-it ***)

(**
Headers and cells are typed. They preserve whether a column represents input/output, protocol metadata, or annotation values.
*)

let headers =
    growth.Headers
    |> Seq.map (sprintf "%A")
    |> Seq.toList

headers
(*** include-it ***)

let firstRow =
    growth.GetRow(0)
    |> Seq.map (sprintf "%A")
    |> Seq.toList

firstRow
(*** include-it ***)

(**
Adding an annotation column writes `Annotation` objects into the appropriate graph slot. Parameter columns go to `Process.ParameterValue`.
*)

growth.AddColumn(
    CompositeHeader.Parameter("light intensity", None),
    ResizeArray([
        CompositeCell.Unitized("120", "umol m-2 s-1", None)
        CompositeCell.Unitized("150", "umol m-2 s-1", None)
    ])
)

let processParameters =
    growth.Processes
    |> Seq.map (fun p ->
        p.ParameterValue
        |> Seq.map (fun pv -> pv.Name + "=" + pv.ValueWithUnitText)
        |> Seq.toList)
    |> Seq.toList

processParameters
(*** include-it ***)

(**
Adding a row creates a new `Process` in both the table and the parent dataset. Empty cells use the table's current headers as a template.
*)

growth.AppendRow()

let afterAppend =
    [ "table rows", growth.RowCount
      "dataset processes", dataset.Processes.Count ]

afterAppend
(*** include-it ***)

(**
The table is a view, not a detached copy. Querying the dataset after a table edit sees the edited process graph.
*)

let allParameterNames =
    dataset.AllAnnotations()
    |> Seq.map (fun pv -> pv.Name)
    |> Seq.distinct
    |> Seq.toList

allParameterNames
(*** include-it ***)

(**
## What To Use When

| Task | API |
|------|-----|
| List tables | `dataset.Tables.GetTables()` |
| Open a named table | `dataset.Tables.GetTable(name)` |
| Inspect column roles | `table.Headers`, `table.Columns` |
| Read a row | `table.GetRow(index)` |
| Add annotation columns | `table.AddColumn` |
| Add rows/processes | `table.AddRow`, `table.AppendRow` |
| Edit the graph through cells | `table.UpdateRow` |
*)
