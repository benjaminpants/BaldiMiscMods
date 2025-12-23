using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TooManyStickers.Patches
{
    [HarmonyPatch(typeof(EnvironmentController))]
    [HarmonyPatch("Update")]
    class EnvironmentControllerPatches
    {
        static Fog fog = new Fog()
        {
            color = new Color(0f, 0f, 0f, 1f),
            maxDist = 80f,
            priority = 16,
            strength = 1,
            startDist = 1f
        };
        static void Postfix(EnvironmentController __instance)
        {
            int lowVision = Singleton<StickerManager>.Instance.StickerValue(TooManyStickersPlugin.stickerEnums["Daredevil_LowVision"]);
            if (lowVision > 0)
            {
                float calculatedDist = Mathf.Max(80f - (lowVision * 15f),5f);
                __instance.AddFog(fog);
                if (fog.maxDist != calculatedDist)
                {
                    fog.maxDist = calculatedDist;
                    fog.startDist = Mathf.Max(calculatedDist - 10f,0);
                    __instance.UpdateFog();
                }
            }
            else
            {
                __instance.RemoveFog(fog);
            }
        }
    }
}
