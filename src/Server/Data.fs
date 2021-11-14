module Server.Data

open FSharp.Data
open Shared.Model

let vatsimDataUri =
    "https://data.vatsim.net/v3/vatsim-data.json"

let transceiversDataUri =
    "https://data.vatsim.net/v3/transceivers-data.json"

type VatsimData = JsonProvider<"ExampleData/VatsimData.json">
type TransceiversData = JsonProvider<"ExampleData/TransceiversData.json">

type UserInfo = { CallerId: CallerId; Name: string }
let userInfo callerId name = { CallerId = callerId; Name = name }

type MeetingKey =
    { Pilot: CallerId
      Controller: CallerId }

[<Measure>]
type encodedVhf

let encodedVhf i = i * 1<encodedVhf>

let callerId cid callSign =
    { Cid = Cid cid
      CallSign = CallSign.FromString callSign }

type VatsimData.Controller with
    member this.CallerId = callerId this.Cid this.Callsign

type VatsimData.Pilot with
    member this.CallerId = callerId this.Cid this.Callsign

type VatsimState =
    private
        { VatsimData: VatsimData.Root
          Frequencies: Map<CallSign, int<encodedVhf> []> }

let getFrequencies callSign state =
    state.Frequencies
    |> Map.tryFind callSign
    |> Option.defaultValue Array.empty

let loadCurrentStateAsync () =
    async {
        let! vatsimDataChild =
            VatsimData.AsyncLoad(vatsimDataUri)
            |> Async.StartChild

        let! transceiversDataChild =
            TransceiversData.AsyncLoad(transceiversDataUri)
            |> Async.StartChild

        let! vatsimData = vatsimDataChild
        let! transceiversData = transceiversDataChild

        let frequenciesByCallSign =
            transceiversData
            |> Seq.map
                (fun coms ->
                    CallSign.FromString coms.Callsign,
                    (coms.Transceivers
                     |> Array.map (fun com -> encodedVhf com.Frequency)))
            |> Map.ofSeq

        return
            { VatsimState.VatsimData = vatsimData
              Frequencies = frequenciesByCallSign }
    }

let getUsers (state: VatsimState) =
    let pilots =
        state.VatsimData.Pilots
        |> Seq.map (fun p -> userInfo (callerId p.Cid p.Callsign) p.Name)

    let controllers =
        state.VatsimData.Controllers
        |> Seq.map (fun c -> userInfo (callerId c.Cid c.Callsign) c.Name)

    pilots |> Seq.append controllers |> List.ofSeq

let getMeetings (state: VatsimState) =

    let controllers =
        query {
            for controller in state.VatsimData.Controllers do
                let callSign = CallSign.FromString controller.Callsign
                let frequencies = state |> getFrequencies callSign

                for frequency in frequencies do
                    select (frequency, controller)
        }
        |> Map.ofSeq

    let meetings =
        query {
            for pilot in state.VatsimData.Pilots do
                let callSign = CallSign.FromString pilot.Callsign

                let com1Frequency =
                    state |> getFrequencies callSign |> Array.tryHead

                let controller =
                    com1Frequency |> Option.bind controllers.TryFind

                where controller.IsSome

                select
                    { Pilot = pilot.CallerId
                      Controller = controller.Value.CallerId }
        }
        |> Seq.toList

    meetings
