using BepInEx;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;
using MTM101BaldAPI;
using System;
using System.Collections;
using UnityEngine;
using System.Linq;
using MTM101BaldAPI.ObjectCreation;
using HarmonyLib;
using System.Collections.Generic;

namespace TooManyStickers
{
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi")]
    /*[BepInDependency("mtm101.rulerp.baldiplus.levelstudio", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("mtm101.rulerp.baldiplus.levelstudioloader", BepInDependency.DependencyFlags.SoftDependency)]*/
    [BepInPlugin("mtm101.baldiplus.toomanystickers", "Too Many Stickers", "0.0.0.0")]
    public class TooManyStickersPlugin : BaseUnityPlugin
    {
        public AssetManager assetMan = new AssetManager();
        string[] stickerEnumsToRegister = new string[]
        {
            "SquishReduce",
            "StealthSpeed",
            "BoostNext",
            "StickerPackSticker",
            "PreserveItem"
        };

        public static Dictionary<string, Sticker> stickerEnums = new Dictionary<string, Sticker>();

        void Awake()
        {
            GeneratorManagement.Register(this, GenerationModType.Addend, GeneratorChanges);
            ModdedSaveGame.AddSaveHandler(Info);
            LoadingEvents.RegisterOnAssetsLoaded(Info, LoadAssets(), LoadingEventOrder.Start);
            LoadingEvents.RegisterOnAssetsLoaded(Info, LoadEnumerator(), LoadingEventOrder.Pre);
            Harmony harmony = new Harmony("mtm101.baldiplus.toomanystickers");
            harmony.PatchAllConditionals();
        }

        void GeneratorChanges(string name, int id, SceneObject sceneObj)
        {
            sceneObj.potentialStickers = sceneObj.potentialStickers.AddRangeToArray(new WeightedSticker[]
            {
                new WeightedSticker()
                {
                    selection = stickerEnums["SquishReduce"],
                    weight = 100
                },
                new WeightedSticker()
                {
                    selection = stickerEnums["StealthSpeed"],
                    weight = 100
                }
            });
            sceneObj.MarkAsNeverUnload();
        }

        IEnumerator LoadAssets()
        {
            yield return 1;
            yield return "Loading sprites...";
            Sprite[] sprites = AssetLoader.TexturesFromMod(this, "*.png", "StickerSprites").ToSprites(1f);
            assetMan.AddRange<Sprite>(sprites, sprites.Select(x => x.texture.name).ToArray());
        }

        IEnumerator LoadEnumerator()
        {
            yield return 1;
            yield return "Creating stickers...";
            for (int i = 0; i < stickerEnumsToRegister.Length; i++)
            {
                stickerEnums.Add(stickerEnumsToRegister[i], EnumExtensions.ExtendEnum<Sticker>(stickerEnumsToRegister[i]));
            }
            new StickerBuilder<ExtendedStickerData>(Info)
                .SetEnum(stickerEnums["SquishReduce"])
                .SetSprite(assetMan.Get<Sprite>("Sticker_SquishReduce"))
                .Build();
            new StickerBuilder<ExtendedStickerData>(Info)
                .SetEnum(stickerEnums["StealthSpeed"])
                .SetSprite(assetMan.Get<Sprite>("Sticker_StealthSpeed"))
                .Build();
            new StickerBuilder<ExtendedStickerData>(Info)
                .SetEnum(stickerEnums["BoostNext"])
                .SetSprite(assetMan.Get<Sprite>("Sticker_BoostNext"))
                .Build();
            new StickerBuilder<StickerPackStickerData>(Info)
                .SetEnum(stickerEnums["StickerPackSticker"])
                .SetSprite(assetMan.Get<Sprite>("Sticker_StickerPack"))
                .Build();
            new StickerBuilder<ExtendedStickerData>(Info)
                .SetEnum(stickerEnums["PreserveItem"])
                .SetSprite(assetMan.Get<Sprite>("Sticker_PreserveItem"))
                .Build();
        }
    }
}
