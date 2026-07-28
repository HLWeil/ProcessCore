module ProcessCore.Tests.WorkspaceProject

open Fable.Pyxpecto
open ProcessCore
open ProcessCore.CrossAsync
open ProcessCore.Helper
open TestingUtils

let private unwrap = function
    | Ok value -> value
    | Error error -> failwith $"Unexpected project error: {error.Kind}: {error.Message}"

let private withWorkspace name action =
    crossAsync {
        let root =
            Path.combineMany [
                "tests"
                "ProcessCore.Tests"
                "TestResults"
                "workspace-project"
                name
            ]
        do! Path.deleteFileOrDirectoryAsync root
        do! Path.ensureDirectoryAsync (Path.combine root ".arc")
        return! action root
    }

let private writeText path text =
    crossAsync {
        do! Path.ensureDirectoryOfFileAsync path
        do! Path.writeFileTextAsync path text
    }

// to-do: remove when a profile file is available on the web
#if !FABLE_COMPILER
open System.Net
open System.Net.Sockets
open System.Text

let private withHttpResponse (body: string) action =
    async {
        let listener = new TcpListener(IPAddress.Loopback, 0)
        listener.Start()
        try
            let endpoint = listener.LocalEndpoint :?> IPEndPoint
            let url = $"http://127.0.0.1:{endpoint.Port}/profile.yml"
            let serve =
                async {
                    use! client = listener.AcceptTcpClientAsync() |> Async.AwaitTask
                    use stream = client.GetStream()
                    let payload = Encoding.UTF8.GetBytes body
                    let header =
                        $"HTTP/1.1 200 OK\r\nContent-Type: application/yaml\r\nContent-Length: {payload.Length}\r\nConnection: close\r\n\r\n"
                    let response = Array.append (Encoding.ASCII.GetBytes header) payload
                    do! stream.WriteAsync(response, 0, response.Length) |> Async.AwaitTask
                }
            let! server = Async.StartChild serve
            let! result = action url
            do! server
            return result
        finally
            listener.Stop()
    }
#endif

let private projectWith codecId rules =
    $"""type: ArcWorkspaceProject
rules:
{rules codecId}
"""

let private fakeCodec id (readCount: int ref) (writeCount: int ref) =
    {
        Id = id
        ReadAsync =
            fun context input ->
                crossAsync {
                    readCount.Value <- readCount.Value + 1
                    let text = System.Text.Encoding.UTF8.GetString input.Primary
                    let parts = text.Split('|')
                    let dataset = context.CreateDataset parts.[0]
                    if parts.Length > 1 && parts.[1] <> "" then
                        dataset.AdditionalType <- Some parts.[1]
                    return Ok dataset
                }
        WriteAsync =
            fun _ dataset ->
                crossAsync {
                    writeCount.Value <- writeCount.Value + 1
                    let additionalType = dataset.AdditionalType |> Option.defaultValue ""
                    let primary =
                        System.Text.Encoding.UTF8.GetBytes $"{dataset.Identifier}|{additionalType}"
                    return Ok { Primary = primary; Files = Map.empty }
                }
    }

let private fakeRegistry readCount writeCount =
    CodecRegistry.empty
    |> CodecRegistry.add (fakeCodec "test.dataset" readCount writeCount)
    |> unwrap

let private simpleRules codecId =
    $"""  - id: root
    codec: {codecId}
    target: root
    path: root.data
  - id: studies
    codec: {codecId}
    target:
      additionalType: Study
    path: "studies/{{dataset.identifier}}/dataset.data"
"""

