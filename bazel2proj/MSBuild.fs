module Bazel2Proj.MSBuild

open System
open System.IO

// Microsoft Visual Studio Solution File, Format Version 12.00
// # Visual Studio Version 16
// VisualStudioVersion = 16.0.30114.105
// MinimumVisualStudioVersion = 10.0.40219.1
// Project("{F2A71F9B-5D33-465A-A702-920D77279786}") = "bazel2proj.tests", "bazel2proj.tests\bazel2proj.tests.fsproj", "{AE23F059-9A9E-4DB2-8B0D-61BAF13918DD}"
// EndProject
// Project("{F2A71F9B-5D33-465A-A702-920D77279786}") = "bazel2proj", "bazel2proj\bazel2proj.fsproj", "{640998D2-1466-497F-97ED-E03AD9F0B524}"
// EndProject
// Global
// 	GlobalSection(SolutionConfigurationPlatforms) = preSolution
// 		Debug|Any CPU = Debug|Any CPU
// 		Release|Any CPU = Release|Any CPU
// 	EndGlobalSection
// 	GlobalSection(SolutionProperties) = preSolution
// 		HideSolutionNode = FALSE
// 	EndGlobalSection
// 	GlobalSection(ProjectConfigurationPlatforms) = postSolution
// 		{AE23F059-9A9E-4DB2-8B0D-61BAF13918DD}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
// 		{AE23F059-9A9E-4DB2-8B0D-61BAF13918DD}.Debug|Any CPU.Build.0 = Debug|Any CPU
// 		{AE23F059-9A9E-4DB2-8B0D-61BAF13918DD}.Release|Any CPU.ActiveCfg = Release|Any CPU
// 		{AE23F059-9A9E-4DB2-8B0D-61BAF13918DD}.Release|Any CPU.Build.0 = Release|Any CPU
// 		{640998D2-1466-497F-97ED-E03AD9F0B524}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
// 		{640998D2-1466-497F-97ED-E03AD9F0B524}.Debug|Any CPU.Build.0 = Debug|Any CPU
// 		{640998D2-1466-497F-97ED-E03AD9F0B524}.Release|Any CPU.ActiveCfg = Release|Any CPU
// 		{640998D2-1466-497F-97ED-E03AD9F0B524}.Release|Any CPU.Build.0 = Release|Any CPU
// 	EndGlobalSection
// EndGlobal

let private fsharpID = Guid.Parse "F2A71F9B-5D33-465A-A702-920D77279786"

let private guidUpper (x : Guid) =
  (string x).ToUpperInvariant()

let generateSln (solutionDirectory : string) (projectFilePaths : string list) =
  let projectFilePaths =
    projectFilePaths
    |> Seq.map
      (fun x ->
        let g = Guid.NewGuid()
        g, x)
    |> Map.ofSeq

  let configs =
    [
      "Release", "Any CPU"
    ]

  [
    "Microsoft Visual Studio Solution File, Format Version 12.00"
    "# Visual Studio Version 16"
    "VisualStudioVersion = 16.0.30114.105"
    "MinimumVisualStudioVersion = 10.0.40219.1"

    for KeyValue (projectID, projectFilePath) in projectFilePaths do
      let projectName = Path.GetFileNameWithoutExtension(projectFilePath)

      let relativeProjectFilePath =
        Path.GetRelativePath(solutionDirectory, projectFilePath)

      sprintf
        "Project(\"{%s}\") = \"%s\", \"%s\", \"{%s}\""
        (guidUpper fsharpID) // TODO
        projectName
        relativeProjectFilePath
        (guidUpper projectID)

      "EndProject"

    "Global"
    "	GlobalSection(SolutionConfigurationPlatforms) = preSolution"
    "		Debug|Any CPU = Debug|Any CPU"
    "		Release|Any CPU = Release|Any CPU"
    "	EndGlobalSection"
    "	GlobalSection(SolutionProperties) = preSolution"
    "		HideSolutionNode = FALSE"
    "	EndGlobalSection"
    "	GlobalSection(ProjectConfigurationPlatforms) = postSolution"

    for KeyValue (projectID, projectFilePath) in projectFilePaths do
      for config, target in configs do
        sprintf "		{%s}.%s|%s.ActiveCfg = %s|%s" (guidUpper projectID) config target config target
        sprintf "		{%s}.%s|%s.Build.0 = %s|%s" (guidUpper projectID) config target config target

    "	EndGlobalSection"
    "EndGlobal"
  ]
  |> String.concat "\n"