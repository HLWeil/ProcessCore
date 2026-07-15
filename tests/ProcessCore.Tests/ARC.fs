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

    testList "Unsorted object store characterization" [

        testCase "shared YAML layer decodes ARC samples as concrete Samples" <| fun _ ->
            let yaml =
                """type: Dataset
identifier: arc-store
samples:
  - type: Sample
    name: orphan-sample
"""
            let arc = ARC.fromYamlString yaml
            Expect.equal arc.Samples.Count 1 "one stored Sample should be decoded"
            Expect.equal arc.Samples.[0].Name "orphan-sample" "the stored value is a concrete Sample"

        testCase "existing YAML layer canonicalizes data files with process endpoints" <| fun _ ->
            let arc = ARC("arc-store")
            let stored = Data("data/orphan.csv")
            arc.AddDataFile(stored)
            let proc = Process("consume")
            arc.AddProcess(proc)
            proc.SetInputData(Data("data/orphan.csv"))
            let attached = proc.InputData() |> Option.get
            Expect.isTrue (obj.ReferenceEquals(stored, attached))
                "A stored Data and equal process endpoint should share one instance"

        testCase "shared YAML layer shares indexed recipes with processes" <| fun _ ->
            let yaml =
                """type: Dataset
identifier: arc-store
recipes:
  - "@id": "#Recipe_measurement"
    type: Recipe
    name: measurement
    version: "1"
processes:
  - type: Process
    name: run
    executesRecipe:
      "@id": "#Recipe_measurement"
"""
            let arc = ARC.fromYamlString yaml
            let stored = arc.Recipes |> Seq.tryExactlyOne
            let attached = arc.Processes.[0].ExecutesRecipe
            Expect.isSome stored "ARC recipes should be accessible as typed Recipes"
            Expect.isTrue (obj.ReferenceEquals(stored.Value, attached.Value))
                "An indexed Recipe and process recipe link should share one instance"

        testCase "legacy protocol-named YAML links decode but re-encode as recipe links" <| fun _ ->
            let legacyYaml =
                """type: Dataset
identifier: arc-store
labProtocols:
  - "@id": "#legacy-recipe"
    type: Recipe
    name: measurement
processes:
  - type: Process
    name: run
    executesProtocol:
      "@id": "#legacy-recipe"
"""
            let arc = ARC.fromYamlString legacyYaml
            Expect.equal arc.Recipes.Count 1 "the legacy top-level index still decodes"
            Expect.isSome arc.Processes.[0].ExecutesRecipe "the legacy process link still resolves"
            Expect.isTrue (obj.ReferenceEquals(arc.Recipes.[0], arc.Processes.[0].ExecutesRecipe.Value))
                "legacy links still use the canonical Recipe"

            let canonicalYaml = arc.toYamlString(2)
            Expect.isTrue (canonicalYaml.Contains("recipes:")) "canonical output uses recipes"
            Expect.isTrue (canonicalYaml.Contains("executesRecipe:")) "canonical output uses executesRecipe"
            Expect.isFalse (canonicalYaml.Contains("labProtocols:")) "canonical output omits the legacy index name"
            Expect.isFalse (canonicalYaml.Contains("executesProtocol:")) "canonical output omits the legacy link name"

        testCase "shared YAML layer does not emit ARC runtime or node back-edge fields" <| fun _ ->
            let arc = ARC("arc-store")
            arc.ArcPath <- Some "C:/runtime-only"
            arc.AddSample(Sample("orphan-sample"))
            let yaml = arc.toYamlString(2)
            Expect.isFalse (yaml.Contains("ArcPath")) "ArcPath is runtime-only"
            Expect.isFalse (yaml.Contains("IsSpreadsheetScaffold")) "representation state is runtime-only"
            Expect.isFalse (yaml.Contains("InputOf")) "Sample back-edges are runtime-only"
            Expect.isFalse (yaml.Contains("OutputOf")) "Sample back-edges are runtime-only"
    ]

    testList "Unsorted object store API" [

        testCase "constructor and Add methods deduplicate equal stored objects" <| fun _ ->
            let sample = Sample("sample-1")
            let recipe = Recipe("measurement", version = "1")
            let arc =
                ARC(
                    "arc-store",
                    samples = [ sample; Sample("sample-1") ],
                    recipes = [ recipe; Recipe("measurement", version = "1") ])
            Expect.equal arc.Samples.Count 1 "equal Samples are stored once"
            Expect.equal arc.Recipes.Count 1 "equal Recipes are stored once"
            Expect.isTrue (obj.ReferenceEquals(sample, arc.Samples.[0])) "the first Sample is canonical"
            Expect.isTrue (obj.ReferenceEquals(recipe, arc.Recipes.[0])) "the first Recipe is canonical"

        testCase "store-before-process reuses Sample Data and Recipe instances" <| fun _ ->
            let arc = ARC("arc-store")
            let sample = Sample("sample-1")
            let data = Data("data/result.csv")
            let recipe = Recipe("measurement", version = "1")
            arc.AddSample(sample)
            arc.AddDataFile(data)
            arc.AddRecipe(recipe)
            let proc = Process("run")
            arc.AddProcess(proc)
            proc.SetInputSample(Sample("sample-1"))
            proc.SetOutputData(Data("data/result.csv"))
            proc.ExecutesRecipe <- Some (Recipe("measurement", version = "1"))
            Expect.isTrue (obj.ReferenceEquals(sample, proc.InputSample().Value)) "stored Sample is reused"
            Expect.isTrue (obj.ReferenceEquals(data, proc.OutputData().Value)) "stored Data is reused"
            Expect.isTrue (obj.ReferenceEquals(recipe, proc.ExecutesRecipe.Value)) "stored Recipe is reused"

        testCase "process-before-store adds canonical process objects to ARC stores" <| fun _ ->
            let arc = ARC("arc-store")
            let sample = Sample("sample-1")
            let data = Data("data/result.csv")
            let recipe = Recipe("measurement", version = "1")
            let proc = Process("run", executesRecipe = recipe)
            proc.SetInputSample(sample)
            proc.SetOutputData(data)
            arc.AddProcess(proc)
            arc.AddSample(Sample("sample-1"))
            arc.AddDataFile(Data("data/result.csv"))
            arc.AddRecipe(Recipe("measurement", version = "1"))
            Expect.isTrue (obj.ReferenceEquals(sample, arc.Samples.[0])) "process Sample enters the store"
            Expect.isTrue (obj.ReferenceEquals(data, arc.DataFiles.[0])) "process Data enters DataFiles"
            Expect.isTrue (obj.ReferenceEquals(recipe, arc.Recipes.[0])) "process Recipe enters the store"

        testCase "removing from stores does not detach a process and final removal evicts identity" <| fun _ ->
            let arc = ARC("arc-store")
            let sample = Sample("sample-1")
            let recipe = Recipe("measurement", version = "1")
            arc.AddSample(sample)
            arc.AddRecipe(recipe)
            let proc = Process("run", executesRecipe = Recipe("measurement", version = "1"))
            proc.SetInputSample(Sample("sample-1"))
            arc.AddProcess(proc)
            arc.RemoveSample(sample)
            arc.RemoveRecipe(recipe)
            Expect.equal arc.Samples.Count 0 "Sample is explicitly removed from the store"
            Expect.equal arc.Recipes.Count 0 "Recipe is explicitly removed from the store"
            Expect.isTrue (obj.ReferenceEquals(sample, proc.InputSample().Value)) "process keeps the Sample"
            Expect.isTrue (obj.ReferenceEquals(recipe, proc.ExecutesRecipe.Value)) "process keeps the Recipe"
            arc.RemoveProcess(proc)
            let replacement = Process("replacement", executesRecipe = Recipe("measurement", version = "1"))
            replacement.SetInputSample(Sample("sample-1"))
            arc.AddProcess(replacement)
            Expect.isFalse (obj.ReferenceEquals(sample, replacement.InputSample().Value)) "unused Sample was evicted"
            Expect.isFalse (obj.ReferenceEquals(recipe, replacement.ExecutesRecipe.Value)) "unused Recipe was evicted"

        testCase "adding a child canonicalizes its stored and linked objects against ARC" <| fun _ ->
            let arc = ARC("arc-store")
            let sample = Sample("sample-1")
            let data = Data("data/result.csv")
            let recipe = Recipe("measurement", version = "1")
            arc.AddSample(sample)
            arc.AddDataFile(data)
            arc.AddRecipe(recipe)
            let child = Dataset("child")
            child.AddDataFile(Data("data/result.csv"))
            let proc = Process("run", executesRecipe = Recipe("measurement", version = "1"))
            proc.SetInputSample(Sample("sample-1"))
            child.AddProcess(proc)
            arc.AddPart(child)
            Expect.isTrue (obj.ReferenceEquals(sample, proc.InputSample().Value)) "child Sample is canonical"
            Expect.isTrue (obj.ReferenceEquals(data, child.DataFiles.[0])) "child DataFile is canonical"
            Expect.isTrue (obj.ReferenceEquals(recipe, proc.ExecutesRecipe.Value)) "child Recipe is canonical"

        testCase "replacing a recipe respects store pinning and final-reference eviction" <| fun _ ->
            let arc = ARC("arc-store")
            let first = Recipe("measurement", version = "1")
            let second = Recipe("normalization", version = "1")
            arc.AddRecipe(first)
            let proc = Process("run", executesRecipe = Recipe("measurement", version = "1"))
            arc.AddProcess(proc)
            proc.ExecutesRecipe <- Some second
            Expect.isTrue (obj.ReferenceEquals(first, arc.Recipes.[0]))
                "replacing a process recipe does not remove a pinned recipe"
            arc.RemoveRecipe(first)
            let later = Process("later", executesRecipe = Recipe("measurement", version = "1"))
            arc.AddProcess(later)
            Expect.isFalse (obj.ReferenceEquals(first, later.ExecutesRecipe.Value))
                "the replaced and unpinned recipe is evicted"

        testCase "detached children rebuild independent data and recipe registries" <| fun _ ->
            let arc = ARC("arc-store")
            let parentData = Data("data/result.csv")
            let parentRecipe = Recipe("measurement", version = "1")
            arc.AddDataFile(parentData)
            arc.AddRecipe(parentRecipe)
            let child = Dataset("child")
            child.AddDataFile(Data("data/result.csv"))
            let original = Process("run", executesRecipe = Recipe("measurement", version = "1"))
            child.AddProcess(original)
            arc.AddPart(child)
            arc.RemovePart(child)

            let later = Process("later", executesRecipe = Recipe("measurement", version = "1"))
            later.SetOutputData(Data("data/result.csv"))
            child.AddProcess(later)
            Expect.isTrue (obj.ReferenceEquals(child.DataFiles.[0], later.OutputData().Value))
                "the detached data store seeds the new child registry"
            Expect.isTrue (obj.ReferenceEquals(original.ExecutesRecipe.Value, later.ExecutesRecipe.Value))
                "the detached process recipe seeds the new child recipe registry"
    ]

    testList "Unsorted object store YAML" [

        testCase "objects stored only on ARC round-trip without processes" <| fun _ ->
            let sample =
                Sample(
                    "orphan-sample",
                    additionalType = "Sample",
                    additionalProperty = [ Annotation("organism", value = "plant") ])
            let data =
                Data(
                    "data/orphan.csv",
                    selector = "#col=2",
                    selectorFormat = "https://www.rfc-editor.org/rfc/rfc7111")
            let recipe = Recipe("orphan-recipe", version = "2")
            let arc = ARC("arc-store", samples = [ sample ], dataFiles = [ data ], recipes = [ recipe ])

            let yaml = arc.toYamlString(2)
            let decoded = ARC.fromYamlString yaml

            Expect.equal decoded.Processes.Count 0 "the ARC has no processes before or after the round-trip"
            Expect.equal decoded.Samples.Count 1 "the ARC-only Sample survives"
            Expect.equal decoded.DataFiles.Count 1 "the ARC-only Data survives"
            Expect.equal decoded.Recipes.Count 1 "the ARC-only Recipe survives"
            Expect.equal decoded.Samples.[0].Name "orphan-sample" "the Sample decodes as a concrete Sample"
            Expect.equal decoded.Samples.[0].AdditionalType (Some "Sample") "Sample fields survive"
            Expect.equal decoded.DataFiles.[0].Path "data/orphan.csv" "the Data decodes as concrete Data"
            Expect.equal decoded.DataFiles.[0].Selector (Some "#col=2") "Data identity fields survive"
            Expect.equal decoded.Recipes.[0].Name (Some "orphan-recipe") "the Recipe decodes as a concrete Recipe"
            Expect.equal decoded.Recipes.[0].Version (Some "2") "Recipe identity fields survive"
            Expect.isTrue (yaml.Contains("samples:")) "the Sample uses the typed samples field"
            Expect.isTrue (yaml.Contains("dataFiles:")) "the Data uses the typed dataFiles field"
            Expect.isTrue (yaml.Contains("recipes:")) "the Recipe uses the typed recipes field"
            Expect.isFalse (yaml.Contains("labProtocols:")) "the legacy recipe index is not emitted"
            Expect.isFalse (yaml.Contains("executesProtocol:")) "the legacy process link is not emitted"

        testCase "stored and linked objects round-trip with identity and without duplicate recipe versions" <| fun _ ->
            let arc = ARC("arc-store")
            let sample = Sample("sample-1", additionalProperty = [ Annotation("organism", value = "plant") ])
            let data = Data("data/result.csv")
            let recipeV1 = Recipe("measurement", version = "1")
            let recipeV2 = Recipe("measurement", version = "2")
            arc.AddSample(sample)
            arc.AddDataFile(data)
            arc.AddRecipe(recipeV1)
            arc.AddRecipe(recipeV2)
            let proc = Process("run", executesRecipe = Recipe("measurement", version = "1"))
            proc.SetInputSample(Sample("sample-1"))
            proc.SetOutputData(Data("data/result.csv"))
            arc.AddProcess(proc)
            arc.SetProperty("customMeta", "preserved")

            let yaml = arc.toYamlString(2)
            let decoded = ARC.fromYamlString yaml

            Expect.equal decoded.Samples.Count 1 "stored Sample survives"
            Expect.equal decoded.DataFiles.Count 1 "stored Data survives"
            Expect.equal decoded.Recipes.Count 2 "distinct Recipe versions survive"
            Expect.isTrue (obj.ReferenceEquals(decoded.Samples.[0], decoded.Processes.[0].InputSample().Value))
                "decoded Sample store and endpoint share identity"
            Expect.isTrue (obj.ReferenceEquals(decoded.DataFiles.[0], decoded.Processes.[0].OutputData().Value))
                "decoded DataFile and endpoint share identity"
            let decodedV1 = decoded.Recipes |> Seq.find (fun recipe -> recipe.Version = Some "1")
            Expect.isTrue (obj.ReferenceEquals(decodedV1, decoded.Processes.[0].ExecutesRecipe.Value))
                "decoded Recipe store and process share identity"
            Expect.equal (decoded.TryGetPropertyValue("customMeta") |> Option.map string) (Some "preserved")
                "unknown ARC metadata survives"
            Expect.isTrue (yaml.Contains("#Recipe_measurement_version_1")) "version 1 has a distinct id"
            Expect.isTrue (yaml.Contains("#Recipe_measurement_version_2")) "version 2 has a distinct id"

        testCase "explicit recipe ids are preserved" <| fun _ ->
            let recipe = Recipe("measurement", version = "1")
            recipe.SetProperty("@id", "https://example.org/protocols/measurement-v1")
            let arc = ARC("arc-store", recipes = [ recipe ])
            let yaml = arc.toYamlString(2)
            Expect.isTrue (yaml.Contains("https://example.org/protocols/measurement-v1"))
                "an explicit @id wins over generated identity"
    ]

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
        arc.AddSample(Sample("staged-sample"))
        arc.AddDataFile(Data("data/staged.csv"))
        arc.AddRecipe(Recipe("staged-recipe", version = "1"))

        do! arc.WriteYMLAsync tempDir
        let! loadedArc = ARC.loadYMLAsync tempDir

        Expect.equal loadedArc.Identifier arc.Identifier "Identifiers should match"
        Expect.equal loadedArc.ArcPath (Some tempDir) "ARC should retain its load path"
        Expect.isFalse loadedArc.IsSpreadsheetScaffold "ARC should retain its YAML representation"
        Expect.equal loadedArc.Samples.Count 1 "stored Sample survives file IO"
        Expect.isTrue (loadedArc.DataFiles |> Seq.exists (fun data -> data.Path = "data/staged.csv"))
            "stored Data survives file IO"
        Expect.isTrue
            (loadedArc.Recipes
             |> Seq.exists (fun recipe -> recipe.Name = Some "staged-recipe" && recipe.Version = Some "1"))
            "stored Recipe survives file IO alongside scaffold recipes"
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

