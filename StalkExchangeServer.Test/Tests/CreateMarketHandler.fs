module CreateMarketHandlerTest

open FsUnit
open GiraffeTestUtilities
open Exchange
open System
open MongoDB.Bson
open StalkExchangeRepository
open System.Threading.Tasks
open Xunit
open CreateMarketHandler
open System.Text
open Newtonsoft.Json
open System.IO
open FSharp.Control.Tasks.V2.ContextSensitive
open Microsoft.AspNetCore.Http

let dummyCreateMarketModel = {
    UserName    = "drew"
}

let dummyExchange: Exchange.Exchange = 
    {
        Id = BsonObjectId(ObjectId.GenerateNewId());
        WeekStartDate = DateTime.SpecifyKind(DateTime(2020, 10, 18), DateTimeKind.Utc);
        Markets = [|{ 
                UserName        = "drew"
                PurchasePrice   = Some 23
                MondayPrice     = None 
                TuesdayPrice    = None 
                WednesdayPrice  = None 
                ThursdayPrice   = None 
                FridayPrice     = None 
                SaturdayPrice   = None 
        }|];
    } 

let createMarketSuccessMock (exchangeId: string) (username: string) = MarketCreate.Success dummyExchange |> Task.FromResult
let createMarketNotFoundMock (exchangeId: string) (username: string) = MarketCreate.ExchangeNotFound |> Task.FromResult
let createMarketErrorMock (exchangeId: string) (username: string) = MarketCreate.Error |> Task.FromResult

[<Fact>]
let ``Should update a market`` () =
    let handler = createMarketHandler createMarketSuccessMock
    let id = "123"
    let postData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(dummyCreateMarketModel))
    
    let context = buildMockContext None
    context.Request.Body <- new MemoryStream(postData)   
        
    task {
        let! response = (handler id ) next context
        response.IsSome |> should equal true

        let context = response.Value
        let body = getBody context
        let expected = dummyExchange |> toExchangeResponse |> serializeToCamelCaseJsonString 

        body |> should equal expected
        context.Response.StatusCode |> should equal StatusCodes.Status201Created
    }

[<Fact>]
let ``Should return not found when the market does not exist`` () =
    let handler = createMarketHandler createMarketNotFoundMock
    let id = "123"
    let postData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(dummyCreateMarketModel))
    
    let context = buildMockContext None
    context.Request.Body <- new MemoryStream(postData)   
        
    task {
        let! response = (handler id) next context
        response.IsSome |> should equal true

        let context = response.Value
        let body = getBody context
        let expected = serializeToCamelCaseJsonString "Exchange not found."

        body |> should equal expected
        context.Response.StatusCode |> should equal StatusCodes.Status404NotFound
    }

[<Fact>]
let ``Should return an error when an unrecoverable error occurs`` () =
    let handler = createMarketHandler createMarketErrorMock
    let id = "123"
    let postData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(dummyCreateMarketModel))
    
    let context = buildMockContext None
    context.Request.Body <- new MemoryStream(postData)   
        
    task {
        let! response = (handler id) next context
        response.IsSome |> should equal true

        let context = response.Value
        let body = getBody context
        let expected = serializeToCamelCaseJsonString "An internal error occurred."

        body |> should equal expected
        context.Response.StatusCode |> should equal StatusCodes.Status500InternalServerError
    }