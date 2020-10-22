module Run

open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open System.Threading.Tasks
open FSharp.Control.Tasks.V2
open Microsoft.Extensions.Logging
open GetExchangeHandler
open CreateExchangeHandler
open CreateMarketHandler
open UpdateMarketHandler

let app : HttpHandler =

  choose [

    POST  >=> route "/api/exchange" >=> warbler (fun _ -> createExchangeHandler)

    GET   >=> routef "/api/exchange/%s" getExchangeHandler

    POST  >=> routef "/api/exchange/%s/markets" createMarketHandler

    PUT   >=> routef "/api/exchange/%s/markets/%s" (fun (weekStartDate, username) -> updateMarketHandler weekStartDate username)

    RequestErrors.NOT_FOUND "Not Found"

  ]

let errorHandler (ex : exn) (logger : ILogger) =
    logger.LogError(EventId(), ex, "An unhandled exception has occurred while executing the request.")
    clearResponse
    >=> ServerErrors.INTERNAL_ERROR ex.Message

[<FunctionName "StalkExchangeAPI">]
let run ([<HttpTrigger (AuthorizationLevel.Anonymous, Route = "{*any}")>] req : HttpRequest, context : ExecutionContext, log : ILogger) =
  let hostingEnvironment = req.HttpContext.GetHostingEnvironment()
  hostingEnvironment.ContentRootPath <- context.FunctionAppDirectory
  let func = Some >> Task.FromResult
  { new Microsoft.AspNetCore.Mvc.IActionResult with
      member _.ExecuteResultAsync(ctx) = 
        task {
          try
            return! app func ctx.HttpContext :> Task
          with exn ->
            return! errorHandler exn log func ctx.HttpContext :> Task
        }
        :> Task }