using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TooManyStickers.Patches
{
    /*
    [HarmonyPatch(typeof(PlayerEntity))]
    [HarmonyPatch("VirtualUpdate")]
    class PlayerEntityUpdatePatch
    {
        static void Prefix(PlayerEntity __instance, EnvironmentController ___environmentController, bool ___squished, ref float ___squishTime)
        {
            if (___squished)
            {
                ___squishTime -= (Time.deltaTime * ___environmentController.EnvironmentTimeScale) * (((float)Singleton<StickerManager>.Instance.StickerValue(TooManyStickersPlugin.stickerEnums["SquishReduce"])) * 0.5f);
                if (___squishTime <= 0f)
                {
                    __instance.Unsquish();
                }
            }
        }
    }*/


    [HarmonyPatch(typeof(PlayerEntity))]
    [HarmonyPatch("Squish")]
    class PlayerEntitySquishPatch
    {
        static void Prefix(PlayerEntity __instance, ref float time)
        {
            time -= time * (((float)Singleton<StickerManager>.Instance.StickerValue(TooManyStickersPlugin.stickerEnums["SquishReduce"])) * 0.2f);
            time = Mathf.Max(time, 1f);
        }
    }
}
