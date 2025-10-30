using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace TooManyStickers.Patches
{
    [HarmonyPatch(typeof(StickerManager))]
    [HarmonyPatch("StickerValue")]
    class StickerValuePatch
    {
        static void Postfix(StickerManager __instance, Sticker sticker, ref int __result)
        {
            bool[] alreadyProcessed = new bool[__instance.activeStickerData.Length];
            for (int i = 0; i < __instance.activeStickerData.Length; i++)
            {
                BoostNextStickerData.CalculateBoost(i, __instance.activeStickerData, alreadyProcessed, out StickerStateData landedOn, out int addition);
                if (landedOn == null) continue;
                if (landedOn.sticker == sticker)
                {
                    __result += (addition * 2);
                }
            }
        }
    }
}
