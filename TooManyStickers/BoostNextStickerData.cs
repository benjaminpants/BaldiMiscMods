using MTM101BaldAPI;
using MTM101BaldAPI.Registers;
using System;
using System.Collections.Generic;
using System.Text;

namespace TooManyStickers
{
    public class BoostNextStickerData : ExtendedStickerData
    {
        public override bool CanBeCovered(StickerStateData data)
        {
            if (!base.CanBeCovered(data)) return false;
            int index = Array.FindIndex(Singleton<StickerManager>.Instance.activeStickerData, x => x == data);
            CalculateBoost(index, Singleton<StickerManager>.Instance.activeStickerData, new bool[Singleton<StickerManager>.Instance.activeStickerData.Length], out StickerStateData landedOn, out _);
            if (landedOn == null) return true;
            return StickerMetaStorage.Instance.Get(landedOn.sticker).value.CanBeCovered(landedOn);
        }

        public static void CalculateBoost(int i, StickerStateData[] activeStickerData, bool[] alreadyProcessed, out StickerStateData landedOn, out int addition)
        {
            landedOn = null;
            addition = 0;
            int offset = 0;
            int attempts = 0;
            while (!alreadyProcessed[(i + offset) % activeStickerData.Length] && activeStickerData[(i + offset) % activeStickerData.Length].sticker == TooManyStickersPlugin.stickerEnums["BoostNext"])
            {
                if (attempts > activeStickerData.Length) break; // give up
                alreadyProcessed[(i + offset) % activeStickerData.Length] = true;
                offset++;
                addition++;
                landedOn = activeStickerData[(i + offset) % activeStickerData.Length];
            }
        }

        public override string GetLocalizedAppliedStickerDescription(StickerStateData data)
        {
            string outputString = base.GetLocalizedAppliedStickerDescription(data);
            int index = Array.FindIndex(Singleton<StickerManager>.Instance.activeStickerData, x => x == data);
            string boostingName = outputString;
            if (index == -1)
            {
                return string.Format(outputString, "ERROR!");
            }
            CalculateBoost(index, Singleton<StickerManager>.Instance.activeStickerData, new bool[Singleton<StickerManager>.Instance.activeStickerData.Length], out StickerStateData landedOn, out _);
            if (landedOn == null)
            {
                return string.Format(outputString, LocalizationManager.Instance.GetLocalizedText("StickerTitle_BoostNext"));
            }
            return string.Format(outputString, LocalizationManager.Instance.GetLocalizedText($"StickerTitle_{landedOn.sticker.ToStringExtended()}"));
        }
    }
}
