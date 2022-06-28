module Bazel2Proj.Tests.Program

open Expecto
open Bazel2Proj.Tests

let tests =
  testList "Bazel2Proj" [
    Bazel.tests
  ]

[<EntryPoint>]
let main args =
  runTestsWithCLIArgs [] args tests
