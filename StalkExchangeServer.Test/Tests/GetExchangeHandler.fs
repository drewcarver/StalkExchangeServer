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

let getExistingExchangeMock (exchangeId: string) = Some dummyExchange |> Task.FromResult  
let getMissingExchangeMock (exchangeId: string) = None |> Task.FromResult 

[<Fact>]
let ``Should return an exchange`` () =
    let handler = getExchangeHandler getExistingExchangeMock
    let context = buildMockContext None
        
    task {
        let! response = (handler "123") next context
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
        let! response = (handler "123") next context
        response.IsSome |> should equal true

        let context = response.Value
        let body = getBody context
        let expected = "\"Market Week Not Found.\""

        body |> should equal expected
        response.Value.Response.StatusCode |> should equal StatusCodes.Status404NotFound
    }
