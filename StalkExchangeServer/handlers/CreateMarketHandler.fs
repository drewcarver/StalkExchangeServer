module CreateMarketHandler

open Exchange
open Giraffe
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2
open System.Threading.Tasks
open StalkExchangeRepository

[<CLIMutable>] 
type CreateMarketModel =
  { 
    UserName: string
  }

let createMarketBuilder = getExchangeCollection () |> createMarket 

let createMarketHandler (createExchange: string -> string -> Task<MarketCreate>) (exchangeId: string): HttpHandler =
  fun (next : HttpFunc) (ctx : HttpContext) ->
    task {
      let! createExchangeModel = ctx.BindJsonAsync<CreateMarketModel>()
      let! result = createExchange exchangeId createExchangeModel.UserName

      return! match result with
              | MarketCreate.Success e          -> Successful.CREATED (toExchangeResponse e) next ctx
              | MarketCreate.ExchangeNotFound   -> RequestErrors.NOT_FOUND "Exchange not found." next ctx
              | MarketCreate.Error              -> ServerErrors.INTERNAL_ERROR "An internal error occurred." next ctx
    }