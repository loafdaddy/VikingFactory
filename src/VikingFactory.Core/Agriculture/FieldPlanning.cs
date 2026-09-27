using System;
using System.Collections.Generic;

namespace VikingFactory.Core.Agriculture
{
    public enum FieldPolicy
    {
        /// <summary>Replant the same crop, reserving the next planting cost before anything is exported.</summary>
        ReplantSame,
        /// <summary>Grow the seed-crop form. Output is seed for another field.</summary>
        SeedProduction,
        /// <summary>Grow food from seed that arrives by belt. Nothing is kept for replanting here.</summary>
        FoodProduction
    }

    /// <summary>
    /// A crop mapping read from the running game: sapling piece, its planting item, and its grown pickable's yield.
    /// </summary>
    public sealed class CropSpec
    {
        public string Sapling = "";
        public string PlantingItem = "";
        public int PlantingCost = 1;
        public string YieldItem = "";
        public float GrowRadius = 0.5f;

        /// <summary>Barley, flax, jotun puffs and magecaps are planted with their own yield. Carrots are not.</summary>
        public bool SelfSeeding => PlantingItem == YieldItem;
    }

    public static class FieldPlanning
    {
        /// <summary>
        /// Items kept back before export: planting cost for every empty assigned tile, plus a safety margin.
        /// </summary>
        public static int Reserve(int emptyTiles, int plantingCost, double safetyFraction = 0.10)
        {
            if (emptyTiles <= 0 || plantingCost <= 0)
                return 0;
            var basic = emptyTiles * plantingCost;
            return basic + (int)Math.Ceiling(basic * Math.Max(0, safetyFraction));
        }

        /// <summary>How many of this item may leave the field this cycle.</summary>
        public static int Exportable(FieldPolicy policy, CropSpec crop, string item, int held, int emptyTiles, double safetyFraction = 0.10)
        {
            if (policy == FieldPolicy.FoodProduction || item != crop.PlantingItem)
                return held;
            return Math.Max(0, held - Reserve(emptyTiles, crop.PlantingCost, safetyFraction));
        }

        /// <summary>
        /// Tile centres for a rectangular field, spaced by the plant's own grow radius. Local metres,
        /// centred on the field origin. The game's own placement check still decides each tile.
        /// </summary>
        public static List<KeyValuePair<float, float>> Tiles(float width, float depth, float growRadius)
        {
            var tiles = new List<KeyValuePair<float, float>>();
            var spacing = Math.Max(0.5f, growRadius * 2f);
            var columns = Math.Max(1, (int)Math.Floor(width / spacing));
            var rows = Math.Max(1, (int)Math.Floor(depth / spacing));
            var startX = -(columns - 1) * spacing / 2f;
            var startZ = -(rows - 1) * spacing / 2f;
            for (var r = 0; r < rows; r++)
            {
                for (var c = 0; c < columns; c++)
                    tiles.Add(new KeyValuePair<float, float>(startX + c * spacing, startZ + r * spacing));
            }

            return tiles;
        }
    }
}
