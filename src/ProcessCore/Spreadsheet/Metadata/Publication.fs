namespace ProcessCore.Spreadsheet

open ProcessCore
open ProcessCore.Helper
open Comment
open Remark
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

    let fromString pubMedID doi author title status statusTermSourceREF statusTermAccessionNumber comments =
        
        let status = status |> Option.map (fun s -> DefinedTerm(s,?tan = statusTermAccessionNumber))
        let sa = ScholarlyArticle( 
            headline = Option.defaultValue "" title,
            ?creativeWorkStatus = status
 
        )
        sa.SetProperty("Comments",comments)
        sa

    let fromSparseTable (matrix : SparseTable) =
        if matrix.ColumnCount = 0 && matrix.CommentKeys.Length <> 0 then
            let comments = SparseTable.GetEmptyComments matrix
            let sa = ScholarlyArticle("")
            sa.SetProperty("Comments",comments)
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