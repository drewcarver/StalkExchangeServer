module CreateMarketHandler

open Giraffe
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2
open System
open DateUtility
open ResultUtilities 

[<CLIMutable>] 
type CreateExchangeModel =
  { 
    UserName: string
  }

let createMarket (username: string) (weekStartDate: DateTime) = 
  task { 
    let! exchange = StalkExchangeRepository.getExchangeCollection () |> StalkExchangeRepository.createMarket weekStartDate username

    return match exchange with
            | Some e  -> Ok e 
            | None    -> Error (RequestErrors.CONFLICT "Unable to create market.")
  }

let createMarketHandler (weekStartDateString: string): HttpHandler =
  fun (next : HttpFunc) (ctx : HttpContext) ->
    task {
      let! createExchangeModel = ctx.BindJsonAsync<CreateExchangeModel>()
      let parsedDate = 
        match parseDate weekStartDateString with
        | Some parsedDate   -> Ok parsedDate
        | None              -> Error (RequestErrors.BAD_REQUEST "Invalid Date.")
      let! result = parsedDate >>=! createMarket createExchangeModel.UserName

      return! match result with
              | Ok r  -> Successful.CREATED r next ctx
              | Error e -> e next ctx
    }