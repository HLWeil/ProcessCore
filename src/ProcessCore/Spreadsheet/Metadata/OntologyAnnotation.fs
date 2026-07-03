namespace ProcessCore.Spreadsheet

open ProcessCore
open ProcessCore.Helper
open Comment
open Remark
open System.Collections.Generic


module OntologyAnnotationSection = 

    let fromSparseTable label labelTSR labelTAN (matrix : SparseTable) =
        if matrix.ColumnCount = 0 && matrix.CommentKeys.Length <> 0 then
            let comments = SparseTable.GetEmptyComments matrix
            let dt = DefinedTerm("")
            if comments.Count > 0 then dt.SetProperty("Comments",comments)
            dt
            |> List.singleton
        else
            List.init matrix.ColumnCount (fun i -> 

                let comments = 
                    matrix.CommentKeys 
                    |> List.map (fun k -> 
                        Comment.fromString k (matrix.TryGetValueDefault("",(k,i))))
                    |> ResizeArray

                let dt = DefinedTerm(
                    name = Option.defaultValue "" (matrix.TryGetValue(label,i)),
                    ?tan = matrix.TryGetValue(labelTAN,i)
                )
                if comments.Count > 0 then dt.SetProperty("Comments",comments)
                dt
            )


    let fromRows (prefix : string option) label labelTSR labelTAN lineNumber (rows : IEnumerator<SparseRow>) =
        let labels = [label;labelTAN;labelTSR]
        match prefix with
        | Some p -> SparseTable.FromRows(rows,labels,lineNumber,p)
        | None -> SparseTable.FromRows(rows,labels,lineNumber)
        |> fun (s,ln,rs,sm) -> (s,ln,rs, fromSparseTable label labelTSR labelTAN  sm)  
    