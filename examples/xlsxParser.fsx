#r "nuget: Fable.Core, 4.3.0"
#r "nuget: DynamicObj"
#r "nuget: FsSpreadsheet, 7.0.0-alpha.1"
#r "nuget: FsSpreadsheet.Net, 7.0.0-alpha.1"

#r @"..\src\ProcessCore\bin\Release\netstandard2.1\ProcessCore.dll"

// #r "nuget: ProcessCore, 0.0.3"
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

arc.RegisterFragmentSelectorProvider (CsvFragmentSelectorProvider())

let d = 
    arc.AllData()
    |> Seq.find (fun d -> d.Selector.IsSome)

arc.DataContextsCoveringData(d).[0].Explication