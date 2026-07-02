// #r "nuget: Fable.Core, 4.3.0"
// #r "nuget: DynamicObj"
// #r @"..\src\ProcessCore\bin\Release\netstandard2.0\ProcessCore.dll"

#r "nuget: ProcessCore, 0.0.3"
//#r "nuget: ARCtrl"


open ProcessCore
open ProcessCore.Table
open FsSpreadsheet
open FsSpreadsheet.Net
open ProcessCore.Helper
open ProcessCore.ScaffoldReader
open ProcessCore.Spreadsheet
open ARC
//open ARCtrl.Helper



//module ARC =

let load (path : string) =
    printfn $"Loading ARC from {path}"
    let filePaths = FileSystemHelper.getAllFilePathsAsync path |> Async.RunSynchronously
    let topLevelDataset =
        filePaths
        |> Seq.pick (fun p ->
            match Path.split p with
            | InvestigationPath _ ->
                try 
                    let wb = readWorkbook path p
                    Investigation.tryFromFsWorkbook wb
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


let arcPath = @"C:\Users\HLWei\source\repos\ARCs\Ru_ChlamyHeatstress"

load arcPath


let dataset = ARC.load arcPath

ProcessCore.ScaffoldReader.ARC.load arcPath

FsWorkbook.fromXlsxFile(@"C:\Users\HLWei\source\repos\ARCs\Ru_ChlamyHeatstress\isa.investigation.xlsx")

dataset.RegisterFragmentSelectorProvider (CsvFragmentSelectorProvider())

dataset.FinalData().[0].UpstreamSamples()
