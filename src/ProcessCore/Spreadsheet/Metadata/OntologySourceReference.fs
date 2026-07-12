namespace ProcessCore.Spreadsheet

open FsSpreadsheet
open ProcessCore
open ProcessCore.Helper
open System.Collections.Generic
open DynamicObj

module OntologySourceReference = 

    let nameLabel = "Term Source Name"
    let fileLabel = "Term Source File"
    let versionLabel = "Term Source Version"
    let descriptionLabel = "Term Source Description"

    
    let labels = [nameLabel;fileLabel;versionLabel;descriptionLabel]

    let fromString (description : string option) (file : string option) (name : string option) (version : string option) (comments : DynamicObj ResizeArray) =
        let d = DynamicObj()
        if comments.Count > 0 then d.SetProperty("Comments",comments)
        if description.IsSome then d.SetProperty(descriptionLabel,description.Value)
        if file.IsSome then d.SetProperty(fileLabel,file.Value)
        if name.IsSome then d.SetProperty(nameLabel,name.Value)
        if version.IsSome then d.SetProperty(versionLabel,version.Value)
        d


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
                    (matrix.TryGetValue(descriptionLabel,i))
                    (matrix.TryGetValue(fileLabel,i))
                    (matrix.TryGetValue(nameLabel,i))
                    (matrix.TryGetValue(versionLabel,i))
                    comments
            )

    let fromRows lineNumber (rows : IEnumerator<SparseRow>) =
        SparseTable.FromRows(rows,labels,lineNumber)
        |> fun (s,ln,rs,sm) -> (s,ln,rs, fromSparseTable sm)

    let toSparseTable (ontologySources: DynamicObj list) =
        let matrix = SparseTable.Create (keys = labels, length = ontologySources.Length + 1)
        let mutable commentKeys = []
        ontologySources
        |> List.iteri (fun i (o: DynamicObj) ->
            let i = i + 1
            let getString name =
                o.TryGetPropertyValue(name)
                |> Option.bind (fun v -> match v with | :? string as s -> Some s | _ -> None)
                |> Option.defaultValue ""
            do matrix.Matrix.Add ((nameLabel, i), getString nameLabel)
            do matrix.Matrix.Add ((fileLabel, i), getString fileLabel)
            do matrix.Matrix.Add ((versionLabel, i), getString versionLabel)
            do matrix.Matrix.Add ((descriptionLabel, i), getString descriptionLabel)

            match o.TryGetPropertyValue("Comments") with
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

    let toRows (ontologySources: DynamicObj list) =
        ontologySources
        |> toSparseTable
        |> fun m -> SparseTable.ToRows(m)
