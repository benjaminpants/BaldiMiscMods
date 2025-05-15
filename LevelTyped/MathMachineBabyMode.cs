using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace LevelTyped
{
    /// <summary>
    /// This class does nothing on it's own and serves as a way for the patches to know what to do
    /// </summary>
    public class MathMachineBabyMode : MonoBehaviour
    {
    }

    [HarmonyPatch(typeof(MathMachine))]
    [HarmonyPatch("NewProblem")]
    static class BabyModePatch
    {
        static bool Prefix(MathMachine __instance, List<int> ____availableAnswers, ref int ___answer, TMP_Text ___answerText, TMP_Text ___signText, TMP_Text ___val1Text, TMP_Text ___val2Text, ref bool ___addition, ref int ___num1, ref int ___num2)
        {
            if (!__instance.GetComponent<MathMachineBabyMode>()) return true;
            // remove all numbers we do not want
            // remove the elements so the repopulator script doesn't know to put them back
            for (int i = __instance.currentNumbers.Count - 1; i >= 0; i--)
            {
                if ((__instance.currentNumbers[i].Value > 3))
                {
                    GameObject.Destroy(__instance.currentNumbers[i].gameObject);
                    __instance.currentNumbers.RemoveAt(i);
                }
            }
            // handle logic
            ____availableAnswers.Clear();
            foreach (MathMachineNumber mathMachineNumber in __instance.currentNumbers)
            {
                if (mathMachineNumber.Available)
                {
                    ____availableAnswers.Add(mathMachineNumber.Value);
                }
            }
            ___answer = -1;
            while (___answer < 0 && ____availableAnswers.Count > 0)
            {
                ___answerText.text = "?";
                if (UnityEngine.Random.Range(0, 2) > 0)
                {
                    ___addition = false;
                }
                if (!___addition)
                {
                    ___signText.text = "-";
                }
                else
                {
                    ___signText.text = "+";
                }
                int num = UnityEngine.Random.Range(0, 4);
                int num2;
                if (___addition)
                {
                    num2 = UnityEngine.Random.Range(0, 4 - num);
                }
                else
                {
                    num2 = UnityEngine.Random.Range(0, num + 1);
                }
                ___val1Text.text = num.ToString();
                ___num1 = num;
                ___val2Text.text = num2.ToString();
                ___num2 = num2;
                if (___addition)
                {
                    ___answer = num + num2;
                }
                else
                {
                    ___answer = num - num2;
                }
                if (!____availableAnswers.Contains(___answer))
                {
                    ___answer = -1;
                }
            }
            return false;
        }
    }
}
