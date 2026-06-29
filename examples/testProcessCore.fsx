#r "nuget: Fable.Core, 4.3.0"
#r "nuget: DynamicObj"
#r "nuget: YAMLicious"
#r @"..\src\ProcessCore\bin\Release\netstandard2.0\ProcessCore.dll"
#r @"..\tests\ProcessCore.Tests\bin\Release\net10.0\ProcessCore.Tests.dll"

open ProcessCore
open ProcessCore.Table
open ProcessCore.Yaml

open ProcessCore.Tests.Graph.DatasetQueries
open ProcessCore.Tests.Fixtures

let f = makeFixtureFourSources()
let pvs = f.DownstreamNode.UpstreamAnnotations()

pvs.Count

let childD1 = Dataset("ChildDataset1")
let childD2 = Dataset("ChildDataset2")
let dataset = Dataset("MyDataset")
dataset.AddPart(childD1)
dataset.AddPart(childD2)

let process1 = Process("MyProcess")
let process2 = Process("MyProcess")
let process3 = Process("MyProcess2")

childD1.AddProcess(process1)
childD1.AddProcess(process2)
childD2.AddProcess(process3)

let sample1 = (Sample("MyInputSample1"))
let sample2 = (Sample("MyInputSample2"))
let sample3 = (Sample("MyOutputSample1"))
let sample4 = (Sample("MyOutputSample2"))
let data1 = (Data("MyOutputData1"))

process1.AddInputSample(sample1)
process2.AddInputSample(sample2)

process1.AddOutputSample(sample3)
process2.AddOutputSample(sample4)

process3.AddInputSample(sample3)
process3.AddOutputData(data1)


process1.AddInputSample(Sample("MyInputSample1"))
process2.AddInputSample(Sample("MyInputSample2"))

process1.AddOutputSample(Sample("MyOutputSample1"))
process2.AddOutputSample(Sample("MyOutputSample2"))

process3.AddInputSample(Sample("MyOutputSample1"))
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

ddd.AddProcess(Process("MyProcess"))
ddd.AddProcess(Process("MyProcess"))
ddd.AddProcess(Process("MyProcess2"))

ddd.Processes

childD1.Processes
|> Seq.length


Process("MyProcess").ReferenceEquals(Process("MyProcess"))

#time

let arabidopsis = Annotation(name = "Organism", value = "Arabidopsis thaliana")
let tenDays = Annotation(name = "Time", value = "10", unit = "day")
let normalTemp = Annotation(name = "Temperature", value = "22", unit = "degree Celsius")
let highTemp = Annotation(name = "Temperature", value = "30", unit = "degree Celsius")


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
        let p = Process(sprintf "Process%d" i)
        let inp = Sample(sprintf "InputSample%d" i, additionalProperty = [arabidopsis])
        let out = Sample(sprintf "OutputSample%d" i, additionalProperty = [arabidopsis])
        p.AddParameterValue(tenDays)
        p.AddParameterValue(normalTemp)
        dataset.AddProcess(p)
        p.AddInputSample(inp)
        p.AddOutputSample(out)


let times =
    [1000; 2000; 5000; 10000; 20000; 50000; 100000; 1000000]
    |> List.map (fun s ->
        printfn "Creating dataset with %d processes..." s
        let f () = createBySize s |> ignore
        s, timeSecond(f)
    )

let processes : ResizeArray<Process> = ResizeArray()

for i in 1 .. 100000 do
    let newProcess = Process(sprintf "Process%d" i)
    if not (processes |> Seq.exists (fun p -> p.ReferenceEquals newProcess)) then
        processes.Add(newProcess)












let yaml = "type: Investigation\nidentifier: DS-1\n"
Yaml.Dataset.fromYamlString false yaml


let yaml2 = "type: Process\nname: p1\n"
Yaml.Process.fromYamlString yaml2
