module GetExchangeHandlerTest

open GetExchangeHandler
open Xunit
open FsUnit
open System
open Newtonsoft.Json.Bson
open MongoDB.Bson
open System.Threading.Tasks
open FSharp.Control.Tasks.V2
open Giraffe
open NSubstitute
open Microsoft.AspNetCore.Http
open Giraffe.Serialization
open System.IO
open System.Text
open Newtonsoft.Json

let next : HttpFunc = Some >> Task.FromResult

let buildMockContext () =
    let context = Substitute.For<HttpContext>()
    context.RequestServices.GetService(typeof<INegotiationConfig>).Returns(DefaultNegotiationConfig()) |> ignore
    context.RequestServices.GetService(typeof<Json.IJsonSerializer>).Returns(NewtonsoftJsonSerializer(NewtonsoftJsonSerializer.DefaultSettings)) |> ignore
    context.Request.Headers.ReturnsForAnyArgs(HeaderDictionary()) |> ignore
    context.Response.Body <- new MemoryStream()
    context

let getBody (ctx : HttpContext) =
    ctx.Response.Body.Position <- 0L
    use reader = new StreamReader(ctx.Response.Body, Encoding.UTF8)
    reader.ReadToEnd()

let dummyExchange: Exchange.Exchange = 
    {
        Id = BsonObjectId(ObjectId.GenerateNewId());
        WeekStartDate = DateTime(2020, 10, 18);
        Markets = [||];
    } 

let getExistingExchangeMock (weekStartDate: DateTime) = Some dummyExchange |> Task.FromResult |> Async.AwaitTask 
let getMissingExchangeMock (weekStartDate: DateTime): Async<Exchange.Exchange option> = None |> Task.FromResult |> Async.AwaitTask 

[<Fact>]
let ``Should return not found when exchange is missing`` () =
    let handler = getExchangeHandler getExistingExchangeMock
    let context = buildMockContext()
        
    task {
        let! response = (handler "2020-10-18T00:00:00Z") next context
        Assert.True(response.IsSome)
        let context = response.Value
        let body = getBody context
        Assert.Equal(JsonConvert.SerializeObject(dummyExchange), body)
    }