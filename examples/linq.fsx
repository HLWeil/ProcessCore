#r "nuget: Fable.Core, 4.3.0"
#r "nuget: DynamicObj"
#r "nuget: YAMLicious"
#r @"..\src\ProcessCore\bin\Release\netstandard2.0\ProcessCore.dll"
#r @"..\src\ProcessCore.YML\bin\Release\netstandard2.0\ProcessCore.YML.dll"
open ProcessCore
open ProcessCore.Table
open ProcessCore.Yaml


let assayFilePath = System.IO.Path.Combine(__SOURCE_DIRECTORY__, "isa", "assay_proteomics.yml")
let ymlContent = System.IO.File.ReadAllText(assayFilePath)

let myAssay = Decode.fromYamlString (Dataset.decoder false) ymlContent


query {
    for data in myAssay.AllData() do
    where data.UpstreamPropertyValues() |> Seq.exists (fun pv -> )
}

myAssay.AllData().[0].UpstreamPropertyValues()
myAssay.AllData().[0].UpstreamNodes()

myAssay.AllMaterials().[3].UpstreamPropertyValues()