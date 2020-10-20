module UpdateMarketHandler

open System
open FSharp.Control.Tasks.V2
open Microsoft.AspNetCore.Http
open DateUtility
open Giraffe
open ResultUtilities
open Bells

type MarketModel = {
  PurchasePrice   : Bells option
  MondayPrice     : Bells option
  TuesdayPrice    : Bells option
  WednesdayPrice  : Bells option 
  ThursdayPrice   : Bells option 
  FridayPrice     : Bells option
  SaturdayPrice   : Bells option
}

let updateMarket (username: string) (market: MarketModel) (weekStartDate: DateTime) = 
  task { 
    let newMarket: Market.Market = { 
      UserName        = username;
      PurchasePrice   = market.PurchasePrice;
      MondayPrice     = market.MondayPrice;
      TuesdayPrice    = market.TuesdayPrice;
      WednesdayPrice  = market.WednesdayPrice;
      ThursdayPrice   = market.ThursdayPrice;
      FridayPrice     = market.FridayPrice;
      SaturdayPrice   = market.SaturdayPrice;
    }
    let! exchange = StalkExchangeRepository.updateMarket weekStartDate newMarket
    

    return match exchange with
            | Some e  -> Ok e 
            | None    -> Error (RequestErrors.CONFLICT "Unable to update market.")
  }

let updateMarketHandler (weekStartDateString: string) (username: string): HttpHandler =
  fun (next : HttpFunc) (ctx : HttpContext) ->
    task {
      let! marketModel = ctx.BindJsonAsync<MarketModel>()
      let! result = parseDate weekStartDateString >>=! updateMarket username marketModel

      return! match result with
              | Ok r  -> Successful.CREATED r next ctx
              | Error e -> ServerErrors.INTERNAL_ERROR e next ctx
    }