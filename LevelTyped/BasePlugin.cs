using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.ObjectCreation;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;
using UnityEngine;
using BepInEx.Configuration;

namespace LevelTyped
{
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi")]
    [BepInPlugin("mtm101.rulerp.baldiplus.leveltyped", "Level Typed", "1.2.0.1")]
    public class LevelTypedPlugin : BaseUnityPlugin
    {
        public AssetManager assetMan = new AssetManager();

        public Dictionary<RoomAsset, RoomAsset> standardToMathMachine = new Dictionary<RoomAsset, RoomAsset>();

        private List<LevelTypedGenerator> extraGenerators = new List<LevelTypedGenerator>();

        internal List<LevelType> validLevelTypes = new List<LevelType>()
        {
            LevelType.Schoolhouse,
            LevelType.Factory,
            LevelType.Laboratory,
            LevelType.Maintenance
        };
        
        public void AddExtraGenerator(LevelTypedGenerator gen)
        {
            extraGenerators.Add(gen);
            validLevelTypes.Add(gen.myLevelType);
        }

        public ConfigEntry<int> levelLookBehind;
        public ConfigEntry<bool> ignoreSchoolhouse;
        public ConfigEntry<bool> vanillaScenesOnly;
        public ConfigEntry<bool> noFloor1Variants;
        public ConfigEntry<bool> oneStyleMode;

        public ConfigEntry<int> weightForNewStyles;
        public ConfigEntry<int> weightForOldSchoolStyle;
        public ConfigEntry<int> weightForNewSchoolStyle;

        public static LevelTypedPlugin Instance;

        public List<string> vanillaRoomGroups = new List<string>()
        {
            "Class",
            "Faculty",
            "Office",
            "LockedRoom"
        };

