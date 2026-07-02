module ProcessCore.ScaffoldReader

open ProcessCore
open ProcessCore.Helper
open ProcessCore.Table
open FsSpreadsheet
open FsSpreadsheet.Net


module CompositeCell =

    let termFromStringCells (tsrCol : int option) (tanCol : int option ) (cellValues : array<string>) : CompositeCell=
        let tan = Option.map (fun i -> cellValues.[i]) tanCol
        let tsr = Option.map (fun i -> cellValues.[i]) tsrCol
        CompositeCell.Term(name = cellValues.[0], ?tan = tan)

    let unitizedFromStringCells (unitCol : int) (tsrCol : int option ) (tanCol : int option) (cellValues : array<string>) : CompositeCell =
        let unit = cellValues.[unitCol]
        let tan = Option.map (fun i -> cellValues.[i]) tanCol
        let tsr = Option.map (fun i -> cellValues.[i]) tsrCol
        CompositeCell.Unitized(cellValues.[0],unit, ?unitTAN = tan)

    let freeTextFromStringCells (cellValues : array<string>) : CompositeCell =
        CompositeCell.FreeText cellValues.[0]

    let dataFromStringCells (format : int option) (selectorFormat : int option) (cellValues : array<string>) : CompositeCell =
        let format = Option.bind (fun i -> cellValues.[i] |> Option.fromValueWithDefault "") format
        let selectorFormat = Option.bind (fun i -> cellValues.[i] |> Option.fromValueWithDefault "") selectorFormat
        let path, selector =
            if cellValues.[0].Contains("#") then
                let parts = cellValues.[0].Split('#', 2)
                parts.[0], Some parts.[1]
            else
                cellValues.[0], None
        let data = Data(path, ?selector = selector, ?encodingFormat = format, ?selectorFormat = selectorFormat)
        CompositeCell.Data(data)

