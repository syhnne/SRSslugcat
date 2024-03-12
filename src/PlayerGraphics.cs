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





internal class CustomPlayerGraphics
{


    internal static readonly Color spearColor = new Color(1f, 0.2f, 0.1f);
    internal static readonly Color32 bodyColor = new Color32(255, 207, 88, 255);
    internal static readonly List<int> ColoredBodyParts = new List<int>() { 2, 3, };


    public static void Disable()
    {
        On.PlayerGraphics.ctor -= PlayerGraphics_ctor;

        On.PlayerGraphics.InitiateSprites -= InitiateSprites;
        On.PlayerGraphics.AddToContainer -= AddToContainer;
        On.PlayerGraphics.DrawSprites -= DrawSprites;
        On.PlayerGraphics.TailSpeckles.DrawSprites -= TailSpecks_DrawSprites;
        On.PlayerGraphics.ApplyPalette -= ApplyPalette;

        On.PlayerGraphics.ColoredBodyPartList -= ColoredBodyPartList;
        On.PlayerGraphics.DefaultBodyPartColorHex -= DefaultBodyPartColorHex;

    }

    public static void Apply()
    {
        On.PlayerGraphics.ctor += PlayerGraphics_ctor;

        On.PlayerGraphics.InitiateSprites += InitiateSprites;
        On.PlayerGraphics.AddToContainer += AddToContainer;
        On.PlayerGraphics.DrawSprites += DrawSprites;
        On.PlayerGraphics.TailSpeckles.DrawSprites += TailSpecks_DrawSprites;
        On.PlayerGraphics.ApplyPalette += ApplyPalette;

        On.PlayerGraphics.ColoredBodyPartList += ColoredBodyPartList;
        On.PlayerGraphics.DefaultBodyPartColorHex += DefaultBodyPartColorHex;

        
    }







