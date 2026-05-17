namespace ProcessCore

open System

/// Semantic relation between two fragment selectors.
type FragmentRelation =
    | Exact
    | Contains
    | Disjoint
    | Unknown

/// Selector-provider contract used by the core traversal layer.
type IFragmentSelectorProvider =
    
    abstract SelectorFormat: string

    /// Relates the first selector as the possible container of the second selector.
    abstract TryRelate: container: string -> candidate: string -> FragmentRelation option

/// Typed selector-provider contract for implementations of a selector language.
[<AbstractClass>]
type FragmentSelectorProviderBase<'Selector>() =    

    abstract SelectorFormat: string
    abstract TryParse: string -> 'Selector option
    abstract ToSelectorString: 'Selector -> string
    abstract Relate: container: 'Selector -> candidate: 'Selector -> FragmentRelation

    interface IFragmentSelectorProvider with

        member this.SelectorFormat =
            this.SelectorFormat

        member this.TryRelate container candidate =
            match this.TryParse container, this.TryParse candidate with
            | Some c, Some s -> this.Relate c s |> Some
            | _ -> None

module FragmentSelector =

    let relate
        (containerPath: string)
        (containerSelector: string option)
        (containerSelectorFormat: string option)
        (candidatePath: string)
        (candidateSelector: string option)
        (candidateSelectorFormat: string option)
        (tryGetProvider: string -> IFragmentSelectorProvider option)
        : FragmentRelation =

        if containerPath <> candidatePath then Disjoint
        else
            match containerSelector, candidateSelector with
            | None, None -> Exact
            | None, Some _ -> Contains
            | Some _, None -> Unknown
            | Some a, Some b when a = b -> Exact
            | Some a, Some b ->
                match containerSelectorFormat, candidateSelectorFormat with
                | Some cf, Some xf when cf = xf ->
                    match tryGetProvider cf with
                    | Some provider ->
                        match provider.TryRelate a b with
                        | Some r -> r
                        | None -> Unknown
                    | None -> Unknown
                | _ -> Unknown

type CsvPosition =
    | Index of int
    | Last

type CsvAxisRange =
    {
        First: CsvPosition
        Last: CsvPosition
    }

type CsvCellRange =
    {
        Rows: CsvAxisRange
        Columns: CsvAxisRange
    }

type CsvFragmentSelector =
    | RowSelector of CsvAxisRange list
    | ColumnSelector of CsvAxisRange list
    | CellSelector of CsvCellRange list

module CsvFragmentSelectorParsing =

    let trimFragmentMarker (text: string) =
        let trimmed = text.Trim()
        if trimmed.StartsWith("#") then trimmed.Substring(1)
        else trimmed

    let tryParseIndex (text: string) =
        match Int32.TryParse text with
        | true, value when value >= 1 -> Some value
        | _ -> None

    let tryParsePosition (text: string) =
        let trimmed = text.Trim()
        if trimmed = "*" then Some Last
        else tryParseIndex trimmed |> Option.map Index

    let tryPositionToRange first last =
        match first, last with
        | Index f, None -> Some { First = Index f; Last = Index f }
        | Index f, Some (Index l) when f <= l -> Some { First = Index f; Last = Index l }
        | Index f, Some Last -> Some { First = Index f; Last = Last }
        | Last, None -> Some { First = Last; Last = Last }
        | Last, Some Last -> Some { First = Last; Last = Last }
        | Last, Some (Index _) -> None
        | Index _, Some (Index _) -> None

    let tryParseSingleSpec (text: string) =
        let parts = text.Split([| '-' |])
        match parts with
        | [| first |] ->
            tryParsePosition first
            |> Option.bind (fun f -> tryPositionToRange f None)
        | [| first; last |] ->
            match tryParsePosition first, tryParsePosition last with
            | Some f, Some l -> tryPositionToRange f (Some l)
            | _ -> None
        | _ -> None

    let tryParseCellSpec (text: string) =
        let parts = text.Split([| '-' |])

        let tryParseCellEndpoint (endpoint: string) =
            let coordinates = endpoint.Split([| ',' |])
            match coordinates with
            | [| row; column |] ->
                match tryParsePosition row, tryParsePosition column with
                | Some r, Some c -> Some (r, c)
                | _ -> None
            | _ -> None

        match parts with
        | [| endpoint |] ->
            match tryParseCellEndpoint endpoint with
            | Some (r, c) ->
                Some { Rows = { First = r; Last = r }; Columns = { First = c; Last = c } }
            | None -> None
        | [| first; last |] ->
            match tryParseCellEndpoint first, tryParseCellEndpoint last with
            | Some (r1, c1), Some (r2, c2) ->
                match tryPositionToRange r1 (Some r2), tryPositionToRange c1 (Some c2) with
                | Some rows, Some columns -> Some { Rows = rows; Columns = columns }
                | _ -> None
            | _ -> None
        | _ -> None

    let tryParseSelections parser (body: string) =
        let specs = body.Split([| ';' |])
        if specs.Length = 0 then None
        else
            let parsed = ResizeArray<_>()
            let mutable valid = true
            for spec in specs do
                if spec = "" then
                    valid <- false
                else
                    match parser spec with
                    | Some selection -> parsed.Add(selection)
                    | None -> valid <- false
            if valid && parsed.Count > 0 then
                parsed |> Seq.toList |> Some
            else None

    let tryParse (text: string) =
        let fragment = trimFragmentMarker text
        if fragment.StartsWith("row=") then
            fragment.Substring("row=".Length)
            |> tryParseSelections tryParseSingleSpec
            |> Option.map RowSelector
        elif fragment.StartsWith("col=") then
            fragment.Substring("col=".Length)
            |> tryParseSelections tryParseSingleSpec
            |> Option.map ColumnSelector
        elif fragment.StartsWith("cell=") then
            fragment.Substring("cell=".Length)
            |> tryParseSelections tryParseCellSpec
            |> Option.map CellSelector
        else None

    let positionToString position =
        match position with
        | Index i -> string i
        | Last -> "*"

    let axisRangeToString range =
        if range.First = range.Last then positionToString range.First
        else positionToString range.First + "-" + positionToString range.Last

    let cellRangeToString range =
        let startCell = positionToString range.Rows.First + "," + positionToString range.Columns.First
        if range.Rows.First = range.Rows.Last && range.Columns.First = range.Columns.Last then
            startCell
        else
            startCell + "-" + positionToString range.Rows.Last + "," + positionToString range.Columns.Last

    let toSelectorString selector =
        match selector with
        | RowSelector rows ->
            "row=" + (rows |> List.map axisRangeToString |> String.concat ";")
        | ColumnSelector columns ->
            "col=" + (columns |> List.map axisRangeToString |> String.concat ";")
        | CellSelector cells ->
            "cell=" + (cells |> List.map cellRangeToString |> String.concat ";")

