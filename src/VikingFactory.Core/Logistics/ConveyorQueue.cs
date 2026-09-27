using System;
using System.Collections.Generic;
using System.Globalization;
using VikingFactory.Core.Items;

namespace VikingFactory.Core.Logistics
{
    /// <summary>
    /// The logical queue a belt or trough owns. Items enter at the back, travel for a fixed time,
    /// and wait at the front until the next endpoint takes them. A full front blocks the belt,
    /// which blocks whatever feeds it. Nothing overtakes and nothing is discarded.
    /// </summary>
    public sealed class ConveyorQueue
    {
        private readonly List<Entry> _items = new List<Entry>();
        private double _sinceAdmit;

        public ConveyorQueue(int capacity, double travelSeconds, double itemsPerMinute)
        {
            if (capacity < 1)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            if (travelSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(travelSeconds));
            if (itemsPerMinute <= 0)
                throw new ArgumentOutOfRangeException(nameof(itemsPerMinute));
            Capacity = capacity;
            TravelSeconds = travelSeconds;
            AdmitInterval = 60.0 / itemsPerMinute;
            _sinceAdmit = AdmitInterval;
        }

        public int Capacity { get; }
        public double TravelSeconds { get; }
        public double AdmitInterval { get; }
        public int Count => _items.Count;

        public IEnumerable<ItemStack> Items
        {
            get
            {
                foreach (var entry in _items)
                    yield return entry.Item;
            }
        }

        public bool CanAccept()
        {
            return _items.Count < Capacity && _sinceAdmit >= AdmitInterval;
        }

        /// <summary>Only single items ride a belt. A stack must be split by its source first.</summary>
        public bool TryAdmit(ItemStack item)
        {
            if (item == null || item.Count != 1 || !CanAccept())
                return false;
            _items.Add(new Entry(item, 0));
            _sinceAdmit = 0;
            return true;
        }

        /// <summary>Unpowered belts do not move and do not admit.</summary>
        public void Advance(double dtSeconds, bool powered, double speedFactor = 1.0)
        {
            if (!powered || dtSeconds <= 0)
                return;
            var step = dtSeconds * Math.Max(0, speedFactor);
            _sinceAdmit += step;
            var limit = TravelSeconds;
            for (var i = 0; i < _items.Count; i++)
            {
                var entry = _items[i];
                entry.Age = Math.Min(limit, entry.Age + step);
                _items[i] = entry;
                // The next item may not pass the one ahead of it.
                limit = Math.Max(0, entry.Age - AdmitInterval);
            }
        }

        public ItemStack? PeekReady()
        {
            if (_items.Count == 0 || _items[0].Age < TravelSeconds)
                return null;
            return _items[0].Item;
        }

        public ItemStack? TakeReady()
        {
            var item = PeekReady();
            if (item != null)
                _items.RemoveAt(0);
            return item;
        }

        public List<ItemStack> TakeAll()
        {
            var all = new List<ItemStack>();
            foreach (var entry in _items)
                all.Add(entry.Item);
            _items.Clear();
            return all;
        }

        public string Save()
        {
            var rows = new List<string>();
            foreach (var entry in _items)
                rows.Add(entry.Age.ToString("R", CultureInfo.InvariantCulture) + "\u001e" + ItemCodec.FormatOne(entry.Item));
            return string.Join("\n", rows.ToArray());
        }

        public void Load(string raw)
        {
            _items.Clear();
            if (string.IsNullOrEmpty(raw))
                return;
            foreach (var row in raw.Split('\n'))
            {
                var cut = row.IndexOf('\u001e');
                if (cut <= 0)
                    continue;
                if (!double.TryParse(row.Substring(0, cut), NumberStyles.Float, CultureInfo.InvariantCulture, out var age))
                    continue;
                var item = ItemCodec.ParseOne(row.Substring(cut + 1));
                if (item == null)
                    continue;
                // A saved queue is restored even beyond capacity. Loading never discards an item.
                _items.Add(new Entry(item, Math.Max(0, Math.Min(TravelSeconds, age))));
            }
        }

        private struct Entry
        {
            public Entry(ItemStack item, double age)
            {
                Item = item;
                Age = age;
            }

            public ItemStack Item;
            public double Age;
        }
    }
}
