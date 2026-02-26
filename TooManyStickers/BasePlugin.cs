using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.ObjectCreation;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TooManyStickers
{
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi")]
    [BepInDependency("mtm101.rulerp.baldiplus.levelstudio", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("mtm101.rulerp.baldiplus.levelstudioloader", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin("mtm101.baldiplus.toomanystickers", "Too Many Stickers", "2.0.0.0")]
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
            "PizzaBonus",
            "PraiseTimeSlow",
            "QuarterChance",
            "Favoritism",
            "ShorterEvents",
            "IceEyes",
            "MoveResist",
            "Daredevil_LessStamina",
            "Daredevil_Divide",
            "Daredevil_BaldiAngry",
            "Daredevil_LowVision",
            "Daredevil_Gum",
            "Daredevil_Dud",
            "Daredevil_ItemUseAntiBonus"
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

        bool ContainsForcedCharacter(SceneObject scenObj, Character chr)
        {
            CustomLevelObject[] objs = scenObj.GetCustomLevelObjects();
            for (int i = 0; i < objs.Length; i++)
            {
                if (objs[i].forcedNpcs.Count(x => x.Character == chr) > 0) return true;
            }
            if (scenObj.forcedNpcs.Count(x => x.Character == chr) > 0) return true;
            foreach (SceneObject scene in scenObj.previousLevels)
            {
                // levelobject forced npcs don't carry across floors so a check for that would be redundant
                if (scenObj.forcedNpcs.Count(x => x.Character == chr) > 0) return true;
            }
            return false;
        }

        void GeneratorChanges(string name, int id, SceneObject sceneObj)
        {
            List<WeightedSticker> potentialStickersToAdd = new List<WeightedSticker>() {
                new WeightedSticker(stickerEnums["SquishReduce"], 100),
                new WeightedSticker(stickerEnums["StealthSpeed"], 100),
                new WeightedSticker(stickerEnums["BoostNext"], 20),
                new WeightedSticker(stickerEnums["PreserveItem"], 60),
                new WeightedSticker(stickerEnums["MapShrink"], 60),
                new WeightedSticker(stickerEnums["StickerPackSticker"], 20),
                new WeightedSticker(stickerEnums["MoreLocks"], 80),
                new WeightedSticker(stickerEnums["AddVents"], 80),
                new WeightedSticker(stickerEnums["PointInvisibility"], 55),
                new WeightedSticker(stickerEnums["PizzaBonus"], 70),
                new WeightedSticker(stickerEnums["PraiseTimeSlow"], 30),
                new WeightedSticker(stickerEnums["QuarterChance"], 60),
                new WeightedSticker(stickerEnums["ShorterEvents"], 80),
                new WeightedSticker(stickerEnums["IceEyes"], 75),
                new WeightedSticker(stickerEnums["MoveResist"], 90),
                new WeightedSticker(stickerEnums["Daredevil_LessStamina"], 100),
                new WeightedSticker(stickerEnums["Daredevil_Divide"], 100),
                new WeightedSticker(stickerEnums["Daredevil_BaldiAngry"], 110),
                new WeightedSticker(stickerEnums["Daredevil_LowVision"], 85),
                new WeightedSticker(stickerEnums["Daredevil_Dud"], 2),
                new WeightedSticker(stickerEnums["Daredevil_ItemUseAntiBonus"], 90)
            };
            if (ContainsForcedCharacter(sceneObj, Character.Principal))
            {
                potentialStickersToAdd.Add(new WeightedSticker(stickerEnums["Favoritism"], 50));
            }
            if (sceneObj.GetMeta().tags.Contains("endless"))
            {
                potentialStickersToAdd.RemoveAll(x => (StickerMetaStorage.Instance.Get(x.selection).flags.HasFlag(StickerFlags.AffectsLevelGeneration) || StickerMetaStorage.Instance.Get(x.selection).flags.HasFlag(StickerFlags.IsBonus)));
            }
            sceneObj.potentialStickers = sceneObj.potentialStickers.AddRangeToArray(potentialStickersToAdd.ToArray());
            sceneObj.MarkAsNeverUnload();
        }

        IEnumerator LoadAssets()
        {
            yield return 3;
            yield return "Loading sprites...";
            Sprite[] sprites = AssetLoader.TexturesFromMod(this, "*.png", "StickerSprites").ToSprites(1f);
            assetMan.AddRange<Sprite>(sprites, sprites.Select(x => x.texture.name).ToArray());
            assetMan.Add<Sprite>("YTPInvisIcon", AssetLoader.SpriteFromMod(this, Vector2.one / 2f, 1f, "YTPInvisibleIcon.png"));
            assetMan.Add<Sprite>("JohnnyTeleporter", AssetLoader.SpriteFromMod(this, new Vector2(0.4168f, 0.4198f), 28f, "JohnnyTeleporter.png"));
            assetMan.Add<Sprite>("JohnnyTeleporter_Press", AssetLoader.SpriteFromMod(this, new Vector2(0.4168f, 0.4198f), 28f, "JohnnyTeleporter_Press.png"));
            assetMan.Add<Sprite>("PizzaItem", AssetLoader.SpriteFromMod(this, Vector2.one / 2f, 25f, "PizzaItem.png"));
            yield return "Loading audio...";
            assetMan.Add<SoundObject>("Jon_Daredevils", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "Jon_Daredevils.wav"), "Vfx_Jon_Daredevils", SoundType.Voice, Color.white));
            yield return "Loading localization...";
            AssetLoader.LocalizationFromMod(this);
        }

        IEnumerator LoadEnumerator()
        {
            yield return 8;
            yield return "Loading/fetching assets...";

            List<Sprite> allSprites = Resources.FindObjectsOfTypeAll<Sprite>().Where(x => x.GetInstanceID() >= 0).ToList();
            assetMan.Add<Sprite>("JohnnyMouthSheet_0", allSprites.First(x => x.name == "JohnnyMouthSheet_0"));
            assetMan.Add<Sprite>("JohnnyMouthSheet_1", allSprites.First(x => x.name == "JohnnyMouthSheet_1"));
            assetMan.Add<Sprite>("JohnnyMouthSheet_2", allSprites.First(x => x.name == "JohnnyMouthSheet_2"));
            assetMan.Add<Sprite>("JohnnyMouthSheet_3", allSprites.First(x => x.name == "JohnnyMouthSheet_3"));
            assetMan.Add<Sprite>("JohnnyMouthSheet_4", allSprites.First(x => x.name == "JohnnyMouthSheet_4"));

            List<Texture2D> allTextures = Resources.FindObjectsOfTypeAll<Texture2D>().Where(x => x.GetInstanceID() >= 0).ToList();
            assetMan.Add("SaloonWall",allTextures.Find(x => x.name == "SaloonWall"));
            assetMan.Add("Carpet", allTextures.Find(x => x.name == "Carpet"));
            assetMan.Add("CeilingNoLight", allTextures.Find(x => x.name == "CeilingNoLight"));
            assetMan.Add<SoundObject>("TeleportSound", (SoundObject)(((ITM_Teleporter)ItemMetaStorage.Instance.FindByEnum(Items.Teleporter).value.item).ReflectionGetVariable("audTeleport")));
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
                .SetDuplicateOddsMultiplier(0.9f)
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

            new StickerBuilder<ExtendedStickerData>(Info)
                .SetEnum(stickerEnums["PizzaBonus"])
                .SetSprite(assetMan.Get<Sprite>("Sticker_Pizza"))
                .SetDuplicateOddsMultiplier(0.85f)
                .SetAsAffectingGenerator()
                .SetAsBonusSticker()
                .Build();

            new StickerBuilder<ExtendedStickerData>(Info)
                .SetEnum(stickerEnums["PraiseTimeSlow"])
                .SetSprite(assetMan.Get<Sprite>("Sticker_PraiseTimeSlow"))
                .SetDuplicateOddsMultiplier(0.9f)
                .Build();

            new StickerBuilder<ExtendedStickerData>(Info)
                .SetEnum(stickerEnums["QuarterChance"])
                .SetSprite(assetMan.Get<Sprite>("Sticker_QuarterChance"))
                .SetDuplicateOddsMultiplier(0.9f)
                .Build();

            new StickerBuilder<ExtendedStickerData>(Info)
                .SetEnum(stickerEnums["Favoritism"])
                .SetSprite(assetMan.Get<Sprite>("Sticker_Favoritism"))
                .SetDuplicateOddsMultiplier(0.7f)
                .Build();

            new StickerBuilder<ExtendedStickerData>(Info)
                .SetEnum(stickerEnums["ShorterEvents"])
                .SetSprite(assetMan.Get<Sprite>("Sticker_ShorterEvents"))
                .SetDuplicateOddsMultiplier(0.8f)
                .Build();

            new StickerBuilder<ExtendedStickerData>(Info)
                .SetEnum(stickerEnums["IceEyes"])
                .SetSprite(assetMan.Get<Sprite>("Sticker_IceEyes"))
                .SetDuplicateOddsMultiplier(0.8f)
                .Build();

            new StickerBuilder<ExtendedStickerData>(Info)
                .SetEnum(stickerEnums["MoveResist"])
                .SetSprite(assetMan.Get<Sprite>("Sticker_MoveResist"))
                .SetDuplicateOddsMultiplier(0.9f)
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

            new StickerBuilder<GumDaredevilStickerData>(Info)
                .SetEnum(stickerEnums["Daredevil_Gum"])
                .SetSprite(assetMan.Get<Sprite>("DaredevilSticker_Gum"))
                .SetTags("tms_daredevil", "tms_daredevil_allowleave")
                .SetValueCap(7) // any higher either stops movement completely or results in negative movespeed
                .SetDuplicateOddsMultiplier(0.9f)
                .Build();

            new StickerBuilder<DaredevilStickerData>(Info)
                .SetEnum(stickerEnums["Daredevil_Dud"])
                .SetSprite(assetMan.Get<Sprite>("DaredevilSticker_Dud"))
                .SetTags("tms_daredevil")
                .Build();

            new StickerBuilder<DaredevilStickerData>(Info)
                .SetEnum(stickerEnums["Daredevil_ItemUseAntiBonus"])
                .SetSprite(assetMan.Get<Sprite>("DaredevilSticker_ItemUseAntiBonus"))
                .SetTags("tms_daredevil")
                .SetAsBonusSticker()
                .Build();

            yield return "Creating daredevil sticker pack...";
            ItemObject stickerTemplate = ItemMetaStorage.Instance.FindByEnum(Items.StickerPack).value;
            ItemObject daredevilPack = new ItemBuilder(Info)
                .SetEnum(Items.StickerPack)
                .SetShopPrice(400)
                .SetGeneratorCost(stickerTemplate.value)
                .SetAsInstantUse()
                .SetSprites(stickerTemplate.itemSpriteSmall, stickerTemplate.itemSpriteLarge)
                .SetNameAndDescription("Itm_StickerPack", "Desc_StickerPack_Daredevil")
                .SetItemComponent<ITM_StickerPack>()
                .Build();

            ITM_StickerPack daredevilStickerItem = ((ITM_StickerPack)daredevilPack.item);
            daredevilStickerItem.ReflectionSetVariable("type", DaredevilStickerPack);
            daredevilStickerItem.ReflectionSetVariable("total", 3);
            assetMan.Add<ItemObject>("daredevilPack", daredevilPack);

            yield return "Creating misc...";
            ItemObject pizzaItem = new ItemBuilder(Info)
                .SetEnum("PizzaBonus")
                .SetShopPrice(100)
                .SetAsInstantUse()
                .SetSprites(assetMan.Get<Sprite>("PizzaItem"), assetMan.Get<Sprite>("PizzaItem"))
                .SetItemComponent<PizzaItem>()
                .Build();

            ((PizzaItem)pizzaItem.item).eatSound = Resources.FindObjectsOfTypeAll<SoundObject>().First(x => x.name == "CartoonEating");

            GameObject pizzaBuilderObject = new GameObject("PizzaBuilder");
            pizzaBuilderObject.ConvertToPrefab(true);
            PizzaBuilder builder = pizzaBuilderObject.AddComponent<PizzaBuilder>();
            builder.pizza = pizzaItem;
            assetMan.Add("Pizza", pizzaItem);
            assetMan.Add<StructureBuilder>("PizzaBuilder", builder);

            yield return "Modifying metadata...";
            StickerMetaStorage.Instance.Get(Sticker.GlueStick).tags.Add("tms_always_in_stickerpack_sticker");
            StickerMetaStorage.Instance.Get(Sticker.Stamina).tags.Add("tms_always_in_stickerpack_sticker");
            // add dareboosts to any that i think deserve them
            StickerMetaStorage.Instance.Get(Sticker.InventorySlot).tags.Add("tms_dareboost");
            StickerMetaStorage.Instance.Get(Sticker.YtpMulitplier).tags.Add("tms_dareboost");
            StickerMetaStorage.Instance.Get(Sticker.BaldiPraise).tags.Add("tms_dareboost");
            StickerMetaStorage.Instance.Get(Sticker.Elevator).tags.Add("tms_dareboost");
            StickerMetaStorage.Instance.Get(Sticker.StickerBonus).tags.Add("tms_never_in_stickerpack_sticker");

            yield return "Modifying pitstop...";
            RoomAsset pitstopRoomAsset = Resources.FindObjectsOfTypeAll<RoomAsset>().First(x => ((UnityEngine.Object)x).name == "Room_JohnnysStore");
            Transform roomBase = pitstopRoomAsset.roomFunctionContainer.transform.Find("RoomBase");

            // unity is stupid so i can't pass in "roomBase.Find("Stickers")" into Instantiate like how i'd like.
            roomBase.Find("Stickers").Find("StickerPickup_8").gameObject.SetActive(false);
            Pickup stickerPickupClone = GameObject.Instantiate<Pickup>(roomBase.Find("Stickers").Find("StickerPickup_8").GetComponent<Pickup>());
            roomBase.Find("Stickers").Find("StickerPickup_8").gameObject.SetActive(true);
            stickerPickupClone.transform.SetParent(roomBase.Find("Stickers"), false);
            stickerPickupClone.gameObject.SetActive(true);
            stickerPickupClone.name = "StickerPickup_Daredevil";
            stickerPickupClone.item = daredevilPack;
            stickerPickupClone.transform.localPosition = new Vector3(60f, 5f, 47f);
            StoreRoomFunction storeRF = pitstopRoomAsset.roomFunctionContainer.GetComponent<StoreRoomFunction>();
            FieldInfo _stickerPickup = AccessTools.Field(typeof(StoreRoomFunction), "stickerPickup");
            _stickerPickup.SetValue(storeRF, ((Pickup[])_stickerPickup.GetValue(storeRF)).AddToArray(stickerPickupClone));

            yield return "Finalizing...";
            if (Chainloader.PluginInfos.ContainsKey("mtm101.rulerp.baldiplus.levelstudioloader"))
            {
                EditorAndLoaderSupport.AddLoaderSupport();
                if (Chainloader.PluginInfos.ContainsKey("mtm101.rulerp.baldiplus.levelstudio"))
                {
                    EditorAndLoaderSupport.AddStudioSupport();
                }
            }
        }
    }
}
