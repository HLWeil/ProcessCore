namespace ProcessCore

open Fable.Core
open ProcessCore.Helper
open CrossAsync

// (identifier: string, ?title: string, ?description: string, ?additionalType: string, ?license: string, ?datePublished: string, ?dateCreated: string, ?dateModified: string, ?processes: seq<Process>, ?hasPart: seq<Dataset>, ?dataFiles: seq<Data>, ?agents: seq<Agent>, ?citations: seq<ScholarlyArticle>, ?dataContexts: seq<DataContext>, ?additionalProperty: seq<Annotation>, ?samples: seq<Sample>, ?recipes: seq<Recipe>)

[<AttachMembers>]
type ARC(identifier: string, ?title: string, ?description: string, ?additionalType: string, ?license: string, ?datePublished: string, ?dateCreated: string, ?dateModified: string, ?processes: seq<Process>, ?hasPart: seq<Dataset>, ?dataFiles: seq<Data>, ?agents: seq<Agent>, ?citations: seq<ScholarlyArticle>, ?dataContexts: seq<DataContext>, ?additionalProperty: seq<Annotation>, ?samples: seq<Sample>, ?recipes: seq<Recipe>) as this =

    inherit Dataset(identifier, ?title=title, ?description=description, ?additionalType=additionalType, ?license=license, ?datePublished=datePublished, ?dateCreated=dateCreated, ?dateModified=dateModified, ?processes=processes, ?hasPart=hasPart, ?dataFiles=dataFiles, ?agents=agents, ?citations=citations, ?dataContexts=dataContexts, ?additionalProperty=additionalProperty)
    
    let mutable _arcPath : string option = None
    let mutable _isSpreadsheetScaffold : bool = false
    let _samples = ResizeArray<Sample>()
    let _recipes = ResizeArray<Recipe>()

    do
        samples |> Option.iter (fun values -> for sample in values do this.AddSample(sample))
        recipes |> Option.iter (fun values -> for recipe in values do this.AddRecipe(recipe))

    member _.Samples = _samples

    member _.Recipes = _recipes

    member internal this.StoreSample(sample: Sample) : Sample =
        let canonical =
            match this.PinNode(SampleNode sample) with
            | SampleNode value -> value
            | DataNode _ -> failwith "A Sample identity key resolved to Data."
        if not (_samples |> Seq.exists (fun current -> current = canonical)) then
            _samples.Add(canonical)
        canonical

    member this.AddSample(sample: Sample) =
        this.StoreSample(sample) |> ignore

    member this.RemoveSample(sample: Sample) =
        match _samples |> Seq.tryFind (fun current -> current = sample) with
        | Some stored ->
            _samples.Remove(stored) |> ignore
            this.UnpinNode(SampleNode stored)
        | None -> ()

    member internal this.StoreRecipe(recipe: Recipe) : Recipe =
        let canonical = this.PinRecipe(recipe)
        if not (_recipes |> Seq.exists (fun current -> current = canonical)) then
            _recipes.Add(canonical)
        canonical

    member this.AddRecipe(recipe: Recipe) =
        this.StoreRecipe(recipe) |> ignore

    member this.RemoveRecipe(recipe: Recipe) =
        match _recipes |> Seq.tryFind (fun current -> current = recipe) with
        | Some stored ->
            _recipes.Remove(stored) |> ignore
            this.UnpinRecipe(stored)
        | None -> ()


    member this.toYamlString(?whiteSpace: int) : string =
        ProcessCore.Yaml.Dataset.toYamlStringIndexedWithStores whiteSpace this.Samples this.Recipes this

    static member fromYamlString(yamlString: string) : ARC =
        YAMLicious.Reader.read yamlString 
        |> ProcessCore.Yaml.Dataset.decoderGenericWithStores
            (fun i -> ARC(i))
            (fun arc sample -> arc.StoreSample(sample))
            (fun arc recipe -> arc.StoreRecipe(recipe))
            false


    member this.ArcPath
        with get() = _arcPath
        and set(value) = _arcPath <- value

    member this.IsSpreadsheetScaffold
        with get() = _isSpreadsheetScaffold
        and set(value) = _isSpreadsheetScaffold <- value

    /// Writes the ARC to the specified path as arc.yml and makes YAML the active representation.
    member this.WriteYMLAsync(arcPath : string) : CrossAsync<unit> =
        crossAsync {
            do! Path.ensureDirectoryAsync arcPath
            _isSpreadsheetScaffold <- false
            _arcPath <- Some arcPath
            let p = Path.combine arcPath "arc.yml"
            let ymlString = this.toYamlString(2)
            do! Path.writeFileTextAsync p ymlString
        }

    /// Writes the ARC to the specified path as a spreadsheet scaffold and makes XLSX the active representation.
    member this.WriteXLSXAsync(arcPath : string) : CrossAsync<unit> =
        crossAsync {
            do! Path.ensureDirectoryAsync arcPath
            _isSpreadsheetScaffold <- true
            _arcPath <- Some arcPath
            do! ScaffoldReader.ARC.writeAsync arcPath this
        }

    /// Writes the ARC using .arc/project.yml when present; otherwise writes arc.yml.
    member this.WriteAsync(arcPath : string) : CrossAsync<unit> =
        let projectPath = Path.combineMany [ arcPath; Path.ARCConfigFolderName; "project.yml" ]
        crossAsync {
            let! projectExists = Path.fileExistsAsync projectPath
            if projectExists then
                let! result = ProjectRuntime.writeAsync CodecRegistry.standard arcPath this
                match result with
                | Ok () ->
                    _arcPath <- Some arcPath
                    _isSpreadsheetScaffold <- false
                | Error error -> return raise (ProjectException error)
            else
                return! this.WriteYMLAsync arcPath
        }

    /// Updates the ARC at the specified path. A destination project file is authoritative and is re-resolved on every update.
    ///
    /// If no path is provided, it will use the ArcPath property. If neither is set, it will throw an exception.
    member this.UpdateAsync(?arcPath : string) : CrossAsync<unit> = 
        crossAsync {
            let arcPath = 
                match arcPath with
                | Some p -> 
                    _arcPath <- Some p; 
                    p
                | None -> 
                    match this.ArcPath with
                    | Some p -> p
                    | None -> failwith "ARC path is not set. Please provide an arcPath or set the ArcPath property."
            do! Path.ensureDirectoryAsync arcPath
            let projectPath = Path.combineMany [ arcPath; Path.ARCConfigFolderName; "project.yml" ]
            let! projectExists = Path.fileExistsAsync projectPath
            if projectExists then
                let! result = ProjectRuntime.writeAsync CodecRegistry.standard arcPath this
                match result with
                | Ok () ->
                    _arcPath <- Some arcPath
                    _isSpreadsheetScaffold <- false
                | Error error -> return raise (ProjectException error)
            elif _isSpreadsheetScaffold then
                return! this.WriteXLSXAsync arcPath
            else
                return! this.WriteYMLAsync arcPath
        }

    /// Loads an ARC from arc.yml in the specified path.
    static member loadYMLAsync(arcPath : string) : CrossAsync<ARC> =
        let p = Path.combine arcPath "arc.yml"
        crossAsync {
            try
                let! ymlString = Path.readFileTextAsync p
                let arc = ARC.fromYamlString ymlString
                arc.ArcPath <- Some arcPath
                arc.IsSpreadsheetScaffold <- false
                return arc
            with
            | ex -> return failwith $"Failed to load ARC from yml at {p}: {ex.Message}"
        }

    /// Loads an ARC from the spreadsheet scaffold in the specified path.
    static member loadXLSXAsync(arcPath : string) : CrossAsync<ARC> =
        crossAsync {
            try
                let! arc = ProcessCore.ScaffoldReader.ARC.loadAsync (fun id -> ARC(id)) arcPath
                arc.ArcPath <- Some arcPath
                arc.IsSpreadsheetScaffold <- true
                return arc
            with
            | ex -> return failwith $"Failed to load ARC from scaffold at {arcPath}: {ex.Message}"
        }

    /// Loads an ARC from an exact .arc/project.yml when present. Otherwise it uses the existing arc.yml/XLSX discovery.
    static member loadAsync(arcPath : string) : CrossAsync<ARC> = 
        let projectPath = Path.combineMany [ arcPath; Path.ARCConfigFolderName; "project.yml" ]
        let yamlPath = Path.combine arcPath "arc.yml"
        crossAsync {
            let! projectExists = Path.fileExistsAsync projectPath
            if projectExists then
                let! result = ProjectRuntime.loadAsync (fun id -> ARC(id) :> Dataset) CodecRegistry.standard arcPath
                match result with
                | Error error -> return raise (ProjectException error)
                | Ok dataset ->
                    match dataset with
                    | :? ARC as arc ->
                        arc.ArcPath <- Some arcPath
                        arc.IsSpreadsheetScaffold <- false
                        return arc
                    | _ ->
                        return raise (
                            ProjectException {
                                Kind = ProjectErrorKind.Target
                                Message = "The root codec did not construct the ARC through CodecContext.CreateDataset."
                                RuleId = None
                                CodecId = None
                                Anchor = None
                                Cause = None
                            }
                        )
            else
                let! yamlExists = Path.fileExistsAsync yamlPath
                if yamlExists then
                    return! ARC.loadYMLAsync arcPath
                else
                    printfn $"No ARC yml file found at {yamlPath}, trying to read ARC Spreadsheet Scaffold"
                    return! ARC.loadXLSXAsync arcPath
        }

    #if !FABLE_COMPILER_JAVASCRIPT && !FABLE_COMPILER_TYPESCRIPT

    /// Writes the ARC to the specified path as arc.yml and makes YAML the active representation.
    member this.WriteYML(arcPath : string) =
        this.WriteYMLAsync arcPath |> Async.RunSynchronously

    /// Writes the ARC to the specified path as a spreadsheet scaffold and makes XLSX the active representation.
    member this.WriteXLSX(arcPath : string) =
        this.WriteXLSXAsync arcPath |> Async.RunSynchronously

    /// Writes the ARC using .arc/project.yml when present; otherwise writes arc.yml.
    member this.Write(arcPath : string) = 
        this.WriteAsync arcPath |> Async.RunSynchronously

    /// Updates the ARC at the specified path, re-resolving an authoritative destination project when present.
    ///
    /// If no path is provided, it will use the ArcPath property. If neither is set, it will throw an exception.
    member this.Update(?arcPath : string) = 
        this.UpdateAsync(?arcPath = arcPath) |> Async.RunSynchronously

    /// Loads an ARC from arc.yml in the specified path.
    static member loadYML(arcPath : string) : ARC =
        ARC.loadYMLAsync arcPath |> Async.RunSynchronously

    /// Loads an ARC from the spreadsheet scaffold in the specified path.
    static member loadXLSX(arcPath : string) : ARC =
        ARC.loadXLSXAsync arcPath |> Async.RunSynchronously

    /// Loads an ARC using an authoritative .arc/project.yml when present, otherwise using existing YAML/XLSX discovery.
    /// If neither is found, it throws an exception.
    static member load(arcPath : string) : ARC = 
        ARC.loadAsync arcPath |> Async.RunSynchronously
    #endif


