namespace ProcessCore.Spreadsheet

open ProcessCore
open ProcessCore.Helper
open FsSpreadsheet
open DynamicObj
open Comment
open Remark
open System.Collections.Generic

module ArcInvestigation = 

    let [<Literal>] identifierLabel = "Investigation Identifier"
    let [<Literal>] titleLabel = "Investigation Title"
    let [<Literal>] descriptionLabel = "Investigation Description"
    let [<Literal>] submissionDateLabel = "Investigation Submission Date"
    let [<Literal>] publicReleaseDateLabel = "Investigation Public Release Date"

    let [<Literal>] investigationLabel = "INVESTIGATION"
    let [<Literal>] ontologySourceReferenceLabel = "ONTOLOGY SOURCE REFERENCE"
    let [<Literal>] publicationsLabel = "INVESTIGATION PUBLICATIONS"
    let [<Literal>] contactsLabel = "INVESTIGATION CONTACTS"
    let [<Literal>] studyLabel = "STUDY"

    let [<Literal>] publicationsLabelPrefix = "Investigation Publication"
    let [<Literal>] contactsLabelPrefix = "Investigation Person"

    let [<Literal>] metadataSheetName = "isa_investigation"
    let [<Literal>] obsoleteMetadataSheetName = "Investigation"

    type InvestigationInfo =
        {
            Identifier : string
            Title : string
            Description : string
            SubmissionDate : string
            PublicReleaseDate : string
            Comments : DynamicObj list
        }

        static member create identifier title description submissionDate publicReleaseDate comments =
            {
                Identifier = identifier
                Title = title
                Description = description
                SubmissionDate = submissionDate
                PublicReleaseDate = publicReleaseDate
                Comments = comments
            }
  
        static member Labels = [identifierLabel; titleLabel; descriptionLabel; submissionDateLabel; publicReleaseDateLabel]
    
        static member FromSparseTable (matrix : SparseTable) =
        
            let i = 0

            let comments = 
                matrix.CommentKeys 
                |> List.map (fun k -> 
                    Comment.fromString k (matrix.TryGetValueDefault("", (k, i))))

            InvestigationInfo.create
                (matrix.TryGetValueDefault("", (identifierLabel, i)))  
                (matrix.TryGetValueDefault("", (titleLabel, i)))  
                (matrix.TryGetValueDefault("", (descriptionLabel, i)))  
                (matrix.TryGetValueDefault("", (submissionDateLabel, i)))  
                (matrix.TryGetValueDefault("", (publicReleaseDateLabel, i)))  
                comments


        static member fromRows lineNumber (rows : IEnumerator<SparseRow>) =
            SparseTable.FromRows(rows, InvestigationInfo.Labels, lineNumber)
            |> fun (s, ln, rs, sm) -> (s, ln, rs, InvestigationInfo.FromSparseTable sm)    
    
 
    let fromParts<'D when 'D :> Dataset> (createF : string -> 'D) (investigationInfo : InvestigationInfo) (ontologySourceReference : DynamicObj list) (publications : ScholarlyArticle list) (contacts : Agent list) (studies : Dataset list) (assays : Dataset list) (remarks : 'A list) : 'D =
        let studyIdentifiers = studies |> List.map (fun s -> s.Identifier)
        let i = createF investigationInfo.Identifier
        i.Title <- Option.fromValueWithDefault "" investigationInfo.Title
        i.Description <- Option.fromValueWithDefault "" investigationInfo.Description
        i.AdditionalType <- Some "Investigation"
        i.DateCreated <- Option.fromValueWithDefault "" investigationInfo.SubmissionDate
        i.DatePublished <- Option.fromValueWithDefault "" investigationInfo.PublicReleaseDate
        for contact in contacts do i.AddAgent(contact)
        for publication in publications do i.AddCitation(publication)
        if ontologySourceReference.Length > 0 then i.SetProperty("OntologySourceReferences", ontologySourceReference)
        if studyIdentifiers.Length > 0 then i.SetProperty("StudyIdentifiers", studyIdentifiers)
        if assays.Length > 0 then i.SetProperty("AssayIdentifiers", assays |> List.map (fun a -> a.Identifier))
        if investigationInfo.Comments.Length > 0 then i.SetProperty("Comments", ResizeArray investigationInfo.Comments)
        i


    let fromRows (createF : string -> 'D) (rows : seq<SparseRow>) =
        if Seq.isEmpty rows then failwith "isa_investigation sheet in Investigation file is empty"

        let en = rows.GetEnumerator()

        let emptyInvestigationInfo = InvestigationInfo.create "" "" "" "" "" []

        let rec loop lastLine ontologySourceReferences investigationInfo publications contacts studies remarks lineNumber =
            match lastLine with

            | Some k when k = ontologySourceReferenceLabel -> 
                let currentLine, lineNumber, newRemarks, ontologySourceReferences = OntologySourceReference.fromRows (lineNumber + 1) en
                loop currentLine ontologySourceReferences investigationInfo publications contacts studies (List.append remarks newRemarks) lineNumber

            | Some k when k = investigationLabel -> 
                let currentLine,lineNumber,newRemarks,investigationInfo = InvestigationInfo.fromRows (lineNumber + 1) en       
                loop currentLine ontologySourceReferences investigationInfo publications contacts studies (List.append remarks newRemarks) lineNumber

            | Some k when k = publicationsLabel -> 
                let currentLine,lineNumber,newRemarks,publications = Publications.fromRows (Some publicationsLabelPrefix) (lineNumber + 1) en       
                loop currentLine ontologySourceReferences investigationInfo publications contacts studies (List.append remarks newRemarks) lineNumber

            | Some k when k = contactsLabel -> 
                let currentLine,lineNumber, newRemarks, contacts = Contacts.fromRows (Some contactsLabelPrefix) (lineNumber + 1) en       
                loop currentLine ontologySourceReferences investigationInfo publications contacts studies (List.append remarks newRemarks) lineNumber

            | Some k when k = studyLabel -> 
                let currentLine, lineNumber, newRemarks, study = Studies.fromRows (lineNumber + 1) en  
                if study.Identifier <> "" && not (Identifier.isMissingIdentifier study.Identifier) then
                    loop currentLine ontologySourceReferences investigationInfo publications contacts (study::studies) (List.append remarks newRemarks) lineNumber
                else 
                    loop currentLine ontologySourceReferences investigationInfo publications contacts studies (List.append remarks newRemarks) lineNumber

            | _ ->
                match en.MoveNext() with
                | true ->
                    let currentLine = en.Current |> SparseRow.tryGetValueAt 0
                    loop currentLine ontologySourceReferences investigationInfo publications contacts studies remarks lineNumber
                | false ->
                    fromParts createF investigationInfo ontologySourceReferences publications contacts studies [] remarks

        let arcInvestigation =
            en.MoveNext() |> ignore
            let currentLine = en.Current |> SparseRow.tryGetValueAt 0
            loop currentLine [] emptyInvestigationInfo [] [] [] [] 1

        if arcInvestigation.Identifier.Equals System.String.Empty then failwith "Mandatory Investigation identifier is not present"

        arcInvestigation

    let fromMetadataSheet (createF : string -> 'D) (sheet : FsWorksheet) =
        try
            let rows =        
                sheet.Rows 
                |> Seq.map SparseRow.fromFsRow
            rows
            |> fromRows createF
        with 
        | err -> failwithf "Failed while parsing metadatasheet: %s" err.Message

    let isMetadataSheetName (name : string) =
        name = metadataSheetName

    let isMetadataSheet (sheet : FsWorksheet) =
        isMetadataSheetName sheet.Name

    let tryGetMetadataSheet (doc : FsWorkbook) =
        doc.GetWorksheets()
        |> Seq.tryFind isMetadataSheet
