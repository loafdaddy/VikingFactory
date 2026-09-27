using System;
using System.Collections.Generic;
using UnityEngine;
using VikingFactory.Core.Items;

namespace VikingFactory.Machines
{
    /// <summary>Collects items a native action already spawned. Only owned drops of the expected prefabs are taken.</summary>
    public static class WorldDrops
    {
        private static int _itemMask = -1;

        public static int Collect(Vector3 center, float radius, ICollection<string> prefabs, StackStore store, int max = 20)
        {
            if (_itemMask < 0)
                _itemMask = LayerMask.GetMask("item");
            var taken = 0;
            foreach (var hit in Physics.OverlapSphere(center, radius, _itemMask))
            {
                if (taken >= max)
                    break;
                var drop = hit.GetComponentInParent<ItemDrop>();
                if (drop == null || drop.m_itemData == null)
                    continue;
                var name = drop.gameObject.name.Replace("(Clone)", "").Trim();
                if (prefabs != null && !prefabs.Contains(name))
                    continue;
                if (!drop.CanPickup(false))
                {
                    drop.RequestOwn();
                    continue;
                }

                drop.Load();
                while (taken < max && drop != null)
                {
                    var one = ItemBridge.FromItemData(drop.m_itemData, 1);
                    if (one.Prefab.Length == 0)
                        one.Prefab = name;
                    if (!store.CanAccept(one))
                        return taken;
                    var last = drop.m_itemData.m_stack <= 1;
                    if (!drop.RemoveOne())
                        break;
                    store.TryAdd(one);
                    taken++;
                    if (last)
                        break;
                }
            }

            return taken;
        }

        public static HashSet<string> Names(DropTable table)
        {
            var set = new HashSet<string>();
            if (table == null || table.m_drops == null)
                return set;
            foreach (var drop in table.m_drops)
            {
                if (drop.m_item != null)
                    set.Add(drop.m_item.name);
            }

            return set;
        }

        public static float GroundHeight(Vector3 position)
        {
            if (ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(position, out var height))
                return height;
            return position.y;
        }
    }

    /// <summary>
    /// Planting through the game's own sapling prefabs, as the cultivator places them. Biome, cultivated
    /// ground, and grow space are checked the way the plant itself checks them; growth stays native.
    /// </summary>
    public static class Planting
    {
        private static Dictionary<string, List<GameObject>> _bySeed;

        public static void Reset()
        {
            _bySeed = null;
        }

        private static void Build()
        {
            _bySeed = new Dictionary<string, List<GameObject>>();
            var cultivator = ItemBridge.Prefab("Cultivator");
            var table = cultivator != null ? cultivator.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces : null;
            if (table == null)
                return;
            foreach (var go in table.m_pieces)
            {
                var piece = go != null ? go.GetComponent<Piece>() : null;
                if (piece == null || go.GetComponent<Plant>() == null || piece.m_resources == null || piece.m_resources.Length == 0)
                    continue;
                var seed = piece.m_resources[0].m_resItem;
                if (seed == null)
                    continue;
                if (!_bySeed.TryGetValue(seed.gameObject.name, out var list))
                {
                    list = new List<GameObject>();
                    _bySeed[seed.gameObject.name] = list;
                }

                list.Add(go);
            }
        }

        public static IReadOnlyList<GameObject> SaplingsFor(string seedPrefab)
        {
            if (_bySeed == null)
                Build();
            return _bySeed.TryGetValue(seedPrefab, out var list) ? list : (IReadOnlyList<GameObject>)new List<GameObject>();
        }

        public static bool IsSeed(string prefab)
        {
            return SaplingsFor(prefab).Count > 0;
        }

        public static int Cost(GameObject sapling)
        {
            var piece = sapling.GetComponent<Piece>();
            return piece != null && piece.m_resources.Length > 0 ? Mathf.Max(1, piece.m_resources[0].m_amount) : 1;
        }

        /// <summary>The item the grown pickable yields, for reserve planning.</summary>
        public static string YieldOf(GameObject sapling)
        {
            var plant = sapling.GetComponent<Plant>();
            if (plant == null || plant.m_grownPrefabs == null)
                return "";
            foreach (var grown in plant.m_grownPrefabs)
            {
                var pickable = grown != null ? grown.GetComponent<Pickable>() : null;
                if (pickable != null && pickable.m_itemPrefab != null)
                    return pickable.m_itemPrefab.name;
            }

            return "";
        }

        public static bool CanPlant(GameObject sapling, Vector3 position, out string reason)
        {
            var plant = sapling.GetComponent<Plant>();
            var piece = sapling.GetComponent<Piece>();
            reason = "";
            var heightmap = Heightmap.FindHeightmap(position);
            if (heightmap == null)
            {
                reason = "No ground here.";
                return false;
            }

            if ((plant.m_needCultivatedGround || (piece != null && piece.m_cultivatedGroundOnly)) && !heightmap.IsCultivated(position))
            {
                reason = "Ground is not cultivated.";
                return false;
            }

            if ((plant.m_biome & Heightmap.FindBiome(position)) == 0)
            {
                reason = "Wrong biome for this crop.";
                return false;
            }

            var radius = Mathf.Max(0.3f, plant.m_growRadius);
            foreach (var hit in Physics.OverlapSphere(position, radius))
            {
                if (hit.GetComponentInParent<Plant>() != null || hit.GetComponentInParent<Pickable>() != null || hit.GetComponentInParent<TreeBase>() != null)
                {
                    reason = "Space is taken.";
                    return false;
                }
            }

            return true;
        }

        public static bool Plant(GameObject sapling, Vector3 position)
        {
            position.y = WorldDrops.GroundHeight(position);
            if (!CanPlant(sapling, position, out _))
                return false;
            var go = UnityEngine.Object.Instantiate(sapling, position, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f));
            return go != null;
        }
    }
}
