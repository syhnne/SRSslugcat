using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SRSslugcat;

internal class SLOracleHooks
{
    public static void Apply()
    {
        On.SLOracleBehaviorHasMark.MoonConversation.AddEvents += MoonConversation_AddEvents;
    }




    private static void MoonConversation_AddEvents(On.SLOracleBehaviorHasMark.MoonConversation.orig_AddEvents orig, SLOracleBehaviorHasMark.MoonConversation self)
    {
        if (self.myBehavior.oracle.room.game.IsStorySession && self.myBehavior.oracle.room.game.GetStorySession.saveStateNumber == Plugin.SlugcatStatsName)
        {
            Plugin.Log("moonRevived:", self.myBehavior.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.moonRevived);
            Plugin.Log("moon conversation:", self.id.ToString(), self.State.neuronsLeft.ToString());

            if (self.id == Conversation.ID.MoonFirstPostMarkConversation)
            {
                switch (Mathf.Clamp(self.State.neuronsLeft, 0, 5))
                {
                    // 不会有雨鹿绞尽脑汁绕过我加的食性限制还要吃神经元罢（
                    // 应当是不会的罢 所以我不写了（
                    case 0:
                        break;
                    case 1:
                        self.events.Add(new Conversation.TextEvent(self, 40, "...", 10));
                        return;
                    case 2:
                        self.events.Add(new Conversation.TextEvent(self, 30, self.Translate("Get... get away..."), 10));
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Please... thiss all I have left."), 10));
                        return;
                    case 3:
                        self.events.Add(new Conversation.TextEvent(self, 30, self.Translate("You!"), 10));
                        self.events.Add(new Conversation.TextEvent(self, 60, self.Translate("...you ate... me. Please go away. I won't speak... to you.<LINE>I... CAN'T speak to you... because... you ate...me..."), 0));
                        return;
                    case 4:
                        // 哈？这两个文件是啥啊
                        /*Plugin.LogAllConversations(self, 35);
                        Plugin.LogAllConversations(self, 37);*/
                        self.LoadEventsFromFile(35);
                        self.LoadEventsFromFile(37);
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("I'm still angry at you, but it is good to have someone to talk to after all self time.<LINE>The scavengers aren't exactly good listeners. They do bring me things though, occasionally..."), 0));
                        return;
                    case 5:

                        // 芜，我明白了，去抄一下魔方节点那里的对话代码
                        // 呃，总之，，我准备使用cutscene mode 并且从pickup candidates里去掉sloracleswarmer防止有雨鹿尝试抓月姐的神经元（主要是为我减小写对话的工作量）
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Hello <PlayerName>."), 0));
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("(The conversation is a work in progress, please wait for future updates)"), 0));
                        return;
                    default:
                        return;

                }
            }

            // 多洗爹？？
            // TODO: 算了 先注释他 以后再思考这个咋写（。 
            /*else if (self.id == Conversation.ID.Moon_Misc_Item && self.describeItem is OxygenMaskModules.OxygenMaskMisc)
            {

            }*/



        }
        else { orig(self); }
    }


}
