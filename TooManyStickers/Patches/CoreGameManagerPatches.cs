using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TooManyStickers.Patches
{
    [HarmonyPatch(typeof(CoreGameManager))]
    [HarmonyPatch("YtpMultiplier", MethodType.Getter)]
    class YtpMultiplierPatch
    {
        static void Postfix(CoreGameManager __instance, ref float __result)
        {
            __result *= 1f - Mathf.Min(((float)Singleton<StickerManager>.Instance.StickerValue(TooManyStickersPlugin.stickerEnums["Daredevil_Divide"]) * 0.30f), 0.98f);
        }
    }
}