module ActivePattern =

    open Regex.ActivePatterns

    let mergeIDInfo idSpace1 localID1 idSpace2 localID2 =
        if idSpace1 <> idSpace2 then failwithf "TermSourceRef %s and %s do not match" idSpace1 idSpace2
        if localID1 <> localID2 then failwithf "LocalID %s and %s do not match" localID1 localID2
        {|TermSourceRef = idSpace1; TermAccessionNumber = $"{idSpace1}:{localID1}"|}

    let (|Term|_|) (categoryParser : string -> string option) (f : DefinedTerm -> CompositeHeader) (cellValues : string []) : (CompositeHeader*(string [] -> CompositeCell)) option =
        let (|AC|_|) s =
            categoryParser s
        match cellValues with
        | [|AC name|] ->
            let ont = DefinedTerm(name)
            (f ont, CompositeCell.termFromStringCells None None)
            |> Some
        | [|AC name; TSRColumnHeader term1; TANColumnHeader term2|] ->
            let term = mergeIDInfo term1.IDSpace term1.LocalID term2.IDSpace term2.LocalID
            let ont = DefinedTerm(name, term.TermSourceRef, term.TermAccessionNumber)
            (f ont, CompositeCell.termFromStringCells (Some 1) (Some 2))
            |> Some
        | [|AC name; TANColumnHeader term2; TSRColumnHeader term1|] ->
            let term = mergeIDInfo term1.IDSpace term1.LocalID term2.IDSpace term2.LocalID
            let ont = DefinedTerm(name, term.TermSourceRef, term.TermAccessionNumber)
            (f ont, CompositeCell.termFromStringCells (Some 2) (Some 1))
            |> Some
        | [|AC name; UnitColumnHeader _; TSRColumnHeader term1; TANColumnHeader term2|] ->
            let term = mergeIDInfo term1.IDSpace term1.LocalID term2.IDSpace term2.LocalID
            let ont = DefinedTerm(name, term.TermSourceRef, term.TermAccessionNumber)
            (f ont, CompositeCell.unitizedFromStringCells 1 (Some 2) (Some 3))
            |> Some
        | [|AC name; UnitColumnHeader _; TANColumnHeader term2; TSRColumnHeader term1|] ->
            let term = mergeIDInfo term1.IDSpace term1.LocalID term2.IDSpace term2.LocalID
            let ont = DefinedTerm(name, term.TermSourceRef, term.TermAccessionNumber)
            (f ont, CompositeCell.unitizedFromStringCells 1 (Some 3) (Some 2))
            |> Some
        | _ -> None

    let (|Parameter|_|) (cellValues : string []) =
        match cellValues with
        | Term Regex.tryParseParameterColumnHeader CompositeHeader.Parameter r ->
            Some r
        | _ -> None

    let (|Factor|_|) (cellValues : string []) =
        match cellValues with
        | Term Regex.tryParseFactorColumnHeader CompositeHeader.Factor r ->
            Some r
        | _ -> None

    let (|Characteristic|_|) (cellValues : string []) =
        match cellValues with
        | Term Regex.tryParseCharacteristicColumnHeader CompositeHeader.Characteristic r ->
            Some r
        | _ -> None

    let (|Component|_|) (cellValues : string []) =
        match cellValues with
        | Term Regex.tryParseComponentColumnHeader CompositeHeader.Component r ->
            Some r
        | _ -> None

    let (|Input|_|) (cellValues : string []) =
        if cellValues.Length = 0 then None
        else
            match cellValues.[0] with
            | InputColumnHeader ioType ->
                let cols = cellValues |> Array.skip 1
                match IOType.ofString ioType with
                | IOType.Data ->
                    let format = cols |> Array.tryFindIndex (fun s -> s.StartsWith("Data Format"))  |> Option.map ((+) 1)
                    let selectorFormat = cols |> Array.tryFindIndex (fun s -> s.StartsWith("Data Selector Format"))  |> Option.map ((+) 1)
                    (CompositeHeader.Input (IOType.Data), CompositeCell.dataFromStringCells format selectorFormat)
                    |> Some
                | ioType ->
                    (CompositeHeader.Input ioType, CompositeCell.freeTextFromStringCells)
                    |> Some
            | _ -> None

    let (|Output|_|) (cellValues : string []) =
        if cellValues.Length = 0 then None
        else
            match cellValues.[0] with
            | OutputColumnHeader ioType ->
                let cols = cellValues |> Array.skip 1
                match IOType.ofString ioType with
                | IOType.Data ->
                    let format = cols |> Array.tryFindIndex (fun s -> s.StartsWith("Data Format"))  |> Option.map ((+) 1)
                    let selectorFormat = cols |> Array.tryFindIndex (fun s -> s.StartsWith("Data Selector Format"))  |> Option.map ((+) 1)
                    (CompositeHeader.Output (IOType.Data), CompositeCell.dataFromStringCells format selectorFormat)
                    |> Some
                | ioType ->
                    (CompositeHeader.Output ioType, CompositeCell.freeTextFromStringCells)
                    |> Some
            | _ -> None

    let (|Comment|_|) (cellValues : string []) =
        match cellValues with
        | [|Comment key|] -> Some (CompositeHeader.Comment key, CompositeCell.freeTextFromStringCells)
        | _ -> None

    let (|ProtocolType|_|) (cellValues : string []) =
        let parser s = if s = "Protocol Type" then Some s else None
        let header _ = CompositeHeader.ProtocolType
        match cellValues with
        | Term parser header r -> Some r
        | _ -> None

    let (|ProtocolHeader|_|) (cellValues : string []) =
        match cellValues with
        | [|"Protocol REF"|] -> Some (CompositeHeader.ProtocolREF, CompositeCell.freeTextFromStringCells)
        | [|"Protocol Description"|] -> Some (CompositeHeader.ProtocolDescription, CompositeCell.freeTextFromStringCells)
        | [|"Protocol Uri"|] -> Some (CompositeHeader.ProtocolUri, CompositeCell.freeTextFromStringCells)
        | [|"Protocol Version"|] -> Some (CompositeHeader.ProtocolVersion, CompositeCell.freeTextFromStringCells)
        | [|"Performer"|] -> Some (CompositeHeader.Performer, CompositeCell.freeTextFromStringCells)
        | [|"Date"|] -> Some (CompositeHeader.Date, CompositeCell.freeTextFromStringCells)
        | _ -> None

    let (|FreeText|_|) (cellValues : string []) =
        match cellValues with
        | [|text|] ->
            (CompositeHeader.FreeText text, CompositeCell.freeTextFromStringCells)
            |> Some
        | _ -> None