let private standardRules _ =
    """  - id: root
    codec: isa.investigation.xlsx
    target: root
    path: isa.investigation.xlsx
  - id: studies
    codec: isa.study.xlsx
    target:
      additionalType: Study
    path: "studies/{dataset.identifier}/isa.study.xlsx"
    files:
      - id: datamap
        path: isa.datamap.xlsx
      - id: resources-placeholder
        path: resources/.gitkeep
        create: empty
      - id: protocols-placeholder
        path: protocols/.gitkeep
        create: empty
  - id: assays
    codec: isa.assay.xlsx
    target:
      additionalType: Assay
    path: "assays/{dataset.identifier}/isa.assay.xlsx"
    files:
      - id: datamap
        path: isa.datamap.xlsx
      - id: dataset-placeholder
        path: dataset/.gitkeep
        create: empty
      - id: protocols-placeholder
        path: protocols/.gitkeep
        create: empty
  - id: workflows
    codec: isa.workflow.xlsx
    target:
      additionalType: Workflow
    path: "workflows/{dataset.identifier}/isa.workflow.xlsx"
    files:
      - id: datamap
        path: isa.datamap.xlsx
      - id: protocols-placeholder
        path: protocols/.gitkeep
        create: empty
  - id: runs
    codec: isa.run.xlsx
    target:
      additionalType: Run
    path: "runs/{dataset.identifier}/isa.run.xlsx"
    files:
      - id: datamap
        path: isa.datamap.xlsx
      - id: dataset-placeholder
        path: dataset/.gitkeep
        create: empty
      - id: protocols-placeholder
        path: protocols/.gitkeep
        create: empty
"""

