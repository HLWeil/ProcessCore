namespace ProcessCore

open System
open System.Globalization
open System.Text.RegularExpressions
open Fable.Core
open YAMLicious.YAMLiciousTypes
open ProcessCore.CrossAsync

type WorkspaceProfileReference =
    | File of string
    | Url of string

type StorageTarget =
    | Root
    | Identifier of string
    | AdditionalType of string

type StorageFileCreation =
    | Empty

type StorageFile = {
    Id: string
    Path: string
    Create: StorageFileCreation option
}

type StorageRule = {
    Id: string
    Codec: string
    Target: StorageTarget
    Path: string
    Files: StorageFile list
}

type WorkspaceProject = {
    WorkspaceProfiles: WorkspaceProfileReference list
    Rules: StorageRule list
}

type WorkspaceProfile = {
    Id: string
    Version: string
    Description: string option
    Rules: StorageRule list
}

[<RequireQualifiedAccess>]
type ProjectErrorKind =
    | Project
    | Profile
    | Rule
    | Codec
    | Path
    | Target
    | Resource

type ProjectError = {
    Kind: ProjectErrorKind
    Message: string
    RuleId: string option
    CodecId: string option
    Anchor: string option
    Cause: string option
}

exception ProjectException of ProjectError

type CodecInput = {
    Primary: byte array
    Files: Map<string, byte array>
}

type CodecOutput = {
    Primary: byte array
    Files: Map<string, byte array>
}

type CodecContext = {
    RelativeAnchor: string
    CreateDataset: string -> Dataset
}

type DatasetCodec = {
    Id: string
    ReadAsync: CodecContext -> CodecInput -> CrossAsync<Result<Dataset, ProjectError>>
    WriteAsync: CodecContext -> Dataset -> CrossAsync<Result<CodecOutput, ProjectError>>
}

type CodecRegistry = private CodecRegistry of Map<string, DatasetCodec>

module private ProjectErrors =

    let create (kind: ProjectErrorKind) (message: string) : ProjectError =
        {
            Kind = kind
            Message = message
            RuleId = None
            CodecId = None
            Anchor = None
            Cause = None
        }

    let withRule (ruleId: string) (error: ProjectError) = { error with RuleId = Some ruleId }
    let withCodec (codecId: string) (error: ProjectError) = { error with CodecId = Some codecId }
    let withAnchor (anchor: string) (error: ProjectError) = { error with Anchor = Some anchor }
    let withCause (cause: exn) (error: ProjectError) = { error with Cause = Some cause.Message }

    let raiseError (error: ProjectError) = raise (ProjectException error)

module private ProjectFileSystem =

    #if FABLE_COMPILER_JAVASCRIPT || FABLE_COMPILER_TYPESCRIPT

    [<Erase>]
    type NodeStats =
        abstract isFile: unit -> bool
        abstract isDirectory: unit -> bool
        abstract isSymbolicLink: unit -> bool

    [<Erase>]
    type NodeDirent =
        abstract name: string
        abstract isFile: unit -> bool
        abstract isDirectory: unit -> bool
        abstract isSymbolicLink: unit -> bool

    [<Import("resolve", "node:path")>]
    let private resolveNodePath (_basePath: string) (_relativePath: string) : string = nativeOnly

    [<Import("relative", "node:path")>]
    let private relativeNodePath (_basePath: string) (_fullPath: string) : string = nativeOnly

    [<Import("dirname", "node:path")>]
    let private dirnameNodePath (_path: string) : string = nativeOnly

    [<Import("existsSync", "node:fs")>]
    let private existsSync (_path: string) : bool = nativeOnly

    [<Import("lstatSync", "node:fs")>]
    let private lstatSync (_path: string) : NodeStats = nativeOnly

    [<Import("readdirSync", "node:fs")>]
    let private readdirSync (_path: string) (_options: obj) : NodeDirent array = nativeOnly

    [<Import("readFileSync", "node:fs")>]
    let private readFileSync (_path: string) (_encoding: string) : string = nativeOnly

    [<Emit("process.platform === 'win32'")>]
    let private nodeIsWindows () : bool = nativeOnly

    let fullPath basePath relativePath = resolveNodePath basePath relativePath
    let relative basePath fullPath = relativeNodePath basePath fullPath
    let dirname path = dirnameNodePath path
    let readAllText path = readFileSync path "utf8"
    let exists path = existsSync path
    let isFile path = existsSync path && (lstatSync path).isFile()
    let isDirectory path = existsSync path && (lstatSync path).isDirectory()
    let isLink path = existsSync path && (lstatSync path).isSymbolicLink()
    let isWindows = nodeIsWindows()

    let enumerateFilesWithoutFollowingLinks root =
        let files = ResizeArray<string>()
        let rec visit directory =
            for entry in readdirSync directory {| withFileTypes = true |} do
                let path = resolveNodePath directory entry.name
                if entry.isSymbolicLink() then
                    if not (entry.isDirectory()) then files.Add path
                elif entry.isFile() then
                    files.Add path
                elif entry.isDirectory() then
                    visit path
        visit root
        List.ofSeq files

    #else

    let fullPath (basePath: string) (relativePath: string) =
        System.IO.Path.GetFullPath(System.IO.Path.Combine(basePath, relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar)))

    let relative (basePath: string) (fullPath: string) = System.IO.Path.GetRelativePath(basePath, fullPath)
    let dirname (path: string) = System.IO.Path.GetDirectoryName path
    let readAllText (path: string) = System.IO.File.ReadAllText path
    let exists (path: string) = System.IO.File.Exists path || System.IO.Directory.Exists path
    let isFile (path: string) = System.IO.File.Exists path
    let isDirectory (path: string) = System.IO.Directory.Exists path

    let isLink (path: string) =
        exists path
        && (System.IO.File.GetAttributes(path) &&& System.IO.FileAttributes.ReparsePoint) <> enum 0

    let isWindows =
        System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Windows
        )

    let enumerateFilesWithoutFollowingLinks (root: string) =
        let files = ResizeArray<string>()
        let rec visit directory =
            for file in System.IO.Directory.GetFiles directory do
                files.Add file
            for child in System.IO.Directory.GetDirectories directory do
                if not (isLink child) then visit child
        visit root
        List.ofSeq files

    #endif

