module Exchange

open System.Linq
open System
open MongoDB.Bson
open Market
open System.Collections.Generic

[<CLIMutable>]
type Exchange = { 
    Id              : BsonObjectId 
    WeekStartDate   : DateTime
    Markets         : IEnumerable<Market.Market> 
} 

type ExchangeResponse = {
    Id              : BsonObjectId 
    WeekStartDate   : DateTime
    Markets         : IEnumerable<Market.MarketResponse> 
}

let toExchangeResponse (exchange: Exchange): ExchangeResponse =
    {
        Id              = exchange.Id;
        WeekStartDate   = exchange.WeekStartDate;
        Markets         = exchange.Markets.Select toMarketResponse 
    }