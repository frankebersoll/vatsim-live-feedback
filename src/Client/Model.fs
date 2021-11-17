module Client.Model

open Feliz.Router
open Shared.Model

type Page =
    | Home of CallSign option
    | Privacy
    | NotFound

type Model =
    { Page: Page
      Meetings: MeetingInfo list
      CallSign: CallSign option
      Input: string
      Message: string option }

type Msg =
    | NavigateTo of Page
    | UrlChanged of string list
    | GotMeetings of CallSign * MeetingInfo list
    | SetInput of string
    | SetCallSign
    | UnhandledError of exn

open Feliz

let parseUrl =
    function
    | [] -> Page.Home None
    | [ Route.Query [ "callsign", value ] ] -> Page.Home(Some(CallSign.FromString value))
    | [ "privacy" ] -> Page.Privacy
    | _ -> Page.NotFound

let getUrl =
    function
    | Home callSign ->
        Router.formatPath (
            "",
            callSign
            |> Option.map (fun c -> [ ("callsign", c.ToString()) ])
            |> Option.defaultValue []
        )
    | Privacy -> Router.formatPath "privacy"
    | NotFound -> failwith "Cannot create URL for 'Not found'"

let getFullUrl page =
    let location = Browser.Dom.window.location
    let url = page |> getUrl
    $"{location.protocol}//{location.host}{url}"
