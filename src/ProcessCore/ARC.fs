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

    /// Writes the ARC to the specified path as arc.yml. This is a convenience alias for WriteYMLAsync.
    member this.WriteAsync(arcPath : string) : CrossAsync<unit> =
        this.WriteYMLAsync arcPath

    /// Updates the ARC at the specified path. If the ARC was loaded from a spreadsheet scaffold, it will update it as a scaffold.
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
            if _isSpreadsheetScaffold then 
                do! this.WriteXLSXAsync arcPath
            else 
                do! this.WriteYMLAsync arcPath

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

    /// Loads an ARC from the specified path. It first looks for an arc.yml file. If not found, it attempts to load from a spreadsheet scaffold.
    /// If neither is found, it throws an exception.
    static member loadAsync(arcPath : string) : CrossAsync<ARC> = 
        let p = Path.combine arcPath "arc.yml"
        crossAsync {
            let! yamlExists = Path.fileExistsAsync p
            let! arc = 
                crossAsync {
                    if yamlExists then
                        return! ARC.loadYMLAsync arcPath
                    else 
                        printfn $"No ARC yml file found at {p}, trying to read ARC Spreadsheet Scaffold"
                        return! ARC.loadXLSXAsync arcPath
                }
            return arc
        }

    #if !FABLE_COMPILER_JAVASCRIPT && !FABLE_COMPILER_TYPESCRIPT

    /// Writes the ARC to the specified path as arc.yml and makes YAML the active representation.
    member this.WriteYML(arcPath : string) =
        this.WriteYMLAsync arcPath |> Async.RunSynchronously

    /// Writes the ARC to the specified path as a spreadsheet scaffold and makes XLSX the active representation.
    member this.WriteXLSX(arcPath : string) =
        this.WriteXLSXAsync arcPath |> Async.RunSynchronously

    /// Writes the ARC to the specified path as arc.yml. This is a convenience alias for WriteYML.
    member this.Write(arcPath : string) = 
        this.WriteYML arcPath

    /// Updates the ARC at the specified path. If the ARC was loaded from a spreadsheet scaffold, it will update it as a scaffold. 
    ///
    /// If no path is provided, it will use the ArcPath property. If neither is set, it will throw an exception.
    member this.Update(?arcPath : string) = 
        let arcPath = 
            match arcPath with
            | Some p -> 
                _arcPath <- Some p; 
                p
            | None -> 
                match this.ArcPath with
                | Some p -> p
                | None -> failwith "ARC path is not set. Please provide an arcPath or set the ArcPath property."
        if _isSpreadsheetScaffold then 
            this.WriteXLSX arcPath
        else 
            this.WriteYML arcPath

    /// Loads an ARC from arc.yml in the specified path.
    static member loadYML(arcPath : string) : ARC =
        ARC.loadYMLAsync arcPath |> Async.RunSynchronously

    /// Loads an ARC from the spreadsheet scaffold in the specified path.
    static member loadXLSX(arcPath : string) : ARC =
        ARC.loadXLSXAsync arcPath |> Async.RunSynchronously

    /// Loads an ARC from the specified path. It first looks for an arc.yml file. If not found, it attempts to load from a spreadsheet scaffold.
    /// If neither is found, it throws an exception.
    static member load(arcPath : string) : ARC = 
        let p = Path.combine arcPath "arc.yml"
        let arc = 
            if Path.fileExistsAsync p |> Async.RunSynchronously then
                ARC.loadYML arcPath
            else 
                printfn $"No ARC yml file found at {p}, trying to read ARC Spreadsheet Scaffold"
                ARC.loadXLSX arcPath
        arc
    #endif
