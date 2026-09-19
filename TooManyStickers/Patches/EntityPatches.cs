using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TooManyStickers.Patches
{
    [HarmonyPatch(typeof(Entity))]
    [HarmonyPatch("SetSpriteVisibility")]
    class EntitySpriteVisPatch
    {
        static void Postfix(Entity __instance, MaterialPropertyBlock ____propertyBlock, Renderer[] ___renderer)
        {
            if (StickerManager.Instance.StickerValue(TooManyStickersPlugin.stickerEnums["ClearVision"]) > 0)
            {
                float clearVisionValue = Mathf.Max(1f - StickerManager.Instance.StickerValue(TooManyStickersPlugin.stickerEnums["ClearVision"]) * 0.25f, 0f);
                if (____propertyBlock == null)
                {
                    ____propertyBlock = new MaterialPropertyBlock();
                }
                foreach (Renderer renderer in ___renderer)
                {
                    renderer.GetPropertyBlock(____propertyBlock);
                    ____propertyBlock.SetFloat("_PercentInvisible", Mathf.Min(____propertyBlock.GetFloat("_PercentInvisible"), clearVisionValue));
                    renderer.SetPropertyBlock(____propertyBlock);
                }
            }
        }
    }
}
