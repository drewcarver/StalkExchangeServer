module CreateExchangeHandler

open Giraffe
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2
open System

[<CLIMutable>] 
type CreateExchangeModel =
  { 
    WeekStartDate : DateTime
  }

let createExchangeHandler : HttpHandler =
  fun (next : HttpFunc) (ctx : HttpContext) ->
    task {
      let! createExchangeModel = ctx.BindJsonAsync<CreateExchangeModel>()
      let! result = StalkExchangeRepository.createExchange createExchangeModel.WeekStartDate 

      return! match result with
              | Ok r  -> Successful.CREATED r next ctx
              | Error e -> ServerErrors.INTERNAL_ERROR e next ctx
    }