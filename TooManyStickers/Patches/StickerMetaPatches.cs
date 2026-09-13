using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Registers;
using System;
using System.Collections.Generic;
using System.Text;

namespace TooManyStickers.Patches
{
    [HarmonyPatch(typeof(StickerMetaStorage))]
    [HarmonyPatch("AddSticker")]
    class StickerMetaPatches
    {
        static void Postfix(ExtendedStickerData sticker)
        {
            if (GlitchStickerStateData.StickerIsValidTargetForPregeneration(sticker.sticker))
            {
                GlitchStickerData.GetOrGenerateGlitchSprite(sticker.sticker.ToStringExtended(), sticker.sprite);
            }
        }
    }
}
