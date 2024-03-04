﻿using SRSslugcat;
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



/// <summary>
/// 牛逼，这段代码实际上没啥用，我还是得手动删掉大部分roomeffects，而且我要手动把设置文件名上的大写字母替换为小写字母他才会生效
/// 很难不流汗
/// </summary>

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
        On.Oracle.SetUpMarbles += Oracle_SetUpMarbles;
        // On.Oracle.ctor += Oracle_ctor;



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




    private static void Oracle_ctor(On.Oracle.orig_ctor orig, Oracle self, AbstractPhysicalObject abstractPhysicalObject, Room room)
    {
        if (room.game.IsStorySession && room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName && room.abstractRoom.name == "SS_AI")
        {
            return;
        }
        orig(self, abstractPhysicalObject, room);
    }



    private static void Oracle_SetUpMarbles(On.Oracle.orig_SetUpMarbles orig, Oracle self)
    {
        if (self.room.game.IsStorySession && self.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName)
        {
            return;
        }
        orig(self);
    }





    private static void SSOracleBehavior_ctor(On.SSOracleBehavior.orig_ctor orig, SSOracleBehavior self, Oracle oracle)
    {
        orig(self, oracle);
        if (oracle.room.game.session is not StoryGameSession || oracle.room.game.GetStorySession.saveState.saveStateNumber != Plugin.SlugcatStatsName) return;
        self.movementBehavior = SSOracleBehavior.MovementBehavior.Meditate;
        // 写在这不会有问题吧（汗
        // 貌似会有问题，还是写个roomspecificscript保险
        /*var abstr = new OxygenMaskModules.OxygenMaskAbstract(oracle.room.game.world, new WorldCoordinate(oracle.room.abstractRoom.index, -1, -1, 0), oracle.room.game.GetNewID());
        self.oracle.room.abstractRoom.AddEntity(abstr);
        self.oracle.room.AddObject(new OxygenMaskModules.OxygenMask(abstr));
        Plugin.Log("OXYGEN MASK test");
        foreach (var obj in oracle.room.physicalObjects)
        {
            foreach (var obj2 in obj)
            {
                Plugin.Log("physicalObj:", obj2.GetType().Name);
            }
        }*/
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
        if (self.realizedRoom == null || !game.IsStorySession || game.GetStorySession.saveStateNumber != Plugin.SlugcatStatsName) { return; }


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
        settings.RemoveAmbientSound(AmbientSound.Type.Spot, "SO_SFX-Data.ogg");
        settings.RemoveAmbientSound(AmbientSound.Type.Spot, "SO_SFX-Data2.ogg");
        settings.RemoveAmbientSound(AmbientSound.Type.Spot, "SO_SFX-Data3.ogg");
        settings.RemoveAmbientSound(AmbientSound.Type.Spot, "SO_SFX-Data4.ogg");
        settings.RemoveAmbientSound(AmbientSound.Type.Spot, "SO_SFX-Data5.ogg");

        /*foreach (AmbientSound sound in settings.ambientSounds)
        {
            Plugin.Log("room:", self.name, "ambientSound:", sound.type.ToString(), sound.sample);
        }*/
        if (self.name.StartsWith("SS"))
        {
            if (settings.GetEffect(RoomSettings.RoomEffect.Type.ZeroG) != null)
            {
                settings.GetEffect(RoomSettings.RoomEffect.Type.ZeroG).amount = 0.9f;
            }
            if (settings.GetEffect(RoomSettings.RoomEffect.Type.BrokenZeroG) != null)
            {
                settings.GetEffect(RoomSettings.RoomEffect.Type.BrokenZeroG).amount = 0f;
            }
        }

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
