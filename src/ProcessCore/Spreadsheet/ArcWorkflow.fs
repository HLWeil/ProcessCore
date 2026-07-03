namespace ProcessCore.Spreadsheet

open ProcessCore
open ProcessCore.Helper
open ProcessCore.Spreadsheet
open FsSpreadsheet
open System.Collections.Generic

module ArcWorkflow =

    let [<Literal>] metadataSheetName = "isa_workflow"
    let [<Literal>] workflowLabel = "WORKFLOW"
    let [<Literal>] contactsLabel = "WORKFLOW CONTACTS"

    let [<Literal>] workflowLabelPrefix = "Workflow"
    let [<Literal>] contactsLabelPrefix = "Workflow Person"
        
    let fromRows (rows : seq<SparseRow>) = 

        let en = rows.GetEnumerator()

        let rec loop lastRow workflow contacts rowNumber =
               
            match lastRow with
            | Some prefix when prefix = workflowLabel -> 
                let currentRow, rowNumber, _, workflow = Workflow.fromRows (rowNumber + 1) en
                loop currentRow (Some workflow) contacts rowNumber

            | Some prefix when prefix = contactsLabel -> 
                let currentLine, rowNumber, _, contacts = Contacts.fromRows (Some contactsLabelPrefix) (rowNumber + 1) en  
                loop currentLine workflow contacts rowNumber
            | _ -> 
                match workflow, contacts with
                | None, contacts ->
                    let workflow : Dataset = Dataset(Identifier.createMissingIdentifier(), additionalType = "Workflow")
                    contacts |> Seq.iter workflow.AddAgent
                    workflow
                | Some workflow, contacts ->
                    let workflow : Dataset = workflow
                    contacts |> Seq.iter workflow.AddAgent
                    workflow
        
        if en.MoveNext () then
            let currentLine = en.Current |> SparseRow.tryGetValueAt 0
            loop currentLine None [] 1
            
        else
            failwith "empty workflow metadata sheet"

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
