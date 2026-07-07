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



//let arcPath = @"C:\Users\HLWei\source\repos\Ru_ChlamyHeatstress"

//let arc = ARC.load arcPath

//arc.ArcPath <- Some (Path.combine __SOURCE_DIRECTORY__ @"testARC")
////arc.IsSpreadsheetScaffold

//arc.Update()


let invIn = @"C:\Users\HLWei\source\repos\ARC-Data-Model\tests\ProcessCore.Tests\TestObjects\fct_gcqtof_assay.xlsx"
let invOut = @"C:\Users\HLWei\source\repos\ARC-Data-Model\tests\ProcessCore.Tests\TestResults\fct_gcqtof_assay_out.xlsx"


let wb = Path.readFileXlsxAsync invIn |> Async.RunSynchronously
let i = ScaffoldReader.Assay.tryFromFsWorkbook wb
let wb2 = ScaffoldReader.Assay.toFsWorkbook i.Value
Path.writeFileXlsxAsync invOut wb2 |> Async.RunSynchronously

i.Value.Tables.GetTableAt(0).Headers[3]

i.Value.Tables.GetTableAt(1).Processes.Count