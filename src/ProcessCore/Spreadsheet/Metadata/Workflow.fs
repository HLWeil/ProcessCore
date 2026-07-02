namespace ProcessCore.Spreadsheet

open ProcessCore
open System.Collections.Generic
open ProcessCore.Helper


module Workflow = 

    let identifierLabel = "Identifier"
    let titleLabel = "Title"
    let descriptionLabel = "Description"
    let workflowTypeLabel = "Type"
    let typeTermAccessionNumberLabel = "Type Term Accession Number"
    let typeTermSourceREFLabel = "Type Term Source REF"
    let subWorkflowIdentifiersLabel = "Sub Workflow Identifiers"
    let uriLabel = "URI"
    let versionLabel = "Version"
    let parametersNameLabel = "Parameters Name"
    let parametersTermAccessionNumberLabel = "Parameters Term Accession Number"
    let parametersTermSourceREFLabel = "Parameters Term Source REF"
    let componentsNameLabel = "Components Name"
    let componentsTypeLabel = "Components Type"
    let componentsTypeTermAccessionNumberLabel = "Components Type Term Accession Number"
    let componentsTypeTermSourceREFLabel = "Components Type Term Source REF"
    let fileNameLabel = "File Name"

    let [<Literal>] workflowLabel = "WORKFLOW"
    let [<Literal>] contactsLabel = "WORKFLOW CONTACTS"

    let [<Literal>] workflowLabelPrefix = "Workflow"
    let [<Literal>] contactsLabelPrefix = "Workflow Person"

    let labels = [
        identifierLabel;
        titleLabel;
        descriptionLabel;
        workflowTypeLabel;
        typeTermAccessionNumberLabel;
        typeTermSourceREFLabel;
        subWorkflowIdentifiersLabel;
        uriLabel;
        versionLabel;
        parametersNameLabel;
        parametersTermAccessionNumberLabel;
        parametersTermSourceREFLabel;
        componentsNameLabel;
        componentsTypeLabel;
        componentsTypeTermAccessionNumberLabel;
        componentsTypeTermSourceREFLabel;
        fileNameLabel
    ]

    let fromString identifier title description workflowType workflowTypeTermAccessionNumber workflowTypeTermSourceREF (subworkflowIdentifiers : string option) uri version parametersName parametersTermAccessionNumber parametersTermSourceREF componentsName componentsType componentsTypeTermAccessionNumber componentsTypeTermSourceREF fileName comments : Dataset =
        let subworkflowIdentifiers = 
            match subworkflowIdentifiers with
            | Some subworkflowIdentifiers -> 
                subworkflowIdentifiers.Split(';') |> Seq.map (fun s -> s.Trim()) |> ResizeArray
            | None -> ResizeArray()
        let workflowType = workflowType |> Option.map (fun wt -> DefinedTerm(name = wt, ?tan = workflowTypeTermAccessionNumber))
        let parameters = DefinedTerm.fromAggregatedStrings ';' parametersName parametersTermSourceREF parametersTermAccessionNumber |> ResizeArray
        let components = Component.fromAggregatedStrings ';' componentsName componentsType componentsTypeTermSourceREF componentsTypeTermAccessionNumber |> ResizeArray
        let identifier =
            match identifier with
            | Some identifier -> identifier
            | None ->
                match fileName with
                | Some fileName ->
                    match Identifier.Workflow.tryIdentifierFromFileName fileName with
                    | Some identifier -> identifier
                    | _ -> Identifier.createMissingIdentifier()
                | None -> Identifier.createMissingIdentifier()
        let workflow = Dataset(identifier, ?title = title, ?description = description, additionalType = "Workflow")
        workflowType |> Option.iter (fun value -> workflow.SetProperty("WorkflowType", value))
        workflow.SetProperty("SubWorkflowIdentifiers", subworkflowIdentifiers)
        workflow.SetProperty("Parameters", parameters)
        workflow.SetProperty("Components", components)
        uri |> Option.iter (fun value -> workflow.SetProperty("URI", value))
        version |> Option.iter (fun value -> workflow.SetProperty("Version", value))
        workflow.SetProperty("Comments", comments)
        workflow

    let fromSparseTable (matrix : SparseTable) : Dataset =
        
        let i = 0

        let comments = 
            matrix.CommentKeys 
            |> List.map (fun k -> 
                Comment.fromString k (matrix.TryGetValueDefault("",(k,i))))

        fromString
            (matrix.TryGetValue(identifierLabel,i))  
            (matrix.TryGetValue(titleLabel,i))
            (matrix.TryGetValue(descriptionLabel,i))
            (matrix.TryGetValue(workflowTypeLabel,i))
            (matrix.TryGetValue(typeTermAccessionNumberLabel,i))
            (matrix.TryGetValue(typeTermSourceREFLabel,i))
            (matrix.TryGetValue(subWorkflowIdentifiersLabel,i))
            (matrix.TryGetValue(uriLabel,i))
            (matrix.TryGetValue(versionLabel,i))
            (matrix.TryGetValueDefault("",(parametersNameLabel,i)))
            (matrix.TryGetValueDefault("",(parametersTermAccessionNumberLabel,i)))
            (matrix.TryGetValueDefault("",(parametersTermSourceREFLabel,i)))
            (matrix.TryGetValueDefault("",(componentsNameLabel,i)))
            (matrix.TryGetValueDefault("",(componentsTypeLabel,i)))
            (matrix.TryGetValueDefault("",(componentsTypeTermAccessionNumberLabel,i)))
            (matrix.TryGetValueDefault("",(componentsTypeTermSourceREFLabel,i)))
            (matrix.TryGetValue(fileNameLabel,i))
            (ResizeArray comments)

    let fromRows lineNumber (rows : IEnumerator<SparseRow>) =
        SparseTable.FromRows(rows,labels,lineNumber, prefix = workflowLabelPrefix)
        |> fun (s,ln,rs,sm) -> (s,ln,rs, fromSparseTable sm)
