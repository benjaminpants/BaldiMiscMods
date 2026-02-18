using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TooManyStickers.Patches
{
    [HarmonyPatch(typeof(ActivityModifier))]
    [HarmonyPatch("Multiplier", MethodType.Getter)]
    class ActivityModifierMultiplierPatch
    {
        static void Postfix(ActivityModifier __instance, Entity ___entity, ref float __result)
        {
            if (!(___entity is PlayerEntity)) return;
            float negativeReduction = 1f - __result;
            if (negativeReduction <= 0f) return; // nothing to reduce
            if (negativeReduction == 1f) return; // probably shouldn't allow the player to move during times where they shouldn't.
            __result = 1f - (negativeReduction * (1f - (Singleton<StickerManager>.Instance.StickerValue(TooManyStickersPlugin.stickerEnums["MoveResist"]) * 0.1f)));
        }
    }

    [HarmonyPatch(typeof(ActivityModifier))]
    [HarmonyPatch("Addend", MethodType.Getter)]
    class ActivityModifierAddendPatch
    {
        static void Postfix(ActivityModifier __instance, Entity ___entity, ref Vector3 __result)
        {
            if (!(___entity is PlayerEntity)) return;
            __result *= (1f - (Singleton<StickerManager>.Instance.StickerValue(TooManyStickersPlugin.stickerEnums["MoveResist"]) * 0.1f));
        }
    }
}
