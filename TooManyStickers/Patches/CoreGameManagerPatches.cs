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

    [HarmonyPatch(typeof(CoreGameManager))]
    [HarmonyPatch("GetStickerBonuses")]
    class GetStickerBonusesPatch
    {
        static void Postfix(CoreGameManager __instance, ref int __result)
        {
            __result -= __instance.GetPlayer(0).itm.GetComponent<ItemUseTracker>().itemsUsed * (100 * Singleton<StickerManager>.Instance.StickerValue(TooManyStickersPlugin.stickerEnums["Daredevil_ItemUseAntiBonus"]));
            PizzaCounter counter = __instance.GetPlayer(0).GetComponent<PizzaCounter>();
            if (counter != null)
            {
                __result += counter.pizzas * 100;
            }

            TMSEcTracker tracker = Singleton<BaseGameManager>.Instance.Ec.GetComponent<TMSEcTracker>();
            int sightlessBonusMult = Singleton<StickerManager>.Instance.StickerValue(TooManyStickersPlugin.stickerEnums["SightlessBonus"]);
            if (sightlessBonusMult != 0)
            {
                __result += Mathf.Max(1000 - Mathf.FloorToInt((tracker.secondsSeenByBaldi * 15)), 0) * sightlessBonusMult;
            }
        }
    }

    [HarmonyPatch(typeof(CoreGameManager))]
    [HarmonyPatch("UpdateLighting")]
    class UpdateLightingPatch
    {
        static void Prefix(CoreGameManager __instance, ref Color color)
        {
            float maxHideableLevel = 0.08f;
            if (__instance.GetPlayer(0) != null)
            {
                maxHideableLevel = __instance.GetPlayer(0).MaxHideableLightLevel;
            }
            float clearVisionVal = Singleton<StickerManager>.Instance.StickerValue(TooManyStickersPlugin.stickerEnums["ClearVision"]) * 0.08f;
            if (clearVisionVal > 0)
            {
                Color toAdd = Color.Lerp(Mathf.Max(color.r, color.g, color.b) <= maxHideableLevel ? Color.yellow : Color.green, Color.white, 0.15f);
                toAdd *= clearVisionVal;
                color += toAdd;
            }
        }
    }
}
