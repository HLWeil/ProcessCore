namespace ProcessCore

open Fable.Core
open ProcessCore.Helper
open CrossAsync

// (identifier: string, ?title: string, ?description: string, ?additionalType: string, ?license: string, ?datePublished: string, ?dateCreated: string, ?dateModified: string, ?processes: seq<Process>, ?hasPart: seq<Dataset>, ?dataFiles: seq<Data>, ?agents: seq<Agent>, ?citations: seq<ScholarlyArticle>, ?dataContexts: seq<DataContext>, ?additionalProperty: seq<Annotation>)

[<AttachMembers>]
type ARC(identifier: string, ?title: string, ?description: string, ?additionalType: string, ?license: string, ?datePublished: string, ?dateCreated: string, ?dateModified: string, ?processes: seq<Process>, ?hasPart: seq<Dataset>, ?dataFiles: seq<Data>, ?agents: seq<Agent>, ?citations: seq<ScholarlyArticle>, ?dataContexts: seq<DataContext>, ?additionalProperty: seq<Annotation>) =

    inherit Dataset(identifier, ?title=title, ?description=description, ?additionalType=additionalType, ?license=license, ?datePublished=datePublished, ?dateCreated=dateCreated, ?dateModified=dateModified, ?processes=processes, ?hasPart=hasPart, ?dataFiles=dataFiles, ?agents=agents, ?citations=citations, ?dataContexts=dataContexts, ?additionalProperty=additionalProperty)
    
    let mutable _arcPath : string option = None
    let mutable _isSpreadsheetScaffold : bool = false


    member this.toYamlString(?whiteSpace: int) : string =
        ProcessCore.Yaml.Dataset.toYamlStringIndexed whiteSpace this

    static member fromYamlString(yamlString: string) : ARC =
        YAMLicious.Reader.read yamlString 
        |> ProcessCore.Yaml.Dataset.decoderGeneric (fun i -> ARC(i)) None None false


    member this.ArcPath
        with get() = _arcPath
        and set(value) = _arcPath <- value

    member this.IsSpreadsheetScaffold
        with get() = _isSpreadsheetScaffold
        and set(value) = _isSpreadsheetScaffold <- value

    /// Writes the ARC to the specified path as arc.yml. If the ARC was loaded from a spreadsheet scaffold, it will still write as a YAML file.
    member this.WriteAsync(arcPath : string) : CrossAsync<unit> = 
        crossAsync {
            do! Path.ensureDirectoryAsync arcPath
            _isSpreadsheetScaffold <- false
            _arcPath <- Some arcPath
            let p = Path.combine arcPath "arc.yml"
            let ymlString = ProcessCore.Yaml.Dataset.toYamlStringIndexed (Some 2) this
            do! Path.writeFileTextAsync p ymlString
        }

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
                do! ScaffoldReader.ARC.writeAsync arcPath this
            else 
                let p = Path.combine arcPath "arc.yml"
                let ymlString = ProcessCore.Yaml.Dataset.toYamlStringIndexed (Some 2) this
                do! Path.writeFileTextAsync p ymlString

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
                        try
                            let! ymlString = Path.readFileTextAsync p
                            return ARC.fromYamlString ymlString
                        with
                        | ex -> return failwith $"Failed to load ARC from yml at {p}: {ex.Message}"           
                    else 
                        printfn $"No ARC yml file found at {p}, trying to read ARC Spreadsheet Scaffold"
                        try 
                            let! arc = ProcessCore.ScaffoldReader.ARC.loadAsync (fun id -> ARC(id)) arcPath
                            arc.IsSpreadsheetScaffold <- true
                            return arc
                        with
                        | ex -> return failwith $"Failed to load ARC from scaffold at {arcPath}: {ex.Message}"
                }
            arc.ArcPath <- Some arcPath
            return arc
        }

    #if !FABLE_COMPILER_JAVASCRIPT && !FABLE_COMPILER_TYPESCRIPT

    /// Writes the ARC to the specified path as arc.yml. If the ARC was loaded from a spreadsheet scaffold, it will still write as a YAML file.
    member this.Write(arcPath : string) = 
        _isSpreadsheetScaffold <- false
        _arcPath <- Some arcPath
        let p = Path.combine arcPath "arc.yml"
        let ymlString = ProcessCore.Yaml.Dataset.toYamlStringIndexed (Some 2) this
        Path.writeFileTextAsync p ymlString
        |> Async.RunSynchronously

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
            ScaffoldReader.ARC.write arcPath this
        else 
            let p = Path.combine arcPath "arc.yml"
            let ymlString = ProcessCore.Yaml.Dataset.toYamlStringIndexed (Some 2) this
            Path.writeFileTextAsync p ymlString
            |> Async.RunSynchronously

    /// Loads an ARC from the specified path. It first looks for an arc.yml file. If not found, it attempts to load from a spreadsheet scaffold.
    /// If neither is found, it throws an exception.
    static member load(arcPath : string) : ARC = 
        let p = Path.combine arcPath "arc.yml"
        let arc = 
            if Path.fileExistsAsync p |> Async.RunSynchronously then
                try
                    let ymlString = Path.readFileTextAsync p |> Async.RunSynchronously
                    ARC.fromYamlString ymlString
                with
                | ex -> failwith $"Failed to load ARC from yml at {p}: {ex.Message}"           
            else 
                printfn $"No ARC yml file found at {p}, trying to read ARC Spreadsheet Scaffold"
                try 
                    let arc = ProcessCore.ScaffoldReader.ARC.load (fun id -> ARC(id)) arcPath
                    arc.IsSpreadsheetScaffold <- true
                    arc
                with
                | ex -> failwith $"Failed to load ARC from scaffold at {arcPath}: {ex.Message}"
        arc.ArcPath <- Some arcPath
        arc
    #endif