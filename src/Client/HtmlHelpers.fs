module Client.HtmlHelpers

open Fable.Core.JsInterop

let inline imageUrl path : string = importDefault path