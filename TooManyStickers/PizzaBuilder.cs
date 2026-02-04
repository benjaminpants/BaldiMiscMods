using System;
using System.Collections.Generic;
using UnityEngine;

namespace TooManyStickers
{
    public class PizzaBuilder : StructureBuilder
    {
        public ItemObject pizza;
        public List<Cell> targetCells = new List<Cell>();
        public override void PostOpenCalcGenerate(LevelGenerator lg, System.Random rng)
        {
            List<Cell> pizzaShapes = lg.Ec.mainHall.GetNewTileList();
            pizzaShapes.RemoveAll(x => (x.offLimits || x.hideFromMap || x.open) || !IsCorner(x) || !x.AllCoverageFits(CellCoverage.Center));
            int pizzaCount = parameters.minMax[0].x;
            bool hasRefilled = false;
            for (int i = 0; i < pizzaCount; i++)
            {
                if (pizzaShapes.Count == 0)
                {
                    if (hasRefilled) break;
                    hasRefilled = true;
                    pizzaShapes = lg.Ec.AllTilesNoGarbage(false, true);
                    pizzaShapes.RemoveAll(x => (x.offLimits || x.hideFromMap) || !IsCorner(x) || !x.AllCoverageFits(CellCoverage.Center) || targetCells.Contains(x));
                }
                int chosenIndex = rng.Next(0, pizzaShapes.Count);
                Cell chosenCell = pizzaShapes[chosenIndex];
                pizzaShapes.Remove(chosenCell);
                targetCells.Add(chosenCell);
            }
        }

        // we generate items at the end to avoid weirdness with room adjusts.
        public override void OnGenerationFinished(LevelBuilder lb)
        {
            base.OnGenerationFinished(lb);
            foreach (Cell chosenCell in targetCells)
            {
                lb.Ec.CreateItem(chosenCell.room, pizza, new Vector2(chosenCell.CenterWorldPosition.x, chosenCell.CenterWorldPosition.z)); // this is technically wrong, but also we dont want the icons on the map
            }
        }

        public bool IsCorner(Cell cell)
        {
            //TileShapeMask.Corner almost never works but lets test anyway
            if (cell.shape.HasFlag(TileShapeMask.Corner)) return true;
            List<Direction> openNavs = cell.AllOpenNavDirections;
            if (openNavs.Count != 2) return false; // not a corner, too many paths
            return !(openNavs[1] == openNavs[0].GetOpposite());
        }
    }
}