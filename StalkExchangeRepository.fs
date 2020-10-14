module StalkExchangeRepository

open MongoDB.Bson
open MongoDB.Driver
open System
open System.Linq
open Microsoft.FSharp.Collections
open FSharp.Control.Tasks.V2.ContextSensitive
open System.Threading.Tasks
open System.Collections.Generic

[<Literal>]
let ConnectionString = ""

[<Literal>]
let DbName = "stalkExchange"

[<Literal>]
let CollectionName = "marketWeeks"

type Bells = int

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

[<CLIMutable>]
type MarketWeek = { 
    Id              : BsonObjectId 
    WeekStartDate   : DateTime
    WeekEndDate     : DateTime
    Markets         : IEnumerable<Market> 
} 

let client              = MongoClient(ConnectionString)
let db                  = client.GetDatabase(DbName)
let marketCollection    = db.GetCollection<MarketWeek>(CollectionName)

let filterCurrentMarketById marketId = Builders<MarketWeek>.Filter.Eq((fun m -> m.Id), marketId)
let filterCurrentMarketByStartDate marketStartDate = Builders<MarketWeek>.Filter.Eq((fun m -> m.WeekStartDate), marketStartDate)

let getMarketByWeek weekStartDate = 
    task {
        let! markets = marketCollection.FindAsync<MarketWeek>(filterCurrentMarketByStartDate weekStartDate)

        return markets.ToEnumerable()
            |> Enumerable.ToArray 
            |> Array.tryHead
    } |> Async.AwaitTask

let getMarketById marketId =
    task {
        let! markets = marketCollection.FindAsync<MarketWeek>(filterCurrentMarketById marketId)

        return markets.ToEnumerable()
            |> Enumerable.ToArray
            |> Array.tryHead
    } |> Async.AwaitTask

let createMarketWeek weekStartDate weekEndDate =
    marketCollection.InsertOneAsync {
        Id              = BsonObjectId(ObjectId.GenerateNewId())
        WeekStartDate   = weekStartDate
        WeekEndDate     = weekEndDate
        Markets         = []
    } |> Async.AwaitTask

let createMarket weekStartDate username = 
    let marketToAdd: Market = {
        UserName        = username
        PurchasePrice   = Some 50
        MondayPrice     = None 
        TuesdayPrice    = None
        WednesdayPrice  = None
        ThursdayPrice   = None
        FridayPrice     = None
        SaturdayPrice   = None
    }
    let updateDefinition = Builders<MarketWeek>.Update.AddToSet((fun marketWeek -> marketWeek.Markets), marketToAdd)
    let options = UpdateOptions(IsUpsert = true)
    
    task {
        let! result = marketCollection.UpdateOneAsync(filterCurrentMarketByStartDate weekStartDate, updateDefinition, options)
        
        return! match result.IsAcknowledged with
                | true -> 
                    task { 
                        let! markets = marketCollection.FindAsync<MarketWeek>(filterCurrentMarketById (BsonObjectId(result.UpsertedId.AsObjectId)))

                        return markets.ToEnumerable()
                            |> Enumerable.ToArray
                            |> Array.tryHead
                    }
                | false -> Task.FromResult(None)
    } |> Async.AwaitTask

let addPurchasePrice (username: string) (purchasePrice: Bells) (marketId: BsonObjectId) =
    task {
        let filter = Builders<MarketWeek>.Filter
        let marketIdAndUsernameFilter = filter.And(
            filter.Eq((fun m -> m.Id), marketId),
            filter.ElemMatch((fun m -> m.Markets), (fun m -> m.UserName = username))
        )

        let update = Builders<MarketWeek>.Update
        let purchasePriceSetter = update.Set((fun m -> m.Markets.First().PurchasePrice), Some purchasePrice)
        
        marketCollection.UpdateOneAsync(marketIdAndUsernameFilter, purchasePriceSetter, UpdateOptions(IsUpsert = true)) |> ignore
    } |> Async.AwaitTask