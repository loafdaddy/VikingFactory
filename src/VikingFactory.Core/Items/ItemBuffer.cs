using System;
using System.Collections.Generic;
using System.Linq;

namespace VikingFactory.Core.Items
{
    /// <summary>
    /// Explicit inventory. Reserved counts cannot be removed by a competing actor.
    /// </summary>
    public sealed class ItemBuffer
    {
        private readonly List<Stack> _stacks = new List<Stack>();

        public string Name { get; }
        public int SlotCount { get; }

        public ItemBuffer(string name, int slotCount)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Buffer name is required.", nameof(name));
            if (slotCount < 1)
                throw new ArgumentOutOfRangeException(nameof(slotCount));
            Name = name;
            SlotCount = slotCount;
        }

        public int CountOf(string prefabId)
        {
            return _stacks.Where(s => s.Record.PrefabId == prefabId).Sum(s => s.Record.Count);
        }

        public int TotalItems()
        {
            return _stacks.Sum(s => s.Record.Count);
        }

        public IReadOnlyList<ItemRecord> Snapshot()
        {
            return _stacks.Select(s => s.Record).ToList();
        }

        public void Add(ItemRecord record)
        {
            var existing = _stacks.FirstOrDefault(s => s.Record.CanStackWith(record) && s.Reserved == 0);
            if (existing != null)
            {
                existing.Record = existing.Record.WithCount(existing.Record.Count + record.Count);
                return;
            }

            if (_stacks.Count >= SlotCount)
                throw new InvalidOperationException("Buffer '" + Name + "' has no free slot.");

            _stacks.Add(new Stack { Record = record, Reserved = 0 });
        }

        public bool CanAccept(ItemRecord record)
        {
            if (_stacks.Any(s => s.Record.CanStackWith(record)))
                return true;
            return _stacks.Count < SlotCount;
        }

        public int Available(string prefabId)
        {
            return _stacks.Where(s => s.Record.PrefabId == prefabId).Sum(s => s.Record.Count - s.Reserved);
        }

        public bool TryReserve(string prefabId, int count, out int reservationId)
        {
            reservationId = 0;
            if (count < 1 || Available(prefabId) < count)
                return false;

            var remaining = count;
            var id = NextReservationId();
            foreach (var stack in _stacks.Where(s => s.Record.PrefabId == prefabId))
            {
                var free = stack.Record.Count - stack.Reserved;
                if (free <= 0)
                    continue;
                var take = Math.Min(free, remaining);
                stack.Reserved += take;
                stack.ReservationIds.Add(id);
                remaining -= take;
                if (remaining == 0)
                    break;
            }

            reservationId = id;
            return true;
        }

        public bool TryPlayerRemove(string prefabId, int count)
        {
            if (Available(prefabId) < count)
                return false;

            var remaining = count;
            for (var i = 0; i < _stacks.Count && remaining > 0; i++)
            {
                var stack = _stacks[i];
                if (stack.Record.PrefabId != prefabId)
                    continue;
                var free = stack.Record.Count - stack.Reserved;
                var take = Math.Min(free, remaining);
                if (take == 0)
                    continue;
                remaining -= take;
                if (stack.Record.Count == take)
                {
                    _stacks.RemoveAt(i);
                    i--;
                }
                else
                {
                    stack.Record = stack.Record.WithCount(stack.Record.Count - take);
                }
            }

            return remaining == 0;
        }

        public ItemRecord? DetachReserved(int reservationId, string prefabId, int count)
        {
            var held = _stacks.Where(s => s.ReservationIds.Contains(reservationId)).Sum(s => s.Reserved);
            if (held < count)
                return null;

            ItemRecord? prototype = null;
            var remaining = count;
            for (var i = 0; i < _stacks.Count && remaining > 0; i++)
            {
                var stack = _stacks[i];
                if (!stack.ReservationIds.Contains(reservationId))
                    continue;
                prototype = stack.Record;
                var take = Math.Min(stack.Reserved, remaining);
                stack.Reserved -= take;
                remaining -= take;
                if (stack.Reserved == 0)
                    stack.ReservationIds.Remove(reservationId);
                if (stack.Record.Count == take)
                {
                    _stacks.RemoveAt(i);
                    i--;
                }
                else
                {
                    stack.Record = stack.Record.WithCount(stack.Record.Count - take);
                }
            }

            return prototype == null ? null : prototype.WithCount(count);
        }

        public void ReleaseReservation(int reservationId)
        {
            foreach (var stack in _stacks)
            {
                if (!stack.ReservationIds.Contains(reservationId))
                    continue;
                stack.ReservationIds.Remove(reservationId);
                if (stack.ReservationIds.Count == 0)
                    stack.Reserved = 0;
            }
        }

        public void RestoreFrom(IEnumerable<ItemRecord> records)
        {
            _stacks.Clear();
            foreach (var record in records)
                Add(record);
        }

        private int _nextReservation = 1;

        private int NextReservationId()
        {
            return _nextReservation++;
        }

        private sealed class Stack
        {
            public ItemRecord Record = null!;
            public int Reserved;
            public List<int> ReservationIds { get; } = new List<int>();
        }
    }
}
