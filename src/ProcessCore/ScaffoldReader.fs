module ProcessCore.ScaffoldReader

open ProcessCore
open ProcessCore.Helper
open ProcessCore.Spreadsheet
open FsSpreadsheet
open ProcessCore.Table

let parseTablesIntoDataset (ds : Dataset) (wb : FsWorkbook) =
    wb.GetWorksheets()
    |> Seq.iter (fun ws ->
        Table.tryFromFsWorksheet ds ws |> ignore
    )
    ds.CollapseProcesses()
    ds.Tables.GetTables() |> Seq.iter (fun t -> t.ColumnCount |> ignore)
    ds

let datasetFromWorkbook (name : string) (wb : FsWorkbook) =

    let d = Dataset(name)

    parseTablesIntoDataset d wb

#if !FABLE_COMPILER_JAVASCRIPT && !FABLE_COMPILER_TYPESCRIPT
let datasetFromPath (name : string) (path : string) : Dataset =
    let wb = Path.readFileXlsx(path)
    datasetFromWorkbook name wb
#endif

module Assay =

    let tryFromFsWorkbook (wb : FsWorkbook) =
        ArcAssay.tryGetMetadataSheet wb
        |> Option.map (fun mdSheet ->
            let a = ArcAssay.fromMetadataSheet mdSheet
            parseTablesIntoDataset a wb |> ignore
            a
        )

    let toFsWorkbook (assay : Dataset) =
        let doc = new FsWorkbook()
        let metadataSheet = ArcAssay.toMetadataSheet (assay)
        doc.AddWorksheet metadataSheet

        assay.Tables.GetTables()
        |> Seq.iteri (fun i -> Table.toFsWorksheet (Some i) >> doc.AddWorksheet)
        doc

module Study =

    let tryFromFsWorkbook (wb : FsWorkbook) =
        ArcStudy.tryGetMetadataSheet wb
        |> Option.map (fun mdSheet ->
            let arcStudy, _ = ArcStudy.fromMetadataSheet mdSheet
            parseTablesIntoDataset arcStudy wb |> ignore
            arcStudy
        )

    let toFsWorkbook (study : Dataset)  =
        let doc = new FsWorkbook()
        let metadataSheet = ArcStudy.toMetadataSheet study None
        doc.AddWorksheet metadataSheet

        study.Tables.GetTables()
        |> Seq.iteri (fun i -> Table.toFsWorksheet (Some i) >> doc.AddWorksheet)

        doc

    let toFsWorkbookWithAssays (study : Dataset) (assays : Dataset list) =
        let doc = new FsWorkbook()
        let metadataSheet = ArcStudy.toMetadataSheet study (Some assays)
        doc.AddWorksheet metadataSheet

        study.Tables.GetTables()
        |> Seq.iteri (fun i -> Table.toFsWorksheet (Some i) >> doc.AddWorksheet)

        doc

module Run =

    let tryFromFsWorkbook (wb : FsWorkbook) =
        ArcRun.tryGetMetadataSheet wb
        |> Option.map (fun mdSheet ->
            let arcRun = ArcRun.fromMetadataSheet mdSheet
            parseTablesIntoDataset arcRun wb |> ignore
            arcRun
        )

    let toFsWorkbook (run : Dataset) =
            let doc = new FsWorkbook()
            let metadataSheet = ArcRun.toMetadataSheet (run)
            doc.AddWorksheet metadataSheet

            run.Tables.GetTables()
            |> Seq.iteri (fun i -> Table.toFsWorksheet (Some i) >> doc.AddWorksheet)

            doc


module Workflow =

    let tryFromFsWorkbook (wb : FsWorkbook) =
        ArcWorkflow.tryGetMetadataSheet wb
        |> Option.map (fun mdSheet ->
            ArcWorkflow.fromMetadataSheet mdSheet
        )

    let toFsWorkbook (workflow : Dataset) = 
        let doc = new FsWorkbook()
        let metadataSheet = ArcWorkflow.toMetadataSheet workflow
        doc.AddWorksheet metadataSheet

        doc


