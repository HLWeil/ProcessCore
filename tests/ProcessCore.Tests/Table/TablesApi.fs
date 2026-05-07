module ProcessCore.Tests.Table.TablesApi

open Fable.Pyxpecto
open ProcessCore
open ProcessCore.Table

// ─── helpers ─────────────────────────────────────────────────────────────────

let makeDatasetWithTwoTables () =
    // "TableA" — 2 processes
    let p1a = LabProcess("TableA")
    p1a.AddInputMaterial(Material("S1", additionalType = "Source"))
    p1a.AddOutputMaterial(Material("O1", additionalType = "Sample"))
    let p2a = LabProcess("TableA")
    p2a.AddInputMaterial(Material("S2", additionalType = "Source"))
    p2a.AddOutputMaterial(Material("O2", additionalType = "Sample"))
    // "TableB" — 1 process
    let p1b = LabProcess("TableB")
    p1b.AddInputMaterial(Material("S3", additionalType = "Source"))
    p1b.AddOutputMaterial(Material("O3", additionalType = "Sample"))
    let ds = Dataset("DS")
    ds.AddProcess(p1a)
    ds.AddProcess(p2a)
    ds.AddProcess(p1b)
    ds

let tests = testList "TablesApi" [

    testCase "GetTables groups by process name" <| fun _ ->
        let ds     = makeDatasetWithTwoTables()
        let tables = Tables(ds).GetTables()
        Expect.equal tables.Count 2 "2 distinct tables"

    testCase "GetTables preserves insertion order" <| fun _ ->
        let ds     = makeDatasetWithTwoTables()
        let names  = Tables(ds).GetTables() |> Seq.map (fun t -> t.Name) |> Seq.toList
        Expect.equal names ["TableA"; "TableB"] "order preserved"

    testCase "TableCount" <| fun _ ->
        let ds = makeDatasetWithTwoTables()
        Expect.equal (Tables(ds).TableCount) 2 "TableCount = 2"

    testCase "TableNames" <| fun _ ->
        let ds    = makeDatasetWithTwoTables()
        let names = Tables(ds).TableNames |> Seq.toList
        Expect.equal names ["TableA"; "TableB"] "TableNames"

    testCase "GetTableAt — index 0 is TableA" <| fun _ ->
        let ds = makeDatasetWithTwoTables()
        let t  = Tables(ds).GetTableAt(0)
        Expect.equal t.Name "TableA" "GetTableAt(0)"

    testCase "GetTableAt — index 1 is TableB" <| fun _ ->
        let ds = makeDatasetWithTwoTables()
        let t  = Tables(ds).GetTableAt(1)
        Expect.equal t.Name "TableB" "GetTableAt(1)"

    testCase "GetTable — returns table by name" <| fun _ ->
        let ds = makeDatasetWithTwoTables()
        let t  = Tables(ds).GetTable("TableA")
        Expect.equal t.Name     "TableA" "name"
        // LabProcess equality is by reference, so AddProcess does not deduplicate same-named procs;
        Expect.equal t.RowCount 2        "RowCount = 2 (two processes with the same name in the dataset)"

    testCase "GetTable — non-existent raises" <| fun _ ->
        let ds = makeDatasetWithTwoTables()
        Expect.throws (fun () -> Tables(ds).GetTable("NoSuchTable") |> ignore) "raises on missing"

    testCase "TryGetTable — found" <| fun _ ->
        let ds = makeDatasetWithTwoTables()
        let t  = Tables(ds).TryGetTable("TableB")
        Expect.isSome t "TryGetTable found"

    testCase "TryGetTable — not found returns None" <| fun _ ->
        let ds = makeDatasetWithTwoTables()
        let t  = Tables(ds).TryGetTable("NoSuchTable")
        Expect.isNone t "TryGetTable not found"

    testCase "AddTable — returns empty table" <| fun _ ->
        let ds = makeDatasetWithTwoTables()
        let t  = Tables(ds).AddTable("TableC")
        Expect.equal t.Name     "TableC" "name"
        Expect.equal t.RowCount 0        "no rows yet"

    testCase "AddTable — duplicate name raises" <| fun _ ->
        let ds = makeDatasetWithTwoTables()
        Expect.throws (fun () -> Tables(ds).AddTable("TableA") |> ignore) "raises on duplicate"

    testCase "RemoveTable — removes all processes with that name" <| fun _ ->
        let ds     = makeDatasetWithTwoTables()
        let before = ds.Processes.Count
        Tables(ds).RemoveTable("TableA")
        // 2 processes with name "TableA" is in the dataset (no dedup by name during AddProcess)
        Expect.equal ds.Processes.Count (before - 2) "one TableA process removed"

    testCase "RemoveTable — other tables unaffected" <| fun _ ->
        let ds = makeDatasetWithTwoTables()
        Tables(ds).RemoveTable("TableA")
        let tablesAfter = Tables(ds).GetTables()
        Expect.equal tablesAfter.Count 1         "one table remains"
        Expect.equal tablesAfter.[0].Name "TableB" "TableB remains"

    testCase "RemoveTable — unknown name is no-op" <| fun _ ->
        let ds     = makeDatasetWithTwoTables()
        let before = ds.Processes.Count
        Tables(ds).RemoveTable("NoSuchTable")
        Expect.equal ds.Processes.Count before "no change"

    testCase "Dataset.Tables extension" <| fun _ ->
        let ds = makeDatasetWithTwoTables()
        Expect.equal ds.Tables.TableCount 2 "extension property works"

]
