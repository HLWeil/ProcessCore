module ProcessCore.ScaffoldReader

open ProcessCore
open ProcessCore.Helper
open ProcessCore.Spreadsheet
open FsSpreadsheet
open FsSpreadsheet.Net


let parseTablesIntoDataset (ds : Dataset) (wb : FsWorkbook) =
    wb.GetWorksheets()
    |> Seq.iter (fun ws ->
        match Table.tryFromFsWorksheet ds ws with
        | Some t -> ds.HasPart.Add(ds) |> ignore
        | None -> () // No annotation table, so we skip this sheet
    )
    ds.CollapseProcesses()
    ds

let datasetFromTables (name : string) (wb : FsWorkbook) =

    let d = Dataset(name)

    wb.GetWorksheets()
    |> Seq.iter (fun ws ->
        match Table.tryFromFsWorksheet d ws with
        | Some t -> d.HasPart.Add(d) |> ignore
        | None -> () // No annotation table, so we skip this sheet
    )
    d.CollapseProcesses()
    let newD = Dataset(name)
    let processes = d.Processes |> Seq.toList
    for p in processes do
        d.RemoveProcess(p)
        newD.AddProcess(p) |> ignore
    newD

let datasetFromPath (name : string) (path : string) =
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

    let tryFromFsWorkbook (wb : FsWorkbook) =
        ArcInvestigation.tryGetMetadataSheet wb
        |> Option.map (fun mdSheet ->
            ArcInvestigation.fromMetadataSheet mdSheet
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

    let load (path : string) =
        let filePaths = FileSystemHelper.getAllFilePathsAsync path |> Async.RunSynchronously
        let topLevelDataset =
            filePaths
            |> Seq.pick (fun p ->
                match Path.split p with
                | InvestigationPath _ ->
                    let wb = readWorkbook path p
                    Investigation.tryFromFsWorkbook wb
                | _ -> None
            )
        let enrichDatasetWithDatamap (p : string) (ds : Dataset)  =
            let datamapPath = getDatamapPathByISAPath p
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
        filePaths
        |> Seq.choose (fun p ->
            match Path.split p with
            | AssayPath _ ->
                readWorkbook path p |> Assay.tryFromFsWorkbook
            | StudyPath _ ->
                readWorkbook path p |> Study.tryFromFsWorkbook
            | WorkflowPath _ ->
                readWorkbook path p |> Workflow.tryFromFsWorkbook
            | RunPath _ ->
                readWorkbook path p |> Run.tryFromFsWorkbook
            | _ -> None
            |> Option.map (fun ds -> 
                enrichDatasetWithDatamap p ds 
                ds
            )
        )
        |> Seq.iter (fun ds -> topLevelDataset.AddPart(ds) |> ignore)
        topLevelDataset


//let arcPath = @"C:\Users\HLWei\source\repos\ARCs\Ru_ChlamyHeatstress"

//let dataset = ARC.load arcPath

//dataset.RegisterFragmentSelectorProvider (CsvFragmentSelectorProvider())

//dataset.FinalData().[0].UpstreamSamples()
