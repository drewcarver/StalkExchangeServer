module UpdateMarketHandler

open System
open FSharp.Control.Tasks.V2
open Microsoft.AspNetCore.Http
open Giraffe
open Bells
open Market
open System.Threading.Tasks
open StalkExchangeRepository

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

let updateMarketBuilder = getExchangeCollection () |> updateMarket

let updateMarketHandler (updateMarket: string -> Market -> Task<MarketUpdate>) (exchangeId: string) (username: string): HttpHandler =
  fun (next : HttpFunc) (ctx : HttpContext) ->
    task {
      let! marketModel = ctx.BindJsonAsync<MarketModel>()
      let market = toMarket username marketModel

      let! result = updateMarket exchangeId market

      return! match result with
              | Success e  -> Successful.OK (Exchange.toExchangeResponse e) next ctx
              | Error      -> ServerErrors.INTERNAL_ERROR "Unexpected server error" next ctx
              | NotFound   -> RequestErrors.NOT_FOUND "Unable to update market. Exchange does not exist." next ctx
    }