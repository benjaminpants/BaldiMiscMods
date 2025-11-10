using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TooManyStickers.Patches
{
    [HarmonyPatch(typeof(CoreGameManager))]
    [HarmonyPatch("AddPoints")]
    [HarmonyPatch(new Type[] { typeof(int), typeof(int), typeof(bool), typeof(bool), typeof(bool)})]
    class AddPointsPatch
    {
        static void Postfix(CoreGameManager __instance, int points, int player, bool playAnimation, bool includeInLevelTotal, bool multiply)
        {
            if (points <= 0) return; // dont bother
            if (!includeInLevelTotal) return;
            if (!playAnimation) return;
            int stickerValue = Singleton<StickerManager>.Instance.StickerValue(TooManyStickersPlugin.stickerEnums["PointInvisibility"]);
            if (stickerValue <= 0) return;
            if (multiply)
            {
                points = Mathf.RoundToInt((float)points * __instance.YtpMultiplier);
            }
            __instance.GetPlayer(player).gameObject.AddComponent<YTPInvis>().Initialize(__instance.GetPlayer(player), (points / 15f) * stickerValue);
        }
    }
}

namespace TooManyStickers
{
    public class YTPInvis : MonoBehaviour
    {
        PlayerManager pm;
        HudGauge gauge;

        float remainingTime = 0f;
        float maxTime = 0f;

        bool initialized = false;

        public void Initialize(PlayerManager pm, float time)
        {
            this.pm = pm;
            gauge = Singleton<CoreGameManager>.Instance.GetHud(pm.playerNumber).gaugeManager.ActivateNewGauge(TooManyStickersPlugin.Instance.assetMan.Get<Sprite>("YTPInvisIcon"), time);
            remainingTime = time;
            maxTime = time;
            pm.SetHidden(true);
            initialized = true;
        }

        void Update()
        {
            if (!initialized) return;
            remainingTime -= Time.deltaTime * pm.ec.PlayerTimeScale;
            gauge.SetValue(maxTime, remainingTime);
            if (remainingTime <= 0f)
            {
                pm.SetHidden(false);
                gauge.Deactivate();
                Destroy(this);
            }
        }
    }
}