module private StrictProjectYaml =

    let identifierPattern = Regex("^[A-Za-z][A-Za-z0-9._-]{0,127}$")
    let versionPattern = Regex("^[A-Za-z0-9][A-Za-z0-9._+-]{0,63}$")
    let numericPattern =
        Regex(
            "^[+-]?(?:0|[1-9][0-9_]*|0[xX][0-9a-fA-F_]+|0[oO][0-7_]+|0[bB][01_]+)(?:\\.[0-9_]+)?(?:[eE][+-]?[0-9_]+)?$"
        )

    let error kind message = ProjectErrors.create kind message |> ProjectErrors.raiseError

    let rec rejectUnsupported kind element =
        match element with
        | YAMLElement.Alias alias -> error kind $"YAML aliases are not supported (found '*{alias}')."
        | YAMLElement.Value content ->
            if content.Tag.IsSome then error kind "Tagged YAML scalar values are not supported."
            if content.Anchor.IsSome then error kind "YAML anchors are not supported."
        | YAMLElement.Mapping (key, value) ->
            if key.Tag.IsSome then error kind "Tagged YAML mapping keys are not supported."
            if key.Anchor.IsSome then error kind "YAML anchors are not supported."
            rejectUnsupported kind value
        | YAMLElement.Sequence values
        | YAMLElement.Object values ->
            values |> List.iter (rejectUnsupported kind)
        | YAMLElement.Comment _
        | YAMLElement.DocumentStart
        | YAMLElement.DocumentEnd
        | YAMLElement.Nil -> ()

    let structuralItems values =
        values
        |> List.filter (function
            | YAMLElement.Comment _
            | YAMLElement.DocumentStart
            | YAMLElement.DocumentEnd -> false
            | _ -> true)

    let rec unwrap = function
        | YAMLElement.Object [ single ] ->
            match single with
            | YAMLElement.Value _
            | YAMLElement.Sequence _
            | YAMLElement.Alias _
            | YAMLElement.Nil -> unwrap single
            | _ -> YAMLElement.Object [ single ]
        | value -> value

    let mappings kind context element =
        match unwrap element with
        | YAMLElement.Object values ->
            let values = structuralItems values
            values
            |> List.map (function
                | YAMLElement.Mapping (key, value) -> key.Value, value
                | other -> error kind $"{context} must be a YAML mapping, but contains {other}.")
        | other -> error kind $"{context} must be a YAML mapping, but found {other}."

    let validateFields kind context allowed required fields =
        let keys = fields |> List.map fst
        keys
        |> List.countBy id
        |> List.tryFind (fun (_, count) -> count > 1)
        |> Option.iter (fun (key, _) -> error kind $"{context} contains duplicate field '{key}'.")
        keys
        |> List.tryFind (fun key -> not (Set.contains key allowed))
        |> Option.iter (fun key -> error kind $"{context} contains unknown field '{key}'.")
        required
        |> Set.iter (fun key ->
            if not (List.contains key keys) then error kind $"{context} is missing required field '{key}'.")

    let field name fields = fields |> List.tryPick (fun (key, value) -> if key = name then Some value else None)

    let isImplicitNonString (value: string) =
        let text = value.Trim()
        text = "~"
        || text.Equals("null", StringComparison.OrdinalIgnoreCase)
        || text.Equals("true", StringComparison.OrdinalIgnoreCase)
        || text.Equals("false", StringComparison.OrdinalIgnoreCase)
        || text.Equals(".nan", StringComparison.OrdinalIgnoreCase)
        || text.Equals(".inf", StringComparison.OrdinalIgnoreCase)
        || text.Equals("+.inf", StringComparison.OrdinalIgnoreCase)
        || text.Equals("-.inf", StringComparison.OrdinalIgnoreCase)
        || numericPattern.IsMatch(text)

    let scalar kind context element =
        match unwrap element with
        | YAMLElement.Value content ->
            let plain =
                match content.Style with
                | None
                | Some ScalarStyle.Plain -> true
                | _ -> false
            if plain && isImplicitNonString content.Value then
                error kind $"{context} must be a YAML string; quote values that resemble booleans, numbers, or null."
            content.Value
        | other -> error kind $"{context} must be a scalar string, but found {other}."

    let nonEmpty kind context (value: string) =
        if String.IsNullOrEmpty value then error kind $"{context} must not be empty."
        value

    let identifier kind context (value: string) =
        if not (identifierPattern.IsMatch value) then
            error kind $"{context} must match ^[A-Za-z][A-Za-z0-9._-]*$ and be at most 128 characters."
        value

    let version kind context (value: string) =
        if not (versionPattern.IsMatch value) then
            error kind $"{context} is not a valid profile version."
        value

    let safeRelativePath kind context (allowCapture: bool) (value: string) =
        nonEmpty kind context value |> ignore
        if value.Contains("\\") then error kind $"{context} must use '/' separators, not backslashes."
        if value.IndexOf('\u0000') >= 0 then error kind $"{context} contains a NUL character."
        if value.StartsWith("/", StringComparison.Ordinal) || value.StartsWith("//", StringComparison.Ordinal) then
            error kind $"{context} must be relative."
        if Regex.IsMatch(value, "^[A-Za-z][A-Za-z0-9+.-]*:") then error kind $"{context} must not be absolute or URI-like."
        let segments = value.Split('/')
        if segments |> Array.exists (fun part -> part = "" || part = "." || part = "..") then
            error kind $"{context} contains an empty or traversal segment."
        let capture = "{dataset.identifier}"
        let captureCount = segments |> Array.filter ((=) capture) |> Array.length
        if captureCount > 1 then error kind $"{context} may contain at most one '{capture}' segment."
        for segment in segments do
            if segment.Contains("{") || segment.Contains("}") then
                if not (allowCapture && segment = capture) then
                    error kind $"{context} contains an unsupported or partial capture."
        value

    let sequence kind context element =
        match unwrap element with
        | YAMLElement.Sequence values -> values
        | other -> error kind $"{context} must be a YAML sequence, but found {other}."

    let parseTarget kind element =
        match unwrap element with
        | YAMLElement.Value _ ->
            let value = scalar kind "rule target" element
            if value = "root" then Root else error kind $"Unknown scalar target '{value}'."
        | _ ->
            let fields = mappings kind "rule target" element
            validateFields kind "rule target" (Set.ofList [ "identifier"; "additionalType" ]) Set.empty fields
            match field "identifier" fields, field "additionalType" fields with
            | Some value, None -> Identifier(scalar kind "target identifier" value |> nonEmpty kind "target identifier")
            | None, Some value -> AdditionalType(scalar kind "target additionalType" value |> nonEmpty kind "target additionalType")
            | _ -> error kind "A rule target must contain exactly one of 'identifier' or 'additionalType'."

    let parseStorageFile kind element =
        let fields = mappings kind "storage file" element
        let allowed = Set.ofList [ "id"; "path"; "create" ]
        validateFields kind "storage file" allowed (Set.ofList [ "id"; "path" ]) fields
        let get name = field name fields |> Option.get
        let create =
            field "create" fields
            |> Option.map (fun value ->
                match scalar kind "storage file create" value with
                | "empty" -> StorageFileCreation.Empty
                | other -> error kind $"Unknown storage file create policy '{other}'.")
        {
            Id = scalar kind "storage file id" (get "id") |> identifier kind "storage file id"
            Path =
                scalar kind "storage file path" (get "path")
                |> safeRelativePath kind "storage file path" false
            Create = create
        }

    let parseStorageFiles kind element =
        let files = sequence kind "storage rule files" element |> List.map (parseStorageFile kind)
        if List.isEmpty files then error kind "Storage rule files must not be empty when declared."
        files
        |> List.countBy (fun file -> file.Id)
        |> List.tryFind (fun (_, count) -> count > 1)
        |> Option.iter (fun (id, _) -> error kind $"Duplicate storage file id '{id}'.")
        files

    let parseRule kind element =
        let fields = mappings kind "storage rule" element
        let allowed = Set.ofList [ "id"; "codec"; "target"; "path"; "files" ]
        let required = Set.ofList [ "id"; "codec"; "target"; "path" ]
        validateFields kind "storage rule" allowed required fields
        let get name = field name fields |> Option.get
        let id = scalar kind "rule id" (get "id") |> identifier kind "rule id"
        let codec = scalar kind "codec id" (get "codec") |> identifier kind "codec id"
        let target = parseTarget kind (get "target")
        let path = scalar kind "rule path" (get "path") |> safeRelativePath kind "rule path" true
        let files =
            field "files" fields
            |> Option.map (parseStorageFiles kind)
            |> Option.defaultValue []
        match target with
        | AdditionalType _ when not (path.Split('/') |> Array.contains "{dataset.identifier}") ->
            error kind "An additionalType rule path must contain the whole-segment '{dataset.identifier}' capture."
        | _ -> ()
        { Id = id; Codec = codec; Target = target; Path = path; Files = files }

    let parseRules kind required element =
        let rules = sequence kind "rules" element |> List.map (parseRule kind)
        if required && List.isEmpty rules then error kind "Profile rules must not be empty."
        rules
        |> List.countBy (fun rule -> rule.Id)
        |> List.tryFind (fun (_, count) -> count > 1)
        |> Option.iter (fun (id, _) -> error kind $"Duplicate rule id '{id}'.")
        rules

    let parseProfileReference element =
        let fields = mappings ProjectErrorKind.Project "workspace profile reference" element
        validateFields ProjectErrorKind.Project "workspace profile reference" (Set.ofList [ "file"; "url" ]) Set.empty fields
        match field "file" fields, field "url" fields with
        | Some value, None ->
            scalar ProjectErrorKind.Project "profile file" value
            |> safeRelativePath ProjectErrorKind.Project "profile file" false
            |> WorkspaceProfileReference.File
        | None, Some value ->
            let url = scalar ProjectErrorKind.Project "profile URL" value |> nonEmpty ProjectErrorKind.Project "profile URL"
            if not (Regex.IsMatch(url, "^https?://[^#]+$")) then
                error ProjectErrorKind.Project "Profile URLs must be absolute HTTP(S) URLs without fragments."
            WorkspaceProfileReference.Url url
        | _ -> error ProjectErrorKind.Project "A workspace profile reference must contain exactly one of 'file' or 'url'."

    let read kind text =
        try
            let element = YAMLicious.Reader.read text
            rejectUnsupported kind element
            element
        with
        | ProjectException error -> ProjectErrors.raiseError error
        | ex -> ProjectErrors.create kind "The YAML document could not be parsed." |> ProjectErrors.withCause ex |> ProjectErrors.raiseError

    let parseProject text =
        let root = read ProjectErrorKind.Project text
        let fields = mappings ProjectErrorKind.Project "ArcWorkspaceProject" root
        let allowed = Set.ofList [ "type"; "workspaceProfiles"; "rules" ]
        validateFields ProjectErrorKind.Project "ArcWorkspaceProject" allowed (Set.singleton "type") fields
        let documentType = field "type" fields |> Option.get |> scalar ProjectErrorKind.Project "project type"
        if documentType <> "ArcWorkspaceProject" then error ProjectErrorKind.Project $"Expected type 'ArcWorkspaceProject', found '{documentType}'."
        let profiles =
            field "workspaceProfiles" fields
            |> Option.map (sequence ProjectErrorKind.Project "workspaceProfiles" >> List.map parseProfileReference)
            |> Option.defaultValue []
        let rules =
            field "rules" fields
            |> Option.map (parseRules ProjectErrorKind.Project false)
            |> Option.defaultValue []
        if List.isEmpty profiles && List.isEmpty rules then
            error ProjectErrorKind.Project "A project must contain at least one workspace profile or local rule."
        { WorkspaceProfiles = profiles; Rules = rules }

    let parseProfile text =
        let root = read ProjectErrorKind.Profile text
        let fields = mappings ProjectErrorKind.Profile "ArcWorkspaceProfile" root
        let allowed = Set.ofList [ "type"; "id"; "version"; "description"; "rules" ]
        validateFields ProjectErrorKind.Profile "ArcWorkspaceProfile" allowed (Set.ofList [ "type"; "id"; "version"; "rules" ]) fields
        let get name = field name fields |> Option.get
        let documentType = scalar ProjectErrorKind.Profile "profile type" (get "type")
        if documentType <> "ArcWorkspaceProfile" then error ProjectErrorKind.Profile $"Expected type 'ArcWorkspaceProfile', found '{documentType}'."
        {
            Id = scalar ProjectErrorKind.Profile "profile id" (get "id") |> identifier ProjectErrorKind.Profile "profile id"
            Version = scalar ProjectErrorKind.Profile "profile version" (get "version") |> version ProjectErrorKind.Profile "profile version"
            Description = field "description" fields |> Option.map (scalar ProjectErrorKind.Profile "profile description")
            Rules = parseRules ProjectErrorKind.Profile true (get "rules")
        }

