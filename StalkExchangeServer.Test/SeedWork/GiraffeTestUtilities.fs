module GiraffeTestUtilities 

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
open Newtonsoft.Json.Serialization

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

let camelContractResolver =  DefaultContractResolver(NamingStrategy = CamelCaseNamingStrategy ())
let jsonSerializerSettings = JsonSerializerSettings(ContractResolver = camelContractResolver)

let serializeToCamelCaseJsonString value = JsonConvert.SerializeObject(value, jsonSerializerSettings)