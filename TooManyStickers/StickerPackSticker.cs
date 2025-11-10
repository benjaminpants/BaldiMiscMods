using MTM101BaldAPI;
using MTM101BaldAPI.Registers;
using System;
using System.Collections.Generic;
using System.Text;

namespace TooManyStickers
{
    public class StickerPackStickerData : ExtendedStickerData
    {
        public override StickerStateData CreateOrGetAppliedStateData(StickerStateData inventoryState)
        {
            WeightedSticker[] randomStickers = Singleton<BaseGameManager>.Instance.InPitstop() ? Singleton<CoreGameManager>.Instance.sceneObject.nextLevel.potentialStickers : Singleton<CoreGameManager>.Instance.sceneObject.potentialStickers;
            ExtendedStickerData extData;
            if ((randomStickers == null) || (randomStickers.Length == 0))
            {
                TooManyStickersPlugin.logger.LogWarning("Sticker Pack sticker had to go to fallback!");
                StickerMetaData[] allStickers = StickerMetaStorage.Instance.All();
                extData = allStickers[UnityEngine.Random.Range(0, allStickers.Length)].value;
            }
            else
            {
                extData = StickerMetaStorage.Instance.Get(WeightedSticker.RandomSelection(randomStickers)).value;
            }
            return extData.CreateOrGetAppliedStateData(extData.CreateStateData(inventoryState.activeLevel, inventoryState.opened, inventoryState.sticky));
        }
    }
}