module WorkspaceProject =

    let parse (text: string) =
        try Ok(StrictProjectYaml.parseProject text)
        with
        | ProjectException error -> Error error

module WorkspaceProfile =

    let parse (text: string) =
        try Ok(StrictProjectYaml.parseProfile text)
        with
        | ProjectException error -> Error error

module private SafeProjectPath =

    let equals (left: string) (right: string) =
        if ProjectFileSystem.isWindows then
            left.ToUpperInvariant() = right.ToUpperInvariant()
        else
            left = right

    let normalizeRelative (value: string) = value.Replace('\\', '/')

    let private hasPrefix (root: string) (path: string) =
        let relative = ProjectFileSystem.relative root path |> normalizeRelative
        relative = ""
        || (relative <> ".."
            && not (relative.StartsWith("../", StringComparison.Ordinal))
            && not (relative.StartsWith("/", StringComparison.Ordinal))
            && not (Regex.IsMatch(relative, "^[A-Za-z][A-Za-z0-9+.-]*:")))

    let private rejectReparseSegments (root: string) (fullPath: string) =
        let check path =
            if ProjectFileSystem.isLink path then
                Error(ProjectErrors.create ProjectErrorKind.Path $"Path traverses reparse point or symbolic link '{path}'.")
            else Ok()
        match check root with
        | Error error -> Error error
        | Ok () ->
            let relative = ProjectFileSystem.relative root fullPath |> normalizeRelative
            let segments =
                if relative = "" then [||]
                else relative.Split('/')
            let mutable current = root
            let mutable failure = None
            for segment in segments do
                if failure.IsNone then
                    current <- ProjectFileSystem.fullPath current segment
                    match check current with
                    | Error error -> failure <- Some error
                    | Ok () -> ()
            match failure with
            | Some error -> Error error
            | None -> Ok()

    let resolve (root: string) (relative: string) =
        try
            let root = ProjectFileSystem.fullPath root ""
            let fullPath = ProjectFileSystem.fullPath root relative
            if not (hasPrefix root fullPath) then
                Error(ProjectErrors.create ProjectErrorKind.Path $"Path '{relative}' escapes its configured root.")
            else
                rejectReparseSegments root fullPath |> Result.map (fun () -> fullPath)
        with
        | ex -> Error(ProjectErrors.create ProjectErrorKind.Path $"Path '{relative}' could not be resolved." |> ProjectErrors.withCause ex)

    let relativeTo (root: string) (fullPath: string) =
        ProjectFileSystem.relative root fullPath |> normalizeRelative

    let safeIdentifier (value: string) =
        not (String.IsNullOrEmpty value)
        && value <> "."
        && value <> ".."
        && not (value.Contains("/"))
        && not (value.Contains("\\"))
        && value.IndexOf('\u0000') < 0

