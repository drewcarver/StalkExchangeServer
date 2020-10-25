module DateUtility

let parseDate (dateString: string) =
  match System.DateTime.TryParse dateString with
  | true, parsedDate    -> Some parsedDate
  | _                   -> None