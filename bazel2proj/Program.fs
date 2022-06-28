module Bazel2Proj.Program

open System
open System.IO
open Thoth.Json.Net

type ProjectInfo =
  {
    BuildFilePath : string
    Srcs : string list
  }

module ProjectInfo =

  let decode =
    Decode.map2
      (fun bfp srcs ->
        {
          BuildFilePath = bfp
          Srcs = srcs
        })
      (Decode.field "build_file_path" Decode.string)
      (Decode.field "srcs" (Decode.list Decode.string))

[<EntryPoint>]
let main argv =
  let jsonPath = argv[0]

  let content = File.ReadAllText(jsonPath)

  let projectInfo =
    content
    |> Decode.unsafeFromString ProjectInfo.decode

  let root = Path.GetDirectoryName(projectInfo.BuildFilePath)

  let xml =
    Xml.element
      "Project"
      [ Xml.stringAttr "Sdk" "Microsoft.NET.Sdk" ]
      [
        Xml.element
          "PropertyGroup"
          []
          [
            Xml.stringElement "OutputType" "Exe"
            Xml.stringElement "TargetFramework" "net6.0"
          ]

        Xml.element
          "ItemGroup"
          []
          [
            for f in projectInfo.Srcs do
              let f = Path.GetRelativePath(root, f)
              Xml.element "Compile" [ Xml.stringAttr "Include" f ] []
          ]
      ]

  printfn "%s" (Xml.toString xml)

  let target = Path.Combine(root, "foo.fsproj")

  File.WriteAllText(target, Xml.toString xml)

  0
