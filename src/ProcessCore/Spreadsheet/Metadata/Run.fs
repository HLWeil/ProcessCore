namespace ProcessCore.Spreadsheet

open ProcessCore
open DynamicObj
open System.Collections.Generic
open ProcessCore.Helper


module Run = 

    let [<Literal>] identifierLabel =                         "Identifier"
    let [<Literal>] titleLabel =                              "Title"
    let [<Literal>] descriptionLabel =                        "Description"
    let [<Literal>] workflowIdentifiersLabel =                "Workflow Identifiers"
    let [<Literal>] measurementTypeLabel =                    "Measurement Type"
    let [<Literal>] measurementTypeTermAccessionNumberLabel = "Measurement Type Term Accession Number"
    let [<Literal>] measurementTypeTermSourceREFLabel =       "Measurement Type Term Source REF"
    let [<Literal>] technologyTypeLabel =                     "Technology Type"
    let [<Literal>] technologyTypeTermAccessionNumberLabel =  "Technology Type Term Accession Number"
    let [<Literal>] technologyTypeTermSourceREFLabel =        "Technology Type Term Source REF"
    let [<Literal>] technologyPlatformLabel =                 "Technology Platform"
    let [<Literal>] fileNameLabel =                           "File Name"

    let [<Literal>] runLabel = "RUN"
    let [<Literal>] performersLabel = "RUN PERFORMERS"

    let [<Literal>] runLabelPrefix = "Run"
    let [<Literal>] performersLabelPrefix = "Run Person"

    let labels = 
        [
            identifierLabel; titleLabel; descriptionLabel; workflowIdentifiersLabel; measurementTypeLabel;measurementTypeTermAccessionNumberLabel;measurementTypeTermSourceREFLabel;
            technologyTypeLabel;technologyTypeTermAccessionNumberLabel;technologyTypeTermSourceREFLabel;technologyPlatformLabel;fileNameLabel
        ]

    
    let fromString identifier title description (workflowIdentifiers : string option) measurementType measurementTypeTermSourceREF measurementTypeTermAccessionNumber technologyType technologyTypeTermSourceREF technologyTypeTermAccessionNumber technologyPlatform fileName (comments : ResizeArray<DynamicObj>) : Dataset =
        let workflowIdentifiers =
            match workflowIdentifiers with
            | Some wi -> wi.Split(';') |> ResizeArray
            | None -> ResizeArray()
        let measurementType = measurementType |> Option.map (fun mt -> DefinedTerm(name = mt, ?tan = measurementTypeTermAccessionNumber))
        let technologyType = technologyType |> Option.map (fun tt -> DefinedTerm(name = tt, ?tan = technologyTypeTermAccessionNumber))
        let identifier =
            match identifier with
            | Some identifier -> identifier
            | None ->
                match fileName with
                | Some fileName ->
                    match Identifier.Assay.tryIdentifierFromFileName fileName with
                    | Some identifier -> identifier
                    | _ -> Identifier.createMissingIdentifier()
                | None -> Identifier.createMissingIdentifier()
        let run = Dataset(identifier, ?title = title, ?description = description, additionalType = "Run")
        measurementType |> Option.iter (fun value -> run.SetProperty("MeasurementType", value))
        technologyType |> Option.iter (fun value -> run.SetProperty("TechnologyType", value))
        technologyPlatform |> Option.iter (fun value -> run.SetProperty("TechnologyPlatform", value))
        if workflowIdentifiers.Count > 0 then run.SetProperty("WorkflowIdentifiers", workflowIdentifiers)
        if comments.Count > 0 then run.SetProperty("Comments", comments)
        run
        
    let fromSparseTable (matrix : SparseTable) : Dataset =
        let i = 0

        let comments = 
            matrix.CommentKeys 
            |> List.map (fun k -> 
                Comment.fromString k (matrix.TryGetValueDefault("",(k,i))))
            |> ResizeArray

        fromString
            (matrix.TryGetValue(identifierLabel,i))
            (matrix.TryGetValue(titleLabel,i))
            (matrix.TryGetValue(descriptionLabel,i))
            (matrix.TryGetValue(workflowIdentifiersLabel,i))
            (matrix.TryGetValue(measurementTypeLabel,i))            
            (matrix.TryGetValue((measurementTypeTermSourceREFLabel,i)))
            (matrix.TryGetValue((measurementTypeTermAccessionNumberLabel,i)))
            (matrix.TryGetValue(technologyTypeLabel,i))             
            (matrix.TryGetValue((technologyTypeTermSourceREFLabel,i)))   
            (matrix.TryGetValue((technologyTypeTermAccessionNumberLabel,i))) 
            (matrix.TryGetValue(technologyPlatformLabel,i))     
            (matrix.TryGetValue(fileNameLabel,i))                    
            comments

    let fromRows lineNumber (rows : IEnumerator<SparseRow>) =
        SparseTable.FromRows(rows,labels,lineNumber,prefix = runLabelPrefix)
        |> fun (s,ln,rs,sm) -> (s,ln,rs, fromSparseTable sm)

    let toSparseTable (run: Dataset) =
        let matrix = SparseTable.Create (keys = labels, length = 2)
        let mutable commentKeys = []
        let processedIdentifier, processedFileName =
            if run.Identifier.StartsWith(Identifier.MISSING_IDENTIFIER) then "", ""
            else run.Identifier, Identifier.Run.fileNameFromIdentifier run.Identifier
        let workflowIdentifiers =
            match run.TryGetPropertyValue("WorkflowIdentifiers") with
            | Some (:? ResizeArray<string> as ids) -> String.concat ";" ids
            | _ -> ""
        let measurementType =
            run.TryGetPropertyValue("MeasurementType")
            |> Option.bind (fun v -> match v with | :? DefinedTerm as dt -> Some dt | _ -> None)
            |> Option.defaultValue (DefinedTerm(""))
        let technologyType =
            run.TryGetPropertyValue("TechnologyType")
            |> Option.bind (fun v -> match v with | :? DefinedTerm as dt -> Some dt | _ -> None)
            |> Option.defaultValue (DefinedTerm(""))
        let technologyPlatform =
            run.TryGetPropertyValue("TechnologyPlatform")
            |> Option.bind (fun v -> match v with | :? string as s -> Some s | _ -> None)
        do matrix.Matrix.Add((identifierLabel, 1), processedIdentifier)
        do matrix.Matrix.Add((titleLabel, 1), Option.defaultValue "" run.Title)
        do matrix.Matrix.Add((descriptionLabel, 1), Option.defaultValue "" run.Description)
        do matrix.Matrix.Add((workflowIdentifiersLabel, 1), workflowIdentifiers)
        do matrix.Matrix.Add((measurementTypeLabel, 1), measurementType.Name)
        do matrix.Matrix.Add((measurementTypeTermAccessionNumberLabel, 1), measurementType.TAN |> Option.defaultValue "")
        do matrix.Matrix.Add((measurementTypeTermSourceREFLabel, 1), measurementType.TryGetTSR() |> Option.defaultValue "")
        do matrix.Matrix.Add((technologyTypeLabel, 1), technologyType.Name)
        do matrix.Matrix.Add((technologyTypeTermAccessionNumberLabel, 1), technologyType.TAN |> Option.defaultValue "")
        do matrix.Matrix.Add((technologyTypeTermSourceREFLabel, 1), technologyType.TryGetTSR() |> Option.defaultValue "")
        do matrix.Matrix.Add((technologyPlatformLabel, 1), Option.defaultValue "" technologyPlatform)
        do matrix.Matrix.Add((fileNameLabel, 1), processedFileName)

        match run.TryGetPropertyValue("Comments") with
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

    let toRows (run : Dataset) =
        run
        |> toSparseTable
        |> fun m -> SparseTable.ToRows(m, prefix = runLabelPrefix)
