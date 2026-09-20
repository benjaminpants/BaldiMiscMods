using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PlaytimeRebalance
{
    [HarmonyPatch(typeof(Playtime))]
    [HarmonyPatch("EndJumprope")]
    class PlaytimeWonPatch
    {
        static void Postfix(Playtime __instance, bool won)
        {
            if (!won) return;
            __instance.behaviorStateMachine.ChangeState(new Playtime_ChaseNPCWanderState(__instance, __instance, new PlaytimeChaseData(60f)));
        }
    }

    [HarmonyPatch(typeof(Jumprope))]
    [HarmonyPatch("Start")]
    class JumpropeDecrementPatch
    {
        static void Prefix(Jumprope __instance, ref int ___maxJumps, ref int ___startVal)
        {
            ___maxJumps = Mathf.Max(___maxJumps - 1, 1);
            ___startVal = Mathf.Min(___startVal, 10);
        }
    }
}
