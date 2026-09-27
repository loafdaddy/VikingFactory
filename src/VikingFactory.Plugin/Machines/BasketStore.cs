using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace VikingFactory.Machines
{
    public sealed class StackRecord
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
    }

    /// <summary>
    /// Small explicit buffer on the basket ZDO. Not a search of nearby chests.
    /// </summary>
    public static class BasketStore
    {
        public const int SlotLimit = 8;
        private const string Key = "vf_items";
        private const char Row = '\n';
        private const char Field = '\u001f';

        public static List<StackRecord> Read(ZDO zdo)
        {
            var list = new List<StackRecord>();
            if (zdo == null)
                return list;
            var raw = zdo.GetString(Key, "");
            if (string.IsNullOrEmpty(raw))
                return list;
            var rows = raw.Split(Row);
            for (var i = 0; i < rows.Length; i++)
            {
                if (rows[i].Length == 0)
                    continue;
                var stack = Parse(rows[i]);
                if (stack != null)
                    list.Add(stack);
            }

            return list;
        }

        public static bool TryTakeOne(ZDO zdo, out StackRecord taken)
        {
            taken = null;
            var list = Read(zdo);
            if (list.Count == 0)
                return false;
            var first = list[0];
            taken = CopyOne(first);
            first.Count -= 1;
            if (first.Count <= 0)
                list.RemoveAt(0);
            Write(zdo, list);
            return true;
        }

        public static bool TryPut(ZDO zdo, StackRecord stack)
        {
            var list = Read(zdo);
            for (var i = 0; i < list.Count; i++)
            {
                if (!SameIdentity(list[i], stack))
                    continue;
                list[i].Count += stack.Count;
                Write(zdo, list);
                return true;
            }

            if (list.Count >= SlotLimit)
                return false;
            list.Add(stack);
            Write(zdo, list);
            return true;
        }

        public static void RestoreOne(ZDO zdo, StackRecord stack)
        {
            TryPut(zdo, stack);
        }

        private static void Write(ZDO zdo, List<StackRecord> list)
        {
            var rows = new string[list.Count];
            for (var i = 0; i < list.Count; i++)
                rows[i] = Format(list[i]);
            zdo.Set(Key, string.Join(Row.ToString(), rows));
        }

        private static StackRecord CopyOne(StackRecord source)
        {
            return new StackRecord
            {
                Prefab = source.Prefab,
                Count = 1,
                Quality = source.Quality,
                Durability = source.Durability,
                Variant = source.Variant,
                CrafterId = source.CrafterId,
                CrafterName = source.CrafterName,
                Cheated = source.Cheated,
                WorldLevel = source.WorldLevel,
                CustomData = new Dictionary<string, string>(source.CustomData)
            };
        }

        private static bool SameIdentity(StackRecord a, StackRecord b)
        {
            if (a.Prefab != b.Prefab || a.Quality != b.Quality || a.Variant != b.Variant || a.Cheated != b.Cheated)
                return false;
            if (a.CrafterId != b.CrafterId || a.CrafterName != b.CrafterName || a.WorldLevel != b.WorldLevel)
                return false;
            if (!a.Durability.Equals(b.Durability))
                return false;
            if (a.CustomData.Count != b.CustomData.Count)
                return false;
            foreach (var pair in a.CustomData)
            {
                string value;
                if (!b.CustomData.TryGetValue(pair.Key, out value) || value != pair.Value)
                    return false;
            }

            return true;
        }

        private static string Format(StackRecord stack)
        {
            var custom = new List<string>();
            foreach (var pair in stack.CustomData)
                custom.Add(Escape(pair.Key) + "=" + Escape(pair.Value));
            return string.Join(Field.ToString(), new[]
            {
                Escape(stack.Prefab),
                stack.Count.ToString(CultureInfo.InvariantCulture),
                stack.Quality.ToString(CultureInfo.InvariantCulture),
                stack.Durability.ToString(CultureInfo.InvariantCulture),
                stack.Variant.ToString(CultureInfo.InvariantCulture),
                stack.CrafterId.ToString(CultureInfo.InvariantCulture),
                Escape(stack.CrafterName),
                stack.Cheated ? "1" : "0",
                stack.WorldLevel.ToString(CultureInfo.InvariantCulture),
                string.Join(";", custom.ToArray())
            });
        }

        private static StackRecord Parse(string row)
        {
            var parts = row.Split(Field);
            if (parts.Length < 9)
                return null;
            var stack = new StackRecord
            {
                Prefab = Unescape(parts[0]),
                Count = int.Parse(parts[1], CultureInfo.InvariantCulture),
                Quality = int.Parse(parts[2], CultureInfo.InvariantCulture),
                Durability = float.Parse(parts[3], CultureInfo.InvariantCulture),
                Variant = int.Parse(parts[4], CultureInfo.InvariantCulture),
                CrafterId = long.Parse(parts[5], CultureInfo.InvariantCulture),
                CrafterName = Unescape(parts[6]),
                Cheated = parts[7] == "1",
                WorldLevel = int.Parse(parts[8], CultureInfo.InvariantCulture)
            };
            if (parts.Length > 9 && parts[9].Length > 0)
            {
                var pairs = parts[9].Split(';');
                for (var i = 0; i < pairs.Length; i++)
                {
                    var cut = pairs[i].IndexOf('=');
                    if (cut <= 0)
                        continue;
                    stack.CustomData[Unescape(pairs[i].Substring(0, cut))] = Unescape(pairs[i].Substring(cut + 1));
                }
            }

            return stack.Count > 0 ? stack : null;
        }

        private static string Escape(string value)
        {
            return (value ?? "").Replace("\\", "\\\\").Replace("\n", "\\n").Replace("=", "\\e").Replace(";", "\\s");
        }

        private static string Unescape(string value)
        {
            return (value ?? "").Replace("\\s", ";").Replace("\\e", "=").Replace("\\n", "\n").Replace("\\\\", "\\");
        }
    }
}
