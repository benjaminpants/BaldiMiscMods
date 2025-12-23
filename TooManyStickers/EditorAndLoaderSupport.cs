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
        }

        public static void AddStudioSupport()
        {
            LevelStudioPlugin.Instance.stickerSprites.Add("squish_reduce", AssetLoader.SpriteFromMod(TooManyStickersPlugin.Instance, Vector2.one / 2f, 1f, "SmallStickerSprites", "squish_reduce.png"));
        }
    }
}