module CompositeHeader =

    open ActivePattern

    let fromStringCells (cellValues : string []) : CompositeHeader*(string [] -> CompositeCell) =
        match cellValues with
        | Parameter p -> p
        | Factor f -> f
        | Characteristic c -> c
        | Component c -> c
        | Input i -> i
        | Output o -> o
        | ProtocolType pt -> pt
        | ProtocolHeader ph -> ph
        | Comment c -> c
        | FreeText ft -> ft
        | _ -> failwithf "Could not parse header group %O" cellValues


    let toStringCells (hasUnit : bool) (header : CompositeHeader) : string [] =
        if header.IsDataColumn then
            [|header.ToString(); "Data Format";  "Data Selector Format"|]
        elif header.IsSingleColumn then
            [|header.ToString()|]
        elif header.IsTermColumn then
            [|
                header.ToString()
                if hasUnit then "Unit"
                $"Term Source REF ({header.GetColumnAccessionShort})"
                $"Term Accession Number ({header.GetColumnAccessionShort})"
            |]
        else
            failwithf "header %O is neither single nor term column" header


module CompositeColumn =

    /// Checks if the column header is a deprecated IO Header. If so, fixes it.
    ///
    /// The old format of IO Headers was only the type of IO so, e.g. "Source Name" or "Raw Data File".
    ///
    /// A "Source Name" column will now be mapped to the propper "Input [Source Name]", and all other IO types will be mapped to "Output [<IO Type>]".
    let fixDeprecatedIOHeader (stringCellCol : string []) =
        if stringCellCol.Length = 0 then
            failwith "Can't fix IOHeader Invalid column, neither header nor values given"
        let values = stringCellCol |> Array.skip 1
        match IOType.ofString (stringCellCol.[0]) with
        | IOType.FreeText _ -> stringCellCol
        | IOType.Source ->
            let comp = CompositeHeader.Input (IOType.Source)
            stringCellCol.[0] <- comp.ToString()
            stringCellCol
        | ioType ->
            let comp = CompositeHeader.Output (ioType)
            stringCellCol.[0] <- comp.ToString()
            stringCellCol

    let fromStringCellColumns (columns : array<string []>) : CompositeColumn =
        let header, cellParser =
            columns
            |> Array.map (fun c -> c.[0])
            |> CompositeHeader.fromStringCells
        let l = columns.[0].Length
        let cells =
            ResizeArray [|
                for i = 1 to l - 1 do
                    columns
                    |> Array.map (fun c -> c.[i])
                    |> cellParser
            |]
        CompositeColumn(header,cells)

    let stringCellColumnsOfFsColumns (columns : FsColumn []) : string [][] =
        columns
        |> Array.map (fun c ->
            c.ToDenseColumn()
            c.Cells
            |> Seq.toArray
            |> Array.map (fun cell -> cell.ValueAsString())
        )


    let fromFsColumns (columns : FsColumn []) : CompositeColumn =
        let stringCellColumns =
            columns
            |> Array.map (fun c ->
                c.ToDenseColumn()
                c.Cells
                |> Seq.toArray
                |> Array.map (fun c -> c.ValueAsString())
            )
        fromStringCellColumns stringCellColumns


// I think we really should not add FSharpAux for exactly one function.
module Aux =

    module List =

        /// Iterates over elements of the input list and groups adjacent elements.
        /// A new group is started when the specified predicate holds about the element
        /// of the list (and at the beginning of the iteration).
        ///
        /// For example:
        ///    List.groupWhen isOdd [3;3;2;4;1;2] = [[3]; [3; 2; 4]; [1; 2]]
        let groupWhen f list =
            list
            |> List.fold (
                fun acc e ->
                    match f e, acc with
                    | true  , _         -> [e] :: acc       // true case
                    | false , h :: t    -> (e :: h) :: t    // false case, non-empty acc list
                    | false , _         -> [[e]]            // false case, empty acc list
            ) []
            |> List.map List.rev
            |> List.rev

