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
    [BepInDependency("mtm101.rulerp.baldiplus.levelstudio", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("mtm101.rulerp.baldiplus.levelstudioloader", BepInDependency.DependencyFlags.SoftDependency)]
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
            "PointInvisibility",
            "Daredevil_LessStamina",
            "Daredevil_Divide",
            "Daredevil_BaldiAngry",
            "Daredevil_LowVision"
        };

        public static Dictionary<string, Sticker> stickerEnums = new Dictionary<string, Sticker>();

        public static StickerPackType DaredevilStickerPack;

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
                new WeightedSticker(stickerEnums["PointInvisibility"], 75),
                new WeightedSticker(stickerEnums["Daredevil_LessStamina"], 100),
                new WeightedSticker(stickerEnums["Daredevil_Divide"], 100),
                new WeightedSticker(stickerEnums["Daredevil_BaldiAngry"], 110),
                new WeightedSticker(stickerEnums["Daredevil_LowVision"], 110)
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
            yield return 4;
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
                .SetTags("tms_dareboost")
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


            yield return "Creating daredevil stickers...";
            DaredevilStickerPack = EnumExtensions.ExtendEnum<StickerPackType>("Daredevil");
            // Daredevil Stickers
            new StickerBuilder<DaredevilStickerData>(Info)
                .SetEnum(stickerEnums["Daredevil_LessStamina"])
                .SetSprite(assetMan.Get<Sprite>("DaredevilSticker_LessStamina"))
                .SetTags("tms_daredevil")
                .SetDuplicateOddsMultiplier(0.9f)
                .SetValueCap(3) // i dont like doing this but like. anything higher will result in negatives and negative stamina = bad.
                .Build();

            new StickerBuilder<DaredevilStickerData>(Info)
                .SetEnum(stickerEnums["Daredevil_Divide"])
                .SetSprite(assetMan.Get<Sprite>("DaredevilSticker_Divide"))
                .SetTags("tms_daredevil")
                .SetDuplicateOddsMultiplier(0.9f)
                .SetValueCap(4) // any further has no effect, maybe in the future i can add some kind of dapenening so higher values have a barely noticable effect but for now...
                .Build();

            new StickerBuilder<DaredevilStickerData>(Info)
                .SetEnum(stickerEnums["Daredevil_BaldiAngry"])
                .SetSprite(assetMan.Get<Sprite>("DaredevilSticker_BaldiAngry"))
                .SetTags("tms_daredevil")
                .SetDuplicateOddsMultiplier(0.9f)
                .Build();

            new StickerBuilder<DaredevilStickerData>(Info)
                .SetEnum(stickerEnums["Daredevil_LowVision"])
                .SetSprite(assetMan.Get<Sprite>("DaredevilSticker_LowVision"))
                .SetTags("tms_daredevil")
                .SetDuplicateOddsMultiplier(0.9f)
                .Build();

            yield return "Modifying metadata...";
            StickerMetaStorage.Instance.Get(Sticker.GlueStick).tags.Add("tms_always_in_stickerpack_sticker");
            StickerMetaStorage.Instance.Get(Sticker.Stamina).tags.Add("tms_always_in_stickerpack_sticker");
            // add dareboosts to any that i think deserve them
            StickerMetaStorage.Instance.Get(Sticker.InventorySlot).tags.Add("tms_dareboost");
            StickerMetaStorage.Instance.Get(Sticker.YtpMulitplier).tags.Add("tms_dareboost");
            StickerMetaStorage.Instance.Get(Sticker.BaldiPraise).tags.Add("tms_dareboost");
            StickerMetaStorage.Instance.Get(Sticker.Elevator).tags.Add("tms_dareboost");
        }
    }
}
