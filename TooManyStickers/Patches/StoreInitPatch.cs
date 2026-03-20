using HarmonyLib;
using MTM101BaldAPI.Registers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace TooManyStickers.Patches
{
    [HarmonyPatch(typeof(StoreRoomFunction))]
    [HarmonyPatch("Initialize")]
    class StoreInitPatch
    {
        static FieldInfo _type = AccessTools.Field(typeof(ITM_StickerPack), "type");
        static void Prefix(RoomController room, ref Pickup[] ___stickerPickup)
        {
            // emulate what storeData will get set to as we need to do a check
            SceneObject storeData = Singleton<CoreGameManager>.Instance.nextLevel;
            if (!Singleton<CoreGameManager>.Instance.sceneObject.storeUsesNextLevelData)
            {
                storeData = Singleton<CoreGameManager>.Instance.sceneObject;
            }
            else
            {
                storeData = Singleton<CoreGameManager>.Instance.nextLevel;
            }
            if (storeData == null) return;
            // if there are daredevil stickers, this patch should do nothing
            if (storeData.potentialStickers.Count(x => x.selection.GetMeta().tags.Contains("tms_daredevil")) > 0) return;
            Pickup[] toRemove = ___stickerPickup.Where(x => ((StickerPackType)_type.GetValue(x.item.item)) == TooManyStickersPlugin.DaredevilStickerPack).ToArray();
            ___stickerPickup = ___stickerPickup.Where(x => ((StickerPackType)_type.GetValue(x.item.item)) != TooManyStickersPlugin.DaredevilStickerPack).ToArray();
            toRemove.Do(x => GameObject.Destroy(x.gameObject));
        }
    }
}
