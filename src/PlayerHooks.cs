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

            // 太好玩了。等我把4个全都搓出来，这里需要3条判定（
            // 其实有空的话，最好把这个东西绑到别的地方去……但我真的懒得重构代码……
            if (playerName == Plugin.SlugcatStatsName && storyName != null && storyName.value != "PebblesSlug")
            {
                Plugin.LogStat("gravity controller added");
                gravityController = new GravityController(player);
            }

                
        }

        public void Update(Player player, bool eu)
        {
            if (player.room == null || player.dead) return;
            gravityController?.Update(eu, storyName == playerName);
        }

    }












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

        On.Player.ClassMechanicsSpearmaster += Player_ClassMechanicsSpearmaster;
        On.Player.Grabability += Player_Grabability;
        On.Spear.Spear_NeedleCanFeed += Spear_NeedleCanFeed;
        On.Spear.HitSomething += Spear_HitSomething;
        On.Spear.DrawSprites += Spear_DrawSprites;
        On.PlayerGraphics.TailSpeckles.setSpearProgress += TailSpeckles_setSpearProgress;
        



        new Hook(
            typeof(SSOracleSwarmer).GetProperty(nameof(SSOracleSwarmer.Edible), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
            SSOracleSwarmer_Edible
            );

        new Hook(
            typeof(SLOracleSwarmer).GetProperty(nameof(SLOracleSwarmer.Edible), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
            SLOracleSwarmer_Edible
            );
    }



    




    // 借用speartype标记拔出的矛的类型。源代码这个数字不会超过3，而且使用的时候是取的他的余数，所以我可以随便往上加
    private static void TailSpeckles_setSpearProgress(On.PlayerGraphics.TailSpeckles.orig_setSpearProgress orig, PlayerGraphics.TailSpeckles self, float p)
    {
        if (self.pGraphics.player.slugcatStats.name == Plugin.SlugcatStatsName && !self.pGraphics.player.Malnourished)
        {
            self.spearType = Random.Range(4, 7);
            self.spearProg = Mathf.Clamp(p, 0f, 1f);
        }
        else { orig(self, p); }
    }





    private static void Spear_DrawSprites(On.Spear.orig_DrawSprites orig, Spear self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);
        if (self.IsNeedle && self.spearmasterNeedleType > 3)
        {
            float num = (float)self.spearmasterNeedle_fadecounter / (float)self.spearmasterNeedle_fadecounter_max;
            if (self.spearmasterNeedle_hasConnection)
            {
                num = 1f;
            }
            if (num < 0.01f)
            {
                num = 0.01f;
            }
            if (ModManager.CoopAvailable && self.jollyCustomColor != null)
            {
                sLeaser.sprites[0].color = self.jollyCustomColor.Value;
            }
            else if (PlayerGraphics.CustomColorsEnabled())
            {
                sLeaser.sprites[0].color = Color.Lerp(PlayerGraphics.CustomColorSafety(2), self.color, 1f - num);
            }
            else
            {
                sLeaser.sprites[0].color = Color.Lerp(Plugin.spearColor, self.color, 1f - num);
            }
        }
    }





    // 使被击中的生物当场去世
    private static bool Spear_HitSomething(On.Spear.orig_HitSomething orig, Spear self, SharedPhysics.CollisionResult result, bool eu)
    {
        bool die = false;
        if (result.obj != null && result.obj is Creature && !(result.obj as Creature).dead
            && self.thrownBy is Player && (self.thrownBy as Player).slugcatStats.name == Plugin.SlugcatStatsName
            && self.Spear_NeedleCanFeed() && self.spearmasterNeedleType > 3
            && (result.obj as Creature).SpearStick(self, Mathf.Lerp(0.55f, 0.62f, Random.value), result.chunk, result.onAppendagePos, self.firstChunk.vel))
        {
            die = true;
        }

        bool res = orig(self, result, eu);
        if (die)
        {
            Plugin.Log("creature instant death:", (result.obj as Creature).GetType().ToString());
            (result.obj as Creature).Violence(self.firstChunk, new Vector2?(self.firstChunk.vel * self.firstChunk.mass * 2f), result.chunk, result.onAppendagePos, Creature.DamageType.Stab, 99f, 20f);

            /*(result.obj as Creature).Die();
            (result.obj as Creature).SetKillTag(self.thrownBy.abstractCreature);*/
        }
        return res;
    }









    private static bool Spear_NeedleCanFeed(On.Spear.orig_Spear_NeedleCanFeed orig, Spear self)
    {
        if (self.thrownBy != null && self.thrownBy is Player && (self.thrownBy as Player).slugcatStats.name == Plugin.SlugcatStatsName && self.spearmasterNeedle && self.spearmasterNeedle_hasConnection)
        {
            return true;
        }
        return orig(self);
    }

    



    // 双持
    private static Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
    {
        if (self.slugcatStats.name == Plugin.SlugcatStatsName && obj is Weapon)
        {
            return Player.ObjectGrabability.OneHand;
        }
        else { return orig(self, obj); }
    }








    // 挂这里不是因为需要挂这里，而是因为这个比较好认（？
    private static void Player_ClassMechanicsSpearmaster(On.Player.orig_ClassMechanicsSpearmaster orig, Player self)
    {
        orig(self);
        if (self.slugcatStats.name != Plugin.SlugcatStatsName) return;


        if ((self.graphicsModule as PlayerGraphics).tailSpecks == null) 
        {
            Plugin.Log("ERROR: tailSpecks not found");
            return;
        }

        // 20
        if (!self.input[0].pckp || self.input[0].y != 1)
        {
            PlayerGraphics.TailSpeckles tailSpecks = (self.graphicsModule as PlayerGraphics).tailSpecks;
            if (tailSpecks.spearProg > 0f)
            {
                tailSpecks.setSpearProgress(Mathf.Lerp(tailSpecks.spearProg, 0f, 0.05f));
                if (tailSpecks.spearProg < 0.025f)
                {
                    tailSpecks.setSpearProgress(0f);
                }
            }
            else
            {
                self.smSpearSoundReady = false;
            }
        }

        // 100
        int num5 = -1;
        for (int i = 0; i < 2; i++) 
        {
            if (self.grasps[i] != null && self.grasps[i].grabbed is IPlayerEdible && (self.grasps[i].grabbed as IPlayerEdible).Edible)
            {
                num5 = i;
            }
        }

        // 174 需要按住拾取和上键
        if ((self.grasps[0] == null || self.grasps[1] == null) && num5 == -1 && self.input[0].y == 1)
        {
            
            PlayerGraphics.TailSpeckles tailSpecks = (self.graphicsModule as PlayerGraphics).tailSpecks;
            if (tailSpecks.spearProg == 0f)
            {
                tailSpecks.newSpearSlot();
            }
            if (tailSpecks.spearProg < 0.1f)
            {
                tailSpecks.setSpearProgress(Mathf.Lerp(tailSpecks.spearProg, 0.11f, 0.1f));
            }
            else
            {
                self.Blink(5);
                if (!self.smSpearSoundReady)
                {
                    self.smSpearSoundReady = true;
                    self.room.PlaySound(MoreSlugcatsEnums.MSCSoundID.SM_Spear_Pull, 0f, 1f, 1f + Random.value * 0.5f);
                }
                tailSpecks.setSpearProgress(Mathf.Lerp(tailSpecks.spearProg, 1f, 0.05f));
            }
            if (tailSpecks.spearProg > 0.6f)
            {
                (self.graphicsModule as PlayerGraphics).head.vel += Custom.RNV() * ((tailSpecks.spearProg - 0.6f) / 0.4f) * 2f;
            }
            if (tailSpecks.spearProg > 0.95f)
            {
                tailSpecks.setSpearProgress(1f);
            }
            if (tailSpecks.spearProg == 1f)
            {
                self.room.PlaySound(MoreSlugcatsEnums.MSCSoundID.SM_Spear_Grab, 0f, 1f, 0.5f + Random.value * 1.5f);
                self.smSpearSoundReady = false;
                Vector2 pos = (self.graphicsModule as PlayerGraphics).tail[(int)((float)(self.graphicsModule as PlayerGraphics).tail.Length / 2f)].pos;
                for (int j = 0; j < 4; j++)
                {
                    Vector2 vector = Custom.DirVec(pos, self.bodyChunks[1].pos);
                    self.room.AddObject(new WaterDrip(pos + Custom.RNV() * Random.value * 1.5f, Custom.RNV() * 3f * Random.value + vector * Mathf.Lerp(2f, 6f, Random.value), false));
                }
                for (int k = 0; k < 5; k++)
                {
                    Vector2 vector2 = Custom.RNV();
                    self.room.AddObject(new Spark(pos + vector2 * Random.value * 40f, vector2 * Mathf.Lerp(4f, 30f, Random.value), Color.white, null, 4, 18));
                }
                int spearType = tailSpecks.spearType;
                tailSpecks.setSpearProgress(0f);
                AbstractSpear abstractSpear = new AbstractSpear(self.room.world, null, self.room.GetWorldCoordinate(self.mainBodyChunk.pos), self.room.game.GetNewID(), false);
                self.room.abstractRoom.AddEntity(abstractSpear);
                abstractSpear.pos = self.abstractCreature.pos;
                abstractSpear.RealizeInRoom();
                Vector2 vector3 = self.bodyChunks[0].pos;
                Vector2 vector4 = Custom.DirVec(self.bodyChunks[1].pos, self.bodyChunks[0].pos);
                if (Mathf.Abs(self.bodyChunks[0].pos.y - self.bodyChunks[1].pos.y) > Mathf.Abs(self.bodyChunks[0].pos.x - self.bodyChunks[1].pos.x) && self.bodyChunks[0].pos.y > self.bodyChunks[1].pos.y)
                {
                    vector3 += Custom.DirVec(self.bodyChunks[1].pos, self.bodyChunks[0].pos) * 5f;
                    vector4 *= -1f;
                    vector4.x += 0.4f * (float)self.flipDirection;
                    vector4.Normalize();
                }
                abstractSpear.realizedObject.firstChunk.HardSetPosition(vector3);
                abstractSpear.realizedObject.firstChunk.vel = Vector2.ClampMagnitude((vector4 * 2f + Custom.RNV() * Random.value) / abstractSpear.realizedObject.firstChunk.mass, 6f);
                if (self.FreeHand() > -1)
                {
                    self.SlugcatGrab(abstractSpear.realizedObject, self.FreeHand());
                }
                if (abstractSpear.type == AbstractPhysicalObject.AbstractObjectType.Spear)
                {
                    (abstractSpear.realizedObject as Spear).Spear_makeNeedle(spearType, true);
                    if ((self.graphicsModule as PlayerGraphics).useJollyColor)
                    {
                        (abstractSpear.realizedObject as Spear).jollyCustomColor = new Color?(PlayerGraphics.JollyColor(self.playerState.playerNumber, 2));
                    }
                }
                self.wantToThrow = 0;
            }
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
                Plugin.LogStat("HUD gravityMeter");
                self.AddPart(new GravityMeter(self, self.fContainers[1], module.gravityController));
            }

        }

    }





    private static void Player_Jump(On.Player.orig_Jump orig, Player self)
    {
        orig(self);
        if (self.slugcatStats.name == Plugin.SlugcatStatsName)
        {
            self.jumpBoost *= 1.1f;
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

        /*// 119
        // 我是矛大师，让我拔矛
        // 没事了，我突然想起来其实我完全可以另起一个函数（（
        ILCursor c2 = new(il);
        if (c2.TryGotoNext(MoveType.After,
            (i) => i.Match(OpCodes.Ldarg_0),
            (i) => i.MatchLdfld<Player>("SlugCatClass"),
            (i) => i.MatchLdsfld<MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName>("Spear"),
            (i) => i.Match(OpCodes.Call)
            ))
        {
            Plugin.Log("grab update c2 match");
            *//*c2.Emit(OpCodes.Ldarg_0);
            c2.Emit(OpCodes.Ldloc, 13);
            c2.EmitDelegate<Func<bool, Player, int, bool>>((edible, self, grasp) =>
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

            });*//*
        }*/

        /*ILCursor c3 = new ILCursor(il);
        // 337末尾，修改神经元的可食用性
        if (c3.TryGotoNext(MoveType.After,
            (i) => i.MatchCall<Creature>("get_grasps"),
            (i) => i.Match(OpCodes.Ldloc_S),
            (i) => i.Match(OpCodes.Ldelem_Ref),
            (i) => i.Match(OpCodes.Ldfld),
            (i) => i.Match(OpCodes.Isinst),
            (i) => i.Match(OpCodes.Callvirt)
            ))
        {
            c3.Emit(OpCodes.Ldarg_0);
            c3.Emit(OpCodes.Ldloc, 13);
            c3.EmitDelegate<Func<bool, Player, int, bool>>((edible, self, grasp) =>
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
        }*/

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
