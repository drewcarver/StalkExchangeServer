module CreateMarketHandler

open Giraffe
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2
open System

[<CLIMutable>] 
type CreateExchangeModel =
  { 
    UserName      : string
    WeekStartDate : DateTime
  }

let createMarketHandler : HttpHandler =
  fun (next : HttpFunc) (ctx : HttpContext) ->
    task {
      let! createMarketModel = ctx.BindJsonAsync<CreateExchangeModel>()
      let! result = StalkExchangeRepository.createMarket createMarketModel.WeekStartDate createMarketModel.UserName

      return! match result with
              | Some r  -> Successful.CREATED r next ctx
              | None    -> ServerErrors.INTERNAL_ERROR "Unable to create market week" next ctx
    }