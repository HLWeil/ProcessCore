module ProcessCore.SQL.Tests.All

open Fable.Pyxpecto

let all =
    testList
        "ProcessCore.SQL"
        [
            TableModelTests.tests
            RowCodecTests.tests
            DotNetDriverTests.tests
            RepositoryCrudTests.tests
        ]
