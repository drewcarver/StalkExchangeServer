module GetExchangesHandler

open Giraffe
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2
open System.Threading.Tasks
open System
open System.Linq
open Microsoft.FSharp.Collections

let getExchangesBuilder (weekStartDate: DateTime option) = 
  let exchangeCollection = StalkExchangeRepository.getExchangeCollection () 
  StalkExchangeRepository.getExchanges exchangeCollection weekStartDate

let validateWeekStartDate (weekStartDate: DateTime) =
  let dayOfWeek = Enum.GetName(typeof<DayOfWeek>, weekStartDate.ToUniversalTime().DayOfWeek)

  if weekStartDate.ToUniversalTime().DayOfWeek = DayOfWeek.Sunday 
    then Ok weekStartDate 
    else Error (RequestErrors.BAD_REQUEST (sprintf "Please Provide a Valid Week Start Date. The Date Provided Is Not a Sunday. Instead Was Given: %s" dayOfWeek))

let getExchangesHandler (getExchanges: DateTime option -> Task<Exchange.Exchange list>) : HttpHandler = 
  fun (next : HttpFunc) (ctx : HttpContext) -> 
    let queryStringGetter = QueryStringUtility.tryGetQuerystringValue ctx.Request.Query
    let weekStartDate = 
      queryStringGetter "WeekStartDate"
      |> Option.bind (seq >> Seq.head >> DateUtility.parseDate)
    
    let exchangesResult = 
      match weekStartDate with 
      | Some weekStartDate  -> validateWeekStartDate weekStartDate |> Result.map (Some >> getExchanges)
      | None                -> getExchanges None |> Ok

    let response = 
      match exchangesResult with
      | Ok exchanges  -> task {
          let! exchangesResult = exchanges
          return! Successful.OK exchangesResult next ctx
        }
      | Error e       -> e next ctx
    response