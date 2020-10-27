module UpdateMarketHandlerTest

open FsUnit
open GiraffeTestUtilities
open Exchange
open System
open MongoDB.Bson
open StalkExchangeRepository
open System.Threading.Tasks
open Xunit
open UpdateMarketHandler
open System.Text
open Newtonsoft.Json
open System.IO
open FSharp.Control.Tasks.V2.ContextSensitive
open Microsoft.AspNetCore.Http

let dummyMarketModel = {
  PurchasePrice   = Option.toNullable (Some 23)
  MondayPrice     = Option.toNullable None 
  TuesdayPrice    = Option.toNullable None 
  WednesdayPrice  = Option.toNullable None 
  ThursdayPrice   = Option.toNullable None 
  FridayPrice     = Option.toNullable None 
  SaturdayPrice   = Option.toNullable None 
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

let updateMarketSuccessMock (exchangeId: string) (market: Market.Market) = Success dummyExchange |> Task.FromResult
let updateMarketNotFoundMock (exchangeId: string) (market: Market.Market) = NotFound |> Task.FromResult
let updateMarketErrorMock (exchangeId: string) (market: Market.Market) = Error |> Task.FromResult

[<Fact>]
let ``Should update a market`` () =
    let handler = updateMarketHandler updateMarketSuccessMock
    let id = "123"
    let putData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(dummyMarketModel))
    
    let context = buildMockContext None
    context.Request.Body <- new MemoryStream(putData)   
        
    task {
        let! response = (handler id "drew") next context
        response.IsSome |> should equal true

        let context = response.Value
        let body = getBody context
        let expected = dummyExchange |> toExchangeResponse |> serializeToCamelCaseJsonString 

        body |> should equal expected
        context.Response.StatusCode |> should equal StatusCodes.Status200OK
    }

[<Fact>]
let ``Should return not found when the market does not exist`` () =
    let handler = updateMarketHandler updateMarketNotFoundMock 
    let id = "123"
    let putData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(dummyMarketModel))
    
    let context = buildMockContext None
    context.Request.Body <- new MemoryStream(putData)   
        
    task {
        let! response = (handler id "drew") next context
        response.IsSome |> should equal true

        let context = response.Value
        let body = getBody context
        let expected = serializeToCamelCaseJsonString "Unable to update market. Exchange does not exist."

        body |> should equal expected
        context.Response.StatusCode |> should equal StatusCodes.Status404NotFound
    }

[<Fact>]
let ``Should return an error when an unrecoverable error occurs`` () =
    let handler = updateMarketHandler updateMarketErrorMock
    let id = "123"
    let putData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(dummyMarketModel))
    
    let context = buildMockContext None
    context.Request.Body <- new MemoryStream(putData)   
        
    task {
        let! response = (handler id "drew") next context
        response.IsSome |> should equal true

        let context = response.Value
        let body = getBody context
        let expected = serializeToCamelCaseJsonString "Unexpected server error"

        body |> should equal expected
        context.Response.StatusCode |> should equal StatusCodes.Status500InternalServerError
    }