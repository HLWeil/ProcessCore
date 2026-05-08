module TestTasks

open System.IO
open BlackFox.Fake
open Fake.Core
open Fake.DotNet

open ProjectInfo
open BasicTasks

let private runTool command args =
    if System.OperatingSystem.IsWindows() then
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
        ]
        |> String.concat " "

    let result = DotNet.exec id "run" args

    if not result.OK then
        failwithf "Pyxpecto tests failed for %s" project

let private transpileFable project outputDir language =
    run dotnet $"fable {project} --lang {language} --noCache -o {outputDir}" ""

let private transpileJavaScriptTests () =
    let installPackages = runTool "npm" [ "install" ]

    if installPackages.ExitCode <> 0 then
        failwith "npm install failed"

    transpileFable jsPyxpectoTestProject jsTestOutputDir "JavaScript"

let private runJavaScriptTests () =
    let testFile = $"{jsTestOutputDir}/Main.js"
    let result = runTool "node" [ testFile; "--fail-on-focused-tests" ]

    if result.ExitCode <> 0 then
        failwith "JavaScript Pyxpecto tests failed"

let private transpilePythonTests () =
    transpileFable pyPyxpectoTestProject pyTestOutputDir "Python"

let private runPythonTests () =
    let testFile =
        [ "Main.py"; "main.py" ]
        |> List.map (fun fileName -> Path.Combine(pyTestOutputDir, fileName))
        |> List.tryFind File.Exists
        |> Option.defaultWith (fun () -> failwith $"Could not find Python test entrypoint in {pyTestOutputDir}.")

    let result = runTool "uv" [ "run"; "python"; testFile; "--fail-on-focused-tests" ]

    if result.ExitCode <> 0 then
        failwith "Python Pyxpecto tests failed"

let runTests =
    BuildTask.create "RunTests" [ clean; buildSolution ] {
        pyxpectoTestProjects
        |> Seq.iter runPyxpectoDotNet
    }

let transpileJs =
    BuildTask.create "TranspileJs" [] {
        transpileJavaScriptTests ()
    }

let testJs =
    BuildTask.create "TestJs" [ transpileJs ] {
        runJavaScriptTests ()
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
        transpilePythonTests ()
    }


let testPy =
    BuildTask.create "TestPy" [ transpilePy ] {
        runPythonTests ()
    }

let runTestsAll =
    BuildTask.create "RunTestsAll" [ clean; buildSolution ] {
        pyxpectoTestProjects
        |> Seq.iter runPyxpectoDotNet

        transpileJavaScriptTests ()
        runJavaScriptTests ()

        transpilePythonTests ()
        runPythonTests ()
    }
