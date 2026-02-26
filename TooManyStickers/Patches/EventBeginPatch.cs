using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TooManyStickers.Patches
{
    [HarmonyPatch(typeof(RandomEvent))]
    [HarmonyPatch("Begin")]
    class EventBeginPatch
    {
        static void Prefix(RandomEvent __instance, ref float ___eventTime)
        {
            int shorterEvents = Singleton<StickerManager>.Instance.StickerValue(TooManyStickersPlugin.stickerEnums["ShorterEvents"]);
            if (shorterEvents <= 0) return;
            ___eventTime -= ___eventTime * Mathf.Min((0.2f * shorterEvents),0.95f);
        }
    }
}
