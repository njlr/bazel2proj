module Utils

let fibonacci =
  seq {
    yield 0
    yield 1

    let mutable n0 = 0
    let mutable n1 = 1

    while true do
      let n2 = n0 + n1
      yield n2
      n0 <- n1
      n1 <- n2
  }
