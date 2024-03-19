using System;
using BepInEx;
using UnityEngine;
using SlugBase.Features;
using static SlugBase.Features.FeatureTypes;
using System.Drawing.Text;
using RWCustom;
using Noise;
using System.Collections.Generic;
using MoreSlugcats;
using BepInEx.Logging;
using Smoke;
using Random = UnityEngine.Random;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Menu.Remix.MixedUI;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlugBase.DataTypes;
using MonoMod.RuntimeDetour;
using System.Reflection;
using SlugBase.SaveData;
using static SRSslugcat.PlayerHooks;
using System.Runtime.InteropServices;
using static MonoMod.InlineRT.MonoModRule;
using System.Runtime.CompilerServices;
using IL.Menu;




namespace SRSslugcat;




// 本来准备考完再整的。但是我急了，所以启动！！

[BepInPlugin(MOD_ID, "srs", "0.1.0")]
class Plugin : BaseUnityPlugin
{
    private const string MOD_ID = "syhnne.SRSslugcat";
    public static new ManualLogSource Logger { get; internal set; }
    public static ConditionalWeakTable<Player, PlayerModule> playerModules = new ConditionalWeakTable<Player, PlayerModule>();
    internal static readonly string SlugcatName = "SRSslugcat";
    internal static readonly SlugcatStats.Name SlugcatStatsName = new SlugcatStats.Name(SlugcatName);
    internal static Plugin instance;
    internal bool IsInit;
    internal static readonly bool ShowLogs = true;
    public Options option;
    public static bool DevMode = false;
    


