namespace ProcessCore

open Fable.Core
open ProcessCore.Helper

// (identifier: string, ?title: string, ?description: string, ?additionalType: string, ?license: string, ?datePublished: string, ?dateCreated: string, ?dateModified: string, ?processes: seq<Process>, ?hasPart: seq<Dataset>, ?dataFiles: seq<Data>, ?agents: seq<Agent>, ?citations: seq<ScholarlyArticle>, ?dataContexts: seq<DataContext>, ?additionalProperty: seq<Annotation>)

[<AttachMembers>]
type ARC(identifier: string, ?title: string, ?description: string, ?additionalType: string, ?license: string, ?datePublished: string, ?dateCreated: string, ?dateModified: string, ?processes: seq<Process>, ?hasPart: seq<Dataset>, ?dataFiles: seq<Data>, ?agents: seq<Agent>, ?citations: seq<ScholarlyArticle>, ?dataContexts: seq<DataContext>, ?additionalProperty: seq<Annotation>) =

    inherit Dataset(identifier, ?title=title, ?description=description, ?additionalType=additionalType, ?license=license, ?datePublished=datePublished, ?dateCreated=dateCreated, ?dateModified=dateModified, ?processes=processes, ?hasPart=hasPart, ?dataFiles=dataFiles, ?agents=agents, ?citations=citations, ?dataContexts=dataContexts, ?additionalProperty=additionalProperty)


    member this.toYamlString(?whiteSpace: int) : string =
        ProcessCore.Yaml.Dataset.toYamlStringIndexed whiteSpace this

    static member fromYamlString(yamlString: string) : ARC =
        YAMLicious.Reader.read yamlString 
        |> ProcessCore.Yaml.Dataset.decoderGeneric (fun i -> ARC(i)) false

    #if !FABLE_COMPILER_JAVASASCRIPT && !FABLE_COMPILER_PYTHON
    member this.Write(arcPath : string) = 
        
        let p = Path.combine arcPath "arc.yml"
        let ymlString = ProcessCore.Yaml.Dataset.toYamlStringIndexed (Some 2) this
        FileSystemHelper.writeFileTextAsync p ymlString
        |> Async.RunSynchronously

    static member load(arcPath : string) : ARC = 
        let p = Path.combine arcPath "arc.yml"
        if FileSystemHelper.fileExistsAsync p |> Async.RunSynchronously then
            try
                let ymlString = FileSystemHelper.readFileTextAsync p |> Async.RunSynchronously
                ARC.fromYamlString ymlString
            with
            | ex -> failwith $"Failed to load ARC from yml at {p}: {ex.Message}"           
        else 
            printfn $"No ARC yml file found at {p}, trying to read ARC Spreadsheet Scaffold"
            try 
                ProcessCore.ScaffoldReader.ARC.load (fun id -> ARC(id)) arcPath
            with
            | ex -> failwith $"Failed to load ARC from scaffold at {arcPath}: {ex.Message}"

    #endif

