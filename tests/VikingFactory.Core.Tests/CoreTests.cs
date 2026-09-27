using System.Linq;
using VikingFactory.Core;
using VikingFactory.Core.Items;
using VikingFactory.Core.Kinetics;
using VikingFactory.Core.Transfers;
using Xunit;

namespace VikingFactory.Core.Tests
{
    public sealed class KineticTests
    {
        [Fact]
        public void HandCrankRunsOneFeederAndStallsOnTwo()
        {
            var single = NetworkWithFeeders(1, crankHeld: true);
            single.Solve();
            Assert.True(single.IsProducing("feeder-0"));
            Assert.Equal(BalanceDefaults.HandCrankDu, single.SupplyDu("feeder-0"));
            Assert.Equal(BalanceDefaults.BronzeFeederDu, single.ReservedDu("feeder-0"));

            var doubled = NetworkWithFeeders(2, crankHeld: true);
            doubled.Solve();
            Assert.False(doubled.IsProducing("feeder-0"));
            Assert.False(doubled.IsProducing("feeder-1"));
            Assert.Equal(KineticStop.Overloaded, doubled.StopReason("feeder-0"));
            Assert.Equal(BalanceDefaults.TwoFeedersDu, doubled.ReservedDu("feeder-0"));
        }

        [Fact]
        public void ReleasedCrankProvidesNoDrive()
        {
            var network = NetworkWithFeeders(1, crankHeld: false);
            network.Solve();
            Assert.Equal(KineticStop.NoPower, network.StopReason("feeder-0"));
            Assert.Equal(0, network.SupplyDu("feeder-0"));
        }

        [Fact]
        public void ShaftsDoNotAddLoad()
        {
            var network = new KineticNetwork();
            network.AddSource("crank", BalanceDefaults.HandCrankDu);
            network.AddTransmission("shaft");
            network.AddConsumer("feeder", BalanceDefaults.BronzeFeederDu);
            network.Connect("crank", "shaft");
            network.Connect("shaft", "feeder");
            network.Solve();
            Assert.Equal(BalanceDefaults.BronzeFeederDu, network.ReservedDu("feeder"));
            Assert.True(network.IsProducing("feeder"));
            Assert.Equal(BalanceDefaults.MilestoneRpm, network.Rpm);
        }

        [Fact]
        public void ClosedClutchDisconnectsTheBranchAndReleasesItsLoad()
        {
            var network = new KineticNetwork();
            network.AddSource("crank", BalanceDefaults.HandCrankDu);
            network.AddClutch("clutch");
            network.AddConsumer("beyond", BalanceDefaults.BronzeFeederDu);
            network.AddConsumer("beside", BalanceDefaults.BronzeFeederDu);
            network.Connect("crank", "clutch");
            network.Connect("clutch", "beyond");
            network.Connect("crank", "beside");
            network.SetClutchClosed("clutch", true);
            network.Solve();

            Assert.False(network.IsProducing("beyond"));
            Assert.True(network.IsProducing("beside"));
            Assert.Equal(BalanceDefaults.BronzeFeederDu, network.ReservedDu("beside"));
        }

        [Fact]
        public void DisabledConsumerReleasesReservation()
        {
            var network = NetworkWithFeeders(2, crankHeld: true);
            network.SetEnabled("feeder-1", false);
            network.Solve();
            Assert.True(network.IsProducing("feeder-0"));
            Assert.False(network.IsProducing("feeder-1"));
            Assert.Equal(BalanceDefaults.BronzeFeederDu, network.ReservedDu("feeder-0"));
        }

        [Fact]
        public void WaterWheelWithoutAVerifiedPlacementProvidesNoDrive()
        {
            var network = new KineticNetwork();
            network.AddSource("wheel", BalanceDefaults.WaterWheelDu, placementRule: "valid-water");
            network.AddConsumer("feeder", BalanceDefaults.BronzeFeederDu);
            network.Connect("wheel", "feeder");
            network.Solve();
            Assert.Equal(KineticStop.InvalidPlacement, network.StopReason("feeder"));
            Assert.Equal(0, network.SupplyDu("feeder"));
        }

        [Fact]
        public void VerifiedWaterWheelRunsTheDocumentedKilnLine()
        {
            var network = new KineticNetwork { Placement = new AlwaysValidProbe() };
            network.AddSource("wheel", BalanceDefaults.WaterWheelDu, placementRule: "valid-water");
            for (var i = 0; i < 3; i++)
                network.AddConsumer("feeder-" + i, BalanceDefaults.BronzeFeederDu);
            for (var i = 0; i < 4; i++)
                network.AddConsumer("belt-" + i, BalanceDefaults.TimberBeltDuPer2M);
            network.Connect("wheel", "feeder-0");
            for (var i = 0; i < 3; i++)
                network.Connect("wheel", "feeder-" + i);
            for (var i = 0; i < 4; i++)
                network.Connect("wheel", "belt-" + i);
            network.Solve();

            Assert.Equal(48, network.SupplyDu("feeder-0"));
            Assert.Equal(22, network.ReservedDu("feeder-0"));
            Assert.True(network.IsProducing("feeder-0"));
            Assert.True(network.IsProducing("belt-3"));
        }

