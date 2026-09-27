using System;
using VikingFactory.Core.Items;

namespace VikingFactory.Core.Logistics
{
    public enum SplitMode
    {
        /// <summary>Fair rotation among outputs that can take the item.</summary>
        RoundRobin,
        /// <summary>Output 0 first, then overflow to the rest in order. This is the manifold splitter.</summary>
        Priority,
        /// <summary>Matching items to output 0. Unmatched items rotate among the rest, or stop.</summary>
        Filter
    }

    /// <summary>
    /// A ghost selection. It is not a consumed item. Match by prefab, and by quality when one is set.
    /// </summary>
    public sealed class ItemFilter
    {
        public ItemFilter(string prefab, int quality = 0)
        {
            Prefab = prefab ?? "";
            Quality = quality;
        }

        public string Prefab { get; }
        public int Quality { get; }
        public bool IsEmpty => Prefab.Length == 0;

        public bool Matches(ItemStack item)
        {
            if (IsEmpty || item == null)
                return false;
            return item.Prefab == Prefab && (Quality <= 0 || item.Quality == Quality);
        }

        public string Save()
        {
            return IsEmpty ? "" : Prefab + (Quality > 0 ? "@" + Quality : "");
        }

        public static ItemFilter Load(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return new ItemFilter("");
            var at = raw.LastIndexOf('@');
            if (at > 0 && int.TryParse(raw.Substring(at + 1), out var quality))
                return new ItemFilter(raw.Substring(0, at), quality);
            return new ItemFilter(raw);
        }
    }

    public sealed class Splitter
    {
        private int _next;

        public Splitter(int outputs)
        {
            if (outputs < 2)
                throw new ArgumentOutOfRangeException(nameof(outputs));
            Outputs = outputs;
        }

        public int Outputs { get; }
        public SplitMode Mode { get; set; } = SplitMode.RoundRobin;
        public ItemFilter Filter { get; set; } = new ItemFilter("");
        public bool StopUnmatched { get; set; }

        /// <summary>Returns the output index for this item, or -1 to hold it. Pure and deterministic.</summary>
        public int Choose(ItemStack item, Func<int, bool> canAccept)
        {
            switch (Mode)
            {
                case SplitMode.Priority:
                    for (var i = 0; i < Outputs; i++)
                    {
                        if (canAccept(i))
                            return i;
                    }

                    return -1;
                case SplitMode.Filter:
                    if (Filter.Matches(item))
                        return canAccept(0) ? 0 : -1;
                    if (StopUnmatched)
                        return -1;
                    return Rotate(canAccept, 1);
                default:
                    return Rotate(canAccept, 0);
            }
        }

        private int Rotate(Func<int, bool> canAccept, int first)
        {
            var span = Outputs - first;
            for (var step = 0; step < span; step++)
            {
                var index = first + (_next + step) % span;
                if (!canAccept(index))
                    continue;
                _next = (index - first + 1) % span;
                return index;
            }

            return -1;
        }
    }

    /// <summary>Fair rotating input preference, so one busy input cannot starve the others.</summary>
    public sealed class Merger
    {
        private int _next;

        public Merger(int inputs)
        {
            if (inputs < 2)
                throw new ArgumentOutOfRangeException(nameof(inputs));
            Inputs = inputs;
        }

        public int Inputs { get; }

        public int Choose(Func<int, bool> hasItem)
        {
            for (var step = 0; step < Inputs; step++)
            {
                var index = (_next + step) % Inputs;
                if (!hasItem(index))
                    continue;
                _next = (index + 1) % Inputs;
                return index;
            }

            return -1;
        }
    }

    /// <summary>
    /// Stock target with high and low thresholds. Delivery stops at High and resumes at Low,
    /// so a dock does not toggle on every item.
    /// </summary>
    public sealed class StockTarget
    {
        private bool _filling = true;

        public StockTarget(int high, int low = -1)
        {
            High = Math.Max(0, high);
            Low = low < 0 ? Math.Max(0, High - Math.Max(1, High / 5)) : Math.Min(low, High);
        }

        public int High { get; }
        public int Low { get; }
        public bool Enabled => High > 0;

        public bool ShouldDeliver(int current)
        {
            if (!Enabled)
                return true;
            if (current >= High)
                _filling = false;
            else if (current <= Low)
                _filling = true;
            return _filling;
        }
    }
}
