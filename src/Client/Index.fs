module Client.Index

open Elmish
open Fable.Remoting.Client
open Shared
open Shared.Model

type Model =
    { Meetings: MeetingInfo list
      CallSign: CallSign option
      Input: string
      Message: string option }

type Msg =
    | GotMeetings of CallSign * MeetingInfo list
    | SetInput of string
    | SetCallSign

let meetingsApi =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<IMeetingsApi>

let init () : Model * Cmd<Msg> =
    let model =
        { Meetings = []
          CallSign = None
          Input = ""
          Message = None }

    model, Cmd.none

let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
    match msg with
    | GotMeetings (callSign, meetings) ->
        match meetings with
        | [] ->
            { model with
                  Input = ""
                  Message = Some "Not found." },
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

            let cmd =
                Cmd.OfAsync.perform meetingsApi.GetMeetings callSign (fun meetings -> GotMeetings(callSign, meetings))

            { model with
                  Input = ""
                  CallSign = None },
            cmd
        else
            model, Cmd.none

open Feliz
open Feliz.Bulma

let navBrand =
    let icon =
        HtmlHelpers.imageUrl "./public/FeedbackLogo.png"

    Bulma.navbarBrand.div [
        Bulma.navbarItem.a [
            prop.href "/"
            prop.children [
                Html.img [
                    prop.src icon
                    prop.alt "Logo"
                    prop.style [ style.marginRight 10 ]
                ]
                Html.h1 [
                    prop.style [
                        style.color.white
                        style.fontWeight.bold
                        style.fontSize (length.em 1.2)
                    ]
                    prop.text "Live Feedback"
                ]
            ]
        ]
    ]

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
                            Interaction.onEnter (fun () -> dispatch SetCallSign)
                            prop.onChange (fun x -> SetInput x |> dispatch)
                        ]
                    ]
                ]
                Bulma.control.p [
                    Bulma.button.a [
                        color.isPrimary
                        prop.disabled (System.String.IsNullOrWhiteSpace(model.Input))
                        prop.onClick (fun _ -> dispatch SetCallSign)
                        prop.text "Get Comms"
                    ]
                ]
            ]
        ]
    ]

let view (model: Model) (dispatch: Msg -> unit) =
    Bulma.hero [
        hero.isFullHeight
        prop.style [
            style.backgroundImageUrl (HtmlHelpers.imageUrl "./public/background.jpg")
            style.backgroundSize.cover
            style.backgroundRepeat.noRepeat
            style.backgroundPosition "center"
        ]
        prop.children [
            Bulma.heroHead [
                Bulma.navbar [
                    navbar.isFixedTop
                    prop.style [
                        style.backgroundColor (color.rgba (0x33, 0x33, 0x33, 0.9))
                    ]
                    prop.children [
                        Bulma.container [ navBrand ]
                    ]
                ]
            ]
            Bulma.heroBody [
                Bulma.container [
                    Bulma.column [
                        column.is6
                        column.isOffset3
                        prop.children [
                            containerBox model dispatch
                        ]
                    ]
                ]
            ]
            Bulma.heroFoot [
                Bulma.tabs [
                    Bulma.container [
                        let link (text: string) (href: string) =
                            Html.li [
                                Html.a [
                                    prop.text text
                                    prop.href href
                                ]
                            ]

                        Html.ul [
                            link "GitHub" "https://github.com/frankebersoll/vatsim-live-feedback"
                            link "Legal Notice" "https://www.youtube.com/watch?v=dQw4w9WgXcQ"
                        ]
                    ]
                ]
            ]
        ]
    ]
