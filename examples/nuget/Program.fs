open Thoth.Json.Net

[<EntryPoint>]
let main argv =
  let json =
    Encode.object
      [
        "message", Encode.string "Here is some JSON"
      ]
    |> Encode.toString 2

  printfn "%s" json

  0