        private static KineticNetwork NetworkWithFeeders(int feeders, bool crankHeld)
        {
            var network = new KineticNetwork();
            network.AddSource("crank", BalanceDefaults.HandCrankDu);
            network.SetEnabled("crank", crankHeld);
            network.AddTransmission("shaft");
            network.Connect("crank", "shaft");
            for (var i = 0; i < feeders; i++)
            {
                var id = "feeder-" + i;
                network.AddConsumer(id, BalanceDefaults.BronzeFeederDu);
                network.Connect("shaft", id);
            }

            return network;
        }

        private sealed class AlwaysValidProbe : IPlacementProbe
        {
            public bool IsValid(string nodeId, string ruleId) => true;
        }
    }

    public sealed class TransferTests
    {
        [Fact]
        public void TwoFeedersCompeteForTheLastItemWithoutDuplication()
        {
            var service = Workshop();
            var before = service.WorldItemCount();
            var first = service.Plan("chest", "basket-a", "Wood", 1);
            var second = service.Plan("chest", "basket-b", "Wood", 1);

            Assert.True(service.TryRun(first.Id));
            Assert.False(service.TryRun(second.Id));
            Assert.Equal(TransferState.Planned, service.Get(second.Id).State);
            Assert.Equal("Missing input", service.Get(second.Id).Reason);
            Assert.Equal(before, service.WorldItemCount());
            Assert.Equal(1, service.GetBuffer("basket-a").CountOf("Wood"));
            Assert.Equal(0, service.GetBuffer("basket-b").CountOf("Wood"));
            Assert.Equal(0, service.GetBuffer("chest").CountOf("Wood"));
        }

        [Fact]
        public void PlayerRemovingTheItemAfterPlanningBlocksTheTransfer()
        {
            var service = Workshop();
            var transfer = service.Plan("chest", "basket-a", "Wood", 1);
            Assert.True(service.GetBuffer("chest").TryPlayerRemove("Wood", 1));
            Assert.False(service.TryRun(transfer.Id));
            Assert.Equal(0, service.WorldItemCount());
            Assert.Equal(0, service.EscrowCount());
        }

        [Fact]
        public void FillingTheDestinationAfterEscrowHoldsTheItemForRecovery()
        {
            var service = Workshop();
            service.GetBuffer("basket-a").Add(new ItemRecord("Stone", 1));
            var transfer = service.Plan("chest", "basket-a", "Wood", 1);
            service.Reserve(transfer.Id);
            service.Escrow(transfer.Id);
            service.GetBuffer("basket-a").Add(new ItemRecord("Resin", 1));
            service.Deliver(transfer.Id);

            Assert.Equal(TransferState.RecoveryRequired, service.Get(transfer.Id).State);
            Assert.Equal(1, service.EscrowCount());
            Assert.Equal(0, service.GetBuffer("basket-a").CountOf("Wood"));

            var recovery = new ItemBuffer("recovery", 4);
            var first = service.TakeRecovery(transfer.Id, recovery);
            var second = service.TakeRecovery(transfer.Id, recovery);
            Assert.Single(first);
            Assert.Empty(second);
            Assert.Equal(1, recovery.CountOf("Wood"));
            Assert.True(recovery.Snapshot().Single().Cheated);
        }

        [Fact]
        public void SaveAndReloadKeepsEscrowAndDoesNotClearProvenance()
        {
            var service = Workshop();
            var transfer = service.Plan("chest", "basket-a", "Wood", 1);
            service.Reserve(transfer.Id);
            service.Escrow(transfer.Id);
            var before = service.WorldItemCount();

            var restored = TransferService.Import(service.Export());
            Assert.Equal(before, restored.WorldItemCount());
            Assert.Equal(TransferState.Escrowed, restored.Get(transfer.Id).State);
            Assert.True(restored.Get(transfer.Id).Escrow!.Cheated);
            Assert.Equal(2, restored.Get(transfer.Id).Escrow!.Quality);
            Assert.Equal("rune", restored.Get(transfer.Id).Escrow!.CustomData);

            restored.Deliver(transfer.Id);
            restored.Commit(transfer.Id);
            var stored = restored.GetBuffer("basket-a").Snapshot().Single();
            Assert.True(stored.Cheated);
            Assert.Equal(2, stored.Quality);
            Assert.Equal("rune", stored.CustomData);
            Assert.Equal("crafter-1", stored.CrafterId);
        }

        [Fact]
        public void DifferentQualitiesDoNotMerge()
        {
            var buffer = new ItemBuffer("chest", 4);
            buffer.Add(new ItemRecord("Bronze", 1, quality: 1));
            buffer.Add(new ItemRecord("Bronze", 1, quality: 2));
            Assert.Equal(2, buffer.Snapshot().Count);
            Assert.Equal(2, buffer.CountOf("Bronze"));
        }

        private static TransferService Workshop()
        {
            var service = new TransferService();
            var chest = new ItemBuffer("chest", 4);
            chest.Add(new ItemRecord(
                "Wood",
                1,
                quality: 2,
                durability: 80f,
                variant: 3,
                crafterId: "crafter-1",
                crafterName: "Tyler",
                customData: "rune",
                cheated: true));
            service.Register(chest);
            service.Register(new ItemBuffer("basket-a", 2));
            service.Register(new ItemBuffer("basket-b", 2));
            return service;
        }
    }
}
