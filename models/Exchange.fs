module Exchange

open System
open MongoDB.Bson
open Market
open System.Collections.Generic

[<CLIMutable>]
type Exchange = { 
    Id              : BsonObjectId 
    WeekStartDate   : DateTime
    WeekEndDate     : DateTime
    Markets         : IEnumerable<Market> 
} 