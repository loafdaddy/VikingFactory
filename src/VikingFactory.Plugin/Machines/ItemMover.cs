using System.Collections.Generic;
using Jotunn.Managers;

namespace VikingFactory.Machines
{
    /// <summary>
    /// Moves one real item between a basket ZDO and a chest inventory.
    /// Runs only on the owning peer. Provenance fields are copied, not cleared.
    /// </summary>
    public static class ItemMover
    {
        public static bool TryMove(WorkshopMachine sourceBasket, Container sourceChest, WorkshopMachine destBasket, Container destChest)
        {
            if (sourceBasket == null && sourceChest == null)
                return false;
            if (destBasket == null && destChest == null)
                return false;
            if (sourceChest != null && !sourceChest.IsOwner())
                return false;
            if (destChest != null && !destChest.IsOwner())
                return false;
            if (sourceBasket != null && !sourceBasket.IsLocalOwner())
                return false;
            if (destBasket != null && !destBasket.IsLocalOwner())
                return false;

            var peek = Peek(sourceBasket, sourceChest);
            if (peek == null || !CanAccept(destBasket, destChest, peek))
                return false;

            StackRecord stack;
            if (sourceBasket != null)
            {
                if (!BasketStore.TryTakeOne(sourceBasket.GetZdo(), out stack))
                    return false;
            }
            else
            {
                stack = TakeFromChest(sourceChest.GetInventory());
                if (stack == null)
                    return false;
            }

            var placed = destBasket != null
                ? BasketStore.TryPut(destBasket.GetZdo(), stack)
                : PutChest(destChest.GetInventory(), stack);
            if (placed)
                return true;

            if (sourceBasket != null)
                BasketStore.RestoreOne(sourceBasket.GetZdo(), stack);
            else
                PutChest(sourceChest.GetInventory(), stack);
            return false;
        }

        private static StackRecord Peek(WorkshopMachine basket, Container chest)
        {
            if (basket != null)
            {
                var list = BasketStore.Read(basket.GetZdo());
                if (list.Count == 0)
                    return null;
                var copy = list[0];
                copy.Count = 1;
                return copy;
            }

            var inventory = chest.GetInventory();
            if (inventory == null)
                return null;
            var items = inventory.GetAllItems();
            if (items == null || items.Count == 0)
                return null;
            return FromItem(items[0]);
        }

        private static bool CanAccept(WorkshopMachine basket, Container chest, StackRecord stack)
        {
            if (basket != null)
            {
                var list = BasketStore.Read(basket.GetZdo());
                for (var i = 0; i < list.Count; i++)
                {
                    if (list[i].Prefab == stack.Prefab && list[i].Quality == stack.Quality && list[i].Cheated == stack.Cheated)
                        return true;
                }

                return list.Count < BasketStore.SlotLimit;
            }

            var inventory = chest.GetInventory();
            var prefab = PrefabManager.Instance.GetPrefab(stack.Prefab);
            return inventory != null && prefab != null && inventory.CanAddItem(prefab, stack.Count);
        }

        private static StackRecord TakeFromChest(Inventory inventory)
        {
            if (inventory == null)
                return null;
            var items = inventory.GetAllItems();
            if (items == null || items.Count == 0)
                return null;
            var item = items[0];
            var stack = FromItem(item);
            if (!inventory.RemoveItem(item, 1))
                return null;
            stack.Count = 1;
            return stack;
        }

        private static bool PutChest(Inventory inventory, StackRecord stack)
        {
            if (inventory == null)
                return false;
            var placed = inventory.AddItem(stack.Prefab, stack.Count, stack.Quality, stack.Variant, stack.CrafterId, stack.CrafterName, stack.Cheated, true);
            if (placed == null)
                return false;
            placed.m_durability = stack.Durability;
            placed.m_worldLevel = stack.WorldLevel;
            placed.m_cheated = stack.Cheated;
            placed.m_variant = stack.Variant;
            placed.m_crafterID = stack.CrafterId;
            placed.m_crafterName = stack.CrafterName;
            if (placed.m_customData == null)
                placed.m_customData = new Dictionary<string, string>();
            foreach (var pair in stack.CustomData)
                placed.m_customData[pair.Key] = pair.Value;
            return true;
        }

        private static StackRecord FromItem(ItemDrop.ItemData item)
        {
            var stack = new StackRecord
            {
                Prefab = item.m_shared.m_name,
                Count = 1,
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
    }
}
