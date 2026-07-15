module ProcessCore.Tests.ARC

open Fable.Pyxpecto
open ProcessCore
open CrossAsync
open ProcessCore.Helper
open ProcessCore.Tests.Fixtures
open TestingUtils
open ProcessCore.Table

let testBaseFolder =
    #if FABLE_COMPILER_JAVASCRIPT || FABLE_COMPILER_TYPESCRIPT
    "./tests/ProcessCore.Tests"
    #else
    __SOURCE_DIRECTORY__
    #endif

let testObjectsFolder = Path.combine testBaseFolder "TestObjects"

let testResultsFolder = 
    #if FABLE_COMPILER_JAVASCRIPT || FABLE_COMPILER_TYPESCRIPT
    Path.combineMany [testBaseFolder; "TestResults"; "js"]
    #endif
    #if FABLE_COMPILER_PYTHON
    Path.combineMany [testBaseFolder; "TestResults"; "py"]
    #endif
    #if !FABLE_COMPILER
    Path.combineMany [testBaseFolder; "TestResults"; "net"]
    #endif


let tests = testList "ARC" [

    testCaseCrossAsync "loadXLSXAsync_scaffold" (crossAsync {
        let testARCPath = Path.combine testObjectsFolder "testARC"
        let! arc = ARC.loadXLSXAsync testARCPath

        Expect.equal arc.Identifier "Facultative-CAM-in-Talinum" "ARC should have correct identifier"
        Expect.equal arc.ArcPath (Some testARCPath) "ARC should retain its load path"
        Expect.isTrue arc.IsSpreadsheetScaffold "ARC should retain its spreadsheet representation"
    })

    testCaseCrossAsync "WriteYMLAsync_loadYMLAsync" (crossAsync {
        let testARCPath = Path.combine testObjectsFolder "testARC"
        let! arc = ARC.loadXLSXAsync testARCPath
        let tempDir = Path.combine testResultsFolder "TestARC_explicit_yml"

        do! arc.WriteYMLAsync tempDir
        let! loadedArc = ARC.loadYMLAsync tempDir

        Expect.equal loadedArc.Identifier arc.Identifier "Identifiers should match"
        Expect.equal loadedArc.ArcPath (Some tempDir) "ARC should retain its load path"
        Expect.isFalse loadedArc.IsSpreadsheetScaffold "ARC should retain its YAML representation"
    })

    testCaseCrossAsync "loadAsync_scaffold" (crossAsync {
        let testARCPath = Path.combine testObjectsFolder "testARC"
        let! arc = ARC.loadAsync testARCPath

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

    testCaseCrossAsync "loadAsync_scaffold_updateAsyncUnchanged" (crossAsync {
        let testARCPath = Path.combine testObjectsFolder "testARC"
        let! arc = ARC.loadAsync testARCPath
        let tempDir = Path.combine testResultsFolder "TestARC_unchanged"
        do! arc.UpdateAsync tempDir
        let! arc2 = ARC.loadAsync tempDir
        Expect.equal arc2.Identifier arc.Identifier "Identifiers should match"
        Expect.equal arc2.AdditionalType arc.AdditionalType "Additional types should match"
        Expect.hasLength (arc2.Agents) (Seq.length arc.Agents) "Number of agents should match"
        Expect.hasLength (arc2.HasPart) (Seq.length arc.HasPart) "Number of parts should match"
    })

    testCaseCrossAsync "loadAsync_scaffold_updateAsyncChanged" (crossAsync {
        let testARCPath = Path.combine testObjectsFolder "testARC"
        let! arc = ARC.loadAsync testARCPath
        let previousAgentsCount = Seq.length arc.Agents
        arc.AddAgent (Agent(givenName = "My", familyName = "Dude"))
        let tempDir = Path.combine testResultsFolder "TestARC_changed"
        do! arc.UpdateAsync tempDir

        //check that xlsx has been written
        let! ymlExists = Path.fileExistsAsync (Path.combine tempDir "arc.yml") 
        Expect.isFalse ymlExists "arc.yml should not exist in scaffold"
        let! xlsxExists = Path.fileExistsAsync (Path.combine tempDir "isa.investigation.xlsx")
        Expect.isTrue xlsxExists "isa.investigation.xlsx should exist in scaffold"

        let! arc2 = ARC.loadAsync tempDir
        Expect.equal arc2.Identifier arc.Identifier "Identifiers should match"
        Expect.equal arc2.AdditionalType arc.AdditionalType "Additional types should match"
        Expect.hasLength (arc2.Agents) (previousAgentsCount + 1) "Number of agents should match"
        Expect.hasLength (arc2.HasPart) (Seq.length arc.HasPart) "Number of parts should match"
    })

    testCaseCrossAsync "loadAsync_scaffold_updateYAML" (crossAsync {
        let testARCPath = Path.combine testObjectsFolder "testARC"
        let! arc = ARC.loadAsync testARCPath
        arc.IsSpreadsheetScaffold <- false
        let tempDir = Path.combine testResultsFolder "TestARC_yml"
        do! arc.UpdateAsync tempDir

        //check that yml has been written
        let! ymlExists = Path.fileExistsAsync (Path.combine tempDir "arc.yml")
        Expect.isTrue ymlExists "arc.yml should exist in scaffold"
        let! xlsxExists = Path.fileExistsAsync (Path.combine tempDir "isa.investigation.xlsx")
        Expect.isFalse xlsxExists "isa.investigation.xlsx should not exist in scaffold"

        let! arc2 = ARC.loadAsync tempDir
        Expect.equal arc2.Identifier arc.Identifier "Identifiers should match"
        Expect.equal arc2.AdditionalType arc.AdditionalType "Additional types should match"
        Expect.hasLength (arc2.Agents) (Seq.length arc.Agents) "Number of agents should match"
        Expect.hasLength (arc2.HasPart) (Seq.length arc.HasPart) "Number of parts should match"
    })

    ]

