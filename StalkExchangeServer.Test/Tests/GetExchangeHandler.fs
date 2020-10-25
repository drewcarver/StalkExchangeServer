module GetExchangeHandlerTest

open GetExchangeHandler
open Xunit
open FsUnit
open System
open MongoDB.Bson
open System.Threading.Tasks
open FSharp.Control.Tasks.V2
open Microsoft.AspNetCore.Http
open GiraffeTestUtilities


let dummyExchange: Exchange.Exchange = 
    {
        Id = BsonObjectId(ObjectId.GenerateNewId());
        WeekStartDate = DateTime.SpecifyKind(DateTime(2020, 10, 18), DateTimeKind.Utc);
        Markets = [||];
    } 

let getExistingExchangeMock (weekStartDate: DateTime) = Some dummyExchange |> Task.FromResult  
let getMissingExchangeMock (weekStartDate: DateTime) = None |> Task.FromResult 

[<Fact>]
let ``Should return an exchange`` () =
    let handler = getExchangeHandler getExistingExchangeMock
    let context = buildMockContext None
        
    task {
        let! response = (handler "2020-10-18T00:00:00Z") next context
        response.IsSome |> should equal true

        let context = response.Value
        let body = getBody context
        let expected = serializeToCamelCaseJsonString dummyExchange

        body |> should equal expected
    }

[<Fact>]
let ``Should not return an exchange when it is missing`` () =
    let handler = getExchangeHandler getMissingExchangeMock
    let context = buildMockContext None
        
    task {
        let! response = (handler "2020-10-18T00:00:00Z") next context
        response.IsSome |> should equal true

        let context = response.Value
        let body = getBody context
        let expected = "\"Market Week Not Found.\""

        body |> should equal expected
        response.Value.Response.StatusCode |> should equal StatusCodes.Status404NotFound
    }

[<Fact>]
let ``Should return bad request when the date is invalid`` () =
    let handler = getExchangeHandler getMissingExchangeMock
    let context = buildMockContext None
        
    task {
        let invalidDateString = "INVALID_DATE"
        let! response = (handler invalidDateString) next context
        response.IsSome |> should equal true

        let context = response.Value
        let body = getBody context
        let expected = "\"Invalid Date.\""

        body |> should equal expected
        response.Value.Response.StatusCode |> should equal StatusCodes.Status400BadRequest
    }

[<Fact>]
let ``Should return bad request when the supplied date is not a Sunday`` () =
    let handler = getExchangeHandler getMissingExchangeMock
    let context = buildMockContext None
        
    task {
        let invalidDateString = "2020-10-19T00:00:00Z"
        let! response = (handler invalidDateString) next context
        response.IsSome |> should equal true

        let context = response.Value
        let body = getBody context
        let expected = "\"Please Provide a Valid Week Start Date. The Date Provided Is Not a Sunday. Instead Was Given: Monday\""

        body |> should equal expected
        response.Value.Response.StatusCode |> should equal StatusCodes.Status400BadRequest
    }