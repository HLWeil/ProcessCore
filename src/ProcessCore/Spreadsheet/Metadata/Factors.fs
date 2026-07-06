namespace ProcessCore.Spreadsheet

open ProcessCore
open ProcessCore.Helper
open Comment
open Remark
open System.Collections.Generic
open DynamicObj

module Factors = 
    
    let nameLabel = "Name"
    let factorTypeLabel = "Type"
    let typeTermAccessionNumberLabel = "Type Term Accession Number"
    let typeTermSourceREFLabel = "Type Term Source REF"

    let labels = [nameLabel;factorTypeLabel;typeTermAccessionNumberLabel;typeTermSourceREFLabel]
    
    let fromString (name : string option) designType typeTermSourceREF typeTermAccessionNumber (comments : ResizeArray<DynamicObj>) =
        let dt = Option.defaultValue "" designType
        let factorType = Annotation(name = dt,?nameTAN = typeTermAccessionNumber, additionalType = "Factor")
        if comments.Count > 0 then factorType.SetProperty("Comments",comments)
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

    let toSparseTable (factors: Annotation seq) =
        let matrix = SparseTable.Create (keys = labels,length=Seq.length factors + 1)
        let mutable commentKeys = []
        factors
        |> Seq.iteri (fun i f ->
            let i = i + 1
            let name = f.TryGetPropertyValue("FactorName") |> Option.map string
            let tan = f.NameTAN |> Option.defaultValue ""
            let tsr = f.NameTAN |> Option.bind Ontology.tryGetTSR |> Option.defaultValue ""
            do matrix.Matrix.Add ((nameLabel,i),                    (Option.defaultValue "" name))
            do matrix.Matrix.Add ((factorTypeLabel,i),              f.Name)
            do matrix.Matrix.Add ((typeTermAccessionNumberLabel,i), tan)
            do matrix.Matrix.Add ((typeTermSourceREFLabel,i),       tsr)

            Comment.getCommentsFromDynamicObj f
            |> Seq.iter (fun (n,v) -> 
                if n <> "FactorName" then                  
                    commentKeys <- n :: commentKeys
                    matrix.Matrix.Add((n,i),v)
            )
        )
        {matrix with CommentKeys = commentKeys |> List.distinct |> List.rev} 

    let fromRows (prefix : string option) lineNumber (rows : IEnumerator<SparseRow>) =
        match prefix with
        | Some p -> SparseTable.FromRows(rows,labels,lineNumber,p)
        | None -> SparseTable.FromRows(rows,labels,lineNumber)
        |> fun (s,ln,rs,sm) -> (s,ln,rs, fromSparseTable sm)

    let toRows prefix (factors : Annotation seq) =
        factors
        |> toSparseTable
        |> fun m -> 
            match prefix with 
            | Some prefix -> SparseTable.ToRows(m,prefix)
            | None -> SparseTable.ToRows(m)
