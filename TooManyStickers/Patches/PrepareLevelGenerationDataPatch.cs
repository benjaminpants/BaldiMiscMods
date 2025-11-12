using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TooManyStickers.Patches
{
    [HarmonyPatch(typeof(BaseGameManager))]
    [HarmonyPatch("PrepareLevelGenerationData")]
    class PrepareLevelGenerationDataPatch
    {
        static void Prefix(LevelGenerationParameters ___levelObject)
        {
            if (Singleton<StickerManager>.Instance.StickerValue(TooManyStickersPlugin.stickerEnums["MapShrink"]) > 0)
            {
                float percentage = Singleton<StickerManager>.Instance.StickerValue(TooManyStickersPlugin.stickerEnums["MapShrink"]) * 0.09f;
                ___levelObject.minSize = new IntVector2(Mathf.CeilToInt(___levelObject.minSize.x - (___levelObject.minSize.x * percentage)), Mathf.CeilToInt(___levelObject.minSize.z - (___levelObject.minSize.z * percentage)));
                ___levelObject.maxSize = new IntVector2(Mathf.CeilToInt(___levelObject.maxSize.x - (___levelObject.maxSize.x * percentage)), Mathf.CeilToInt(___levelObject.maxSize.z - (___levelObject.maxSize.z * percentage)));
            }
            // add vents
            if (Singleton<StickerManager>.Instance.StickerValue(TooManyStickersPlugin.stickerEnums["AddVents"]) > 0)
            {
                int ventCount = Singleton<StickerManager>.Instance.StickerValue(TooManyStickersPlugin.stickerEnums["AddVents"]);
                ___levelObject.forcedStructures = ___levelObject.forcedStructures.AddToArray(new StructureWithParameters()
                {
                    prefab = TooManyStickersPlugin.Instance.assetMan.Get<Structure_Vent>("Structure_Vent"),
                    parameters = new StructureParameters()
                    {
                        chance = new float[0],
                        prefab = new WeightedGameObject[0],
                        minMax = new IntVector2[]
                        {
                            new IntVector2(ventCount * 3, ventCount * 3),
                            new IntVector2(2,5),
                            new IntVector2(22 + ventCount, 22 + ventCount)
                        }
                    }
                });
            }
        }
    }

    [HarmonyPatch(typeof(BaseGameManager))]
    [HarmonyPatch("PrepareLevelGenerationModifier")]
    class PrepareLevelGenerationModifierPatch
    {
        static void Prefix(LevelGenerationModifier ___levelGenerationModifier)
        {
            // TODO: figure out how to cap locked room count
            if (Singleton<StickerManager>.Instance.StickerValue(TooManyStickersPlugin.stickerEnums["MoreLocks"]) > 0)
            {
                int roomCount = Singleton<StickerManager>.Instance.StickerValue(TooManyStickersPlugin.stickerEnums["MoreLocks"]);
                ___levelGenerationModifier.additionalRoomGroup.Add(new RoomGroup()
                {
                    wallTexture = new WeightedTexture2D[] { new WeightedTexture2D() { selection = TooManyStickersPlugin.Instance.assetMan.Get<Texture2D>("SaloonWall"), weight = 100 } },
                    floorTexture = new WeightedTexture2D[] { new WeightedTexture2D() { selection = TooManyStickersPlugin.Instance.assetMan.Get<Texture2D>("Carpet"), weight = 100 } },
                    ceilingTexture = new WeightedTexture2D[] { new WeightedTexture2D() { selection = TooManyStickersPlugin.Instance.assetMan.Get<Texture2D>("CeilingNoLight"), weight = 100 } },
                    light = new WeightedTransform[] { new WeightedTransform() { selection = TooManyStickersPlugin.Instance.assetMan.Get<Transform>("FluorescentLight"), weight = 100 } },
                    minRooms = roomCount,
                    maxRooms = roomCount,
                    name = "ExtraLockedRooms",
                    stickToHallChance = 1f,
                    potentialRooms = new WeightedRoomAsset[] {
                        new WeightedRoomAsset()
                        {
                            selection = TooManyStickersPlugin.Instance.assetMan.Get<RoomAsset>("Room_Faculty_School_Locked_0_NormalItems"),
                            weight = 100
                        },
                        new WeightedRoomAsset()
                        {
                            selection = TooManyStickersPlugin.Instance.assetMan.Get<RoomAsset>("Room_Faculty_School_Locked_0_HighEndItems"),
                            weight = 20
                        }
                    }
                });
            }
        }
    }
}
