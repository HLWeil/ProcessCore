module ProcessCore.Tests.Table.TableAux

open Fable.Pyxpecto
open ProcessCore
open ProcessCore.Table

let tests = testList "TableAux" [

    // ── MaterialIOType ────────────────────────────────────────────────────────

    testCase "MaterialIOType — Source" <| fun _ ->
        let m = Material("S1", additionalType = "Source")
        Expect.equal (TableAux.MaterialIOType m) IOType.Source "AdditionalType=Source → IOType.Source"

    testCase "MaterialIOType — Sample" <| fun _ ->
        let m = Material("S1", additionalType = "Sample")
        Expect.equal (TableAux.MaterialIOType m) IOType.Sample "AdditionalType=Sample → IOType.Sample"

    testCase "MaterialIOType — Material" <| fun _ ->
        let m = Material("S1", additionalType = "Material")
        Expect.equal (TableAux.MaterialIOType m) IOType.Material "AdditionalType=Material → IOType.Material"

    testCase "MaterialIOType — None defaults to Sample" <| fun _ ->
        let m = Material("S1")
        Expect.equal (TableAux.MaterialIOType m) IOType.Sample "None → IOType.Sample"

    testCase "MaterialIOType — unknown string → FreeText" <| fun _ ->
        let m = Material("S1", additionalType = "SpecialNode")
        match TableAux.MaterialIOType m with
        | IOType.FreeText s -> Expect.equal s "SpecialNode" "FreeText carries the string"
        | _ -> failwith "Expected FreeText"

    // ── PVToCell ──────────────────────────────────────────────────────────────

    testCase "PVToCell — unitized PV" <| fun _ ->
        let pv = PropertyValue("temperature", value = "37", unit = "°C", unitTAN = "UO:0000027")
        match TableAux.PVToCell pv with
        | CompositeCell.Unitized(v, u, _) ->
            Expect.equal v "37" "value"
            Expect.equal u "°C" "unit"
        | _ -> failwith "Expected Unitized"

    testCase "PVToCell — term PV (valueTAN, no unit)" <| fun _ ->
        let pv = PropertyValue("enzyme", value = "Trypsin", valueTAN = "NCIT:C17077")
        match TableAux.PVToCell pv with
        | CompositeCell.Term(n, Some t) ->
            Expect.equal n "Trypsin"      "name"
            Expect.equal t "NCIT:C17077"  "TAN"
        | _ -> failwith "Expected Term"

    testCase "PVToCell — freetext PV (value only)" <| fun _ ->
        let pv = PropertyValue("comment", value = "hello")
        match TableAux.PVToCell pv with
        | CompositeCell.FreeText v -> Expect.equal v "hello" "value"
        | _ -> failwith "Expected FreeText"

    testCase "PVToCell — no value returns empty FreeText" <| fun _ ->
        let pv = PropertyValue("empty")
        match TableAux.PVToCell pv with
        | CompositeCell.FreeText "" -> ()
        | _ -> failwith "Expected empty FreeText"

    // ── PVToHeader ────────────────────────────────────────────────────────────

    testCase "PVToHeader — ParameterValue" <| fun _ ->
        let pv = PropertyValue("temperature", additionalType = "ParameterValue")
        match TableAux.PVToHeader pv with
        | CompositeHeader.Parameter(dt) when dt.Name = "temperature" -> ()
        | _ -> failwith "Expected Parameter"

    testCase "PVToHeader — FactorValue" <| fun _ ->
        let pv = PropertyValue("growth_phase", additionalType = "FactorValue")
        match TableAux.PVToHeader pv with
        | CompositeHeader.Factor(dt) when dt.Name = "growth_phase" -> ()
        | _ -> failwith "Expected Factor"

    testCase "PVToHeader — CharacteristicValue" <| fun _ ->
        let pv = PropertyValue("organism", additionalType = "CharacteristicValue")
        match TableAux.PVToHeader pv with
        | CompositeHeader.Characteristic(dt) when dt.Name = "organism" -> ()
        | _ -> failwith "Expected Characteristic"

    testCase "PVToHeader — Component" <| fun _ ->
        let pv = PropertyValue("instrument", additionalType = "Component")
        match TableAux.PVToHeader pv with
        | CompositeHeader.Component(dt) when dt.Name = "instrument" -> ()
        | _ -> failwith "Expected Component"

    testCase "PVToHeader — no AdditionalType defaults to Parameter" <| fun _ ->
        let pv = PropertyValue("whatever")
        match TableAux.PVToHeader pv with
        | CompositeHeader.Parameter(dt) when dt.Name = "whatever" -> ()
        | _ -> failwith "Expected Parameter default"

    // ── ApplyCellToPV ─────────────────────────────────────────────────────────

    testCase "ApplyCellToPV — FreeText sets Value, clears others" <| fun _ ->
        let pv = PropertyValue("x", value = "old", unit = "°C", valueTAN = "tan", unitTAN = "uTAN")
        TableAux.ApplyCellToPV(pv, CompositeCell.FreeText "new")
        Expect.equal pv.Value    (Some "new") "Value updated"
        Expect.equal pv.Unit     None         "Unit cleared"
        Expect.equal pv.ValueTAN None         "ValueTAN cleared"
        Expect.equal pv.UnitTAN  None         "UnitTAN cleared"

    testCase "ApplyCellToPV — Term sets Value and ValueTAN, clears unit" <| fun _ ->
        let pv = PropertyValue("x", unit = "°C", unitTAN = "uTAN")
        TableAux.ApplyCellToPV(pv, CompositeCell.Term("Trypsin", Some "NCIT:C17077"))
        Expect.equal pv.Value    (Some "Trypsin")      "Value"
        Expect.equal pv.ValueTAN (Some "NCIT:C17077")  "ValueTAN"
        Expect.equal pv.Unit     None                  "Unit cleared"
        Expect.equal pv.UnitTAN  None                  "UnitTAN cleared"

    testCase "ApplyCellToPV — Unitized sets all three" <| fun _ ->
        let pv = PropertyValue("x")
        TableAux.ApplyCellToPV(pv, CompositeCell.Unitized("37", "°C", Some "UO:0000027"))
        Expect.equal pv.Value   (Some "37")           "Value"
        Expect.equal pv.Unit    (Some "°C")           "Unit"
        Expect.equal pv.UnitTAN (Some "UO:0000027")   "UnitTAN"

    testCase "ApplyCellToPV — Data cell is a no-op" <| fun _ ->
        let pv = PropertyValue("x", value = "original")
        TableAux.ApplyCellToPV(pv, CompositeCell.Data(Data("file.csv")))
        Expect.equal pv.Value (Some "original") "Value unchanged"

    // ── MakePV roundtrip ──────────────────────────────────────────────────────

    testCase "MakePV roundtrip — Parameter + Unitized" <| fun _ ->
        let temp = DefinedTerm("temperature", "PATO:0000146")
        let header = CompositeHeader.Parameter(temp)
        let cell   = CompositeCell.Unitized("37", "°C", Some "UO:0000027")
        let pv     = TableAux.MakePV(header, cell)
        Expect.equal pv.Name           "temperature"        "name"
        Expect.equal pv.NameTAN        (Some "PATO:0000146") "nameTAN"
        Expect.equal pv.AdditionalType (Some "ParameterValue") "additionalType"
        Expect.equal pv.Value          (Some "37")           "value"
        Expect.equal pv.Unit           (Some "°C")           "unit"

    testCase "MakePV roundtrip — Characteristic + Term" <| fun _ ->
        let organism = DefinedTerm("organism")
        let header = CompositeHeader.Characteristic(organism)
        let cell   = CompositeCell.Term("E. coli", Some "NCBITAXON:562")
        let pv     = TableAux.MakePV(header, cell)
        Expect.equal pv.AdditionalType (Some "CharacteristicValue") "additionalType"
        Expect.equal pv.Value          (Some "E. coli")             "value"
        Expect.equal pv.ValueTAN       (Some "NCBITAXON:562")       "valueTAN"

]
