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
        r.SetProperty("Comments",comments)
        r

    let fromSparseTable (matrix : SparseTable) =
        if matrix.ColumnCount = 0 && matrix.CommentKeys.Length <> 0 then
            let comments = SparseTable.GetEmptyComments matrix
            let r = Recipe()
            r.SetProperty("Comments",comments)
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
