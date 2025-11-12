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
using BepInEx.Logging;

namespace TooManyStickers
{
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi")]
    /*[BepInDependency("mtm101.rulerp.baldiplus.levelstudio", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("mtm101.rulerp.baldiplus.levelstudioloader", BepInDependency.DependencyFlags.SoftDependency)]*/
    [BepInPlugin("mtm101.baldiplus.toomanystickers", "Too Many Stickers", "0.0.0.0")]
    public class TooManyStickersPlugin : BaseUnityPlugin
    {
        public static TooManyStickersPlugin Instance;
        public static ManualLogSource logger;
        public AssetManager assetMan = new AssetManager();
        string[] stickerEnumsToRegister = new string[]
        {
            "SquishReduce",
            "StealthSpeed",
            "BoostNext",
            "StickerPackSticker",
            "PreserveItem",
            "MapShrink",
            "MoreLocks",
            "AddVents",
            "PointInvisibility"
        };

        public static Dictionary<string, Sticker> stickerEnums = new Dictionary<string, Sticker>();

        void Awake()
        {
            GeneratorManagement.Register(this, GenerationModType.Addend, GeneratorChanges);
            ModdedSaveGame.AddSaveHandler(Info);
            ModdedHighscoreManager.AddModToList(Info);
            LoadingEvents.RegisterOnAssetsLoaded(Info, LoadAssets(), LoadingEventOrder.Start);
            LoadingEvents.RegisterOnAssetsLoaded(Info, LoadEnumerator(), LoadingEventOrder.Pre);
            Instance = this;
            Harmony harmony = new Harmony("mtm101.baldiplus.toomanystickers");
            harmony.PatchAllConditionals();
            logger = Logger;
        }

        void GeneratorChanges(string name, int id, SceneObject sceneObj)
        {
            List<WeightedSticker> potentialStickersToAdd = new List<WeightedSticker>() {
                new WeightedSticker(stickerEnums["SquishReduce"], 100),
                new WeightedSticker(stickerEnums["StealthSpeed"], 100),
                new WeightedSticker(stickerEnums["BoostNext"], 30),
                new WeightedSticker(stickerEnums["PreserveItem"], 60),
                new WeightedSticker(stickerEnums["MapShrink"], 60),
                new WeightedSticker(stickerEnums["StickerPackSticker"], 20),
                new WeightedSticker(stickerEnums["MoreLocks"], 80),
                new WeightedSticker(stickerEnums["AddVents"], 80),
                new WeightedSticker(stickerEnums["PointInvisibility"], 75)
            };
            if (sceneObj.GetMeta().tags.Contains("endless"))
            {
                potentialStickersToAdd.RemoveAll(x => StickerMetaStorage.Instance.Get(x.selection).value.affectsLevelGeneration);
            }
            sceneObj.potentialStickers = sceneObj.potentialStickers.AddRangeToArray(potentialStickersToAdd.ToArray());
            sceneObj.MarkAsNeverUnload();
        }

        IEnumerator LoadAssets()
        {
            yield return 2;
            yield return "Loading sprites...";
            Sprite[] sprites = AssetLoader.TexturesFromMod(this, "*.png", "StickerSprites").ToSprites(1f);
            assetMan.AddRange<Sprite>(sprites, sprites.Select(x => x.texture.name).ToArray());
            assetMan.Add<Sprite>("YTPInvisIcon", AssetLoader.SpriteFromMod(this, Vector2.one / 2f, 1f, "YTPInvisibleIcon.png"));
            yield return "Loading localization...";
            AssetLoader.LocalizationFromMod(this);
        }

        IEnumerator LoadEnumerator()
        {
            yield return 2;
            yield return "Loading/fetching assets...";
            List<Texture2D> allTextures = Resources.FindObjectsOfTypeAll<Texture2D>().Where(x => x.GetInstanceID() >= 0).ToList();
            assetMan.Add("SaloonWall",allTextures.Find(x => x.name == "SaloonWall"));
            assetMan.Add("Carpet", allTextures.Find(x => x.name == "Carpet"));
            assetMan.Add("CeilingNoLight", allTextures.Find(x => x.name == "CeilingNoLight"));
            Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>().Where(x => x.GetInstanceID() >= 0 && x.transform.parent == null).ToArray();
            assetMan.Add("FluorescentLight", transforms.First(x => x.name == "FluorescentLight"));
            assetMan.AddFromResourcesNoClones<RoomAsset>();
            assetMan.AddFromResourcesNoClones<Structure_Vent>();
            yield return "Creating stickers...";
            for (int i = 0; i < stickerEnumsToRegister.Length; i++)
            {
                stickerEnums.Add(stickerEnumsToRegister[i], EnumExtensions.ExtendEnum<Sticker>(stickerEnumsToRegister[i]));
            }
            new StickerBuilder<ExtendedStickerData>(Info)
                .SetEnum(stickerEnums["SquishReduce"])
                .SetSprite(assetMan.Get<Sprite>("Sticker_SquishReduce"))
                .SetDuplicateOddsMultiplier(0.9f)
                .Build();
            new StickerBuilder<ExtendedStickerData>(Info)
                .SetEnum(stickerEnums["StealthSpeed"])
                .SetSprite(assetMan.Get<Sprite>("Sticker_StealthSpeed"))
                .SetDuplicateOddsMultiplier(0.9f)
                .Build();
            new StickerBuilder<BoostNextStickerData>(Info)
                .SetEnum(stickerEnums["BoostNext"])
                .SetSprite(assetMan.Get<Sprite>("Sticker_BoostNext"))
                .SetDuplicateOddsMultiplier(0.55f)
                .Build();
            new StickerBuilder<StickerPackStickerData>(Info)
                .SetEnum(stickerEnums["StickerPackSticker"])
                .SetSprite(assetMan.Get<Sprite>("Sticker_StickerPack"))
                .SetDuplicateOddsMultiplier(0.1f) // getting multiple of these in a row would be cruel
                .Build();
            new StickerBuilder<ExtendedStickerData>(Info)
                .SetEnum(stickerEnums["PreserveItem"])
                .SetSprite(assetMan.Get<Sprite>("Sticker_PreserveItem"))
                .SetDuplicateOddsMultiplier(0.75f)
                .Build();
            new StickerBuilder<ExtendedStickerData>(Info)
                .SetEnum(stickerEnums["MapShrink"])
                .SetSprite(assetMan.Get<Sprite>("Sticker_MapShrink"))
                .SetDuplicateOddsMultiplier(0.7f)
                .SetValueCap(10)
                .SetAsAffectingGenerator()
                .Build();
            new StickerBuilder<ExtendedStickerData>(Info)
                .SetEnum(stickerEnums["MoreLocks"])
                .SetSprite(assetMan.Get<Sprite>("Sticker_MoreLocks"))
                .SetDuplicateOddsMultiplier(0.8f)
                .SetAsAffectingGenerator()
                .Build();
            new StickerBuilder<ExtendedStickerData>(Info)
                .SetEnum(stickerEnums["AddVents"])
                .SetSprite(assetMan.Get<Sprite>("Sticker_AddVents"))
                .SetDuplicateOddsMultiplier(0.75f)
                .SetAsAffectingGenerator()
                .Build();
            new StickerBuilder<ExtendedStickerData>(Info)
                .SetEnum(stickerEnums["PointInvisibility"])
                .SetSprite(assetMan.Get<Sprite>("Sticker_PointInvisibility"))
                .SetDuplicateOddsMultiplier(0.85f)
                .Build();
        }
    }
}
