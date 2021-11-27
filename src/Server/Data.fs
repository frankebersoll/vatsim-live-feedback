module Server.Data

open System
open FSharp.Data
open Shared.Model

type VatsimDataUris =
    { FlightDataUri: string
      TransceiversDataUri: string }

let liveDataUris =
    { FlightDataUri = "https://data.vatsim.net/v3/vatsim-data.json"
      TransceiversDataUri = "https://data.vatsim.net/v3/transceivers-data.json" }

type FlightData = JsonProvider<"ExampleData/VatsimData.json">
type TransceiversData = JsonProvider<"ExampleData/TransceiversData.json">

type UserInfo = { CallerId: CallerId; Name: string }
let userInfo callerId name = { CallerId = callerId; Name = name }

type MeetingKey =
    { Pilot: CallerId
      Controller: CallerId }

let callerId cid callSign =
    { Cid = Cid cid
      CallSign = CallSign.FromString callSign }

type FlightData.Controller with
    member this.CallerId = callerId this.Cid this.Callsign
    member this.CallSign = CallSign.FromString this.Callsign

type FlightData.Pilot with
    member this.CallerId = callerId this.Cid this.Callsign

type Frequency =
    | Vhf of decimal
    | Unicom of int
    static member decode(vhf: int) =
        let freq = vhf / 1000

        match freq with
        | 122800 -> Unicom(vhf % 1000)
        | _ -> Vhf(decimal (freq) / 1000m)

type Transceiver =
    { Coordinate: float * float
      Frequency: Frequency }

type VatsimData =
    { FlightData: FlightData.Root
      Transceivers: Map<CallSign, Transceiver list>
      WorldFreq: (float * float * int) list }

let getTransceivers callSign state =
    state.Transceivers
    |> Map.tryFind callSign
    |> Option.defaultValue List.empty

let loadCurrentStateAsync uris =
    async {
        let! flightDataChild =
            FlightData.AsyncLoad(uris.FlightDataUri)
            |> Async.StartChild

        let! transceiversDataChild =
            TransceiversData.AsyncLoad(uris.TransceiversDataUri)
            |> Async.StartChild

        let! flightData = flightDataChild
        let! transceiversData = transceiversDataChild

        let transceiversByCallsign =
            transceiversData
            |> Seq.map
                (fun coms ->
                    CallSign.FromString coms.Callsign,
                    (coms.Transceivers
                     |> Seq.map
                         (fun com ->
                             { Transceiver.Coordinate = (com.LatDeg, com.LonDeg)
                               Frequency = Frequency.decode com.Frequency })
                     |> List.ofSeq))
            |> Map.ofSeq

        let worldFreq =
            transceiversData
            |> Seq.choose (fun coms -> coms.Transceivers |> Array.tryHead)
            |> Seq.map (fun trans -> float (trans.LatDeg), float (trans.LonDeg), trans.Frequency)
            |> List.ofSeq

        return
            { VatsimData.FlightData = flightData
              Transceivers = transceiversByCallsign
              WorldFreq = worldFreq }
    }

let getUsers (state: VatsimData) =
    let pilots =
        state.FlightData.Pilots
        |> Seq.map (fun p -> userInfo (callerId p.Cid p.Callsign) p.Name)

    let controllers =
        state.FlightData.Controllers
        |> Seq.map (fun c -> userInfo (callerId c.Cid c.Callsign) c.Name)

    pilots |> Seq.append controllers |> List.ofSeq

let getMeetings (state: VatsimData) =

    let calculateDistance (p1Latitude, p1Longitude) (p2Latitude, p2Longitude) =
        let r = 6371.0 // km

        let dLat =
            (p2Latitude - p1Latitude) * Math.PI / 180.0

        let dLon =
            (p2Longitude - p1Longitude) * Math.PI / 180.0

        let lat1 = p1Latitude * Math.PI / 180.0
        let lat2 = p2Latitude * Math.PI / 180.0

        let a =
            Math.Sin(dLat / 2.0) * Math.Sin(dLat / 2.0)
            + Math.Sin(dLon / 2.0)
              * Math.Sin(dLon / 2.0)
              * Math.Cos(lat1)
              * Math.Cos(lat2)

        let c =
            2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a))

        r * c

    let controllers =

        let isValidFrequency = function
            | Vhf 199.998m -> false
            | Unicom _ -> false
            | _ -> true

        query {
            for controller in state.FlightData.Controllers do
                for transceiver in state |> getTransceivers controller.CallSign do
                    where (isValidFrequency transceiver.Frequency)
                    groupBy transceiver.Frequency into g
                    select (g.Key, g |> Seq.toList)
        }
        |> Map.ofSeq

    let meetings =
        query {
            for pilot in state.FlightData.Pilots do
                let callSign = CallSign.FromString pilot.Callsign

                let com1 =
                    state |> getTransceivers callSign |> Seq.tryHead

                where com1.IsSome
                let com1 = com1.Value

                let controllers = controllers.TryFind com1.Frequency
                where controllers.IsSome
                let controllers = controllers.Value

                let pilotDistance = calculateDistance com1.Coordinate

                let controller =
                    if controllers.Length = 1 then
                        fst controllers.Head
                    else
                        let sorted =
                            controllers
                            |> Seq.sortBy (fun (_, t: Transceiver) -> pilotDistance t.Coordinate)
                            |> Seq.toList

                        sorted.Head |> fst

                select
                    { Pilot = pilot.CallerId
                      Controller = controller.CallerId }
        }
        |> Seq.toList

    meetings