module ProjectIO =

    let loadAsync (registry: CodecRegistry) (workspaceRoot: string) : CrossAsync<Result<ARC, ProjectError>> =
        crossAsync {
            let! result = ProjectRuntime.loadAsync (fun id -> ARC(id) :> Dataset) registry workspaceRoot
            return
                result
                |> Result.bind (fun dataset ->
                    match dataset with
                    | :? ARC as arc ->
                        arc.ArcPath <- Some workspaceRoot
                        arc.IsSpreadsheetScaffold <- false
                        Ok arc
                    | _ ->
                        Error {
                            Kind = ProjectErrorKind.Target
                            Message = "The root codec did not construct the ARC through CodecContext.CreateDataset."
                            RuleId = None
                            CodecId = None
                            Anchor = None
                            Cause = None
                        })
        }

    let writeAsync
        (registry: CodecRegistry)
        (workspaceRoot: string)
        (arc: ARC)
        : CrossAsync<Result<unit, ProjectError>>
        =
        ProjectRuntime.writeAsync registry workspaceRoot arc

    #if !FABLE_COMPILER_JAVASCRIPT && !FABLE_COMPILER_TYPESCRIPT
    let load (registry: CodecRegistry) (workspaceRoot: string) =
        loadAsync registry workspaceRoot |> Async.RunSynchronously

    let write (registry: CodecRegistry) (workspaceRoot: string) (arc: ARC) =
        writeAsync registry workspaceRoot arc |> Async.RunSynchronously
    #endif
