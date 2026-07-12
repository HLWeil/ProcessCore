module ProcessCore.Tests.Spreadsheet.Scaffold


open Fable.Pyxpecto
open ProcessCore
open CrossAsync
open ProcessCore.Helper
open ProcessCore.Tests.Fixtures
open TestingUtils
open ProcessCore.Table


let tests = testList "Workbooks" [

    let testBaseFolder =
        #if FABLE_COMPILER_JAVASCRIPT || FABLE_COMPILER_TYPESCRIPT
        "./tests/ProcessCore.Tests/TestObjects"
        #else
        Path.combine __SOURCE_DIRECTORY__ "../TestObjects"
        #endif

    testCaseCrossAsync "ReadTestARC" (crossAsync {
        let testARCPath = Path.combine testBaseFolder "testARC"
        let! arc = ScaffoldReader.ARC.loadAsync (ARC) testARCPath

        Expect.equal arc.Identifier "Facultative-CAM-in-Talinum" "ARC should have correct identifier"
        let arcAT = Expect.wantSome arc.AdditionalType "ARC has additional type"
        Expect.equal arcAT "Investigation" "ARC has correct additional type"
        Expect.hasLength arc.Agents 5 "ARC should have 5 agents"

        Expect.hasLength arc.HasPart 2 "ARC should have 2 parts"
        let assay = Expect.wantSome (arc.TryGetPart "GCqTOF_targets") "Assay should be present"
        let assayAT = Expect.wantSome assay.AdditionalType "Assay has additional type"
        Expect.equal assayAT "Assay" "Assay has correct additional type"
        Expect.hasLength assay.DataContexts 3 "Assay should have 3 data contexts"

        let study = Expect.wantSome (arc.TryGetPart "TalinumSamples-STRI") "Study should be present"
        let studyAT = Expect.wantSome study.AdditionalType "Study has additional type"
        Expect.equal studyAT "Study" "Study has correct additional type"
    })
    ]

