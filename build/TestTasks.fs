module TestTasks

open System
open BlackFox.Fake
open Fake.Core
open Fake.DotNet

open ProjectInfo
open BasicTasks

let private runTool command args =
    if OperatingSystem.IsWindows() then
        CreateProcess.fromRawCommand "cmd.exe" (["/c"; command] @ args)
        |> Proc.run
    else
        CreateProcess.fromRawCommand command args
        |> Proc.run

let private runPyxpectoDotNet project =
    let args =
        [
            "--project"
            $"\"{project}\""
            "--configuration"
            configuration
            "--no-build"
            "--"
            "--fail-on-focused-tests"
        ]
        |> String.concat " "

    let result = DotNet.exec id "run" args

    if not result.OK then
        failwithf "Pyxpecto tests failed for %s" project

let runTests =
    BuildTask.create "RunTests" [ clean; buildSolution ] {
        pyxpectoTestProjects
        |> Seq.iter runPyxpectoDotNet
    }

let transpileJs =
    BuildTask.create "TranspileJs" [] {
        let restoreTools = DotNet.exec id "tool" "restore"

        if not restoreTools.OK then
            failwith "dotnet tool restore failed"

        let installPackages = runTool "npm" [ "install" ]

        if installPackages.ExitCode <> 0 then
            failwith "npm install failed"

        let args =
            [
                "fable"
                jsPyxpectoTestProject
                "-o"
                jsTestOutputDir
                "--lang"
                "JavaScript"
            ]
            |> String.concat " "

        let transpile = DotNet.exec id "tool" $"run {args}"

        if not transpile.OK then
            failwith "Fable JavaScript transpilation failed"
    }

let testJs =
    BuildTask.create "TestJs" [ transpileJs ] {
        let testFile = $"{jsTestOutputDir}/Main.js"
        let result = runTool "node" [ testFile; "--fail-on-focused-tests" ]

        if result.ExitCode <> 0 then
            failwith "JavaScript Pyxpecto tests failed"
    }

let transpileTs =
    BuildTask.create "TranspileTs" [] {
        printfn "Skipping TranspileTs: Fable TypeScript tooling will be wired after connector/package choices are made."
    }

let testTs =
    BuildTask.create "TestTs" [ transpileTs ] {
        printfn "Skipping TestTs: TypeScript test execution is not wired yet."
    }

let transpilePy =
    BuildTask.create "TranspilePy" [] {
        printfn "Skipping TranspilePy: Fable Python tooling will be wired after connector/package choices are made."
    }

let testPy =
    BuildTask.create "TestPy" [ transpilePy ] {
        printfn "Skipping TestPy: Python test execution is not wired yet."
    }
