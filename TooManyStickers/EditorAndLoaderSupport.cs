using MTM101BaldAPI.AssetTools;
using PlusLevelStudio;
using PlusStudioLevelLoader;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TooManyStickers
{
    internal static class EditorAndLoaderSupport
    {
        public static void AddLoaderSupport()
        {
            LevelLoaderPlugin.Instance.stickerAliases.Add("squish_reduce", TooManyStickersPlugin.stickerEnums["SquishReduce"]);
            LevelLoaderPlugin.Instance.stickerAliases.Add("stealth_speed", TooManyStickersPlugin.stickerEnums["StealthSpeed"]);
            LevelLoaderPlugin.Instance.stickerAliases.Add("boost_next", TooManyStickersPlugin.stickerEnums["BoostNext"]);
            LevelLoaderPlugin.Instance.stickerAliases.Add("sticker_pack_sticker", TooManyStickersPlugin.stickerEnums["StickerPackSticker"]);
            LevelLoaderPlugin.Instance.stickerAliases.Add("preserve_item", TooManyStickersPlugin.stickerEnums["PreserveItem"]);
            LevelLoaderPlugin.Instance.stickerAliases.Add("point_invis", TooManyStickersPlugin.stickerEnums["PointInvisibility"]);
            LevelLoaderPlugin.Instance.stickerAliases.Add("daredevil_baldiangry", TooManyStickersPlugin.stickerEnums["Daredevil_BaldiAngry"]);
            LevelLoaderPlugin.Instance.stickerAliases.Add("daredevil_divide", TooManyStickersPlugin.stickerEnums["Daredevil_Divide"]);
            LevelLoaderPlugin.Instance.stickerAliases.Add("daredevil_dud", TooManyStickersPlugin.stickerEnums["Daredevil_Dud"]);
            LevelLoaderPlugin.Instance.stickerAliases.Add("daredevil_itemuseantibonus", TooManyStickersPlugin.stickerEnums["Daredevil_ItemUseAntiBonus"]);
            LevelLoaderPlugin.Instance.stickerAliases.Add("daredevil_lessstamina", TooManyStickersPlugin.stickerEnums["Daredevil_LessStamina"]);
            LevelLoaderPlugin.Instance.stickerAliases.Add("daredevil_lowvision", TooManyStickersPlugin.stickerEnums["Daredevil_LowVision"]);
        }

        public static void AddStudioSupport()
        {
            AddStickerStudio("squish_reduce");
            AddStickerStudio("stealth_speed");
            AddStickerStudio("boost_next");
            AddStickerStudio("sticker_pack_sticker");
            AddStickerStudio("preserve_item");
            AddStickerStudio("point_invis");

            AddStickerStudio("daredevil_baldiangry");
            AddStickerStudio("daredevil_divide");
            AddStickerStudio("daredevil_dud");
            AddStickerStudio("daredevil_itemuseantibonus");
            AddStickerStudio("daredevil_lessstamina");
            AddStickerStudio("daredevil_lowvision");
        }

        public static void AddStickerStudio(string sticker)
        {
            LevelStudioPlugin.Instance.selectableStickers.Add(sticker);
            LevelStudioPlugin.Instance.stickerSprites.Add(sticker, AssetLoader.SpriteFromMod(TooManyStickersPlugin.Instance, Vector2.one / 2f, 1f, "SmallStickerSprites", sticker + ".png"));
        }
    }
}
