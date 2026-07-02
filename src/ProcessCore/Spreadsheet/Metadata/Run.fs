namespace ProcessCore.Spreadsheet

open ProcessCore
open Comment
open Remark
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

    
    let fromString identifier title description (workflowIdentifiers : string option) measurementType measurementTypeTermSourceREF measurementTypeTermAccessionNumber technologyType technologyTypeTermSourceREF technologyTypeTermAccessionNumber technologyPlatform fileName comments : Dataset =
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
        run.SetProperty("WorkflowIdentifiers", workflowIdentifiers)
        run.SetProperty("Comments", comments)
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
