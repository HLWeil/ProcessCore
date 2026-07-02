namespace ProcessCore.Spreadsheet

open ProcessCore
open ProcessCore.Helper
open Comment
open Remark
open System.Collections.Generic

module Factors = 
    
    let nameLabel = "Name"
    let factorTypeLabel = "Type"
    let typeTermAccessionNumberLabel = "Type Term Accession Number"
    let typeTermSourceREFLabel = "Type Term Source REF"

    let labels = [nameLabel;factorTypeLabel;typeTermAccessionNumberLabel;typeTermSourceREFLabel]
    
    let fromString (name : string option) designType typeTermSourceREF typeTermAccessionNumber comments =
        let dt = Option.defaultValue "" designType
        let factorType = Annotation(name = dt,?nameTAN = typeTermAccessionNumber, additionalType = "Factor")
        factorType.SetProperty("Comments",comments)
        if name.IsSome then factorType.SetProperty("FactorName",name.Value)
        factorType

    let fromSparseTable (matrix : SparseTable) =
        if matrix.ColumnCount = 0 && matrix.CommentKeys.Length <> 0 then
            let comments = SparseTable.GetEmptyComments matrix
            fromString None None None None comments
            |> List.singleton
        else
            List.init matrix.ColumnCount (fun i -> 

                let comments = 
                    matrix.CommentKeys 
                    |> List.map (fun k -> 
                        Comment.fromString k (matrix.TryGetValueDefault("",(k,i))))
                    |> ResizeArray

                fromString
                    (matrix.TryGetValue(nameLabel,i))
                    (matrix.TryGetValue(factorTypeLabel,i))
                    (matrix.TryGetValue((typeTermSourceREFLabel,i)))
                    (matrix.TryGetValue((typeTermAccessionNumberLabel,i)))
                    comments
            )

    let fromRows (prefix : string option) lineNumber (rows : IEnumerator<SparseRow>) =
        match prefix with
        | Some p -> SparseTable.FromRows(rows,labels,lineNumber,p)
        | None -> SparseTable.FromRows(rows,labels,lineNumber)
        |> fun (s,ln,rs,sm) -> (s,ln,rs, fromSparseTable sm)
