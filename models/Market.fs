module Market

open Bells

[<CLIMutable>]
type Market = {
    UserName        : string
    PurchasePrice   : Bells option
    MondayPrice     : Bells option
    TuesdayPrice    : Bells option
    WednesdayPrice  : Bells option 
    ThursdayPrice   : Bells option 
    FridayPrice     : Bells option
    SaturdayPrice   : Bells option
}