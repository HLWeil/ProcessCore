module ProcessCore.Helper.Path


let [<Literal>] PathSeperator = '/'
let [<Literal>] PathSeperatorWindows = '\\'
let seperators = [|PathSeperator; PathSeperatorWindows|]

let split(path: string) =
    path.Split(seperators, enum<System.StringSplitOptions>(3))
    |> Array.filter (fun p -> p <> "" && p <> ".")

let combine (path1 : string) (path2 : string) : string =
    let path1_trimmed = path1.TrimEnd(seperators)
    let path2_trimmed = path2.TrimStart(seperators)
    let combined = path1_trimmed + string PathSeperator + path2_trimmed
    combined // should we trim any excessive path seperators?

let combineMany (paths : string seq) : string =
    paths
    |> Seq.filter (fun p -> not (System.String.IsNullOrWhiteSpace p))
    |> Seq.reduce combine

let getFileName (path : string) : string =
    #if FABLE_COMPILER_PYTHON
    emitPyExpr (path) "Path(path).name"
    #else
    System.IO.Path.GetFileName path
    #endif

let getExtension (path : string) : string =
    #if FABLE_COMPILER_PYTHON
    emitPyExpr (path) "Path(path).suffix"
    #else
    System.IO.Path.GetExtension path
    #endif


// Files
let [<Literal>] DatamapFileName = "isa.datamap.xlsx"
let [<Literal>] AssayFileName = "isa.assay.xlsx"
let [<Literal>] StudyFileName = "isa.study.xlsx"
let [<Literal>] WorkflowFileName = "isa.workflow.xlsx"
let [<Literal>] WorkflowCWLFileName = "workflow.cwl"
let [<Literal>] RunFileName = "isa.run.xlsx"
let [<Literal>] RunCWLFileName = "run.cwl"
let [<Literal>] RunYMLFileName = "run.yml"
let [<Literal>] InvestigationFileName = "isa.investigation.xlsx"
let [<Literal>] GitKeepFileName = ".gitkeep"
let [<Literal>] READMEFileName = "README.md"
let [<Literal>] ValidationPackagesYamlFileName = "validation_packages.yml"
let [<Literal>] LICENSEFileName = "LICENSE"
let alternativeLICENSEFileNames = ["LICENSE.txt"; "LICENSE.md"; "LICENSE.rst"]



// Folder
let [<Literal>] ARCConfigFolderName = ".arc"
let [<Literal>] AssaysFolderName = "assays"
let [<Literal>] StudiesFolderName = "studies"
let [<Literal>] WorkflowsFolderName = "workflows"
let [<Literal>] RunsFolderName = "runs"
let [<Literal>] AssayProtocolsFolderName = "protocols"
let [<Literal>] AssayDatasetFolderName = "dataset"
let [<Literal>] StudiesProtocolsFolderName = "protocols"
let [<Literal>] StudiesResourcesFolderName = "resources"

