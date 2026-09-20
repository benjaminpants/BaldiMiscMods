using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlaytimeRebalance
{
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi")]
    [BepInPlugin("mtm101.rulerp.baldiplus.playtimerebalance", "Playtime Rebalance", "0.0.0.0")]
    public class PlaytimeRebalancePlugin : BaseUnityPlugin
    {
        void Awake()
        {
            LoadingEvents.RegisterOnAssetsLoaded(Info, LoadEnumerator(), LoadingEventOrder.Pre);
            GeneratorManagement.Register(this, GenerationModType.Addend, GenChange);
            ModdedSaveGame.AddSaveHandler(Info);
            Harmony harmony = new Harmony("mtm101.rulerp.baldiplus.playtimerebalance");
            harmony.PatchAllConditionals();
        }

        void GenChange(string sceneName, int id, SceneObject sceneObj)
        {
            WeightedNPC foundPlaytime = sceneObj.potentialNPCs.Find(x => x.selection.Character == Character.Playtime);
            if (foundPlaytime == null)
            {
                if (sceneName == "F1")
                {
                    sceneObj.potentialNPCs.Add(new WeightedNPC()
                    {
                        selection = NPCMetaStorage.Instance.Get(Character.Playtime).prefabs["Playtime"],
                        weight = 25
                    });
                }
                return;
            }
            foundPlaytime.weight += 15;
            //foundPlaytime.weight = 9999;
        }

        IEnumerator LoadEnumerator()
        {
            yield return 2;
            yield return "Modifying Playtime...";
            Playtime pt = (Playtime)NPCMetaStorage.Instance.Get(Character.Playtime).prefabs["Playtime"];
            pt.ReflectionSetVariable("runSpeed", (float)pt.ReflectionGetVariable("runSpeed") + 7f);
            //((Jumprope)pt.ReflectionGetVariable("jumpropePre")).ReflectionSetVariable("maxJumps", 5);
            yield return "Modifying meta...";
            NPCMetaStorage.Instance.Get(Character.Cumulo).tags.Add("pltr_notarget");
            NPCMetaStorage.Instance.Get(Character.LookAt).tags.Add("pltr_notarget");
        }
    }
}
