module ProcessCore.Tests.Synchronization

open Fable.Pyxpecto

open ProcessCore

let tests =
    testList
        "synchronization"
        [
            testCase "placeholder test" (fun _ ->

                let p1 = LabProcess("Process1")
                Expect.isTrue true "Placeholder test should pass."
            )
        ]