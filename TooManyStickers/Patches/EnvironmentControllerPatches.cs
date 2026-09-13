using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace TooManyStickers.Patches
{
    [HarmonyPatch(typeof(EnvironmentController))]
    [HarmonyPatch("Update")]
    class EnvironmentControllerUpdatePatch
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

    [HarmonyPatch(typeof(EnvironmentController))]
    [HarmonyPatch("Start")]
    class EControllerStartPatch
    {
        static void Prefix(EnvironmentController __instance)
        {
            __instance.gameObject.AddComponent<TMSEcTracker>();
        }
    }

    [HarmonyPatch(typeof(EnvironmentController))]
    [HarmonyPatch("UpdateFog")]
    class EControllerUpdateFogPatch
    {
        static void Postfix()
        {
            float clearVisionOff = Singleton<StickerManager>.Instance.StickerValue(TooManyStickersPlugin.stickerEnums["ClearVision"]) * 20f;
            if (clearVisionOff <= 0f) return;
            if (Shader.GetGlobalInt("_FogActive") == 0) return;
            Shader.SetGlobalFloat("_FogStartDistance", Shader.GetGlobalFloat("_FogStartDistance") + clearVisionOff);
            Shader.SetGlobalFloat("_FogMaxDistance", Shader.GetGlobalFloat("_FogMaxDistance") + clearVisionOff);
        }
    }
}

namespace TooManyStickers
{
    public class TMSEcTracker : MonoBehaviour
    {
        public static TMSEcTracker Instance;
        public EnvironmentController ec;
        static FieldInfo _lightMap = AccessTools.Field(typeof(EnvironmentController), "lightMap");
        static MethodInfo _SetSpriteVisibility = AccessTools.Method(typeof(Entity), "SetSpriteVisibility");

        void Awake()
        {
            Instance = this;
            ec = GetComponent<EnvironmentController>();
            clearVisionLastLevel = Singleton<StickerManager>.Instance.StickerValue(TooManyStickersPlugin.stickerEnums["ClearVision"]);
            Singleton<StickerManager>.Instance.OnStickerApplied += RefreshClearVision;
        }

        void OnDestroy()
        {

        }

        void ForceLightmapRefresh()
        {
            LightingController[,] lightMap = (LightingController[,])_lightMap.GetValue(ec);
            for (int x = 0; x < lightMap.GetLength(0); x++)
            {
                for (int y = 0; y < lightMap.GetLength(0); y++)
                {
                    Singleton<CoreGameManager>.Instance.UpdateLighting(lightMap[x,y].Color, lightMap[x, y].position);
                }
            }
            Singleton<CoreGameManager>.Instance.UpdateLightMap();
        }

        void RefreshClearVision()
        {
            int clearVision = Singleton<StickerManager>.Instance.StickerValue(TooManyStickersPlugin.stickerEnums["ClearVision"]);

            if (clearVision != clearVisionLastLevel)
            {
                if (clearVision == 0 || clearVisionLastLevel == 0)
                {
                    foreach (var item in Entity.allEntities)
                    {
                        _SetSpriteVisibility.Invoke(item, null);
                    }
                }
                ForceLightmapRefresh();
                ec.UpdateFog();
            }
            clearVisionLastLevel = clearVision;
        }

        public int clearVisionLastLevel = 0;
        public float secondsSeenByBaldi = 0f;
    }
}
