using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TooManyStickers.Patches
{

    public class PlayerStealthSpeedManager : MonoBehaviour
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
            if (pm.plm.Entity.Hidden)
            {
                myMoveMod.movementMultiplier = 1f + (Singleton<StickerManager>.Instance.StickerValue(TooManyStickersPlugin.stickerEnums["StealthSpeed"]) * 0.2f);
            }
            else
            {
                myMoveMod.movementMultiplier = 1f;
            }
        }
    }

    [HarmonyPatch(typeof(PlayerMovement))]
    [HarmonyPatch("Start")]
    class PlayerMovementPatch
    {
        static void Prefix(PlayerMovement __instance, Entity ___entity)
        {
            __instance.gameObject.AddComponent<PlayerStealthSpeedManager>().pm = __instance.pm;
        }
    }
}
