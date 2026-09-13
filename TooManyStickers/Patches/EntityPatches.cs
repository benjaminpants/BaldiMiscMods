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
                if (____propertyBlock == null)
                {
                    ____propertyBlock = new MaterialPropertyBlock();
                }
                foreach (Renderer renderer in ___renderer)
                {
                    renderer.GetPropertyBlock(____propertyBlock);
                    ____propertyBlock.SetFloat("_PercentInvisible", 0f);
                    renderer.SetPropertyBlock(____propertyBlock);
                }
            }
        }
    }
}
