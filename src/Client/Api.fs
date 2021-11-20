module Client.Api

open Fable.Remoting.Client
open Shared

let meetings =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<IMeetingsApi>
