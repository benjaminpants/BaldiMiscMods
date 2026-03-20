using HarmonyLib;
using MTM101BaldAPI.Registers;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlusPresents
{
    [HarmonyPatch(typeof(Pickup))]
    [HarmonyPatch("Collect")]
    class CollectPatch
    {
        internal class SoundObjectRef
        {
            public SoundObject sound;
            public ItemObject item;

            public SoundObjectRef(ItemObject item, SoundObject sound)
            {
                this.item = item;
                this.sound = sound;
            }
        }
        static void Prefix(Pickup __instance, out SoundObjectRef __state)
        {
            __state = null;
            if (__instance.item.itemType != PlusPresentsPlugin.presentItem.itemType) return;
            SoundObject presentSound = __instance.item.audPickupOverride;
            __instance.item = PlusPresentsPlugin.GetRandomItem();
            if (__instance.item == null)
            {
                UnityEngine.Debug.LogWarning("Error! Couldn't find valid present item!");
                __instance.item = ItemMetaStorage.Instance.FindByEnum(Items.InvisibilityElixir).value;
            }
            __state = new SoundObjectRef(__instance.item, __instance.item.audPickupOverride);
            __instance.item.audPickupOverride = presentSound;
        }

        static void Postfix(SoundObjectRef __state)
        {
            if (__state == null) return;
            __state.item.audPickupOverride = __state.sound;
        }
    }
}
