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
                extData = StickerMetaStorage.Instance.Get(Sticker.Nothing).value; // lol
            }
            else
            {
                extData = StickerMetaStorage.Instance.Get(WeightedSticker.RandomSelection(randomStickers)).value;
            }
            return extData.CreateOrGetAppliedStateData(extData.CreateStateData(inventoryState.activeLevel, inventoryState.opened));
        }
    }
}
