using System.Collections.Generic;
using System.Linq;
using VikingFactory.Core.Agriculture;
using VikingFactory.Core.Diagnostics;
using VikingFactory.Core.Husbandry;
using VikingFactory.Core.Production;
using VikingFactory.Core.Runtime;
using Xunit;

namespace VikingFactory.Core.Tests
{
    public sealed class ProductionTests
    {
        [Fact]
        public void QuarryMakesOneStonePerTenActiveSecondsAndStopsAtFifty()
        {
            var quarry = new TimedProducer(BalanceDefaults.QuarrySecondsPerStone, BalanceDefaults.QuarryCapacity);
            Assert.Equal(0, quarry.Tick(100, false));
            Assert.Equal(6, quarry.Tick(60, true));
            quarry.Tick(10000, true);
            Assert.Equal(50, quarry.Buffered);
            Assert.Equal(0, quarry.Tick(100, true));
            Assert.Equal(0, quarry.Progress);
        }

        [Fact]
        public void SpacingBlocksTheLaterSiteDeterministically()
        {
            var a = new SiteSpacing.Site("a", 0, 0);
            var b = new SiteSpacing.Site("b", 5, 0);
            var c = new SiteSpacing.Site("c", 20, 0);
            var all = new[] { a, b, c };
            Assert.True(SiteSpacing.IsClear(a, all, 12, out _));
            Assert.False(SiteSpacing.IsClear(b, all, 12, out var by));
            Assert.Equal("a", by);
            Assert.True(SiteSpacing.IsClear(c, all, 12, out _));
        }

        [Fact]
        public void ProgressionGateFailsClosedForUnresolvedKeys()
        {
            var gate = new ProgressionGate();
            Assert.False(gate.IsUnlocked(new[] { "Moder" }, _ => true, out var missing));
            Assert.Contains("not found", missing);
            gate.Resolve("Moder", "defeated_dragon");
            Assert.False(gate.IsUnlocked(new[] { "Moder" }, _ => false, out _));
            Assert.True(gate.IsUnlocked(new[] { "Moder" }, k => k == "defeated_dragon", out _));
        }

        [Fact]
        public void DeepExtractionAllowlistDeniesUnknownOutputs()
        {
            Assert.NotNull(DeepExtraction.Find("CopperOre"));
            Assert.Null(DeepExtraction.Find("FlametalOre"));
            Assert.Null(DeepExtraction.Find("BlackMetalScrap"));
        }

        [Fact]
        public void CommissioningIsConsumedOnceAndCannotBeRefunded()
        {
            var commission = new Commission("CopperOre", 10);
            Assert.Equal(0, commission.Offer("TinOre", 5, out var wrong));
            Assert.Equal(CommissionResult.WrongItem, wrong);
            Assert.Equal(6, commission.Offer("CopperOre", 6, out _));
            Assert.Equal(4, commission.Offer("CopperOre", 50, out _));
            Assert.True(commission.Complete);
            Assert.Equal(0, commission.Offer("CopperOre", 1, out var done));
            Assert.Equal(CommissionResult.AlreadyCommissioned, done);
            var reloaded = Commission.Load("CopperOre", 10, commission.Save());
            Assert.True(reloaded.Complete);
        }

        [Fact]
        public void CoppiceGrowsOnceWaitsWhenMatureAndFixesItsSpecies()
        {
            var bed = new CoppiceBed(nativeGrowSeconds: 3000);
            Assert.Equal(3, bed.Plant("BeechSeeds", 3, out _));
            Assert.Equal(0, bed.Plant("PineCone", 2, out var reason));
            Assert.NotEqual("", reason);
            Assert.Equal(2, bed.Plant("BeechSeeds", 9, out _));
            Assert.True(bed.Commissioned);
            Assert.Equal(3000, bed.CycleSeconds);

            bed.Tick(2999);
            Assert.False(bed.Mature);
            bed.Tick(100000);
            Assert.True(bed.Mature);
            var harvest = bed.HarvestTimber();
            Assert.Equal(20, harvest.Single(p => p.Key == "Wood").Value);
            Assert.Empty(bed.HarvestTimber());

            var reloaded = CoppiceBed.Load(bed.Save(), 3000);
            Assert.True(reloaded.Commissioned);
            Assert.False(reloaded.Mature);
        }

        [Fact]
        public void CoppiceModeSwitchesOnlyAtTheStartOfACycle()
        {
            var bed = new CoppiceBed();
            bed.Plant("BeechSeeds", 5, out _);
            Assert.True(bed.SetMode(CoppiceMode.Resin, out _));
            bed.Tick(1200);
            Assert.Equal(2, bed.ResinBuffered);
            Assert.False(bed.SetMode(CoppiceMode.Timber, out _));
            Assert.False(bed.Mature);

            var pine = new CoppiceBed();
            pine.Plant("PineCone", 5, out _);
            Assert.False(pine.SetMode(CoppiceMode.Resin, out _));
        }

        [Fact]
        public void ForageIsAllowlistedAndNeverFasterThanNativeRespawn()
        {
            Assert.NotNull(Forage.Find("Raspberry"));
            Assert.Null(Forage.Find("RoyalJelly"));
            Assert.Equal(600, Forage.SecondsPerItem(5));
            Assert.Equal(18000, Forage.SecondsPerItem(300));
        }

