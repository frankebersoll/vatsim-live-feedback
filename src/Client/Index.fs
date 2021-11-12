module Client.Index

open Elmish
open Fable.Remoting.Client
open Shared

type Model = { Todos: Todo list; Input: string }

type Msg =
    | GotTodos of Todo list
    | SetInput of string
    | AddTodo
    | AddedTodo of Todo

let todosApi =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<ITodosApi>

let init () : Model * Cmd<Msg> =
    let model = { Todos = []; Input = "" }

    let cmd =
        Cmd.OfAsync.perform todosApi.getTodos () GotTodos

    model, cmd

let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
    match msg with
    | GotTodos todos -> { model with Todos = todos }, Cmd.none
    | SetInput value -> { model with Input = value }, Cmd.none
    | AddTodo ->
        if model.Input |> Todo.isValid then
            let todo = Todo.create model.Input
            let cmd =
                Cmd.OfAsync.perform todosApi.addTodo todo AddedTodo

            { model with Input = "" }, cmd
        else
            model, Cmd.none
    | AddedTodo todo ->
        { model with
              Todos = model.Todos @ [ todo ] },
        Cmd.none

open Feliz
open Feliz.Bulma

let navBrand =
    let icon = HtmlHelpers.imageUrl "./public/FeedbackLogo.png"
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
                        style.fontSize(length.em 1.2)
                    ]
                    prop.text "Live Feedback"
                ]
            ]
        ]
    ]

let containerBox (model: Model) (dispatch: Msg -> unit) =
    Bulma.box [
        Bulma.content [
            Html.ol [
                for todo in model.Todos do
                    Html.li [ prop.text todo.Description ]
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
                            prop.placeholder "VATSIM ID (CID)"
                            Interaction.onEnter (fun () -> dispatch AddTodo)
                            prop.onChange (fun x -> SetInput x |> dispatch)
                        ]
                    ]
                ]
                Bulma.control.p [
                    Bulma.button.a [
                        color.isPrimary
                        prop.disabled (Todo.isValid model.Input |> not)
                        prop.onClick (fun _ -> dispatch AddTodo)
                        prop.text "Add"
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
                        style.backgroundColor(color.rgba(0x33, 0x33, 0x33, 0.9))
                    ]
                    prop.children [
                        Bulma.container [
                            navBrand
                        ]
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
                        let link (text: string) (href: string) = Html.li [ Html.a [ prop.text text; prop.href href ] ]
                        Html.ul [
                            link "GitHub" "https://github.com/frankebersoll/vatsim-live-feedback"
                            link "Legal Notice" "https://www.youtube.com/watch?v=dQw4w9WgXcQ"
                        ]
                    ]
                ]
            ]
        ]
    ]
