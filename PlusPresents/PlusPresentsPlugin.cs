using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.ObjectCreation;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace PlusPresents
{
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi")]
    [BepInDependency("mtm101.rulerp.baldiplus.levelstudio", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("mtm101.rulerp.baldiplus.levelstudioloader", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin("mtm101.rulerp.baldiplus.pluspresents", "Plus Presents!", "1.0.0.0")]
    public class PlusPresentsPlugin : BaseUnityPlugin
    {
        public static ItemObject presentItem; // I could use an AssetManager but this mod is so small it isn't really necessary
        public static Sticker betterPresentSticker;
        public static PlusPresentsPlugin Instance;

        public static ItemObject GetRandomItem()
        {
            WeightedItemObject[] weightedItems;
            if (Singleton<CoreGameManager>.Instance.sceneObject.storeUsesNextLevelData)
            {
                weightedItems = Singleton<CoreGameManager>.Instance.sceneObject.nextLevel.shopItems;
            }
            else
            {
                weightedItems = Singleton<CoreGameManager>.Instance.sceneObject.shopItems;
            }
            if ((weightedItems == null) || (weightedItems.Length == 0))
            {
                weightedItems = Singleton<BaseGameManager>.Instance.levelObject.potentialItems;
            }
            if (weightedItems == null) return null;
            if (weightedItems.Length == 0) return null;
            weightedItems = weightedItems.Where(ItemShouldBeIncluded).Select(x => new WeightedItemObject()
            {
                selection = x.selection,
                weight = x.weight
            }).ToArray();
            float goodWeightMultiplier = 0.015f + (StickerManager.Instance.StickerValue(betterPresentSticker) * 0.20f);
            for (int i = 0; i < weightedItems.Length; i++)
            {
                float additionalWeight = (weightedItems[i].selection.price / 10f) + (weightedItems[i].selection.value * (weightedItems[i].selection.GetMeta().tags.Contains("presents_lessvalue") ? 1f : 2f));
                weightedItems[i].weight = Mathf.RoundToInt(Mathf.Lerp(weightedItems[i].weight, additionalWeight, goodWeightMultiplier));
            }
            return WeightedItemObject.RandomSelection(weightedItems);
        }

        public static bool ItemShouldBeIncluded(WeightedItemObject data)
        {
            if (!data.selection.addToInventory) return false;
            ItemMetaData meta = data.selection.GetMeta();
            if (meta == null) return false;
            if (meta.flags.HasFlag(ItemFlags.InstantUse)) return false;
            if (meta.flags.HasFlag(ItemFlags.Unobtainable)) return false;
            return !meta.tags.Contains("presents_nopresent");
        }

        void Awake()
        {
            GeneratorManagement.Register(this, GenerationModType.Addend, GeneratorChanges);
            ModdedSaveGame.AddSaveHandler(Info);
            LoadingEvents.RegisterOnAssetsLoaded(Info, LoadEnumerator(), LoadingEventOrder.Pre);
            Harmony harmony = new Harmony("mtm101.rulerp.baldiplus.pluspresents");
            harmony.PatchAllConditionals();
            Instance = this;
        }

        IEnumerator LoadEnumerator()
        {
            bool studioInstalled = Chainloader.PluginInfos.ContainsKey("mtm101.rulerp.baldiplus.levelstudio");
            bool loaderInstalled = Chainloader.PluginInfos.ContainsKey("mtm101.rulerp.baldiplus.levelstudioloader");
            yield return 3 + (studioInstalled ? 1 : 0) + (loaderInstalled ? 1 : 0);
            yield return "Loading present and sticker...";
            Sprite presentSprite = AssetFinder.FindOfTypeWithName<Sprite>("PresentIcon_Large", true);
            presentItem = new ItemBuilder(Info)
                .SetEnum("PlusPresent")
                .SetItemComponent<Item>()
                .SetGeneratorCost(45)
                .SetShopPrice(600)
                .SetNameAndDescription("Itm_PlusPresent", "Desc_PlusPresent")
                .SetSprites(AssetLoader.SpriteFromMod(this, Vector2.one / 2f, 25f, "PresentIcon_Small.png"), presentSprite)
                .SetPickupSound(AssetFinder.FindOfTypeWithName<SoundObject>("StickerPacket_Open", true))
                .SetAsInstantUse()
                .Build();

            ExtendedStickerData data = new StickerBuilder<ExtendedStickerData>(Info)
                .SetEnum("BetterPresents")
                .SetDuplicateOddsMultiplier(0.8f)
                .SetSprite(AssetLoader.SpriteFromMod(this, Vector2.one / 2f, 1f, "Sticker_BetterPresents.png"))
                .Build();

            betterPresentSticker = data.sticker;

            yield return "Loading localization...";
            AssetLoader.LocalizationFromFile(Path.Combine(AssetLoader.GetModPath(this), "Localization.json"), Language.English);
            yield return "Modifying meta...";
            ItemMetaStorage.Instance.FindByEnum(Items.Apple).tags.Add("presents_lessvalue");
            if (!loaderInstalled) yield break;
            yield return "Adding to loader...";
            EditorAndLoaderSupport.AddLoaderSupport();
            if (!studioInstalled) yield break;
            yield return "Adding to studio...";
            EditorAndLoaderSupport.AddStudioSupport();
        }

        void GeneratorChanges(string levelName, int levelId, SceneObject obj)
        {
            CustomLevelObject[] levels = obj.GetCustomLevelObjects();
            foreach (CustomLevelObject level in levels)
            {
                if (level.IsModifiedByMod(Info)) return;
                level.potentialItems = level.potentialItems.AddToArray(new WeightedItemObject
                {
                    selection = presentItem,
                    weight = 70
                });
                level.forcedItems.Add(presentItem);
                level.MarkAsModifiedByMod(Info);
            }
            if (obj.shopItems != null)
            {
                obj.shopItems = obj.shopItems.AddToArray(new WeightedItemObject
                {
                    selection = presentItem,
                    weight = 40
                });
            }
            if (obj.potentialStickers != null)
            {
                obj.potentialStickers = obj.potentialStickers.AddToArray(new WeightedSticker(betterPresentSticker, 90));
            }
        }
    }
}
