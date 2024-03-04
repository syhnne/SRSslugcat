
using Fisobs.Items;
using Fisobs.Core;
using UnityEngine;
using Fisobs.Properties;
using System.Linq;
using Fisobs.Sandbox;

namespace SRSslugcat;


/// 光看名字想必不会有人想到这东西是用来防止玩家在水下暴毙的罢
/// 
/// 

internal static class OxygenMaskModules
{
    public static SLOracleBehaviorHasMark.MiscItemType OxygenMaskMisc = new("OxygenMask", false);

    public static void Apply()
    {
        On.AbstractPhysicalObject.UsesAPersistantTracker += AbstractPhysicalObject_UsesAPersistantTracker;
        On.MoreSlugcats.MSCRoomSpecificScript.AddRoomSpecificScript += MSCRoomSpecificScript_AddRoomSpecificScript;
    }


    private static bool AbstractPhysicalObject_UsesAPersistantTracker(On.AbstractPhysicalObject.orig_UsesAPersistantTracker orig, AbstractPhysicalObject abs)
    {
        if (abs is OxygenMaskAbstract)
        {
            return true;
        }
        return orig(abs);
    }




    // 上一次改这个函数忘了写orig(self)，导致我的矛大师一开局就在陆游，水猫到了五卵石那发现没有电池。
    private static void MSCRoomSpecificScript_AddRoomSpecificScript(On.MoreSlugcats.MSCRoomSpecificScript.orig_AddRoomSpecificScript orig, Room room)
    {
        string name = room.abstractRoom.name;

        if (name == "SS_AI" && room.game.IsStorySession && room.game.GetStorySession.saveState.saveStateNumber == Plugin.SlugcatStatsName)
        {
            Plugin.Log("AddRoomSpecificScript: OxygenMask");
            room.AddObject(new PlaceOxygenMask(room));
        }
        orig(room);
    }







    // 这和放电池的流程完全一致，进行一个复制粘贴
    // 那么问题来了，，我要是想防止玩家每次来到这个地方他都会刷新一个新的氧气面罩，那我是不是还得改deathpersistentdata（汗
    // 躲得过初一，躲不过十五啊，，，
    public class PlaceOxygenMask : UpdatableAndDeletable
    {
        private OxygenMask myMask;
        private int timer;
        public PlaceOxygenMask(Room room)
        {
            timer = 0;
            this.room = room;
        }



        public override void Update(bool eu)
        {

            Plugin.Log("update");
            base.Update(eu);
            Vector2 vector = new(300f, 300f);
            if (timer == 10 && room.game.session is StoryGameSession && myMask == null)
            {
                AbstractPhysicalObject abstr = new OxygenMaskAbstract(room.game.world, new WorldCoordinate(room.abstractRoom.index, -1, -1, 0), room.game.GetNewID());
                abstr.destroyOnAbstraction = true;
                this.room.abstractRoom.AddEntity(abstr);
                abstr.RealizeInRoom();
                this.myMask = (abstr.realizedObject as OxygenMask);
                this.myMask.firstChunk.pos = vector;
            }
            timer++;

            if (timer == 20)
            {
                foreach (var obj in room.physicalObjects)
                {
                    foreach (var obj2 in obj)
                    {
                        Plugin.Log("physicalObj:", obj2.GetType().Name);
                    }
                }
                Destroy();
            }
        }

        public void ReloadRooms()
        {
            for (int i = this.room.world.activeRooms.Count - 1; i >= 0; i--)
            {
                if (this.room.world.activeRooms[i] != this.room.game.cameras[0].room)
                {
                    if (this.room.game.roomRealizer != null)
                    {
                        this.room.game.roomRealizer.KillRoom(this.room.world.activeRooms[i].abstractRoom);
                    }
                    else
                    {
                        this.room.world.activeRooms[i].abstractRoom.Abstractize();
                    }
                }
            }
        }

        


    }
































    public class OxygenMask : Weapon
    {

        public float lastAirInLungs;
        public float AirInLungs;
        public int count;
        public OxygenMaskAbstract Abstr;

        public OxygenMask(OxygenMaskAbstract abstr) : base(abstr, abstr.world)
        {
            Abstr = abstr;
            lastAirInLungs = 1f;
            AirInLungs = 1f;
            base.bodyChunks = new BodyChunk[1];
            base.bodyChunks[0] = new BodyChunk(this, 0, new Vector2(0f, 0f), 5f, 0.14f);
            this.bodyChunkConnections = new PhysicalObject.BodyChunkConnection[0];
            base.airFriction = 0.999f;
            base.gravity = 0.9f;
            this.bounce = 0.4f;
            this.surfaceFriction = 0.3f;
            this.collisionLayer = 2;
            base.waterFriction = 0.98f;
            base.buoyancy = 0.6f;
            // bodyChunks[0].pos = new Vector2(300, 300);
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            
            ChangeCollisionLayer(grabbedBy.Count == 0 ? 2 : 1);
            firstChunk.collideWithTerrain = grabbedBy.Count == 0;
            firstChunk.collideWithSlopes = grabbedBy.Count == 0;
            if (base.Submersion >= 0.5f)
            {
                this.room.AddObject(new Bubble(base.firstChunk.pos, base.firstChunk.vel, false, false));
            }

            
            if (grabbedBy.Count > 0 && grabbedBy[0].grabber != null && grabbedBy[0].grabber is Player)
            {
                /*Player p = grabbedBy[0].grabber as Player;
                lastAirInLungs = AirInLungs;
                AirInLungs = p.airInLungs;
                Plugin.Log("AirInLungs:", lastAirInLungs, AirInLungs, "COUNT:", timer);
                if (lastAirInLungs > AirInLungs && timer < Abstr.lungCapacityBonus)
                {
                    p.airInLungs = lastAirInLungs;
                }*/
                if (count == Abstr.lungCapacityBonus)
                {
                    count = 0;
                }
                count++;
            }

            


        }



        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[1];
            sLeaser.sprites[0] = new FSprite("Futile_White", true);
            AddToContainer(sLeaser, rCam, null);
        }


        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Vector2 pos = Vector2.Lerp(firstChunk.lastPos, firstChunk.pos, timeStacker);
            float num = Mathf.InverseLerp(305f, 380f, timeStacker);
            pos.y -= 20f * Mathf.Pow(num, 3f);

