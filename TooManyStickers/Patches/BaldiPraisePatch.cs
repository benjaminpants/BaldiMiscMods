using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TooManyStickers.Patches
{
    [HarmonyPatch(typeof(Baldi))]
    [HarmonyPatch("Praise")]
    class BaldiPraisePatch
    {
        static bool Prefix(Baldi __instance, float time, bool rewardSticker)
        {
            int count = Singleton<StickerManager>.Instance.StickerValue(TooManyStickersPlugin.stickerEnums["PraiseTimeSlow"]);
            if (count <= 0) return true;
            float num = 0f;
            if (rewardSticker)
            {
                num = (float)(3 * Singleton<StickerManager>.Instance.StickerValue(Sticker.BaldiPraise));
            }

            BaldiPraiseTimeTracker comp;
            if (!__instance.ec.TryGetComponent<BaldiPraiseTimeTracker>(out comp))
            {
                comp = __instance.gameObject.AddComponent<BaldiPraiseTimeTracker>();
                comp.StartUp(__instance.ec, time + num, Mathf.Pow(0.5f, count));
            }
            comp.timeRemaining = time + num;
            return false;
        }
    }

    public class BaldiPraiseTimeTracker : MonoBehaviour
    {
        public EnvironmentController env;
        public float timeRemaining;
        TimeScaleModifier timeMod;

        public void StartUp(EnvironmentController ec, float time, float timeScale)
        {
            env = ec;
            timeRemaining = time;
            timeMod = new TimeScaleModifier();
            timeMod.npcTimeScale = timeScale;
            timeMod.playerTimeScale = 1f;
            timeMod.environmentTimeScale = timeScale;
            env.AddTimeScale(timeMod);
        }

        void Update()
        {
            if (env == null) return;
            timeRemaining -= Time.deltaTime;
            if (timeRemaining <= 0)
            {
                env.RemoveTimeScale(timeMod);
                Destroy(this);
            }
        }

        void OnDestroy()
        {
            if (env == null) return;
            env.RemoveTimeScale(timeMod);
        }
    }
}
