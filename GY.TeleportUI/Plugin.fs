namespace GY.Teleport

open System.Collections
open System.Collections.Generic
open GY.Teleport.Config
open Rocket.API.Collections
open Rocket.Core.Plugins
open Rocket.Unturned
open Rocket.Unturned.Chat
open Rocket.Unturned.Events
open Rocket.Unturned.Player
open Steamworks
open UnityEngine

type Plugin() =
    inherit RocketPlugin<Config>()
    static member val TeleportData = Dictionary<CSteamID, Dictionary<CSteamID, bool>>()
    static member val Cfg : Config = Unchecked.defaultof<Config> with get, set
    static member val Instance : Plugin = Unchecked.defaultof<Plugin> with get, set
    static member ExecuteTeleport(caller : UnturnedPlayer, target : UnturnedPlayer) =
        async {
            Plugin.TeleportData.[target.CSteamID].[caller.CSteamID] <- true
            do! Async.Sleep(Plugin.Cfg.TpaDelay * 1000)
            if Plugin.TeleportData.ContainsKey(target.CSteamID) then
                if Plugin.TeleportData.[target.CSteamID].ContainsKey(caller.CSteamID) then
                    Plugin.TeleportData.[target.CSteamID].Remove(caller.CSteamID) |> ignore
                    caller.Teleport(target)
        }
        
    static member SendTeleportRequest(caller : UnturnedPlayer, target : UnturnedPlayer) =
        Plugin.TeleportData.[target.CSteamID].Add(caller.CSteamID, false)
        async {
            do! Async.Sleep(Plugin.Cfg.TpaAccept * 1000)
            if Plugin.TeleportData.ContainsKey(target.CSteamID) &&
               Plugin.TeleportData.[target.CSteamID].ContainsKey(caller.CSteamID) then
                 if not Plugin.TeleportData.[target.CSteamID].[caller.CSteamID] then
                    Plugin.TeleportData.[target.CSteamID].Remove(caller.CSteamID) |> ignore
                    UnturnedChat.Say(caller, Plugin.Instance.Translate("tpa_failed_time", target.DisplayName), Color.yellow)
        }
  
 
    override this.DefaultTranslations =
        let dictionary = TranslationList()
        dict["invalid_usage", "Команда введена наверно, используйте {0}!";
        "tpa_empty", "У вас нет входящих запросов на телепортацию.";
        "tpa_failed", "Что-то пошло не так, никто не телепортировался :(";
        "tpa_failed_caller", "Что-то пошло не так, вы не смогли телепортироваться :(";
        "tpa_failed_dmg", "Игрок {0} атакован, телепортация отменена.";
        "tpa_failed_dmg_player", "Вы получили урон, все ваши телепортации отменены.";
        "tpa_deny", "Вы отклонили запрос на телепортацию от игрока {0}.";
        "tpa_deny_caller", "Игрок {0} отклонил ваш запрос на телепортацию.";
        "tpa_income_empty", "У вас нет исходящих запросов на телепортацию.";
        "tpa_cancel", "Игрок {0} отменил запрос телепортации.";
        "tpa_cancel_caller", "Вы отменили запрос телепортации к {0}.";
        "player_null", "Игрок не найден!";
        "tpa_send_already", "Вы уже отправили запрос на телепортацию этому игроку.";
        "tpa_failed_time", "Игрок {0} проигнорировл ваш запрос на телепортацию.";
        "tpa_failed_movement", "Игрок {0} двигался, телепортация отменена.";
        "tpa_failed_movement_caller", "Вы двигались, все телепортации отменены.";
        "tpa_failed_disconnect", "Игрок {0} вышел с сервера, телепортация отменена.";
        "tpa_send", "Вы отправили запрос на телепортацию игроку {0}.";
        "tpa_send_caller", "Игрок {0} отправил запрос на телепортацию.";
        "tpa_accept", "Вы приняли запрос на телепортацию от игрока {0}.";
        "tpa_accept_caller", "Игрок {0} принял ваш запрос на телепортацию."]
        |> Seq.iter(fun kv -> dictionary.Add(kv.Key, kv.Value))
        dictionary
       
    override this.Load() =
        Plugin.Instance <- this
        Plugin.Cfg <- this.Configuration.Instance
        
        if Plugin.Cfg.CancelOnDamage then
          UnturnedEvents.add_OnPlayerDamaged(fun player cause limb killer direction damage times canDamage -> this.OnPlayerDamaged player)
        if Plugin.Cfg.CancelOnMove then
          UnturnedPlayerEvents.add_OnPlayerUpdatePosition(fun player vector -> this.OnPlayerUpdatePos player)
          
        U.Events.add_OnPlayerConnected(fun x -> this.OnPlayerConnected x)
        U.Events.add_OnPlayerDisconnected(fun x -> this.OnPlayerDisconnected x)
    
    override this.Unload() =
        Plugin.Instance <- Unchecked.defaultof<Plugin>
        Plugin.Cfg <- Unchecked.defaultof<Config>
        
        if Plugin.Cfg.CancelOnDamage then
          UnturnedEvents.remove_OnPlayerDamaged(fun player cause limb killer direction damage times canDamage -> this.OnPlayerDamaged player)
        if Plugin.Cfg.CancelOnMove then
          UnturnedPlayerEvents.remove_OnPlayerUpdatePosition(fun player vector -> this.OnPlayerUpdatePos player)
          
        U.Events.remove_OnPlayerConnected(fun x -> this.OnPlayerConnected x)
        U.Events.remove_OnPlayerDisconnected(fun x -> this.OnPlayerDisconnected x)
    
    member this.OnPlayerConnected(player : UnturnedPlayer) =
        Plugin.TeleportData.Add(player.CSteamID, Dictionary<CSteamID, bool>())
    member this.OnPlayerDisconnected(player : UnturnedPlayer) =
        Plugin.TeleportData.[player.CSteamID] |> Seq.iter(fun x -> UnturnedChat.Say(x.Key, this.Translate("tpa_failed_disconnect", player.DisplayName), Color.yellow))
        Plugin.TeleportData.Remove(player.CSteamID) |> ignore
        
        Plugin.TeleportData
        |> Seq.where(fun x -> x.Value.ContainsKey(player.CSteamID))
        |> Seq.map(fun x -> x.Key)
        |> Seq.iter(fun x ->
            Plugin.TeleportData.[x].Remove(player.CSteamID) |> ignore
            UnturnedChat.Say(x, this.Translate("tpa_failed_disconnect", player.DisplayName), Color.yellow))
        
    member this.OnPlayerDamaged(player : UnturnedPlayer) =
         Plugin.TeleportData
        |> Seq.where(fun x -> x.Value.ContainsKey(player.CSteamID))
        |> Seq.map(fun x -> x.Key)
        |> Seq.iter(fun x ->
            Plugin.TeleportData.[x].Remove(player.CSteamID) |> ignore
            UnturnedChat.Say(x, this.Translate("tpa_failed_dmg", player.DisplayName), Color.yellow)
            UnturnedChat.Say(player, this.Translate("tpa_failed_dmg_player", Color.yellow)))
            
        
    member this.OnPlayerUpdatePos(player : UnturnedPlayer) =
         Plugin.TeleportData
        |> Seq.where(fun x -> x.Value.ContainsKey(player.CSteamID))
        |> Seq.map(fun x -> x.Key)
        |> Seq.iter(fun x ->
            Plugin.TeleportData.[x].Remove(player.CSteamID) |> ignore
            UnturnedChat.Say(x, this.Translate("tpa_failed_movement", player.DisplayName))
            UnturnedChat.Say(player, this.Translate("tpa_failed_movement_caller", Color.yellow)))
    
    
