module Bazel2Proj.Bazel

open System
open System.Diagnostics
open System.IO
open Thoth.Json.Net
open Bazel2Proj.Thoth

let private parseLabelKind (x : string) =
  [
    let lines = x.Split([| '\n' |], StringSplitOptions.RemoveEmptyEntries)

    for line in lines do
      let parts = line.Split([| ' ' |], StringSplitOptions.RemoveEmptyEntries)

      let kind = parts[0]
      let ty = parts[1]
      let label = parts[2]

      {
        Kind = kind
        Type = ty
        Label = label
      }
  ]

let private escape (x : string) =
  x.Replace("\"", "\\\"")

let private executeBash (workingDirectory : string) (command : string) =
  async {
    use p = new Process()

    p.EnableRaisingEvents <- true

    p.StartInfo.FileName <- "/bin/bash"
    p.StartInfo.WorkingDirectory <- workingDirectory
    p.StartInfo.Arguments <- $"-c \"{escape command}\""
    p.StartInfo.RedirectStandardOutput <- true
    p.StartInfo.RedirectStandardError <- true
    p.StartInfo.CreateNoWindow <- true

    p.Start() |> ignore

    do!
      p.WaitForExitAsync()
      |> Async.AwaitTask

    let! stdout =
      p.StandardOutput.ReadToEndAsync()
      |> Async.AwaitTask

    let! stderr =
      p.StandardError.ReadToEndAsync()
      |> Async.AwaitTask

    return p.ExitCode, stdout, stderr
  }

let fetchInfo (workspacePath : string) =
  async {
    let command = "bazel info"

    let! exitCode, stdout, stderr = executeBash workspacePath command

    if exitCode <> 0 then
      failwith $"Failed to execute `{command}`: \n{stderr}"

    let lines = stdout.Split([| '\n' |])

    let workspace =
      lines
      |> Seq.find (fun x -> x.StartsWith("workspace: "))
      |> fun x -> x.Substring("workspace: ".Length)

    return
      {
        Workspace = workspace
      }
  }

let findTargets (workspacePath : string) =
  async {
    let query =
      [
        "fsharp_binary"
        "fsharp_library"
        "csharp_binary"
        "csharp_library"
      ]
      |> Seq.map (fun x -> $"""kind("{x}", "//...")""")
      |> String.concat " union "

    let command = $"""bazel query '{query}' --output label_kind"""

    let! exitCode, stdout, stderr = executeBash workspacePath command

    if exitCode <> 0 then
      failwith $"Failed to execute Bazel query `{command}`: \n{stderr}"

    return parseLabelKind stdout
  }

let buildAspect (workspacePath : string) (target : string) =
  async {
    let command = $"""bazel build {target} --aspects @bazel2proj//:aspect.bzl%%bazel2proj_aspect --build_event_json_file=events.json --output_groups=+default,+jsons"""

    let! exitCode, stdout, stderr = executeBash workspacePath command

    if exitCode <> 0 then
      failwith $"Failed to build Bazel aspect: \n{stderr}"

    printfn "%s" stdout

    let! eventsJsonLines =
      File.ReadAllLinesAsync("./events.json")
      |> Async.AwaitTask

    let fileEvents =
      [
        for line in eventsJsonLines do
          line
          |> Decode.fromString FileEvent.decode
      ]
      |> List.choose
        (
          function
          | Ok x -> Some x
          | Error _ -> None
        )

    return fileEvents
  }
