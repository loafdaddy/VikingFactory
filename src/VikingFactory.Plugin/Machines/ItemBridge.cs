using System.Collections.Generic;
using Jotunn.Managers;
using UnityEngine;
using VikingFactory.Core.Items;

namespace VikingFactory.Machines
{
    /// <summary>
    /// Converts between the game's ItemData and the core ItemStack. Every provenance field is copied.
    /// A durability below zero means a freshly made item, which takes the item's own maximum.
    /// </summary>
    public static class ItemBridge
    {
        public static GameObject Prefab(string prefab)
        {
            if (string.IsNullOrEmpty(prefab))
                return null;
            GameObject go = null;
            if (ObjectDB.instance != null)
                go = ObjectDB.instance.GetItemPrefab(prefab);
            if (go == null && PrefabManager.Instance != null)
                go = PrefabManager.Instance.GetPrefab(prefab);
            return go != null && go.GetComponent<ItemDrop>() != null ? go : null;
        }

        public static string PrefabName(ItemDrop.ItemData item)
        {
            if (item == null)
                return "";
            if (item.m_dropPrefab != null)
                return item.m_dropPrefab.name;
            if (ObjectDB.instance != null)
            {
                var go = ObjectDB.instance.GetItemPrefab(item.m_shared);
                if (go != null)
                    return go.name;
            }

            return "";
        }

        public static ItemStack FromItemData(ItemDrop.ItemData item, int count)
        {
            var stack = new ItemStack
            {
                Prefab = PrefabName(item),
                Count = count,
                Quality = item.m_quality,
                Durability = item.m_durability,
                Variant = item.m_variant,
                CrafterId = item.m_crafterID,
                CrafterName = item.m_crafterName ?? "",
                Cheated = item.m_cheated,
                WorldLevel = item.m_worldLevel
            };
            if (item.m_customData != null)
            {
                foreach (var pair in item.m_customData)
                    stack.CustomData[pair.Key] = pair.Value;
            }

            return stack;
        }

        public static ItemDrop.ItemData ToItemData(ItemStack stack)
        {
            var go = Prefab(stack.Prefab);
            if (go == null)
                return null;
            var data = go.GetComponent<ItemDrop>().m_itemData.Clone();
            data.m_dropPrefab = go;
            data.m_stack = stack.Count;
            data.m_quality = Mathf.Max(1, stack.Quality);
            data.m_variant = stack.Variant;
            data.m_durability = stack.Durability < 0f ? data.GetMaxDurability() : stack.Durability;
            data.m_crafterID = stack.CrafterId;
            data.m_crafterName = stack.CrafterName ?? "";
            data.m_worldLevel = stack.WorldLevel;
            data.m_cheated = stack.Cheated;
            data.m_customData = new Dictionary<string, string>(stack.CustomData);
            return data;
        }

        public static int MaxStack(string prefab)
        {
            var go = Prefab(prefab);
            return go == null ? 1 : Mathf.Max(1, go.GetComponent<ItemDrop>().m_itemData.m_shared.m_maxStackSize);
        }

        public static bool Matches(ItemStack stack, ItemDrop.ItemData item)
        {
            return item != null
                && PrefabName(item) == stack.Prefab
                && item.m_quality == stack.Quality
                && item.m_variant == stack.Variant
                && item.m_cheated == stack.Cheated
                && item.m_crafterID == stack.CrafterId
                && item.m_worldLevel == stack.WorldLevel;
        }

        public static bool CanAdd(Inventory inventory, ItemStack stack)
        {
            var data = ToItemData(stack);
            return inventory != null && data != null && inventory.CanAddItem(data, stack.Count);
        }

        /// <summary>Adds through the game's own inventory, so native stacking rules apply.</summary>
        public static bool Add(Inventory inventory, ItemStack stack)
        {
            var data = ToItemData(stack);
            if (inventory == null || data == null || !inventory.CanAddItem(data, stack.Count))
                return false;
            return inventory.AddItem(data);
        }

        public static bool RemoveOne(Inventory inventory, ItemStack stack)
        {
            if (inventory == null)
                return false;
            var items = inventory.GetAllItems();
            for (var i = 0; i < items.Count; i++)
            {
                if (Matches(stack, items[i]))
                    return inventory.RemoveItem(items[i], 1);
            }

            return false;
        }

        /// <summary>Drops real items into the world, split into native stack sizes. Used for recovery and destruction.</summary>
        public static void Drop(ItemStack stack, Vector3 position)
        {
            var max = MaxStack(stack.Prefab);
            var remaining = stack.Count;
            while (remaining > 0)
            {
                var take = Mathf.Min(max, remaining);
                var data = ToItemData(stack.Copy(take));
                if (data == null)
                {
                    Debug.LogWarning("[VikingFactory] Could not drop " + stack + ": prefab not found.");
                    return;
                }

                var offset = Random.insideUnitSphere * 0.3f;
                offset.y = Mathf.Abs(offset.y);
                ItemDrop.DropItem(data, take, position + Vector3.up * 0.5f + offset, Quaternion.identity);
                remaining -= take;
            }
        }
    }
}
