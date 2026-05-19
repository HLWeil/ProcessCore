module ProjectInfo

open Fake.Core

let project = "ProcessCore"

let testProject = "tests/ProcessCore.Tests"

let solutionFile = $"{project}.sln"

let configuration = "Release"

let gitOwner = "HLWeil"

let gitHome = $"https://github.com/{gitOwner}"

let projectRepo = $"https://github.com/{gitOwner}/{project}"

let netPkgDir = "./dist/net"
let npmPkgDir = "./dist/ts"
let pyPkgDir = "./dist/py"

// Create RELEASE_NOTES.md if not existing. Or "release" would throw an error.
Fake.Extensions.Release.ReleaseNotes.ensure()

let release = ReleaseNotes.load "RELEASE_NOTES.md"

let stableVersion = SemVer.parse release.NugetVersion

let stableVersionTag = sprintf "%i.%i.%i" stableVersion.Major stableVersion.Minor stableVersion.Patch

let mutable prereleaseSuffix = ""

let mutable prereleaseTag = ""

let mutable isPrerelease = false
