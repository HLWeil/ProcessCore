#r "nuget: Fable.Core, 4.3.0"
#r "nuget: DynamicObj"
#r "nuget: YAMLicious"
#r @"..\src\ProcessCore\bin\Release\netstandard2.0\ProcessCore.dll"


open ProcessCore
open ProcessCore.Table
open ProcessCore.Yaml


let assayFilePath = System.IO.Path.Combine(__SOURCE_DIRECTORY__, "isa", "assay_proteomics.yml")
let ymlContent = System.IO.File.ReadAllText(assayFilePath)

let myAssay = Dataset.fromYamlString false ymlContent



query {
    for data in myAssay.AllData() do
    where (data.UpstreamPropertyValues() |> Seq.exists (fun pv -> pv.NameText = "temperature" && pv.ValueText = "25"))
}

myAssay.AllData().[0].UpstreamPropertyValues()


myAssay.AllData().[0].UpstreamPropertyValues()
myAssay.AllData().[0].UpstreamNodes().[0].UpstreamPropertyValues()

(myAssay.AllData().[0].UpstreamNodes().[0].GetInputOf() |> Seq.item 0).ParameterValue

myAssay.AllMaterials().[3].UpstreamPropertyValues()
