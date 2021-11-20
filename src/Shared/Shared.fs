namespace Shared

open System

module Model =

    [<Struct>]
    type Cid = Cid of int

    [<Struct>]
    type CallSign =
        private
        | CallSign of string
        override this.ToString() =
            match this with
            | CallSign callSign -> callSign
        static member FromString(s: string) =
            CallSign (s.ToUpper())

    type CallerId = { Cid: Cid; CallSign: CallSign }

    type MeetingInfo =
        { CallSign: CallSign
          Name: string
          Start: DateTimeOffset }

    type AuthenticationInfo =
        { Authority: string
          RedirectUri: string
          ClientId: string }

module Route =
    [<Literal>]
    let oauthCallbackPath = "oidc-signin"
    let builder typeName methodName =
        sprintf "/api/%s/%s" typeName methodName

type IMeetingsApi =
    {
        GetMeetings: Model.CallSign -> Async<Model.MeetingInfo list>
        Authorize: string -> Async<string>
        GetAuthenticationInfo: unit -> Async<Model.AuthenticationInfo>
    }
