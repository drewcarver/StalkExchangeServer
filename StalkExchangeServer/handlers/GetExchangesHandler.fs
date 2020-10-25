module GetExchangesHandler

open Giraffe
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2
open System.Threading.Tasks


let getExchangesBuilder () = StalkExchangeRepository.getExchangeCollection () |> StalkExchangeRepository.getExchanges

let getExchangesHandler (getExchanges: unit -> Task<Exchange.Exchange list>) : HttpHandler = 
  fun (next : HttpFunc) (ctx : HttpContext) -> 
    task {
      let! exchanges = getExchanges ()

      return! Successful.OK exchanges next ctx
    }
