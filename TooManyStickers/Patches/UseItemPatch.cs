using HarmonyLib;
using MTM101BaldAPI.Registers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;

namespace TooManyStickers.Patches
{

    [HarmonyPatch(typeof(ItemManager))]
    [HarmonyPatch("Awake")]
    class ItemManagerAwakeClass
    {
        static void Postfix(ItemManager __instance)
        {
            __instance.gameObject.AddComponent<ItemUseTracker>();
        }
    }

    [HarmonyPatch(typeof(ItemManager))]
    [HarmonyPatch("UseItem")]
    class UseItemPatch
    {
        static ItemObject toPreserve = null;
        static bool handledAlready = false;

        static void OnItemActuallyUsed(ItemManager manager)
        {
            PreserveItemPotentially(manager);
        }

        static void PreserveItemPotentially(ItemManager manager)
        {
            if (handledAlready)
            {
                return;
            }
            if (toPreserve == null) return;
            // right here is the best way to check if the item changed and assume a usage
            if (manager.items[manager.selectedItem] != toPreserve)
            {
                manager.GetComponent<ItemUseTracker>().itemsUsed++;
            }
            handledAlready = true;
            // to preserve, or not to preserve, that is the question
            float preserveChance = Singleton<StickerManager>.Instance.StickerValue(TooManyStickersPlugin.stickerEnums["PreserveItem"]) * 0.08f;
            if (UnityEngine.Random.Range(0f, 1f) <= preserveChance)
            {
                manager.SetItem(toPreserve, manager.selectedItem);
                return; // the item has been preserved, do nothing
            }
            if (manager.items[manager.selectedItem].itemType == Items.None)
            {
                float quarterChance = Singleton<StickerManager>.Instance.StickerValue(TooManyStickersPlugin.stickerEnums["QuarterChance"]) * 0.1f;
                if (UnityEngine.Random.Range(0f, 1f) <= quarterChance)
                {
                    manager.SetItem(ItemMetaStorage.Instance.FindByEnum(Items.Quarter).value, manager.selectedItem);
                    return; // the item has been quarterified, do nothing
                }
            }
        }

        static MethodInfo _OnItemActuallyUsed = AccessTools.Method(typeof(UseItemPatch), "OnItemActuallyUsed");

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
                    yield return new CodeInstruction(OpCodes.Call, _OnItemActuallyUsed); //UseItemPatch.OnItemActuallyUsed
                }
            }
            if (!didPatchOne) throw new Exception("Unable to patch one!");
            yield break;
        }
    }
}

namespace TooManyStickers
{
    public class ItemUseTracker : MonoBehaviour
    {
        public int itemsUsed = 0;
    }
}