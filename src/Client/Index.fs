module Client.Index

open Client.Model
open Elmish
open Feliz.Router
open Shared
open Shared.Model

let onPageChanged model page =
    match page with
    | Page.Home (Some callSign) ->
        model, Cmd.OfAsync.perform Api.meetings.GetMeetings callSign (fun meetings -> GotMeetings(callSign, meetings))
    | Page.OidcSignin code ->
        { model with
              Authentication = Authenticating },
        Auth.handleCode code
    | _ -> model, Cmd.none

let init () : Model * Cmd<Msg> =
    let page = Router.currentPath () |> parseUrl

    let model =
        { Page = page
          Authentication = AuthModel.Unauthenticated
          Meetings = []
          CallSign = None
          Input = ""
          Message = None }

    onPageChanged model page

let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
    match msg with
    | SignIn -> { model with Authentication = Authenticating }, Auth.navigateToAuthCmd ()
    | Authenticated user ->
        { model with
              Authentication = User user },
        Cmd.none
    | NavigateTo page -> model, Cmd.navigatePath (page |> getUrl)
    | UrlChanged segments ->
        let page = parseUrl segments
        onPageChanged { model with Page = page } page
    | GotMeetings (callSign, meetings) ->
        match meetings with
        | [] ->
            { model with
                  Input = ""
                  Message = Some $"Call sign '{callSign}' not online." },
            Cmd.none
        | _ ->
            { model with
                  CallSign = Some callSign
                  Meetings = meetings },
            Cmd.none
    | SetInput value ->
        { model with
              Input =
                  value
                      .ToUpper()
                      .Replace("-", "_")
                      .Replace(" ", "_")
              Message = None },
        Cmd.none
    | SetCallSign ->
        if
            System.String.IsNullOrWhiteSpace(model.Input)
            |> not
        then
            let callSign = model.Input |> CallSign.FromString
            { model with Input = "" }, Cmd.navigatePath (Page.Home(Some callSign) |> getUrl)
        else
            model, Cmd.none
    | UnhandledError e ->
        { model with
              Model.Message = Some e.Message },
        Cmd.none

open Feliz
open Feliz.Bulma

let containerBox (model: Model) (dispatch: Msg -> unit) =
    Bulma.box [
        match model.CallSign with
        | Some callSign -> Bulma.title (callSign.ToString())
        | None -> Html.none
        match model.Message with
        | Some message ->
            Bulma.message [
                color.isDanger
                prop.children [
                    Bulma.messageBody message
                ]
            ]
        | None -> Html.none
        Bulma.content [
            Html.ol [
                for todo in model.Meetings do
                    Html.li [
                        prop.text $"{todo.CallSign} ({todo.Name})"
                        prop.onClick
                            (fun _ ->
                                SetInput(todo.CallSign.ToString()) |> dispatch
                                SetCallSign |> dispatch)
                    ]
            ]
        ]
        Bulma.field.div [
            field.isGrouped
            prop.children [
                Bulma.control.p [
                    control.isExpanded
                    prop.children [
                        Bulma.input.text [
                            prop.value model.Input
                            prop.placeholder "Enter Call Sign"
                            prop.onKeyDown (key.enter, (fun _ -> dispatch SetCallSign))
                            prop.onChange (fun x -> SetInput x |> dispatch)
                        ]
                    ]
                ]
                Bulma.control.p [
                    Bulma.button.a [
                        color.isDark
                        prop.disabled (System.String.IsNullOrWhiteSpace(model.Input))
                        prop.onClick (fun _ -> dispatch SetCallSign)
                        prop.text "Get Comms"
                    ]
                ]
            ]
        ]
    ]

let index (model: Model) (dispatch: Msg -> unit) =
    Common.MainTemplate
        model
        dispatch
        [ Bulma.column [
              column.is6
              column.isOffset3
              prop.children [
                  containerBox model dispatch
              ]
          ] ]

[<ReactComponent>]
let MainView (model: Model) (dispatch: Msg -> unit) =
    let dispatch = React.useCallbackRef dispatch

    React.router [
        router.pathMode
        router.onUrlChanged (UrlChanged >> dispatch)
        router.children [
            match model.Page with
            | Page.Home _ -> index model dispatch
            | Page.OidcSignin _ -> index model dispatch
            | Page.Privacy -> Privacy.privacy model dispatch
            | Page.NotFound -> Html.h1 "Not found"
        ]
    ]
