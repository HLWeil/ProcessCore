module ProcessCore.Tests.Types.LabProtocol

open Fable.Pyxpecto
open ProcessCore

let tests = testList "LabProtocol" [

    testCase "optional name constructor" <| fun _ ->
        let proto = LabProtocol()
        Expect.isNone proto.Name "Name should be None when not provided"

    testCase "equality by name and version" <| fun _ ->
        let p1 = LabProtocol("extraction", version = "1.0")
        let p2 = LabProtocol("extraction", version = "1.0")
        Expect.equal p1 p2 "Same name + version → equal"

    testCase "inequality different version" <| fun _ ->
        let p1 = LabProtocol("extraction", version = "1.0")
        let p2 = LabProtocol("extraction", version = "2.0")
        Expect.notEqual p1 p2 "Same name, different version → not equal"

    testCase "inequality different name" <| fun _ ->
        let p1 = LabProtocol("extraction")
        let p2 = LabProtocol("digestion")
        Expect.notEqual p1 p2 "Different names → not equal"

    testCase "AddParameter deduplicates by name" <| fun _ ->
        let proto = LabProtocol("extraction")
        let fp    = FormalParameter("temperature")
        proto.AddParameter(fp)
        proto.AddParameter(fp)
        Expect.equal proto.Parameters.Count 1 "Same FP added twice → one entry"

    testCase "RemoveParameter" <| fun _ ->
        let proto = LabProtocol("extraction")
        let fp    = FormalParameter("temperature")
        proto.AddParameter(fp)
        proto.RemoveParameter(fp)
        Expect.equal proto.Parameters.Count 0 "Parameter should be removed"

    testCase "RemoveParameter no-op for missing" <| fun _ ->
        let proto = LabProtocol("extraction")
        let fp    = FormalParameter("temperature")
        proto.RemoveParameter(fp)  // should not throw
        Expect.equal proto.Parameters.Count 0 "Count remains zero"

    testCase "TryGetParameter found" <| fun _ ->
        let proto = LabProtocol("extraction")
        let fp    = FormalParameter("temperature")
        proto.AddParameter(fp)
        let result = proto.TryGetParameter("temperature")
        Expect.isSome result "Should find the parameter"
        Expect.equal result.Value fp "Should return the correct FormalParameter"

    testCase "TryGetParameter not found" <| fun _ ->
        let proto  = LabProtocol("extraction")
        let result = proto.TryGetParameter("rpm")
        Expect.isNone result "Should return None for missing parameter"

    testCase "AddLabEquipment deduplicates" <| fun _ ->
        let proto = LabProtocol("extraction")
        let pv    = PropertyValue("instrument", value = "Orbitrap")
        proto.AddLabEquipment(pv)
        proto.AddLabEquipment(pv)
        Expect.equal proto.LabEquipment.Count 1 "Identical PV added twice → one entry"

    testCase "RemoveLabEquipment" <| fun _ ->
        let proto = LabProtocol("extraction")
        let pv    = PropertyValue("instrument", value = "Orbitrap")
        proto.AddLabEquipment(pv)
        proto.RemoveLabEquipment(pv)
        Expect.equal proto.LabEquipment.Count 0 "LabEquipment PV should be removed"

    testCase "AddAdditionalProperty deduplicates" <| fun _ ->
        let proto = LabProtocol("extraction")
        let pv    = PropertyValue("note", value = "overnight incubation")
        proto.AddAdditionalProperty(pv)
        proto.AddAdditionalProperty(pv)
        Expect.equal proto.AdditionalProperty.Count 1 "Identical PV added twice → one entry"

]