type private PathTemplate = {
    Text: string
    Segments: string array
    CaptureIndex: int option
}

type private ResolvedRule = {
    QualifiedId: string
    Codec: DatasetCodec
    Target: StorageTarget
    Template: PathTemplate
    Files: StorageFile list
}

type private ResolvedProject = {
    WorkspaceRoot: string
    Rules: ResolvedRule list
    ReservedIdentifiers: Set<string>
}

type private PreparedFile = {
    Definition: StorageFile
    Path: string
    RelativePath: string
}

type private PreparedBinding = {
    Rule: ResolvedRule
    Anchor: string
    RelativeAnchor: string
    Files: PreparedFile list
    CapturedIdentifier: string option
    Dataset: Dataset option
}

module private PathTemplates =

    let create (value: string) =
        let segments = value.Split('/')
        {
            Text = value
            Segments = segments
            CaptureIndex = segments |> Array.tryFindIndex ((=) "{dataset.identifier}")
        }

    let render (identifier: string) (template: PathTemplate) =
        match template.CaptureIndex with
        | None -> Ok template.Text
        | Some index when SafeProjectPath.safeIdentifier identifier ->
            let segments = Array.copy template.Segments
            segments.[index] <- identifier
            Ok(String.Join("/", segments))
        | Some _ -> Error(ProjectErrors.create ProjectErrorKind.Path $"Dataset identifier '{identifier}' is not a safe path segment.")

    let tryMatch (relative: string) (template: PathTemplate) =
        let candidate = SafeProjectPath.normalizeRelative relative
        let parts = candidate.Split('/')
        if parts.Length <> template.Segments.Length then None
        else
            let mutable matches = true
            let mutable captured = None
            for index in 0 .. parts.Length - 1 do
                match template.CaptureIndex with
                | Some captureIndex when captureIndex = index ->
                    if SafeProjectPath.safeIdentifier parts.[index] then captured <- Some parts.[index]
                    else matches <- false
                | _ ->
                    if not (SafeProjectPath.equals parts.[index] template.Segments.[index]) then matches <- false
            if matches then Some captured else None

module private StandardProjectCodecs =

    let error (codecId: string) (context: CodecContext) (message: string) =
        ProjectErrors.create ProjectErrorKind.Resource message
        |> ProjectErrors.withCodec codecId
        |> ProjectErrors.withAnchor context.RelativeAnchor

    let datamapId = "datamap"

    let enrichFromDatamap
        (codecId: string)
        (context: CodecContext)
        (files: Map<string, byte array>)
        (dataset: Dataset)
        =
        crossAsync {
            match Map.tryFind datamapId files with
            | None -> return Ok dataset
            | Some bytes ->
                try
                    let! workbook = ProcessCore.Helper.Path.readXlsxBytesAsync bytes
                    for dataContext in ProcessCore.Spreadsheet.Datamap.dataContextsFromFsWorkbook workbook do
                        dataset.AddDataContext(dataContext)
                    return Ok dataset
                with
                | ex ->
                    return Error(error codecId context "The declared Datamap workbook could not be read." |> ProjectErrors.withCause ex)
        }

    let read
        (codecId: string)
        (includeDatamap: bool)
        (parser: CodecContext -> FsSpreadsheet.FsWorkbook -> Dataset option)
        (context: CodecContext)
        (input: CodecInput)
        =
        crossAsync {
            try
                let! workbook = ProcessCore.Helper.Path.readXlsxBytesAsync input.Primary
                match parser context workbook with
                | None -> return Error(error codecId context $"Workbook is not a valid '{codecId}' resource.")
                | Some dataset when includeDatamap ->
                    return! enrichFromDatamap codecId context input.Files dataset
                | Some dataset -> return Ok dataset
            with
            | ex -> return Error(error codecId context "The workbook codec failed while reading." |> ProjectErrors.withCause ex)
        }

    let write
        (codecId: string)
        (includeDatamap: bool)
        (serializer: Dataset -> FsSpreadsheet.FsWorkbook)
        (context: CodecContext)
        (dataset: Dataset)
        =
        crossAsync {
            try
                let! primary = serializer dataset |> ProcessCore.Helper.Path.writeXlsxBytesAsync
                if includeDatamap && dataset.DataContexts.Count > 0 then
                    let! datamap =
                        dataset
                        |> ProcessCore.Spreadsheet.Datamap.toFsWorkbook
                        |> ProcessCore.Helper.Path.writeXlsxBytesAsync
                    return Ok { Primary = primary; Files = Map.ofList [ datamapId, datamap ] }
                else
                    return Ok { Primary = primary; Files = Map.empty }
            with
            | ex -> return Error(error codecId context "The workbook codec failed while writing." |> ProjectErrors.withCause ex)
        }

    let investigationId = "isa.investigation.xlsx"
    let studyId = "isa.study.xlsx"
    let assayId = "isa.assay.xlsx"
    let workflowId = "isa.workflow.xlsx"
    let runId = "isa.run.xlsx"

    let investigation: DatasetCodec = {
        Id = investigationId
        ReadAsync =
            read investigationId false (fun context workbook ->
                ProcessCore.ScaffoldReader.Investigation.tryFromFsWorkbook context.CreateDataset workbook)
        WriteAsync = write investigationId false ProcessCore.ScaffoldReader.Investigation.toFsWorkbook
    }

    let study: DatasetCodec = {
        Id = studyId
        ReadAsync = read studyId true (fun _ workbook -> ProcessCore.ScaffoldReader.Study.tryFromFsWorkbook workbook)
        WriteAsync = write studyId true ProcessCore.ScaffoldReader.Study.toFsWorkbook
    }

    let assay: DatasetCodec = {
        Id = assayId
        ReadAsync = read assayId true (fun _ workbook -> ProcessCore.ScaffoldReader.Assay.tryFromFsWorkbook workbook)
        WriteAsync = write assayId true ProcessCore.ScaffoldReader.Assay.toFsWorkbook
    }

    let workflow: DatasetCodec = {
        Id = workflowId
        ReadAsync = read workflowId true (fun _ workbook -> ProcessCore.ScaffoldReader.Workflow.tryFromFsWorkbook workbook)
        WriteAsync = write workflowId true ProcessCore.ScaffoldReader.Workflow.toFsWorkbook
    }

    let run: DatasetCodec = {
        Id = runId
        ReadAsync = read runId true (fun _ workbook -> ProcessCore.ScaffoldReader.Run.tryFromFsWorkbook workbook)
        WriteAsync = write runId true ProcessCore.ScaffoldReader.Run.toFsWorkbook
    }

    let all = [ investigation; study; assay; workflow; run ]

