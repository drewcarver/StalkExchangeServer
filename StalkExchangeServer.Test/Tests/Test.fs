open Xunit
open FsUnit

[<Fact>]
let ``Should be true`` () =
    1 |> should equal 1