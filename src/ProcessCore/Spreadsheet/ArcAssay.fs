namespace ProcessCore.Spreadsheet

open ProcessCore
open ProcessCore.Helper
open ProcessCore.Spreadsheet
open FsSpreadsheet

module ArcAssay =

    let [<Literal>] obsoleteAssaysLabel = "ASSAY METADATA"
    let [<Literal>] assaysLabel = "ASSAY"
    let [<Literal>] contactsLabel = "ASSAY PERFORMERS"

    let [<Literal>] assaysPrefix = "Assay"
    let [<Literal>] contactsPrefix = "Assay Person"

    let [<Literal>] obsoleteMetadataSheetName = "Assay"
    let [<Literal>] metadataSheetName = "isa_assay"

    let fromRows (rows : seq<SparseRow>) =
        let hasPrefix = 
            rows
            |> Seq.exists (fun row -> row |> Seq.head |> snd |> fun s -> s.StartsWith(assaysPrefix))
        let aPrefix, cPrefix = 
            if hasPrefix then 
                Some assaysPrefix, Some contactsPrefix
            else None, None
        let en = rows.GetEnumerator()
        let rec loop lastRow assay performers rowNumber =
               
            match lastRow with
            | Some prefix when prefix = assaysLabel || prefix = obsoleteAssaysLabel -> 
                let currentRow, rowNumber, _, assays = Assays.fromRows aPrefix (rowNumber + 1) en       
                let assay = assays |> List.tryHead |> Option.orElse assay
                loop currentRow assay performers rowNumber

            | Some prefix when prefix = contactsLabel -> 
                let currentLine, rowNumber, _, contacts = Contacts.fromRows cPrefix (rowNumber + 1) en  
                loop currentLine assay (Some contacts) rowNumber
            | _ -> 
                let assay =
                    assay
                    |> Option.defaultWith (fun () -> Dataset(Identifier.createMissingIdentifier(), additionalType = "Assay"))
                performers
                |> Option.iter (fun contacts ->
                    assay.Agents.Clear()
                    contacts |> Seq.iter assay.AddAgent)
                assay
        
        if en.MoveNext () then
            let currentLine = en.Current |> SparseRow.tryGetValueAt 0
            loop currentLine None None 1
            
        else
            failwith "empty assay metadata sheet"

    let fromMetadataSheet (sheet : FsWorksheet) : Dataset =
        try
            let rows =        
                sheet.Rows 
                |> Seq.map SparseRow.fromFsRow
            rows
            |> fromRows
        with 
        | err -> failwithf "Failed while parsing metadatasheet: %s" err.Message

    let fromMetadataCollection (collection : seq<seq<string option>>) : Dataset =
        try
            let rows =        
                collection 
                |> Seq.map SparseRow.fromAllValues   
            rows
            |> fromRows
        with 
        | err -> failwithf "Failed while parsing metadatasheet: %s" err.Message

    let isMetadataSheetName (name : string) =
        name = metadataSheetName || name = obsoleteMetadataSheetName

    let isMetadataSheet (sheet : FsWorksheet) =
        isMetadataSheetName sheet.Name

    let tryGetMetadataSheet (doc : FsWorkbook) =
        doc.GetWorksheets()
        |> Seq.tryFind isMetadataSheet