module Table =

    type ColumnOrder =
        | InputClass = 1
        | ProtocolClass = 2
        | ParamsClass = 3
        | OutputClass = 4

    let classifyHeaderOrder (header : CompositeHeader) =
        match header with
        | CompositeHeader.Input             _ -> ColumnOrder.InputClass

        | CompositeHeader.ProtocolType
        | CompositeHeader.ProtocolDescription
        | CompositeHeader.ProtocolUri
        | CompositeHeader.ProtocolVersion
        | CompositeHeader.ProtocolREF
        | CompositeHeader.Performer
        | CompositeHeader.Date                -> ColumnOrder.ProtocolClass

        | CompositeHeader.Component         _
        | CompositeHeader.Characteristic    _
        | CompositeHeader.Factor            _
        | CompositeHeader.Parameter         _
        | CompositeHeader.Comment           _
        | CompositeHeader.FreeText          _ -> ColumnOrder.ParamsClass

        | CompositeHeader.Output            _ -> ColumnOrder.OutputClass

    let classifyColumnOrder (column : CompositeColumn) =
        column.Header
        |> classifyHeaderOrder

    [<Literal>]
    let annotationTablePrefix = "annotationTable"

    let helperColumnStrings =
        [
            "Term Source REF"
            "Term Accession Number"
            "Unit"
            "Data Format"
            "Data Selector Format"
        ]

    let groupColumnsByHeader (stringCellColumns : array<string []>) =
        stringCellColumns
        |> Array.toList
        |> Aux.List.groupWhen (fun c ->
            let v = c.[0]
            helperColumnStrings
            |> List.exists (fun s -> v.StartsWith s)
            |> not
        )
        |> Array.ofList
        |> Array.map Array.ofList

    /// Returns the annotation table of the worksheet if it exists, else returns None
    let tryAnnotationTable (sheet : FsWorksheet) =
        sheet.Tables
        |> Seq.tryFind (fun t -> t.Name.StartsWith annotationTablePrefix)

    /// Groups and parses a collection of single columns into the according ISA composite columns
    let composeColumns (stringCellColumns : array<string []>) : CompositeColumn [] =
        stringCellColumns
        |> groupColumnsByHeader
        |> Array.map CompositeColumn.fromStringCellColumns

    ///// Groups and parses a collection of single columns into the according ISA composite columns
    //let composeArcTableValues (stringCellColumns : array<string []>) : CompositeHeader [] * ArcTableAux.ArcTableValues =
    //    let valueMap = System.Collections.Generic.Dictionary<int, CompositeCell>()
    //    let rowCount = stringCellColumns.[0].Length - 1
    //    let headers, columns =
    //        stringCellColumns
    //        |> groupColumnsByHeader
    //        |> Array.map (CompositeColumn.ColumnValueRefs.fromStringCellColumns valueMap)
    //        |> Array.unzip
    //    headers, ArcTableAux.ArcTableValues.fromRefColumns(columns, valueMap, rowCount)


    /// Returns the protocol described by the headers and a function for parsing the values of the matrix to the processes of this protocol
    let tryFromFsWorksheet (ds : Dataset) (sheet : FsWorksheet) =
        try
            match tryAnnotationTable sheet with
            | Some (t: FsTable) ->
                let stringCellColumns =
                    [|
                    for c = 1 to t.RangeAddress.LastAddress.ColumnNumber do
                        [|for r = 1 to t.RangeAddress.LastAddress.RowNumber do
                            match sheet.CellCollection.TryGetCell(r,c) with
                            | Some cell -> cell.ValueAsString()
                            | None -> ""
                        |]
                    |]
                let columns =
                    stringCellColumns
                    |> Array.map CompositeColumn.fixDeprecatedIOHeader
                    |> composeColumns
                let t = Table(sheet.Name, ResizeArray(),ds)
                columns
                |> Array.iter (fun c -> t.AddColumn(c.Header, c.Cells))
                t
                |> Some
            | None ->
                None
        with
        | err -> failwithf "Could not parse table with name \"%s\":\n%s" sheet.Name err.Message


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

    open ARCtrl.Spreadsheet.ArcAssay

    let fromFsWorkbook (wb : FsWorkbook) =
        let mdSheet = wb.GetWorksheetByName(metadataSheetName)
        let arcAssay = fromMetadataSheet mdSheet
        let assay = datasetFromTables arcAssay.Identifier wb
        assay.AdditionalType <- Some "Assay"
        assay.Description <- arcAssay.Description
        assay.Title <- arcAssay.Title
        assay

