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


let invIn = @"C:\Users\HLWei\source\repos\ARC_tools\ARC-Data-Model\tests\ProcessCore.Tests\TestObjects\fct_gcqtof_assay.xlsx"
let invOut = @"C:\Users\HLWei\source\repos\ARC_tools\ARC-Data-Model\tests\ProcessCore.Tests\TestResults\fct_gcqtof_assay_out.xlsx"


let wb = Path.readFileXlsxAsync invIn |> Async.RunSynchronously
let i = ScaffoldReader.Assay.tryFromFsWorkbook wb
let wb2 = ScaffoldReader.Assay.toFsWorkbook i.Value
Path.writeFileXlsxAsync invOut wb2 |> Async.RunSynchronously

i.Value.Tables.GetTableAt(0).Headers[3]

i.Value.Tables.GetTableAt(1).Processes.Count

i.Value.Processes.Count



let inputs = 
    CompositeColumn(
        header = CompositeHeader.Input IOType.Sample,
        cells = ResizeArray [
            CompositeCell.FreeText "Std. Mix 5µM"
            CompositeCell.FreeText "Std. Mix 5µM"
            CompositeCell.FreeText "blank 1"
            CompositeCell.FreeText "blank 1"
            CompositeCell.FreeText "DB23"
            CompositeCell.FreeText "DB23"
        ]
    )

let protocolRef = 
    CompositeColumn(
        header = CompositeHeader.ProtocolREF,
        cells =  (List.init 6 (fun _ -> CompositeCell.FreeText "gas_chromatography.md") |> ResizeArray)
    )

let param = 
    CompositeColumn(
        header = CompositeHeader.Parameter(DefinedTerm(name = "MS sample type", tan = "DPBO:0000045")),
        cells = 
            (List.init 6 (fun i -> 
                if i <= 3 then CompositeCell.Term("",None)
                else CompositeCell.Term("material sample", Some "https://bioregistry.io/OBI:0000747")
            ) |> ResizeArray)
    )

let param2 = 
    CompositeColumn(
        header = CompositeHeader.Parameter(DefinedTerm(name = "Chromatography instrument model", tan = "DPBO:0000046")),
        cells =  (List.init 6 (fun _ -> CompositeCell.FreeText "Agilent 7890B GC") |> ResizeArray)
    )

let outputs = 
    CompositeColumn(
        header = CompositeHeader.Output IOType.Sample,
        cells = ResizeArray [
            CompositeCell.FreeText "150112_03"
            CompositeCell.FreeText "150112_04"
            CompositeCell.FreeText "150112_15"
            CompositeCell.FreeText "150112_16"
            CompositeCell.FreeText "150112_55"
            CompositeCell.FreeText "150112_56"
        ]
    )

let columns = 
    [| inputs; param; outputs |]
    |> ResizeArray
let d = Dataset("MyDataset")
let t = Table("SheetName", ResizeArray(),d)
columns
|> Seq.iter (fun c -> t.AddColumn(c.Header, c.Cells))
d.CollapseProcesses()




d.Processes.Count
d.Tables.GetTableAt(0)



d.Tables.GetTableAt(0).ColumnCount
d.Tables.GetTableAt(0).Columns
d.Tables.GetTableAt(0).Dataset
d.Tables.GetTableAt(0).Headers
d.Tables.GetTableAt(0).Name
d.Tables.GetTableAt(0).Processes
d.Tables.GetTableAt(0).RowCount

d.Tables.GetTableAt(0).Columns[0].Cells.Count


let ws = Table.toFsWorksheet (Some 1) (d.Tables.GetTableAt(0))
ws.RescanRows()
ws.Rows.Count

// d.Tables.GetTableAt(0).Columns[0].Cells.Count

let cs = 
    d.Tables.GetTableAt(0).Columns
    |> List.ofSeq
    |> List.sortBy Table.classifyColumnOrder
    |> List.collect CompositeColumn.toStringCellColumns

d.Tables.GetTableAt(0).Columns
|> List.ofSeq
|> Seq.head
|> fun c -> c.Cells
|> Seq.toArray

let firstCall = d.Tables.GetTableAt(0).Columns[0].Cells
let secondCall = d.Tables.GetTableAt(0).Columns[0].Cells

firstCall
secondCall






















let inputs = 
    CompositeColumn(
        header = CompositeHeader.Input IOType.Sample,
        cells = ResizeArray [
            CompositeCell.FreeText "Std. Mix 5µM"
            CompositeCell.FreeText "blank 1"
            CompositeCell.FreeText "DB23"
        ]
    )

let param = 
    CompositeColumn(
        header = CompositeHeader.Parameter(DefinedTerm(name = "MS sample type", tan = "DPBO:0000045")),
        cells = ResizeArray [
            CompositeCell.Term("",None)
            CompositeCell.Term("",None)
            CompositeCell.Term("material sample", Some "https://bioregistry.io/OBI:0000747")
        ]
    )

let outputs = 
    CompositeColumn(
        header = CompositeHeader.Output IOType.Sample,
        cells = ResizeArray [
            CompositeCell.FreeText "150112_03"
            CompositeCell.FreeText "150112_15"
            CompositeCell.FreeText "150112_55"
        ]
    )

let columns = 
    [| inputs; param; outputs |]
    |> ResizeArray
let d = Dataset("MyDataset")
let t = Table("SheetName", ResizeArray(),d)
columns
|> Seq.iter (fun c -> t.AddColumn(c.Header, c.Cells))

d.CollapseProcesses()


let cells = d.Tables.GetTableAt(0).Columns[0].Cells 
inputs.Cells

d.Processes.Count

Expect.equal  2 "2 processes after collapse"
Expect.equal (d.Tables.GetTableAt(0).RowCount) 3 "3 rows in collapsed table"
Expect.equal (d.Tables.GetTableAt(0).Processes.Count) 2 "2 processes in collapsed table"