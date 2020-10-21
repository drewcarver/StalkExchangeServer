module ResultUtilities

open System.Threading.Tasks

let (>>=) x f = Result.bind f x
let (>>=!) (x: Result<'a, 'b>) (f: 'a -> Task<Result<'c, 'b>>) = 
    match x with
      | Ok value -> f value
      | Error e  -> (Error e |> Task.FromResult)