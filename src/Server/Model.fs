namespace Server.Model

open System
open System.Collections.Generic
open Server.Data

module History =

    type MeetingItem =
        { Key: MeetingKey
          TargetId: CallerId
          Start: DateTimeOffset }

    type private MeetingHistoryItem =
        { Key: MeetingKey
          Start: DateTimeOffset
          mutable Timestamp: DateTimeOffset }
        member this.Ping timestamp = this.Timestamp <- timestamp

    let private createHistoryItem key now =
        { Key = key
          Start = now
          Timestamp = now }

    type private MeetingHistoryDict = Dictionary<MeetingKey, MeetingHistoryItem>

    type MeetingHistory =
        private
            { Meetings: MeetingHistoryDict
              Pending: MeetingHistoryDict
              ByCid: Dictionary<Cid, MeetingItem list> }

    let create () =
        { Meetings = Dictionary()
          Pending = Dictionary()
          ByCid = Dictionary() }

    type MeetingHistoryUpdate =
        { Added: MeetingKey list
          Removed: MeetingKey list }

    let get (cid: Cid) (history: MeetingHistory) =
        match history.ByCid.TryGetValue cid with
        | true, meetings -> meetings
        | _ -> []

    let getCids (history: MeetingHistory) = history.ByCid.Keys |> Seq.toList

    let update (meetings: MeetingKey seq) (now: DateTimeOffset) (maxAge: TimeSpan) (history: MeetingHistory) =

        let historyMeetings = history.Meetings
        let pending = history.Pending
        let byCid = history.ByCid

        let addForCid (cid: Cid) (target: CallerId) (item: MeetingHistoryItem) =
            let item =
                { MeetingItem.Key = item.Key
                  TargetId = target
                  Start = item.Start }

            match byCid.TryGetValue(cid) with
            | true, meetings -> byCid.[cid] <- item :: meetings
            | false, _ -> byCid.Add(cid, [ item ])

        let removeForCid cid key =
            match byCid.TryGetValue(cid) with
            | true, meetings ->
                let result =
                    meetings |> List.filter (fun x -> x.Key <> key)

                if meetings.IsEmpty then
                    byCid.Remove(cid) |> ignore
                else
                    byCid.[cid] <- result

            | false, _ -> ()

        let add item =
            pending.Remove(item.Key) |> ignore
            historyMeetings.Add(item.Key, item)
            addForCid item.Key.Controller.Cid item.Key.Pilot item
            addForCid item.Key.Pilot.Cid item.Key.Controller item

        let remove item =
            historyMeetings.Remove(item.Key) |> ignore
            removeForCid item.Key.Controller.Cid item.Key
            removeForCid item.Key.Pilot.Cid item.Key

        let additions =
            [ for key in meetings do
                  match historyMeetings.TryGetValue(key) with
                  | true, item -> item.Ping now
                  | false, _ ->
                      match pending.TryGetValue(key) with
                      | true, item ->
                          add item
                          yield key
                      | false, _ ->
                          let item = createHistoryItem key now
                          pending.Add(key, item) ]

        let minTimestamp = now - maxAge
        let isOutdated item = item.Timestamp < minTimestamp

        let outdatedItems =
            historyMeetings.Values |> Seq.filter isOutdated

        let removed =
            [ for item in outdatedItems do
                  remove item
                  yield item.Key ]

        { Added = additions; Removed = removed }

type Store =
    { GetMeetings: Cid -> History.MeetingItem list
      GetCids: unit -> Cid list }

module Store =

    type StoreMsg =
        private
        | UpdateHistory of MeetingKey seq
        | GetMeetingsRequest of AsyncReplyChannel<History.MeetingItem list> * Cid
        | GetCids of AsyncReplyChannel<Cid list>

    let create () =
        let history = History.create ()
        let maxAge = TimeSpan.FromMinutes(30.0)
        let updateInterval = TimeSpan.FromSeconds(15.0)

        let rec runloop (mailbox: MailboxProcessor<StoreMsg>) =
            async {
                let! msg = mailbox.Receive()

                match msg with
                | GetMeetingsRequest (channel, cid) -> history |> History.get cid |> channel.Reply
                | GetCids channel -> history |> History.getCids |> channel.Reply
                | UpdateHistory meetings ->
                    let now = DateTimeOffset.Now

                    let updates =
                        history |> History.update meetings now maxAge

                    updates |> ignore

                do! runloop mailbox
            }

        let mailbox = MailboxProcessor.Start runloop

        async {
            while true do
                let! state = loadCurrentStateAsync ()
                let meetings = getMeetings state
                mailbox.Post(UpdateHistory meetings)
                do! Async.Sleep updateInterval
        }
        |> Async.Start

        let getMeetings cid =
            mailbox.PostAndReply(fun replyChannel -> GetMeetingsRequest(replyChannel, cid))

        let getCids() = mailbox.PostAndReply(GetCids)

        {
            GetMeetings = getMeetings
            GetCids = getCids
        }