module Investigation =

    let tryFromFsWorkbook (createF : string -> 'D) (wb : FsWorkbook) =
        ArcInvestigation.tryGetMetadataSheet wb
        |> Option.map (fun mdSheet ->
            ArcInvestigation.fromMetadataSheet createF mdSheet
        )
    
    let toFsWorkbook (investigation : Dataset) : FsWorkbook =           
        try
            let wb = new FsWorkbook()
            let sheet = FsWorksheet(ArcInvestigation.metadataSheetName)
            investigation
            |> ArcInvestigation.toRows
            |> Seq.iteri (fun rowI r -> SparseRow.writeToSheet (rowI + 1) r sheet)                     
            wb.AddWorksheet(sheet)
            wb
        with
        | err -> failwithf "Could not write investigation to spreadsheet: %s" err.Message

module ARC =

    let (|AssayPath|_|) (input) =
        match input with
        | [|Path.AssaysFolderName; anyAssayName; Path.AssayFileName|] -> 
            let path = Path.combineMany input
            Some path
        | _ -> None

    let (|StudyPath|_|) (input) =
        match input with
        | [|Path.StudiesFolderName; anyStudyName; Path.StudyFileName|] -> 
            let path = Path.combineMany input
            Some path
        | _ -> None

    let (|WorkflowPath|_|) (input) =
        match input with
        | [|Path.WorkflowsFolderName; anyWorkflowName; Path.WorkflowFileName|] -> 
            let path = Path.combineMany input
            Some path
        | _ -> None

    let (|RunPath|_|) (input) =
        match input with
        | [|Path.RunsFolderName; anyRunName; Path.RunFileName|] -> 
            let path = Path.combineMany input
            Some path
        | _ -> None

    let (|InvestigationPath|_|) (input) =
        match input with
        | [|Path.InvestigationFileName|] -> 
            let path = Path.combineMany input
            Some path
        | _ -> None

    let getAssayPath (identifier : string) = 
        Path.combineMany [Path.AssaysFolderName; identifier; Path.AssayFileName]

    let getStudyPath (identifier : string) = 
        Path.combineMany [Path.StudiesFolderName; identifier; Path.StudyFileName]

    let getRunPath (identifier : string) = 
        Path.combineMany [Path.RunsFolderName; identifier; Path.RunFileName]

    let getWorkflowPath (identifier : string) = 
        Path.combineMany [Path.WorkflowsFolderName; identifier; Path.WorkflowFileName]

    let getDatamapPathByISAPath (p : string) = 
        p.Replace(Path.InvestigationFileName, Path.DatamapFileName)
         .Replace(Path.AssayFileName, Path.DatamapFileName)
         .Replace(Path.StudyFileName, Path.DatamapFileName)
         .Replace(Path.WorkflowFileName, Path.DatamapFileName)
         .Replace(Path.RunFileName, Path.DatamapFileName)

    #if !FABLE_COMPILER_JAVASCRIPT && !FABLE_COMPILER_TYPESCRIPT

    let readWorkbook (arcPath : string) (wbPath : string) =
        let path = Path.combine arcPath wbPath
        Path.readFileXlsx(path)

    let writeWorkbook (arcPath : string) (wbPath : string) (wb : FsWorkbook) =
        let path = Path.combine arcPath wbPath
        Path.writeFileXlsx(path) wb

    let load (createF : string -> 'D) (path : string) =
        printfn $"Loading ARC from {path}"
        let filePaths = Path.getAllFilePathsAsync path |> Async.RunSynchronously
        let topLevelDataset =
            filePaths
            |> Seq.pick (fun p ->
                match Path.split p with
                | InvestigationPath _ ->
                    try 
                        let wb = readWorkbook path p
                        Investigation.tryFromFsWorkbook createF wb
                    with
                    | ex -> 
                        printfn $"Failed to load investigation from {p}: {ex.Message}"
                        None
                | _ -> None
            )
        let enrichDatasetWithDatamap (p : string) (ds : Dataset)  =
        
            try
                let datamapPath = getDatamapPathByISAPath p
                printfn $"Reading datamap from path {datamapPath}"

                filePaths
                |> Array.tryPick (fun p ->
                    if p = datamapPath then
                        let wb = readWorkbook path p
                        Datamap.dataContextsFromFsWorkbook wb |> Some
                    else None
                )
                |> fun dcs -> 
                    match dcs with
                    | Some dcs -> for dc in dcs do ds.AddDataContext(dc)
                    | None -> ()
            with 
            | ex -> printfn $"Failed to load datamap from {p}: {ex.Message}"
        filePaths
        |> Seq.choose (fun p ->
            match Path.split p with
            | AssayPath _ ->
                printfn $"Reading assay from path {p}"
                try readWorkbook path p |> Assay.tryFromFsWorkbook
                with
                | ex -> 
                    printfn $"Failed to load assay from {p}: {ex.Message}"
                    None
            | StudyPath _ ->
                printfn $"Reading study from path {p}"
                try readWorkbook path p |> Study.tryFromFsWorkbook
                with
                | ex -> 
                    printfn $"Failed to load study from {p}: {ex.Message}"
                    None
            | WorkflowPath _ ->
                printfn $"Reading workflow from path {p}"
                try readWorkbook path p |> Workflow.tryFromFsWorkbook
                with
                | ex -> 
                    printfn $"Failed to load workflow from {p}: {ex.Message}"
                    None
            | RunPath _ ->
                printfn $"Reading run from path {p}"
                try readWorkbook path p |> Run.tryFromFsWorkbook
                with
                | ex -> 
                    printfn $"Failed to load run from {p}: {ex.Message}"
                    None
            | _ -> None
            |> Option.map (fun ds -> 
                enrichDatasetWithDatamap p ds 
                ds
            )
        )
        |> Seq.iter (fun ds -> 
            printfn $"Adding dataset {ds.Identifier} to top-level dataset"
            try topLevelDataset.AddPart(ds) |> ignore
            with
            | ex -> printfn $"Failed to add dataset {ds.Identifier} to top-level dataset: {ex.Message}"
        )
        topLevelDataset

    let write (arcPath : string) (arc : #Dataset) =
        arc.HasPart
        |> Seq.iter (fun d ->
            match d.AdditionalType with
            | Some "Assay" -> 
                printfn $"Writing assay {d.Identifier}"
                let p = getAssayPath d.Identifier
                Path.ensureDirectoryOfFileAsync (Path.combine arcPath p) |> Async.RunSynchronously
                let wb = Assay.toFsWorkbook d
                writeWorkbook arcPath p wb
                if d.DataContexts.Count > 0 then
                    let p = getDatamapPathByISAPath p
                    let wb = Datamap.toFsWorkbook d
                    writeWorkbook arcPath p wb   
            | Some "Study" ->
                printfn $"Writing study {d.Identifier}"
                let p = getStudyPath d.Identifier
                Path.ensureDirectoryOfFileAsync (Path.combine arcPath p) |> Async.RunSynchronously
                let wb = Study.toFsWorkbook d
                writeWorkbook arcPath p wb
                if d.DataContexts.Count > 0 then
                    let p = getDatamapPathByISAPath p
                    let wb = Datamap.toFsWorkbook d
                    writeWorkbook arcPath p wb
            | Some "Run" ->
                printfn $"Writing run {d.Identifier}"
                let p = getRunPath d.Identifier
                Path.ensureDirectoryOfFileAsync (Path.combine arcPath p) |> Async.RunSynchronously
                let wb = Run.toFsWorkbook d
                writeWorkbook arcPath p wb
                if d.DataContexts.Count > 0 then
                    let p = getDatamapPathByISAPath p
                    let wb = Datamap.toFsWorkbook d
                    writeWorkbook arcPath p wb
            | Some "Workflow" ->
                printfn $"Writing workflow {d.Identifier}"
                let p = getWorkflowPath d.Identifier
                Path.ensureDirectoryOfFileAsync (Path.combine arcPath p) |> Async.RunSynchronously
                let wb = Workflow.toFsWorkbook d
                writeWorkbook arcPath p wb
                if d.DataContexts.Count > 0 then
                    let p = getDatamapPathByISAPath p
                    let wb = Datamap.toFsWorkbook d
                    writeWorkbook arcPath p wb
            | _ -> ()                             
        )
        Investigation.toFsWorkbook arc
        |> writeWorkbook arcPath Path.InvestigationFileName

    #endif