module DocumentationTasks

open Helpers
open ProjectInfo
open BasicTasks

open BlackFox.Fake

let buildDocs =
    BuildTask.create "BuildDocs" [ buildSolution ] {
        printfn "building docs with stable version %s" versionController.StableVersionTag
        runDotNet (sprintf "fsdocs build --output docs/output --eval --clean --properties Configuration=%s --parameters fsdocs-package-version %s" configuration versionController.StableVersionTag) "./"
    }

// let buildDocsPrerelease =
//     BuildTask.create "BuildDocsPrerelease" [ setPrereleaseTag; buildSolution ] {
//         printfn "building docs with prerelease version %s" prereleaseTag
//         runDotNet (sprintf "fsdocs build --output docs/output --eval --clean --properties Configuration=%s --parameters fsdocs-package-version %s" configuration prereleaseTag) "./"
//     }

let watchDocs =
    BuildTask.create "WatchDocs" [ buildSolution ] {
        printfn "watching docs with stable version %s" versionController.StableVersionTag
        runDotNet (sprintf "fsdocs watch --eval --clean --properties Configuration=%s --parameters fsdocs-package-version %s" configuration versionController.StableVersionTag) "./"
    }

// let watchDocsPrerelease =
//     BuildTask.create "WatchDocsPrerelease" [ setPrereleaseTag; buildSolution ] {
//         printfn "watching docs with prerelease version %s" prereleaseTag
//         runDotNet (sprintf "fsdocs watch --eval --clean --properties Configuration=%s --parameters fsdocs-package-version %s" configuration prereleaseTag) "./"
//     }
