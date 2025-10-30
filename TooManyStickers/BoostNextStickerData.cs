using MTM101BaldAPI;
using System;
using System.Collections.Generic;
using System.Text;

namespace TooManyStickers
{
    public class BoostNextStickerData : ExtendedStickerData
    {
        public override bool CanBeCovered(StickerStateData data)
        {
            // TODO: implement logic that finds which StickerStateData this object is covering and then if it cant be covered neither can we
            // the reasoning for the above is so that if we are boosting a generator modifying sticker we cant be removed
            return base.CanBeCovered(data);
        }
    }
}
