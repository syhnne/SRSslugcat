using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoreSlugcats;

namespace SRSslugcat;

internal class CustomLore
{
    public static void Disable()
    {
        On.MoreSlugcats.MSCRoomSpecificScript.OE_GourmandEnding.Update -= MSCRoomSpecificScript_GourmandEnding_Update;
        On.RegionGate.customOEGateRequirements -= RegionGate_customOEGateRequirements;
        On.RainWorldGame.ctor -= RainWorldGame_ctor;
    }

    public static void Apply()
    {
        On.MoreSlugcats.MSCRoomSpecificScript.OE_GourmandEnding.Update += MSCRoomSpecificScript_GourmandEnding_Update;
        On.RegionGate.customOEGateRequirements += RegionGate_customOEGateRequirements;
        On.RainWorldGame.ctor += RainWorldGame_ctor;
    }


    // 复活月姐
    private static void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
    {
        orig(self, manager);
        if (self.IsStorySession && self.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName)
        {
            // self.GetStorySession.saveState.miscWorldSaveData.moonRevived = true;
            self.GetStorySession.saveState.miscWorldSaveData.moonHeartRestored = true;
            self.GetStorySession.saveState.miscWorldSaveData.pebblesEnergyTaken = true;
        }
    }



    // 防止玩家归乡
    private static void MSCRoomSpecificScript_GourmandEnding_Update(On.MoreSlugcats.MSCRoomSpecificScript.OE_GourmandEnding.orig_Update orig, MSCRoomSpecificScript.OE_GourmandEnding self, bool eu)
    {
        if (self.room.game.IsStorySession && self.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName) return;
        orig(self, eu);
    }



    // 打开外层空间的门。这是你唯一出去的路了，毕竟不能走根源设施（但说实话被困外层空间挺难顶的，给他们一个3-4级的初始业力罢
    private static bool RegionGate_customOEGateRequirements(On.RegionGate.orig_customOEGateRequirements orig, RegionGate self)
    {
        if (self.room.game.IsStorySession && self.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName)
        {
            return true;
        }
        return orig(self);
    }








}
