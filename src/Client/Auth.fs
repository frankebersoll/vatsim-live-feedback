module Client.Auth

open Elmish
open Feliz.Router

let navigateToAuthCmd () =

    let authenticate _ =
        async {
            let! config = Api.meetings.GetAuthenticationInfo()

            let query =
                Router.encodeQueryString (
                    [ ("client_id", config.ClientId)
                      ("redirect_uri", config.RedirectUri)
                      ("response_type", "code")
                      ("scope", "full_name") ]
                )

            let uri =
                $"{config.Authority}/oauth/authorize{query}"

            Browser.Dom.window.location.assign uri
        }
        |> Async.StartImmediate

    Cmd.ofSub authenticate

let handleCode (code: string) =
    let handleCode dispatch =
        async {
            let! authResult = Api.meetings.Authorize code
            dispatch (Model.Authenticated authResult)
        }
        |> Async.StartImmediate

    Cmd.batch [
        Cmd.ofMsg (Model.NavigateTo(Model.Home None))
        Cmd.ofSub handleCode
    ]
