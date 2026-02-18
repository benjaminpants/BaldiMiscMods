using HarmonyLib;
using MTM101BaldAPI.Registers;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TooManyStickers.Patches
{
    [HarmonyPatch(typeof(NPC))]
    [HarmonyPatch("Initialize")]
    class NPCInitializePatch
    {
        static void Postfix(NPC __instance)
        {
            NPCMetadata meta = __instance.GetMeta();
            if (meta == null) return;
            if (meta.tags.Contains("tms_iceeyes_immune")) return;
            __instance.gameObject.AddComponent<IceEyesManager>().Begin(__instance);
        }
    }

    [HarmonyPatch(typeof(NPC))]
    [HarmonyPatch("Sighted")]
    class NPCSightedPatch
    {
        static void Prefix(NPC __instance)
        {
            if (__instance.TryGetComponent<IceEyesManager>(out IceEyesManager m))
            {
                m.Sighted();
            }
        }
    }

    [HarmonyPatch(typeof(NPC))]
    [HarmonyPatch("Unsighted")]
    class NPCUnsightedPatch
    {
        static void Prefix(NPC __instance)
        {
            if (__instance.TryGetComponent<IceEyesManager>(out IceEyesManager m))
            {
                m.Unsighted();
            }
        }
    }
}

namespace TooManyStickers
{
    public class IceEyesManager : MonoBehaviour
    {
        public float mult = 0f;
        public MovementModifier moveMod;
        public bool beingObserved = false;
        public NPC npc;
        public void Sighted()
        {
            beingObserved = true;
        }

        public void Unsighted()
        {
            beingObserved = false;
        }

        public void Begin(NPC npc)
        {
            this.npc = npc;
            moveMod = new MovementModifier(Vector3.zero, 1f);
            npc.Entity.ExternalActivity.moveMods.Add(moveMod);
        }

        void Update()
        {
            // mult the time by two so it takes two seconds for an NPC to fully incubate
            mult = Mathf.Clamp(mult + ((Time.deltaTime * npc.ec.PlayerTimeScale * 0.5f) * (beingObserved ? 1f : -1f)), 0f, 1f);
            float maxMult = (Singleton<StickerManager>.Instance.StickerValue(TooManyStickersPlugin.stickerEnums["IceEyes"]) * 0.08f);
            moveMod.movementMultiplier = Mathf.Max(1f - (maxMult * mult),0.1f);
        }

    }
}
