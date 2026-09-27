using System.Collections.Generic;
using System.Linq;
using VikingFactory.Core.Crafting;
using VikingFactory.Core.Items;
using Xunit;

namespace VikingFactory.Core.Tests
{
    public sealed class CraftingTests
    {
        private static readonly MillConditions Ready = new MillConditions { Powered = true, SpeedMultiplier = 1, StationProblem = "", QuotaAllows = true };

        [Fact]
        public void BronzeNailsCraftOnceFromEscrow()
        {
            var mill = Taught(Nails());
            Assert.True(mill.TryAcceptInput(Item("Bronze", 1)));
            mill.Tick(0.2, Ready);
            Assert.Equal(MillState.Running, mill.State);
            Assert.Single(mill.Escrow);
            Assert.Equal(6, RecipeMill.CycleSeconds(1));
            mill.Tick(5.9, Ready);
            Assert.Equal(1, mill.CraftsCompleted);
            Assert.Empty(mill.Escrow);
            var output = mill.Output.Single();
            Assert.Equal(20, output.Count);
            Assert.Equal(7L, output.CrafterId);
        }

        [Fact]
        public void FiveIngredientRecipeIsSupported()
        {
            var recipe = new RecipeSpec
            {
                Id = "Five",
                OutputPrefab = "Thing",
                Ingredients = new List<Ingredient> { new Ingredient("A", 1), new Ingredient("B", 2), new Ingredient("C", 1), new Ingredient("D", 3), new Ingredient("E", 1) }
            };
            Assert.NotEqual("", recipe.UnsupportedReason(4));
            var mill = Taught(recipe);
            foreach (var ingredient in recipe.Ingredients)
                Assert.True(mill.TryAcceptInput(Item(ingredient.Prefab, ingredient.Amount)));
            mill.Tick(14, Ready);
            Assert.Equal(1, mill.CraftsCompleted);
        }

        [Fact]
        public void UnrelatedInputIsRefusedAndBufferIsBounded()
        {
            var mill = Taught(Nails());
            Assert.False(mill.TryAcceptInput(Item("Wood", 1)));
            Assert.True(mill.TryAcceptInput(Item("Bronze", 2)));
            Assert.False(mill.TryAcceptInput(Item("Bronze", 1)));
        }

        [Fact]
        public void StationRemovedOrPowerLostMidJobPausesAndKeepsIngredients()
        {
            var mill = Taught(Nails());
            mill.TryAcceptInput(Item("Bronze", 1));
            mill.Tick(1, Ready);
            var missing = Ready;
            missing.StationProblem = "Station missing.";
            mill.Tick(100, missing);
            var off = Ready;
            off.Powered = false;
            mill.Tick(100, off);
            Assert.Equal(MillState.Paused, mill.State);
            Assert.Equal(0, mill.CraftsCompleted);
            Assert.Equal(1, mill.HeldCount("Bronze"));
        }

        [Fact]
        public void FullOutputBlocksBeforeInputIsConsumed()
        {
            var mill = Taught(Nails());
            for (var i = 0; i < 4; i++)
            {
                mill.TryAcceptInput(Item("Bronze", 1));
                mill.Tick(0.1, Ready);
                mill.Tick(10, Ready);
            }

            mill.TryAcceptInput(Item("Bronze", 1));
            mill.Tick(10, Ready);
            Assert.Equal(MillState.OutputFull, mill.State);
            Assert.Equal(1, mill.Buffered("Bronze"));
        }

        [Fact]
        public void RecipeChangeAndCancelReturnIngredientsExactlyOnce()
        {
            var mill = Taught(Nails());
            mill.TryAcceptInput(Item("Bronze", 2));
            mill.Tick(1, Ready);
            Assert.True(mill.Teach(Arrows(), 7, "Tyler", out _));
            Assert.Equal(2, mill.TakeRecovery().Sum(s => s.Count));
            Assert.Empty(mill.TakeRecovery());
        }

        [Fact]
        public void QuotaStopsNewCraftsButNotTheOneInProgress()
        {
            var mill = Taught(Nails());
            mill.TryAcceptInput(Item("Bronze", 2));
            mill.Tick(1, Ready);
            var quota = Ready;
            quota.QuotaAllows = false;
            mill.Tick(10, quota);
            mill.Tick(1, quota);
            Assert.Equal(1, mill.CraftsCompleted);
            Assert.Equal(MillState.QuotaReached, mill.State);
        }

