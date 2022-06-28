open System
open Utils

[<EntryPoint>]
let main argv =
  printfn "Hello, world."
  printfn "Some numbers for you:"

  for x in fibonacci |> Seq.take 10 do
    printfn "%i" x

  0
