module Run

open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open System.Threading.Tasks
open FSharp.Control.Tasks.V2
open Microsoft.Extensions.Logging
open System

[<CLIMutable>] 
type CreateExchangeModel =
  { 
    UserName      : string
    WeekStartDate : DateTime
  }

let createMarketHandler : HttpHandler =
  fun (next : HttpFunc) (ctx : HttpContext) ->
    task {
      let! createMarketModel = ctx.BindJsonAsync<CreateExchangeModel>()
      let! result = StalkExchangeRepository.createMarket createMarketModel.WeekStartDate createMarketModel.UserName

      return! match result with
              | Some r  -> Successful.CREATED r next ctx
              | None    -> ServerErrors.INTERNAL_ERROR "Unable to create market week" next ctx
    }

let parseDate (dateString: string) =
  let couldParse, parsedDate = System.DateTime.TryParse(dateString)

  if couldParse 
    then Ok parsedDate 
    else Error (RequestErrors.NOT_FOUND "Invalid Date.")

let validateWeekStartDate (weekStartDate: DateTime) =
  let dayOfWeek = Enum.GetName(typeof<DayOfWeek>, weekStartDate.ToUniversalTime().DayOfWeek)

  if weekStartDate.ToUniversalTime().DayOfWeek = DayOfWeek.Sunday 
    then Ok weekStartDate 
    else Error (RequestErrors.BAD_REQUEST (sprintf "Please Provide a Valid Week Start Date. The Date Provided Is Not a Sunday. Instead Was Given: %s" dayOfWeek))

let getMarkets (weekStartDate: DateTime) = 
  task { 
    let! markets = StalkExchangeRepository.getMarketByWeek weekStartDate

    return match markets with
            | Some m  -> Ok m 
            | None    -> Error (RequestErrors.NOT_FOUND "Market Week Not Found.")
  }

let (>>=) x f = Result.bind f x
let (>>=!) (x: Result<'a, 'b>) (f: 'a -> Task<Result<'c, 'b>>) = 
    match x with
      | Ok value -> f value
      | Error e  -> (Error e |> Task.FromResult)

let getMarketHandler (weekStartDateString: string) : HttpHandler =
  fun (next : HttpFunc) (ctx : HttpContext) ->
    task {
      let! result = parseDate weekStartDateString >>= validateWeekStartDate >>=! getMarkets

      return! match result with
              | Ok markets  -> Successful.OK markets next ctx
              | Error error -> error next ctx
    }

let app : HttpHandler =

  choose [

    POST  >=> route "/api/exchange" >=> warbler (fun _ -> createMarketHandler)

    GET   >=> routef "/api/exchange/%s" getMarketHandler

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