let tests =
    testList "Workspace project" [

        testList "strict documents" [

            testCase "parses the three target shapes and local profiles" <| fun _ ->
                let project =
                    """type: ArcWorkspaceProject
workspaceProfiles:
  - file: profiles/isa.yml
rules:
  - id: root
    codec: test.dataset
    target: root
    path: root.data
  - id: exact
    codec: test.dataset
    target:
      identifier: special
    path: special.data
  - id: typed
    codec: test.dataset
    target:
      additionalType: Study
    path: "studies/{dataset.identifier}/dataset.data"
    files:
      - id: metadata
        path: metadata.json
      - id: placeholder
        path: dataset/.gitkeep
        create: empty
"""
                    |> ProcessCore.WorkspaceProject.parse
                    |> unwrap
                Expect.equal project.WorkspaceProfiles [ WorkspaceProfileReference.File "profiles/isa.yml" ] "profile reference"
                Expect.equal project.Rules.Length 3 "all rules are parsed"
                Expect.equal project.Rules.[0].Target Root "root target"
                Expect.equal project.Rules.[1].Target (Identifier "special") "identifier target"
                Expect.equal project.Rules.[2].Target (AdditionalType "Study") "type target"
                Expect.isEmpty project.Rules.[0].Files "legacy four-field rules remain valid"
                Expect.equal project.Rules.[2].Files.Length 2 "auxiliary files are parsed"
                Expect.equal project.Rules.[2].Files.[0].Id "metadata" "file IDs are retained"
                Expect.equal project.Rules.[2].Files.[1].Create (Some StorageFileCreation.Empty) "empty creation is parsed"

            testCase "rejects unknown and duplicate fields" <| fun _ ->
                let unknown =
                    """type: ArcWorkspaceProject
extra: no
rules: []
"""
                    |> ProcessCore.WorkspaceProject.parse
                let duplicate =
                    """type: ArcWorkspaceProject
type: ArcWorkspaceProject
rules: []
"""
                    |> ProcessCore.WorkspaceProject.parse
                Expect.isError unknown "unknown fields are rejected"
                Expect.isError duplicate "duplicate fields are rejected"

            testCase "rejects implicit non-string scalars, aliases, and partial captures" <| fun _ ->
                let numeric =
                    """type: ArcWorkspaceProfile
id: example
version: 1.0
rules:
  - id: root
    codec: test.dataset
    target: root
    path: root.data
"""
                    |> ProcessCore.WorkspaceProfile.parse
                let alias =
                    """type: ArcWorkspaceProject
rules:
  - &base
    id: root
    codec: test.dataset
    target: root
    path: root.data
  - *base
"""
                    |> ProcessCore.WorkspaceProject.parse
                let capture =
                    """type: ArcWorkspaceProject
rules:
  - id: root
    codec: test.dataset
    target: root
    path: "root-{dataset.identifier}.data"
"""
                    |> ProcessCore.WorkspaceProject.parse
                Expect.isError numeric "an unquoted numeric profile version is not a string"
                Expect.isError alias "aliases are rejected"
                Expect.isError capture "captures must occupy a whole segment"

            testCase "rejects duplicate file IDs, unsafe paths, and unknown create policies" <| fun _ ->
                let parse files =
                    $"""type: ArcWorkspaceProject
rules:
  - id: root
    codec: test.dataset
    target: root
    path: root.data
    files:
{files}
"""
                    |> ProcessCore.WorkspaceProject.parse
                let duplicate =
                    parse
                        """      - id: duplicate
        path: first.data
      - id: duplicate
        path: second.data"""
                let unsafe =
                    parse
                        """      - id: unsafe
        path: ../outside.data"""
                let create =
                    parse
                        """      - id: generated
        path: generated.data
        create: arbitrary"""
                Expect.isError duplicate "file IDs are unique within a rule"
                Expect.isError unsafe "file paths are confined and anchor-relative"
                Expect.isError create "only empty file creation is supported"
        ]

        testList "registry and resolution" [

            testCase "registry addition is immutable and rejects duplicate exact IDs" <| fun _ ->
                let reads, writes = ref 0, ref 0
                let codec = fakeCodec "test.dataset" reads writes
                let first = CodecRegistry.add codec CodecRegistry.empty |> unwrap
                Expect.isSome (CodecRegistry.tryFind codec.Id first) "the new registry contains the codec"
                Expect.isNone (CodecRegistry.tryFind codec.Id CodecRegistry.empty) "the original registry remains unchanged"
                Expect.isError (CodecRegistry.add codec first) "duplicate IDs are rejected"

            testCaseCrossAsync "loads a confined local profile" (
                withWorkspace "local-profile" <| fun root ->
                    crossAsync {
                        do!
                            writeText
                                (Path.combineMany [ root; ".arc"; "profile.yml" ])
                                """type: ArcWorkspaceProfile
id: example
version: "1.0"
rules:
  - id: root
    codec: test.dataset
    target: root
    path: root.data
"""
                        do! writeText (Path.combine root "root.data") "root|Investigation"
                        do!
                            writeText
                                (Path.combineMany [ root; ".arc"; "project.yml" ])
                                """type: ArcWorkspaceProject
workspaceProfiles:
  - file: profile.yml
"""
                        let registry = fakeRegistry (ref 0) (ref 0)
                        let! loaded = ARC.loadProjectAsync(registry, root)
                        Expect.equal (loaded |> unwrap).Identifier "root" "the file profile contributes its rule"
                    }
            )

// to-do: run in js and py when a profile file is available on the web
#if !FABLE_COMPILER
            testCaseCrossAsync "downloads and resolves a URL profile" (
                withWorkspace "url-profile" <| fun root ->
                    withHttpResponse
                        """type: ArcWorkspaceProfile
id: remote
version: "1.0"
rules:
  - id: root
    codec: test.dataset
    target: root
    path: root.data
"""
                    <| fun url ->
                        crossAsync {
                            do! writeText (Path.combine root "root.data") "root|Investigation"
                            do!
                                writeText
                                    (Path.combineMany [ root; ".arc"; "project.yml" ])
                                    $"""type: ArcWorkspaceProject
workspaceProfiles:
  - url: "{url}"
"""
                            let reads, writes = ref 0, ref 0
                            let registry = fakeRegistry reads writes
                            let! loaded = ARC.loadProjectAsync(registry, root)
                            Expect.equal (loaded |> unwrap).Identifier "root" "the downloaded profile contributes its rule"
                            Expect.equal reads.Value 1 "the resolved rule invokes the codec"
                        }
            )
#endif

            testCaseCrossAsync "reports URL download failures as anchored profile errors" (
                withWorkspace "url-profile-failure" <| fun root ->
                    crossAsync {
                        let url = "http://127.0.0.1:1/profile.yml"
                        do!
                            writeText
                                (Path.combineMany [ root; ".arc"; "project.yml" ])
                                $"""type: ArcWorkspaceProject
workspaceProfiles:
  - url: "{url}"
"""
                        let! result =
                            ARC.loadProjectAsync(fakeRegistry (ref 0) (ref 0), root)
                        match result with
                        | Error error ->
                            Expect.equal error.Kind ProjectErrorKind.Profile "download failures are profile errors"
                            Expect.equal error.Anchor (Some url) "the failing URL is preserved as the error anchor"
                            Expect.isSome error.Cause "the download exception is preserved as the cause"
                        | Ok _ -> failwith "An unavailable URL profile must fail."
                    }
            )

            testCaseCrossAsync "detects concrete collisions before codec invocation" (
                withWorkspace "collisions" <| fun root ->
                    crossAsync {
                        let reads, writes = ref 0, ref 0
                        let registry = fakeRegistry reads writes
                        do! writeText (Path.combine root "same") "same|Investigation"
                        do!
                            writeText
                                (Path.combineMany [ root; ".arc"; "project.yml" ])
                                """type: ArcWorkspaceProject
rules:
  - id: root
    codec: test.dataset
    target: root
    path: same
  - id: exact
    codec: test.dataset
    target:
      identifier: same
    path: "{dataset.identifier}"
"""
                        let! result = ARC.loadProjectAsync(registry, root)
                        match result with
                        | Error error -> Expect.equal error.Kind ProjectErrorKind.Path "collision is a path error"
                        | Ok _ -> failwith "Colliding bindings must fail."
                        Expect.equal reads.Value 0 "preflight must complete before codec access"
                    }
            )

            testCaseCrossAsync "includes auxiliary resources in collision preflight" (
                withWorkspace "auxiliary-collisions" <| fun root ->
                    crossAsync {
                        let reads, writes = ref 0, ref 0
                        let registry = fakeRegistry reads writes
                        do! writeText (Path.combine root "root.data") "root|Investigation"
                        do! writeText (Path.combine root "child.data") "child|Study"
                        do!
                            writeText
                                (Path.combineMany [ root; ".arc"; "project.yml" ])
                                """type: ArcWorkspaceProject
rules:
  - id: root
    codec: test.dataset
    target: root
    path: root.data
    files:
      - id: child-copy
        path: child.data
  - id: exact
    codec: test.dataset
    target:
      identifier: child
    path: child.data
"""
                        let! result = ARC.loadProjectAsync(registry, root)
                        match result with
                        | Error error -> Expect.equal error.Kind ProjectErrorKind.Path "auxiliary collisions are path errors"
                        | Ok _ -> failwith "An auxiliary file must not collide with an anchor."
                        Expect.equal reads.Value 0 "collision preflight runs before codec access"
                    }
            )

            testCaseCrossAsync "converts synchronous codec exceptions into structured failures" (
                withWorkspace "codec-exception" <| fun root ->
                    crossAsync {
                        let throwingCodec = {
                            Id = "test.throwing"
                            ReadAsync = fun _ _ -> failwith "synchronous codec failure"
                            WriteAsync = fun _ _ -> failwith "synchronous codec failure"
                        }
                        let registry = CodecRegistry.add throwingCodec CodecRegistry.empty |> unwrap
                        do! writeText (Path.combine root "root.data") "unused"
                        do!
                            writeText
                                (Path.combineMany [ root; ".arc"; "project.yml" ])
                                """type: ArcWorkspaceProject
rules:
  - id: root
    codec: test.throwing
    target: root
    path: root.data
"""
                        let! result = ARC.loadProjectAsync(registry, root)
                        match result with
                        | Error error ->
                            Expect.equal error.Kind ProjectErrorKind.Codec "the exception becomes a codec failure"
                            Expect.isSome error.Cause "the original cause is retained"
                        | Ok _ -> failwith "A throwing codec must fail the operation."
                    }
            )
        ]

        testList "execution and facade" [

            testCaseCrossAsync "custom codecs round-trip direct children without deleting stale files" (
                withWorkspace "custom-round-trip" <| fun root ->
                    crossAsync {
                        let reads, writes = ref 0, ref 0
                        let registry = fakeRegistry reads writes
                        do! writeText (Path.combineMany [ root; ".arc"; "project.yml" ]) (projectWith "test.dataset" simpleRules)
                        do! writeText (Path.combine root "stale.data") "keep"
                        let arc = ARC("root", additionalType = "Investigation")
                        let study = Dataset("study-1", additionalType = "Study")
                        study.AddPart(Dataset("nested"))
                        arc.AddPart study

                        let! written = arc.WriteProjectAsync(registry, root)
                        written |> unwrap
                        let! staleExists = Path.fileExistsAsync (Path.combine root "stale.data")
                        Expect.isTrue staleExists "project writes do not delete stale resources"
                        let! loaded = ARC.loadProjectAsync(registry, root)
                        let loaded = loaded |> unwrap
                        Expect.equal loaded.Identifier "root" "root identifier round-trips"
                        Expect.equal loaded.HasPart.Count 1 "the selected child is attached directly"
                        Expect.equal loaded.HasPart.[0].Identifier "study-1" "captured identifier round-trips"
                        Expect.equal loaded.HasPart.[0].PartOf (Some (loaded :> Dataset)) "AddPart establishes parentage"
                        Expect.equal writes.Value 2 "root and child are written once"
                        Expect.equal reads.Value 2 "root and child are read once"
                    }
            )

            testCaseCrossAsync "filesystem handling supplies named files and creates empty files" (
                withWorkspace "declared-files" <| fun root ->
                    crossAsync {
                        let seenFiles = ref Map.empty
                        let codec = {
                            Id = "test.resources"
                            ReadAsync =
                                fun context input ->
                                    crossAsync {
                                        seenFiles.Value <- input.Files
                                        let identifier = System.Text.Encoding.UTF8.GetString input.Primary
                                        return Ok(context.CreateDataset identifier)
                                    }
                            WriteAsync =
                                fun _ dataset ->
                                    crossAsync {
                                        return
                                            Ok {
                                                Primary = System.Text.Encoding.UTF8.GetBytes dataset.Identifier
                                                Files =
                                                    Map.ofList [
                                                        "metadata", System.Text.Encoding.UTF8.GetBytes "declared"
                                                    ]
                                            }
                                    }
                        }
                        let registry = CodecRegistry.add codec CodecRegistry.empty |> unwrap
                        do!
                            writeText
                                (Path.combineMany [ root; ".arc"; "project.yml" ])
                                """type: ArcWorkspaceProject
rules:
  - id: root
    codec: test.resources
    target: root
    path: relocated/root.data
    files:
      - id: metadata
        path: metadata/info.txt
      - id: placeholder
        path: dataset/.gitkeep
        create: empty
"""
                        let! written = ARC("root").WriteProjectAsync(registry, root)
                        written |> unwrap
                        let metadataPath = Path.combineMany [ root; "relocated"; "metadata"; "info.txt" ]
                        let placeholderPath = Path.combineMany [ root; "relocated"; "dataset"; ".gitkeep" ]
                        let! metadata = Path.readFileTextAsync metadataPath
                        let! placeholder = Path.readFileBinaryAsync placeholderPath
                        Expect.equal metadata "declared" "codec output is written to its declared path"
                        Expect.isEmpty placeholder "project-managed files are zero-byte files"

                        let! loaded = ARC.loadProjectAsync(registry, root)
                        Expect.equal (loaded |> unwrap).Identifier "root" "the primary resource is decoded"
                        Expect.isTrue (Map.containsKey "metadata" seenFiles.Value) "existing codec files are supplied by ID"
                        Expect.isTrue (Map.containsKey "placeholder" seenFiles.Value) "existing managed files are supplied by ID"
                    }
            )

            testCaseCrossAsync "rejects undeclared codec output before writing resources" (
                withWorkspace "undeclared-output" <| fun root ->
                    crossAsync {
                        let codec = {
                            Id = "test.undeclared"
                            ReadAsync = fun context _ -> crossAsync { return Ok(context.CreateDataset "root") }
                            WriteAsync =
                                fun _ _ ->
                                    crossAsync {
                                        return
                                            Ok {
                                                Primary = System.Text.Encoding.UTF8.GetBytes "root"
                                                Files =
                                                    Map.ofList [
                                                        "surprise", System.Text.Encoding.UTF8.GetBytes "not declared"
                                                    ]
                                            }
                                    }
                        }
                        let registry = CodecRegistry.add codec CodecRegistry.empty |> unwrap
                        do!
                            writeText
                                (Path.combineMany [ root; ".arc"; "project.yml" ])
                                """type: ArcWorkspaceProject
rules:
  - id: root
    codec: test.undeclared
    target: root
    path: root.data
"""
                        let! result = ARC("root").WriteProjectAsync(registry, root)
                        match result with
                        | Error error -> Expect.equal error.Kind ProjectErrorKind.Resource "undeclared output is a resource error"
                        | Ok () -> failwith "Undeclared codec output must fail."
                        let! primaryExists = Path.fileExistsAsync (Path.combine root "root.data")
                        Expect.isFalse primaryExists "output validation happens before the primary write"
                    }
            )

            testCaseCrossAsync "anchor-relative files follow exact-target relocation" (
                withWorkspace "relocated-exact" <| fun root ->
                    crossAsync {
                        let registry = fakeRegistry (ref 0) (ref 0)
                        do!
                            writeText
                                (Path.combineMany [ root; ".arc"; "project.yml" ])
                                """type: ArcWorkspaceProject
rules:
  - id: root
    codec: test.dataset
    target: root
    path: root.data
  - id: relocated
    codec: test.dataset
    target:
      identifier: assay
    path: special/location/assay.data
    files:
      - id: dataset-placeholder
        path: dataset/.gitkeep
        create: empty
"""
                        let arc = ARC("root")
                        arc.AddPart(Dataset("assay", additionalType = "Assay"))
                        let! written = arc.WriteProjectAsync(registry, root)
                        written |> unwrap
                        let marker = Path.combineMany [ root; "special"; "location"; "dataset"; ".gitkeep" ]
                        let! markerExists = Path.fileExistsAsync marker
                        Expect.isTrue markerExists "the auxiliary path is relative to the relocated anchor"
                    }
            )

            testCaseCrossAsync "generic project failures do not fall back, while explicit YAML bypasses the project" (
                withWorkspace "facade-authority" <| fun root ->
                    crossAsync {
                        let yamlArc = ARC("yaml-fallback")
                        do! yamlArc.WriteYMLAsync root
                        do!
                            writeText
                                (Path.combineMany [ root; ".arc"; "project.yml" ])
                                """type: ArcWorkspaceProject
rules:
  - id: root
    codec: missing.codec
    target: root
    path: missing.data
"""
                        let! genericResult =
                            ARC.loadAsync root
                            |> CrossAsync.map Ok
                            |> CrossAsync.catchWith (fun ex -> Error ex)
                        match genericResult with
                        | Error (ProjectException error) ->
                            Expect.equal error.Kind ProjectErrorKind.Codec "the project codec failure is preserved"
                        | Error ex -> return raise ex
                        | Ok _ -> failwith "Generic load must not fall back to arc.yml."

                        let! explicitYaml = ARC.loadYMLAsync root
                        Expect.equal explicitYaml.Identifier "yaml-fallback" "explicit YAML loading bypasses project discovery"
                    }
            )

            testCaseCrossAsync "standard registry round-trips all five ISA workbooks and an adjacent Datamap" (
                withWorkspace "standard-round-trip" <| fun root ->
                    crossAsync {
                        do! writeText (Path.combineMany [ root; ".arc"; "project.yml" ]) (projectWith "" standardRules)
                        let arc = ARC("investigation", additionalType = "Investigation")
                        let study = Dataset("study", additionalType = "Study")
                        study.AddDataContext(DataContext(Data("data/result.csv")))
                        arc.AddPart study
                        arc.AddPart(Dataset("assay", additionalType = "Assay"))
                        arc.AddPart(Dataset("workflow", additionalType = "Workflow"))
                        arc.AddPart(Dataset("run", additionalType = "Run"))

                        let! written = arc.WriteProjectAsync(CodecRegistry.standard, root)
                        written |> unwrap
                        let! loaded = ARC.loadProjectAsync(CodecRegistry.standard, root)
                        let loaded = loaded |> unwrap
                        let children =
                            loaded.HasPart
                            |> Seq.map (fun dataset -> dataset.Identifier, dataset.AdditionalType)
                            |> Set.ofSeq
                        Expect.equal loaded.Identifier "investigation" "investigation codec round-trips"
                        Expect.equal children.Count 4 "all four child codec kinds round-trip"
                        Expect.isTrue (children.Contains("study", Some "Study")) "study codec"
                        Expect.isTrue (children.Contains("assay", Some "Assay")) "assay codec"
                        Expect.isTrue (children.Contains("workflow", Some "Workflow")) "workflow codec"
                        Expect.isTrue (children.Contains("run", Some "Run")) "run codec"
                        let! datamapExists =
                            Path.fileExistsAsync (
                                Path.combineMany [ root; "studies"; "study"; "isa.datamap.xlsx" ]
                            )
                        Expect.isTrue datamapExists "the study codec owns its adjacent Datamap"
                        let! assayDatamapExists =
                            Path.fileExistsAsync (
                                Path.combineMany [ root; "assays"; "assay"; "isa.datamap.xlsx" ]
                            )
                        Expect.isFalse assayDatamapExists "a Datamap is omitted when the Dataset has no data contexts"
                        let assayMarker =
                            Path.combineMany [ root; "assays"; "assay"; "dataset"; ".gitkeep" ]
                        let! assayMarkerExists = Path.fileExistsAsync assayMarker
                        Expect.isTrue assayMarkerExists "the assay dataset marker is always created"
                        let expectedMarkers =
                            [
                                [ "studies"; "study"; "resources"; ".gitkeep" ]
                                [ "studies"; "study"; "protocols"; ".gitkeep" ]
                                [ "assays"; "assay"; "protocols"; ".gitkeep" ]
                                [ "workflows"; "workflow"; "protocols"; ".gitkeep" ]
                                [ "runs"; "run"; "dataset"; ".gitkeep" ]
                                [ "runs"; "run"; "protocols"; ".gitkeep" ]
                            ]
                        for relative in expectedMarkers do
                            let! exists = Path.fileExistsAsync (Path.combineMany (root :: relative))
                            let displayPath = String.concat "/" relative
                            Expect.isTrue exists $"standard placeholder '{displayPath}' is created"
                        let loadedStudy = loaded.HasPart |> Seq.find (fun dataset -> dataset.Identifier = "study")
                        Expect.equal loadedStudy.DataContexts.Count 1 "the adjacent Datamap is read back"
                    }
            )
        ]
    ]
