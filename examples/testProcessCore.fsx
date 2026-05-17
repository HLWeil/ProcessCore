#r "nuget: Fable.Core, 4.3.0"
#r "nuget: DynamicObj"
#r "nuget: YAMLicious"
#r @"..\src\ProcessCore\bin\Release\netstandard2.0\ProcessCore.dll"
#r @"..\src\ProcessCore.YML\bin\Release\netstandard2.0\ProcessCore.YML.dll"
#r @"..\tests\ProcessCore.Tests\bin\Release\net10.0\ProcessCore.Tests.dll"

open ProcessCore
open ProcessCore.Table
open ProcessCore.Yaml

open ProcessCore.Tests.Graph.DatasetQueries
open ProcessCore.Tests.Fixtures

let f = makeFixtureFourSources()
let pvs = f.DownstreamNode.UpstreamPropertyValues()

pvs.Count

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



let assayFilePath = @"./isa/assay_proteomics.yml"
let ymlContent = System.IO.File.ReadAllText(assayFilePath)

let myAssay = Decode.fromYamlString (Dataset.decoder false) ymlContent

myAssay.Processes


let ddd = Dataset(identifier = "MyDataset")

ddd.AddProcess(LabProcess("MyProcess"))
ddd.AddProcess(LabProcess("MyProcess"))
ddd.AddProcess(LabProcess("MyProcess2"))

ddd.Processes

childD1.Processes
|> Seq.length


LabProcess("MyProcess").ReferenceEquals(LabProcess("MyProcess"))

#time

let arabidopsis = PropertyValue(name = "Organism", value = "Arabidopsis thaliana")
let tenDays = PropertyValue(name = "Time", value = "10", unit = "day")
let normalTemp = PropertyValue(name = "Temperature", value = "22", unit = "degree Celsius")
let highTemp = PropertyValue(name = "Temperature", value = "30", unit = "degree Celsius")


let timeSecond (f: unit -> unit) =
    let stopwatch = System.Diagnostics.Stopwatch.StartNew()
    f()
    f()
    f()
    stopwatch.Stop()
    stopwatch.Elapsed.TotalSeconds / 3.


let wait1Second () =
    System.Threading.Thread.Sleep(1000)

timeSecond wait1Second

let createBySize (size: int) =
    let dataset = Dataset("Dataset")
    for i in 1 .. size do
        let p = LabProcess(sprintf "Process%d" i)
        let inp = Material(sprintf "InputMaterial%d" i, additionalProperty = [arabidopsis])
        let out = Material(sprintf "OutputMaterial%d" i, additionalProperty = [arabidopsis])
        p.AddParameterValue(tenDays)
        p.AddParameterValue(normalTemp)
        dataset.AddProcess(p)
        p.AddInputMaterial(inp)
        p.AddOutputMaterial(out)


let times = 
    [1000; 2000; 5000; 10000; 20000; 50000; 100000; 1000000]
    |> List.map (fun s -> 
        printfn "Creating dataset with %d processes..." s
        let f () = createBySize s |> ignore
        s, timeSecond(f)
    )

let processes : ResizeArray<LabProcess> = ResizeArray()

for i in 1 .. 100000 do
    let newProcess = LabProcess(sprintf "Process%d" i)
    if not (processes |> Seq.exists (fun p -> p.ReferenceEquals newProcess)) then
        processes.Add(newProcess)












let yaml = "type: Investigation\nidentifier: DS-1\n"
Yaml.Dataset.fromYamlString false yaml


let yaml2 = "type: Process\nname: p1\n"
Yaml.LabProcess.fromYamlString yaml2