module Market

open Bells
open System

[<CLIMutable>]
type Market = {
    UserName        : string
    PurchasePrice   : Bells option
    MondayPrice     : Bells option
    TuesdayPrice    : Bells option
    WednesdayPrice  : Bells option 
    ThursdayPrice   : Bells option 
    FridayPrice     : Bells option
    SaturdayPrice   : Bells option
}

type MarketResponse = {
    UserName        : string
    PurchasePrice   : Nullable<Bells> 
    MondayPrice     : Nullable<Bells> 
    TuesdayPrice    : Nullable<Bells> 
    WednesdayPrice  : Nullable<Bells> 
    ThursdayPrice   : Nullable<Bells> 
    FridayPrice     : Nullable<Bells> 
    SaturdayPrice   : Nullable<Bells> 
}

let toMarketResponse (market: Market): MarketResponse =
    {
      UserName        = market.UserName;
      PurchasePrice   = Option.toNullable(market.PurchasePrice);
      MondayPrice     = Option.toNullable(market.MondayPrice);
      TuesdayPrice    = Option.toNullable(market.TuesdayPrice);
      WednesdayPrice  = Option.toNullable(market.WednesdayPrice);
      ThursdayPrice   = Option.toNullable(market.ThursdayPrice);
      FridayPrice     = Option.toNullable(market.FridayPrice);
      SaturdayPrice   = Option.toNullable(market.SaturdayPrice);
    }