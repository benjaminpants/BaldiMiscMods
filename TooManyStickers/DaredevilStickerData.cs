using MTM101BaldAPI;
using MTM101BaldAPI.Registers;
using System;
using System.Collections.Generic;
using System.Text;

namespace TooManyStickers
{
    public class DaredevilStickerData : ExtendedStickerData
    {
        // daredevil stickers cannot be removed once applied.
        // they can only be removed once the level is completed.
        public override bool CanBeCovered(StickerStateData data)
        {
            return false;
        }

        /// <summary>
        /// Returns true if any of the passed in stickers can be replaced by this one.
        /// This is used to determine if a Daredevil sticker can be reasonably given to the player.
        /// </summary>
        /// <param name="toTest"></param>
        /// <returns></returns>
        public virtual bool TestIfCanBeGiven(StickerStateData[] toTest)
        {
            StickerStateData myState = CreateStateData(0, true, false);
            for (int i = 0; i < toTest.Length; i++)
            {
                if (StickerMetaStorage.Instance.Get(toTest[i].sticker).value.CanBeCovered(myState)) return true;
            }
            return false;
        }
    }
}
