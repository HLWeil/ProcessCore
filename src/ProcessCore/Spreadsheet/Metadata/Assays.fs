namespace ProcessCore.Spreadsheet

open ProcessCore
open DynamicObj
open Comment
open Remark
open System.Collections.Generic
open ProcessCore.Helper


module Assays = 

    let [<Literal>] identifierLabel =                         "Identifier"
    let [<Literal>] titleLabel =                              "Title"
    let [<Literal>] descriptionLabel =                        "Description"
    let [<Literal>] measurementTypeLabel =                    "Measurement Type"
    let [<Literal>] measurementTypeTermAccessionNumberLabel = "Measurement Type Term Accession Number"
    let [<Literal>] measurementTypeTermSourceREFLabel =       "Measurement Type Term Source REF"
    let [<Literal>] technologyTypeLabel =                     "Technology Type"
    let [<Literal>] technologyTypeTermAccessionNumberLabel =  "Technology Type Term Accession Number"
    let [<Literal>] technologyTypeTermSourceREFLabel =        "Technology Type Term Source REF"
    let [<Literal>] technologyPlatformLabel =                 "Technology Platform"
    let [<Literal>] fileNameLabel =                           "File Name"

    let labels = 
        [
            identifierLabel; titleLabel; descriptionLabel; measurementTypeLabel;measurementTypeTermAccessionNumberLabel;measurementTypeTermSourceREFLabel;
            technologyTypeLabel;technologyTypeTermAccessionNumberLabel;technologyTypeTermSourceREFLabel;technologyPlatformLabel;fileNameLabel
        ]

    
    let fromString identifier title description measurementType measurementTypeTermSourceREF measurementTypeTermAccessionNumber technologyType technologyTypeTermSourceREF technologyTypeTermAccessionNumber (technologyPlatform : string option) fileName (comments : ResizeArray<DynamicObj>) : Dataset = 
        let measurementType = measurementType |> Option.map (fun mt -> DefinedTerm(name = mt,?tan = measurementTypeTermAccessionNumber))
        let technologyType = technologyType |> Option.map (fun tt -> DefinedTerm(name = tt,?tan = technologyTypeTermAccessionNumber))
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
        let assay = Dataset(
            identifier = identifier,
            ?title = title,
            ?description = description,
            additionalType = "Assay"
        )
        if measurementType.IsSome then assay.SetProperty("MeasurementType",measurementType.Value)
        if technologyType.IsSome then assay.SetProperty("TechnologyType",technologyType.Value)
        if technologyPlatform.IsSome then assay.SetProperty("TechnologyPlatform",technologyPlatform.Value)
        if comments.Count > 0 then assay.SetProperty("Comments",comments)
        assay

        
    let fromSparseTable (matrix : SparseTable) : Dataset list=
        if matrix.ColumnCount = 0 && matrix.CommentKeys.Length <> 0 then
            let comments = SparseTable.GetEmptyComments matrix
            let ds = Dataset(Identifier.createMissingIdentifier())
            ds.SetProperty("Comments",comments)
            ds
            |> List.singleton
        else
            List.init matrix.ColumnCount (fun i -> 

                let comments = 
                    matrix.CommentKeys 
                    |> List.map (fun k -> 
                        Comment.fromString k (matrix.TryGetValueDefault("",(k,i))))
                    |> ResizeArray

                fromString
                    (matrix.TryGetValue(identifierLabel,i))
                    (matrix.TryGetValue(titleLabel,i))
                    (matrix.TryGetValue(descriptionLabel,i))
                    (matrix.TryGetValue(measurementTypeLabel,i))            
                    (matrix.TryGetValue((measurementTypeTermSourceREFLabel,i)))
                    (matrix.TryGetValue((measurementTypeTermAccessionNumberLabel,i)))
                    (matrix.TryGetValue(technologyTypeLabel,i))             
                    (matrix.TryGetValue((technologyTypeTermSourceREFLabel,i)))   
                    (matrix.TryGetValue((technologyTypeTermAccessionNumberLabel,i))) 
                    (matrix.TryGetValue(technologyPlatformLabel,i))     
                    (matrix.TryGetValue(fileNameLabel,i))                    
                    comments
            )


    let fromRows (prefix : string option) lineNumber (rows : IEnumerator<SparseRow>) =
        SparseTable.FromRows(rows,labels,lineNumber,?prefix = prefix)
        |> fun (s,ln,rs,sm) -> (s,ln,rs, fromSparseTable sm)

    let toSparseTable (assays: Dataset list) =
        let matrix = SparseTable.Create (keys = labels, length = assays.Length + 1)
        let mutable commentKeys = []
        assays
        |> List.iteri (fun i assay ->
            let i = i + 1
            let measurementType =
                assay.TryGetPropertyValue("MeasurementType")
                |> Option.bind (fun v -> match v with | :? DefinedTerm as dt -> Some dt | _ -> None)
            let technologyType =
                assay.TryGetPropertyValue("TechnologyType")
                |> Option.bind (fun v -> match v with | :? DefinedTerm as dt -> Some dt | _ -> None)
            let technologyPlatform =
                assay.TryGetPropertyValue("TechnologyPlatform")
                |> Option.bind (fun v -> match v with | :? string as s -> Some s | _ -> None)
            let processedFileName =
                if Identifier.isMissingIdentifier assay.Identifier then ""
                else Identifier.Assay.fileNameFromIdentifier assay.Identifier
            let mt = measurementType |> Option.defaultValue (DefinedTerm(""))
            let tt = technologyType |> Option.defaultValue (DefinedTerm(""))
            do matrix.Matrix.Add((identifierLabel, i), Identifier.removeMissingIdentifier assay.Identifier)
            do matrix.Matrix.Add((titleLabel, i), Option.defaultValue "" assay.Title)
            do matrix.Matrix.Add((descriptionLabel, i), Option.defaultValue "" assay.Description)
            do matrix.Matrix.Add((measurementTypeLabel, i), mt.Name)
            do matrix.Matrix.Add((measurementTypeTermAccessionNumberLabel, i), mt.TAN |> Option.defaultValue "")
            do matrix.Matrix.Add((measurementTypeTermSourceREFLabel, i), mt.TryGetTSR() |> Option.defaultValue "")
            do matrix.Matrix.Add((technologyTypeLabel, i), tt.Name)
            do matrix.Matrix.Add((technologyTypeTermAccessionNumberLabel, i), tt.TAN |> Option.defaultValue "")
            do matrix.Matrix.Add((technologyTypeTermSourceREFLabel, i), tt.TryGetTSR() |> Option.defaultValue "")
            do matrix.Matrix.Add((technologyPlatformLabel, i), Option.defaultValue "" technologyPlatform)
            do matrix.Matrix.Add((fileNameLabel, i), processedFileName)

            match assay.TryGetPropertyValue("Comments") with
            | Some (:? ResizeArray<DynamicObj> as comments) ->
                comments
                |> Seq.iter (fun comment ->
                    match Comment.toString comment with
                    | Some name, Some value ->
                        commentKeys <- name :: commentKeys
                        matrix.Matrix.Add((name, i), value)
                    | _ -> ()
                )
            | _ -> ()
        )
        { matrix with CommentKeys = commentKeys |> List.distinct |> List.rev }

    let toRows prefix (assays : Dataset list) =
        assays
        |> toSparseTable
        |> fun m ->
            match prefix with
            | Some p -> SparseTable.ToRows(m, p)
            | None -> SparseTable.ToRows(m)
 