module CodecRegistry =

    let empty = CodecRegistry Map.empty

    let add codec (CodecRegistry codecs) =
        if String.IsNullOrEmpty codec.Id || not (StrictProjectYaml.identifierPattern.IsMatch codec.Id) then
            Error(ProjectErrors.create ProjectErrorKind.Codec "A codec ID must be a valid project identifier." |> ProjectErrors.withCodec codec.Id)
        elif Map.containsKey codec.Id codecs then
            Error(ProjectErrors.create ProjectErrorKind.Codec $"Codec '{codec.Id}' is already registered." |> ProjectErrors.withCodec codec.Id)
        else
            Ok(CodecRegistry(Map.add codec.Id codec codecs))

    let tryFind codecId (CodecRegistry codecs) = Map.tryFind codecId codecs

    let standard =
        StandardProjectCodecs.all
        |> List.fold (fun registry codec ->
            match add codec registry with
            | Ok next -> next
            | Error error -> ProjectErrors.raiseError error) empty

module private ProjectResolution =

    let templateHasCapture template = template.CaptureIndex.IsSome

    let rec private duplicateBy comparer getValue items =
        match items with
        | [] -> None
        | head :: tail ->
            match tail |> List.tryFind (fun item -> comparer (getValue head) (getValue item)) with
            | Some duplicate -> Some(head, duplicate)
            | None -> duplicateBy comparer getValue tail

    let private finishResolution
        (registry: CodecRegistry)
        workspaceRoot
        (project: WorkspaceProject)
        (profiles: WorkspaceProfile list)
        =
        try
            profiles
            |> List.countBy (fun profile -> profile.Id)
            |> List.tryFind (fun (_, count) -> count > 1)
            |> Option.iter (fun (id, _) ->
                ProjectErrors.create ProjectErrorKind.Profile $"Profile id '{id}' is referenced more than once."
                |> ProjectErrors.raiseError)
            let expanded =
                [
                    for profile in profiles do
                        for rule in profile.Rules do
                            yield $"{profile.Id}#{rule.Id}", rule
                    for rule in project.Rules do
                        yield $"project#{rule.Id}", rule
                ]
            expanded
            |> List.countBy fst
            |> List.tryFind (fun (_, count) -> count > 1)
            |> Option.iter (fun (id, _) ->
                ProjectErrors.create ProjectErrorKind.Rule $"Qualified rule id '{id}' is duplicated."
                |> ProjectErrors.withRule id
                |> ProjectErrors.raiseError)
            let rules =
                expanded
                |> List.map (fun (qualifiedId, rule) ->
                    let codec =
                        match CodecRegistry.tryFind rule.Codec registry with
                        | Some codec -> codec
                        | None ->
                            ProjectErrors.create ProjectErrorKind.Codec $"Codec '{rule.Codec}' is not registered."
                            |> ProjectErrors.withRule qualifiedId
                            |> ProjectErrors.withCodec rule.Codec
                            |> ProjectErrors.raiseError
                    {
                        QualifiedId = qualifiedId
                        Codec = codec
                        Target = rule.Target
                        Template = PathTemplates.create rule.Path
                        Files = rule.Files
                    })
            let rootCount = rules |> List.filter (fun rule -> rule.Target = Root) |> List.length
            if rootCount <> 1 then
                ProjectErrors.create ProjectErrorKind.Target $"A resolved project must contain exactly one root rule, but found {rootCount}."
                |> ProjectErrors.raiseError
            let rejectDuplicateTarget targetName chooser =
                rules
                |> List.choose (fun rule -> chooser rule.Target |> Option.map (fun value -> value, rule.QualifiedId))
                |> List.groupBy fst
                |> List.tryFind (fun (_, matches) -> List.length matches > 1)
                |> Option.iter (fun (value, matches) ->
                    let ids = matches |> List.map snd |> String.concat ", "
                    ProjectErrors.create ProjectErrorKind.Target $"Duplicate {targetName} target '{value}' in rules {ids}."
                    |> ProjectErrors.raiseError)
            rejectDuplicateTarget "identifier" (function Identifier value -> Some value | _ -> None)
            rejectDuplicateTarget "additionalType" (function AdditionalType value -> Some value | _ -> None)
            let declaredTemplates =
                rules
                |> List.collect (fun rule ->
                    let directory =
                        ProjectFileSystem.dirname rule.Template.Text
                        |> SafeProjectPath.normalizeRelative
                    let auxiliary =
                        rule.Files
                        |> List.map (fun file ->
                            let path =
                                if String.IsNullOrEmpty directory then file.Path
                                else directory + "/" + file.Path
                            rule, Some file.Id, path)
                    (rule, None, rule.Template.Text) :: auxiliary)
            declaredTemplates
            |> duplicateBy SafeProjectPath.equals (fun (_, _, path) -> path)
            |> Option.iter (fun ((firstRule, firstFile, path), (secondRule, secondFile, _)) ->
                let resourceName rule file =
                    match file with
                    | None -> $"anchor of {rule.QualifiedId}"
                    | Some id -> $"file '{id}' of {rule.QualifiedId}"
                ProjectErrors.create ProjectErrorKind.Path $"{resourceName firstRule firstFile} and {resourceName secondRule secondFile} declare the same path template '{path}'."
                |> ProjectErrors.withAnchor path
                |> ProjectErrors.raiseError)
            let reserved =
                rules
                |> List.choose (fun rule -> match rule.Target with Identifier value -> Some value | _ -> None)
                |> Set.ofList
            Ok {
                WorkspaceRoot = workspaceRoot
                Rules = rules
                ReservedIdentifiers = reserved
            }
        with
        | ProjectException error -> Error error
        | ex -> Error(ProjectErrors.create ProjectErrorKind.Project "Project resolution failed." |> ProjectErrors.withCause ex)

    let private downloadProfile url : CrossAsync<Result<string, ProjectError>> =
        let download : CrossAsync<string> =
            #if FABLE_COMPILER_JAVASCRIPT || FABLE_COMPILER_TYPESCRIPT
            ProcessCore.WebRequest.downloadFile url |> Async.StartAsPromise
            #else
            ProcessCore.WebRequest.downloadFile url
            #endif
        download
        |> CrossAsync.map Ok
        |> CrossAsync.catchWith (fun ex ->
            Error(
                ProjectErrors.create ProjectErrorKind.Profile $"Profile URL '{url}' could not be downloaded."
                |> ProjectErrors.withAnchor url
                |> ProjectErrors.withCause ex
            ))

    let private parseProfile source text =
        match WorkspaceProfile.parse text with
        | Ok profile -> profile
        | Error error -> ProjectErrors.raiseError { error with Anchor = Some source }

    let resolve (registry: CodecRegistry) workspaceRoot : CrossAsync<Result<ResolvedProject, ProjectError>> =
        crossAsync {
            try
                let workspaceRoot = ProjectFileSystem.fullPath workspaceRoot ""
                let projectRelative = ".arc/project.yml"
                let projectPath =
                    match SafeProjectPath.resolve workspaceRoot projectRelative with
                    | Ok path -> path
                    | Error error -> ProjectErrors.raiseError error
                if not (ProjectFileSystem.isFile projectPath) then
                    ProjectErrors.create ProjectErrorKind.Project $"No project file exists at '{projectRelative}'."
                    |> ProjectErrors.withAnchor projectRelative
                    |> ProjectErrors.raiseError
                let project =
                    match WorkspaceProject.parse (ProjectFileSystem.readAllText projectPath) with
                    | Ok project -> project
                    | Error error -> ProjectErrors.raiseError error
                let arcDirectory = ProjectFileSystem.dirname projectPath
                let profiles = ResizeArray<WorkspaceProfile>()
                for profileReference in project.WorkspaceProfiles do
                    match profileReference with
                    | WorkspaceProfileReference.File relative ->
                        let path =
                            match SafeProjectPath.resolve arcDirectory relative with
                            | Ok path -> path
                            | Error error ->
                                ProjectErrors.raiseError {
                                    error with
                                        Kind = ProjectErrorKind.Profile
                                        Anchor = Some relative
                                }
                        if not (ProjectFileSystem.isFile path) then
                            ProjectErrors.create ProjectErrorKind.Profile $"Profile file '{relative}' does not exist."
                            |> ProjectErrors.withAnchor relative
                            |> ProjectErrors.raiseError
                        profiles.Add(parseProfile relative (ProjectFileSystem.readAllText path))
                    | WorkspaceProfileReference.Url url ->
                        let! downloaded = downloadProfile url
                        match downloaded with
                        | Ok text -> profiles.Add(parseProfile url text)
                        | Error error -> ProjectErrors.raiseError error
                return finishResolution registry workspaceRoot project (List.ofSeq profiles)
            with
            | ProjectException error -> return Error error
            | ex ->
                return
                    Error(
                        ProjectErrors.create ProjectErrorKind.Project "Project resolution failed."
                        |> ProjectErrors.withCause ex
                    )
        }

