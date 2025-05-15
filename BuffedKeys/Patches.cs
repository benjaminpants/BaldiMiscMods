using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace BuffedKeys
{
    [HarmonyPatch(typeof(GameLock))]
    [HarmonyPatch("ItemFits")]
    static class GameLockPatch
    {
        static bool Prefix(Items item, ref bool __result)
        {
            if (item == Items.DetentionKey)
            {
                __result = true;
                return false;
            }
            return true;
        }
    }
}
