module Client.Model

open Feliz.Router
open Shared.Model

type Page =
    | Home of CallSign option
    | OidcSignin of code: string
    | Privacy
    | NotFound

type AuthModel =
    | Unauthenticated
    | Authenticating
    | User of string

type Model =
    { Page: Page
      Authentication: AuthModel
      Meetings: MeetingInfo list
      CallSign: CallSign option
      Input: string
      Message: string option }

type Msg =
    | SignIn
    | Authenticated of string
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
    | [ Shared.Route.oauthCallbackPath; Route.Query [ "code", code ] ] -> Page.OidcSignin code
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
    | Page.OidcSignin code -> Router.formatPath(Shared.Route.oauthCallbackPath, [("code", code)])
    | Page.NotFound -> Router.formatPath "notfound"

let toFullUrl (url: string) =
    let location = Browser.Dom.window.location
    System.Uri(System.Uri($"{location.protocol}//{location.host}"), url).ToString()

