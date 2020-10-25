module DateUtility

open Giraffe

let parseDate (dateString: string) =
  let couldParse, parsedDate = System.DateTime.TryParse(dateString)

  if couldParse 
    then Ok parsedDate 
    else Error (RequestErrors.BAD_REQUEST "Invalid Date.")