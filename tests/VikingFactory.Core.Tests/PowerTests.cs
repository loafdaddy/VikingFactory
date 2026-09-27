using VikingFactory.Core;
using VikingFactory.Core.Kinetics;
using Xunit;

namespace VikingFactory.Core.Tests
{
    public sealed class PowerTests
    {
        [Fact]
        public void SupplyEqualToDemandRunsAndOneDuShortStalls()
        {
            var exact = Pair(12);
            exact.Solve();
            Assert.True(exact.IsProducing("a"));

            var shortBy1 = Pair(11);
            shortBy1.Solve();
            Assert.Equal(KineticStop.Overloaded, shortBy1.StopReason("a"));
        }

        [Fact]
        public void DirectionIsCarriedAndAReversingCogFlipsIt()
        {
            var network = new KineticNetwork();
            network.AddSource("crank", 8);
            network.AddTransmission("shaft");
            network.AddTransmission("reverser");
            network.AddConsumer("feeder", 6);
            network.Connect("crank", "shaft");
            network.Connect("shaft", "reverser", -1);
            network.Connect("reverser", "feeder");
            network.Solve();
            Assert.Equal(16, network.Speed("shaft"));
            Assert.Equal(-16, network.Speed("feeder"));
            Assert.True(network.IsProducing("feeder"));
        }

        [Fact]
        public void TwoToOneCogDoublesSpeedDownstream()
        {
            var network = new KineticNetwork();
            network.AddSource("wheel", 48, "", 16);
            network.AddTransmission("cog");
            network.AddConsumer("mill", 16);
            network.Connect("wheel", "cog", 2);
            network.Connect("cog", "mill");
            network.Solve();
            Assert.Equal(32, network.Speed("mill"));
        }

        [Fact]
        public void ContradictoryRatioLoopStallsAsInvalidRotation()
        {
            var network = new KineticNetwork();
            network.AddSource("crank", 8);
            network.AddTransmission("a");
            network.AddTransmission("b");
            network.AddConsumer("feeder", 6);
            network.Connect("crank", "a");
            network.Connect("a", "b", 2);
            network.Connect("b", "crank");
            network.Connect("a", "feeder");
            network.Solve();
            Assert.Equal(KineticStop.InvalidRotation, network.StopReason("feeder"));
            Assert.Equal(0, network.Speed("feeder"));
        }

        [Fact]
        public void SourceTurningAgainstTheNetworkStallsIt()
        {
            var network = new KineticNetwork();
            network.AddSource("left", 8);
            network.AddSource("right", 8);
            network.AddConsumer("feeder", 6);
            network.Connect("left", "feeder");
            network.Connect("feeder", "right", -1);
            network.Solve();
            Assert.Equal(KineticStop.InvalidRotation, network.StopReason("feeder"));
        }

        [Fact]
        public void MismatchedSourceSpeedsConflictUntilAGovernorJoinsThem()
        {
            var network = new KineticNetwork();
            network.AddSource("wheel", 48, "", 16);
            network.AddSource("steam", 160, "", 32);
            network.AddConsumer("mill", 16);
            network.Connect("wheel", "mill");
            network.Connect("steam", "mill");
            network.Solve();
            Assert.Equal(KineticStop.InvalidRotation, network.StopReason("mill"));

            var governed = new KineticNetwork();
            governed.AddSource("wheel", 48, "", 16);
            governed.AddSource("steam", 160, "", 32);
            governed.AddGovernor("gov", 16);
            governed.AddConsumer("mill", 16);
            governed.Connect("wheel", "gov");
            governed.Connect("steam", "gov");
            governed.Connect("gov", "mill");
            governed.Solve();
            Assert.True(governed.IsProducing("mill"));
            Assert.Equal(208, governed.SupplyDu("mill"));
        }

        [Fact]
        public void GovernorFasterThanASourceIdlesThatSource()
        {
            var network = new KineticNetwork();
            network.AddSource("wheel", 48, "", 16);
            network.AddSource("steam", 160, "", 32);
            network.AddGovernor("gov", 32);
            network.AddConsumer("mill", 16);
            network.Connect("wheel", "gov");
            network.Connect("steam", "gov");
            network.Connect("gov", "mill");
            network.Solve();
            Assert.True(network.IsProducing("mill"));
            Assert.False(network.IsContributing("wheel"));
            Assert.Equal(160, network.SupplyDu("mill"));
        }

        [Fact]
        public void ConsumerSpeedCapLimitsUsefulRpmOnly()
        {
            var network = new KineticNetwork();
            network.AddSource("steam", 160, "", 32);
            network.AddConsumer("feeder", 6, 16);
            network.Connect("steam", "feeder");
            network.Solve();
            Assert.Equal(32, network.Speed("feeder"));
            Assert.Equal(16, network.UsefulRpm("feeder"));
        }

