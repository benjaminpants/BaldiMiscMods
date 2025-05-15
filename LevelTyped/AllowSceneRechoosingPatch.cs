using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using MTM101BaldAPI;
using UnityEngine;

namespace LevelTyped
{
    [HarmonyPatch(typeof(GameInitializer))]
    [HarmonyPatch("GetControlledRandomLevelData")]
    [ConditionalPatchConfig("mtm101.rulerp.baldiplus.leveltyped","Technical", "Use Scene Choosing Patch")]
    static class AllowSceneRechoosingPatch
    {
        static bool Prefix(GameInitializer __instance, SceneObject ___sceneObject, ref LevelObject __result)
        {
            if (LevelTypedPlugin.Instance.oneStyleMode.Value)
            {
                System.Random chosenRandom = new System.Random(Singleton<CoreGameManager>.Instance.Seed());
                LevelType chosenType = LevelTypedPlugin.Instance.validLevelTypes[chosenRandom.Next(0, LevelTypedPlugin.Instance.validLevelTypes.Count)];
                __result = ___sceneObject.GetCustomLevelObjects().First(x => x.type == chosenType);
                if (__result == null)
                {
                    __result = WeightedSelection<LevelObject>.ControlledRandomSelection(___sceneObject.randomizedLevelObject, new System.Random(Singleton<CoreGameManager>.Instance.Seed() + ___sceneObject.levelNo));
                }
                return false;
            }


            List<LevelType> types = new List<LevelType>();
            // only care about the past 3 levels
            for (int i = Mathf.Max(___sceneObject.previousLevels.Length - LevelTypedPlugin.Instance.levelLookBehind.Value, 0); i < ___sceneObject.previousLevels.Length; i++)
            {
                // simulate the logic below but for previous level objects within our allowed range
                if (___sceneObject.previousLevels[i].randomizedLevelObject.Length != 0)
                {
                    System.Random rng = new System.Random(Singleton<CoreGameManager>.Instance.Seed() + ___sceneObject.previousLevels[i].levelNo);
                    List<WeightedLevelObject> weightedObjects = new List<WeightedLevelObject>();
                    for (int j = 0; j < ___sceneObject.previousLevels[i].randomizedLevelObject.Length; j++)
                    {
                        if (!types.Contains(___sceneObject.previousLevels[i].randomizedLevelObject[j].selection.type))
                        {
                            weightedObjects.Add(___sceneObject.previousLevels[i].randomizedLevelObject[j]);
                        }
                    }
                    if (weightedObjects.Count == 0)
                    {
                        weightedObjects = ___sceneObject.previousLevels[i].randomizedLevelObject.ToList();
                    }
                    weightedObjects.ControlledShuffle(rng);
                    types.Add(WeightedSelection<LevelObject>.ControlledRandomSelectionList(WeightedLevelObject.Convert(weightedObjects), rng).type);
                    // this is bad but im doing it anyway because im lazy
                    if (LevelTypedPlugin.Instance.ignoreSchoolhouse.Value)
                    {
                        types.Remove(LevelType.Schoolhouse); // who cares how many schoolhouses you get in a row
                    }
                }
                else if (___sceneObject.levelObject != null)
                {
                    types.Add(___sceneObject.levelObject.type);
                }
            }
            if (LevelTypedPlugin.Instance.ignoreSchoolhouse.Value)
            {
                types.Remove(LevelType.Schoolhouse); // who cares how many schoolhouses you get in a row
            }
            System.Random rng2 = new System.Random(Singleton<CoreGameManager>.Instance.Seed() + ___sceneObject.levelNo);
            List<WeightedLevelObject> finalOptions = new List<WeightedLevelObject>();
            for (int k = 0; k < ___sceneObject.randomizedLevelObject.Length; k++)
            {
                if (!types.Contains(___sceneObject.randomizedLevelObject[k].selection.type))
                {
                    finalOptions.Add(___sceneObject.randomizedLevelObject[k]);
                }
            }
            // just incase something has invalidated all of our options
            if (finalOptions.Count == 0)
            {
                finalOptions = ___sceneObject.randomizedLevelObject.ToList();
            }
            finalOptions.ControlledShuffle(rng2);
            __result = WeightedSelection<LevelObject>.ControlledRandomSelectionList(WeightedLevelObject.Convert(finalOptions), rng2);
            return false;
        }
    }
}
