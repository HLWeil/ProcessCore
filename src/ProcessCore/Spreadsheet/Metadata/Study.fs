namespace ProcessCore.Spreadsheet

open ProcessCore
open System.Collections.Generic
open ProcessCore.Helper
open DynamicObj

module Studies = 

    let [<Literal>] identifierLabel = "Study Identifier"
    let [<Literal>] titleLabel = "Study Title"
    let [<Literal>] descriptionLabel = "Study Description"
    let [<Literal>] submissionDateLabel = "Study Submission Date"
    let [<Literal>] publicReleaseDateLabel = "Study Public Release Date"
    let [<Literal>] fileNameLabel = "Study File Name"

    let [<Literal>] designDescriptorsLabelPrefix = "Study Design"
    let [<Literal>] publicationsLabelPrefix = "Study Publication"
    let [<Literal>] factorsLabelPrefix = "Study Factor"
    let [<Literal>] assaysLabelPrefix = "Study Assay"
    let [<Literal>] protocolsLabelPrefix = "Study Protocol"
    let [<Literal>] contactsLabelPrefix = "Study Person"

    let [<Literal>] designDescriptorsLabel = "STUDY DESIGN DESCRIPTORS"
    let [<Literal>] publicationsLabel = "STUDY PUBLICATIONS"
    let [<Literal>] factorsLabel = "STUDY FACTORS"
    let [<Literal>] assaysLabel = "STUDY ASSAYS"
    let [<Literal>] protocolsLabel = "STUDY PROTOCOLS"
    let [<Literal>] contactsLabel = "STUDY CONTACTS"

    type StudyInfo =
        {
        Identifier : string
        Title : string
        Description : string
        SubmissionDate : string
        PublicReleaseDate : string
        FileName : string
        Comments : DynamicObj list
        }

        static member create identifier title description submissionDate publicReleaseDate fileName comments =
            {
            Identifier = identifier
            Title = title
            Description = description
            SubmissionDate = submissionDate
            PublicReleaseDate = publicReleaseDate
            FileName = fileName
            Comments = comments
            }
  
        static member Labels = [identifierLabel;titleLabel;descriptionLabel;submissionDateLabel;publicReleaseDateLabel;fileNameLabel]
    
        static member FromSparseTable (matrix : SparseTable) =
        
            let i = 0

            let comments = 
                matrix.CommentKeys 
                |> List.map (fun k -> 
                    Comment.fromString k (matrix.TryGetValueDefault("",(k,i))))

            StudyInfo.create
                (matrix.TryGetValueDefault(Identifier.createMissingIdentifier(),(identifierLabel,i)))  
                (matrix.TryGetValueDefault("",(titleLabel,i)))  
                (matrix.TryGetValueDefault("",(descriptionLabel,i)))  
                (matrix.TryGetValueDefault("",(submissionDateLabel,i)))  
                (matrix.TryGetValueDefault("",(publicReleaseDateLabel,i)))  
                (matrix.TryGetValueDefault("",(fileNameLabel,i)))                    
                comments


        static member fromRows lineNumber (rows : IEnumerator<SparseRow>) =
            SparseTable.FromRows(rows,StudyInfo.Labels,lineNumber)
            |> fun (s,ln,rs,sm) -> (s,ln,rs, StudyInfo.FromSparseTable sm)
   
    
    /// FACTORS AND PROTOCOLS ARE NOT USED ANYMORE, Lukas, 21.03.24
    // We made these changes as merging duplicated top level metadata with the underlying Table sequence is time consuming, complex and error prone
    let fromParts (studyInfo:StudyInfo) (designDescriptors:DefinedTerm list) (publications: ScholarlyArticle list) (factors: Annotation list) (assays: Dataset list) (protocols : Recipe list) (contacts: Agent list) =
        let assayIdentifiers = assays |> List.map (fun assay -> assay.Identifier)
        let s = Dataset(
            studyInfo.Identifier,
            additionalType = "Study",
            ?title = Option.fromValueWithDefault "" studyInfo.Title,
            ?description = Option.fromValueWithDefault "" studyInfo.Description,
            ?dateCreated = Option.fromValueWithDefault "" studyInfo.SubmissionDate,
            ?datePublished = Option.fromValueWithDefault "" studyInfo.PublicReleaseDate,
            agents = ResizeArray contacts,
            citations = ResizeArray publications       
        ) 
        if designDescriptors.Length > 0 then s.SetProperty("StudyDesignDescriptors", ResizeArray designDescriptors)
        if studyInfo.Comments.Length > 0 then s.SetProperty("Comments", ResizeArray studyInfo.Comments)
        if assayIdentifiers.Length > 0 then s.SetProperty("AssayIdentifiers", ResizeArray assayIdentifiers)
        s

    let fromRows lineNumber (en:IEnumerator<SparseRow>) = 

        let rec loop lastLine (studyInfo : StudyInfo) designDescriptors publications factors assays protocols contacts remarks lineNumber =
           
            match lastLine with

            | Some k when k = designDescriptorsLabel -> 
                let currentLine,lineNumber,newRemarks,designDescriptors = DesignDescriptors.fromRows (Some designDescriptorsLabelPrefix) (lineNumber + 1) en         
                loop currentLine studyInfo designDescriptors publications factors assays protocols contacts (List.append remarks newRemarks) lineNumber

            | Some k when k = publicationsLabel -> 
                let currentLine,lineNumber,newRemarks,publications = Publications.fromRows (Some publicationsLabelPrefix) (lineNumber + 1) en       
                loop currentLine studyInfo designDescriptors publications factors assays protocols contacts (List.append remarks newRemarks) lineNumber

            | Some k when k = factorsLabel -> 
                let currentLine,lineNumber,newRemarks,factors = Factors.fromRows (Some factorsLabelPrefix) (lineNumber + 1) en       
                loop currentLine studyInfo designDescriptors publications factors assays protocols contacts (List.append remarks newRemarks) lineNumber

            | Some k when k = assaysLabel -> 
                let currentLine,lineNumber,newRemarks,assays = Assays.fromRows (Some assaysLabelPrefix) (lineNumber + 1) en       
                loop currentLine studyInfo designDescriptors publications factors assays protocols contacts (List.append remarks newRemarks) lineNumber

            | Some k when k = protocolsLabel -> 
                let currentLine,lineNumber,newRemarks,protocols = Protocols.fromRows (Some protocolsLabelPrefix) (lineNumber + 1) en  
                loop currentLine studyInfo designDescriptors publications factors assays protocols contacts (List.append remarks newRemarks) lineNumber

            | Some k when k = contactsLabel -> 
                let currentLine,lineNumber,newRemarks,contacts = Contacts.fromRows (Some contactsLabelPrefix) (lineNumber + 1) en  
                loop currentLine studyInfo designDescriptors publications factors assays protocols contacts (List.append remarks newRemarks) lineNumber

            | k -> 
                k,lineNumber,remarks, fromParts studyInfo designDescriptors publications factors assays protocols contacts
    
        let currentLine,lineNumber,remarks,item = StudyInfo.fromRows lineNumber en  
        loop currentLine item [] [] [] [] [] [] remarks lineNumber