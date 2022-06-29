module Bazel2Proj.Program

open System
open System.IO
open Thoth.Json.Net

type ProjectInfo =
  {
    BuildFilePath : string
    Srcs : string list
    Deps : string list
  }

module ProjectInfo =

  let decode : Decoder<ProjectInfo> =
    Decode.map3
      (fun bfp srcs deps ->
        {
          BuildFilePath = bfp
          Srcs = srcs
          Deps = deps
        })
      (Decode.field "build_file_path" Decode.string)
      (Decode.field "srcs" (Decode.list Decode.string))
      (Decode.field "deps" (Decode.list Decode.string))

let private labelName (label : string) =
  label.Split([| ':' |]) |> Seq.last

let private labelPath (label : string) =
  label.Split([| ':' |])
  |> Seq.head
  |> fun x -> if x.StartsWith("//") then x.Substring(2) else x

let fsprojPath (label : string) =
  (if label.StartsWith("//") then
    label.Substring(2)
  else
    label)
  |> fun x -> x.Split([| ':' |])
  |> String.concat "/"
  |> fun x -> x + ".fsproj"

let private aspectOutputPath (label : string) =
  let name = labelName label
  let path = labelPath label
  Path.Combine("bazel-bin", path, $"bazel2proj_{name}.json")

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

    let! allTargets =
      [
        for labelKind in labelKinds do
          async {
            let aspectOutputPath = aspectOutputPath labelKind.Label

            let! content =
              File.ReadAllTextAsync(Path.Combine(workspacePath, aspectOutputPath))
              |> Async.AwaitTask

            let projectInfo =
              content
              |> Decode.unsafeFromString ProjectInfo.decode

            return labelKind.Label, projectInfo
          }
      ]
      |> Async.Parallel

    let allTargets = Map.ofSeq allTargets

    printfn "%A" allTargets

    for KeyValue (label, target) in allTargets do
      let aop = aspectOutputPath label

      let! content =
        File.ReadAllTextAsync(Path.Combine(workspacePath, aop))
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
                  let projectPath = Path.Combine(workspacePath, labelPath label)
                  let f = Path.GetRelativePath(projectPath, f)
                  Xml.element "Compile" [ Xml.stringAttr "Include" f ] []
              ]

            if not (Seq.isEmpty projectInfo.Deps) then
              Xml.element
                "ItemGroup"
                []
                [
                  for d in projectInfo.Deps do
                    match allTargets |> Map.tryFind d with
                    | Some _ ->
                      let includePath =
                        Path.GetRelativePath(
                          Path.GetDirectoryName(fsprojPath label),
                          fsprojPath d)

                      Xml.element
                        "ProjectReference"
                        [
                          Xml.stringAttr "Include" includePath
                        ]
                        []
                    | None -> ()
                ]
          ]

      let target =
        Path.Combine(workspacePath, fsprojPath label)

      do!
        File.WriteAllTextAsync(target, Xml.toString xml)
        |> Async.AwaitTask

      printfn $"Wrote {target}"

      ()
    ()
  }
  |> Async.RunSynchronously

  0
