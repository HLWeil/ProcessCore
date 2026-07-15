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

        static member ToSparseTable (study: Dataset) =
            let matrix = SparseTable.Create (keys = StudyInfo.Labels, length = 2)
            let mutable commentKeys = []
            let processedIdentifier, processedFileName =
                if study.Identifier.StartsWith(Identifier.MISSING_IDENTIFIER) then "", ""
                else study.Identifier, Identifier.Study.fileNameFromIdentifier study.Identifier
            do matrix.Matrix.Add((identifierLabel, 1), processedIdentifier)
            do matrix.Matrix.Add((titleLabel, 1), Option.defaultValue "" study.Title)
            do matrix.Matrix.Add((descriptionLabel, 1), Option.defaultValue "" study.Description)
            do matrix.Matrix.Add((submissionDateLabel, 1), Option.defaultValue "" study.DateCreated)
            do matrix.Matrix.Add((publicReleaseDateLabel, 1), Option.defaultValue "" study.DatePublished)
            do matrix.Matrix.Add((fileNameLabel, 1), processedFileName)

            match study.TryGetPropertyValue("Comments") with
            | Some (:? ResizeArray<DynamicObj> as comments) ->
                comments
                |> Seq.iter (fun comment ->
                    match Comment.toString comment with
                    | Some name, Some value ->
                        commentKeys <- name :: commentKeys
                        matrix.Matrix.Add((name, 1), value)
                    | _ -> ()
                )
            | _ -> ()

            { matrix with CommentKeys = commentKeys |> List.distinct |> List.rev }

        static member toRows (study: Dataset) =
            study
            |> StudyInfo.ToSparseTable
            |> SparseTable.ToRows
   
    
    /// FACTORS AND PROTOCOLS ARE NOT USED ANYMORE, Lukas, 21.03.24
    // We made these changes as merging duplicated top level metadata with the underlying Table sequence is time consuming, complex and error prone
    let fromParts (studyInfo:StudyInfo) (designDescriptors:DefinedTerm list) (publications: ScholarlyArticle list) (factors: Annotation list) (assays: Dataset list) (protocols : Recipe list) (contacts: Agent list) =
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
        if factors.Length > 0 then s.SetProperty("StudyFactors", ResizeArray factors)
        if assays.Length > 0 then s.SetProperty("Assays", ResizeArray assays)
        if protocols.Length > 0 then s.SetProperty("Protocols", ResizeArray protocols)
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

    let toSparseTable (study: Dataset) =
        StudyInfo.ToSparseTable study

    let toRows (study: Dataset) (assays: Dataset list option) =
        let designDescriptors =
            study.TryGetPropertyValue("StudyDesignDescriptors")
            |> Option.bind (fun v -> match v with | :? ResizeArray<DefinedTerm> as values -> Some values | _ -> None)
            |> Option.defaultValue (ResizeArray())
        let publications = study.Citations |> Seq.toList
        let contacts = study.Agents |> Seq.toList
        let protocols =
            match study.TryGetPropertyValue("Protocols") with
            | Some (:? ResizeArray<Recipe> as values) -> values |> Seq.toList
            | _ -> []
            //study.Processes
            //|> Seq.choose (fun p -> p.ExecutesRecipe)
            //|> Seq.toList
        let factors =
            match study.TryGetPropertyValue("StudyFactors") with
            | Some (:? ResizeArray<Annotation> as values) -> values |> Seq.toList
            | _ -> 
                study.AllAnnotations()
                |> Seq.filter (fun a -> a.AdditionalType = Some "Factor")
                |> Seq.distinct
                |> Seq.toList
        let assays =
            match assays with
            | Some items -> items
            | None ->
                match study.TryGetPropertyValue("Assays") with
                | Some (:? ResizeArray<Dataset> as values) -> values |> Seq.toList
                | _ -> []
                //match study.TryGetPropertyValue("AssayIdentifiers") with
                //| Some (:? ResizeArray<string> as ids) ->
                //    match study.PartOf with
                //    | Some parent ->
                //        ids
                //        |> Seq.map (fun id ->
                //            match parent.TryGetPart(id) with
                //            | Some assay -> assay
                //            | None -> Dataset(id, additionalType = "Assay")                       
                //        )
                //        |> Seq.toList
                //    | None -> 
                //        ids
                //        |> Seq.map (fun id -> Dataset(id, additionalType = "Assay"))
                //        |> Seq.toList
                //| _ -> []
        seq {
            yield! StudyInfo.toRows study

            yield SparseRow.fromValues [designDescriptorsLabel]
            yield! DesignDescriptors.toRows (Some designDescriptorsLabelPrefix) (List.ofSeq designDescriptors)

            yield SparseRow.fromValues [publicationsLabel]
            yield! Publications.toRows (Some publicationsLabelPrefix) publications

            yield SparseRow.fromValues [factorsLabel]
            yield! Factors.toRows (Some factorsLabelPrefix) factors

            yield SparseRow.fromValues [assaysLabel]
            yield! Assays.toRows (Some assaysLabelPrefix) assays

            yield SparseRow.fromValues [protocolsLabel]
            yield! Protocols.toRows (Some protocolsLabelPrefix) protocols

            yield SparseRow.fromValues [contactsLabel]
            yield! Contacts.toRows (Some contactsLabelPrefix) contacts
        }