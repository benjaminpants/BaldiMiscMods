using HarmonyLib;
using MTM101BaldAPI.Registers;
using System;
using System.Collections.Generic;
using System.Text;

namespace TooManyStickers.Patches
{
    [HarmonyPatch(typeof(StickerManager))]
    [HarmonyPatch("ClearAppliedStickers")]
    class ClearAppliedStickersPatch
    {
        // daredevil stickers should always be removed!
        static void Postfix(StickerManager __instance)
        {
            for (int i = 0; i < __instance.activeStickerData.Length; i++)
            {
                if (__instance.activeStickerData[i].GetMeta().tags.Contains("tms_daredevil"))
                {
                    __instance.activeStickerData[i] = new StickerStateData(Sticker.Nothing, 0, true, false);
                }
            }    
        }
    }

    [HarmonyPatch(typeof(StickerManager))]
    [HarmonyPatch("AdvanceStickerUsage")]
    class AdvanceStickerUsagePatch
    {
        // daredevil stickers should always be removed!
        static void Postfix(StickerManager __instance, int value)
        {
            for (int i = 0; i < __instance.appliedStickerRemainingNotebooks.Length; i++)
            {
                if (__instance.SlotUpgraded(i) && (__instance.activeStickerData[i].GetMeta().tags.Contains("tms_daredevil")))
                {
                    __instance.appliedStickerRemainingNotebooks[i] -= value;
                    if (__instance.appliedStickerRemainingNotebooks[i] <= 0)
                    {
                        __instance.ApplySticker(new StickerStateData(Sticker.Nothing, 0, true, false), i);
                        __instance.appliedStickerRemainingNotebooks[i] = 0;
                    }
                }
            }
        }
    }
}
