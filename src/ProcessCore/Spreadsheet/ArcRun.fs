namespace ProcessCore.Spreadsheet

open ProcessCore
open ProcessCore.Helper
open ProcessCore.Spreadsheet
open FsSpreadsheet

module ArcRun =

    let [<Literal>] metadataSheetName = "isa_run"
    let [<Literal>] runLabel = "RUN"
    let [<Literal>] performersLabel = "RUN PERFORMERS"

    let [<Literal>] runLabelPrefix = "Run"
    let [<Literal>] performersLabelPrefix = "Run Person"

    let fromRows (rows : seq<SparseRow>) = 

        let en = rows.GetEnumerator()

        let rec loop lastRow run performers rowNumber =
               
            match lastRow with
            | Some prefix when prefix = runLabel -> 
                let currentRow, rowNumber, _, run = Run.fromRows (rowNumber + 1) en
                loop currentRow (Some run) performers rowNumber

            | Some prefix when prefix = performersLabel -> 
                let currentLine, rowNumber, _, performers = Contacts.fromRows (Some performersLabelPrefix) (rowNumber + 1) en  
                loop currentLine run performers rowNumber
            | _ -> 
                match run, performers with
                | None, performers ->
                    let run : Dataset = Dataset(Identifier.createMissingIdentifier(), additionalType = "Run")
                    performers |> Seq.iter run.AddAgent
                    run
                | Some run, performers ->
                    let run : Dataset = run
                    performers |> Seq.iter run.AddAgent
                    run
        
        if en.MoveNext () then
            let currentLine = en.Current |> SparseRow.tryGetValueAt 0
            loop currentLine None [] 1
            
        else
            failwith "empty run metadata sheet"

    let fromMetadataSheet (sheet : FsWorksheet) : Dataset =
        try
            let rows =        
                sheet.Rows 
                |> Seq.map SparseRow.fromFsRow
            rows
            |> fromRows
        with 
        | err -> failwithf "Failed while parsing metadatasheet: %s" err.Message

    let isMetadataSheetName (name : string) =
        name = metadataSheetName

    let isMetadataSheet (sheet : FsWorksheet) =
        isMetadataSheetName sheet.Name

    let tryGetMetadataSheet (doc : FsWorkbook) =
        doc.GetWorksheets()
        |> Seq.tryFind isMetadataSheet