module Study =

    open ARCtrl.Spreadsheet.ArcStudy

    let fromFsWorkbook (wb : FsWorkbook) =
        let mdSheet = wb.GetWorksheetByName(metadataSheetName)
        let arcStudy = fromMetadataSheet mdSheet |> fst
        let study = datasetFromTables arcStudy.Identifier wb
        study.AdditionalType <- Some "Study"
        study.Description <- arcStudy.Description
        study.Title <- arcStudy.Title
        study

module Run =

    open ARCtrl.Spreadsheet.ArcRun

    let fromFsWorkbook (wb : FsWorkbook) =
        let mdSheet = wb.GetWorksheetByName(metadataSheetName)
        let arcRun = fromMetadataSheet mdSheet
        let run = datasetFromTables arcRun.Identifier wb
        run.AdditionalType <- Some "Run"
        run.Description <- arcRun.Description
        run.Title <- arcRun.Title
        run

module Workflow =

    open ARCtrl.Spreadsheet.ArcWorkflow

    let fromFsWorkbook (wb : FsWorkbook) =
        let mdSheet = wb.GetWorksheetByName(metadataSheetName)
        let arcWorkflow = fromMetadataSheet mdSheet
        let workflow = datasetFromTables arcWorkflow.Identifier wb
        workflow.AdditionalType <- Some "Workflow"
        workflow.Description <- arcWorkflow.Description
        workflow.Title <- arcWorkflow.Title
        workflow

module Investigation =

    open ARCtrl.Spreadsheet.ArcInvestigationExtensions
    open ARCtrl

    let fromFsWorkbook (wb : FsWorkbook) =
        let arcInvestigation = ArcInvestigation.fromFsWorkbook wb
        Dataset(
                arcInvestigation.Identifier,
                ?name = arcInvestigation.Title,
                ?description = arcInvestigation.Description,
                additionalType = "Investigation"
            )

module ARC =

    open ARCtrl
    open ARCtrl.Contract

    let readWorkbook (arcPath : string) (wbPath : string) =
        let path = ArcPathHelper.combine arcPath wbPath
        FsWorkbook.fromXlsxFile(path)

    let load (path : string) =
        let filePaths = FileSystemHelper.getAllFilePathsAsync path |> Async.RunSynchronously
        let topLevelDataset =
            filePaths
            |> Seq.pick (fun p ->
                match ARCtrl.ArcPathHelper.split p with
                | InvestigationPath _ ->
                    let wb = readWorkbook path p
                    Investigation.fromFsWorkbook wb |> Some
                | _ -> None
            )
        filePaths
        |> Seq.choose (fun p ->
            match ARCtrl.ArcPathHelper.split p with
            | AssayPath _ ->
                readWorkbook path p |> Assay.fromFsWorkbook |> Some
            | StudyPath _ ->
                readWorkbook path p |> Study.fromFsWorkbook |> Some
            | WorkflowPath _ ->
                readWorkbook path p |> Workflow.fromFsWorkbook |> Some
            | RunPath _ ->
                readWorkbook path p |> Run.fromFsWorkbook |> Some
            | _ -> None
        )
        |> Seq.iter (fun ds -> topLevelDataset.AddPart(ds) |> ignore)
        topLevelDataset


//let arcPath = @"C:\Users\HLWei\source\repos\ARCs\Ru_ChlamyHeatstress"

//let dataset = ARC.load arcPath

//dataset.RegisterFragmentSelectorProvider (CsvFragmentSelectorProvider())

//dataset.FinalData().[0].UpstreamSamples()
