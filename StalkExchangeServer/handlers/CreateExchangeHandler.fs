module CreateExchangeHandler

open Giraffe
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2
open System
open Exchange
open System.Threading.Tasks

[<CLIMutable>] 
type CreateExchangeModel =
  { 
    WeekStartDate : DateTime
  }

let createExchangeBuilder = StalkExchangeRepository.getExchangeCollection () |> StalkExchangeRepository.createExchange 

let createExchangeHandler (createExchange: DateTime -> Task<Result<Exchange, string>>) =
  fun (next : HttpFunc) (ctx : HttpContext) ->
    task {
      let! createExchangeModel = ctx.BindJsonAsync<CreateExchangeModel>()
      let! result = createExchange createExchangeModel.WeekStartDate

      return! match result with
              | Ok r  -> Successful.CREATED r next ctx
              | Error e -> RequestErrors.CONFLICT e next ctx
    }