module ProcessCore.ScaffoldReader

open ProcessCore
open ProcessCore.Helper
open ProcessCore.Spreadsheet
open FsSpreadsheet
open FsSpreadsheet.Net


let parseTablesIntoDataset (ds : Dataset) (wb : FsWorkbook) =
    wb.GetWorksheets()
    |> Seq.iter (fun ws ->
        Table.tryFromFsWorksheet ds ws |> ignore
    )
    ds.CollapseProcesses()
    ds

let datasetFromTables (name : string) (wb : FsWorkbook) =

    let d = Dataset(name)

    parseTablesIntoDataset d wb

let datasetFromPath (name : string) (path : string) : Dataset =
    let wb = FsWorkbook.fromXlsxFile(path)
    datasetFromTables name wb

module Assay =

    let tryFromFsWorkbook (wb : FsWorkbook) =
        ArcAssay.tryGetMetadataSheet wb
        |> Option.map (fun mdSheet ->
            let a = ArcAssay.fromMetadataSheet mdSheet
            parseTablesIntoDataset a wb |> ignore
            a
        )


module Study =

    let tryFromFsWorkbook (wb : FsWorkbook) =
        ArcStudy.tryGetMetadataSheet wb
        |> Option.map (fun mdSheet ->
            let arcStudy, _ = ArcStudy.fromMetadataSheet mdSheet
            parseTablesIntoDataset arcStudy wb |> ignore
            arcStudy
        )

module Run =

    let tryFromFsWorkbook (wb : FsWorkbook) =
        ArcRun.tryGetMetadataSheet wb
        |> Option.map (fun mdSheet ->
            let arcRun = ArcRun.fromMetadataSheet mdSheet
            parseTablesIntoDataset arcRun wb |> ignore
            arcRun
        )


module Workflow =

    let tryFromFsWorkbook (wb : FsWorkbook) =
        ArcWorkflow.tryGetMetadataSheet wb
        |> Option.map (fun mdSheet ->
            ArcWorkflow.fromMetadataSheet mdSheet
        )

module Investigation =

    let tryFromFsWorkbook (createF : string -> 'D) (wb : FsWorkbook) =
        ArcInvestigation.tryGetMetadataSheet wb
        |> Option.map (fun mdSheet ->
            ArcInvestigation.fromMetadataSheet createF mdSheet
        )

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

    let readWorkbook (arcPath : string) (wbPath : string) =
        let path = Path.combine arcPath wbPath
        FsWorkbook.fromXlsxFile(path)

    let getDatamapPathByISAPath (p : string) = 
        p.Replace(Path.InvestigationFileName, Path.DatamapFileName)
         .Replace(Path.AssayFileName, Path.DatamapFileName)
         .Replace(Path.StudyFileName, Path.DatamapFileName)
         .Replace(Path.WorkflowFileName, Path.DatamapFileName)
         .Replace(Path.RunFileName, Path.DatamapFileName)

    let load (createF : string -> 'D) (path : string) =
        printfn $"Loading ARC from {path}"
        let filePaths = FileSystemHelper.getAllFilePathsAsync path |> Async.RunSynchronously
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