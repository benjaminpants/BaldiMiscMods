using MTM101BaldAPI.AssetTools;
using PlusLevelStudio;
using PlusLevelStudio.Editor;
using PlusLevelStudio.Editor.Tools;
using PlusStudioLevelLoader;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PlusPresents
{
    public static class EditorAndLoaderSupport
    {
        public static void AddLoaderSupport()
        {
            LevelLoaderPlugin.Instance.itemObjects.Add("pluspresent", PlusPresentsPlugin.presentItem);
            LevelLoaderPlugin.Instance.stickerAliases.Add("betterpresents", PlusPresentsPlugin.betterPresentSticker);
        }

        public static void AddStudioSupport()
        {
            LevelStudioPlugin.Instance.selectableGeneratorItems.Add("pluspresent");
            LevelStudioPlugin.Instance.selectableShopItems.Add("pluspresent");
            LevelStudioPlugin.Instance.selectableStickers.Add("betterpresents");
            LevelStudioPlugin.Instance.stickerSprites.Add("betterpresents", AssetLoader.SpriteFromMod(PlusPresentsPlugin.Instance, Vector2.one / 2f, 1f, "betterpresents.png"));
            EditorInterfaceModes.AddModeCallback((EditorMode mode, bool vanillaCompat) =>
            {
                EditorInterfaceModes.AddToolToCategory(mode, "items", new ItemTool("pluspresent"));
            });
        }
    }
}
