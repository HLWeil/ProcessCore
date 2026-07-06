namespace ProcessCore.Spreadsheet

open ProcessCore
open ProcessCore.Helper
open Comment
open Remark
open DynamicObj
open System.Collections.Generic

module Publications = 

    let pubMedIDLabel =                     "PubMed ID"
    let doiLabel =                          "DOI"
    let authorListLabel =                   "Author List"
    let titleLabel =                        "Title"
    let statusLabel =                       "Status"
    let statusTermAccessionNumberLabel =    "Status Term Accession Number"
    let statusTermSourceREFLabel =          "Status Term Source REF"

    let labels = [pubMedIDLabel;doiLabel;authorListLabel;titleLabel;statusLabel;statusTermAccessionNumberLabel;statusTermSourceREFLabel]

    let fromString pubMedID doi author title status statusTermSourceREF statusTermAccessionNumber (comments : ResizeArray<DynamicObj>) =
        
        let status = status |> Option.map (fun s -> DefinedTerm(s,?tan = statusTermAccessionNumber))
        let sa = ScholarlyArticle( 
            headline = Option.defaultValue "" title,
            ?creativeWorkStatus = status
 
        )
        if comments.Count > 0 then sa.SetProperty("Comments",comments)
        sa

    let fromSparseTable (matrix : SparseTable) =
        if matrix.ColumnCount = 0 && matrix.CommentKeys.Length <> 0 then
            let comments = SparseTable.GetEmptyComments matrix
            let sa = ScholarlyArticle("")
            if comments.Count > 0 then sa.SetProperty("Comments",comments)
            sa
            |> List.singleton
        else
            List.init matrix.ColumnCount (fun i -> 

                let comments = 
                    matrix.CommentKeys 
                    |> List.map (fun k -> 
                        Comment.fromString k (matrix.TryGetValueDefault("",(k,i))))
                    |> ResizeArray

                fromString
                    (matrix.TryGetValue(pubMedIDLabel,i))            
                    (matrix.TryGetValue(doiLabel,i))             
                    (matrix.TryGetValue(authorListLabel,i))         
                    (matrix.TryGetValue(titleLabel,i))                 
                    (matrix.TryGetValue(statusLabel,i))                
                    (matrix.TryGetValue((statusTermSourceREFLabel,i)))    
                    (matrix.TryGetValue((statusTermAccessionNumberLabel,i)))
                    comments
            )

    let fromRows (prefix : string option) lineNumber (rows : IEnumerator<SparseRow>) =
        match prefix with
        | Some p -> SparseTable.FromRows(rows,labels,lineNumber,p)
        | None -> SparseTable.FromRows(rows,labels,lineNumber)
        |> fun (s,ln,rs,sm) -> (s,ln,rs, fromSparseTable sm)

    let toSparseTable (publications: ScholarlyArticle list) =
        let matrix = SparseTable.Create (keys = labels, length = publications.Length + 1)
        let mutable commentKeys = []
        publications
        |> List.iteri (fun i p ->
            let i = i + 1
            let authors =
                p.Authors
                |> Seq.map (fun a ->
                    match a.FamilyName, a.GivenName with
                    | Some familyName, givenName when givenName <> "" -> $"{givenName} {familyName}"
                    | Some familyName, _ -> familyName
                    | None, givenName -> givenName)
                |> String.concat ";"
            let status = p.CreativeWorkStatus |> Option.defaultValue (DefinedTerm(""))
            do matrix.Matrix.Add((pubMedIDLabel, i), "")
            do matrix.Matrix.Add((doiLabel, i), "")
            do matrix.Matrix.Add((authorListLabel, i), authors)
            do matrix.Matrix.Add((titleLabel, i), p.Headline)
            do matrix.Matrix.Add((statusLabel, i), status.Name)
            do matrix.Matrix.Add((statusTermAccessionNumberLabel, i), status.TAN |> Option.defaultValue "")
            do matrix.Matrix.Add((statusTermSourceREFLabel, i), status.TryGetTSR() |> Option.defaultValue "")

            match p.TryGetPropertyValue("Comments") with
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

    let toRows prefix (publications: ScholarlyArticle list) =
        publications
        |> toSparseTable
        |> fun m ->
            match prefix with
            | Some p -> SparseTable.ToRows(m, p)
            | None -> SparseTable.ToRows(m)