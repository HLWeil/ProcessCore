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

    let fromRows (prefix : string option) lineNumber (rows : IEnumerator<SparseRow>) =
        SparseTable.FromRows(rows,labels,lineNumber,?prefix = prefix)
        |> fun (s,ln,rs,sm) -> (s,ln,rs, fromSparseTable sm)