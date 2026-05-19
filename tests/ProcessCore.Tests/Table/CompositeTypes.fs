module ProcessCore.Tests.Table.CompositeTypes

open Fable.Pyxpecto
open ProcessCore
open ProcessCore.Table

let tests = testList "CompositeTypes" [

    // ── IOType ────────────────────────────────────────────────────────────────

    testList "IOType" [

        testCase "Source case" <| fun _ ->
            let t = IOType.Source
            match t with
            | IOType.Source -> ()
            | _ -> failwith "Expected Source"

        testCase "Sample case" <| fun _ ->
            match IOType.Sample with
            | IOType.Sample -> ()
            | _ -> failwith "Expected Sample"

        testCase "Material case" <| fun _ ->
            match IOType.Material with
            | IOType.Material -> ()
            | _ -> failwith "Expected Material"

        testCase "Data case" <| fun _ ->
            match IOType.Data with
            | IOType.Data -> ()
            | _ -> failwith "Expected Data"

        testCase "FreeText case carries string" <| fun _ ->
            match IOType.FreeText "custom" with
            | IOType.FreeText s -> Expect.equal s "custom" "FreeText value preserved"
            | _ -> failwith "Expected FreeText"

    ]

    // ── CompositeHeader ───────────────────────────────────────────────────────

    testList "CompositeHeader" [

        testCase "Parameter carries name and TAN" <| fun _ ->
            let h = CompositeHeader.Parameter("Temperature", Some "PATO:0000146")
            match h with
            | CompositeHeader.Parameter(n, tan) ->
                Expect.equal n   "Temperature"         "name"
                Expect.equal tan (Some "PATO:0000146")  "TAN"
            | _ -> failwith "Expected Parameter"

        testCase "Parameter TAN can be None" <| fun _ ->
            match CompositeHeader.Parameter("enzyme", None) with
            | CompositeHeader.Parameter(_, None) -> ()
            | _ -> failwith "Expected Parameter with None TAN"

        testCase "Characteristic header" <| fun _ ->
            match CompositeHeader.Characteristic("organism", Some "OBI:001") with
            | CompositeHeader.Characteristic("organism", Some "OBI:001") -> ()
            | _ -> failwith "Expected Characteristic"

        testCase "Factor header" <| fun _ ->
            match CompositeHeader.Factor("growth_phase", None) with
            | CompositeHeader.Factor("growth_phase", None) -> ()
            | _ -> failwith "Expected Factor"

        testCase "Component header" <| fun _ ->
            match CompositeHeader.Component("instrument", None) with
            | CompositeHeader.Component("instrument", None) -> ()
            | _ -> failwith "Expected Component"

        testCase "ProtocolREF header" <| fun _ ->
            match CompositeHeader.ProtocolREF with
            | CompositeHeader.ProtocolREF -> ()
            | _ -> failwith "Expected ProtocolREF"

        testCase "ProtocolType header" <| fun _ ->
            match CompositeHeader.ProtocolType with
            | CompositeHeader.ProtocolType -> ()
            | _ -> failwith "Expected ProtocolType"

        testCase "Input carries IOType" <| fun _ ->
            match CompositeHeader.Input IOType.Source with
            | CompositeHeader.Input IOType.Source -> ()
            | _ -> failwith "Expected Input(Source)"

        testCase "Output carries IOType" <| fun _ ->
            match CompositeHeader.Output IOType.Data with
            | CompositeHeader.Output IOType.Data -> ()
            | _ -> failwith "Expected Output(Data)"

    ]

    // ── CompositeCell ─────────────────────────────────────────────────────────

    testList "CompositeCell" [

        testCase "FreeText cell" <| fun _ ->
            match CompositeCell.FreeText "hello" with
            | CompositeCell.FreeText s -> Expect.equal s "hello" "value preserved"
            | _ -> failwith "Expected FreeText"

        testCase "Term cell with TAN" <| fun _ ->
            match CompositeCell.Term("Trypsin", Some "NCIT:C17077") with
            | CompositeCell.Term(n, Some t) ->
                Expect.equal n "Trypsin"      "name"
                Expect.equal t "NCIT:C17077"  "TAN"
            | _ -> failwith "Expected Term"

        testCase "Unitized cell" <| fun _ ->
            match CompositeCell.Unitized("37", "°C", Some "UO:0000027") with
            | CompositeCell.Unitized(v, u, Some uTAN) ->
                Expect.equal v "37"           "value"
                Expect.equal u "°C"           "unit"
                Expect.equal uTAN "UO:0000027" "unitTAN"
            | _ -> failwith "Expected Unitized"

        testCase "Data cell carries Data object" <| fun _ ->
            let d = Data("rawData1.csv")
            match CompositeCell.Data d with
            | CompositeCell.Data d2 -> Expect.equal d2.Path "rawData1.csv" "path preserved"
            | _ -> failwith "Expected Data cell"

    ]

    // ── CompositeColumn ───────────────────────────────────────────────────────

    testList "CompositeColumn" [

        testCase "ColumnCount matches Cells.Count" <| fun _ ->
            let cells = ResizeArray<CompositeCell>([| CompositeCell.FreeText "a"; CompositeCell.FreeText "b" |])
            let col   = CompositeColumn(CompositeHeader.Parameter("temp", None), cells)
            Expect.equal col.ColumnCount 2 "ColumnCount = 2"

        testCase "Header is accessible" <| fun _ ->
            let col = CompositeColumn(CompositeHeader.ProtocolREF, ResizeArray())
            match col.Header with
            | CompositeHeader.ProtocolREF -> ()
            | _ -> failwith "Expected ProtocolREF header"

        testCase "Empty column has ColumnCount 0" <| fun _ ->
            let col = CompositeColumn(CompositeHeader.Parameter("x", None), ResizeArray())
            Expect.equal col.ColumnCount 0 "empty column"

    ]

]
