module Client.Common

open Browser.Types
open Client.Model
open Feliz
open Feliz.Bulma

let onClickDispatch (msg: Msg) (dispatch: Msg -> unit) (e: MouseEvent) =
    e.preventDefault ()
    dispatch msg

let goTo (page: Page) dispatch e =
    onClickDispatch (NavigateTo page) dispatch e

type LinkTarget =
    | Url of string
    | Page of Page

module prop =
    let navigate (dispatch: Msg -> unit) (page: Page) =
        let url = getUrl page

        [ prop.href url
          prop.onClick (goTo page dispatch) ]

let link dispatch target =
    [ match target with
      | Url url ->
          prop.href url
          prop.target.blank
      | Page page -> yield! prop.navigate dispatch page ]

let icon (cls: string) =
    Bulma.icon [
        icon.isSmall
        prop.children [
            Html.i [ prop.className cls ]
        ]
    ]

type HeaderProps =
    { Dispatch: Msg -> unit
      Auth: AuthModel }

type FooterProps = { Dispatch: Msg -> unit }

let activeDevelopment () =
    Bulma.tag [
        color.isWarning
        prop.text "This app is under active development"
    ]

let header (props: HeaderProps) =
    let icon =
        Lib.Parcel.publicAsset "FeedbackLogo.png"

    let navBarBrand =
        Bulma.navbarBrand.div [
            Bulma.navbarItem.a [
                yield! prop.navigate props.Dispatch (Page.Home None)
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

    let navbarAuthItem =
        Bulma.navbarItem.div [
            let isAuthenticating =
                match props.Auth with
                | Authenticating -> true
                | _ -> false

            match props.Auth with
            | User user ->
                Html.div [
                    color.hasTextWhite
                    prop.children [
                        Bulma.icon [
                            Html.i [ prop.className "fas fa-user" ]
                        ]
                        Html.span user
                    ]
                ]
            | _ ->
                Bulma.button.button [
                    if isAuthenticating then
                        button.isLoading
                    prop.text "Sign in"
                    prop.onClick (fun _ -> props.Dispatch Msg.SignIn)
                ]
        ]

    Bulma.heroHead [
        Bulma.navbar [
            prop.style [
                style.backgroundColor (color.rgba (0x33, 0x33, 0x33, 0.9))
            ]
            prop.children [
                Bulma.container [
                    navBarBrand
                    Bulma.navbarMenu [
                        Bulma.navbarEnd.div [ navbarAuthItem ]
                    ]
                ]
            ]
        ]
    ]

let rec Header = React.memo (nameof Header, header)

let footer (props: FooterProps) =
    Bulma.heroFoot [
        Bulma.tabs [
            tabs.isSmall
            prop.children [
                Bulma.container [
                    let link (iconClass: string) (text: string) target =
                        Html.li [
                            Html.a [
                                yield! link props.Dispatch target
                                prop.children [
                                    icon iconClass
                                    Html.text text
                                ]
                            ]
                        ]

                    Html.ul [
                        link "fas fa-mask" "Privacy Policy" (Page Page.Privacy)
                        link "fab fa-github" "GitHub" (Url Constants.githubProjectUrl)
                        link "fab fa-discord" "Discord" (Url Constants.discordInviteUrl)
                    ]
                ]
            ]
        ]
    ]

let rec Footer = React.memo (nameof Footer, footer)

[<ReactComponent>]
let MainTemplate (model: Model.Model) (dispatch: Msg -> unit) (content: ReactElement seq) =
    Bulma.hero [
        hero.isFullHeight
        prop.children [
            Header
                { Dispatch = dispatch
                  Auth = model.Authentication }
            Bulma.heroBody [
                prop.children [
                    Bulma.container content
                ]
            ]
            Footer { Dispatch = dispatch }
        ]
    ]
