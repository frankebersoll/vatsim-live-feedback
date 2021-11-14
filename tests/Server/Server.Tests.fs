module Server.Tests

open Expecto
open Shared


[<Tests>]
let server = testList "Data" []

let all =
    testList "All"
        [
            Tests.shared
            server
        ]

[<EntryPoint>]
let main _ = runTestsWithCLIArgs [] [||] all