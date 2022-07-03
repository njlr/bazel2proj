namespace Bazel2Proj

type BazelInfo =
  {
    Workspace : string
  }

type LabelKind =
  {
    Kind : string
    Type : string
    Label : string
  }

type FileInfo =
  {
    IsDirectory : bool
    IsSource : bool
    Path : string
    Root : string
  }

type SrcInfo =
  {
    Files : FileInfo list
  }

type RuleInfo =
  {
    Kind : string
    Deps : string list
    Srcs : (SrcInfo list) option
    Libs : (SrcInfo list) option
    Refs : (SrcInfo list) option
  }

type TargetInfo =
  {
    WorkspaceName : string
    BuildFilePath : string
    Label : string
    Rule : RuleInfo
  }

type OutputFile =
  {
    Name : string
    Uri : string
    PathPrefix : string list
  }

type FileEvent =
  {
    NamedSetOfFiles : OutputFile list
  }

// {
//   "deps":[],
//   "kind":"import_library",
//   "libs":[
//     "@nuget.fsharp.core.v6.0.5//:libs"
//   ],
//   "refs":[
//     {
//       "files":[
//         {
//           "is_directory":false,
//           "is_source":true,
//           "path":"external/nuget.fsharp.core.v6.0.5/lib/netstandard2.1/FSharp.Core.dll",
//           "root":""
//         }
//       ]
//     }
//   ]
// }



// {
//   "id":{
//     "namedSet":{
//       "id":"1"
//     }
//   },
//   "namedSetOfFiles":{
//     "files":[
//       {
//         "name":"external/paket.main/microsoft.netcore.app.ref/6.0.5/bazel2proj_6.0.5.json",
//         "uri":"file:///home/njlr/.cache/bazel/_bazel_njlr/71a88baea652242b320fc8af0b10a821/execroot/__main__/bazel-out/k8-fastbuild-ST-7193614e1cdd/bin/external/paket.main/microsoft.netcore.app.ref/6.0.5/bazel2proj_6.0.5.json",
//         "pathPrefix":["bazel-out","k8-fastbuild-ST-7193614e1cdd","bin"]
//       },
//       {
//         "name":"bazel2proj_app.json",
//         "uri":"file:///home/njlr/.cache/bazel/_bazel_njlr/71a88baea652242b320fc8af0b10a821/execroot/__main__/bazel-out/k8-fastbuild/bin/bazel2proj_app.json",
//         "pathPrefix":["bazel-out","k8-fastbuild","bin"]
//       }
//     ]
//   }
// }
