open System
open MathUtils

[<EntryPoint>]
let main argv =
  let x = 3
  let y = 4

  printfn $"{x} + {y} = {add x y}"

  0
