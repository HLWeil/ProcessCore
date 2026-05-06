module ProcessCore.Tests.Types.LabProcess

open Fable.Pyxpecto
open ProcessCore

let tests = testList "LabProcess" [

    testCase "equality by name" <| fun _ ->
        let p1 = LabProcess("p1")
        let p2 = LabProcess("p1")
        Expect.equal p1 p2 "Same name → equal"

    testCase "inequality different name" <| fun _ ->
        let p1 = LabProcess("p1")
        let p2 = LabProcess("p2")
        Expect.notEqual p1 p2 "Different names → not equal"

    testCase "AddInput deduplicates material" <| fun _ ->
        let p = LabProcess("p")
        let m = Material("Sample1")
        p.AddInputMaterial(m)
        p.AddInputMaterial(m)
        Expect.equal p.Inputs.Count 1 "Identical material added twice → one input"

    testCase "AddInput deduplicates data" <| fun _ ->
        let p = LabProcess("p")
        let d = Data("file.csv")
        p.AddInputData(d)
        p.AddInputData(d)
        Expect.equal p.Inputs.Count 1 "Identical data added twice → one input"

    testCase "RemoveInput material clears back-edge" <| fun _ ->
        let p = LabProcess("p")
        let m = Material("Sample1")
        p.AddInputMaterial(m)
        Expect.isTrue (m.InputOf |> Seq.exists (fun x -> x = p)) "Back-edge set before removal"
        p.RemoveInputMaterial(m)
        Expect.equal p.Inputs.Count 0 "Input removed from process"
        Expect.isFalse (m.InputOf |> Seq.exists (fun x -> x = p)) "Back-edge cleared after removal"

    testCase "RemoveInput data clears back-edge" <| fun _ ->
        let p = LabProcess("p")
        let d = Data("file.csv")
        p.AddInputData(d)
        p.RemoveInputData(d)
        Expect.equal p.Inputs.Count 0 "Input removed from process"
        Expect.isFalse (d.InputOf |> Seq.exists (fun x -> x = p)) "Back-edge cleared after removal"

    testCase "AddOutput deduplicates material" <| fun _ ->
        let p = LabProcess("p")
        let m = Material("Sample2")
        p.AddOutputMaterial(m)
        p.AddOutputMaterial(m)
        Expect.equal p.Outputs.Count 1 "Identical material added twice → one output"

    testCase "RemoveOutput material clears back-edge" <| fun _ ->
        let p = LabProcess("p")
        let m = Material("Sample2")
        p.AddOutputMaterial(m)
        Expect.isTrue (m.OutputOf |> Seq.exists (fun x -> x = p)) "Back-edge set before removal"
        p.RemoveOutputMaterial(m)
        Expect.equal p.Outputs.Count 0 "Output removed from process"
        Expect.isFalse (m.OutputOf |> Seq.exists (fun x -> x = p)) "Back-edge cleared after removal"

    testCase "AddParameterValue deduplicates" <| fun _ ->
        let p  = LabProcess("p")
        let pv = PropertyValue("temperature", value = "37")
        p.AddParameterValue(pv)
        p.AddParameterValue(pv)
        Expect.equal p.ParameterValue.Count 1 "Identical PV added twice → one entry"

    testCase "RemoveParameterValue" <| fun _ ->
        let p  = LabProcess("p")
        let pv = PropertyValue("temperature", value = "37")
        p.AddParameterValue(pv)
        p.RemoveParameterValue(pv)
        Expect.equal p.ParameterValue.Count 0 "PV should be removed"

    testCase "TryGetParameterValue found" <| fun _ ->
        let p  = LabProcess("p")
        let pv = PropertyValue("temperature", value = "37")
        p.AddParameterValue(pv)
        let result = p.TryGetParameterValue("temperature")
        Expect.isSome result "Should find the PV"
        Expect.equal result.Value pv "Should return the correct PV"

    testCase "TryGetParameterValue not found" <| fun _ ->
        let p      = LabProcess("p")
        let result = p.TryGetParameterValue("temperature")
        Expect.isNone result "Should return None for missing PV"

    testCase "GetParameterValue throws if missing" <| fun _ ->
        let p = LabProcess("p")
        Expect.throws (fun () -> p.GetParameterValue("temperature") |> ignore) "Should throw for missing PV"

    testCase "InputMaterials and InputData filter correctly" <| fun _ ->
        let p = LabProcess("p")
        let m = Material("Sample1")
        let d = Data("file.csv")
        p.AddInputMaterial(m)
        p.AddInputData(d)
        Expect.equal p.Inputs.Count 2 "Two inputs total"
        let mats  = p.InputMaterials()
        let datas = p.InputData()
        Expect.equal mats.Count  1 "One material input"
        Expect.equal datas.Count 1 "One data input"
        Expect.equal mats.[0]  m "Material input is correct"
        Expect.equal datas.[0] d "Data input is correct"

    testCase "OutputMaterials and OutputData filter correctly" <| fun _ ->
        let p = LabProcess("p")
        let m = Material("Sample2")
        let d = Data("output.csv")
        p.AddOutputMaterial(m)
        p.AddOutputData(d)
        let mats  = p.OutputMaterials()
        let datas = p.OutputData()
        Expect.equal mats.Count  1 "One material output"
        Expect.equal datas.Count 1 "One data output"
        Expect.equal mats.[0]  m "Material output is correct"
        Expect.equal datas.[0] d "Data output is correct"

    testCase "ProtocolParameters returns empty without protocol" <| fun _ ->
        let p = LabProcess("p")
        Expect.equal (p.ProtocolParameters().Count) 0 "No protocol → empty parameter list"

    testCase "ProtocolParameters delegates to protocol" <| fun _ ->
        let proto = LabProtocol("extraction")
        proto.AddParameter(FormalParameter("temperature"))
        proto.AddParameter(FormalParameter("rpm"))
        let p = LabProcess("p")
        p.ExecutesProtocol <- Some proto
        Expect.equal (p.ProtocolParameters().Count) 2 "Should return parameters from protocol"

    testCase "PropertyValuesByName - parameter source" <| fun _ ->
        let p  = LabProcess("p")
        let pv = PropertyValue("temperature", value = "37", additionalType = "ParameterValue")
        p.AddParameterValue(pv)
        let result = p.PropertyValuesByName("temperature")
        Expect.equal result.Count 1 "Should find PV in ParameterValue"

    testCase "PropertyValuesByName - input node source" <| fun _ ->
        let p  = LabProcess("p")
        let m  = Material("Sample1")
        let pv = PropertyValue("organism", value = "E. coli", additionalType = "CharacteristicValue")
        m.AddAdditionalProperty(pv)
        p.AddInputMaterial(m)
        let result = p.PropertyValuesByName("organism")
        Expect.equal result.Count 1 "Should find PV in input node AdditionalProperty"

    testCase "PropertyValuesByName - output node source" <| fun _ ->
        let p  = LabProcess("p")
        let m  = Material("Sample2")
        let pv = PropertyValue("growth_phase", value = "log", additionalType = "FactorValue")
        m.AddAdditionalProperty(pv)
        p.AddOutputMaterial(m)
        let result = p.PropertyValuesByName("growth_phase")
        Expect.equal result.Count 1 "Should find PV in output node AdditionalProperty"

    testCase "PropertyValuesByName - protocol component source" <| fun _ ->
        let proto = LabProtocol("measurement")
        let pv    = PropertyValue("instrument", value = "Orbitrap", additionalType = "Component")
        proto.AddLabEquipment(pv)
        let p = LabProcess("p")
        p.ExecutesProtocol <- Some proto
        let result = p.PropertyValuesByName("instrument")
        Expect.equal result.Count 1 "Should find PV in protocol LabEquipment"

    testCase "PropertyValuesByName - no match returns empty" <| fun _ ->
        let p      = LabProcess("p")
        let result = p.PropertyValuesByName("nonexistent")
        Expect.equal result.Count 0 "Unknown name → empty result"

]
