using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TooManyStickers.Patches
{

    public class TooManyStickersPlayerSpeedManager : MonoBehaviour
    {
        public PlayerManager pm;
        public MovementModifier myMoveMod;

        void Update()
        {
            if (Singleton<StickerManager>.Instance == null) return;
            if (pm == null) return;
            if (myMoveMod == null)
            {
                myMoveMod = new MovementModifier(Vector3.zero, 1f);
                pm.plm.Entity.ExternalActivity.moveMods.Add(myMoveMod);
            }
            myMoveMod.movementMultiplier = 1f;
            if (pm.plm.Entity.Hidden)
            {
                myMoveMod.movementMultiplier += (Singleton<StickerManager>.Instance.StickerValue(TooManyStickersPlugin.stickerEnums["StealthSpeed"]) * 0.2f);
            }
        }
    }

    [HarmonyPatch(typeof(PlayerMovement))]
    [HarmonyPatch("Start")]
    class PlayerMovementPatch
    {
        static void Prefix(PlayerMovement __instance, Entity ___entity)
        {
            __instance.gameObject.AddComponent<TooManyStickersPlayerSpeedManager>().pm = __instance.pm;
        }
    }

    [HarmonyPatch(typeof(PlayerMovement))]
    [HarmonyPatch("StaminaMax", MethodType.Getter)]
    class StaminaMaxPatch
    {
        static void Postfix(PlayerMovement __instance, ref float __result)
        {
            __result -= Mathf.Min((__instance.staminaMax * (Singleton<StickerManager>.Instance.StickerValue(TooManyStickersPlugin.stickerEnums["Daredevil_LessStamina"])) * 0.30f), __result - 1f);
        }
    }
}
