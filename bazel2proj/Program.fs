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

  let decode : Decoder<ProjectInfo> =
    Decode.map2
      (fun bfp srcs ->
        {
          BuildFilePath = bfp
          Srcs = srcs
        })
      (Decode.field "build_file_path" Decode.string)
      (Decode.field "srcs" (Decode.list Decode.string))

let private labelName (label : string) =
  label.Split([| ':' |]) |> Seq.last

let private aspectOutputPath (label : string) =
  let name = labelName label
  $"bazel-bin/bazel2proj_{name}.json"

[<EntryPoint>]
let main argv =
  async {
    let workspacePath =
      argv
      |> Array.tryItem 0
      |> Option.defaultValue (Directory.GetCurrentDirectory())

    let! labelKinds = Bazel.findTargets workspacePath

    printfn "Found the following targets: "
    for labelKind in labelKinds do
      printfn $"  {labelKind.Label}"

    for labelKind in labelKinds do
      do! Bazel.buildAspect workspacePath labelKind.Label
      printfn $"Built aspect for {labelKind.Label}"

    printfn "Reading aspect outputs..."

    for labelKind in labelKinds do
      let aspectOutputPath = aspectOutputPath labelKind.Label

      let! content =
        File.ReadAllTextAsync(Path.Combine(workspacePath, aspectOutputPath))
        |> Async.AwaitTask

      let projectInfo =
        content
        |> Decode.unsafeFromString ProjectInfo.decode

      printfn "%A" projectInfo

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
                  let f = Path.GetRelativePath(workspacePath, f)
                  Xml.element "Compile" [ Xml.stringAttr "Include" f ] []
              ]
          ]

      let target = Path.Combine(workspacePath, labelName labelKind.Label + ".fsproj")

      do!
        File.WriteAllTextAsync(target, Xml.toString xml)
        |> Async.AwaitTask

      printfn $"Wrote {target}"

      ()
    ()
  }
  |> Async.RunSynchronously

  // let jsonPath = argv[0]

  // let content = File.ReadAllText(jsonPath)

  // let projectInfo =
  //   content
  //   |> Decode.unsafeFromString ProjectInfo.decode

  // let root = Path.GetDirectoryName(projectInfo.BuildFilePath)

  // let xml =
  //   Xml.element
  //     "Project"
  //     [ Xml.stringAttr "Sdk" "Microsoft.NET.Sdk" ]
  //     [
  //       Xml.element
  //         "PropertyGroup"
  //         []
  //         [
  //           Xml.stringElement "OutputType" "Exe"
  //           Xml.stringElement "TargetFramework" "net6.0"
  //         ]

  //       Xml.element
  //         "ItemGroup"
  //         []
  //         [
  //           for f in projectInfo.Srcs do
  //             let f = Path.GetRelativePath(root, f)
  //             Xml.element "Compile" [ Xml.stringAttr "Include" f ] []
  //         ]
  //     ]

  // printfn "%s" (Xml.toString xml)

  // let target = Path.Combine(root, "foo.fsproj")

  // File.WriteAllText(target, Xml.toString xml)

  0
