using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace BuffedKeys
{
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi")]
    [BepInPlugin("mtm101.rulerp.baldiplus.buffedkeys", "Buffed Keys", "1.0.0.1")]
    public class BuffedKeysPlugin : BaseUnityPlugin
    {
        void Awake()
        {
            ModdedSaveGame.AddSaveHandler(Info);
            LoadingEvents.RegisterOnAssetsLoaded(Info, MinorTweaks(), LoadingEventOrder.Post);
            Harmony harmony = new Harmony("mtm101.rulerp.baldiplus.buffedkeys");
            harmony.PatchAllConditionals();
        }

        IEnumerator MinorTweaks()
        {
            yield return 1;
            yield return "Modifying keys...";
            ItemMetaStorage.Instance.FindByEnum(Items.DetentionKey).itemObjects.Do(x => x.value += 10); // increase all key itemObjects value by 10
        }
    }
}
