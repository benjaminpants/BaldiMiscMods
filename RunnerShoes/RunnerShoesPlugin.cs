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
using UnityEngine;

namespace RunnerShoes
{
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi")]
    [BepInDependency("mtm101.rulerp.baldiplus.levelstudio", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("mtm101.rulerp.baldiplus.levelstudioloader", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin("mtm101.rulerp.baldiplus.runnershoes", "Runner Shoes", "1.0.0.0")]
    public class RunnerShoesPlugin : BaseUnityPlugin
    {
        public static ItemObject runnerShoes; // I could use an AssetManager but this mod is so small it isn't really necessary
        void Awake()
        {
            GeneratorManagement.Register(this, GenerationModType.Addend, GeneratorChanges);
            ModdedSaveGame.AddSaveHandler(Info);
            LoadingEvents.RegisterOnAssetsLoaded(Info, LoadEnumerator(), LoadingEventOrder.Pre);
        }

        IEnumerator LoadEnumerator()
        {
            bool studioInstalled = Chainloader.PluginInfos.ContainsKey("mtm101.rulerp.baldiplus.levelstudio");
            bool loaderInstalled = Chainloader.PluginInfos.ContainsKey("mtm101.rulerp.baldiplus.levelstudioloader");
            yield return 2 + (studioInstalled ? 1 : 0) + (loaderInstalled ? 1 : 0);
            yield return "Loading shoes...";
            Sprite runningShoesSmall = AssetLoader.SpriteFromMod(this, Vector2.one / 2f, 25f, "RunnerShoes_Small.png");
            runnerShoes = new ItemBuilder(Info)
                .SetEnum("RunnerShoes")
                .SetItemComponent<ITM_RunnerShoes>()
                .SetGeneratorCost(77)
                .SetShopPrice(600)
                .SetNameAndDescription("Itm_RunnerShoes", "Desc_RunnerShoes")
                .SetSprites(runningShoesSmall, AssetLoader.SpriteFromMod(this, Vector2.one / 2f, 50f, "RunnerShoes_Large.png"))
                .Build();
            ITM_RunnerShoes shoeItem = (ITM_RunnerShoes)runnerShoes.item;
            shoeItem.setTime = 90f;
            shoeItem.gaugeSprite = runningShoesSmall;
            yield return "Loading localization...";
            AssetLoader.LocalizationFromFile(Path.Combine(AssetLoader.GetModPath(this), "Localization.json"), Language.English);
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
                    selection = runnerShoes,
                    weight = 50
                });
                level.MarkAsModifiedByMod(Info);
            }
            if (obj.shopItems != null)
            {
                obj.shopItems = obj.shopItems.AddToArray(new WeightedItemObject
                {
                    selection = runnerShoes,
                    weight = 25
                });
            }
        }
    }
}
