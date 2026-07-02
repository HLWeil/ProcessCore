namespace ProcessCore.Spreadsheet

open ProcessCore
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

    
    let fromString identifier title description measurementType measurementTypeTermSourceREF measurementTypeTermAccessionNumber technologyType technologyTypeTermSourceREF technologyTypeTermAccessionNumber (technologyPlatform : string option) fileName comments : Dataset = 
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
        assay.SetProperty("Comments",comments)
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
 