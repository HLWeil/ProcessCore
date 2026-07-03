module ProcessCore.Spreadsheet.DatamapTable

open ProcessCore
open ProcessCore.Table
open ProcessCore.Helper
open FsSpreadsheet

[<Literal>]
let datamapTablePrefix = "datamapTable"

let helperColumnStrings = 
    [
        "Term Source REF"
        "Term Accession Number"
        "Data Format"
        "Data Selector Format"
    ]

let groupColumnsByHeader (columns : list<FsColumn>) = 
    columns
    |> Aux.List.groupWhen (fun c -> 
        let v = c.[1].ValueAsString()
        helperColumnStrings
        |> List.exists (fun s -> v.StartsWith s) 
        |> not
    )

/// Returns the annotation table of the worksheet if it exists, else returns None
let tryDatamapTable (sheet : FsWorksheet) =
    sheet.Tables
    |> Seq.tryFind (fun t -> t.Name.StartsWith datamapTablePrefix)

/// Groups and parses a collection of single columns into the according ISA composite columns
let composeColumns (columns : seq<FsColumn>) : ResizeArray<DataContext> =
    let l = (columns |> Seq.item 0).MaxRowIndex - 1
    let dc = ResizeArray([| for i = 0 to l - 1 do yield DataContext(Data("dummy"))|])
    columns
    |> Seq.toList
    |> groupColumnsByHeader
    |> List.iter (DatamapColumn.setFromFsColumns dc >> ignore)
    dc

let tryDataContextsFromFsWorksheet (sheet : FsWorksheet) : ResizeArray<DataContext> option =
    try
        match tryDatamapTable sheet with
        | Some (t: FsTable) -> 
            let dataContexts = 
                t.GetColumns(sheet.CellCollection)
                |> composeColumns
            Some dataContexts
        | None ->
            None
    with
    | err -> failwithf "Could not parse datamap table with name \"%s\":\n%s" sheet.Name err.Message


/// Returns the protocol described by the headers and a function for parsing the values of the matrix to the processes of this protocol
let tryFromFsWorksheet (sheet : FsWorksheet) =
    try
        match tryDatamapTable sheet with
        | Some (t: FsTable) -> 
            let dataContexts = 
                t.GetColumns(sheet.CellCollection)
                |> composeColumns
            Dataset(identifier = Identifier.createMissingIdentifier(), additionalType = "Datamap", dataContexts = dataContexts)
            |> Some
        | None ->
            None
    with
    | err -> failwithf "Could not parse datamap table with name \"%s\":\n%s" sheet.Name err.Message
