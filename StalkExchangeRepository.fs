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

[<Literal>]
let ConnectionString = ""

[<Literal>]
let DbName = "stalkExchange"

[<Literal>]
let CollectionName = "marketWeeks"


let client              = MongoClient(ConnectionString)
let db                  = client.GetDatabase(DbName)
let marketCollection    = db.GetCollection<Exchange>(CollectionName)

let filterCurrentMarketById marketId = Builders<Exchange>.Filter.Eq((fun m -> m.Id), marketId)
let filterCurrentMarketByStartDate marketStartDate = Builders<Exchange>.Filter.Eq((fun m -> m.WeekStartDate), marketStartDate)

let getExchangeByWeek weekStartDate = 
    task {
        let! markets = marketCollection.FindAsync<Exchange>(filterCurrentMarketByStartDate weekStartDate)

        return markets.ToEnumerable()
            |> Enumerable.ToArray 
            |> Array.tryHead
    } |> Async.AwaitTask

let getMarketById marketId =
    task {
        let! markets = marketCollection.FindAsync<Exchange>(filterCurrentMarketById marketId)

        return markets.ToEnumerable()
            |> Enumerable.ToArray
            |> Array.tryHead
    } |> Async.AwaitTask

let createExchange weekStartDate =
        try
            task {
                let! _ = marketCollection.InsertOneAsync {
                    Id              = BsonObjectId(ObjectId.GenerateNewId())
                    WeekStartDate   = weekStartDate
                    Markets         = []
                } 

                let! savedWeek = marketCollection.FindAsync<Exchange>(filterCurrentMarketByStartDate weekStartDate)
                
                return Ok(savedWeek.ToList().FirstOrDefault())
            }
        with
        | _ -> Error("An exchange with that start date already exists.") |> Task.FromResult

let createMarket weekStartDate username = 
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
        let! result = marketCollection.UpdateOneAsync(filterCurrentMarketByStartDate weekStartDate, updateDefinition, options)
        
        return! if result.IsAcknowledged
            then task { 
                        let! markets = marketCollection.FindAsync<Exchange>(filterCurrentMarketById (BsonObjectId(result.UpsertedId.AsObjectId)))

                        return markets.ToEnumerable()
                            |> Enumerable.ToArray
                            |> Array.tryHead
            }
            else Task.FromResult(None)
    } 

let updateMarket weekStartDate market = 
    let updateDefinition = Builders<Exchange>.Update.AddToSet((fun marketWeek -> marketWeek.Markets), market)
    let options = UpdateOptions(IsUpsert = true)
    
    task {
        let! result = marketCollection.UpdateOneAsync(filterCurrentMarketByStartDate weekStartDate, updateDefinition, options)
        
        return! if result.IsAcknowledged
            then task { 
                        let! markets = marketCollection.FindAsync<Exchange>(filterCurrentMarketById (BsonObjectId(result.UpsertedId.AsObjectId)))

                        return markets.ToEnumerable()
                            |> Enumerable.ToArray
                            |> Array.tryHead
            }
            else Task.FromResult(None)
    } 


let addPurchasePrice (username: string) (purchasePrice: Bells) (marketId: BsonObjectId) =
    task {
        let filter = Builders<Exchange>.Filter
        let marketIdAndUsernameFilter = filter.And(
            filter.Eq((fun m -> m.Id), marketId),
            filter.ElemMatch((fun m -> m.Markets), (fun m -> m.UserName = username))
        )

        let update = Builders<Exchange>.Update
        let purchasePriceSetter = update.Set((fun m -> m.Markets.First().PurchasePrice), Some purchasePrice)
        
        marketCollection.UpdateOneAsync(marketIdAndUsernameFilter, purchasePriceSetter, UpdateOptions(IsUpsert = true)) |> ignore
    } |> Async.AwaitTask