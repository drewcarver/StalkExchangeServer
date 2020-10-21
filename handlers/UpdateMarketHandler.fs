module UpdateMarketHandler

open System
open FSharp.Control.Tasks.V2
open Microsoft.AspNetCore.Http
open DateUtility
open Giraffe
open ResultUtilities
open Bells

[<CLIMutable>]
type MarketModel = {
  PurchasePrice   : Nullable<Bells>
  MondayPrice     : Nullable<Bells> 
  TuesdayPrice    : Nullable<Bells> 
  WednesdayPrice  : Nullable<Bells> 
  ThursdayPrice   : Nullable<Bells> 
  FridayPrice     : Nullable<Bells> 
  SaturdayPrice   : Nullable<Bells> 
}

let toMarket (username: string) (market: MarketModel) : Market.Market =
  { 
      UserName        = username;
      PurchasePrice   = Option.ofNullable(market.PurchasePrice);
      MondayPrice     = Option.ofNullable(market.MondayPrice);
      TuesdayPrice    = Option.ofNullable(market.TuesdayPrice);
      WednesdayPrice  = Option.ofNullable(market.WednesdayPrice);
      ThursdayPrice   = Option.ofNullable(market.ThursdayPrice);
      FridayPrice     = Option.ofNullable(market.FridayPrice);
      SaturdayPrice   = Option.ofNullable(market.SaturdayPrice);
  }

let updateMarket (market: Market.Market) (weekStartDate: DateTime) = 
  task { 
    let! exchange = StalkExchangeRepository.getExchangeCollection () |> StalkExchangeRepository.updateMarket weekStartDate market 

    return match exchange with
            | Some e  -> Ok e 
            | None    -> Error (RequestErrors.CONFLICT "Unable to update market.")
  }

let updateMarketHandler (weekStartDateString: string) (username: string): HttpHandler =
  fun (next : HttpFunc) (ctx : HttpContext) ->
    task {
      let! marketModel = ctx.BindJsonAsync<MarketModel>()
      let market = toMarket username marketModel
      let! result = parseDate weekStartDateString >>=! updateMarket market

      return! match result with
              | Ok r  -> Successful.CREATED (Exchange.toExchangeResponse r) next ctx
              | Error e -> ServerErrors.INTERNAL_ERROR e next ctx
    }