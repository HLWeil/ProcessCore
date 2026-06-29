module ProcessCore.Tests.Types.Process

open Fable.Pyxpecto
open ProcessCore

let tests = testList "Process" [

    testCase "equality by name" <| fun _ ->
        let p1 = Process("p1")
        let p2 = Process("p1")
        Expect.equal p1 p2 "Same name → equal"

    testCase "inequality different name" <| fun _ ->
        let p1 = Process("p1")
        let p2 = Process("p2")
        Expect.notEqual p1 p2 "Different names → not equal"

    testCase "AddInput does not deduplicate sample" <| fun _ ->
        let p = Process("p")
        let m = Sample("Sample1")
        p.AddInputSample(m)
        p.AddInputSample(m)
        Expect.equal p.Inputs.Count 2 "Identical sample added twice → two inputs"

    testCase "AddInput does not deduplicate data" <| fun _ ->
        let p = Process("p")
        let d = Data("file.csv")
        p.AddInputData(d)
        p.AddInputData(d)
        Expect.equal p.Inputs.Count 2 "Identical data added twice → two inputs"

    testCase "RemoveInput sample clears back-edge" <| fun _ ->
        let p = Process("p")
        let m = Sample("Sample1")
        p.AddInputSample(m)
        Expect.isTrue (m.InputOf |> Seq.exists (fun x -> x = p)) "Back-edge set before removal"
        p.RemoveInputSample(m)
        Expect.equal p.Inputs.Count 0 "Input removed from process"
        Expect.isFalse (m.InputOf |> Seq.exists (fun x -> x = p)) "Back-edge cleared after removal"

    testCase "RemoveInput data clears back-edge" <| fun _ ->
        let p = Process("p")
        let d = Data("file.csv")
        p.AddInputData(d)
        p.RemoveInputData(d)
        Expect.equal p.Inputs.Count 0 "Input removed from process"
        Expect.isFalse (d.InputOf |> Seq.exists (fun x -> x = p)) "Back-edge cleared after removal"

    testCase "AddOutput does not deduplicate sample" <| fun _ ->
        let p = Process("p")
        let m = Sample("Sample2")
        p.AddOutputSample(m)
        p.AddOutputSample(m)
        Expect.equal p.Outputs.Count 2 "Identical sample added twice → two outputs"

    testCase "RemoveOutput sample clears back-edge" <| fun _ ->
        let p = Process("p")
        let m = Sample("Sample2")
        p.AddOutputSample(m)
        Expect.isTrue (m.OutputOf |> Seq.exists (fun x -> x = p)) "Back-edge set before removal"
        p.RemoveOutputSample(m)
        Expect.equal p.Outputs.Count 0 "Output removed from process"
        Expect.isFalse (m.OutputOf |> Seq.exists (fun x -> x = p)) "Back-edge cleared after removal"

    testCase "AddParameterValue does not deduplicate" <| fun _ ->
        let p  = Process("p")
        let pv = Annotation("temperature", value = "37")
        p.AddParameterValue(pv)
        p.AddParameterValue(pv)
        Expect.equal p.ParameterValue.Count 2 "Identical PV added twice → two entries"

    testCase "RemoveParameterValue" <| fun _ ->
        let p  = Process("p")
        let pv = Annotation("temperature", value = "37")
        p.AddParameterValue(pv)
        p.RemoveParameterValue(pv)
        Expect.equal p.ParameterValue.Count 0 "PV should be removed"

    testCase "TryGetParameterValue found" <| fun _ ->
        let p  = Process("p")
        let pv = Annotation("temperature", value = "37")
        p.AddParameterValue(pv)
        let result = p.TryGetParameterValue("temperature")
        Expect.isSome result "Should find the PV"
        Expect.equal result.Value pv "Should return the correct PV"

    testCase "TryGetParameterValue not found" <| fun _ ->
        let p      = Process("p")
        let result = p.TryGetParameterValue("temperature")
        Expect.isNone result "Should return None for missing PV"

    testCase "GetParameterValue throws if missing" <| fun _ ->
        let p = Process("p")
        Expect.throws (fun () -> p.GetParameterValue("temperature") |> ignore) "Should throw for missing PV"

    testCase "InputSamples and InputData filter correctly" <| fun _ ->
        let p = Process("p")
        let m = Sample("Sample1")
        let d = Data("file.csv")
        p.AddInputSample(m)
        p.AddInputData(d)
        Expect.equal p.Inputs.Count 2 "Two inputs total"
        let mats  = p.InputSamples()
        let datas = p.InputData()
        Expect.equal mats.Count  1 "One sample input"
        Expect.equal datas.Count 1 "One data input"
        Expect.equal mats.[0]  m "Sample input is correct"
        Expect.equal datas.[0] d "Data input is correct"

    testCase "OutputSamples and OutputData filter correctly" <| fun _ ->
        let p = Process("p")
        let m = Sample("Sample2")
        let d = Data("output.csv")
        p.AddOutputSample(m)
        p.AddOutputData(d)
        let mats  = p.OutputSamples()
        let datas = p.OutputData()
        Expect.equal mats.Count  1 "One sample output"
        Expect.equal datas.Count 1 "One data output"
        Expect.equal mats.[0]  m "Sample output is correct"
        Expect.equal datas.[0] d "Data output is correct"

    testCase "ProtocolParameters returns empty without protocol" <| fun _ ->
        let p = Process("p")
        Expect.equal (p.ProtocolParameters().Count) 0 "No protocol → empty parameter list"

    testCase "ProtocolParameters delegates to protocol" <| fun _ ->
        let proto = Recipe("extraction")
        proto.AddParameter(FormalParameter("temperature"))
        proto.AddParameter(FormalParameter("rpm"))
        let p = Process("p")
        p.ExecutesProtocol <- Some proto
        Expect.equal (p.ProtocolParameters().Count) 2 "Should return parameters from protocol"

    testCase "AnnotationsByName - parameter source" <| fun _ ->
        let p  = Process("p")
        let pv = Annotation("temperature", value = "37", additionalType = "ParameterValue")
        p.AddParameterValue(pv)
        let result = p.AnnotationsByName("temperature")
        Expect.equal result.Count 1 "Should find PV in ParameterValue"

    testCase "AnnotationsByName - input node source" <| fun _ ->
        let p  = Process("p")
        let m  = Sample("Sample1")
        let pv = Annotation("organism", value = "E. coli", additionalType = "CharacteristicValue")
        m.AddAdditionalProperty(pv)
        p.AddInputSample(m)
        let result = p.AnnotationsByName("organism")
        Expect.equal result.Count 1 "Should find PV in input node AdditionalProperty"

    testCase "AnnotationsByName - output node source" <| fun _ ->
        let p  = Process("p")
        let m  = Sample("Sample2")
        let pv = Annotation("growth_phase", value = "log", additionalType = "FactorValue")
        m.AddAdditionalProperty(pv)
        p.AddOutputSample(m)
        let result = p.AnnotationsByName("growth_phase")
        Expect.equal result.Count 1 "Should find PV in output node AdditionalProperty"

    testCase "AnnotationsByName - protocol component source" <| fun _ ->
        let proto = Recipe("measurement")
        let pv    = Annotation("instrument", value = "Orbitrap", additionalType = "Component")
        proto.AddLabEquipment(pv)
        let p = Process("p")
        p.ExecutesProtocol <- Some proto
        let result = p.AnnotationsByName("instrument")
        Expect.equal result.Count 1 "Should find PV in protocol LabEquipment"

    testCase "AnnotationsByName - no match returns empty" <| fun _ ->
        let p      = Process("p")
        let result = p.AnnotationsByName("nonexistent")
        Expect.equal result.Count 0 "Unknown name → empty result"

]
