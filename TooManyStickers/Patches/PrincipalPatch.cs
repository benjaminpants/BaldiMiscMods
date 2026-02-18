using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TooManyStickers.Patches
{
    [HarmonyPatch(typeof(Principal))]
    [HarmonyPatch("SendToDetention")]
    class PrincipalPatch
    {
        static void Prefix(Principal __instance, int ___detentionLevel, out int? __state)
        {
            __state = null;
            int favoritismValue = Singleton<StickerManager>.Instance.StickerValue(TooManyStickersPlugin.stickerEnums["Favoritism"]);
            if (favoritismValue <= 0) return;
            PrincipalDelayCounter counter;
            if (!__instance.TryGetComponent(out counter))
            {
                counter = __instance.gameObject.AddComponent<PrincipalDelayCounter>();
            }
            if (counter.delaysDone < favoritismValue)
            {
                counter.delaysDone++;
                __state = ___detentionLevel;
            }
            else
            {
                counter.delaysDone = 0;
            }
        }

        static void Postfix(ref int ___detentionLevel, int? __state)
        {
            if (__state == null) return;
            ___detentionLevel = __state.Value;
        }
    }

    public class PrincipalDelayCounter : MonoBehaviour
    {
        public int delaysDone = 0;
    }
}
