[<AutoOpen>]
module SharedHelpers

let inline nullCoerce defaultValue value = if value = null then defaultValue else value

let inline nullCoerceBy defaultFactory value = if value = null then defaultFactory() else value