    private static void ApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        orig(self, sLeaser, rCam, palette);
        if (self.player.slugcatStats.name == Plugin.SlugcatStatsName)
        {
            for (int i = 0; i < 12; i++)
            {
                if (ColoredBodyParts.Contains(i))
                {
                    sLeaser.sprites[i].color = new Color(1f, 1f, 1f);
                }
                else
                {
                    sLeaser.sprites[i].color = bodyColor;
                }
            }
        }
    }






    private static List<string> DefaultBodyPartColorHex(On.PlayerGraphics.orig_DefaultBodyPartColorHex orig, SlugcatStats.Name slugcatID)
    {
        if (slugcatID == Plugin.SlugcatStatsName)
        {
            List<string> list = new List<string>();
            Color col = PlayerGraphics.DefaultSlugcatColor(slugcatID);
            list.Add(Custom.colorToHex(col));
            list.Add("101010");
            list.Add("ff371b");
            return list;
        }
        else { return orig(slugcatID); }
    }






    private static List<string> ColoredBodyPartList(On.PlayerGraphics.orig_ColoredBodyPartList orig, SlugcatStats.Name slugcatID)
    {
        if (slugcatID == Plugin.SlugcatStatsName)
        {
            List<string> list = new List<string>
            {
                "Body",
                "Eyes",
                "Spears"
            };
            return list;
        }
        else { return orig(slugcatID); }
    }








    private static void PlayerGraphics_ctor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
    {
        orig(self, ow);
        if (self.player != null && self.player.slugcatStats.name == Plugin.SlugcatStatsName)
        {
            self.tailSpecks = new PlayerGraphics.TailSpeckles(self, ModManager.MSC? 13 : 12);

            // 这个数据回头再改（
            if (self.player.playerState.isPup)
            {
                self.tail[0] = new TailSegment(self, 7f, 2f, null, 0.85f, 1f, 1f, true);
                self.tail[1] = new TailSegment(self, 5f, 3.5f, self.tail[0], 0.85f, 1f, 0.5f, true);
                self.tail[2] = new TailSegment(self, 4f, 3.5f, self.tail[1], 0.85f, 1f, 0.5f, true);
                self.tail[3] = new TailSegment(self, 2f, 3.5f, self.tail[2], 0.85f, 1f, 0.5f, true);
            }
            else
            {
                self.tail[0] = new TailSegment(self, 7f, 4f, null, 0.85f, 1f, 1f, true);
                self.tail[1] = new TailSegment(self, 6f, 7f, self.tail[0], 0.85f, 1f, 0.5f, true);
                self.tail[2] = new TailSegment(self, 4f, 7f, self.tail[1], 0.85f, 1f, 0.5f, true);
                self.tail[3] = new TailSegment(self, 3.5f, 7f, self.tail[2], 0.85f, 1f, 0.5f, true);
                self.tail.Append(new TailSegment(self, 2f, 5f, self.tail[3], 0.85f, 1f, 0.5f, true));
            }


        }
    }





    // 卧槽 这原函数里藏啥了，我一执行他 他就绕过去
    // 我另一个mod里头可没见过这种场面
    // 有没有人能告诉我，为啥这个orig还自带return的作用
    // 如果不执行这个函数，那么后面RemoveAllSpritesFromContainer()的时候就会报错，就算他不报错，AddChild也会报错
    //
    // 除此以外，还有一个很可疑的地方就是options tab加载不出来，这俩可能是同一个bug导致的罢
    // 但是为什么啊（抓狂）我另一个mod没这问题啊！！！！！！！！！！！！！！
    //
    // 卧槽 见鬼了 我把剩下俩函数注释掉，他执行了，然后报了一个 我闻所未闻的错
    private static void InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        orig(self, sLeaser, rCam);
        if (self.player.slugcatStats.name == Plugin.SlugcatStatsName && self.tailSpecks != null)
        {
            /*for (int i = 0; i < sLeaser.sprites.Length + self.tailSpecks.numberOfSprites; i++)
            {
                sLeaser.sprites.Append(new FSprite());
            }
            self.tailSpecks.InitiateSprites(sLeaser, rCam);

            self.AddToContainer(sLeaser, rCam, null);*/

            sLeaser.RemoveAllSpritesFromContainer();
            sLeaser.sprites = new FSprite[13 + self.tailSpecks.numberOfSprites];

            sLeaser.sprites[0] = new FSprite("BodyA", true);
            sLeaser.sprites[0].anchorY = 0.7894737f;
            if (self.RenderAsPup)
            {
                sLeaser.sprites[0].scaleY = 0.5f;
            }
            sLeaser.sprites[1] = new FSprite("HipsA", true);
            TriangleMesh.Triangle[] tris = new TriangleMesh.Triangle[]
            {
            new TriangleMesh.Triangle(0, 1, 2),
            new TriangleMesh.Triangle(1, 2, 3),
            new TriangleMesh.Triangle(4, 5, 6),
            new TriangleMesh.Triangle(5, 6, 7),
            new TriangleMesh.Triangle(8, 9, 10),
            new TriangleMesh.Triangle(9, 10, 11),
            new TriangleMesh.Triangle(12, 13, 14),
            new TriangleMesh.Triangle(2, 3, 4),
            new TriangleMesh.Triangle(3, 4, 5),
            new TriangleMesh.Triangle(6, 7, 8),
            new TriangleMesh.Triangle(7, 8, 9),
            new TriangleMesh.Triangle(10, 11, 12),
            new TriangleMesh.Triangle(11, 12, 13)
            };
            TriangleMesh triangleMesh;
            if (Futile.atlasManager.DoesContainElementWithName("srs_tail"))
            {
                triangleMesh = new TriangleMesh("srs_tail", tris, false, false);
            }
            else
            {
                triangleMesh = new TriangleMesh("Futile_White", tris, false, false);
            }
            sLeaser.sprites[2] = triangleMesh;
            if (Futile.atlasManager.DoesContainElementWithName("srs_HeadA0"))
            {
                sLeaser.sprites[3] = new FSprite("srs_HeadA0", true);
            }
            else
            {
                sLeaser.sprites[3] = new FSprite("HeadA0", true);
            }
            sLeaser.sprites[4] = new FSprite("LegsA0", true);
            sLeaser.sprites[4].anchorY = 0.25f;
            sLeaser.sprites[5] = new FSprite("PlayerArm0", true);
            sLeaser.sprites[5].anchorX = 0.9f;
            sLeaser.sprites[5].scaleY = -1f;
            sLeaser.sprites[6] = new FSprite("PlayerArm0", true);
            sLeaser.sprites[6].anchorX = 0.9f;
            sLeaser.sprites[7] = new FSprite("OnTopOfTerrainHand", true);
            sLeaser.sprites[8] = new FSprite("OnTopOfTerrainHand", true);
            sLeaser.sprites[8].scaleX = -1f;
            sLeaser.sprites[9] = new FSprite("FaceA0", true);
            sLeaser.sprites[11] = new FSprite("pixel", true);
            sLeaser.sprites[11].scale = 5f;
            sLeaser.sprites[10] = new FSprite("Futile_White", true);
            sLeaser.sprites[10].shader = rCam.game.rainWorld.Shaders["FlatLight"];

            self.tailSpecks.InitiateSprites(sLeaser, rCam);
            self.gown.InitiateSprite(self.gownIndex, sLeaser, rCam);

            // 太烧脑了，我终于懂了，0-11是原版sprite，12是msc加的那个gown，13开始才是我需要的


            self.AddToContainer(sLeaser, rCam, null);
        }



    }





    // TODO: 目前这个会导致看不到tailSpecks。正合我意，不用修造矛过程的bug了（逃
    // 我的建议是等有空的时候直接重写一个类似的东西（。
    private static void AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
        orig(self, sLeaser, rCam, newContatiner);
        // 确认了，是以下部分的某一句话导致initiatesprites直接return了
        if (self.player.slugcatStats.name == Plugin.SlugcatStatsName && self.tailSpecks != null)
        {
            // 就是这一句话导致的，但这是咋回事
            // 不是 为什么我在这随便加一句什么涉及到newContainer的语句 就会出问题啊
            // 噢噢 他是null啊（擦汗
            // Plugin.Log(newContatiner.GetChildCount());
            /*for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                newContatiner.AddChild(sLeaser.sprites[i]);
            }*/
            // self.tailSpecks.AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Midground"));

            sLeaser.RemoveAllSpritesFromContainer();
            newContatiner ??= rCam.ReturnFContainer("Midground");
            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                if (i >= self.tailSpecks.startSprite && i < self.tailSpecks.startSprite + self.tailSpecks.numberOfSprites)
                {
                    rCam.ReturnFContainer("Midground").AddChild(sLeaser.sprites[i]);
                }
                else if (i == self.gownIndex)
                {
                    newContatiner = rCam.ReturnFContainer("Items");
                    newContatiner.AddChild(sLeaser.sprites[i]);
                }
                else
                {
                    if (i < 12)
                    {

                        if ((i <= 6 || i >= 9) && i <= 9)
                        {
                            newContatiner.AddChild(sLeaser.sprites[i]);
                        }
                        else
                        {
                            rCam.ReturnFContainer("Foreground").AddChild(sLeaser.sprites[i]);
                        }
                    }
                }

            }

        }



        
    }




    private static void DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);
        if (self.player.slugcatStats.name == Plugin.SlugcatStatsName && self.tailSpecks != null)
        {
            self.tailSpecks.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                if (Futile.atlasManager.DoesContainElementWithName("srs_" + sLeaser.sprites[i].element.name))
                {
                    sLeaser.sprites[i].element = Futile.atlasManager.GetElementWithName(sLeaser.sprites[i].element.name.Replace(sLeaser.sprites[i].element.name, "srs_" + sLeaser.sprites[i].element.name));
                }
            }


        }
        
    }






    private static void TailSpecks_DrawSprites(On.PlayerGraphics.TailSpeckles.orig_DrawSprites orig, PlayerGraphics.TailSpeckles self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);
        if (self.pGraphics.player.SlugCatClass == Plugin.SlugcatStatsName)
        {
            for (int i = 0; i < self.rows; i++)
            {
                for (int j = 0; j < self.lines; j++)
                {
                    sLeaser.sprites[self.startSprite + i * self.lines + j].color = spearColor;
                    sLeaser.sprites[self.startSprite + i * self.lines + j].alpha = 0.7f;

                    if (i == self.spearRow && j == self.spearLine)
                    {
                        if (ModManager.CoopAvailable && self.pGraphics.player.IsJollyPlayer)
                        {
                            sLeaser.sprites[self.startSprite + self.lines * self.rows].color = PlayerGraphics.JollyColor(self.pGraphics.player.playerState.playerNumber, 2);
                        }
                        else if (PlayerGraphics.CustomColorsEnabled())
                        {
                            sLeaser.sprites[self.startSprite + self.lines * self.rows].color = PlayerGraphics.CustomColorSafety(2);
                        }
                        else if (self.pGraphics.player.Malnourished)
                        {
                            sLeaser.sprites[self.startSprite + self.lines * self.rows].color = Color.white;
                        }
                        else
                        {
                            sLeaser.sprites[self.startSprite + self.lines * self.rows].color = spearColor;
                        }

                    }
                }
            }

            
        }
    }













}
