module Client.Interaction

open Feliz

let onEnter f = prop.onKeyDown (fun e -> if e.key = "Enter" then f())