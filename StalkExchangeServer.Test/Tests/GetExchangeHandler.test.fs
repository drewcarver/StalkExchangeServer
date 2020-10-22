module GetExchangeHandlerTest

open GetExchangeHandler
open Xunit
open FsUnit
open System
open Newtonsoft.Json.Bson
open MongoDB.Bson
open System.Threading.Tasks
open FSharp.Control.Tasks.V2.ContextSensitive
open Giraffe

let getDummyExchange: Exchange.Exchange option = 
    Some {
        Id = BsonObjectId(ObjectId.GenerateNewId());
        WeekStartDate = DateTime(2020, 10, 18);
        Markets = [||];
    } 

let getExistingExchangeMock (weekStartDate: DateTime) = getDummyExchange |> Task.FromResult 
let getMissingExchangeMock (weekStartDate: DateTime): Async<Exchange.Exchange option> = None |> Task.FromResult |> Async.AwaitTask 

[<Fact>]
let ``Should return not found when exchange is missing`` () =
    task {
        let weekStartDate = DateTime(2020, 10, 18)
        let! result = getExchange getMissingExchangeMock weekStartDate
        let expected = Error (RequestErrors.NOT_FOUND "Market Week Not Found.")

        result |> should equal expected
    }