    public void OnDisable()
    {
        try
        {
            On.Player.Update -= Player_Update;
            On.Player.ctor -= Player_ctor;
            On.Player.LungUpdate -= Player_LungUpdate;
            On.Player.IsObjectThrowable -= Player_IsObjectThrowable;

            On.World.GetNode -= World_GetNode;

            
            On.RainWorld.OnModsInit -= Extras.WrapInit(LoadResources);
            PlayerHooks.Disable();
            CustomPlayerGraphics.Disable();
            SSRoomEffects.Disable();
            SLOracleHooks.Disable();
            OxygenMaskModules.Disable();
            CustomLore.Disable();

            instance = null;
            option = null;








        }
        catch (Exception ex)
        {
            base.Logger.LogError(ex);
        }
    }
    public void OnEnable()
    {
        try
        {
            instance = this;
            option = new Options();
            On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources);
            PlayerHooks.Apply();
            CustomPlayerGraphics.Apply();
            SSRoomEffects.Apply();
            SLOracleHooks.Apply();
            OxygenMaskModules.Apply();
            CustomLore.Apply();
            


            On.Player.Update += Player_Update;
            On.Player.ctor += Player_ctor;
            On.Player.LungUpdate += Player_LungUpdate;
            On.Player.IsObjectThrowable += Player_IsObjectThrowable;

            On.World.GetNode += World_GetNode;


            if (!Futile.atlasManager.DoesContainElementWithName("srs_tail"))
            {
                Futile.atlasManager.LoadAtlas("atlases/srs_tail");
            }
            if (!Futile.atlasManager.DoesContainElementWithName("srs_HeadA0"))
            {
                Futile.atlasManager.LoadAtlas("atlases/srs_head");
            }

        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
            base.Logger.LogError(ex);
        }
    }



    private void LoadResources(RainWorld rainWorld)
    {
       
        try
        {
            MachineConnector.SetRegisteredOI("syhnne.SRSslugcat", this.option);

            LogStat("INIT");
            // Futile.atlasManager.LoadAtlas("atlases/headset");
            
            
            // Futile.atlasManager.LogAllElementNames();
        }
        catch (Exception ex)
        {
            base.Logger.LogError(ex);
            throw;
        }
    }



    /// <summary>
    /// 输出日志。搜索的时候带上后面的冒号
    /// </summary>
    /// <param name="text"></param>
    public static void Log(params object[] text)
    {
        if (ShowLogs)
        {
            string log = "";
            foreach (object s in text)
            {
                log += s.ToString();
                log += " ";
            }
            Debug.Log("[SRSslugcat] : " + log);
        }

    }

    /// <summary>
    /// 用来输出一些我暂时用不到，但测试时可能有用的日志，后面没有那个冒号，这样我不想搜索的时候就搜不到
    /// </summary>
    /// <param name="text"></param>
    public static void LogStat(params object[] text)
    {
        if (ShowLogs)
        {
            string log = "";
            foreach (object s in text)
            {
                log += s.ToString();
                log += " ";
            }
            Debug.Log("[SRSslugcat] " + log);
        }

    }










    // 绷不住了，直到最后我也不知道到底是谁在给这个函数传非法参数。
    // 我随便给他返回了一个空的坐标，想着这样我就能从报错信息里知道调用方是谁，结果他不吱声了。
    // 总之他跑起来了，就这样吧
    private AbstractRoomNode World_GetNode(On.World.orig_GetNode orig, World self, WorldCoordinate c)
    {
        // Plugin.Log("GetNode - room nodes:", self.GetAbstractRoom(c.room).nodes.Length, "abstractnode:", c.abstractNode);
        if (c.abstractNode > self.GetAbstractRoom(c.room).nodes.Length || c.abstractNode < 0)
        {
            // Plugin.Log("!!!!!!!!");
            return new AbstractRoomNode();
        }
        return orig(self, c);
    }




    private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);

        if (world.game.session is StoryGameSession && world.game.GetStorySession.characterStats.name.value == SlugcatName && self.slugcatStats.name.value == SlugcatName)
        {
            playerModules.Add(self, new PlayerModule(self));

        }

    }




    // update
    private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        bool getModule = playerModules.TryGetValue(self, out var module) && module.playerName == SlugcatStatsName;

        if (getModule) 
        { 
            module.Update(self, eu);
        }
        orig(self, eu);


        if (self.room == null || self.dead || !getModule || self.slugcatStats.name != Plugin.SlugcatStatsName) return;
        bool isMyStory = self.room.game.IsStorySession && self.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName;

        // 为了防止玩家发现虚空流体就是水这个事实（。
        if (!self.Malnourished && self.Submersion > 0.2f && self.room.abstractRoom.name != "SB_L01" && self.room.abstractRoom.name != "FR_FINAL")
        {
            WaterDeath(self, self.room);
        }
        // 啊这，下面这个先不要了，我发现即便我在避难所里了也能死掉
        /*if (self.room.game.globalRain != null && self.room.game.globalRain.Intensity > 0.2f)
        {
            WaterDeath(self, self.room);
        }*/

    }







    // 不知道咋写，先凑合一下（
    // 啊啊啊啊啊啊啊啊啊啊啊啊啊我的耳朵！！
    // 嗷 原来是他被调用好几次（
    internal void WaterDeath(Player player, Room room)
    {
        if (player.dead) { return; }
        for (int i = 0; i < player.grasps.Length; i++)
        {
            if (player.grasps[i] != null && (player.grasps[i].grabbed is OxygenMaskModules.OxygenMask))
            {
                return;
            }
            else if (player.grasps[i] != null && player.grasps[i].grabbed is BubbleGrass) 
            {
                BubbleGrass bubbleGrass = player.grasps[i].grabbed as BubbleGrass;
                Plugin.Log("bubbleGrass oxygen left:", bubbleGrass.AbstrBubbleGrass.oxygenLeft);
                if (player.animation == Player.AnimationIndex.SurfaceSwim)
                {
                    bubbleGrass.AbstrBubbleGrass.oxygenLeft = Mathf.Max(0f, bubbleGrass.AbstrBubbleGrass.oxygenLeft - 0.0009090909f);
                }
                if (bubbleGrass.AbstrBubbleGrass.oxygenLeft > 0f) return;
            }
        }
        // 咋说，这玩意儿应该不能被放在肚子里，这很奇怪（
        // if (player.objectInStomach is OxygenMaskModules.OxygenMaskAbstract) { return; }

        Plugin.Log("waterdeath");
        Vector2 vector = Vector2.Lerp(player.firstChunk.pos, player.firstChunk.lastPos, 0.35f);
        room.PlaySound(SoundID.Firecracker_Burn, vector);
        room.ScreenMovement(new Vector2?(vector), default(Vector2), 1.3f);
        room.InGameNoise(new InGameNoise(vector, 8000f, player, 1f));
        player.Die();
    }






    #region OxygenMask

    private void Player_LungUpdate(On.Player.orig_LungUpdate orig, Player self)
    {
        bool haveOxygenMask = false;
        OxygenMaskModules.OxygenMask mask = null;
        for (int i = 0; i < self.grasps.Length; i++)
        {
            if (self.grasps[i] != null && self.grasps[i].grabbed is OxygenMaskModules.OxygenMask)
            {
                haveOxygenMask = true; 
                mask = self.grasps[i].grabbed as OxygenMaskModules.OxygenMask;
                break;
            }
        }
        if (haveOxygenMask && mask != null && mask.count != 1)
        {
            // 没错，按照整数倍提高肺活量的最好办法就是——抽帧！
            // 现在fp也可以拥有比肩水猫的肺活量了，我还是把这个数改小一点罢
        }
        else { orig(self); }

    }



    // 防止氧气面罩被扔出去（虽然fisobs貌似附带了类似的功能，但他不好使啊（汗
    private bool Player_IsObjectThrowable(On.Player.orig_IsObjectThrowable orig, Player self, PhysicalObject obj)
    {
        if (obj is OxygenMaskModules.OxygenMask)
        {
            return false;
        }
        return orig(self, obj);
    }

    #endregion






    // TODO: 修复一下用fp玩剧情模式，ssai会有零重力的bug。我怀疑是两个模组在一起发生了化学反应
}