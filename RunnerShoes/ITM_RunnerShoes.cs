using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using MTM101BaldAPI.PlusExtensions;
using MTM101BaldAPI.Components;

namespace RunnerShoes
{
    public class ITM_RunnerShoes : Item
    {
        public float setTime;
        public Sprite gaugeSprite;
        ValueModifier currentModifier;
        HudGauge gauge;
        public override bool Use(PlayerManager pm)
        {
            this.pm = pm;
            gauge = Singleton<CoreGameManager>.Instance.GetHud(pm.playerNumber).gaugeManager.ActivateNewGauge(gaugeSprite, this.setTime);
            currentModifier = new ValueModifier(2f);
            pm.plm.GetModifier().AddModifier("staminaMax", currentModifier);
            StartCoroutine(Timer());
            return true;
        }

        IEnumerator Timer()
        {
            float time = setTime;
            while (time > 0f)
            {
                time -= Time.deltaTime * pm.PlayerTimeScale;
                gauge.SetValue(setTime, time);
                yield return null;
            }
            pm.plm.GetModifier().RemoveModifier(currentModifier);
            gauge.Deactivate();
            time = 2f;
            while (time > 0f)
            {
                time -= Time.deltaTime;
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
