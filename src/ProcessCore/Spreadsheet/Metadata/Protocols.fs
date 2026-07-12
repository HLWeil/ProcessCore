namespace ProcessCore.Spreadsheet

open ProcessCore
open ProcessCore.Helper
open Comment
open Remark
open DynamicObj
open System.Collections.Generic

module Protocols = 

    let nameLabel = "Name"
    let protocolTypeLabel = "Type"
    let typeTermAccessionNumberLabel = "Type Term Accession Number"
    let typeTermSourceREFLabel = "Type Term Source REF"
    let descriptionLabel = "Description"
    let uriLabel = "URI"
    let versionLabel = "Version"
    let parametersNameLabel = "Parameters Name"
    let parametersTermAccessionNumberLabel = "Parameters Term Accession Number"
    let parametersTermSourceREFLabel = "Parameters Term Source REF"
    let componentsNameLabel = "Components Name"
    let componentsTypeLabel = "Components Type"
    let componentsTypeTermAccessionNumberLabel = "Components Type Term Accession Number"
    let componentsTypeTermSourceREFLabel = "Components Type Term Source REF"

    let labels = 
        [
            nameLabel;protocolTypeLabel;typeTermAccessionNumberLabel;typeTermSourceREFLabel;descriptionLabel;uriLabel;versionLabel;
            parametersNameLabel;parametersTermAccessionNumberLabel;parametersTermSourceREFLabel;
            componentsNameLabel;componentsTypeLabel;componentsTypeTermAccessionNumberLabel;componentsTypeTermSourceREFLabel
        ]

    let fromString (name : string option) protocolType typeTermAccessionNumber typeTermSourceREF description uri version parametersName parametersTermAccessionNumber parametersTermSourceREF componentsName componentsType componentsTypeTermAccessionNumber componentsTypeTermSourceREF (comments : ResizeArray<DynamicObj>) =
        let protocolType = match protocolType with
                           | Some pt -> DefinedTerm(name = pt, ?tan = typeTermAccessionNumber) |> Some 
                           | None -> None
        let parameters = ProtocolParameter.fromAggregatedStrings ';' parametersName parametersTermSourceREF parametersTermAccessionNumber 
        let components = Component.fromAggregatedStrings ';' componentsName componentsType componentsTypeTermSourceREF componentsTypeTermAccessionNumber

        let r = Recipe(?name = name, ?description = description, ?version = version, ?url = uri, ?intendedUse = protocolType, parameters = parameters, components = components)
        if comments.Count > 0 then r.SetProperty("Comments",comments)
        r

    let fromSparseTable (matrix : SparseTable) =
        if matrix.ColumnCount = 0 && matrix.CommentKeys.Length <> 0 then
            let comments = SparseTable.GetEmptyComments matrix
            let r = Recipe()
            if comments.Count > 0 then r.SetProperty("Comments",comments)
            r
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
                    (matrix.TryGetValue(protocolTypeLabel,i))
                    (matrix.TryGetValue(typeTermAccessionNumberLabel,i))
                    (matrix.TryGetValue(typeTermSourceREFLabel,i))
                    (matrix.TryGetValue(descriptionLabel,i))
                    (matrix.TryGetValue(uriLabel,i))
                    (matrix.TryGetValue(versionLabel,i))
                    (matrix.TryGetValueDefault("",(parametersNameLabel,i)))
                    (matrix.TryGetValueDefault("",(parametersTermAccessionNumberLabel,i)))
                    (matrix.TryGetValueDefault("",(parametersTermSourceREFLabel,i)))
                    (matrix.TryGetValueDefault("",(componentsNameLabel,i)))
                    (matrix.TryGetValueDefault("",(componentsTypeLabel,i)))
                    (matrix.TryGetValueDefault("",(componentsTypeTermAccessionNumberLabel,i)))
                    (matrix.TryGetValueDefault("",(componentsTypeTermSourceREFLabel,i)))
                    comments
            )
 
    let fromRows (prefix : string option) lineNumber (rows : IEnumerator<SparseRow>) =
        match prefix with
        | Some p -> SparseTable.FromRows(rows,labels,lineNumber,p)
        | None -> SparseTable.FromRows(rows,labels,lineNumber)
        |> fun (s,ln,rs,sm) -> (s,ln,rs, fromSparseTable sm)

    let toSparseTable (protocols: Recipe list) =
        let matrix = SparseTable.Create (keys = labels, length = protocols.Length + 1)
        let mutable commentKeys = []
        protocols
        |> List.iteri (fun i p ->
            let i = i + 1
            let protocolType = p.IntendedUse |> Option.defaultValue (DefinedTerm(""))
            let parameters = ProtocolParameter.toAggregatedStrings ';' p.Parameters
            let components = Component.toAggregatedStrings ';' p.Components
            do matrix.Matrix.Add ((nameLabel, i), Option.defaultValue "" p.Name)
            do matrix.Matrix.Add ((protocolTypeLabel, i), protocolType.Name)
            do matrix.Matrix.Add ((typeTermAccessionNumberLabel, i), protocolType.TAN |> Option.defaultValue "")
            do matrix.Matrix.Add ((typeTermSourceREFLabel, i), protocolType.TryGetTSR() |> Option.defaultValue "")
            do matrix.Matrix.Add ((descriptionLabel, i), Option.defaultValue "" p.Description)
            do matrix.Matrix.Add ((uriLabel, i), Option.defaultValue "" p.Url)
            do matrix.Matrix.Add ((versionLabel, i), Option.defaultValue "" p.Version)
            do matrix.Matrix.Add ((parametersNameLabel, i), parameters.TermNameAgg)
            do matrix.Matrix.Add ((parametersTermAccessionNumberLabel, i), parameters.TermAccessionNumberAgg)
            do matrix.Matrix.Add ((parametersTermSourceREFLabel, i), parameters.TermSourceREFAgg)
            do matrix.Matrix.Add ((componentsNameLabel, i), components.NameAgg)
            do matrix.Matrix.Add ((componentsTypeLabel, i), components.TermNameAgg)
            do matrix.Matrix.Add ((componentsTypeTermAccessionNumberLabel, i), components.TermAccessionNumberAgg)
            do matrix.Matrix.Add ((componentsTypeTermSourceREFLabel, i), components.TermSourceREFAgg)

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

    let toRows prefix (protocols : Recipe list) =
        protocols
        |> toSparseTable
        |> fun m ->
            match prefix with
            | Some p -> SparseTable.ToRows(m, p)
            | None -> SparseTable.ToRows(m)
