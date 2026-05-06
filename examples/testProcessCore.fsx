#r "../src/ProcessCore/bin/Debug/netstandard2.0/ProcessCore.dll"

#r "nuget: DynamicObj"


open ProcessCore

let p1 = LabProcess("Growth1")
let p2 = LabProcess("Growth2")
let p3 = LabProcess("Extraction")

let d1 = Dataset("MyDataset")
let d2 = Dataset("MyDataset2")
let outerD = Dataset("MyDataset3")

let s1 = Material("Source1")
let s2 = Material("Source2")

let s3 = Material("Material1")
let s4 = Material("Material2")

let s5 = Material("Output")


p1.AddInput(MaterialNode s1)
p1.AddOutput(MaterialNode s3)

p2.AddInput(MaterialNode s2)
p2.AddOutput(MaterialNode s4)

p3.AddInput(MaterialNode s3)
p3.AddOutput(MaterialNode s5)

d1.AddProcess(p1)
d1.AddProcess(p2)
d2.AddProcess(p3)

outerD.AddPart(d1)
outerD.AddPart(d2)

s5.UpstreamMaterials().Count

s5.OutputOf