        [Fact]
        public void FlywheelChargesFromSurplusAndBridgesADeficit()
        {
            var network = SailLine(100, 0);
            network.Solve(10);
            Assert.Equal(480, network.StoredEnergy("fly"), 3);

            var calm = SailLine(0, 480);
            calm.Solve(1);
            Assert.True(calm.IsProducing("mill"));
            Assert.Equal(440, calm.StoredEnergy("fly"), 3);
        }

        [Fact]
        public void FlywheelThatCannotCoverTheDeficitStallsAndKeepsItsEnergy()
        {
            var network = SailLine(0, 10);
            network.Solve(1);
            Assert.False(network.IsProducing("mill"));
            Assert.Equal(10, network.StoredEnergy("fly"), 3);
        }

        [Fact]
        public void FlywheelDischargeIsCappedAt80Du()
        {
            var network = new KineticNetwork();
            network.AddSource("sail", 0);
            network.AddFlywheel("fly", 2400);
            network.AddConsumer("big", 100);
            network.Connect("sail", "fly");
            network.Connect("fly", "big");
            network.Solve(1);
            Assert.False(network.IsProducing("big"));
            Assert.Equal(2400, network.StoredEnergy("fly"), 3);
        }

        [Fact]
        public void FlywheelEnergyIsConservedWhenTheNetworkSplits()
        {
            var network = new KineticNetwork();
            network.AddSource("crank", 8);
            network.AddFlywheel("a", 100);
            network.AddFlywheel("b", 300);
            network.AddClutch("clutch");
            network.Connect("crank", "a");
            network.Connect("a", "clutch");
            network.Connect("clutch", "b");
            network.SetClutchClosed("clutch", true);
            network.Solve(0);
            Assert.Equal(400, network.StoredEnergy("a") + network.StoredEnergy("b"), 3);
        }

        [Fact]
        public void DisabledSourceReportsNoPower()
        {
            var network = new KineticNetwork();
            network.AddSource("steam", 160, "", 32);
            network.SetEnabled("steam", false);
            network.AddConsumer("mill", 16);
            network.Connect("steam", "mill");
            network.Solve();
            Assert.Equal(KineticStop.NoPower, network.StopReason("mill"));
        }

        [Fact]
        public void FuelIsTakenOnlyWhenThePaidIntervalIsSpent()
        {
            var engine = new FuelledSource(30);
            var coal = 3;
            bool Take() { if (coal == 0) return false; coal--; return true; }
            Assert.True(engine.Prepare(Take));
            Assert.Equal(2, coal);
            engine.Accrue(20, true);
            Assert.True(engine.Prepare(Take));
            Assert.Equal(2, coal);
            engine.Accrue(15, true);
            Assert.True(engine.Prepare(Take));
            Assert.Equal(1, coal);
        }

        [Fact]
        public void StalledEngineKeepsPaidTimeAndReloadDoesNotTakeASecondCoal()
        {
            var engine = new FuelledSource(30);
            var coal = 5;
            bool Take() { if (coal == 0) return false; coal--; return true; }
            engine.Prepare(Take);
            engine.Accrue(10, false);
            Assert.Equal(30, engine.PaidSeconds);
            var reloaded = new FuelledSource(30, engine.PaidSeconds);
            Assert.True(reloaded.Prepare(Take));
            Assert.Equal(4, coal);
            Assert.False(new FuelledSource(30).Prepare(() => false));
        }

        [Theory]
        [InlineData(0.0, 1.0, 0)]
        [InlineData(0.05, 1.0, 0)]
        [InlineData(0.15, 1.0, 38)]
        [InlineData(0.5, 1.0, 128)]
        [InlineData(1.0, 1.0, 256)]
        [InlineData(1.0, 0.1, 32)]
        public void SailCapacityFollowsWindAndExposure(double wind, double exposure, int expected)
        {
            Assert.Equal(expected, BalanceDefaults.SailCapacity(wind, exposure));
        }

        private static KineticNetwork Pair(int supply)
        {
            var network = new KineticNetwork();
            network.AddSource("crank", supply);
            network.AddConsumer("a", 6);
            network.AddConsumer("b", 6);
            network.Connect("crank", "a");
            network.Connect("crank", "b");
            return network;
        }

        private static KineticNetwork SailLine(int capacity, double stored)
        {
            var network = new KineticNetwork();
            network.AddSource("sail", capacity, "", 32);
            network.AddFlywheel("fly", stored);
            network.AddConsumer("mill", 40);
            network.Connect("sail", "fly");
            network.Connect("fly", "mill");
            return network;
        }
    }
}
