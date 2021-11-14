module Server.App

open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open Saturn

open Shared
open Shared.Model

let store = Model.Store.create ()

let meetingsApi =

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

    { GetMeetings = getMeetings }

let webApp =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue meetingsApi
    |> Remoting.buildHttpHandler

let app =
    application {
        url "http://0.0.0.0:8085"
        use_router webApp
        memory_cache
        use_static "public"
        use_gzip
    }

run app
