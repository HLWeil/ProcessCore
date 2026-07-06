namespace ProcessCore.Spreadsheet

open ProcessCore
open System.Collections.Generic
open ProcessCore.Helper
open DynamicObj


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

    let fromString identifier title description workflowType workflowTypeTermAccessionNumber workflowTypeTermSourceREF (subworkflowIdentifiers : string option) uri version parametersName parametersTermAccessionNumber parametersTermSourceREF componentsName componentsType componentsTypeTermAccessionNumber componentsTypeTermSourceREF fileName (comments : ResizeArray<DynamicObj>) : Dataset =
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
        if subworkflowIdentifiers.Count > 0 then workflow.SetProperty("SubWorkflowIdentifiers", subworkflowIdentifiers)
        if parameters.Count > 0 then workflow.SetProperty("Parameters", parameters)
        if components.Count > 0 then workflow.SetProperty("Components", components)
        uri |> Option.iter (fun value -> workflow.SetProperty("URI", value))
        version |> Option.iter (fun value -> workflow.SetProperty("Version", value))
        if comments.Count > 0 then workflow.SetProperty("Comments", comments)
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

    let toSparseTable (workflow: Dataset) =
        let matrix = SparseTable.Create (keys = labels, length = 2)
        let mutable commentKeys = []
        let processedIdentifier, processedFileName =
            if workflow.Identifier.StartsWith(Identifier.MISSING_IDENTIFIER) then "", ""
            else workflow.Identifier, Identifier.Workflow.fileNameFromIdentifier workflow.Identifier
        let workflowType =
            workflow.TryGetPropertyValue("WorkflowType")
            |> Option.bind (fun v -> match v with | :? DefinedTerm as dt -> Some dt | _ -> None)
            |> Option.defaultValue (DefinedTerm(""))
        let parameters =
            workflow.TryGetPropertyValue("Parameters")
            |> Option.bind (fun v -> match v with | :? ResizeArray<DefinedTerm> as values -> Some values | _ -> None)
            |> Option.defaultValue (ResizeArray())
        let components =
            workflow.TryGetPropertyValue("Components")
            |> Option.bind (fun v -> match v with | :? ResizeArray<Annotation> as values -> Some values | _ -> None)
            |> Option.defaultValue (ResizeArray())
        let subworkflowIdentifiers =
            workflow.TryGetPropertyValue("SubWorkflowIdentifiers")
            |> Option.bind (fun v -> match v with | :? ResizeArray<string> as values -> Some values | _ -> None)
            |> Option.map (String.concat ";")
            |> Option.defaultValue ""
        do matrix.Matrix.Add((identifierLabel, 1), processedIdentifier)
        do matrix.Matrix.Add((titleLabel, 1), Option.defaultValue "" workflow.Title)
        do matrix.Matrix.Add((descriptionLabel, 1), Option.defaultValue "" workflow.Description)
        do matrix.Matrix.Add((workflowTypeLabel, 1), workflowType.Name)
        do matrix.Matrix.Add((typeTermAccessionNumberLabel, 1), workflowType.TAN |> Option.defaultValue "")
        do matrix.Matrix.Add((typeTermSourceREFLabel, 1), workflowType.TryGetTSR() |> Option.defaultValue "")
        do matrix.Matrix.Add((subWorkflowIdentifiersLabel, 1), subworkflowIdentifiers)
        do matrix.Matrix.Add((uriLabel, 1),
            workflow.TryGetPropertyValue("URI")
            |> Option.bind (fun v -> match v with | :? string as s -> Some s | _ -> None)
            |> Option.defaultValue "")
        do matrix.Matrix.Add((versionLabel, 1),
            workflow.TryGetPropertyValue("Version")
            |> Option.bind (fun v -> match v with | :? string as s -> Some s | _ -> None)
            |> Option.defaultValue "")
        do matrix.Matrix.Add((parametersNameLabel, 1), parameters |> Seq.map (fun fp -> fp.Name) |> String.concat ";")
        do matrix.Matrix.Add((parametersTermAccessionNumberLabel, 1), parameters |> Seq.map (fun fp -> fp.TAN |> Option.defaultValue "") |> String.concat ";")
        do matrix.Matrix.Add((parametersTermSourceREFLabel, 1), parameters |> Seq.map (fun fp -> fp.TryGetTSR() |> Option.defaultValue "") |> String.concat ";")
        do matrix.Matrix.Add((componentsNameLabel, 1), components |> Seq.map (fun pv -> pv.TryGetPropertyValue("componentName") |> Option.bind (fun v -> match v with | :? string as s -> Some s | _ -> None) |> Option.defaultValue "") |> String.concat ";")
        do matrix.Matrix.Add((componentsTypeLabel, 1), components |> Seq.map (fun pv -> pv.Name) |> String.concat ";")
        do matrix.Matrix.Add((componentsTypeTermAccessionNumberLabel, 1), components |> Seq.map (fun pv -> pv.NameTAN |> Option.defaultValue "") |> String.concat ";")
        do matrix.Matrix.Add((componentsTypeTermSourceREFLabel, 1), components |> Seq.map (fun pv -> DefinedTerm(pv.Name, ?tan = pv.NameTAN).TryGetTSR() |> Option.defaultValue "") |> String.concat ";")
        do matrix.Matrix.Add((fileNameLabel, 1), processedFileName)

        match workflow.TryGetPropertyValue("Comments") with
        | Some (:? ResizeArray<DynamicObj> as comments) ->
            comments
            |> Seq.iter (fun comment ->
                match Comment.toString comment with
                | Some name, Some value ->
                    commentKeys <- name :: commentKeys
                    matrix.Matrix.Add((name, 1), value)
                | _ -> ()
            )
        | _ -> ()

        { matrix with CommentKeys = commentKeys |> List.distinct |> List.rev }

    let toRows (workflow: Dataset) =
        workflow
        |> toSparseTable
        |> fun st -> SparseTable.ToRows(st, prefix = workflowLabelPrefix)
