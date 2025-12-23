using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Registers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TooManyStickers
{
    public class StickerPackStickerData : ExtendedStickerData
    {

        public override void ApplySticker(StickerManager manager, StickerStateData inventoryState, int slot)
        {
            ExtendedStickerData chosenSticker = ChooseRandomSticker();
            chosenSticker.ApplySticker(manager, chosenSticker.CreateStateData(inventoryState.activeLevel, inventoryState.opened, inventoryState.sticky), slot);
        }

        public ExtendedStickerData ChooseRandomSticker()
        {
            List<WeightedSticker> randomStickers = (Singleton<BaseGameManager>.Instance.InPitstop() ? Singleton<CoreGameManager>.Instance.nextLevel.potentialStickers : Singleton<CoreGameManager>.Instance.sceneObject.potentialStickers).ToList();
            ExtendedStickerData extData;
            if ((randomStickers == null) || (randomStickers.Count == 0))
            {
                TooManyStickersPlugin.logger.LogWarning("Sticker Pack sticker had to go to fallback!");
                randomStickers = StickerMetaStorage.Instance.All().Select(x => new WeightedSticker(x.value.sticker, 100)).ToList();
            }
            else
            {
                // add stickers tagged as "always present"
                StickerMetaStorage.Instance.FindAllWithTags(false, "tms_always_in_stickerpack_sticker").Do(x => randomStickers.Add(new WeightedSticker(x.value.sticker, 25)));
            }
            randomStickers.RemoveAll(x => StickerMetaStorage.Instance.Get(x.selection).tags.Contains("tms_never_in_stickerpack_sticker"));
            randomStickers.RemoveAll(x => StickerMetaStorage.Instance.Get(x.selection).tags.Contains("tms_daredevil"));
            extData = StickerMetaStorage.Instance.Get(WeightedSticker.RandomSelection(randomStickers.ToArray())).value;
            return extData;
        }

        public override StickerStateData CreateOrGetAppliedStateData(StickerStateData inventoryState)
        {
            ExtendedStickerData extData = ChooseRandomSticker();
            return extData.CreateOrGetAppliedStateData(extData.CreateStateData(inventoryState.activeLevel, inventoryState.opened, inventoryState.sticky));
        }
    }
}
