module Bazel2Proj.Bazel

open System
open System.Diagnostics

type LabelKind =
  {
    Kind : string
    Type : string
    Label : string
  }

let parseLabelKind (x : string) =
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

let findTargets (workspacePath : string) =
  async {
    let command = """bazel query 'kind("fsharp_binary", "//...")' --output label_kind"""

    let! exitCode, stdout, stderr = executeBash workspacePath command

    if exitCode <> 0 then
      failwith $"Failed to execute Bazel query: \n{stderr}"

    return parseLabelKind stdout
  }

let buildAspect (workspacePath : string) (target : string) =
  async {
    let command = $"""bazel build {target} --aspects @bazel2proj//:aspect.bzl%%print_aspect --output_groups=+default,+jsons"""

    let! exitCode, _, stderr = executeBash workspacePath command

    if exitCode <> 0 then
      failwith $"Failed to build Bazel aspect: \n{stderr}"
  }
