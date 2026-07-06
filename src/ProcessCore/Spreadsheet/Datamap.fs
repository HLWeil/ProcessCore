module ProcessCore.Spreadsheet.Datamap

open ProcessCore
open ProcessCore.Helper
open FsSpreadsheet

/// Reads an datamap from a spreadsheet
let dataContextsFromFsWorkbook (doc : FsWorkbook) = 
    try
        let worksheets = doc.GetWorksheets()
        let sheetIsEmpty (sheet : FsWorksheet) = sheet.CellCollection.Count = 0
        let dataContexts = 
            worksheets
            |> Seq.tryPick DatamapTable.tryDataContextsFromFsWorksheet
        match dataContexts with
        | Some dc -> dc
        | None -> 
            if worksheets |> Seq.forall sheetIsEmpty then
                ResizeArray<DataContext>()
            else
                failwith "No DatamapTable was found in any of the sheets of the workbook"
    with
    | err -> failwithf "Could not parse datamap: \n%s" err.Message
            
let fromFsWorkbook (doc : FsWorkbook) = 
    try
        dataContextsFromFsWorkbook doc
        |> fun dataContexts -> Dataset(identifier = Identifier.createMissingIdentifier(), additionalType = "Datamap", dataContexts = dataContexts)
    with
    | err -> failwithf "Could not parse datamap: \n%s" err.Message


let toFsWorkbook (datamap : Dataset) =
    let doc = new FsWorkbook()

    DatamapTable.toFsWorksheet datamap
    |> doc.AddWorksheet
    doc