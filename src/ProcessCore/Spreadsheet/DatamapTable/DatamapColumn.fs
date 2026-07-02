module ProcessCore.Spreadsheet.DatamapColumn

open ProcessCore
open ProcessCore.Table
open ProcessCore.Helper
open FsSpreadsheet
open DatamapHeader.ActivePattern

let setFromFsColumns (dc : ResizeArray<DataContext>) (columns : list<FsColumn>) : ResizeArray<DataContext> =
    let cellParser = 
        columns
        |> List.map (fun c -> c.[1])
        |> DatamapHeader.fromFsCells
    for i = 0 to dc.Count - 1 do
        columns
        |> List.map (fun c -> c.[i+2])
        |> cellParser (dc.[i])
        |> ignore
    dc
