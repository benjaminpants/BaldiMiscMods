using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Registers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace TooManyStickers.Patches
{
    [HarmonyPatch(typeof(StickerManager))]
    [HarmonyPatch("GiveRandomStickers")]
    [HarmonyPriority(Priority.High)]
    class GiveRandomStickersPatch
    {
        static bool Prefix(StickerManager __instance, ref WeightedSticker[] potentialStickers, StickerPackType packType, int amount, bool openNow)
        {
            // if we are handling the daredevil sticker pack, pass in potentialStickers as normal into GiveDaredevilStickers
            if (packType == TooManyStickersPlugin.DaredevilStickerPack)
            {
                GiveDaredevilStickers(__instance, potentialStickers, amount, openNow);
                return false;
            }
            // if we are handling any other sticker pack, remove daredevil stickers from potentialStickers before proceeding (so Daredevil stickers cant be accquired through normal sticker packs)
            potentialStickers = potentialStickers.Where(x => !StickerMetaStorage.Instance.Get(x.selection).tags.Contains("tms_daredevil")).ToArray();
            return true;
        }

        static MethodInfo _GiveNormalRandomStickers = AccessTools.Method(typeof(StickerManager), "GiveNormalRandomStickers");

        static void GiveDaredevilStickers(StickerManager man, WeightedSticker[] potentialStickers, int amount, bool openNow)
        {
            // since we are using linq already, might as well use it to create new WeightedSticker objects to avoid modifying the originals.
            WeightedSticker[] regularStickers = potentialStickers.Where(x => !StickerMetaStorage.Instance.Get(x.selection).tags.Contains("tms_daredevil")).Select(x => new WeightedSticker(x.selection, x.weight)).ToArray();
            WeightedSticker[] dareDevilStickers = potentialStickers.Where(x => StickerMetaStorage.Instance.Get(x.selection).tags.Contains("tms_daredevil")).ToArray();
            int daredevilsToGive = 0;
            while (true)
            {
                if (UnityEngine.Random.Range(0f, 1f) >= 0.2f) break;
                daredevilsToGive++;
            }
            if (daredevilsToGive > 0)
            {
                // give daredevils first
                GiveDaredevilStickersDares(man, dareDevilStickers, daredevilsToGive, true);
            }
            // calculate the average
            float average = 0f;
            for (int i = 0; i < regularStickers.Length; i++)
            {
                average += regularStickers[i].weight;
            }
            average /= regularStickers.Length;
            for (int i = 0; i < regularStickers.Length; i++)
            {
                if (regularStickers[i].weight > average)
                {
                    regularStickers[i].weight /= 2;
                }
                else
                {
                    regularStickers[i].weight *= 3;
                }
                if (regularStickers[i].selection.GetMeta().tags.Contains("tms_dareboost"))
                {
                    regularStickers[i].weight += 30;
                }
            }
            _GiveNormalRandomStickers.Invoke(man, new object[] { regularStickers, amount, openNow, false });
        }

        static void GiveDaredevilStickersDares(StickerManager man, WeightedSticker[] potentialDaredevils, int amount, bool openNow)
        {
            // ugh i hate this
            List<WeightedSelection<Sticker>> potentialStickers = potentialDaredevils.Select(x => (WeightedSelection<Sticker>)new WeightedSticker(x.selection, Mathf.RoundToInt(x.weight * (x.selection.GetMeta().value.CalculateDuplicateOddsMultiplier(man))))).ToList();
            potentialStickers.RemoveAll(x => !((DaredevilStickerData)x.selection.GetMeta().value).TestIfCanBeGiven(man.activeStickerData)); // remove all we can't give
            if (potentialStickers.Count == 0)
            {
                // TODO: when the gum daredevil sticker is added, just put it here with a weight of int.MaxValue
            }
            for (int i = 0; i < amount; i++)
            {
                int chosenIndex = WeightedSticker.RandomIndexList(potentialStickers);
                potentialStickers[chosenIndex].weight /= 2;
                man.AddSticker(potentialStickers[chosenIndex].selection, openNow, false, true);
            }
        }
    }
}