            sLeaser.sprites[0].isVisible = true;
            sLeaser.sprites[0].scale = 1f;
            sLeaser.sprites[0].x = pos.x - camPos.x;
            sLeaser.sprites[0].y = pos.y - camPos.y;

            if (slatedForDeletetion || room != rCam.room)
            {
                sLeaser.CleanSpritesAndRemove();
            }
        }


        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            sLeaser.sprites[0].color = Color.white;
        }


        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer? newContainer)
        {
            newContainer ??= rCam.ReturnFContainer("Items");

            foreach (FSprite fsprite in sLeaser.sprites)
            {
                fsprite.RemoveFromContainer();
                newContainer.AddChild(fsprite);
            }
        }

    }










    #region Abstract

    // 我想的是，数字是几就可以提供几倍的肺活量，但实测下来发现肯定不到，所以狠狠加大力度（
    public class OxygenMaskAbstract : AbstractPhysicalObject
    {
        public int lungCapacityBonus;
        
        public OxygenMaskAbstract(World world, WorldCoordinate pos, EntityID ID) : base(world, OxygenMaskFisob.OxygenMask, null, pos, ID)
        {
            lungCapacityBonus = 3;
        }
        public override void Realize()
        {
            base.Realize();
            if (realizedObject == null)
                realizedObject = new OxygenMask(this);
        }
        public override string ToString()
        {
            return this.SaveToString($"{lungCapacityBonus}");
        }
    }

    #endregion





    #region Fisob

    public class OxygenMaskFisob : Fisob
    {
        public static readonly AbstractPhysicalObject.AbstractObjectType OxygenMask = new("OxygenMask", true);
        public OxygenMaskFisob() : base(OxygenMask)
        {

        }

        public override AbstractPhysicalObject Parse(World world, EntitySaveData saveData, SandboxUnlock? unlock)
        {
            // Centi shield data is just floats separated by ; characters.
            string[] p = saveData.CustomData.Split(';');

            if (p.Length < 1)
            {
                p = new string[1];
            }

            var result = new OxygenMaskAbstract(world, saveData.Pos, saveData.ID)
            {
                lungCapacityBonus = int.TryParse(p[0], out var b) ? b : 0,
            };

            // If this is coming from a sandbox unlock, the hue and size should depend on the data value (see CentiShieldIcon below).
            if (unlock is SandboxUnlock u)
            {
                result.lungCapacityBonus = u.Data;

            }

            return result;
        }
    }

    #endregion





    #region Icon

    public class OxygenMaskIcon : Icon
    {
        public override int Data(AbstractPhysicalObject apo)
        {
            return apo is OxygenMaskAbstract m ? (int)m.lungCapacityBonus : 0;
        }

        public override Color SpriteColor(int data)
        {
            return RWCustom.Custom.HSL2RGB(data / 1000f, 0.65f, 0.4f);
        }

        public override string SpriteName(int data)
        {
            // Fisobs autoloads the file in the mod folder named "icon_{Type}.png"
            // To use that, just remove the png suffix: "icon_CentiShield"
            return "icon_OxygenMask";
        }
    }


    #endregion





    #region Properties

    public class OxygenMaskProperties : ItemProperties
    {

        public override void Throwable(Player player, ref bool throwable)
            => throwable = false;

        // 把这个送给拾荒者还不如赶紧产几根矛（
        // 应该不会有人这么干的罢
        public override void ScavCollectScore(Scavenger scavenger, ref int score)
            => score = 2;

        public override void ScavWeaponPickupScore(Scavenger scav, ref int score)
            => score = 0;

        // Don't throw shields
        public override void ScavWeaponUseScore(Scavenger scav, ref int score)
            => score = 0;

        public override void Grabability(Player player, ref Player.ObjectGrabability grabability)
        {
            // The player can only grab one centishield at a time,
            // but that shouldn't prevent them from grabbing a spear,
            // so don't use Player.ObjectGrabability.BigOneHand

            if (player.grasps.Any(g => g?.grabbed is OxygenMask))
            {
                grabability = Player.ObjectGrabability.CantGrab;
            }
            else
            {
                grabability = Player.ObjectGrabability.OneHand;
            }
        }
    }

    #endregion



}
