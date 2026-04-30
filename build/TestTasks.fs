module TestTasks

open BlackFox.Fake
open Fake.DotNet

open ProjectInfo
open BasicTasks

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
        printfn "Skipping TranspileJs: Fable JavaScript tooling will be wired after connector/package choices are made."
    }

let testJs =
    BuildTask.create "TestJs" [ transpileJs ] {
        printfn "Skipping TestJs: JavaScript test execution is not wired yet."
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
