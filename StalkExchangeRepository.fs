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

let getExchangeByWeek (weekStartDate: DateTime) (exchangeCollection: IMongoCollection<Exchange>) = 
    task {
        let! markets = exchangeCollection.FindAsync<Exchange>(filterCurrentExchangeByStartDate weekStartDate)

        return markets.ToEnumerable()
            |> Enumerable.ToArray 
            |> Array.tryHead
    } |> Async.AwaitTask

let getMarketById (exchangeCollection: IMongoCollection<Exchange>) (marketId: BsonObjectId) =
    task {
        let! markets = exchangeCollection.FindAsync<Exchange>(filterCurrentExchangeById marketId)

        return markets.ToEnumerable()
            |> Enumerable.ToArray
            |> Array.tryHead
    } |> Async.AwaitTask

let createExchange (weekStartDate: DateTime) (exchangeCollection: IMongoCollection<Exchange>) =
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

let createMarket (weekStartDate: DateTime) (username: string) (exchangeCollection: IMongoCollection<Exchange>) = 
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
        let! result = exchangeCollection.UpdateOneAsync(filterCurrentExchangeByStartDate weekStartDate, updateDefinition, options)
        
        return! if result.IsAcknowledged
            then task { 
                        let! markets = exchangeCollection.FindAsync<Exchange>(filterCurrentExchangeById (BsonObjectId(result.UpsertedId.AsObjectId)))

                        return markets.ToEnumerable()
                            |> Enumerable.ToArray
                            |> Array.tryHead
            }
            else Task.FromResult(None)
    } 

let updateMarket (weekStartDate: DateTime) (market: Market.Market) (exchangeCollection: IMongoCollection<Exchange>) = 
    let filter = Builders<Exchange>.Filter.And(filterCurrentExchangeByStartDate weekStartDate, filterByMarketUsername market.UserName)
    let updateDefinition = Builders<Exchange>.Update.Set((fun e -> e.Markets.ElementAt(-1)), market)
    let options = UpdateOptions(IsUpsert = false)
    
    task {
        let! result = exchangeCollection.UpdateOneAsync(filter, updateDefinition, options)
        
        return! if result.IsAcknowledged
            then task { 
                        let! markets = exchangeCollection.FindAsync<Exchange>(filterCurrentExchangeByStartDate weekStartDate)

                        return markets.ToEnumerable()
                            |> Enumerable.ToArray
                            |> Array.tryHead
            }
            else Task.FromResult(None)
    } 