        void Awake()
        {
            Instance = this;
            GeneratorManagement.Register(this, GenerationModType.Preparation, GeneratorChanges);
            ModdedSaveGame.AddSaveHandler(this.Info);
            LoadingEvents.RegisterOnAssetsLoaded(Info, LoadEnumerator(), LoadingEventOrder.Pre);
            LoadingEvents.RegisterOnAssetsLoaded(Info, PostLoadEnumerator(), LoadingEventOrder.Post);

            vanillaScenesOnly = Config.Bind("General",
                "Vanilla BB+ ScenesObjects/Floors only",
                true,
                @"If false, the mod will attempt to add variants to modded floors and levels.
It is suggested to leave this at true.");

            noFloor1Variants = Config.Bind("General",
                "Disable Floor 1 Variants",
                true,
                @"If false, floor 1 will be able to have variants like the other floors.
Beware, glitches and jank galore!");

            levelLookBehind = Config.Bind("General",
                "Level Look Behind",
                2,
                @"How many levels behind a level will lookback to avoid duplicates.
0 to make levels not care about the previous levels, allowing for multiple of the same type in a row.
(The schoolhouse style will always allow for multiple in a row, unless 'Ignore Schoolhouse Style during Look Behind' is turned off)");

            ignoreSchoolhouse = Config.Bind("General",
                "Ignore Schoolhouse Style during Look Behind",
                true,
                @"Determines if the schoolhouse style will be ignored when levels are checking for duplicates.
It is suggested to leave this off, otherwise the schoolhouse style will be unable to occur multiple times in a row.");

            weightForNewStyles = Config.Bind("Weights",
                "New Style Weight",
                25,
                @"Determines the weight of the styles added to schoolhouse floors that didn't have those styles before.
Set to zero to disable adding new styles to schoolhouse floors.");

            weightForOldSchoolStyle = Config.Bind("Weights",
                "Old Schoolhouse Style Weight",
                250,
                @"Determines the weight of the schoolhouse style for floors that have it in vanilla BB+.
Set to zero to remove the schoolhouse style from schoolhouse floors. (EX: F1, F2, and F3)");

            weightForNewSchoolStyle = Config.Bind("Weights",
                "New Schoolhouse Weight",
                25,
                @"Determines the weight of schoolhouse styles added to floors that didn't have it before.
Set to zero to disable adding schoolhouse styles to floors that don't have one (EX: F4 and F5)");


            oneStyleMode = Config.Bind("Fun",
                "One Style Mode",
                false,
                @"If enabled, one style will be chosen each run based off the seed, and every floor on that seed wil be that style.
It is suggested to turn Floor 1 variants on for the full effect.");

            Config.Bind("Technical",
                "Use Scene Choosing Patch",
                true,
                @"Determines if the mod's patch for choosing the scene/level style is enabled. This patch allows multiple schoolhouse themes to be selected, makes it only care about a certain number of previous floors, and stops crashing if no valid types are found.
It is recommended to leave this to true, as turning it off will likely cause crashes. Exists for mod compatability if other mods patch the scene choosing method.");

            Harmony harmony = new Harmony("mtm101.rulerp.baldiplus.leveltyped");
            harmony.PatchAllConditionals();
        }

        IEnumerator LoadEnumerator()
        {
            yield return 3;
            yield return "Fetching resources...";
            assetMan.AddFromResourcesNoClones<StructureBuilder>();
            assetMan.AddFromResourcesNoClones<RoomAsset>();
            assetMan.AddFromResourcesNoClones<Cubemap>();
            assetMan.Add<Structure_LevelBox>("FactoryBoxButItsTheCorrectPrefabWHY", Resources.FindObjectsOfTypeAll<Structure_LevelBox>().First(x => x.name == "FactoryBoxConstructor" && x.GetInstanceID() >= 0 && x.ReflectionGetVariable("boxTransform") != null));
            assetMan.Add<GameObject>("LockdownDoor_TrapCheck", Resources.FindObjectsOfTypeAll<GameObject>().First(x => x.GetInstanceID() >= 0 && x.name == "LockdownDoor_TrapCheck"));
            assetMan.Add<GameObject>("LockdownDoor_Shut", Resources.FindObjectsOfTypeAll<GameObject>().First(x => x.GetInstanceID() >= 0 && x.name == "LockdownDoor_TrapCheck"));
            assetMan.Add<Texture2D>("Transparent", Resources.FindObjectsOfTypeAll<Texture2D>().First(x => x.GetInstanceID() >= 0 && x.name == "Transparent" && x.isReadable));
            assetMan.Add<Texture2D>("DiamondPlating", Resources.FindObjectsOfTypeAll<Texture2D>().First(x => x.GetInstanceID() >= 0 && x.name == "DiamongPlateFloor"));
            assetMan.Add<Texture2D>("ColoredBrickWall", Resources.FindObjectsOfTypeAll<Texture2D>().First(x => x.GetInstanceID() >= 0 && x.name == "ColoredBrickWall"));
            assetMan.Add<Texture2D>("MaintenanceFloor", Resources.FindObjectsOfTypeAll<Texture2D>().First(x => x.GetInstanceID() >= 0 && x.name == "MaintenanceFloor"));

            assetMan.Add<Texture2D>("LabWall_Texture", Resources.FindObjectsOfTypeAll<Texture2D>().First(x => x.GetInstanceID() >= 0 && x.name == "LabWall_Texture"));
            assetMan.Add<Texture2D>("LabFloor_Texture", Resources.FindObjectsOfTypeAll<Texture2D>().First(x => x.GetInstanceID() >= 0 && x.name == "LabFloor_Texture"));
            assetMan.Add<Texture2D>("LabCeiling_Texture", Resources.FindObjectsOfTypeAll<Texture2D>().First(x => x.GetInstanceID() >= 0 && x.name == "LabCeiling_Texture"));

            assetMan.Add<Texture2D>("ElCeiling", Resources.FindObjectsOfTypeAll<Texture2D>().First(x => x.GetInstanceID() >= 0 && x.name == "ElCeiling"));
            assetMan.Add<Transform>("CordedHangingLight", Resources.FindObjectsOfTypeAll<Transform>().First(x => x.GetInstanceID() >= 0 && x.name == "CordedHangingLight"));
            assetMan.Add<Transform>("CagedLight", Resources.FindObjectsOfTypeAll<Transform>().First(x => x.GetInstanceID() >= 0 && x.name == "CagedLight"));

            assetMan.Add<Texture2D>("Wall", Resources.FindObjectsOfTypeAll<Texture2D>().First(x => x.GetInstanceID() >= 0 && x.name == "Wall"));
            assetMan.Add<Texture2D>("TileFloor", Resources.FindObjectsOfTypeAll<Texture2D>().First(x => x.GetInstanceID() >= 0 && x.name == "TileFloor"));
            assetMan.Add<Texture2D>("Carpet", Resources.FindObjectsOfTypeAll<Texture2D>().First(x => x.GetInstanceID() >= 0 && x.name == "Carpet"));
            assetMan.Add<Texture2D>("BasicFloor", Resources.FindObjectsOfTypeAll<Texture2D>().First(x => x.GetInstanceID() >= 0 && x.name == "BasicFloor"));
            assetMan.Add<Texture2D>("CeilingNoLight", Resources.FindObjectsOfTypeAll<Texture2D>().First(x => x.GetInstanceID() >= 0 && x.name == "CeilingNoLight"));
            assetMan.Add<Texture2D>("PlasticTable", Resources.FindObjectsOfTypeAll<Texture2D>().First(x => x.GetInstanceID() >= 0 && x.name == "PlasticTable"));
            assetMan.Add<Transform>("FluorescentLight", Resources.FindObjectsOfTypeAll<Transform>().First(x => x.GetInstanceID() >= 0 && x.name == "FluorescentLight"));
            assetMan.Add<Transform>("HangingLight", Resources.FindObjectsOfTypeAll<Transform>().First(x => x.GetInstanceID() >= 0 && x.name == "HangingLight"));

            assetMan.AddFromResourcesNoClones<Activity>();
            yield return "Creating appropiate prefabs...";

            // create the materials first
            Material babyMMNormal = new Material(Resources.FindObjectsOfTypeAll<Material>().First(x => x.GetInstanceID() >= 0 && x.name == "math_front_normal"));
            babyMMNormal.SetMainTexture(AssetLoader.TextureFromMod(this, "MMJR_Front_Default.png"));
            Material babyMMWrong = new Material(Resources.FindObjectsOfTypeAll<Material>().First(x => x.GetInstanceID() >= 0 && x.name == "math_front_incorrect"));
            babyMMWrong.SetMainTexture(AssetLoader.TextureFromMod(this, "MMJR_Front_Wrong.png"));
            Material babyMMCorrect = new Material(Resources.FindObjectsOfTypeAll<Material>().First(x => x.GetInstanceID() >= 0 && x.name == "math_front_correct"));
            babyMMCorrect.SetMainTexture(AssetLoader.TextureFromMod(this, "MMJR_Front_Right.png"));

            MathMachine newMachine = GameObject.Instantiate<MathMachine>(assetMan.Get<MathMachine>("MathMachine"), MTM101BaldiDevAPI.prefabTransform);
            newMachine.name = "MathMachineBaby";
            newMachine.gameObject.AddComponent<MathMachineBabyMode>();
            newMachine.ReflectionSetVariable("normalPoints", 10);
            newMachine.ReflectionSetVariable("bonusPoints", 25);
            newMachine.ReflectionSetVariable("defaultMat", babyMMNormal);
            newMachine.ReflectionSetVariable("incorrectMat", babyMMWrong);
            newMachine.ReflectionSetVariable("correctMat", babyMMCorrect);
            assetMan.Add<Activity>("MathMachineBaby", newMachine);

            GameObject passEnsureObject = new GameObject("BusPassEnsurer");
            passEnsureObject.transform.SetParent(MTM101BaldiDevAPI.prefabTransform, false);
            Structure_BusPassEnsurer ensurer = passEnsureObject.AddComponent<Structure_BusPassEnsurer>();
            ensurer.busPass = ItemMetaStorage.Instance.FindByEnum(Items.BusPass).value;
            assetMan.Add<Structure_BusPassEnsurer>("BusPassEnsurer", ensurer);

            yield return "Creating RoomAssets...";
            RoomAsset[] playgrounds = Resources.FindObjectsOfTypeAll<RoomAsset>().Where(x => x.roomFunctionContainer != null).Where(x => x.GetInstanceID() >= 0 && x.roomFunctionContainer.name == "PlaygroundRoomFunction").ToArray();

            RoomFunctionContainer twilightContainer = GameObject.Instantiate<RoomFunctionContainer>(playgrounds[0].roomFunctionContainer, MTM101BaldiDevAPI.prefabTransform);
            twilightContainer.name = twilightContainer.name.Replace("(Clone)", "_Twilight");

            twilightContainer.GetComponent<SunlightRoomFunction>().color = new Color(0.9623f, 0.7254f, 0.4221f, 1f);

            for (int i = 0; i < playgrounds.Length; i++)
            {
                RoomAsset newPlayground = RoomAsset.Instantiate<RoomAsset>(playgrounds[i]);
                newPlayground.roomFunctionContainer = twilightContainer;
                ((UnityEngine.Object)newPlayground).name = ((UnityEngine.Object)newPlayground).name.Replace("(Clone)", "_Twilight");
                assetMan.Add<RoomAsset>(((UnityEngine.Object)newPlayground).name, newPlayground);
            }
        }

        IEnumerator PostLoadEnumerator()
        {
            yield return 1;
            yield return "Creating MM variants...";
            RoomAsset[] allAsset = Resources.FindObjectsOfTypeAll<RoomAsset>().Where(x => x.hasActivity).ToArray();
            for (int i = 0; i < allAsset.Length; i++)
            {
                CreateOrGetMathMachineVariantForAsset(allAsset[i]);
            }

        }

        RoomAsset CreateOrGetMathMachineVariantForAsset(RoomAsset asset)
        {
            if (!asset.hasActivity) return asset;
            if (!(asset.activity.prefab is NoActivity)) return asset;
            if (standardToMathMachine.ContainsKey(asset))
            {
                return standardToMathMachine[asset];
            }
            RoomAsset newAsset = RoomAsset.Instantiate<RoomAsset>(asset); //create a new copy
            ((UnityEngine.Object)newAsset).name = ((UnityEngine.Object)newAsset).name.Replace("(Clone)", "(MathMachineBaby)");
            if (newAsset.hasActivity)
            {
                newAsset.activity.prefab = assetMan.Get<Activity>("MathMachineBaby");
            }
            // now remove any objects that may intersect with the machine/are too close
            newAsset.basicObjects.RemoveAll(x => Vector3.Distance(x.position, newAsset.activity.position) <= 5f);
            newAsset.itemSpawnPoints.RemoveAll(x => Vector3.Distance(x.position, newAsset.activity.position) <= 5f);

            standardToMathMachine.Add(asset, newAsset);
            return newAsset;
        }

        public void AddBusPassEnsurerIfNecessary(CustomLevelObject clo)
        {
            for (int i = 0; i < clo.roomGroup.Length; i++)
            {
                RoomGroup group = clo.roomGroup[i];
                for (int j = 0; j < group.potentialRooms.Length; j++)
                {
                    if (group.potentialRooms[j].selection.itemList.Find(x => x.itemType == Items.BusPass))
                    {
                        clo.forcedStructures = clo.forcedStructures.AddItem(new StructureWithParameters()
                        {
                            parameters = new StructureParameters(),
                            prefab = assetMan.Get<Structure_BusPassEnsurer>("BusPassEnsurer")
                        }).ToArray();
                        return;
                    }
                }
            }
        }

        void SetupSceneObjectForExtraLevelsIfNecessary(SceneObject obj)
        {
            if ((obj.randomizedLevelObject == null) || (obj.randomizedLevelObject.Length == 0))
            {
                CustomLevelObject ogLevel = (CustomLevelObject)obj.levelObject;
                obj.randomizedLevelObject = new WeightedLevelObject[]
                {
                        new WeightedLevelObject()
                        {
                            weight = weightForOldSchoolStyle.Value,
                            selection = ogLevel
                        }
                };
                obj.levelObject = null;
            }
        }

        void ChangeLights(LevelObject lvlObj, string hallLights, string roomLights)
        {

            lvlObj.hallLights = new WeightedTransform[]
            {
                new WeightedTransform()
                {
                    selection=assetMan.Get<Transform>(hallLights),
                    weight=100,
                }
            };

            for (int i = 0; i < lvlObj.roomGroup.Length; i++)
            {
                if (vanillaRoomGroups.Contains(lvlObj.roomGroup[i].name))
                {
                    lvlObj.roomGroup[i].light = new WeightedTransform[]
                    {
                        new WeightedTransform()
                        {
                            selection=assetMan.Get<Transform>(roomLights),
                            weight=100,
                        }
                    };
                }
            }
        }

        void ExecuteTypedGenerators(string levelName, int levelId, SceneObject obj)
        {
            if (obj.levelObject != null) return; // we currently do not support this
            foreach (LevelTypedGenerator ltg in extraGenerators)
            {
                if (!ltg.ShouldGenerate(levelName, levelId, obj)) continue;
                CustomLevelObject oglObj = (CustomLevelObject)obj.randomizedLevelObject.Select(x => x.selection).FirstOrDefault(x => x.type == ltg.levelTypeToBaseOff);
                if (oglObj == null) continue;
                CustomLevelObject clone = oglObj.MakeClone();
                switch (clone.type)
                {
                    default:
                        throw new NotImplementedException("Unknown level type for renaming: " + clone.type);
                    case LevelType.Schoolhouse:
                        clone.name = clone.name.Replace("(Clone)", "").Replace("Schoolhouse", ltg.levelObjectName);
                        break;
                    case LevelType.Factory:
                        clone.name = clone.name.Replace("(Clone)", "").Replace("Factory", ltg.levelObjectName);
                        break;
                    case LevelType.Laboratory:
                        clone.name = clone.name.Replace("(Clone)", "").Replace("Laboratory", ltg.levelObjectName);
                        break;
                    case LevelType.Maintenance:
                        clone.name = clone.name.Replace("(Clone)", "").Replace("Maintenance", ltg.levelObjectName);
                        break;
                }
                ltg.ApplyChanges(levelName, levelId, clone); //WABAM!
                obj.randomizedLevelObject = obj.randomizedLevelObject.AddItem(new WeightedLevelObject()
                {
                    selection = clone,
                    weight = ltg.GetWeight(weightForNewStyles.Value)
                }).ToArray();
            }
        }

        void GeneratorChanges(string levelName, int levelId, SceneObject obj)
        {
            if (obj.GetMeta().tags.Contains("debug")) return; // dont create event test variants lol
            if (levelName == "END") return;
            if (vanillaScenesOnly.Value)
            {
                if (obj.GetMeta().info != MTM101BaldiDevAPI.Instance.Info) return; // not a vanilla level, ignore it
            }
            if (noFloor1Variants.Value)
            {
                if ((levelName == "F1") && (levelId == 0)) return;
            }
            LevelType[] supportedTypes = obj.GetMeta().GetSupportedLevelTypes();
            if (!supportedTypes.Contains(LevelType.Schoolhouse))
            {
                CustomLevelObject schoolhouseObject = null;
                if (supportedTypes.Contains(LevelType.Maintenance))
                {
                    schoolhouseObject = obj.GetCustomLevelObjects().First(x => x.type == LevelType.Maintenance).MakeClone();
                }
                // todo: figure out what to do if it doesn't support maintenance
                


                if (schoolhouseObject == null) return;

                schoolhouseObject.type = LevelType.Schoolhouse;
                schoolhouseObject.name = schoolhouseObject.name.Replace("(Clone)", "").Replace("Maintenance", "Schoolhouse");
                schoolhouseObject.specialRoomsStickToEdge = levelId >= 3;

                if (levelId >= 3)
                {
                    schoolhouseObject.minSize = new IntVector2(schoolhouseObject.minSize.x + (levelId / 2), schoolhouseObject.minSize.z + (levelId / 2));
                    schoolhouseObject.maxSize = new IntVector2(schoolhouseObject.minSize.x + (levelId), schoolhouseObject.minSize.z + (levelId));
                    schoolhouseObject.minPlots += levelId * 3;
                    schoolhouseObject.maxPlots += levelId * 4;
                    if (schoolhouseObject.potentialPrePlotSpecialHalls.Length != 0)
                    {
                        schoolhouseObject.maxPrePlotSpecialHalls += 3;
                    }
                    if (schoolhouseObject.potentialPostPlotSpecialHalls.Length != 0)
                    {
                        schoolhouseObject.maxPostPlotSpecialHalls += 3;
                    }
                }

                schoolhouseObject.hallWallTexs = new WeightedTexture2D[]
                {
                    new WeightedTexture2D()
                    {
                        weight=100,
                        selection=assetMan.Get<Texture2D>("Wall")
                    }
                };

                schoolhouseObject.hallCeilingTexs = new WeightedTexture2D[]
                {
                    new WeightedTexture2D()
                    {
                        weight=100,
                        selection=assetMan.Get<Texture2D>("CeilingNoLight")
                    },
                    new WeightedTexture2D()
                    {
                        weight=100,
                        selection=assetMan.Get<Texture2D>("PlasticTable")
                    }
                };

                schoolhouseObject.hallFloorTexs = new WeightedTexture2D[]
                {
                    new WeightedTexture2D()
                    {
                        weight=100,
                        selection=assetMan.Get<Texture2D>("BasicFloor")
                    },
                    new WeightedTexture2D()
                    {
                        weight=100,
                        selection=assetMan.Get<Texture2D>("Carpet")
                    },
                    new WeightedTexture2D()
                    {
                        weight=100,
                        selection=assetMan.Get<Texture2D>("TileFloor")
                    }
                };

                schoolhouseObject.maxLightDistance = 9;
                schoolhouseObject.standardLightColor = new Color(1f, 0.9412f - (levelId * 0.01f), 0.8667f - (levelId * 0.015f), 1f);
                schoolhouseObject.standardLightStrength = 10;

                schoolhouseObject.potentialSpecialRooms = new WeightedRoomAsset[]
                {
                    new WeightedRoomAsset()
                    {
                        weight=25,
                        selection=assetMan.Get<RoomAsset>("Room_Cafeteria_1")
                    },
                    new WeightedRoomAsset()
                    {
                        weight=25,
                        selection=assetMan.Get<RoomAsset>("Room_Cafeteria_2")
                    },
                    new WeightedRoomAsset()
                    {
                        weight=50,
                        selection=assetMan.Get<RoomAsset>("Room_Cafeteria_3")
                    },
                    new WeightedRoomAsset()
                    {
                        weight=100,
                        selection=assetMan.Get<RoomAsset>("Room_Cafeteria_Hard_1")
                    },
                    new WeightedRoomAsset()
                    {
                        weight=100,
                        selection=assetMan.Get<RoomAsset>("Room_Cafeteria_Hard_2")
                    },
                    new WeightedRoomAsset()
                    {
                        weight=100,
                        selection=assetMan.Get<RoomAsset>("Room_Library_1")
                    },
                    new WeightedRoomAsset()
                    {
                        weight=100,
                        selection=assetMan.Get<RoomAsset>("Room_Library_2")
                    },
                    new WeightedRoomAsset()
                    {
                        weight=100,
                        selection=assetMan.Get<RoomAsset>("Room_Library_3")
                    },
                    new WeightedRoomAsset()
                    {
                        weight=75,
                        selection=assetMan.Get<RoomAsset>("Room_Playground_1" + (levelId >= 3 ? "_Twilight" : ""))
                    },
                    new WeightedRoomAsset()
                    {
                        weight=75,
                        selection=assetMan.Get<RoomAsset>("Room_Playground_2" + (levelId >= 3 ? "_Twilight" : ""))
                    },
                    new WeightedRoomAsset()
                    {
                        weight=75,
                        selection=assetMan.Get<RoomAsset>("Room_Playground_3" + (levelId >= 3 ? "_Twilight" : ""))
                    }
                };

                if (levelId >= 4)
                {
                    schoolhouseObject.minSpecialRooms = 3;
                    schoolhouseObject.maxSpecialRooms = 3;
                }
                else
                {
                    schoolhouseObject.minSpecialRooms = 2;
                    schoolhouseObject.maxSpecialRooms = 2;
                }

                RoomGroup officeGroup = schoolhouseObject.roomGroup.First(x => x.name == "Office");
                if (officeGroup != null)
                {
                    officeGroup.maxRooms += 1;
                }

                ChangeLights(schoolhouseObject, "FluorescentLight", "FluorescentLight");

                List<StructureWithParameters> schoolStructures = schoolhouseObject.forcedStructures.ToList();
                schoolStructures.RemoveAll(x => x.prefab is Structure_Vent);
                schoolStructures.RemoveAll(x => x.prefab is Structure_PowerLever);
                schoolStructures.RemoveAll(x => x.prefab is Structure_SteamValves);
                // increase the chance for all swinging door constructors
                schoolStructures.Where(x => x.prefab.name == "SwingingDoorConstructor").Do(x =>
                {
                    x.parameters.chance[0] += 0.1f;
                });
                schoolhouseObject.forcedStructures = schoolStructures.ToArray();

                schoolhouseObject.potentialStructures = new WeightedStructureWithParameters[]
                {
                    new WeightedStructureWithParameters()
                    {
                        weight=75,
                        selection=new StructureWithParameters()
                        {
                            parameters = new StructureParameters()
                            {
                                minMax = new IntVector2[]
                                {
                                    new IntVector2(1,Mathf.Max(levelId,1)),
                                    new IntVector2(0,12)
                                }
                            },
                            prefab = assetMan.Get<StructureBuilder>("Rotohall_Structure")
                        }
                    },
                    new WeightedStructureWithParameters()
                    {
                        weight=100,
                        selection=new StructureWithParameters()
                        {
                            parameters = new StructureParameters()
                            {
                                minMax = new IntVector2[]
                                {
                                    new IntVector2(2,3),
                                    new IntVector2(4,8)
                                },
                                chance = new float[] { 1f }
                            },
                            prefab = assetMan.Get<StructureBuilder>("SteamValveConstructor")
                        }
                    },
                    new WeightedStructureWithParameters()
                    {
                        weight=100,
                        selection=new StructureWithParameters()
                        {
                            parameters = new StructureParameters()
                            {
                                minMax = new IntVector2[]
                                {
                                    new IntVector2(2,3),
                                    new IntVector2(2,7),
                                    new IntVector2(20,20)
                                }
                            },
                            prefab = assetMan.Get<StructureBuilder>("Structure_Vent")
                        }
                    },
                    new WeightedStructureWithParameters()
                    {
                        weight=100,
                        selection=new StructureWithParameters()
                        {
                            parameters = new StructureParameters()
                            {
                                minMax = new IntVector2[]
                                {
                                    new IntVector2(8,12),
                                    new IntVector2(2,3),
                                },
                                chance = new float[] { 1f }
                            },
                            prefab = assetMan.Get<StructureBuilder>("LockdownDoorConstructor")
                        }
                    },
                    new WeightedStructureWithParameters()
                    {
                        weight=75,
                        selection=new StructureWithParameters()
                        {
                            parameters = new StructureParameters()
                            {
                                minMax = new IntVector2[]
                                {
                                    new IntVector2(8,6),
                                    new IntVector2(2,2),
                                },
                                chance = new float[] { 1f }
                            },
                            prefab = assetMan.Get<StructureBuilder>("FacultyOnlyDoorConstructor")
                        }
                    },
                    new WeightedStructureWithParameters()
                    {
                        weight=75,
                        selection=new StructureWithParameters()
                        {
                            parameters = new StructureParameters()
                            {
                                minMax = new IntVector2[]
                                {
                                    new IntVector2(2,3),
                                    new IntVector2(0,0),
                                    new IntVector2(3,3),
                                    new IntVector2(0,30),
                                },
                                chance = new float[] { 0f }
                            },
                            prefab = assetMan.Get<StructureBuilder>("PowerLeverConstructor")
                        }
                    }
                };
                schoolhouseObject.minSpecialBuilders = levelId;
                schoolhouseObject.maxSpecialBuilders = levelId;

                schoolhouseObject.maxItemValue += 150;

                schoolhouseObject.skybox = assetMan.Get<Cubemap>((levelId >= 3 ? "Cubemap_Twilight" : "Cubemap_DayStandard"));

                SetupSceneObjectForExtraLevelsIfNecessary(obj);
                obj.randomizedLevelObject = obj.randomizedLevelObject.AddToArray(new WeightedLevelObject()
                {
                    selection = schoolhouseObject,
                    weight = weightForNewSchoolStyle.Value
                });

                obj.randomizedLevelObject = obj.randomizedLevelObject.Where(x => x.weight > 0).ToArray();
                // we need to create the schoolhouse style just incase.

                ExecuteTypedGenerators(levelName, levelId, obj);

                return;
            }
            CustomLevelObject standardObject = obj.GetCustomLevelObjects().First(x => x.type == LevelType.Schoolhouse);
            if (!supportedTypes.Contains(LevelType.Factory))
            {
                // this applies to all levels
                CustomLevelObject factoryObject = standardObject.MakeClone();
                factoryObject.potentialStructures = new WeightedStructureWithParameters[] { };
                factoryObject.minSpecialBuilders = 0;
                factoryObject.maxSpecialBuilders = 0;
                factoryObject.name = factoryObject.name.Replace("(Clone)", "").Replace("Schoolhouse", "Factory");
                factoryObject.type = LevelType.Factory;

                // textures
                factoryObject.hallCeilingTexs = new WeightedTexture2D[]
                {
                    new WeightedTexture2D()
                    {
                        selection=assetMan.Get<Texture2D>("Transparent"),
                        weight=100
                    }
                };

                factoryObject.hallFloorTexs = new WeightedTexture2D[]
                {
                    new WeightedTexture2D()
                    {
                        selection=assetMan.Get<Texture2D>("DiamondPlating"),
                        weight=100
                    }
                };

                ChangeLights(factoryObject, "CordedHangingLight", "HangingLight");

                // add our builders to the very beginning of the list
                List<StructureWithParameters> structureList = factoryObject.forcedStructures.ToList();

                structureList.AddRange(new StructureWithParameters[]
                {
                    new StructureWithParameters()
                    {
                        parameters = new StructureParameters(),
                        prefab = assetMan.Get<StructureBuilder>("ConveyorBeltConstructor")
                    },
                    new StructureWithParameters()
                    {
                        parameters = new StructureParameters()
                        {
                            chance=new float[] { 0.5f },
                            prefab=new WeightedGameObject[] { 
                            new WeightedGameObject()
                            {
                                selection = assetMan.Get<GameObject>("LockdownDoor_TrapCheck"),
                                weight = 80
                            },
                            new WeightedGameObject()
                            {
                                selection = assetMan.Get<GameObject>("LockdownDoor_Shut"),
                                weight = 20
                            }
                            },
                            minMax = new IntVector2[]
                            {
                                new IntVector2(3,6),
                                new IntVector2(4,6)
                            }
                        },
                        prefab = assetMan.Get<StructureBuilder>("LockdownDoorConstructor")
                    },
                    new StructureWithParameters()
                    {
                        parameters = new StructureParameters()
                        {
                            minMax= new IntVector2[]
                            {
                                new IntVector2(1 + levelId,2 + levelId),
                                new IntVector2(0,6)
                            }
                        },
                        prefab=assetMan.Get<StructureBuilder>("Rotohall_Structure")
                    },
                    new StructureWithParameters()
                    {
                        parameters = new StructureParameters(),
                        prefab = assetMan.Get<StructureBuilder>("FactoryBoxButItsTheCorrectPrefabWHY")
                    }
                });
                factoryObject.forcedStructures = structureList.ToArray();

                // attempt to make things a little more crazy
                factoryObject.minPlots += 3;
                factoryObject.maxPlots += 4;
                // uncertain if this is actually contributing at all
                // but factory levels on 1 seem less horribly unstable when this is done
                if (levelName == "F1")
                {
                    factoryObject.minSize = new IntVector2(factoryObject.minSize.x + 5, factoryObject.minSize.z + 5);
                    factoryObject.maxSize = new IntVector2(factoryObject.minSize.x + 5, factoryObject.minSize.z + 5);
                }
                factoryObject.potentialSpecialRooms = new WeightedRoomAsset[0];
                factoryObject.minSpecialRooms = 0;
                factoryObject.maxSpecialRooms = 0;
                SetupSceneObjectForExtraLevelsIfNecessary(obj);
                obj.randomizedLevelObject = obj.randomizedLevelObject.AddToArray(new WeightedLevelObject()
                {
                    selection=factoryObject,
                    weight=weightForNewStyles.Value
                });
                obj.MarkAsNeverUnload();
            }

            if (!supportedTypes.Contains(LevelType.Maintenance))
            {
                // this applies to all levels
                CustomLevelObject maintenanceObject = standardObject.MakeClone();
                maintenanceObject.potentialStructures = new WeightedStructureWithParameters[] { };
                maintenanceObject.minSpecialBuilders = 0;
                maintenanceObject.maxSpecialBuilders = 0;
                maintenanceObject.name = maintenanceObject.name.Replace("(Clone)", "").Replace("Schoolhouse", "Maintenance");
                maintenanceObject.type = LevelType.Maintenance;

                maintenanceObject.potentialSpecialRooms = new WeightedRoomAsset[1]
                {
                    new WeightedRoomAsset()
                    {
                        selection=assetMan.Get<RoomAsset>("Room_LightbulbTesting_0")
                    }
                };

                if (levelName == "F1")
                {
                    maintenanceObject.forcedStructures = maintenanceObject.forcedStructures.AddToArray(new StructureWithParameters()
                    {
                        prefab=assetMan.Get<StructureBuilder>("Structure_Vent"),
                        parameters=new StructureParameters()
                        {
                            minMax=new IntVector2[]
                            {
                                new IntVector2(1,1),
                                new IntVector2(2,5),
                                new IntVector2(20,0)
                            }
                        }
                    });
                }
                else
                {
                    maintenanceObject.forcedStructures = maintenanceObject.forcedStructures.AddToArray(new StructureWithParameters()
                    {
                        prefab = assetMan.Get<StructureBuilder>("Structure_Vent"),
                        parameters = new StructureParameters()
                        {
                            minMax = new IntVector2[]
                            {
                                new IntVector2(2 + Mathf.Min(levelId,1),3 + levelId),
                                new IntVector2(2,5),
                                new IntVector2(20,0)
                            }
                        }
                    });
                }
                maintenanceObject.forcedStructures = maintenanceObject.forcedStructures.AddRangeToArray(new StructureWithParameters[]
                {
                    new StructureWithParameters()
                    {
                        prefab = assetMan.Get<StructureBuilder>("SteamValveConstructor"),
                        parameters = new StructureParameters()
                        {
                            minMax = new IntVector2[]
                            {
                                new IntVector2((levelId * 2),(levelId * 2) + 1),
                                new IntVector2(3,6 + Mathf.Min(levelId,8)),
                            },
                            chance=new float[] { 0.5f }
                        }
                    },
                    new StructureWithParameters()
                    {
                        prefab = assetMan.Get<StructureBuilder>("PowerLeverConstructor"),
                        parameters = new StructureParameters()
                        {
                            minMax = new IntVector2[]
                            {
                                new IntVector2(5,5),
                                new IntVector2(1,Mathf.Clamp(levelId,1,2)),
                                new IntVector2((levelName == "F1") ? 2 : 3,(levelName == "F1") ? 2 : 3),
                                new IntVector2(0,30 + (levelId * 10)),
                            },
                            chance=new float[] { (levelName == "F1") ? 0f : 0.12f }
                        }
                    }
                });
                maintenanceObject.minSpecialRooms = 1;
                maintenanceObject.maxSpecialRooms = 1;

                ChangeLights(maintenanceObject, "CagedLight", "CagedLight");

                maintenanceObject.standardLightColor = new Color(0.8774f, 0.818f, 0.4759f, 1f);
                maintenanceObject.maxLightDistance -= 1;
                maintenanceObject.standardLightStrength = 7;

                maintenanceObject.hallWallTexs = new WeightedTexture2D[1]
                {
                    new WeightedTexture2D()
                    {
                        weight=100,
                        selection=assetMan.Get<Texture2D>("ColoredBrickWall")
                    }
                };
                maintenanceObject.hallFloorTexs = new WeightedTexture2D[1]
                {
                    new WeightedTexture2D()
                    {
                        weight=100,
                        selection=assetMan.Get<Texture2D>("MaintenanceFloor")
                    }
                };
                maintenanceObject.hallCeilingTexs = new WeightedTexture2D[1]
                {
                    new WeightedTexture2D()
                    {
                        weight=100,
                        selection=assetMan.Get<Texture2D>("ElCeiling")
                    }
                };

                RoomGroup classGroup = maintenanceObject.roomGroup.First(x => x.name == "Class");
                for (int i = 0; i < classGroup.potentialRooms.Length; i++)
                {
                    classGroup.potentialRooms[i].selection = CreateOrGetMathMachineVariantForAsset(classGroup.potentialRooms[i].selection);
                }

                SetupSceneObjectForExtraLevelsIfNecessary(obj);
                obj.randomizedLevelObject = obj.randomizedLevelObject.AddToArray(new WeightedLevelObject()
                {
                    selection = maintenanceObject,
                    weight = weightForNewStyles.Value
                });

                obj.MarkAsNeverUnload();
            }

            if (!supportedTypes.Contains(LevelType.Laboratory))
            {
                CustomLevelObject laboratoryObject = standardObject.MakeClone();
                laboratoryObject.potentialStructures = new WeightedStructureWithParameters[] { };
                laboratoryObject.minSpecialBuilders = 0;
                laboratoryObject.maxSpecialBuilders = 0;
                laboratoryObject.name = laboratoryObject.name.Replace("(Clone)", "").Replace("Schoolhouse", "Laboratory");
                laboratoryObject.type = LevelType.Laboratory;

                laboratoryObject.potentialSpecialRooms = new WeightedRoomAsset[0];
                laboratoryObject.minSpecialRooms = 0;
                laboratoryObject.maxSpecialRooms = 0;

                laboratoryObject.hallWallTexs = new WeightedTexture2D[]
                {
                    new WeightedTexture2D()
                    {
                        weight=100,
                        selection=assetMan.Get<Texture2D>("LabWall_Texture")
                    }
                };
                laboratoryObject.hallFloorTexs = new WeightedTexture2D[]
                {
                    new WeightedTexture2D()
                    {
                        weight=100,
                        selection=assetMan.Get<Texture2D>("LabFloor_Texture")
                    }
                };
                laboratoryObject.hallCeilingTexs = new WeightedTexture2D[]
                {
                    new WeightedTexture2D()
                    {
                        weight=100,
                        selection=assetMan.Get<Texture2D>("LabCeiling_Texture")
                    }
                };
                // the constructor NEEDS to be at the start. otherwise, bad things. BADDD THINGS...
                List<StructureWithParameters> structureList = laboratoryObject.forcedStructures.ToList();
                structureList.Insert(0, new StructureWithParameters()
                {
                    parameters = new StructureParameters(),
                    prefab = assetMan.Get<StructureBuilder>("TeleporterRoomConstructor")
                });
                laboratoryObject.forcedStructures = structureList.ToArray();
                AddBusPassEnsurerIfNecessary(laboratoryObject);
                laboratoryObject.skybox = assetMan.Get<Cubemap>("Cubemap_Void");

                ChangeLights(laboratoryObject, "HangingLight", "HangingLight");

                if (laboratoryObject.minSize.x < 30 || laboratoryObject.minSize.z < 30)
                {
                    laboratoryObject.minSize = new IntVector2(laboratoryObject.minSize.x + 8, laboratoryObject.minSize.z + 8);
                    laboratoryObject.maxSize = new IntVector2(laboratoryObject.minSize.x + 10, laboratoryObject.minSize.z + 10);
                    if (levelName == "F1")
                    {
                        laboratoryObject.minPlots *= 2;
                        laboratoryObject.maxPlots *= 2;
                    }
                }

                SetupSceneObjectForExtraLevelsIfNecessary(obj);
                obj.randomizedLevelObject = obj.randomizedLevelObject.AddToArray(new WeightedLevelObject()
                {
                    selection = laboratoryObject,
                    weight = weightForNewStyles.Value
                });
                obj.MarkAsNeverUnload();
            }

            obj.randomizedLevelObject = obj.randomizedLevelObject.Where(x => x.weight > 0).ToArray();

            ExecuteTypedGenerators(levelName, levelId, obj);
        }
    }
}
