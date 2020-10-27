module GetExchangeHandler

open Giraffe
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2
open System.Threading.Tasks

let getExchange (getExchangeFromDb: string -> Task<Exchange.Exchange option>) (id: string) = 
  task { 
    let! markets = getExchangeFromDb id

    return match markets with
            | Some m  -> Ok m 
            | None    -> Error (RequestErrors.NOT_FOUND "Market Week Not Found.")
  }

let getExchangeBuilder = StalkExchangeRepository.getExchangeCollection () |> StalkExchangeRepository.getExchangeById 

let getExchangeHandler (getExchangeFromDb: string -> Task<Exchange.Exchange option>) (exchangeId: string) : HttpHandler = 
  fun (next : HttpFunc) (ctx : HttpContext) ->
    task {
      let! result = getExchange getExchangeFromDb exchangeId

      return! match result with
              | Ok exchange  -> Successful.OK (Exchange.toExchangeResponse exchange) next ctx
              | Error error -> error next ctx
    }