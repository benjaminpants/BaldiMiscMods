using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Registers;
using System;
using System.Collections.Generic;
using System.Text;

namespace TooManyStickers.Patches
{
    [HarmonyPatch]
    class StickerPatches
    {
        [HarmonyPatch(typeof(StickerMetaStorage))]
        [HarmonyPatch("AddSticker")]
        [HarmonyPostfix]
        static void Postfix(ExtendedStickerData sticker)
        {
            if (GlitchStickerStateData.StickerIsValidTargetForPregeneration(sticker.sticker))
            {
                GlitchStickerData.GetOrGenerateGlitchSprite(sticker.sticker.ToStringExtended(), sticker.sprite);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StickerManager))]
        [HarmonyPatch("ApplySticker")]
        static bool ApplyStickerPatch(StickerStateData sticker, int slot, StickerManager __instance, int[] ___appliedStickerRemainingNotebooks, StickerManager.StickerAppliedDelegate ___OnStickerApplied)
        {
            if (__instance.activeStickerData[slot].sticker == Sticker.Nothing) return true;
            if (__instance.activeStickerData[slot] is ChestStickerStateData chestData)
            {
                if (chestData.isFull)
                {
                    return true;
                }
                if (sticker.GetMeta().value.CanCoverSticker(sticker, chestData, -1, slot) == BooleanHandshake.AlwaysTrue) return true; // we must allow stickers that return AlwaysTrue to have priority, likely a gluestick
                if (!chestData.CanContainSticker(sticker).AsBool()) return false;
                for (int i = 0; i < chestData.stickerStates.Length; i++)
                {
                    if (chestData.stickerStates[i] == null)
                    {
                        chestData.stickerStates[i] = sticker.GetMeta().value.CreateOrGetAppliedStateData(sticker);
                        ___OnStickerApplied.Invoke();
                        return false;
                    }
                }
                return false;
            }
            else if (sticker is ChestStickerStateData chestAsData)
            {
                StickerStateData coveringState = __instance.activeStickerData[slot];
                if (chestAsData.isFull)
                {
                    return true;
                }
                if (!chestAsData.CanContainSticker(coveringState).AsBool()) return true;
                for (int i = 0; i < chestAsData.stickerStates.Length; i++)
                {
                    if (chestAsData.stickerStates[i] == null)
                    {
                        chestAsData.stickerStates[i] = coveringState.GetMeta().value.CreateOrGetAppliedStateData(coveringState);
                        return true;
                    }
                }
            }
            return true;
        }
    }
}
