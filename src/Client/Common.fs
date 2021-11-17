module Client.Common

open Browser.Types
open Client.Model
open Feliz
open Feliz.Bulma
open Feliz.Router

let goTo (dispatch: Msg -> unit) (page: Page) (e: MouseEvent) =
    e.preventDefault ()
    dispatch (NavigateTo page)

type LinkTarget =
    | Url of string
    | Page of Page

module prop =
    let navigate (dispatch: Msg -> unit) (page: Page) =
        let url = Router.formatPath ()

        [ prop.href url
          prop.onClick (goTo dispatch page) ]

let link dispatch target props =
    Html.a [
        match target with
        | Url url ->
            prop.href url
            prop.target.blank
        | Page page -> yield! prop.navigate dispatch page
        yield! props
    ]

let icon (cls: string) =
    Bulma.icon [
        icon.isSmall
        prop.children [
            Html.i [ prop.className cls ]
        ]
    ]

type DispatchProps = { Dispatch: Msg -> unit }

let header props =
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

    Bulma.heroHead [
        Bulma.navbar [
            prop.style [
                style.backgroundColor (color.rgba (0x33, 0x33, 0x33, 0.9))
            ]
            prop.children [
                Bulma.container [
                    navBarBrand
                    Bulma.navbarMenu [
                        Bulma.navbarEnd.div [
                            Bulma.navbarItem.div [
                                Bulma.tag [
                                    color.isWarning
                                    prop.text "This app is under active development"
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]

let rec Header = React.memo (nameof Header, header)

let footer props =
    Bulma.heroFoot [
        Bulma.tabs [
            tabs.isSmall
            prop.children [
                Bulma.container [
                    let link (iconClass: string) (text: string) target =
                        Html.li [
                            link
                                props.Dispatch
                                target
                                [ prop.children [
                                      icon iconClass
                                      Html.text text
                                  ] ]
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
let MainTemplate (dispatch: Msg -> unit) (content: ReactElement seq) =
    Bulma.hero [
        hero.isFullHeight
        prop.children [
            Header { Dispatch = dispatch }
            Bulma.heroBody [
                prop.children [
                    Bulma.container content
                ]
            ]
            Footer { Dispatch = dispatch }
        ]
    ]
