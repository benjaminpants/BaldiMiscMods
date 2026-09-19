using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace TooManyStickers.Patches
{
    [HarmonyPatch(typeof(StickerManager))]
    [HarmonyPatch("StickerValue")]
    [HarmonyPriority(Priority.High)]
    class StickerValuePatch
    {
        static void Postfix(StickerManager __instance, Sticker sticker, ref int __result)
        {
            
            bool[] alreadyProcessed = new bool[__instance.activeStickerData.Length];
            for (int i = 0; i < __instance.activeStickerData.Length; i++)
            {
                if (__instance.activeStickerData[i] is GlitchStickerStateData glitchAt)
                {
                    if (glitchAt.stickerMimicing == sticker)
                    {
                        __result++;
                    }
                }
                if (__instance.activeStickerData[i] is ChestStickerStateData chestAt)
                {
                    for (int j = 0; j < chestAt.stickerStates.Length; j++)
                    {
                        if (chestAt.stickerStates[j] == null) continue;
                        if (chestAt.stickerStates[j].sticker == sticker)
                        {
                            __result++;
                        }
                    }
                }
                BoostNextStickerData.CalculateBoost(i, __instance.activeStickerData, alreadyProcessed, out StickerStateData landedOn, out int addition);
                if (landedOn == null) continue;
                Sticker landedOnSticker = landedOn.sticker;
                if (landedOn is GlitchStickerStateData glitchLandedOn)
                {
                    landedOnSticker = glitchLandedOn.stickerMimicing;
                }
                if (landedOn is ChestStickerStateData chestLandedOn)
                {
                    for (int j = 0; j < chestLandedOn.stickerStates.Length; j++)
                    {
                        if (chestLandedOn.stickerStates[j] == null) continue;
                        if (chestLandedOn.stickerStates[j].sticker == sticker)
                        {
                            __result += (addition * 2);
                        }
                    }
                    return;
                }
                if (landedOnSticker == sticker)
                {
                    __result += (addition * 2);
                }
            }
        }
    }
}
