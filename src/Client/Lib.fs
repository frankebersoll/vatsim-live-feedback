module Client.Lib

open Fable.Core

module Parcel =
    [<Emit("new URL('../public/' + $0, import.meta.url)")>]
    let inline publicAsset _path = jsNative

    [<Emit("process.env[$0]")>]
    let envVariable (_env: string) = jsNative

[<Global("cookieconsent")>]
module CookieConsent =
    let run (_options: obj) = jsNative
