module Bazel2Proj.Thoth

open Thoth.Json.Net

module FileInfo =

  let decode : Decoder<FileInfo> =
    Decode.map4
      (fun isDirectory isSource path root ->
        {
          IsDirectory = isDirectory
          IsSource = isSource
          Path = path
          Root = root
        })
      (Decode.field "is_directory" Decode.bool)
      (Decode.field "is_source" Decode.bool)
      (Decode.field "path" Decode.string)
      (Decode.field "root" Decode.string)

module SrcInfo =

  let decode : Decoder<SrcInfo> =
    Decode.field "files" (Decode.list FileInfo.decode)
    |> Decode.map (fun xs -> { Files = xs })

module RuleInfo =

  let decode : Decoder<RuleInfo> =
    Decode.map5
      (fun kind srcs libs refs deps ->
        {
          Kind = kind
          Srcs = srcs
          Libs = libs
          Refs = refs
          Deps = deps
        })
      (Decode.field "kind" Decode.string)
      (Decode.optional "srcs" (Decode.list SrcInfo.decode))
      (Decode.optional "libs" (Decode.list SrcInfo.decode))
      (Decode.optional "refs" (Decode.list SrcInfo.decode))
      (Decode.field "deps" (Decode.list Decode.string))

module TargetInfo =

  let decode : Decoder<TargetInfo> =
    Decode.map4
      (fun rule label bfp wn ->
        {
          Rule = rule
          Label = label
          BuildFilePath = bfp
          WorkspaceName = wn
        })
      (Decode.field "rule" RuleInfo.decode)
      (Decode.field "label" Decode.string)
      (Decode.field "build_file_path" Decode.string)
      (Decode.field "workspace_name" Decode.string)

module OutputFile =

  let decode : Decoder<OutputFile> =
    Decode.map3
      (fun name uri pp ->
        {
          Name = name
          Uri = uri
          PathPrefix = pp
        })
      (Decode.field "name" Decode.string)
      (Decode.field "uri" Decode.string)
      (Decode.field "pathPrefix" (Decode.list Decode.string))

module FileEvent =

  let decode : Decoder<FileEvent> =
    (Decode.field "namedSetOfFiles" (Decode.field "files" (Decode.list OutputFile.decode)))
    |> Decode.map
      (fun files ->
        {
          NamedSetOfFiles = files
        })
