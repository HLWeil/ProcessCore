namespace ProcessCore

open Fable.Core
open ProcessCore.Helper

// (identifier: string, ?title: string, ?description: string, ?additionalType: string, ?license: string, ?datePublished: string, ?dateCreated: string, ?dateModified: string, ?processes: seq<Process>, ?hasPart: seq<Dataset>, ?dataFiles: seq<Data>, ?agents: seq<Agent>, ?citations: seq<ScholarlyArticle>, ?dataContexts: seq<DataContext>, ?additionalProperty: seq<Annotation>)

[<AttachMembers>]
type ARC(identifier: string, ?title: string, ?description: string, ?additionalType: string, ?license: string, ?datePublished: string, ?dateCreated: string, ?dateModified: string, ?processes: seq<Process>, ?hasPart: seq<Dataset>, ?dataFiles: seq<Data>, ?agents: seq<Agent>, ?citations: seq<ScholarlyArticle>, ?dataContexts: seq<DataContext>, ?additionalProperty: seq<Annotation>) =

    inherit Dataset(identifier, ?title=title, ?description=description, ?additionalType=additionalType, ?license=license, ?datePublished=datePublished, ?dateCreated=dateCreated, ?dateModified=dateModified, ?processes=processes, ?hasPart=hasPart, ?dataFiles=dataFiles, ?agents=agents, ?citations=citations, ?dataContexts=dataContexts, ?additionalProperty=additionalProperty)

    #if !FABLE_COMPILER
    
    let mutable _arcPath : string option = None
    let mutable _isSpreadsheetScaffold : bool = false

    #endif


    member this.toYamlString(?whiteSpace: int) : string =
        ProcessCore.Yaml.Dataset.toYamlStringIndexed whiteSpace this

    static member fromYamlString(yamlString: string) : ARC =
        YAMLicious.Reader.read yamlString 
        |> ProcessCore.Yaml.Dataset.decoderGeneric (fun i -> ARC(i)) None None false

    #if !FABLE_COMPILER

    member this.ArcPath
        with get() = _arcPath
        and set(value) = _arcPath <- value

    member this.IsSpreadsheetScaffold
        with get() = _isSpreadsheetScaffold
        and set(value) = _isSpreadsheetScaffold <- value

    member this.Write(arcPath : string) = 
        _isSpreadsheetScaffold <- false
        _arcPath <- Some arcPath
        let p = Path.combine arcPath "arc.yml"
        let ymlString = ProcessCore.Yaml.Dataset.toYamlStringIndexed (Some 2) this
        Path.writeFileTextAsync p ymlString
        |> Async.RunSynchronously

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
        let p = Path.combine arcPath "arc.yml"
        let ymlString = ProcessCore.Yaml.Dataset.toYamlStringIndexed (Some 2) this
        Path.writeFileTextAsync p ymlString
        |> Async.RunSynchronously

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