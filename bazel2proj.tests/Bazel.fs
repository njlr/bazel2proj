module Bazel2Proj.Tests.Bazel

open Expecto
open Bazel2Proj
open Bazel2Proj.Bazel

let tests =
  testList "Bazel" [
    test "parseLabelKind 1" {
      let content =
        [
          "fsharp_binary rule //app:app"
          "fsharp_library rule //app:lib"
        ]
        |> String.concat "\n"

      let actual = parseLabelKind content

      let expected =
        [
          {
            Kind = "fsharp_binary"
            Type = "rule"
            Label = "//app:app"
          }
          {
            Kind = "fsharp_library"
            Type = "rule"
            Label = "//app:lib"
          }
        ]

      Expect.equal actual expected ""
    }
  ]
