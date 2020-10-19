module GetExchangeHandler

open Giraffe
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2
open System
open ResultUtilities
open DateUtility


let validateWeekStartDate (weekStartDate: DateTime) =
  let dayOfWeek = Enum.GetName(typeof<DayOfWeek>, weekStartDate.ToUniversalTime().DayOfWeek)

  if weekStartDate.ToUniversalTime().DayOfWeek = DayOfWeek.Sunday 
    then Ok weekStartDate 
    else Error (RequestErrors.BAD_REQUEST (sprintf "Please Provide a Valid Week Start Date. The Date Provided Is Not a Sunday. Instead Was Given: %s" dayOfWeek))


let getExchange (weekStartDate: DateTime) = 
  task { 
    let! markets = StalkExchangeRepository.getExchangeByWeek weekStartDate

    return match markets with
            | Some m  -> Ok m 
            | None    -> Error (RequestErrors.NOT_FOUND "Market Week Not Found.")
  }


let getExchangeHandler (weekStartDateString: string) : HttpHandler =
  fun (next : HttpFunc) (ctx : HttpContext) ->
    task {
      let! result = parseDate weekStartDateString >>= validateWeekStartDate >>=! getExchange

      return! match result with
              | Ok markets  -> Successful.OK markets next ctx
              | Error error -> error next ctx
    }