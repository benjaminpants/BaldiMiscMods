using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.ObjectCreation;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;
using UnityEngine;

namespace AllCharactersEveryFloor
{
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi")]
    [BepInPlugin("mtm101.rulerp.bbplus.allcharacterseveryfloor", "All Characters Every Floor", "1.2.0.0")]
    public class AllCharactersEveryFloorPlugin : BaseUnityPlugin
    {
        void Awake()
        {
            GeneratorManagement.Register(this, GenerationModType.Finalizer, GeneratorChanges);
            /*LoadingEvents.RegisterOnAssetsLoaded(this.Info, () =>
            {
                Resources.FindObjectsOfTypeAll<CustomLevelObject>().Do(x =>
                {
                    GeneratorChanges("0",0, x);
                });
            }, true);*/
            ModdedSaveGame.AddSaveHandler(this.Info);
        }

        void GeneratorChanges(string levelName, int levelId, SceneObject obj)
        {
            /*List<NPC> allPossibleNPCs = new List<NPC>();
            allPossibleNPCs.AddRange(obj.previousLevels.SelectMany(x => x.potentialNPCs).Select(x => x.selection));
            obj.potentialNPCs.RemoveAll(x => allPossibleNPCs.Contains(x.selection));
            obj.forcedNpcs = obj.forcedNpcs.AddRangeToArray(obj.potentialNPCs.Select(x => x.selection).Distinct().ToArray());
            obj.potentialNPCs.Clear();
            obj.additionalNPCs = 0;
            */
            obj.additionalNPCs = int.MaxValue;
        }
    }
}