module StalkExchangeRepository

open MongoDB.Bson
open MongoDB.Driver
open System.Linq
open Microsoft.FSharp.Collections
open FSharp.Control.Tasks.V2.ContextSensitive
open System.Threading.Tasks
open Exchange
open Market
open Bells
open System
open Microsoft.FSharp.Collections

[<Literal>]
let DbName = "stalkExchange"

[<Literal>]
let CollectionName = "marketWeeks"

let getExchangeCollection () =
    let connectionString = System.Environment.GetEnvironmentVariable("ConnectionString", EnvironmentVariableTarget.Process);
    let client = MongoClient(connectionString)
    let db = client.GetDatabase(DbName)

    db.GetCollection<Exchange>(CollectionName)

let filterCurrentExchangeById marketId = Builders<Exchange>.Filter.Eq((fun m -> m.Id), marketId)
let filterCurrentExchangeByStartDate marketStartDate = Builders<Exchange>.Filter.Eq((fun m -> m.WeekStartDate), marketStartDate)
let filterByMarketUsername username = Builders<Exchange>.Filter.ElemMatch((fun e -> e.Markets), (fun (m: Market.Market) -> m.UserName = username))

let getExchanges (exchangeCollection: IMongoCollection<Exchange>) (weekStartDate: DateTime option) = 
    task {
        let exchangeFilter = 
            match weekStartDate with
            | Some wsd  -> Builders<Exchange>.Filter.Eq((fun e -> e.WeekStartDate), wsd)
            | None      -> Builders<Exchange>.Filter.Empty
        let! exchanges = exchangeCollection.FindAsync<Exchange>(exchangeFilter)

        return exchanges.ToEnumerable()
            |> Enumerable.ToArray
            |> Array.toList
    } 

let getExchangeById (exchangeCollection: IMongoCollection<Exchange>) (exchangeId: string) = 
    task {
        let! exchanges = exchangeCollection.FindAsync<Exchange>(filterCurrentExchangeById (BsonObjectId.Create exchangeId))

        return exchanges.ToEnumerable()
            |> Enumerable.ToArray 
            |> Array.tryHead
    } 

let getMarketById (exchangeCollection: IMongoCollection<Exchange>) (marketId: BsonObjectId) =
    task {
        let! markets = exchangeCollection.FindAsync<Exchange>(filterCurrentExchangeById marketId)

        return markets.ToEnumerable()
            |> Enumerable.ToArray
            |> Array.tryHead
    } 

let createExchange (exchangeCollection: IMongoCollection<Exchange>) (weekStartDate: DateTime) =
        try
            task {
                let! _ = exchangeCollection.InsertOneAsync {
                    Id              = BsonObjectId(ObjectId.GenerateNewId())
                    WeekStartDate   = weekStartDate
                    Markets         = []
                } 

                let! savedWeek = exchangeCollection.FindAsync<Exchange>(filterCurrentExchangeByStartDate weekStartDate)
                
                return Ok(savedWeek.ToList().FirstOrDefault())
            }
        with
        | _ -> Error("An exchange with that start date already exists.") |> Task.FromResult

type MarketCreate =
    | Success of Exchange
    | ExchangeNotFound
    | Error

let createMarket (exchangeCollection: IMongoCollection<Exchange>) (exchangeId: string) (username: string) = 
    let newMarket: Market = {
        UserName        = username
        PurchasePrice   = None
        MondayPrice     = None 
        TuesdayPrice    = None
        WednesdayPrice  = None
        ThursdayPrice   = None
        FridayPrice     = None
        SaturdayPrice   = None
    }
    let updateDefinition = Builders<Exchange>.Update.AddToSet((fun marketWeek -> marketWeek.Markets), newMarket)
    let options = UpdateOptions(IsUpsert = true)
    
    task {
        let! result = exchangeCollection.UpdateOneAsync(filterCurrentExchangeById (BsonObjectId.Create exchangeId), updateDefinition, options)
        
        return! if result.IsAcknowledged
            then task { 
                let! markets = exchangeCollection.FindAsync<Exchange>(filterCurrentExchangeById (BsonObjectId(result.UpsertedId.AsObjectId)))

                let result = 
                    markets.ToEnumerable()
                    |> Enumerable.ToArray
                    |> Array.tryHead

                return match result with
                        | Some e    -> Success e
                        | None      -> ExchangeNotFound
            }
            else Error |> Task.FromResult
    } 

type MarketUpdate = 
    | Success of Exchange
    | NotFound
    | Error

let updateMarket (exchangeCollection: IMongoCollection<Exchange>) (exchangeId: string) (market: Market.Market) = 
    let exchangeIdFilter = filterCurrentExchangeById (BsonObjectId.Create exchangeId)
    let marketUsernameFilter = filterByMarketUsername market.UserName
    let filter = Builders<Exchange>.Filter.And(exchangeIdFilter, marketUsernameFilter)
    let updateDefinition = Builders<Exchange>.Update.Set((fun e -> e.Markets.ElementAt(-1)), market)
    let options = UpdateOptions(IsUpsert = false)
    
    task {
        let! result = exchangeCollection.UpdateOneAsync(filter, updateDefinition, options)
        
        return! if result.IsAcknowledged
            then task { 
                let! markets = exchangeCollection.FindAsync<Exchange>(exchangeIdFilter)

                let updatedMarket = 
                    markets.ToEnumerable()
                    |> Enumerable.ToArray
                    |> Array.tryHead
                
                return match updatedMarket with 
                        | Some m    -> Success m
                        | None      -> NotFound
            }
            else Error |> Task.FromResult
    } 
