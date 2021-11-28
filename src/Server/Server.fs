module Server.App

open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Configuration
open Saturn
open Giraffe

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

let apiRouter =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromContext getApi
    |> Remoting.buildHttpHandler

let spaRouter : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        let fileProvider = ctx.GetService<IWebHostEnvironment>().WebRootFileProvider
        let index = fileProvider.GetFileInfo("index.html")
        let path = index.PhysicalPath
        htmlFile path next ctx

let webApp =
    router {
        forward "" apiRouter
        get "/" spaRouter
        get "/privacy" spaRouter
        get "/oidc-signin" spaRouter
    }

let configureServices (services: IServiceCollection) =
    let config =
        Application.Config.getConfiguration services

    services
    |> Authentication.configure (config.GetSection "OAuth")

open System.IO

let webRootPath =
    let currentDir = Directory.GetCurrentDirectory()
    let dist = Path.GetFullPath("../Client/dist", currentDir)
    let pub = Path.GetFullPath("wwwroot", currentDir)
    if Directory.Exists(dist) then dist else pub

let configureHost (host: IHostBuilder) =
    host
        .ConfigureAppConfiguration(fun b ->
        b.AddEnvironmentVariables("VLF_") |> ignore)

let configureWebHost (host: IWebHostBuilder) =
    host
        .UseWebRoot(webRootPath)

let app =
    application {
        url "http://0.0.0.0:8085"
        use_router webApp
        memory_cache
        use_static webRootPath
        use_gzip
        use_cookies_authentication "VatsimLiveFeedback"
        service_config configureServices
        host_config configureHost
        webhost_config configureWebHost
    }

run app
