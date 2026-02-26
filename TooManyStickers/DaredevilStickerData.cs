using MTM101BaldAPI;
using MTM101BaldAPI.Registers;
using System;
using System.Collections.Generic;
using System.Text;

namespace TooManyStickers
{
    public class DaredevilStickerData : ExtendedStickerData
    {
        public virtual bool canBeForceApplied => true;
        // daredevil stickers cannot be removed once applied.
        // they can only be removed once the level is completed.
        public override BooleanHandshake CanBeCovered(StickerStateData thisSticker, StickerStateData coveringSticker)
        {
            return BooleanHandshake.FalseIfAgree;
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
                if (myState.CanCover(toTest[i], -1, i)) return true;
            }
            return false;
        }
    }

    public class GumDaredevilStickerData : DaredevilStickerData
    {
        public override bool canBeForceApplied => false;
        public override bool TestIfCanBeGiven(StickerStateData[] toTest)
        {
            return true;
        }

        public override void ApplySticker(StickerManager manager, StickerStateData inventoryState, int slot)
        {
            // Do literally nothing for now
            //base.ApplySticker(manager, inventoryState, slot);
        }
    }
}
