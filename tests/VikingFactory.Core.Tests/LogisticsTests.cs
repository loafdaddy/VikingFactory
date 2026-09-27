using System.Collections.Generic;
using System.Linq;
using VikingFactory.Core.Items;
using VikingFactory.Core.Logistics;
using Xunit;

namespace VikingFactory.Core.Tests
{
    public sealed class LogisticsTests
    {
        [Fact]
        public void CodecRoundTripsEveryFieldIncludingAwkwardText()
        {
            var stack = new ItemStack
            {
                Prefab = "Sword\\Iron",
                Count = 3,
                Quality = 4,
                Durability = 12.345f,
                Variant = 2,
                CrafterId = -1234567890123L,
                CrafterName = "Ty;ler=\\s\n\u001f",
                Cheated = true,
                WorldLevel = 2,
                CustomData = new Dictionary<string, string> { ["a=b;c"] = "\\e", ["rune"] = "x\ny" }
            };
            var back = ItemCodec.Parse(ItemCodec.Format(new[] { stack, stack.Copy(1) }));
            Assert.Equal(2, back.Count);
            Assert.True(back[0].SameIdentity(stack));
            Assert.Equal(3, back[0].Count);
            Assert.Equal("\\e", back[0].CustomData["a=b;c"]);
            Assert.Equal("x\ny", back[0].CustomData["rune"]);
            Assert.Empty(ItemCodec.Parse("junk\nmore junk"));
        }

        [Fact]
        public void StoreDoesNotMergeDifferentIdentitiesAndRefusesWhenFull()
        {
            var store = new StackStore(4);
            store.TryAdd(new ItemStack { Prefab = "Bronze" });
            store.TryAdd(new ItemStack { Prefab = "Bronze", Quality = 2 });
            store.TryAdd(new ItemStack { Prefab = "Bronze", Cheated = true });
            store.TryAdd(new ItemStack { Prefab = "Bronze" });
            Assert.Equal(3, store.Stacks.Count);

            var small = new StackStore(1, 2);
            Assert.True(small.TryAdd(new ItemStack { Prefab = "Wood", Count = 2 }));
            Assert.False(small.TryAdd(new ItemStack { Prefab = "Wood" }));
            Assert.Equal(2, small.Total());
        }

        [Fact]
        public void BeltCarriesItemsInOrderAtItsRateAndOnlyWhilePowered()
        {
            var belt = new ConveyorQueue(4, 2, 30);
            Assert.True(belt.TryAdmit(Item("A")));
            Assert.False(belt.TryAdmit(Item("B")));
            belt.Advance(5, false);
            Assert.Null(belt.PeekReady());
            belt.Advance(2, true);
            Assert.True(belt.TryAdmit(Item("B")));
            Assert.Equal("A", belt.TakeReady()!.Prefab);
            Assert.Null(belt.TakeReady());
            belt.Advance(2, true);
            Assert.Equal("B", belt.TakeReady()!.Prefab);
        }

        [Fact]
        public void BlockedBeltBacksUpAndNeverDropsItems()
        {
            var belt = new ConveyorQueue(3, 1, 60);
            var admitted = 0;
            for (var i = 0; i < 20; i++)
            {
                if (belt.TryAdmit(Item("Ore")))
                    admitted++;
                belt.Advance(1, true);
            }

            Assert.Equal(3, admitted);
            Assert.Equal(3, belt.Count);
        }

        [Fact]
        public void BeltSaveAndLoadKeepsItemsAndProgress()
        {
            var belt = new ConveyorQueue(4, 2, 60);
            belt.TryAdmit(new ItemStack { Prefab = "Iron", Cheated = true });
            belt.Advance(1.5, true);
            var reloaded = new ConveyorQueue(4, 2, 60);
            reloaded.Load(belt.Save());
            reloaded.Advance(0.5, true);
            Assert.True(reloaded.TakeReady()!.Cheated);
        }

        [Fact]
        public void SplittersRotatePrioritiseAndFilter()
        {
            var splitter = new Splitter(3);
            Assert.Equal(new[] { 0, 1, 2, 0 }, Enumerable.Range(0, 4).Select(_ => splitter.Choose(Item("Ore"), _ => true)));
            Assert.Equal(-1, splitter.Choose(Item("Ore"), _ => false));

            var priority = new Splitter(3) { Mode = SplitMode.Priority };
            Assert.Equal(0, priority.Choose(Item("Ore"), _ => true));
            Assert.Equal(1, priority.Choose(Item("Ore"), i => i != 0));

            var filter = new Splitter(3) { Mode = SplitMode.Filter, Filter = new ItemFilter("Coal") };
            Assert.Equal(0, filter.Choose(Item("Coal"), _ => true));
            Assert.Equal(-1, filter.Choose(Item("Coal"), i => i != 0));
            Assert.Equal(1, filter.Choose(Item("Ore"), _ => true));
            filter.StopUnmatched = true;
            Assert.Equal(-1, filter.Choose(Item("Ore"), _ => true));

            var quality = ItemFilter.Load(new ItemFilter("Sword", 2).Save());
            Assert.False(quality.Matches(new ItemStack { Prefab = "Sword", Quality = 1 }));
        }

        [Fact]
        public void MergerRotatesAndStockTargetUsesHysteresis()
        {
            var merger = new Merger(3);
            Assert.Equal(new[] { 0, 1, 2, 0 }, Enumerable.Range(0, 4).Select(_ => merger.Choose(_ => true)));

            var target = new StockTarget(10, 5);
            Assert.True(target.ShouldDeliver(9));
            Assert.False(target.ShouldDeliver(10));
            Assert.False(target.ShouldDeliver(7));
            Assert.True(target.ShouldDeliver(5));
        }

        private static ItemStack Item(string prefab) => new ItemStack { Prefab = prefab };
    }
}
