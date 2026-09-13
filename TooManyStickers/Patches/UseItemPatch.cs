using HarmonyLib;
using MTM101BaldAPI.Reflection;
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

        static FieldInfo grapple_speed = AccessTools.Field(typeof(ITM_GrapplingHook), "speed");
        static FieldInfo grapple_initialForce = AccessTools.Field(typeof(ITM_GrapplingHook), "initialForce");
        static FieldInfo grapple_forceIncrease = AccessTools.Field(typeof(ITM_GrapplingHook), "forceIncrease");
        static FieldInfo grapple_maxPressure = AccessTools.Field(typeof(ITM_GrapplingHook), "maxPressure");

        static void ItemCreated(ItemManager manag, Item item)
        {
            if (StickerManager.Instance.StickerValue(TooManyStickersPlugin.stickerEnums["ItemSpeed"]) <= 0) return;
            float itemSpeedStickerVal = 1f + (StickerManager.Instance.StickerValue(TooManyStickersPlugin.stickerEnums["ItemSpeed"]) * 0.25f);
            ItemMetaData itemMeta = manag.items[manag.selectedItem].GetMeta();
            if (itemMeta.tags.Contains("tms_itemspeed_manualeffect"))
            {
                MethodInfo info = AccessTools.Method(item.GetType(), "ItemSpeedStickerEffect", new Type[] { typeof(float) });
                if (info == null) return;
                info.Invoke(item, new object[] { itemSpeedStickerVal });
                return;
            }
            if (item is ITM_GrapplingHook grappleItem)
            {
                grapple_speed.SetValue(grappleItem, (float)grapple_speed.GetValue(grappleItem) * itemSpeedStickerVal);
                grapple_initialForce.SetValue(grappleItem, (float)grapple_initialForce.GetValue(grappleItem) * itemSpeedStickerVal);
                grapple_forceIncrease.SetValue(grappleItem, (float)grapple_forceIncrease.GetValue(grappleItem) * itemSpeedStickerVal);
                grapple_maxPressure.SetValue(grappleItem, (float)grapple_maxPressure.GetValue(grappleItem) * itemSpeedStickerVal);
                return;
            }
            // only do anything if this Item uses an entity, as otherwise "speed" could mean many things
            if (item.TryGetComponent<Entity>(out Entity ent))
            {
                if (!itemMeta.tags.Contains("tms_itemspeed_novarmanip"))
                {
                    FieldInfo fInfo = AccessTools.FindIncludingBaseTypes(item.GetType(), (Type t) => t.GetField("speed", AccessTools.all)); // using this instead of AccessTools.Field because that prints an unnecessary warning into the console.
                    //AccessTools.Field(__instance.GetType(), "speed");
                    if (fInfo != null)
                    {
                        if (fInfo.FieldType == typeof(int))
                        {
                            fInfo.SetValue(item, Mathf.CeilToInt(((int)fInfo.GetValue(item)) * itemSpeedStickerVal));
                            return;
                        }
                        else if (fInfo.FieldType == typeof(float))
                        {
                            fInfo.SetValue(item, (float)fInfo.GetValue(item) * itemSpeedStickerVal);
                            return;
                        }
                        else if (fInfo.FieldType == typeof(double))
                        {
                            fInfo.SetValue(item, (double)fInfo.GetValue(item) * itemSpeedStickerVal);
                            return;
                        }
                    }
                }
                ent.ExternalActivity.moveMods.Add(new MovementModifier(Vector3.zero, itemSpeedStickerVal));
            }
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
        static MethodInfo _ItemCreated = AccessTools.Method(typeof(UseItemPatch), "ItemCreated");

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
            bool didPatchTwo = false;
            CodeInstruction[] codeInstructions = instructions.ToArray();
            for (int i = 0; i < codeInstructions.Length; i++)
            {
                CodeInstruction instruction = codeInstructions[i];
                yield return instruction;
                if (!didPatchOne && (instruction.opcode == OpCodes.Call) && (((MethodInfo)instruction.operand) == AccessTools.Method(typeof(ItemManager), "RemoveItem")))
                {
                    didPatchOne = true;
                    yield return new CodeInstruction(OpCodes.Ldarg_0); //this
                    yield return new CodeInstruction(OpCodes.Call, _OnItemActuallyUsed); //UseItemPatch.OnItemActuallyUsed
                }
                if (i - 3 < 0) continue;
                if (didPatchTwo) continue;
                if ((instruction.opcode == OpCodes.Stloc_0) 
                    && (codeInstructions[i - 1].opcode == OpCodes.Call && ((MethodInfo)codeInstructions[i - 1].operand).Name == "Instantiate")
                    && (codeInstructions[i - 2].opcode == OpCodes.Ldfld && ((FieldInfo)codeInstructions[i - 2].operand) == AccessTools.Field(typeof(ItemObject), "item")))
                {
                    didPatchTwo = true;
                    yield return new CodeInstruction(OpCodes.Ldarg_0); // this
                    yield return new CodeInstruction(OpCodes.Ldloc_0); // item (local variable)
                    yield return new CodeInstruction(OpCodes.Call, _ItemCreated); // UseItemPatch.ItemCreated
                }
            }
            if (!didPatchOne) throw new Exception("Unable to patch one!");
            if (!didPatchTwo) throw new Exception("Unable to patch two!");
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