using SRSslugcat;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;







namespace SRSslugcat;





internal class SSRoomEffects


{
    public static void Apply()
    {
        On.GravityDisruptor.Update += GravityDisruptor_Update;
        On.CoralBrain.SSMusicTrigger.Trigger += CoralBrain_SSMusicTrigger_Trigger;
        On.SSOracleBehavior.ctor += SSOracleBehavior_ctor;
        On.ZapCoil.Update += ZapCoil_Update;
        On.ZapCoilLight.Update += ZapCoilLight_Update;
        On.AbstractRoom.RealizeRoom += AbstractRoom_RealizeRoom;
        On.SSLightRod.Update += SSLightRod_Update;
        On.Room.Loaded += Room_Loaded;
        On.SSOracleBehavior.UnconciousUpdate += SSOracleBehavior_UnconciousUpdate;



        new Hook(
            typeof(Oracle).GetProperty(nameof(Oracle.Consious), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
            Oracle_Consious
            );


    }


    // 在考虑要不要直接把人偶删掉（

    private delegate bool orig_Consious(Oracle self);
    private static bool Oracle_Consious(orig_Consious orig, Oracle self)
    {
        var result = orig(self);
        if (self.room.game.session is StoryGameSession && self.ID == Oracle.OracleID.SS && (self.room.game.session as StoryGameSession).saveState.saveStateNumber == Plugin.SlugcatStatsName)
        {
            result = false;
        }
        return result;
    }




    private static void SSOracleBehavior_ctor(On.SSOracleBehavior.orig_ctor orig, SSOracleBehavior self, Oracle oracle)
    {
        orig(self, oracle);
        if (oracle.room.game.session is not StoryGameSession || oracle.room.game.GetStorySession.saveState.saveStateNumber != Plugin.SlugcatStatsName) return;
        self.movementBehavior = SSOracleBehavior.MovementBehavior.Meditate;
    }



    // 
    private static void SSOracleBehavior_UnconciousUpdate(On.SSOracleBehavior.orig_UnconciousUpdate orig, SSOracleBehavior self)
    {
        if (self.oracle.room.game != null && self.oracle.room.game.session is StoryGameSession && self.oracle.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName && self.oracle.ID == Oracle.OracleID.SS)
        {
            self.FindPlayer();
            self.unconciousTick += 1f;
            self.oracle.setGravity(0.9f);


            if (self.player == null) 
            {
                self.oracle.room.gravity = 1f;
                return; 
            }
            bool getModule = Plugin.playerModules.TryGetValue(self.player, out var module) && module.playerName == Plugin.SlugcatStatsName;
            if (getModule && module.gravityController != null && module.gravityController.enabled)
            {
                self.oracle.room.gravity = module.gravityController.gravityBonus * 0.1f;
            }
        }
        else { orig(self); }
    }








    private static void Room_Loaded(On.Room.orig_Loaded orig, Room self)
    {
        if (self.game != null && self.game.IsStorySession && self.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName && !self.game.GetStorySession.saveState.deathPersistentSaveData.altEnding
            && self.roomSettings != null && self.roomSettings.placedObjects.Count > 0)
        {

            for (int i = 0; i < self.roomSettings.placedObjects.Count; i++)
            {
                PlacedObject obj = self.roomSettings.placedObjects[i];
                if (obj.type == PlacedObject.Type.ProjectedStars)
                {
                    obj.active = false;
                }
            }
        }
        orig(self);
    }




    private static void ZapCoilLight_Update(On.ZapCoilLight.orig_Update orig, ZapCoilLight self, bool eu)
    {
        orig(self, eu);
        if (self.room != null && self.room.game.IsStorySession && self.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName)
        {
            self.lightSource.alpha = 0f;
        }
    }





    // 去除大部分房间效果
    private static void AbstractRoom_RealizeRoom(On.AbstractRoom.orig_RealizeRoom orig, AbstractRoom self, World world, RainWorldGame game)
    {
        orig(self, world, game);
        if (self.realizedRoom == null) { return; }
        if (game.IsStorySession && game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName)
        {
            RoomSettings settings = self.realizedRoom.roomSettings;

            settings.RemoveEffect(RoomSettings.RoomEffect.Type.SSMusic);
            settings.RemoveEffect(RoomSettings.RoomEffect.Type.ProjectedScanLines);
            settings.RemoveEffect(RoomSettings.RoomEffect.Type.SuperStructureProjector);
            settings.RemoveEffect(RoomSettings.RoomEffect.Type.SSSwarmers);


            settings.RemoveAmbientSound(AmbientSound.Type.Spot, "SO_IND-Turbine.ogg");
            settings.RemoveAmbientSound(AmbientSound.Type.Spot, "SO_IND-Deep50hz.ogg");
            settings.RemoveAmbientSound(AmbientSound.Type.Spot, "SO_SFX-Escape.ogg");
            settings.RemoveAmbientSound(AmbientSound.Type.Omnidirectional, "AM_IND-SuperStructure.ogg");
            settings.RemoveAmbientSound(AmbientSound.Type.Spot, "SO_SFX-DataTrans.ogg");
            settings.RemoveAmbientSound(AmbientSound.Type.Spot, "SO_SFX-DataTrans2.ogg");
            settings.RemoveAmbientSound(AmbientSound.Type.Spot, "SO_SFX-DataStream.ogg");
            settings.RemoveAmbientSound(AmbientSound.Type.Spot, "SO_SFX-Data4.ogg");
            settings.RemoveAmbientSound(AmbientSound.Type.Spot, "SO_SFX-Data5.ogg");
            
            /*foreach (AmbientSound sound in settings.ambientSounds)
            {
                Plugin.Log("room:", self.name, "ambientSound:", sound.type.ToString(), sound.sample);
            }*/
            if (self.name == "SS_AI")
            {
                if (settings.GetEffect(RoomSettings.RoomEffect.Type.DarkenLights) == null)
                {
                    settings.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.DarkenLights, 0.2f, false));
                }
                if (settings.GetEffect(RoomSettings.RoomEffect.Type.Darkness) == null)
                {
                    settings.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.Darkness, 0.1f, false));
                }
                if (settings.GetEffect(RoomSettings.RoomEffect.Type.Contrast) == null)
                {
                    settings.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.Contrast, 0.1f, false));
                }
            }
        }
    }







    private static void SSLightRod_Update(On.SSLightRod.orig_Update orig, SSLightRod self, bool eu)
    {
        orig(self, eu);
        if (self.room != null && self.room.game.IsStorySession && self.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName)
        {
            self.lights = new List<SSLightRod.LightVessel>();
            self.color = new Color(0.1f, 0.1f, 0.1f);
        }

    }















    // TODO: 修好这个东西（我不想修了，反正他卡bug的也就那么几帧，无脑catch完事
    // 关掉！必须要关掉！
    private static void ZapCoil_Update(On.ZapCoil.orig_Update orig, ZapCoil self, bool eu)
    {
        try
        {
            if (self.room.game.IsStorySession && self.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName && self.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName)
            {
                self.powered = false;
            }
            orig(self, eu);
        }
        catch
        {
            // base.Logger.LogError(ex);
        }

    }




    private static void GravityDisruptor_Update(On.GravityDisruptor.orig_Update orig, GravityDisruptor self, bool eu)
    {
        orig(self, eu);
        if (self.room != null && self.room.game.IsStorySession && self.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName)
        {
            self.power = 0f;

        }
    }






    // 啊啊啊啊啊啊啊啊啊别放音乐了
    private static void CoralBrain_SSMusicTrigger_Trigger(On.CoralBrain.SSMusicTrigger.orig_Trigger orig, CoralBrain.SSMusicTrigger self)
    {
        if (self.room.game.IsStorySession && self.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName)
        {
            return;
        }
        orig(self);
    }




}
