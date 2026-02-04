using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TooManyStickers
{
    public class PizzaItem : Item
    {
        public SoundObject eatSound;
        public override bool Use(PlayerManager pm)
        {
            StartCoroutine(Eat(pm));
            return true;
        }

        IEnumerator Eat(PlayerManager pm)
        {
            Singleton<CoreGameManager>.Instance.audMan.PlaySingle(eatSound);
            float eatTime = eatSound.soundClip.length;
            MovementModifier moveMod = new MovementModifier(Vector3.zero, 0.3f);
            pm.Am.moveMods.Add(moveMod);
            while (eatTime > 0f)
            {
                eatTime -= Time.deltaTime * pm.PlayerTimeScale;
                yield return null;
            }
            pm.Am.moveMods.Remove(moveMod);

            PizzaCounter counter = null;
            if (!pm.TryGetComponent<PizzaCounter>(out counter))
            {
                counter = pm.gameObject.AddComponent<PizzaCounter>();
            }
            pm.plm.stamina = Mathf.Max(pm.plm.staminaMax, pm.plm.stamina);
            counter.pizzas++;
            Destroy(gameObject);
        }
    }
    
    public class PizzaCounter : MonoBehaviour
    {
        public int pizzas = 0;
    }
}
