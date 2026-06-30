module ProcessCore.Tests.Types.Recipe

open Fable.Pyxpecto
open ProcessCore

let tests = testList "Recipe" [

    testCase "optional name constructor" <| fun _ ->
        let proto = Recipe()
        Expect.isNone proto.Name "Name should be None when not provided"

    testCase "equality by name and version" <| fun _ ->
        let p1 = Recipe("extraction", version = "1.0")
        let p2 = Recipe("extraction", version = "1.0")
        Expect.equal p1 p2 "Same name + version → equal"

    testCase "inequality different version" <| fun _ ->
        let p1 = Recipe("extraction", version = "1.0")
        let p2 = Recipe("extraction", version = "2.0")
        Expect.notEqual p1 p2 "Same name, different version → not equal"

    testCase "inequality different name" <| fun _ ->
        let p1 = Recipe("extraction")
        let p2 = Recipe("digestion")
        Expect.notEqual p1 p2 "Different names → not equal"

    testCase "AddParameter deduplicates by name" <| fun _ ->
        let proto = Recipe("extraction")
        let fp    = FormalParameter("temperature")
        proto.AddParameter(fp)
        proto.AddParameter(fp)
        Expect.equal proto.Parameters.Count 1 "Same FP added twice → one entry"

    testCase "RemoveParameter" <| fun _ ->
        let proto = Recipe("extraction")
        let fp    = FormalParameter("temperature")
        proto.AddParameter(fp)
        proto.RemoveParameter(fp)
        Expect.equal proto.Parameters.Count 0 "Parameter should be removed"

    testCase "RemoveParameter no-op for missing" <| fun _ ->
        let proto = Recipe("extraction")
        let fp    = FormalParameter("temperature")
        proto.RemoveParameter(fp)  // should not throw
        Expect.equal proto.Parameters.Count 0 "Count remains zero"

    testCase "TryGetParameter found" <| fun _ ->
        let proto = Recipe("extraction")
        let fp    = FormalParameter("temperature")
        proto.AddParameter(fp)
        let result = proto.TryGetParameter("temperature")
        Expect.isSome result "Should find the parameter"
        Expect.equal result.Value fp "Should return the correct FormalParameter"

    testCase "TryGetParameter not found" <| fun _ ->
        let proto  = Recipe("extraction")
        let result = proto.TryGetParameter("rpm")
        Expect.isNone result "Should return None for missing parameter"

    testCase "AddComponent deduplicates" <| fun _ ->
        let proto = Recipe("extraction")
        let pv    = Annotation("instrument", value = "Orbitrap")
        proto.AddComponent(pv)
        proto.AddComponent(pv)
        Expect.equal proto.Components.Count 1 "Identical PV added twice → one entry"

    testCase "RemoveComponent" <| fun _ ->
        let proto = Recipe("extraction")
        let pv    = Annotation("instrument", value = "Orbitrap")
        proto.AddComponent(pv)
        proto.RemoveComponent(pv)
        Expect.equal proto.Components.Count 0 "Component PV should be removed"

    testCase "components can hold software and materials" <| fun _ ->
        let proto = Recipe("analysis")
        let tool = Annotation("software", value = "FragPipe")
        let component = Annotation("enzyme", value = "Trypsin")
        proto.AddComponent(tool)
        proto.AddComponent(component)

        Expect.equal proto.Components.Count 2 "Both components should be retained"
        proto.RemoveComponent(tool)
        Expect.equal proto.Components.Count 1 "Remaining component should be retained"
        proto.RemoveComponent(component)
        Expect.equal proto.Components.Count 0 "Component should be removed"

    testCase "AddAdditionalProperty deduplicates" <| fun _ ->
        let proto = Recipe("extraction")
        let pv    = Annotation("note", value = "overnight incubation")
        proto.AddAdditionalProperty(pv)
        proto.AddAdditionalProperty(pv)
        Expect.equal proto.AdditionalProperty.Count 1 "Identical PV added twice → one entry"

]