module private ProjectPreparation =

    let targetOrdinal = function Root -> 0 | Identifier _ -> 1 | AdditionalType _ -> 2

    let sortBindings (bindings: PreparedBinding list) =
        bindings
        |> List.sortBy (fun binding ->
            targetOrdinal binding.Rule.Target,
            (match binding.Rule.Target with Root -> "" | Identifier value | AdditionalType value -> value),
            binding.RelativeAnchor,
            binding.Rule.QualifiedId)

    let allFiles root =
        try Ok(ProjectFileSystem.enumerateFilesWithoutFollowingLinks root)
        with
        | ex -> Error(ProjectErrors.create ProjectErrorKind.Resource "Workspace resources could not be enumerated." |> ProjectErrors.withCause ex)

    let createBinding
        (project: ResolvedProject)
        (rule: ResolvedRule)
        (anchor: string)
        (relativeAnchor: string)
        (capturedIdentifier: string option)
        (dataset: Dataset option)
        : PreparedBinding
        =
        let directory =
            ProjectFileSystem.dirname relativeAnchor
            |> SafeProjectPath.normalizeRelative
        let files =
            rule.Files
            |> List.map (fun (definition: StorageFile) ->
                let relative =
                    if String.IsNullOrEmpty directory then definition.Path
                    else directory + "/" + definition.Path
                let path =
                    match SafeProjectPath.resolve project.WorkspaceRoot relative with
                    | Ok value -> value
                    | Error error ->
                        ProjectErrors.raiseError {
                            error with
                                RuleId = Some rule.QualifiedId
                                CodecId = Some rule.Codec.Id
                                Anchor = Some relative
                        }
                {
                    Definition = definition
                    Path = path
                    RelativePath = relative
                })
        {
            Rule = rule
            Anchor = anchor
            RelativeAnchor = relativeAnchor
            Files = files
            CapturedIdentifier = capturedIdentifier
            Dataset = dataset
        }

    let discover (project: ResolvedProject) (rule: ResolvedRule) files =
        files
        |> List.choose (fun path ->
            let relative = SafeProjectPath.relativeTo project.WorkspaceRoot path
            match PathTemplates.tryMatch relative rule.Template with
            | None -> None
            | Some captured ->
                match SafeProjectPath.resolve project.WorkspaceRoot relative with
                | Error error -> ProjectErrors.raiseError { error with RuleId = Some rule.QualifiedId; Anchor = Some relative }
                | Ok anchor ->
                    Some(createBinding project rule anchor relative captured None))

    let checkCollisions (bindings: PreparedBinding list) =
        let rec duplicate items =
            match items with
            | [] -> None
            | head :: tail ->
                let _, _, headPath = head
                match tail |> List.tryFind (fun (_, _, path) -> SafeProjectPath.equals headPath path) with
                | Some second -> Some(head, second)
                | None -> duplicate tail
        let resources =
            bindings
            |> List.collect (fun binding ->
                let anchor = binding.Rule.QualifiedId, None, binding.Anchor
                let files =
                    binding.Files
                    |> List.map (fun file -> binding.Rule.QualifiedId, Some file.Definition.Id, file.Path)
                anchor :: files)
        match duplicate resources with
            | Some ((firstRule, firstFile, path), (secondRule, secondFile, _)) ->
                let resourceName rule file =
                    match file with
                    | None -> $"anchor of {rule}"
                    | Some id -> $"file '{id}' of {rule}"
                Error(
                    ProjectErrors.create ProjectErrorKind.Path $"{resourceName firstRule firstFile} and {resourceName secondRule secondFile} resolve to the same path."
                    |> ProjectErrors.withAnchor path
                )
            | None -> Ok(sortBindings bindings)

    let prepareRead project =
        try
            match allFiles project.WorkspaceRoot with
            | Error error -> Error error
            | Ok files ->
                let bindings =
                    project.Rules
                    |> List.collect (fun rule ->
                        match rule.Target, rule.Template.CaptureIndex with
                        | (Root | Identifier _), None ->
                            let anchor =
                                match SafeProjectPath.resolve project.WorkspaceRoot rule.Template.Text with
                                | Ok path -> path
                                | Error error ->
                                    ProjectErrors.raiseError { error with RuleId = Some rule.QualifiedId; Anchor = Some rule.Template.Text }
                            if not (ProjectFileSystem.isFile anchor) then
                                ProjectErrors.create ProjectErrorKind.Resource $"Required resource '{rule.Template.Text}' does not exist."
                                |> ProjectErrors.withRule rule.QualifiedId
                                |> ProjectErrors.withCodec rule.Codec.Id
                                |> ProjectErrors.withAnchor rule.Template.Text
                                |> ProjectErrors.raiseError
                            [ createBinding project rule anchor rule.Template.Text None None ]
                        | (Root | Identifier _), Some _ ->
                            let matches = discover project rule files
                            if List.length matches <> 1 then
                                ProjectErrors.create ProjectErrorKind.Resource $"Rule '{rule.QualifiedId}' must resolve to exactly one resource, but found {List.length matches}."
                                |> ProjectErrors.withRule rule.QualifiedId
                                |> ProjectErrors.withCodec rule.Codec.Id
                                |> ProjectErrors.withAnchor rule.Template.Text
                                |> ProjectErrors.raiseError
                            matches
                        | AdditionalType _, _ ->
                            discover project rule files
                            |> List.filter (fun binding ->
                                binding.CapturedIdentifier
                                |> Option.forall (fun identifier -> not (Set.contains identifier project.ReservedIdentifiers))))
                checkCollisions bindings
        with
        | ProjectException error -> Error error
        | ex -> Error(ProjectErrors.create ProjectErrorKind.Project "Read preparation failed." |> ProjectErrors.withCause ex)

    let prepareWrite project (root: Dataset) =
        try
            let bindings =
                project.Rules
                |> List.collect (fun rule ->
                    let selected =
                        match rule.Target with
                        | Root -> [ root ]
                        | Identifier identifier ->
                            match root.HasPart |> Seq.tryFind (fun dataset -> dataset.Identifier = identifier) with
                            | Some dataset -> [ dataset ]
                            | None ->
                                ProjectErrors.create ProjectErrorKind.Target $"Required direct child '{identifier}' does not exist."
                                |> ProjectErrors.withRule rule.QualifiedId
                                |> ProjectErrors.raiseError
                        | AdditionalType additionalType ->
                            root.HasPart
                            |> Seq.filter (fun dataset ->
                                dataset.AdditionalType = Some additionalType
                                && not (Set.contains dataset.Identifier project.ReservedIdentifiers))
                            |> Seq.toList
                    selected
                    |> List.map (fun dataset ->
                        let relative =
                            match PathTemplates.render dataset.Identifier rule.Template with
                            | Ok value -> value
                            | Error error ->
                                ProjectErrors.raiseError {
                                    error with
                                        RuleId = Some rule.QualifiedId
                                        CodecId = Some rule.Codec.Id
                                        Anchor = Some rule.Template.Text
                                }
                        let anchor =
                            match SafeProjectPath.resolve project.WorkspaceRoot relative with
                            | Ok value -> value
                            | Error error ->
                                ProjectErrors.raiseError {
                                    error with
                                        RuleId = Some rule.QualifiedId
                                        CodecId = Some rule.Codec.Id
                                        Anchor = Some relative
                                }
                        createBinding
                            project
                            rule
                            anchor
                            relative
                            (rule.Template.CaptureIndex |> Option.map (fun _ -> dataset.Identifier))
                            (Some dataset)))
            checkCollisions bindings
        with
        | ProjectException error -> Error error
        | ex -> Error(ProjectErrors.create ProjectErrorKind.Project "Write preparation failed." |> ProjectErrors.withCause ex)

