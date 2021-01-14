namespace GY.Teleport

open System
open GY.Teleport
open Rocket.API
open Rocket.Unturned.Chat
open Rocket.Unturned.Player
open UnityEngine

type CommandTpa() =
    interface IRocketCommand with
     member val AllowedCaller = AllowedCaller.Player
     member val Name = "Tpa"
     member val Help = String.Empty
     member val Syntax = "/Tpa [nick/accept/deny/cancel]"
     member val Aliases = ResizeArray[]
     member val Permissions = ResizeArray["gy.tpa"]
     
     member this.Execute(player, args) =
         if not (args.Length = 1) then
             UnturnedChat.Say(player, Plugin.Instance.Translate("invalid_usage", (this :> IRocketCommand).Syntax), Color.red)
             ()
         else
         
         let uPlayer = downcast player : UnturnedPlayer
         
         match args.[0].ToLower() with
         | "accept" | "a" ->
             let incomeTpa = Plugin.TeleportData.[uPlayer.CSteamID] |> Seq.tryFind(fun x -> not x.Value)
             if incomeTpa.IsNone then
                 UnturnedChat.Say(player, Plugin.Instance.Translate("tpa_empty"), Color.red)
             else
             let caller = UnturnedPlayer.FromCSteamID incomeTpa.Value.Key
             UnturnedChat.Say(uPlayer, Plugin.Instance.Translate("tpa_accept", caller.DisplayName), Color.yellow)
             UnturnedChat.Say(caller, Plugin.Instance.Translate("tpa_accept_caller", uPlayer.DisplayName), Color.yellow)
             
             Plugin.ExecuteTeleport(caller, uPlayer) |> Async.StartImmediate
         | "deny" | "d" ->
             let incomeTpa = Plugin.TeleportData.[uPlayer.CSteamID] |> Seq.tryFind(fun x -> not x.Value)
             if incomeTpa.IsNone then
                 UnturnedChat.Say(player, Plugin.Instance.Translate("tpa_empty"), Color.red)
             else
                 let caller = UnturnedPlayer.FromCSteamID incomeTpa.Value.Key
                 UnturnedChat.Say(uPlayer, Plugin.Instance.Translate("tpa_deny", caller.DisplayName), Color.yellow)
                 UnturnedChat.Say(caller, Plugin.Instance.Translate("tpa_deny_caller", uPlayer.DisplayName), Color.yellow)
                 
                 Plugin.TeleportData.[uPlayer.CSteamID].Remove(incomeTpa.Value.Key) |> ignore
         | "cancel" | "c" ->
             let firstTpa = Plugin.TeleportData |> Seq.tryFind(fun x -> x.Value.ContainsKey uPlayer.CSteamID)
             if firstTpa.IsNone then
                 UnturnedChat.Say(player, Plugin.Instance.Translate("tpa_income_empty"), Color.red)
             else
                 let target = UnturnedPlayer.FromCSteamID firstTpa.Value.Key
                 UnturnedChat.Say(target, Plugin.Instance.Translate("tpa_cancel", uPlayer.DisplayName), Color.yellow)
                 UnturnedChat.Say(uPlayer, Plugin.Instance.Translate("tpa_cancel_caller", target.DisplayName), Color.yellow)
                 
                 Plugin.TeleportData.[target.CSteamID].Remove(uPlayer.CSteamID) |> ignore
         | _ ->
             let target = UnturnedPlayer.FromName args.[0]
             if isNull target then
                 UnturnedChat.Say(player, Plugin.Instance.Translate("player_null", (this :> IRocketCommand).Syntax), Color.red)
                 ()
             else
                 
             if Plugin.TeleportData.[target.CSteamID].ContainsKey uPlayer.CSteamID then
                 UnturnedChat.Say(player, Plugin.Instance.Translate("tpa_send_already"), Color.red)
                 ()
             else
                 
             UnturnedChat.Say(uPlayer, Plugin.Instance.Translate("tpa_send", target.DisplayName), Color.yellow)
             UnturnedChat.Say(target, Plugin.Instance.Translate("tpa_send_caller", uPlayer.DisplayName), Color.yellow)
             
             Plugin.SendTeleportRequest(uPlayer, target) |> Async.StartImmediate

             
             