using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace VikingFactory.Core.Items
{
    /// <summary>
    /// A real item as a machine holds it: prefab id plus every field the game keeps on an item.
    /// Machines copy this record. They never clear quality, crafter, custom data, or the cheated flag.
    /// </summary>
    public sealed class ItemStack
    {
        public string Prefab = "";
        public int Count = 1;
        public int Quality = 1;
        public float Durability = 100f;
        public int Variant;
        public long CrafterId;
        public string CrafterName = "";
        public bool Cheated;
        public int WorldLevel;
        public Dictionary<string, string> CustomData = new Dictionary<string, string>();

        public ItemStack Copy(int count)
        {
            return new ItemStack
            {
                Prefab = Prefab,
                Count = count,
                Quality = Quality,
                Durability = Durability,
                Variant = Variant,
                CrafterId = CrafterId,
                CrafterName = CrafterName,
                Cheated = Cheated,
                WorldLevel = WorldLevel,
                CustomData = new Dictionary<string, string>(CustomData)
            };
        }

        /// <summary>Items stack only when every identity field matches. Names alone are not enough.</summary>
        public bool SameIdentity(ItemStack other)
        {
            if (other == null)
                return false;
            if (Prefab != other.Prefab || Quality != other.Quality || Variant != other.Variant || Cheated != other.Cheated)
                return false;
            if (CrafterId != other.CrafterId || CrafterName != other.CrafterName || WorldLevel != other.WorldLevel)
                return false;
            if (!Durability.Equals(other.Durability))
                return false;
            if (CustomData.Count != other.CustomData.Count)
                return false;
            foreach (var pair in CustomData)
            {
                if (!other.CustomData.TryGetValue(pair.Key, out var value) || value != pair.Value)
                    return false;
            }

            return true;
        }

        public override string ToString()
        {
            return Prefab + " x" + Count + (Quality > 1 ? " q" + Quality : "");
        }
    }

    /// <summary>
    /// Text form for a ZDO string. One row per stack, fields split by U+001F, rows by newline.
    /// Escaping is character by character, so any prefab, name, or custom-data text round-trips.
    /// </summary>
    public static class ItemCodec
    {
        private const char Row = '\n';
        private const char Field = '\u001f';

        public static string Format(IEnumerable<ItemStack> stacks)
        {
            var rows = new List<string>();
            foreach (var stack in stacks)
                rows.Add(FormatOne(stack));
            return string.Join(Row.ToString(), rows.ToArray());
        }

        public static List<ItemStack> Parse(string raw)
        {
            var list = new List<ItemStack>();
            if (string.IsNullOrEmpty(raw))
                return list;
            foreach (var row in raw.Split(Row))
            {
                if (row.Length == 0)
                    continue;
                var stack = ParseOne(row);
                if (stack != null)
                    list.Add(stack);
            }

            return list;
        }

        public static string FormatOne(ItemStack stack)
        {
            var custom = new List<string>();
            var keys = new List<string>(stack.CustomData.Keys);
            keys.Sort(StringComparer.Ordinal);
            foreach (var key in keys)
                custom.Add(Escape(key) + "=" + Escape(stack.CustomData[key]));
            return string.Join(Field.ToString(), new[]
            {
                Escape(stack.Prefab),
                stack.Count.ToString(CultureInfo.InvariantCulture),
                stack.Quality.ToString(CultureInfo.InvariantCulture),
                stack.Durability.ToString("R", CultureInfo.InvariantCulture),
                stack.Variant.ToString(CultureInfo.InvariantCulture),
                stack.CrafterId.ToString(CultureInfo.InvariantCulture),
                Escape(stack.CrafterName),
                stack.Cheated ? "1" : "0",
                stack.WorldLevel.ToString(CultureInfo.InvariantCulture),
                string.Join(";", custom.ToArray())
            });
        }

        public static ItemStack? ParseOne(string row)
        {
            var parts = row.Split(Field);
            if (parts.Length < 9)
                return null;
            int count, quality, variant, worldLevel;
            float durability;
            long crafter;
            if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out count)
                || !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out quality)
                || !float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out durability)
                || !int.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out variant)
                || !long.TryParse(parts[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out crafter)
                || !int.TryParse(parts[8], NumberStyles.Integer, CultureInfo.InvariantCulture, out worldLevel))
                return null;
            if (count < 1)
                return null;
            var stack = new ItemStack
            {
                Prefab = Unescape(parts[0]),
                Count = count,
                Quality = quality,
                Durability = durability,
                Variant = variant,
                CrafterId = crafter,
                CrafterName = Unescape(parts[6]),
                Cheated = parts[7] == "1",
                WorldLevel = worldLevel
            };
            if (parts.Length > 9 && parts[9].Length > 0)
            {
                foreach (var pair in SplitUnescaped(parts[9], ';'))
                {
                    var cut = IndexOfUnescaped(pair, '=');
                    if (cut <= 0)
                        continue;
                    stack.CustomData[Unescape(pair.Substring(0, cut))] = Unescape(pair.Substring(cut + 1));
                }
            }

            return stack;
        }

        public static string Escape(string value)
        {
            var text = value ?? "";
            var builder = new StringBuilder(text.Length);
            foreach (var c in text)
            {
                switch (c)
                {
                    case '\\': builder.Append("\\\\"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '=': builder.Append("\\e"); break;
                    case ';': builder.Append("\\s"); break;
                    case '\u001f': builder.Append("\\f"); break;
                    default: builder.Append(c); break;
                }
            }

            return builder.ToString();
        }

        public static string Unescape(string value)
        {
            var text = value ?? "";
            var builder = new StringBuilder(text.Length);
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (c != '\\' || i + 1 >= text.Length)
                {
                    builder.Append(c);
                    continue;
                }

                var next = text[++i];
                switch (next)
                {
                    case '\\': builder.Append('\\'); break;
                    case 'n': builder.Append('\n'); break;
                    case 'e': builder.Append('='); break;
                    case 's': builder.Append(';'); break;
                    case 'f': builder.Append('\u001f'); break;
                    default: builder.Append(next); break;
                }
            }

            return builder.ToString();
        }

        private static IEnumerable<string> SplitUnescaped(string text, char separator)
        {
            var start = 0;
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] == '\\')
                {
                    i++;
                    continue;
                }

                if (text[i] == separator)
                {
                    yield return text.Substring(start, i - start);
                    start = i + 1;
                }
            }

            yield return text.Substring(start);
        }

        private static int IndexOfUnescaped(string text, char target)
        {
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] == '\\')
                {
                    i++;
                    continue;
                }

                if (text[i] == target)
                    return i;
            }

            return -1;
        }
    }

    /// <summary>A small slot-limited store of ItemStacks, as a basket or machine buffer holds them.</summary>
    public sealed class StackStore
    {
        private readonly List<ItemStack> _stacks;

        public StackStore(int slotLimit, int maxPerSlot = int.MaxValue, IEnumerable<ItemStack>? initial = null)
        {
            if (slotLimit < 1)
                throw new ArgumentOutOfRangeException(nameof(slotLimit));
            SlotLimit = slotLimit;
            MaxPerSlot = Math.Max(1, maxPerSlot);
            _stacks = initial != null ? new List<ItemStack>(initial) : new List<ItemStack>();
        }

        public int SlotLimit { get; }
        public int MaxPerSlot { get; }
        public IReadOnlyList<ItemStack> Stacks => _stacks;

        public int Total()
        {
            var total = 0;
            foreach (var stack in _stacks)
                total += stack.Count;
            return total;
        }

        public int CountOf(string prefab)
        {
            var total = 0;
            foreach (var stack in _stacks)
            {
                if (stack.Prefab == prefab)
                    total += stack.Count;
            }

            return total;
        }

        public bool CanAccept(ItemStack stack)
        {
            var remaining = stack.Count;
            foreach (var existing in _stacks)
            {
                if (existing.SameIdentity(stack))
                    remaining -= Math.Max(0, MaxPerSlot - existing.Count);
            }

            if (remaining <= 0)
                return true;
            var freeSlots = SlotLimit - _stacks.Count;
            return (long)freeSlots * MaxPerSlot >= remaining;
        }

        public bool TryAdd(ItemStack stack)
        {
            if (stack == null || stack.Count < 1 || !CanAccept(stack))
                return false;
            var remaining = stack.Count;
            foreach (var existing in _stacks)
            {
                if (remaining <= 0)
                    break;
                if (!existing.SameIdentity(stack))
                    continue;
                var room = Math.Max(0, MaxPerSlot - existing.Count);
                var take = Math.Min(room, remaining);
                existing.Count += take;
                remaining -= take;
            }

            while (remaining > 0)
            {
                var take = Math.Min(MaxPerSlot, remaining);
                _stacks.Add(stack.Copy(take));
                remaining -= take;
            }

            return true;
        }

        public ItemStack? Peek(Func<ItemStack, bool>? filter = null)
        {
            foreach (var stack in _stacks)
            {
                if (filter == null || filter(stack))
                    return stack;
            }

            return null;
        }

        /// <summary>Takes one item of the first matching stack. The returned copy keeps every field.</summary>
        public ItemStack? TakeOne(Func<ItemStack, bool>? filter = null)
        {
            for (var i = 0; i < _stacks.Count; i++)
            {
                var stack = _stacks[i];
                if (filter != null && !filter(stack))
                    continue;
                var one = stack.Copy(1);
                stack.Count -= 1;
                if (stack.Count <= 0)
                    _stacks.RemoveAt(i);
                return one;
            }

            return null;
        }

        /// <summary>Takes an exact count of one prefab, oldest stacks first. Returns null and changes nothing if short.</summary>
        public List<ItemStack>? TakeExact(string prefab, int count)
        {
            if (CountOf(prefab) < count)
                return null;
            var taken = new List<ItemStack>();
            var remaining = count;
            for (var i = 0; i < _stacks.Count && remaining > 0; i++)
            {
                var stack = _stacks[i];
                if (stack.Prefab != prefab)
                    continue;
                var take = Math.Min(stack.Count, remaining);
                taken.Add(stack.Copy(take));
                stack.Count -= take;
                remaining -= take;
                if (stack.Count <= 0)
                {
                    _stacks.RemoveAt(i);
                    i--;
                }
            }

            return taken;
        }

        public List<ItemStack> TakeAll()
        {
            var all = new List<ItemStack>(_stacks);
            _stacks.Clear();
            return all;
        }
    }
}