        [Fact]
        public void SpeedIsCappedWithATwoSecondFloorAndCheatedIsKept()
        {
            Assert.Equal(3, Taught(Nails()).EffectiveCycleSeconds(4));
            var assembler = new RecipeMill(8, 4);
            assembler.Teach(Nails(), 1, "", out _);
            Assert.Equal(2, assembler.EffectiveCycleSeconds(4));

            var mill = Taught(Nails());
            var bronze = Item("Bronze", 1);
            bronze.Cheated = true;
            mill.TryAcceptInput(bronze);
            mill.Tick(0.1, Ready);
            mill.Tick(10, Ready);
            Assert.True(mill.Output.Single().Cheated);
        }

        [Fact]
        public void SaveAndReloadDuringEscrowFinishesOnce()
        {
            var mill = Taught(Nails());
            mill.TryAcceptInput(Item("Bronze", 2));
            mill.Tick(3, Ready);
            var reloaded = new RecipeMill();
            reloaded.Load(mill.SaveState(), mill.SaveInputs(), mill.SaveEscrow(), mill.SaveOutput(), mill.SaveRecovery(), id => id == "Recipe_BronzeNails" ? Nails() : null);
            Assert.True(reloaded.HasJob);
            reloaded.Tick(3, Ready);
            Assert.Equal(1, reloaded.CraftsCompleted);
            Assert.Equal(1, reloaded.Buffered("Bronze"));

            var missing = new RecipeMill();
            missing.Load(mill.SaveState(), mill.SaveInputs(), mill.SaveEscrow(), mill.SaveOutput(), mill.SaveRecovery(), _ => null);
            Assert.Equal(MillState.NoRecipe, missing.State);
            Assert.Equal(2, missing.TakeRecovery().Sum(s => s.Count));
        }

        [Fact]
        public void DestroyDrainsEveryHeldItemOnce()
        {
            var mill = Taught(Nails());
            mill.TryAcceptInput(Item("Bronze", 2));
            mill.Tick(1, Ready);
            Assert.Equal(2, mill.DrainAll().Sum(s => s.Count));
            Assert.Empty(mill.DrainAll());
        }

        [Fact]
        public void UpgradeConsumesTheExistingItemAndKeepsIdentity()
        {
            var sword = new RecipeSpec
            {
                Id = "Recipe_SwordBronze",
                OutputPrefab = "SwordBronze",
                MaxQuality = 4,
                Ingredients = new List<Ingredient> { new Ingredient("Bronze", 8, 4), new Ingredient("Wood", 2, 0) }
            };
            var item = new ItemStack { Prefab = "SwordBronze", CrafterName = "Tyler", CustomData = { ["rune"] = "a" } };
            Assert.Equal("", UpgradePlan.Check(sword, item, 2));
            Assert.Equal("Station level too low.", UpgradePlan.Check(sword, item, 1));
            Assert.Equal(4, UpgradePlan.NextLevelCost(sword, 1).Single().Amount);
            var upgraded = UpgradePlan.Apply(item);
            Assert.Equal(2, upgraded.Quality);
            Assert.Equal("a", upgraded.CustomData["rune"]);
            Assert.NotEqual("", UpgradePlan.Check(sword, new ItemStack { Prefab = "SwordBronze", Quality = 4 }, 10));
        }

        private static RecipeMill Taught(RecipeSpec recipe)
        {
            var mill = new RecipeMill();
            Assert.True(mill.Teach(recipe, 7, "Tyler", out var reason), reason);
            return mill;
        }

        private static RecipeSpec Nails() => new RecipeSpec
        {
            Id = "Recipe_BronzeNails",
            OutputPrefab = "BronzeNails",
            OutputAmount = 20,
            Station = "$piece_forge",
            Ingredients = new List<Ingredient> { new Ingredient("Bronze", 1) }
        };

        private static RecipeSpec Arrows() => new RecipeSpec
        {
            Id = "Recipe_ArrowWood",
            OutputPrefab = "ArrowWood",
            OutputAmount = 20,
            Ingredients = new List<Ingredient> { new Ingredient("Wood", 8) }
        };

        private static ItemStack Item(string prefab, int count) => new ItemStack { Prefab = prefab, Count = count };
    }
}
