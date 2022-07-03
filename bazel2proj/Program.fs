module Bazel2Proj.Program

open System
open System.IO
open Thoth.Json.Net
open Bazel2Proj.Thoth

let private labelName (label : string) =
  label.Split([| ':' |]) |> Seq.last

let private labelPath (label : string) =
  label.Split([| ':' |])
  |> Seq.head
  |> fun x -> if x.StartsWith("//") then x.Substring(2) else x

let projectName (label : string) =
  (if label.StartsWith("//") then
    label.Substring(2)
  else
    label)
  |> fun x -> x.Split([| ':' |], StringSplitOptions.RemoveEmptyEntries)
  |> String.concat "/"

let projectPath (workspacePath : string) (label : string) (ruleInfo : RuleInfo) =
  let projectExt =
    match ruleInfo.Kind with
    | "fsharp_binary" -> ".fsproj"
    | "fsharp_library" -> ".fsproj"
    | "csharp_binary" -> ".csproj"
    | "csharp_library" -> ".csproj"
    | x -> failwith $"Unrecognized kind \"{x}\""

  Path.Combine(workspacePath, projectName label + projectExt)

let private aspectOutputPath (label : string) =
  let name = labelName label
  let path = labelPath label
  Path.Combine("bazel-bin", path, $"bazel2proj_{name}.json")

let rec private transitiveClosure (allTargets : Map<string, TargetInfo>) (rule : RuleInfo) =
  [
    for d in rule.Deps do
      yield d

      match allTargets |> Map.tryFind d with
      | Some e ->
        if e.Rule.Kind = "import_library" then
          yield! transitiveClosure allTargets e.Rule
      | None -> ()
  ]

[<EntryPoint>]
let main argv =
  async {
    let workspacePath =
      argv
      |> Array.tryItem 0
      |> Option.defaultValue (Directory.GetCurrentDirectory())

    let! bazelInfo = Bazel.fetchInfo workspacePath

    let! labelKinds = Bazel.findTargets workspacePath

    printfn "Found the following targets: "
    for labelKind in labelKinds do
      printfn $"  {labelKind.Label}"

    let! fileEvents =
      [
        for labelKind in labelKinds do
          async {
            let! fileEvents = Bazel.buildAspect workspacePath labelKind.Label
            printfn $"Built aspect for {labelKind.Label}"

            return fileEvents
          }
      ]
      |> Async.Sequential

    let fileEvents = fileEvents |> Seq.collect id

    for fe in fileEvents do
      printfn "%A" fe

    let aspectOutputPaths =
      [
        for fe in fileEvents do
          for f in fe.NamedSetOfFiles do
            if f.Uri.EndsWith(".json") then
              Uri f.Uri
              |> fun x -> x.AbsolutePath
      ]
      |> List.distinct

    printfn "Reading aspect outputs..."

    let! allTargets =
      [
        for aspectOutputPath in aspectOutputPaths do
          async {
            let! content =
              File.ReadAllTextAsync(Path.Combine(workspacePath, aspectOutputPath))
              |> Async.AwaitTask

            let targetInfo =
              match content |> Decode.fromString TargetInfo.decode with
              | Ok x -> x
              | Error message ->
                failwith $"Failed to decode {aspectOutputPath}: {message}"

            return targetInfo.Label, targetInfo
          }
      ]
      |> fun jobs -> Async.Parallel(jobs, maxDegreeOfParallelism = 8)

    let allTargets = Map.ofSeq allTargets

    printfn "%A" allTargets

    let dotNetProjectKinds =
      Set.ofSeq
        [
          "fsharp_binary"
          "fsharp_library"
          "csharp_binary"
          "csharp_library"
        ]

    let projectTargets =
      allTargets
      |> Map.filter (fun _ v -> Seq.contains v.Rule.Kind dotNetProjectKinds)

    let workspaceName =
      bazelInfo.Workspace.Split('/')
      |> Seq.last

    let execPath =
      Path.Combine(workspacePath, "bazel-" + workspaceName)

    for KeyValue (label, targetInfo) in projectTargets do
      printfn "%A" targetInfo

      let ruleInfo = targetInfo.Rule

      let outputType =
        match ruleInfo.Kind with
        | "fsharp_binary" -> "Exe"
        | "csharp_binary" -> "Exe"
        | "fsharp_library" -> "Library"
        | "csharp_library" -> "Library"
        | x -> failwith $"Unrecognized kind \"{x}\""

      let projectExt =
        match ruleInfo.Kind with
        | "fsharp_binary" -> ".fsproj"
        | "fsharp_library" -> ".fsproj"
        | "csharp_binary" -> ".csproj"
        | "csharp_library" -> ".csproj"
        | x -> failwith $"Unrecognized kind \"{x}\""

      let target = projectPath workspacePath label ruleInfo

      let xml =
        Xml.element
          "Project"
          [ Xml.stringAttr "Sdk" "Microsoft.NET.Sdk" ]
          [
            Xml.element
              "PropertyGroup"
              []
              [
                Xml.stringElement "OutputType" outputType
                Xml.stringElement "TargetFramework" "net6.0"
              ]

            Xml.element
              "ItemGroup"
              []
              [
                let srcs =
                  ruleInfo.Srcs
                  |> Option.defaultValue []
                  |> Seq.collect (fun srcs -> srcs.Files)
                  |> Seq.filter (fun fi -> fi.IsSource)

                for fileInfo in srcs do
                  let projectPath = Path.Combine(workspacePath, labelPath label)
                  let f = Path.GetRelativePath(projectPath, fileInfo.Path)
                  Xml.element "Compile" [ Xml.stringAttr "Include" f ] []
              ]

            let deps =
              ruleInfo.Deps
              |> Seq.choose (fun d -> Map.tryFind d allTargets)
              |> Seq.collect (fun d -> d.Label :: transitiveClosure allTargets d.Rule)
              |> Seq.distinct
              |> Seq.toList

            // printfn "TRANSITIVE CLOSURE: %A" tc

            if not (Seq.isEmpty deps) then
              Xml.element
                "ItemGroup"
                []
                [
                  for d in deps do
                    match allTargets |> Map.tryFind d with
                    | Some dep ->
                      if Seq.contains dep.Rule.Kind dotNetProjectKinds then
                        let includePath =
                          Path.GetRelativePath(
                            Path.GetDirectoryName(target),
                            projectPath workspacePath d dep.Rule)

                        Xml.element
                          "ProjectReference"
                          [
                            Xml.stringAttr "Include" includePath
                          ]
                          []

                      if dep.Rule.Kind = "import_library" then
                        for lib in dep.Rule.Libs |> Option.defaultValue [] do
                          match Seq.tryHead lib.Files with
                          | Some file ->
                            let dllName =
                              file.Path.Split([| '/' |])
                              |> Seq.last
                              |> fun x ->
                                if x.EndsWith(".dll") then
                                  x.Substring(0, x.Length - ".dll".Length)
                                else
                                  x

                            let hintPath =
                              Path.GetRelativePath(Path.GetDirectoryName(target), Path.Combine(execPath, file.Path))

                            Xml.element
                              "Reference"
                              [
                                Xml.stringAttr "Include" dllName
                              ]
                              [
                                Xml.stringElement "HintPath" hintPath
                              ]
                          | None -> ()
                    | None -> ()
                ]
          ]

      do!
        File.WriteAllTextAsync(target, Xml.toString xml)
        |> Async.AwaitTask

      printfn $"Wrote {target}"

      ()
    ()
  }
  |> Async.RunSynchronously

  0
