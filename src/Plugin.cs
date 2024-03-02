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
    public static bool DevMode = true;
    internal static readonly Color spearColor = new Color(1f, 0.2f, 0.1f);
    internal static readonly Color32 bodyColor = new Color32(255, 207, 88, 255);
    internal static readonly List<int> ColoredBodyParts = new List<int>() { 2, 3, };



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
            
            On.Player.Update += Player_Update;
            On.Player.ctor += Player_ctor;

            On.World.GetNode += World_GetNode;





        }
        catch (Exception ex)
        {
            base.Logger.LogError(ex);
        }
    }



    private void LoadResources(RainWorld rainWorld)
    {
       
        try
        {
            MachineConnector.SetRegisteredOI("syhnne.SRSslugcat", this.option);

            bool isInit = IsInit;
            if (!isInit)
            {
                LogStat("INIT");
                IsInit = true;
                // Futile.atlasManager.LoadAtlas("atlases/headset");
                Futile.atlasManager.LoadAtlas("atlases/head");
                Futile.atlasManager.LoadAtlas("atlases/tail");
                // Futile.atlasManager.LogAllElementNames();
            }
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











    private AbstractRoomNode World_GetNode(On.World.orig_GetNode orig, World self, WorldCoordinate c)
    {
        Plugin.Log("GetNode - room nodes:", self.GetAbstractRoom(c.room).nodes.Length, "abstractnode:", c.abstractNode);
        if (c.abstractNode > self.GetAbstractRoom(c.room).nodes.Length || c.abstractNode < 0)
        {
            Plugin.Log("!!!!!!!!");
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
        if (getModule) module.Update(self, eu);
        orig(self, eu);


        if (self.room == null || self.dead || self.slugcatStats.name != SlugcatStatsName) return;
        bool isMyStory = self.room.game.IsStorySession && self.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName;

        if (!self.Malnourished && self.Submersion > 0.2f)
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
        Plugin.Log("waterdeath");
        Vector2 vector = Vector2.Lerp(player.firstChunk.pos, player.firstChunk.lastPos, 0.35f);
        room.PlaySound(SoundID.Firecracker_Burn, vector);
        room.ScreenMovement(new Vector2?(vector), default(Vector2), 1.3f);
        room.InGameNoise(new InGameNoise(vector, 8000f, player, 1f));
        player.Die();
    }



}