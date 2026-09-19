using MTM101BaldAPI;
using MTM101BaldAPI.Registers;
using PlusStudioLevelFormat;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace TooManyStickers
{
    public class ChestStickerData : ExtendedStickerData
    {

        public Sprite openSprite;

        public override BooleanHandshake CanBeCovered(StickerStateData thisSticker, StickerStateData coveringSticker)
        {
            ChestStickerStateData chestState = (ChestStickerStateData)thisSticker;
            if ((!chestState.CanContainSticker(coveringSticker).AsBool()) && !chestState.isFull) return BooleanHandshake.FalseIfAgree;
            BooleanHandshake[] results = new BooleanHandshake[chestState.stickerStates.Length];
            for (int i = 0; i < chestState.stickerStates.Length; i++)
            {
                if (chestState.stickerStates[i] == null) return BooleanHandshake.TrueIfAgree; // we have a free slot! let it go here!
                results[i] = chestState.stickerStates[i].GetMeta().value.CanBeCovered(chestState.stickerStates[i], coveringSticker);
            }
            bool finalRes = true;
            for (int i = 0; i < results.Length; i++)
            {
                finalRes &= results[i].AsBool();
            }

            return !finalRes ? BooleanHandshake.FalseIfAgree : base.CanBeCovered(thisSticker, coveringSticker);
        }

        public override void ApplySticker(StickerManager manager, StickerStateData inventoryState, int slot)
        {
            base.ApplySticker(manager, inventoryState, slot);
        }

        public override BooleanHandshake CanCoverSticker(StickerStateData thisSticker, StickerStateData otherSticker, int heldStickerSlot, int otherStickerSlot)
        {
            return ((ChestStickerStateData)thisSticker).CanContainSticker(otherSticker);
        }

        public override Sprite GetInventorySprite(StickerStateData data)
        {
            return base.GetInventorySprite(data);
        }

        public override Sprite GetAppliedSprite(StickerStateData data)
        {
            ChestStickerStateData chestState = (ChestStickerStateData)data;
            return chestState.isFull ? base.GetAppliedSprite(data) : openSprite;
        }

        public override string GetLocalizedAppliedStickerDescription(StickerStateData data)
        {
            ChestStickerStateData chestState = (ChestStickerStateData)data;
            for (int i = 0; i < chestState.stickerStates.Length; i++)
            {
                if (chestState.stickerStates[i] != null)
                {
                    return GetLocalizedContentsDescription(chestState);
                }
            }
            return base.GetLocalizedAppliedStickerDescription(data);
        }

        public virtual string GetLocalizedContentsDescription(ChestStickerStateData data)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(LocalizationManager.Instance.GetLocalizedText($"StickerTitle_{EnumExtensions.GetExtendedName<Sticker>((int)sticker)}"));
            sb.Append("<br>");
            sb.Append(LocalizationManager.Instance.GetLocalizedText($"StickerDescriptionShort_{EnumExtensions.GetExtendedName<Sticker>((int)sticker)}"));
            sb.Append("<br>");
            for (int i = 0; i < data.stickerStates.Length; i++)
            {
                if (data.stickerStates[i] == null)
                {
                    break;
                }
                sb.Append(data.stickerStates[i].GetMeta().value.GetLocalizedStickerTitle(data.stickerStates[i]));
                sb.Append("<br>");
            }
            List<Sticker> describedStickers = new List<Sticker>();
            for (int i = 0; i < data.stickerStates.Length; i++)
            {
                if (data.stickerStates[i] == null)
                {
                    break;
                }
                if (describedStickers.Contains(data.stickerStates[i].sticker)) continue;
                sb.Append("<br>");
                sb.Append(data.stickerStates[i].GetMeta().value.GetLocalizedStickerDescription(data.stickerStates[i]));
                describedStickers.Add(data.stickerStates[i].sticker);
            }
            return sb.ToString();
        }

        public override StickerStateData CreateStateData(int activeLevel, bool opened, bool sticky)
        {
            return new ChestStickerStateData(sticker, activeLevel, opened, sticky);
        }

        public override StickerStateData CreateOrGetAppliedStateData(StickerStateData inventoryState)
        {
            inventoryState.opened = true;
            inventoryState.activeLevel = Singleton<BaseGameManager>.Instance.CurrentLevel;
            return inventoryState;
        }
    }

    public class ChestStickerStateData : ExtendedStickerStateData
    {
        public StickerStateData[] stickerStates = new StickerStateData[2];

        public bool isFull
        {
            get
            {
                for (int i = 0; i < stickerStates.Length; i++)
                {
                    if (stickerStates[i] == null) return false;
                }
                return true;
            }
        }

        public ChestStickerStateData(Sticker sticker, int activeLevel, bool opened, bool sticky) : base(sticker, activeLevel, opened, sticky)
        {
        }

        public virtual BooleanHandshake CanContainSticker(StickerStateData otherData)
        {
            StickerMetaData meta = otherData.GetMeta();
            if (meta.tags.Contains("tms_nocheststicker") || meta.tags.Contains("tms_daredevil")) // Placeholder until we properly have implicit tags
            {
                return BooleanHandshake.FalseIfAgree;
            }
            return meta.flags.HasFlag(StickerFlags.AffectsLevelGeneration) ? BooleanHandshake.AlwaysTrue : BooleanHandshake.TrueIfAgree;
        }

        const byte version = 0;
        public override void VirtualWrite(BinaryWriter writer)
        {
            base.VirtualWrite(writer);
            writer.Write(version);
            writer.Write((byte)stickerStates.Length);
            for (int i = 0; i < stickerStates.Length; i++)
            {
                if (stickerStates[i] == null)
                {
                    writer.Write(Sticker.Nothing.ToString());
                    continue;
                }
                writer.Write(stickerStates[i].sticker.ToStringExtended());
                stickerStates[i].Write(writer);
            }
        }

        public override void VirtualReadInto(BinaryReader reader)
        {
            base.VirtualReadInto(reader);
            byte version = reader.ReadByte();
            int count = reader.ReadByte();
            stickerStates = new StickerStateData[count];
            for (int i = 0; i < count; i++)
            {
                EnumExtensions.GetFromExtendedNameSafe(reader.ReadString(), out Sticker? sticker);
                if (!sticker.HasValue)
                {
                    sticker = Sticker.Silence; // There WAS a sticker here, so leaving null in this slot would cause problems, so lets just put earmuffs there.
                }
                if (sticker == Sticker.Nothing) continue;
                StickerStateData data = StickerMetaStorage.Instance.Get(sticker.Value).value.CreateStateData(activeLevel, opened, sticky);
                data.ReadInto(reader);
            }
        }
    }
}
