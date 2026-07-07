namespace ProcessCore.Spreadsheet

open ProcessCore
open ProcessCore.Helper

open DynamicObj
open System.Collections.Generic

module Contacts = 

    let lastNameLabel = "Last Name"
    let firstNameLabel = "First Name"
    let midInitialsLabel = "Mid Initials"
    let emailLabel = "Email"
    let phoneLabel = "Phone"
    let faxLabel = "Fax"
    let addressLabel = "Address"
    let affiliationLabel = "Affiliation"
    let rolesLabel = "Roles"
    let rolesTermAccessionNumberLabel = "Roles Term Accession Number"
    let rolesTermSourceREFLabel = "Roles Term Source REF"

    let labels = [lastNameLabel;firstNameLabel;midInitialsLabel;emailLabel;phoneLabel;faxLabel;addressLabel;affiliationLabel;rolesLabel;rolesTermAccessionNumberLabel;rolesTermSourceREFLabel]

    let fromString lastName firstName midInitials email phone (fax : string option) address affiliation role rolesTermAccessionNumber rolesTermSourceREF (comments : ResizeArray<DynamicObj>) =
        let roles = DefinedTerm.fromAggregatedStrings ';' role rolesTermSourceREF rolesTermAccessionNumber
        let affilitation = affiliation |> Option.map (fun n -> Organization(name = n))
        let orcid = 
            comments 
            |> Seq.tryPick (fun c -> 
                match c.TryGetPropertyValue("Name") with
                | Some (:? string as n) when n = "ORCID" -> c.TryGetPropertyValue("Value") |> Option.map string
                | _ -> None
            )
            |> fun o ->
                match o with
                | Some "" | None -> None
                | Some v -> Some v
        let comments = 
            comments
            |> Seq.filter (fun c -> 
                match c.TryGetPropertyValue("Name") with
                | Some (:? string as n) when n <> "ORCID" -> true
                | _ -> false
            )
            |> ResizeArray
        let a = Agent(
            givenName = Option.defaultValue "" firstName,
            ?id = orcid,
            ?familyName = lastName,
            ?email = email,
            ?affiliation = affilitation,
            jobTitles = roles,
            ?additionalName = midInitials,
            ?address = address,
            ?telephone = phone
        ) 
        if fax.IsSome then a.SetProperty("Fax",fax.Value)
        if comments.Count > 0 then a.SetProperty("Comments",comments)
        a

    let fromSparseTable (matrix : SparseTable) =
        if matrix.ColumnCount = 0 && matrix.CommentKeys.Length <> 0 then
            let comments = SparseTable.GetEmptyComments matrix
            let a = Agent("")
            if comments.Count > 0 then a.SetProperty("Comments",comments)
            a
            |> List.singleton
        else
            List.init matrix.ColumnCount (fun i -> 
                let comments = 
                    matrix.CommentKeys 
                    |> List.map (fun k -> 
                        Comment.fromString k (matrix.TryGetValueDefault("",(k,i))))
                    |> ResizeArray
                fromString
                    (matrix.TryGetValue(lastNameLabel,i))
                    (matrix.TryGetValue(firstNameLabel,i))
                    (matrix.TryGetValue(midInitialsLabel,i))
                    (matrix.TryGetValue(emailLabel,i))
                    (matrix.TryGetValue(phoneLabel,i))
                    (matrix.TryGetValue(faxLabel,i))
                    (matrix.TryGetValue(addressLabel,i))
                    (matrix.TryGetValue(affiliationLabel,i))
                    (matrix.TryGetValueDefault("",(rolesLabel,i)))
                    (matrix.TryGetValueDefault("",(rolesTermAccessionNumberLabel,i)))
                    (matrix.TryGetValueDefault("",(rolesTermSourceREFLabel,i)))
                    comments
            )

    let toSparseTable (persons:Agent seq) =
        let matrix = SparseTable.Create (keys = labels,length=Seq.length persons + 1)
        let mutable commentKeys = []
        persons
        //|> Seq.map Person.setCommentFromORCID
        |> Seq.iteri (fun i p ->
            let i = i + 1
            let fax = p.TryGetPropertyValue("Fax") |> Option.map string
            let rAgg = p.JobTitles |> DefinedTerm.toAggregatedStrings ';'
            do matrix.Matrix.Add ((lastNameLabel,i),                    (Option.defaultValue ""  p.FamilyName     ))
            do matrix.Matrix.Add ((firstNameLabel,i),                   (p.GivenName   ))
            do matrix.Matrix.Add ((midInitialsLabel,i),                 (Option.defaultValue ""  p.AdditionalName  ))
            do matrix.Matrix.Add ((emailLabel,i),                       (Option.defaultValue ""  p.Email        ))
            do matrix.Matrix.Add ((phoneLabel,i),                       (Option.defaultValue ""  p.Telephone        ))
            do matrix.Matrix.Add ((faxLabel,i),                         (Option.defaultValue ""  fax          ))
            do matrix.Matrix.Add ((addressLabel,i),                     (Option.defaultValue ""  p.Address      ))
            do matrix.Matrix.Add ((affiliationLabel,i),                 (Option.defaultValue ""  (p.Affiliation |> Option.map (fun o -> o.Name))  ))
            do matrix.Matrix.Add ((rolesLabel,i),                       rAgg.TermNameAgg)  
            do matrix.Matrix.Add ((rolesTermAccessionNumberLabel,i),    rAgg.TermAccessionNumberAgg)
            do matrix.Matrix.Add ((rolesTermSourceREFLabel,i),          rAgg.TermSourceREFAgg)
            
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
            if p.Id.IsSome then 
                do matrix.Matrix.Add(("ORCID",i),p.Id.Value)
                commentKeys <- "ORCID" :: commentKeys
        )
        {matrix with CommentKeys = commentKeys |> List.distinct |> List.rev} 


    let fromRows (prefix : string option) lineNumber (rows : IEnumerator<SparseRow>) =
        SparseTable.FromRows(rows,labels,lineNumber,?prefix = prefix)
        |> fun (s,ln,rs,sm) -> (s,ln,rs, fromSparseTable sm)


    let toRows (prefix : string option) (persons : Agent seq) =
        persons
        |> toSparseTable
        |> fun m -> 
            match prefix with 
            | Some prefix -> SparseTable.ToRows(m,prefix)
            | None -> SparseTable.ToRows(m)