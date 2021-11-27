module Client.App

open Elmish
open Elmish.React


#if DEBUG
open Elmish.HMR
#endif

Lib.CookieConsent.run (
    let privacyUrl = Model.getUrl Model.Page.Privacy |> Model.toFullUrl

    {| notice_banner_type = "simple"
       consent_type = "express"
       palette = "light"
       language = "en"
       page_load_consent_levels = [ "strictly-necessary" ]
       notice_banner_reject_button_hide = false
       preferences_center_close_button_hide = false
       website_privacy_policy_url = privacyUrl |}
)

Program.mkProgram Index.init Index.update Index.MainView
#if DEBUG
|> Program.withConsoleTrace
#endif
|> Program.withReactSynchronous "app-root"
|> Program.run
