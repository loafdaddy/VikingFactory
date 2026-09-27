using System.Collections.Generic;
using VikingFactory.Core.Items;

namespace VikingFactory.Core.Crafting
{
    /// <summary>
    /// A quality upgrade consumes one specific existing item plus the next level's resources.
    /// It never creates a higher-quality item from nothing, and it skips no level.
    /// </summary>
    public static class UpgradePlan
    {
        /// <summary>Resources for raising an item from quality to quality + 1, as the game charges them.</summary>
        public static List<Ingredient> NextLevelCost(RecipeSpec recipe, int currentQuality)
        {
            var cost = new List<Ingredient>();
            foreach (var ingredient in recipe.Ingredients)
            {
                var amount = ingredient.AmountPerLevel * currentQuality;
                if (amount > 0)
                    cost.Add(new Ingredient(ingredient.Prefab, amount));
            }

            return cost;
        }

        public static int RequiredStationLevel(RecipeSpec recipe, int targetQuality)
        {
            return System.Math.Max(1, recipe.StationLevel) + (targetQuality - 1);
        }

        public static string Check(RecipeSpec recipe, ItemStack item, int stationLevel)
        {
            if (item == null || item.Prefab != recipe.OutputPrefab)
                return "That item is not made by this recipe.";
            if (item.Count != 1)
                return "Upgrade one item at a time.";
            if (item.Quality >= recipe.MaxQuality)
                return "Already at the highest quality.";
            var target = item.Quality + 1;
            if (stationLevel < RequiredStationLevel(recipe, target))
                return "Station level too low.";
            if (NextLevelCost(recipe, item.Quality).Count == 0)
                return "This recipe has no upgrade cost.";
            return "";
        }

        /// <summary>The same item, one level higher. Crafter, variant, and custom data stay. Durability resets to full.</summary>
        public static ItemStack Apply(ItemStack item)
        {
            var upgraded = item.Copy(1);
            upgraded.Quality = item.Quality + 1;
            upgraded.Durability = -1;
            return upgraded;
        }
    }
}