        [Fact]
        public void SeedReserveKeepsPlantingCostBeforeExport()
        {
            var carrot = new CropSpec { PlantingItem = "CarrotSeeds", YieldItem = "Carrot" };
            var barley = new CropSpec { PlantingItem = "Barley", YieldItem = "Barley" };
            Assert.True(barley.SelfSeeding);
            Assert.False(carrot.SelfSeeding);
            Assert.Equal(11, FieldPlanning.Reserve(10, 1));
            Assert.Equal(9, FieldPlanning.Exportable(FieldPolicy.ReplantSame, barley, "Barley", 20, 10));
            Assert.Equal(20, FieldPlanning.Exportable(FieldPolicy.FoodProduction, barley, "Barley", 20, 10));
            Assert.Equal(20, FieldPlanning.Exportable(FieldPolicy.ReplantSame, carrot, "Carrot", 20, 10));
            Assert.Equal(16, FieldPlanning.Tiles(4, 4, 0.5f).Count);
        }

        [Fact]
        public void CullingKeepsBreedersAndNeverTakesNamedOrYoungAnimals()
        {
            var policy = new CullingPolicy { Enabled = true, MinBreeders = 2, KeepAbove = 2 };
            var herd = new List<Animal>
            {
                new Animal { Id = "1", Species = "Boar", Tamed = true, Adult = true, Named = true },
                new Animal { Id = "2", Species = "Boar", Tamed = true, Adult = true },
                new Animal { Id = "3", Species = "Boar", Tamed = true, Adult = true, Level = 2 },
                new Animal { Id = "4", Species = "Boar", Tamed = true, Adult = false },
                new Animal { Id = "5", Species = "Wolf", Tamed = true, Adult = true }
            };
            Assert.Equal("2", policy.SelectSurplus(herd, out _)!.Id);
            Assert.Null(policy.SelectSurplus(herd.Where(a => a.Id != "3"), out _));
            Assert.Null(new CullingPolicy().SelectSurplus(herd, out _));
            Assert.True(FeedPolicy.ShouldRelease(1, 0));
            Assert.False(FeedPolicy.ShouldRelease(1, 1));
        }

        [Fact]
        public void ClockDropsSleepJumpsAndBoundsCatchUp()
        {
            var clock = new FixedStepClock(0.2, 5, 2);
            Assert.Equal(1, clock.Advance(0.25));
            Assert.Equal(0, clock.Advance(3600));
            Assert.Equal(1, clock.Discontinuities);
            Assert.Equal(5, clock.Advance(1.9));
        }

        [Fact]
        public void OwnershipHandoffPausesForOneObservation()
        {
            var tracker = new OwnershipTracker();
            tracker.Observe(1, true);
            Assert.True(tracker.ShouldRehydrate);
            Assert.False(tracker.CanAct);
            tracker.Observe(1, true);
            Assert.True(tracker.CanAct);
            tracker.Observe(2, false);
            Assert.False(tracker.CanAct);
            tracker.Observe(1, true);
            Assert.False(tracker.CanAct);
            Assert.Equal(3, tracker.Epoch);
        }

        [Fact]
        public void SchemaAndPresetsFailSafe()
        {
            Assert.Equal(SchemaDecision.PauseNewer, SchemaGate.Check(SchemaGate.Current + 1));
            Assert.Equal(SchemaDecision.Migrate, SchemaGate.Check(0));
            var vanilla = FeatureFlags.For(Preset.VanillaSupply, true);
            Assert.False(vanilla.DeepExtraction);
            Assert.False(vanilla.Coppice);
            Assert.True(vanilla.BedrockQuarry);
            Assert.False(FeatureFlags.For(Preset.BalancedIndustry, false).BedrockQuarry);
        }

        [Fact]
        public void CoverageFollowsRenewableChainsAndExcludesAttacks()
        {
            var input = new CoverageInput
            {
                Items =
                {
                    new CoverageItem { Prefab = "Wood", SharedName = "$item_wood" },
                    new CoverageItem { Prefab = "Coal", SharedName = "$item_coal" },
                    new CoverageItem { Prefab = "IronOre", SharedName = "$item_ironore" },
                    new CoverageItem { Prefab = "Iron", SharedName = "$item_iron" },
                    new CoverageItem { Prefab = "TrophyBoar", SharedName = "$item_trophy_boar" },
                    new CoverageItem { Prefab = "Gold", SharedName = "$item_gold" },
                    new CoverageItem { Prefab = "Bite", SharedName = "Bite attack" }
                },
                Producers =
                {
                    new CoverageProducer { Kind = "smelter", Station = "charcoal_kiln", Output = "$item_coal", Inputs = { "$item_wood" }, Automated = true },
                    new CoverageProducer { Kind = "smelter", Station = "smelter", Output = "$item_iron", Inputs = { "$item_ironore" }, Automated = true },
                    new CoverageProducer { Kind = "cooking", Station = "piece_FrostFoundry", Output = "$item_gold", Inputs = { "$item_x" }, Automated = false }
                },
                ModRenewable = { "$item_wood" }
            };
            var rows = CoverageClassifier.Classify(input).ToDictionary(r => r.Prefab);
            Assert.Equal(6, rows.Count);
            Assert.Equal(CoverageCategory.RenewableChain, rows["Coal"].Category);
            Assert.Equal(CoverageCategory.FiniteSupplyAutomatedProcessing, rows["Iron"].Category);
            Assert.Equal(CoverageCategory.ManualOrUniqueByDesign, rows["TrophyBoar"].Category);
            Assert.Equal(CoverageCategory.UnsupportedPendingAdapter, rows["Gold"].Category);
            Assert.Contains("RenewableChain: 2", CoverageClassifier.ToCsv(rows.Values, input));
        }
    }
}
