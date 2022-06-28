module Utils

let fibonacci =
  seq {
    let mutable n0 = 0
    let mutable n1 = 1

    yield n0
    yield n1

    while true do
      let n2 = n0 + n1
      yield n2
      n0 <- n1
      n1 <- n2
  }
