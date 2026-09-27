using System;

namespace VikingFactory.Core.Items
{
    /// <summary>
    /// A real item identity. Transfers copy this record. They do not clear provenance.
    /// </summary>
    public sealed class ItemRecord
    {
        public ItemRecord(
            string prefabId,
            int count,
            int quality = 1,
            float durability = 100f,
            int variant = 0,
            string crafterId = "",
            string crafterName = "",
            string customData = "",
            bool cheated = false)
        {
            if (string.IsNullOrWhiteSpace(prefabId))
                throw new ArgumentException("Prefab id is required.", nameof(prefabId));
            if (count < 1)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (quality < 1)
                throw new ArgumentOutOfRangeException(nameof(quality));

            PrefabId = prefabId;
            Count = count;
            Quality = quality;
            Durability = durability;
            Variant = variant;
            CrafterId = crafterId ?? "";
            CrafterName = crafterName ?? "";
            CustomData = customData ?? "";
            Cheated = cheated;
        }

        public string PrefabId { get; }
        public int Count { get; }
        public int Quality { get; }
        public float Durability { get; }
        public int Variant { get; }
        public string CrafterId { get; }
        public string CrafterName { get; }
        public string CustomData { get; }
        public bool Cheated { get; }

        public bool CanStackWith(ItemRecord other)
        {
            return other != null
                && PrefabId == other.PrefabId
                && Quality == other.Quality
                && Durability.Equals(other.Durability)
                && Variant == other.Variant
                && CrafterId == other.CrafterId
                && CrafterName == other.CrafterName
                && CustomData == other.CustomData
                && Cheated == other.Cheated;
        }

        public ItemRecord WithCount(int count)
        {
            return new ItemRecord(PrefabId, count, Quality, Durability, Variant, CrafterId, CrafterName, CustomData, Cheated);
        }
    }
}
