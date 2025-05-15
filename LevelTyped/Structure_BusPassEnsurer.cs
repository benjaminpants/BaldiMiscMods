using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LevelTyped
{
    public class Structure_BusPassEnsurer : StructureBuilder
    {
        public ItemObject busPass;
        System.Random myRng;

        public override void Generate(LevelGenerator lg, System.Random rng)
        {
            base.Generate(lg, rng);
            myRng = new System.Random(rng.Next());
        }

        public override void OnGenerationFinished(LevelBuilder lb)
        {
            base.OnGenerationFinished(lb);
            if (lb.Ec.items.Find(x => x.item == busPass) != null) return;
            Debug.LogWarning("Bus Pass not found! Replacing highest value item!");
            List<Pickup> foundReplacables = new List<Pickup>();
            int highestReplaceableValue = int.MinValue;
            for (int i = 0; i < lb.Ec.items.Count; i++)
            {
                Pickup item = lb.Ec.items[i];
                if (item.item.value > highestReplaceableValue)
                {
                    foundReplacables.Clear();
                    foundReplacables.Add(item);
                    highestReplaceableValue = item.item.value;
                }
                else if (item.item.value == highestReplaceableValue)
                {
                    foundReplacables.Add(item);
                }
            }
            if (foundReplacables.Count == 0)
            {
                Debug.LogWarning("No items on level?? The fuck?");
                return;
            }
            Pickup chosenPickup = foundReplacables[myRng.Next(0, foundReplacables.Count)];
            Debug.Log("Replacing: " + chosenPickup.name + "!");
            chosenPickup.AssignItem(busPass);
        }
    }
}
