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
                Sticker landedOn = Sticker.Nothing;
                int addition = 0;
                int offset = 0;
                int attempts = 0;
                while (!alreadyProcessed[(i + offset) % __instance.activeStickerData.Length] && __instance.activeStickerData[(i + offset) % __instance.activeStickerData.Length].sticker == TooManyStickersPlugin.stickerEnums["BoostNext"])
                {
                    if (attempts > __instance.activeStickerData.Length) break; // give up
                    alreadyProcessed[(i + offset) % __instance.activeStickerData.Length] = true;
                    offset++;
                    addition++;
                    landedOn = __instance.activeStickerData[(i + offset) % __instance.activeStickerData.Length].sticker;
                }
                if (landedOn == sticker)
                {
                    __result += (addition * 2);
                }
            }
        }
    }
}
