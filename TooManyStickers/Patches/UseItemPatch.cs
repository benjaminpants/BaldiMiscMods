using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace TooManyStickers.Patches
{
    [HarmonyPatch(typeof(ItemManager))]
    [HarmonyPatch("UseItem")]
    class UseItemPatch
    {
        static ItemObject toPreserve = null;
        static bool handledAlready = false;

        static void PreserveItemPotentially(ItemManager manager)
        {
            if (handledAlready)
            {
                return;
            }
            if (toPreserve == null) return;
            handledAlready = true;
            // to preserve, or not to preserve, that is the question
            float preserveChance = Singleton<StickerManager>.Instance.StickerValue(TooManyStickersPlugin.stickerEnums["PreserveItem"]) * 0.08f;
            if (UnityEngine.Random.Range(0f, 1f) <= preserveChance)
            {
                manager.SetItem(toPreserve, manager.selectedItem);
            }
        }

        static MethodInfo _preserve = AccessTools.Method(typeof(UseItemPatch), "PreserveItemPotentially");

        [HarmonyPriority(Priority.VeryLow)]
        static void Prefix(ItemManager __instance)
        {
            handledAlready = false;
            toPreserve = __instance.items[__instance.selectedItem];
        }

        static void Postfix(ItemManager __instance, bool ___disabled)
        {
            if (!(!___disabled || (__instance.items[__instance.selectedItem].overrideDisabled && __instance.maxItem >= 0))) return;
            PreserveItemPotentially(__instance);
            toPreserve = null;
            handledAlready = false;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool didPatchOne = false;
            CodeInstruction[] codeInstructions = instructions.ToArray();
            for (int i = 0; i < codeInstructions.Length; i++)
            {
                CodeInstruction instruction = codeInstructions[i];
                yield return instruction;
                if ((instruction.opcode == OpCodes.Call) && (((MethodInfo)instruction.operand) == AccessTools.Method(typeof(ItemManager), "RemoveItem")))
                {
                    didPatchOne = true;
                    yield return new CodeInstruction(OpCodes.Ldarg_0); //this
                    yield return new CodeInstruction(OpCodes.Call, _preserve); //UseItemPatch.PreserveItemPotentially
                }
            }
            if (!didPatchOne) throw new Exception("Unable to patch one!");
            yield break;
        }
    }
}
