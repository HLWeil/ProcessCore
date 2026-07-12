namespace ProcessCore.Spreadsheet

open ProcessCore
open ProcessCore.Helper
open ProcessCore.Spreadsheet
open FsSpreadsheet

module ArcStudy = 

    let [<Literal>] obsoleteStudiesLabel = "STUDY METADATA"
    let [<Literal>] studiesLabel = "STUDY"

    let [<Literal>] obsoleteMetadataSheetName = "Study"
    let [<Literal>] metadataSheetName = "isa_study"

    let fromRows (rows : seq<SparseRow>) =
        let en = rows.GetEnumerator()
        en.MoveNext() |> ignore  
        let _, _, _,study = Studies.fromRows 2 en
        study

    let fromMetadataSheet (sheet : FsWorksheet) : Dataset*Dataset list =
        try            
            sheet.Rows 
            |> Seq.map SparseRow.fromFsRow
            |> fromRows
            |> fun study -> (study, [])
        with 
        | err -> failwithf "Failed while parsing metadatasheet: %s" err.Message

    let fromMetadataCollection (collection : seq<seq<string option>>) : Dataset*Dataset list =
        try
            collection
            |> Seq.map SparseRow.fromAllValues
            |> fromRows
            |> fun study -> (study, [])
        with 
        | err -> failwithf "Failed while parsing metadatasheet: %s" err.Message

    let isMetadataSheetName (name : string) =
        name = metadataSheetName || name = obsoleteMetadataSheetName

    let isMetadataSheet (sheet : FsWorksheet) =
        isMetadataSheetName sheet.Name

    let tryGetMetadataSheet (doc : FsWorkbook) =
        doc.GetWorksheets()
        |> Seq.tryFind isMetadataSheet

    let toMetadataSheet (study : Dataset) (assays : Dataset list option) : FsWorksheet =
        //let toRows (study:ArcStudy) assays =
        //    seq {          
        //        yield  SparseRow.fromValues [studiesLabel]
        //        yield! Studies.StudyInfo.toRows study
        //    }
        let sheet = FsWorksheet(metadataSheetName)
        Studies.toRows study assays
        |> Seq.append [SparseRow.fromValues [studiesLabel]]
        |> Seq.iteri (fun rowI r -> SparseRow.writeToSheet (rowI + 1) r sheet)    
        sheet

    let toMetadataCollection (study : Dataset) (assays : Dataset list option) =
        Studies.toRows study assays
        |> Seq.append [SparseRow.fromValues [studiesLabel]]
        |> Seq.map (fun row -> SparseRow.getAllValues row)