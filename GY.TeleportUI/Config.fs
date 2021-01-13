module GY.Teleport.Config

open Rocket.API

type Config() =
    [<DefaultValue>] val mutable CancelOnMove : bool
    [<DefaultValue>] val mutable CancelOnDamage : bool
    [<DefaultValue>] val mutable TpaDelay : int
    [<DefaultValue>] val mutable TpaAccept : int
    
    interface IRocketPluginConfiguration with
     override this.LoadDefaults() =
         this.CancelOnDamage <- false
         this.CancelOnMove <- false
         this.TpaDelay <- 3
         this.TpaAccept <- 6
         