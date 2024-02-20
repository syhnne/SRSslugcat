using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using UnityEngine;
using SlugBase.Features;
using static SlugBase.Features.FeatureTypes;
using System.Drawing.Text;
using RWCustom;
using Noise;
using MoreSlugcats;
using BepInEx.Logging;
using Smoke;
using Random = UnityEngine.Random;
// using ImprovedInput;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Menu.Remix.MixedUI;
using System.ComponentModel;
using SlugBase.DataTypes;
using MonoMod.RuntimeDetour;
using System.Reflection;
using SlugBase.SaveData;
using SlugBase;
using static SRSslugcat.PlayerHooks;














namespace SRSslugcat;


internal class PlayerHooks
{



    internal static void Apply()
    {
        On.HUD.HUD.InitSinglePlayerHud += HUD_InitSinglePlayerHud;
        IL.Player.GrabUpdate += Player_GrabUpdate;
        On.Player.Destroy += Player_Destroy;
        On.Player.Die += Player_Die;
        On.Player.CanBeSwallowed += Player_CanBeSwallowed;
        On.Player.ObjectCountsAsFood += Player_ObjectCountsAsFood;
        On.Player.NewRoom += Player_NewRoom;
        On.Player.Jump += Player_Jump;
        On.Player.MovementUpdate += Player_MovementUpdate;

        new Hook(
            typeof(SSOracleSwarmer).GetProperty(nameof(SSOracleSwarmer.Edible), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
            SSOracleSwarmer_Edible
            );

        new Hook(
            typeof(SLOracleSwarmer).GetProperty(nameof(SLOracleSwarmer.Edible), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
            SLOracleSwarmer_Edible
            );
    }















    public class PlayerModule
    {
        public readonly WeakReference<Player> playerRef;
        internal readonly SlugcatStats.Name playerName;
        internal readonly SlugcatStats.Name storyName;
        internal GravityController gravityController;


        public PlayerModule(Player player)
        {
            playerRef = new WeakReference<Player>(player);
            playerName = player.slugcatStats.name;
            if (player.room.game.session is StoryGameSession)
            {
                storyName = player.room.game.GetStorySession.saveStateNumber;
            }
            else { storyName = null; }


            if (playerName == Plugin.SlugcatStatsName && storyName != null)
            {
                Plugin.Log("gravity controller added");
                gravityController = new GravityController(player);
            }

                
        }

        public void Update(Player player, bool eu)
        {
            if (player.room == null) return;
            gravityController?.Update(eu, storyName == playerName);
        }

    }
















    // 启用重力控制时阻止y轴输入
    private static void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
    {
        bool getModule = Plugin.playerModules.TryGetValue(self, out var module) && module.playerName == Plugin.SlugcatStatsName;
        if (getModule)
        {
            if (module.gravityController != null && module.gravityController.isAbleToUse)
            {
                module.gravityController.inputY = self.input[0].y;
                self.input[0].y = 0;
            }
        }
        orig(self, eu);
    }







    // 重力显示
    private static void HUD_InitSinglePlayerHud(On.HUD.HUD.orig_InitSinglePlayerHud orig, HUD.HUD self, RoomCamera cam)
    {
        orig(self, cam);
        if ((self.owner as Player).slugcatStats.name == Plugin.SlugcatStatsName)
        {
            bool getModule = Plugin.playerModules.TryGetValue((self.owner as Player), out var module) && module.playerName == Plugin.SlugcatStatsName;
            if (getModule)
            {
                Plugin.Log("HUD add part");
                self.AddPart(new GravityMeter(self, self.fContainers[1], module.gravityController));
            }

        }

    }





    private static void Player_Jump(On.Player.orig_Jump orig, Player self)
    {
        orig(self);
        if (self.slugcatStats.name == Plugin.SlugcatStatsName)
        {
            self.jumpBoost *= 1.4f;
        }
    }





    // 垃圾回收
    private static void Player_Destroy(On.Player.orig_Destroy orig, Player self)
    {
        bool getModule = Plugin.playerModules.TryGetValue(self, out var module) && module.playerName == Plugin.SlugcatStatsName;
        if (getModule)
        {
            module.gravityController?.Destroy();
        }
        orig(self);
    }








    // 死亡时去除重力效果
    private static void Player_Die(On.Player.orig_Die orig, Player self)
    {
        bool getModule = Plugin.playerModules.TryGetValue(self, out var module) && module.playerName == Plugin.SlugcatStatsName;
        if (getModule && self.slugcatStats.name == Plugin.SlugcatStatsName)
        {
            module.gravityController.Die();
        }
        orig(self);

    }










    // 房间发生变化时保留重力变化
    private static void Player_NewRoom(On.Player.orig_NewRoom orig, Player self, Room newRoom)
    {
        orig(self, newRoom);
        
        bool getModule = Plugin.playerModules.TryGetValue(self, out var module) && module.playerName == Plugin.SlugcatStatsName;
        if (getModule && self.slugcatStats.name == Plugin.SlugcatStatsName)
        {
            bool isMyStory = newRoom.game.session is StoryGameSession && newRoom.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName;
            module.gravityController.NewRoom(isMyStory);
        }
    }





    // 以下都是有关不能吃神经元
    private static bool Player_CanBeSwallowed(On.Player.orig_CanBeSwallowed orig, Player self, PhysicalObject testObj)
    {
        if (self.slugcatStats.name.value == Plugin.SlugcatName)
        {
            return (!ModManager.MSC || !(self.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Spear)) && (testObj is Rock || testObj is DataPearl || testObj is FlareBomb || testObj is Lantern || testObj is FirecrackerPlant || (testObj is VultureGrub && !(testObj as VultureGrub).dead) || (testObj is Hazer && !(testObj as Hazer).dead && !(testObj as Hazer).hasSprayed) || testObj is FlyLure || testObj is ScavengerBomb || testObj is PuffBall || testObj is SporePlant || testObj is BubbleGrass || testObj is OracleSwarmer || testObj is NSHSwarmer || testObj is OverseerCarcass || (ModManager.MSC && testObj is FireEgg && self.FoodInStomach >= self.MaxFoodInStomach) || (ModManager.MSC && testObj is SingularityBomb && !(testObj as SingularityBomb).activateSingularity && !(testObj as SingularityBomb).activateSucktion));
        }
        else { return orig(self, testObj); }
    }




    private static bool Player_ObjectCountsAsFood(On.Player.orig_ObjectCountsAsFood orig, Player self, PhysicalObject obj)
    {
        bool result = orig(self, obj);
        if (self.slugcatStats.name == Plugin.SlugcatStatsName)
        {
            result = result && obj is not OracleSwarmer;
        }
        return result;
    }



    private static void Player_GrabUpdate(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        // 337末尾，修改神经元的可食用性
        if (c.TryGotoNext(MoveType.After,
            (i) => i.MatchCall<Creature>("get_grasps"),
            (i) => i.Match(OpCodes.Ldloc_S),
            (i) => i.Match(OpCodes.Ldelem_Ref),
            (i) => i.Match(OpCodes.Ldfld),
            (i) => i.Match(OpCodes.Isinst),
            (i) => i.Match(OpCodes.Callvirt)
            ))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, 13);
            c.EmitDelegate<Func<bool, Player, int, bool>>((edible, self, grasp) =>
            {
                if (self.slugcatStats.name == Plugin.SlugcatStatsName)
                {
                    bool isNotOracleSwarmer = self.grasps[grasp].grabbed is not OracleSwarmer;
                    return (edible && isNotOracleSwarmer);
                }
                else
                {
                    return edible;
                }

            });
        }

    }





    private delegate bool orig_SLOracleSwarmerEdible(SLOracleSwarmer self);
    private static bool SLOracleSwarmer_Edible(orig_SLOracleSwarmerEdible orig, SLOracleSwarmer self)
    {
        var result = orig(self);
        if (self.grabbedBy[0] != null && self.grabbedBy[0].grabber is Player && (self.grabbedBy[0].grabber as Player).slugcatStats.name == Plugin.SlugcatStatsName)
        {
            result = false;
        }
        return result;
    }



    private delegate bool orig_SSOracleSwarmerEdible(SSOracleSwarmer self);
    private static bool SSOracleSwarmer_Edible(orig_SSOracleSwarmerEdible orig, SSOracleSwarmer self)
    {
        var result = orig(self);
        if (self.grabbedBy[0] != null && self.grabbedBy[0].grabber is Player && (self.grabbedBy[0].grabber as Player).slugcatStats.name == Plugin.SlugcatStatsName)
        {
            result = false;
        }
        return result;
    }




}
