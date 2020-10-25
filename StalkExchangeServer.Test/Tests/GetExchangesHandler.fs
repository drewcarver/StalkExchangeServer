module GetExchangesHandlerTest

open GetExchangesHandler
open Xunit
open FsUnit
open System
open MongoDB.Bson
open System.Threading.Tasks
open FSharp.Control.Tasks.V2
open GiraffeTestUtilities
open Microsoft.Extensions.Primitives
open Microsoft.AspNetCore.Http


let dummyExchange: Exchange.Exchange = 
    {
        Id = BsonObjectId(ObjectId.GenerateNewId());
        WeekStartDate = DateTime.SpecifyKind(DateTime(2020, 10, 18), DateTimeKind.Utc);
        Markets = [||];
    } 

let getExchangesMock (weekStartDate: DateTime option) = 
    [dummyExchange; dummyExchange] 
    |> List.filter (fun e -> 
        match weekStartDate with
        | Some weekStartDate    -> e.WeekStartDate = weekStartDate
        | None                  -> true
       ) 
    |> Task.FromResult  

[<Fact>]
let ``Should return exchanges`` () =
    let handler = getExchangesHandler getExchangesMock
    let context = buildMockContext None
        
    task {
        let! response = handler next context
        response.IsSome |> should equal true

        let context = response.Value
        let body = getBody context
        let expected = serializeToCamelCaseJsonString [dummyExchange; dummyExchange]

        body |> should equal expected
    }

[<Fact>]
let ``Should filter exchanges by week start date`` () =
    let handler = getExchangesHandler getExchangesMock
    let querystringParameters = 
        seq ["WeekStartDate", StringValues("2020-10-11T00:00:00Z")]
        |> Map

    let context = buildMockContext (Some querystringParameters)
        
    task {
        let! response = handler next context
        response.IsSome |> should equal true

        let context = response.Value
        let body = getBody context
        let expected = serializeToCamelCaseJsonString []

        body |> should equal expected
    }

[<Fact>]
let ``Should return bad request when the start date is not a Sunday`` () =
    let handler = getExchangesHandler getExchangesMock
    let querystringParameters = 
        seq ["WeekStartDate", StringValues("2020-10-10T00:00:00Z")]
        |> Map

    let context = buildMockContext (Some querystringParameters)
        
    task {
        let! response = handler next context
        response.IsSome |> should equal true

        let context = response.Value
        let body = getBody context
        let expectedBody = "\"Please Provide a Valid Week Start Date. The Date Provided Is Not a Sunday. Instead Was Given: Saturday\""

        context.Response.StatusCode |> should equal StatusCodes.Status400BadRequest
        body |> should equal expectedBody
    }