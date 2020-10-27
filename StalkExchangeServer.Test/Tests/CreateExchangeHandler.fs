module CreateExchangeHandlerTest

open CreateExchangeHandler
open Xunit
open FsUnit
open System
open MongoDB.Bson
open System.Threading.Tasks
open FSharp.Control.Tasks.V2
open Microsoft.AspNetCore.Http
open GiraffeTestUtilities
open System.Text
open Newtonsoft.Json
open System.IO

let dummyExchange: Exchange.Exchange = 
    {
        Id = BsonObjectId(ObjectId.GenerateNewId());
        WeekStartDate = DateTime.SpecifyKind(DateTime(2020, 10, 18), DateTimeKind.Utc);
        Markets = [||];
    } 

let createExchangeSuccessMock (weekStartDate: DateTime) = dummyExchange |> Ok |> Task.FromResult  
let createExchangeFailureMock (weekStartDate: DateTime) = "Market already exists" |> Error |> Task.FromResult  

[<Fact>]
let ``Should create a new exchange`` () =
    let handler = createExchangeHandler createExchangeSuccessMock 
    let weekStartDate = DateTime.SpecifyKind(DateTime(2020, 10, 18), DateTimeKind.Utc)
    let postData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject({ WeekStartDate = weekStartDate}))
    
    let context = buildMockContext None
    context.Request.Body <- new MemoryStream(postData)   
        
    task {
        let! response = handler next context
        response.IsSome |> should equal true

        let context = response.Value
        let body = getBody context
        let expected = serializeToCamelCaseJsonString dummyExchange

        body |> should equal expected
        context.Response.StatusCode |> should equal StatusCodes.Status201Created
    }

[<Fact>]
let ``Should return an error when unable to create a new exchange`` () =
    let handler = createExchangeHandler createExchangeFailureMock
    let weekStartDate = DateTime.SpecifyKind(DateTime(2020, 10, 18), DateTimeKind.Utc)
    let postData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject({ WeekStartDate = weekStartDate}))
    
    let context = buildMockContext None
    context.Request.Body <- new MemoryStream(postData)   
        
    task {
        let! response = handler next context
        response.IsSome |> should equal true

        let context = response.Value
        let body = getBody context
        let expected = "\"Market already exists\"" 

        body |> should equal expected
        context.Response.StatusCode |> should equal StatusCodes.Status409Conflict
    }