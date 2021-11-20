module Server.App

open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Saturn

open Shared
open Shared.Model

let store = Model.Store.create ()

let createApi (authState: Authentication.AuthenticationState) =

    let getMeetings callSign =
        async {
            let meetings = store.GetMeetings(callSign)

            let infos =
                meetings
                |> List.map
                    (fun m ->
                        { MeetingInfo.Start = m.Start
                          Name = m.Name
                          CallSign = m.TargetId.CallSign })

            return infos
        }

    let getUser =
        async {
            let user = Authentication.getUser authState
            return user
        }

    let authorize (code: string) =
        async {
            let! result = Authentication.authorize code authState

            match result with
            | Ok state ->
                let user = Authentication.getUser state
                return user.Identity.Name
            | Error s -> return s
        }

    let getAuthenticationInfo () =
        Authentication.getAuthConfig authState
        |> async.Return

    { GetMeetings = getMeetings
      Authorize = authorize
      GetAuthenticationInfo = getAuthenticationInfo }

let getApi (ctx: HttpContext) =
    let authSvc = Authentication.getState ctx
    createApi authSvc

let webApp =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromContext getApi
    |> Remoting.buildHttpHandler

let configure (services: IServiceCollection) =
    let config =
        Application.Config.getConfiguration services

    services
    |> Authentication.configure (config.GetSection "Auth")

let app =
    application {
        url "http://0.0.0.0:8085"
        use_router webApp
        memory_cache
        use_static "public"
        use_gzip
        use_cookies_authentication "VatsimLiveFeedback"
        service_config configure
    }

run app
