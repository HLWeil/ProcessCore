#r "nuget: Fable.Core, 4.3.0"
#r "nuget: DynamicObj"
#r @"C:\Users\HLWei\source\repos\ARC-Data-Model\src\ProcessCore\bin\Debug\netstandard2.0\ProcessCore.dll"

open ProcessCore
open ProcessCore.Table

DefinedTerm("dawdwa")


let childD1 = Dataset("ChildDataset1")
let childD2 = Dataset("ChildDataset2")
let dataset = Dataset("MyDataset")
dataset.AddPart(childD1)
dataset.AddPart(childD2)

let process1 = LabProcess("MyProcess")
let process2 = LabProcess("MyProcess")
let process3 = LabProcess("MyProcess2")

childD1.AddProcess(process1)
childD1.AddProcess(process2)
childD2.AddProcess(process3)

let material1 = (Material("MyInputMaterial1"))
let material2 = (Material("MyInputMaterial2"))
let material3 = (Material("MyOutputMaterial1"))
let material4 = (Material("MyOutputMaterial2"))
let data1 = (Data("MyOutputData1"))

process1.AddInputMaterial(material1)
process2.AddInputMaterial(material2)

process1.AddOutputMaterial(material3)
process2.AddOutputMaterial(material4)

process3.AddInputMaterial(material3)
process3.AddOutputData(data1)


process1.AddInputMaterial(Material("MyInputMaterial1"))
process2.AddInputMaterial(Material("MyInputMaterial2"))

process1.AddOutputMaterial(Material("MyOutputMaterial1"))
process2.AddOutputMaterial(Material("MyOutputMaterial2"))

process3.AddInputMaterial(Material("MyOutputMaterial1"))
process3.AddOutputData(Data("MyOutputData1"))



dataset.Tables

dataset.AllData().[0].UpstreamNodes()
|> Seq.length

dataset.Tables.RemoveTable(dataset.Tables.TableNames.[1])