module internal ProjectRuntime =

    let private context createDataset (binding: PreparedBinding) : CodecContext =
        {
            RelativeAnchor = binding.RelativeAnchor
            CreateDataset = createDataset
        }

    let private resourceFailure (binding: PreparedBinding) message ex =
        Error(
            ProjectErrors.create ProjectErrorKind.Resource message
            |> ProjectErrors.withRule binding.Rule.QualifiedId
            |> ProjectErrors.withCodec binding.Rule.Codec.Id
            |> ProjectErrors.withAnchor binding.RelativeAnchor
            |> ProjectErrors.withCause ex
        )

    let private readInput (binding: PreparedBinding) : CrossAsync<Result<CodecInput, ProjectError>> =
        crossAsync {
            try
                let! primary = ProcessCore.Helper.Path.readFileBinaryAsync binding.Anchor
                let mutable files = Map.empty
                for file in binding.Files do
                    let! exists = ProcessCore.Helper.Path.fileExistsAsync file.Path
                    if exists then
                        let! bytes = ProcessCore.Helper.Path.readFileBinaryAsync file.Path
                        files <- Map.add file.Definition.Id bytes files
                return Ok { Primary = primary; Files = files }
            with
            | ex -> return resourceFailure binding "Declared resources could not be read." ex
        }

    let private invokeRead createDataset (binding: PreparedBinding) =
        let codecFailure ex =
            Error(
                ProjectErrors.create ProjectErrorKind.Codec "A codec threw while reading a Dataset."
                |> ProjectErrors.withRule binding.Rule.QualifiedId
                |> ProjectErrors.withCodec binding.Rule.Codec.Id
                |> ProjectErrors.withAnchor binding.RelativeAnchor
                |> ProjectErrors.withCause ex
            )
        crossAsync {
            let! input = readInput binding
            match input with
            | Error error -> return Error error
            | Ok input ->
                try
                    return!
                        binding.Rule.Codec.ReadAsync (context createDataset binding) input
                        |> CrossAsync.catchWith codecFailure
                with
                | ex -> return codecFailure ex
        }

    let private writeOutput (binding: PreparedBinding) (output: CodecOutput) =
        crossAsync {
            try
                let codecFiles =
                    binding.Files
                    |> List.choose (fun file ->
                        match file.Definition.Create with
                        | None -> Some(file.Definition.Id, file)
                        | Some StorageFileCreation.Empty -> None)
                    |> Map.ofList
                let undeclared =
                    output.Files
                    |> Map.toList
                    |> List.tryFind (fun (id, _) -> not (Map.containsKey id codecFiles))
                match undeclared with
                | Some (id, _) ->
                    return
                        Error(
                            ProjectErrors.create ProjectErrorKind.Resource $"Codec returned undeclared or project-managed file '{id}'."
                            |> ProjectErrors.withRule binding.Rule.QualifiedId
                            |> ProjectErrors.withCodec binding.Rule.Codec.Id
                            |> ProjectErrors.withAnchor binding.RelativeAnchor
                        )
                | None ->
                    do! ProcessCore.Helper.Path.ensureDirectoryOfFileAsync binding.Anchor
                    do! ProcessCore.Helper.Path.writeFileBinaryAsync binding.Anchor output.Primary
                    for KeyValue(id, bytes) in output.Files do
                        let file = Map.find id codecFiles
                        do! ProcessCore.Helper.Path.ensureDirectoryOfFileAsync file.Path
                        do! ProcessCore.Helper.Path.writeFileBinaryAsync file.Path bytes
                    for file in binding.Files do
                        match file.Definition.Create with
                        | Some StorageFileCreation.Empty ->
                            do! ProcessCore.Helper.Path.ensureDirectoryOfFileAsync file.Path
                            do! ProcessCore.Helper.Path.writeFileBinaryAsync file.Path [||]
                        | None -> ()
                    return Ok()
            with
            | ex -> return resourceFailure binding "Declared resources could not be written." ex
        }

    let private invokeWrite (binding: PreparedBinding) dataset =
        let codecFailure ex =
            Error(
                ProjectErrors.create ProjectErrorKind.Codec "A codec threw while writing a Dataset."
                |> ProjectErrors.withRule binding.Rule.QualifiedId
                |> ProjectErrors.withCodec binding.Rule.Codec.Id
                |> ProjectErrors.withAnchor binding.RelativeAnchor
                |> ProjectErrors.withCause ex
            )
        crossAsync {
            let! encoded =
                try
                    binding.Rule.Codec.WriteAsync (context (fun id -> Dataset(id)) binding) dataset
                    |> CrossAsync.catchWith codecFailure
                with
                | ex -> crossAsync { return codecFailure ex }
            match encoded with
            | Error error -> return Error error
            | Ok output -> return! writeOutput binding output
        }

    let private validateRead binding (dataset: Dataset) =
        let fail message =
            Error(
                ProjectErrors.create ProjectErrorKind.Resource message
                |> ProjectErrors.withRule binding.Rule.QualifiedId
                |> ProjectErrors.withCodec binding.Rule.Codec.Id
                |> ProjectErrors.withAnchor binding.RelativeAnchor
            )
        match binding.CapturedIdentifier with
        | Some captured when dataset.Identifier <> captured ->
            fail $"Dataset identifier '{dataset.Identifier}' does not match captured identifier '{captured}'."
        | _ ->
            match binding.Rule.Target with
            | Identifier expected when dataset.Identifier <> expected ->
                fail $"Dataset identifier '{dataset.Identifier}' does not match target identifier '{expected}'."
            | AdditionalType expected when dataset.AdditionalType <> Some expected ->
                fail $"Dataset '{dataset.Identifier}' does not have required additionalType '{expected}'."
            | _ -> Ok dataset

    let loadAsync createRoot registry workspaceRoot : CrossAsync<Result<Dataset, ProjectError>> =
        crossAsync {
            let! resolved = ProjectResolution.resolve registry workspaceRoot
            match resolved with
            | Error error -> return Error error
            | Ok project ->
                match ProjectPreparation.prepareRead project with
                | Error error -> return Error error
                | Ok bindings ->
                    let rootBinding = bindings |> List.find (fun binding -> binding.Rule.Target = Root)
                    let! rootResult = invokeRead createRoot rootBinding
                    match rootResult |> Result.bind (validateRead rootBinding) with
                    | Error error -> return Error error
                    | Ok root ->
                        let childBindings = bindings |> List.filter (fun binding -> binding.Rule.Target <> Root)
                        let mutable failure = None
                        for binding in childBindings do
                            if failure.IsNone then
                                let! childResult = invokeRead (fun id -> Dataset(id)) binding
                                match childResult |> Result.bind (validateRead binding) with
                                | Error error -> failure <- Some error
                                | Ok child ->
                                    try root.AddPart(child)
                                    with
                                    | ex ->
                                        failure <-
                                            Some(
                                                ProjectErrors.create ProjectErrorKind.Resource $"Dataset '{child.Identifier}' could not be attached to the ARC root."
                                                |> ProjectErrors.withRule binding.Rule.QualifiedId
                                                |> ProjectErrors.withCodec binding.Rule.Codec.Id
                                                |> ProjectErrors.withAnchor binding.RelativeAnchor
                                                |> ProjectErrors.withCause ex
                                            )
                        match failure with
                        | Some error -> return Error error
                        | None -> return Ok root
        }

    let writeAsync registry workspaceRoot (root: Dataset) : CrossAsync<Result<unit, ProjectError>> =
        crossAsync {
            let! resolved = ProjectResolution.resolve registry workspaceRoot
            match resolved with
            | Error error -> return Error error
            | Ok project ->
                match ProjectPreparation.prepareWrite project root with
                | Error error -> return Error error
                | Ok bindings ->
                    let mutable failure = None
                    for binding in bindings do
                        if failure.IsNone then
                            let dataset = binding.Dataset |> Option.get
                            let! result = invokeWrite binding dataset
                            match result with
                            | Ok () -> ()
                            | Error error -> failure <- Some error
                    return
                        match failure with
                        | Some error -> Error error
                        | None -> Ok()
        }
