module Server.Authentication

open System
open System.Net
open System.Net.Http
open System.Net.Http.Headers
open System.Net.Http.Json
open System.Security.Claims
open System.Threading.Tasks
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Options
open FsToolkit.ErrorHandling

open Shared.Model

type AuthenticationOptions() =
    member val Authority: string = "https://auth-demo.vatsim.org" with get, set
    member val ClientId: string = "" with get, set
    member val ClientSecret: string = "" with get, set

type AuthenticationState =
    private
        { HttpContext: HttpContext
          BackChannel: HttpClient
          BaseUri: Uri
          Options: AuthenticationOptions
          User: ClaimsPrincipal
          Properties: AuthenticationProperties }

module VatsimConnect =
    type TokenResponse =
        { token_type: string
          scopes: string list
          expires_in: int
          access_token: string
          refresh_token: string option }

    type PersonalData =
        { name_first: string
          name_last: string
          name_full: string }

    type UserInfoData = { cid: int; personal: PersonalData }
    type UserInfoResponse = { data: UserInfoData }
    let httpClientName = "Authentication"
    let accessTokenName = "AccessToken"
    let refreshTokenName = "RefreshToken"

    let urlEncode keyValues =
        new FormUrlEncodedContent(dict keyValues)

    let getRedirectUri state = Uri(state.BaseUri, Shared.Route.oauthCallbackPath)

    let tryGetContent<'T> (response: Task<HttpResponseMessage>) =
        async {
            let! response = response |> Async.AwaitTask

            if response.StatusCode = HttpStatusCode.OK then
                let! json =
                    response.Content.ReadFromJsonAsync<'T>()
                    |> Async.AwaitTask

                return Ok json
            else
                return Error response.ReasonPhrase
        }

    let signIn state (ctx: HttpContext) =
        if state.User.Identity.IsAuthenticated then
            ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, state.User, state.Properties)
        else
            failwith "User is not authenticated"

open VatsimConnect

let getState (ctx: HttpContext) =
    let clientFactory = ctx.GetService<IHttpClientFactory>()

    let options =
        ctx
            .GetService<IOptions<AuthenticationOptions>>()
            .Value

    let client =
        clientFactory.CreateClient(httpClientName)

    let svc = ctx.GetService<IAuthenticationService>()

    let auth =
        svc
            .AuthenticateAsync(
                ctx,
                CookieAuthenticationDefaults.AuthenticationScheme
            )
            .Result

    let baseUri = Uri($"{ctx.Request.Scheme}://{ctx.Request.Host.ToUriComponent()}")

    { HttpContext = ctx
      BackChannel = client
      BaseUri = baseUri
      Options = options
      User = auth.Principal
      Properties =
          auth.Properties
          |> nullCoerceBy AuthenticationProperties }

let configure (config: IConfigurationSection) (services: IServiceCollection) =
    let configureClient (serviceProvider: IServiceProvider) (client: HttpClient) =
        let options =
            serviceProvider
                .GetRequiredService<IOptions<AuthenticationOptions>>()
                .Value

        client.BaseAddress <- Uri(options.Authority)

    services
        .Configure<AuthenticationOptions>(config)
        .AddHttpClient(httpClientName)
        .ConfigureHttpClient(configureClient)
    |> ignore

    services

let authorize code state =
    let backChannel = state.BackChannel
    let options = state.Options
    let redirectUri = getRedirectUri state

    asyncResult {
        use data =
            urlEncode [
                ("grant_type", "authorization_code")
                ("client_id", options.ClientId)
                ("client_secret", options.ClientSecret)
                ("redirect_uri", redirectUri.ToString())
                ("code", code)
            ]

        let! token =
            backChannel.PostAsync("/oauth/token", data)
            |> tryGetContent<TokenResponse>
            |> AsyncResult.catch (fun e ->
                e.Message)

        let expires =
            DateTimeOffset.Now
            + TimeSpan.FromSeconds(float token.expires_in)

        use msg =
            new HttpRequestMessage(HttpMethod.Get, "/api/user")

        msg.Headers.Authorization <- AuthenticationHeaderValue("Bearer", token.access_token)

        let! userInfo =
            backChannel.SendAsync(msg)
            |> tryGetContent<UserInfoResponse>

        let identity =
            ClaimsIdentity(
                [ Claim(ClaimTypes.Sid, userInfo.data.cid.ToString())
                  Claim(ClaimTypes.Name, userInfo.data.personal.name_full)
                  Claim(ClaimTypes.GivenName, userInfo.data.personal.name_first)
                  Claim(ClaimTypes.Surname, userInfo.data.personal.name_last) ],
                CookieAuthenticationDefaults.AuthenticationScheme
            )

        let principal = ClaimsPrincipal(identity)
        let properties =
            AuthenticationProperties(
                ExpiresUtc = expires.ToUniversalTime(),
                IsPersistent = true,
                AllowRefresh = true)

        properties.StoreTokens(
            [ AuthenticationToken(Name = accessTokenName, Value = token.access_token)
              AuthenticationToken(Name = refreshTokenName, Value = (token.refresh_token |> Option.defaultValue "")) ]
        )

        let newState =
            { state with
                  User = principal
                  Properties = properties }

        do! signIn newState state.HttpContext

        return newState
    }

let getAuthConfig (state: AuthenticationState) =
    let options = state.Options
    { AuthenticationInfo.Authority = options.Authority
      RedirectUri = (getRedirectUri state).ToString()
      ClientId = options.ClientId }

let getUser (state: AuthenticationState) = state.User
