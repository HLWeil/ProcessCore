#r "nuget: Fable.Core, 5.0.0"
#r "nuget: DynamicObj"
#r "nuget: FsSpreadsheet, 7.0.0-alpha.1"
#r "nuget: FsSpreadsheet.Net, 7.0.0-alpha.1"
#r "nuget: YAMLicious"

#r @"..\src\ProcessCore\bin\Release\netstandard2.1\ProcessCore.dll"

//#r "nuget: ProcessCore, 0.0.4"
//#r "nuget: ARCtrl"


open ProcessCore
open ProcessCore.Table
open FsSpreadsheet
open FsSpreadsheet.Net
open ProcessCore.Helper
// open ProcessCore.ScaffoldReader
open ProcessCore.Spreadsheet
// open ARC
//open ARCtrl.Helper



let arcPath = @"C:\Users\HLWei\source\repos\ARCs\Ru_ChlamyHeatstress"

let arc = ARC.load arcPath

arc.Update()


ar^^

let s = 
    arc.AllSamples()
    |> Seq.find (fun s -> s.Name = "run_35_A")

arc.RegisterFragmentSelectorProvider (CsvFragmentSelectorProvider())

let d = 
    arc.AllData()
    |> Seq.find (fun d -> d.Selector.IsSome)

arc.DataContextsCoveringData(d).[0].


ProcessCore.Yaml.Dataset.toYamlStringIndexed (Some 2) (arc.HasPart[3])
|> ProcessCore.Yaml.Dataset.fromYamlString false


arc.RemovePart(arc.HasPart[0])

arc
|> Yaml.Dataset.toYamlStringIndexed (Some 2)
|> ProcessCore.Yaml.Dataset.fromYamlString false


arc.toYamlString(2)
|> ARC.fromYamlString


arc.toYamlString(2)
|> Yaml.Dataset.fromYamlString false


let yaml = 
    arc.toYamlString(2)
    |> YAMLicious.Reader.read

yaml
|> Yaml.Dataset.decoderGeneric (fun i -> Dataset(i)) false

yaml
|> Yaml.Dataset.decoderGeneric (fun i -> ARC(i)) false

arc
|> Yaml.Dataset.toYamlStringIndexed (Some 2)

|> ARC.fromYamlString





|> fun s -> System.IO.File.WriteAllText(@"C:\Users\HLWei\source\repos\ARCs\Ru_ChlamyHeatstress\datasetTest.yaml", s)

arc.toYamlString(2)
|> fun s -> System.IO.File.WriteAllText(@"C:\Users\HLWei\source\repos\ARCs\Ru_ChlamyHeatstress\arcTest.yaml", s)