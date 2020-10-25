module QueryStringUtility
open Microsoft.AspNetCore.Http

let tryGetQuerystringValue (query: IQueryCollection) (key: string) =
  match query.TryGetValue key with
  | true, value   -> Some value
  | _             -> None
