using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Registers;
using System;
using System.Collections;
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
                if (UnityEngine.Random.Range(0f, 1f) >= 0.35f) break;
                daredevilsToGive++;
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

            // give daredevils last for suspense
            if (daredevilsToGive > 0)
            {
                GiveDaredevilStickersDares(man, dareDevilStickers, daredevilsToGive, true, false);
            }
        }

        static void GiveDaredevilStickersDares(StickerManager man, WeightedSticker[] potentialDaredevils, int amount, bool openNow, bool forceApply)
        {
            List<WeightedSelection<Sticker>> potentialStickers = potentialDaredevils.Select(x => (WeightedSelection<Sticker>)new WeightedSticker(x.selection, Mathf.RoundToInt(x.weight * (x.selection.GetMeta().value.CalculateDuplicateOddsMultiplier(man))))).ToList();
            bool anySuccessfullyGiven = false; // if we were able to give ANY regular daredevils, we want this true so we dont give gum stickers, as giving gum stickers and regular stickers just makes it easy to dispose of the gum stickers.
            for (int i = 0; i < amount; i++)
            {
                int stickersAdded = Singleton<StickerManager>.Instance.stickerInventory.Count(x => x.GetMeta().tags.Contains("tms_daredevil")) + Singleton<StickerManager>.Instance.activeStickerData.Count(x => x.GetMeta().tags.Contains("tms_daredevil"));
                potentialStickers.RemoveAll(x => !((DaredevilStickerData)x.selection.GetMeta().value).TestIfCanBeGiven(man.activeStickerData)); // remove all we can't give
                if (potentialStickers.Count == 0)
                {
                    potentialStickers.Add(new WeightedSelection<Sticker>() { weight = int.MaxValue, selection = TooManyStickersPlugin.stickerEnums["Daredevil_Dud"] });
                }
                int chosenIndex = WeightedSticker.RandomIndexList(potentialStickers);
                // subtract 10 from weight and clamp to 10, but if the current weight is smaller than 10 (only possible if the initial weight was less than 10) then stick with that initial weight
                potentialStickers[chosenIndex].weight = Mathf.Max(potentialStickers[chosenIndex].weight - 10,Mathf.Min(potentialStickers[chosenIndex].weight, 10));
                // get all the potential slots this can be put in
                StickerStateData data = potentialStickers[chosenIndex].selection.GetMeta().value.CreateStateData(0, openNow, false);
                List<int> potentialSlots = new List<int>();
                for (int j = 0; j < man.activeStickerData.Length; j++)
                {
                    //if ((data.GetMeta().value.CouldCoverSticker(man, data, man.activeStickerData[j], 0, j)) && man.activeStickerData[j].GetMeta().value.CanBeCovered(man.activeStickerData[j]))
                    if (data.CanCover(man.activeStickerData[j], 0, j))
                    {
                        potentialSlots.Add(j);
                    }
                }
                if ((potentialSlots.Count == 0) && (!anySuccessfullyGiven))
                {
                    man.AddSticker(TooManyStickersPlugin.stickerEnums["Daredevil_Gum"], openNow, false, true);
                    continue;
                }
                if (forceApply && ((DaredevilStickerData)potentialStickers[chosenIndex].selection.GetMeta().value).canBeForceApplied)
                {
                    Singleton<CoreGameManager>.Instance.GetHud(0).ShowCollectedSticker(data.GetMeta().value.GetInventorySprite(data));
                    man.ApplySticker(data, potentialSlots[UnityEngine.Random.Range(0, potentialSlots.Count)]);
                }
                else
                {
                    if ((potentialSlots.Count - stickersAdded) <= 0)
                    {
                        if (!anySuccessfullyGiven)
                        {
                            man.AddSticker(TooManyStickersPlugin.stickerEnums["Daredevil_Gum"], openNow, false, true);
                        }
                        continue;
                    }
                    anySuccessfullyGiven = true;
                    man.AddExistingSticker(data, true);
                }
            }
        }
    }
}