module CsvFragmentSelectorRelation =

    type Rectangle =
        {
            Rows: CsvAxisRange
            Columns: CsvAxisRange
        }

    let allAxis = { First = Index 1; Last = Last }

    let toRectangles selector =
        match selector with
        | RowSelector rows ->
            rows |> List.map (fun rowRange -> { Rows = rowRange; Columns = allAxis })
        | ColumnSelector columns ->
            columns |> List.map (fun columnRange -> { Rows = allAxis; Columns = columnRange })
        | CellSelector cells ->
            cells |> List.map (fun cellRange -> { Rows = cellRange.Rows; Columns = cellRange.Columns })

    let lowerContains containerLower candidateLower =
        match containerLower, candidateLower with
        | Index c, Index x -> c <= x
        | Index _, Last -> true
        | Last, Last -> true
        | Last, Index _ -> false

    let upperContains containerUpper candidateUpper =
        match containerUpper, candidateUpper with
        | Last, _ -> true
        | Index _, Last -> false
        | Index c, Index x -> c >= x

    let axisContains container candidate =
        lowerContains container.First candidate.First
        && upperContains container.Last candidate.Last

    let axisDisjoint a b =
        match a.Last, b.Last with
        | Index aLast, _ ->
            match b.First with
            | Index bFirst -> aLast < bFirst
            | Last -> false
        | Last, _ -> false
        ||
        match b.Last, a.First with
        | Index bLast, Index aFirst -> bLast < aFirst
        | _ -> false

    let rectangleContains container candidate =
        axisContains container.Rows candidate.Rows
        && axisContains container.Columns candidate.Columns

    let rectangleDisjoint a b =
        axisDisjoint a.Rows b.Rows
        || axisDisjoint a.Columns b.Columns

    let contains container candidate =
        let containerRects = toRectangles container
        let candidateRects = toRectangles candidate
        candidateRects
        |> List.forall (fun candidateRect ->
            containerRects
            |> List.exists (fun containerRect -> rectangleContains containerRect candidateRect))

    let disjoint container candidate =
        let containerRects = toRectangles container
        let candidateRects = toRectangles candidate
        containerRects
        |> List.forall (fun containerRect ->
            candidateRects
            |> List.forall (fun candidateRect -> rectangleDisjoint containerRect candidateRect))

/// RFC 7111 fragment selector provider for text/csv row, column, and cell fragments.
type CsvFragmentSelectorProvider() =

    inherit FragmentSelectorProviderBase<CsvFragmentSelector>()

    static member SelectorFormatUri = "https://datatracker.ietf.org/doc/html/rfc7111"

    override _.SelectorFormat = CsvFragmentSelectorProvider.SelectorFormatUri

    override _.TryParse(text: string) =
        CsvFragmentSelectorParsing.tryParse text

    override _.ToSelectorString(selector: CsvFragmentSelector) =
        CsvFragmentSelectorParsing.toSelectorString selector

    override _.Relate(container: CsvFragmentSelector) (candidate: CsvFragmentSelector) =
        if container = candidate then Exact
        elif CsvFragmentSelectorRelation.contains container candidate then Contains
        elif CsvFragmentSelectorRelation.disjoint container candidate then Disjoint
        else Unknown