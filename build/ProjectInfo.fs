module ProjectInfo

open Fake.Core

let project = "ARC-Data-Model"

let pyxpectoTestProjects =
    [
        "tests/ProcessCore.Tests/ProcessCore.Tests.fsproj"
        "tests/ProcessCore.YAML.Tests/ProcessCore.YAML.Tests.fsproj"
        "tests/ProcessCore.SQL.Tests/ProcessCore.SQL.Tests.fsproj"
    ]

let jsPyxpectoTestProject = "tests/ProcessCore.SQL.Tests/ProcessCore.SQL.Tests.fsproj"

let jsTestOutputDir = "build/out/js-tests"

let pyPyxpectoTestProject = "tests/ProcessCore.SQL.Tests/ProcessCore.SQL.Tests.fsproj"

let pyTestOutputDir = "build/out/py-tests"

let solutionFile = $"{project}.sln"

let configuration = "Release"

let gitOwner = "HLWeil"

let gitHome = $"https://github.com/{gitOwner}"

let projectRepo = $"https://github.com/{gitOwner}/{project}"

let pkgDir = "pkg"

// Create RELEASE_NOTES.md if not existing. Or "release" would throw an error.
Fake.Extensions.Release.ReleaseNotes.ensure()

let release = ReleaseNotes.load "RELEASE_NOTES.md"

let stableVersion = SemVer.parse release.NugetVersion

let stableVersionTag = sprintf "%i.%i.%i" stableVersion.Major stableVersion.Minor stableVersion.Patch

let mutable prereleaseSuffix = ""

let mutable prereleaseTag = ""

let mutable isPrerelease = false
