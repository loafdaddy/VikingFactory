using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace VikingFactory
{
    public sealed class ExportCatalogueCommand : ConsoleCommand
    {
        public override string Name => "vf_catalogue";

        public override string Help => "Write the live item, recipe, and station catalogue.";

        public override void Run(string[] args)
        {
            var path = CatalogueExport.Write();
            global::Console.instance?.Print("VikingFactory catalogue: " + path);
        }
    }

    public static class CatalogueExport
    {
        public static string Write()
        {
            var directory = Path.Combine(Paths.ConfigPath, "VikingFactory");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "catalogue.json");
            File.WriteAllText(path, Build(), new UTF8Encoding(false));
            return path;
        }

        private static string Build()
        {
            var json = new StringBuilder();
            json.Append("{\n");
            var db = BestDatabase();
            var scene = ZNetScene.instance;
            var itemObjects = FromCache(typeof(ItemDrop));
            if (db != null && db.m_items != null && db.m_items.Count > itemObjects.Count)
                itemObjects = db.m_items;
            var prefabs = scene != null && scene.m_prefabs != null && scene.m_prefabs.Count > 0
                ? scene.m_prefabs
                : null;
            json.Append("  \"objectDb\": ").Append(db != null ? "true" : "false").Append(",\n");
            json.Append("  \"prefabScene\": ").Append(scene != null ? "true" : "false").Append(",\n");
            json.Append("  \"woodPrefab\": ").Append(PrefabManager.Instance != null && PrefabManager.Instance.GetPrefab("Wood") != null ? "true" : "false").Append(",\n");

            var items = itemObjects;
            var recipes = db != null ? db.m_recipes : null;
            json.Append("  \"itemCount\": ").Append(items != null ? items.Count : 0).Append(",\n");
            json.Append("  \"recipeCount\": ").Append(recipes != null ? recipes.Count : 0).Append(",\n");

            json.Append("  \"items\": [\n");
            WriteItems(json, items);
            json.Append("  ],\n");

            json.Append("  \"recipes\": [\n");
            WriteRecipes(json, recipes);
            json.Append("  ],\n");

            json.Append("  \"smelters\": [\n");
            WriteSmelters(json, prefabs ?? FromCache(typeof(Smelter)));
            json.Append("  ],\n");
            json.Append("  \"cooking\": [\n");
            WriteCooking(json, prefabs ?? FromCache(typeof(CookingStation)));
            json.Append("  ],\n");
            json.Append("  \"fermenters\": [\n");
            WriteFermenters(json, prefabs ?? FromCache(typeof(Fermenter)));
            json.Append("  ],\n");
            json.Append("  \"plants\": [\n");
            WritePlants(json, prefabs ?? FromCache(typeof(Plant)));
            json.Append("  ],\n");
            json.Append("  \"beehives\": [\n");
            WriteBeehives(json, prefabs ?? FromCache(typeof(Beehive)));
            json.Append("  ],\n");
            json.Append("  \"sapCollectors\": [\n");
            WriteSap(json, prefabs ?? FromCache(typeof(SapCollector)));
            json.Append("  ],\n");
            json.Append("  \"pickables\": [\n");
            WritePickables(json, prefabs ?? FromCache(typeof(Pickable)));
            json.Append("  ],\n");
            json.Append("  \"pieces\": [\n");
            WritePieces(json, prefabs ?? FromCache(typeof(Piece)));
            json.Append("  ],\n");
            json.Append("  \"creatureDrops\": [\n");
            WriteCreatureDrops(json, prefabs ?? FromCache(typeof(CharacterDrop)));
            json.Append("  ],\n");
            json.Append("  \"traders\": [\n");
            WriteTraders(json, prefabs ?? FromCache(typeof(Trader)));
            json.Append("  ]\n");
            json.Append("}\n");
            return json.ToString();
        }

        private static ObjectDB BestDatabase()
        {
            ObjectDB best = ObjectDB.instance;
            var bestCount = best != null && best.m_recipes != null ? best.m_recipes.Count : -1;
            var map = PrefabManager.Cache.GetPrefabs(typeof(ObjectDB));
            if (map == null)
                return best;
            foreach (var pair in map)
            {
                var db = pair.Value as ObjectDB;
                if (db == null || db.m_recipes == null || db.m_recipes.Count <= bestCount)
                    continue;
                best = db;
                bestCount = db.m_recipes.Count;
            }
            return best;
        }

        private static List<GameObject> FromCache(Type type)
        {
            var result = new List<GameObject>();
            var map = PrefabManager.Cache.GetPrefabs(type);
            if (map == null)
                return result;
            foreach (var pair in map)
            {
                var component = pair.Value as Component;
                if (component != null)
                    result.Add(component.gameObject);
            }
            return result;
        }

        private static void WriteItems(StringBuilder json, List<GameObject> items)
        {
            var first = true;
            if (items == null)
                return;
            for (var i = 0; i < items.Count; i++)
            {
                var go = items[i];
                if (go == null)
                    continue;
                var drop = go.GetComponent<ItemDrop>();
                var shared = drop != null && drop.m_itemData != null ? drop.m_itemData.m_shared : null;
                Comma(json, ref first);
                json.Append("    {\"prefab\": \"").Append(Esc(go.name)).Append("\"");
                json.Append(", \"name\": \"").Append(Esc(shared != null ? shared.m_name : "")).Append("\"");
                json.Append(", \"maxStack\": ").Append(shared != null ? shared.m_maxStackSize : 0);
                json.Append(", \"maxQuality\": ").Append(shared != null ? shared.m_maxQuality : 0);
                json.Append(", \"teleportable\": ").Append(shared != null && shared.m_teleportable ? "true" : "false");
                json.Append("}");
            }
            if (!first)
                json.Append("\n");
        }

        private static void WriteRecipes(StringBuilder json, List<Recipe> recipes)
        {
            var first = true;
            if (recipes == null)
                return;
            for (var i = 0; i < recipes.Count; i++)
            {
                var recipe = recipes[i];
                if (recipe == null)
                    continue;
                Comma(json, ref first);
                json.Append("    {\"output\": \"").Append(Esc(ItemName(recipe.m_item))).Append("\"");
                json.Append(", \"amount\": ").Append(recipe.m_amount);
                json.Append(", \"enabled\": ").Append(recipe.m_enabled ? "true" : "false");
                json.Append(", \"station\": \"").Append(Esc(recipe.m_craftingStation != null ? recipe.m_craftingStation.m_name : "")).Append("\"");
                json.Append(", \"stationLevel\": ").Append(recipe.m_minStationLevel);
                json.Append(", \"onlyOne\": ").Append(recipe.m_requireOnlyOneIngredient ? "true" : "false");
                json.Append(", \"upgradeOnly\": ").Append(recipe.m_noCraftOnlyUpgrade ? "true" : "false");
                json.Append(", \"resources\": [");
                var resources = recipe.m_resources;
                if (resources != null)
                {
                    for (var r = 0; r < resources.Length; r++)
                    {
                        if (r > 0)
                            json.Append(", ");
                        var requirement = resources[r];
                        json.Append("{\"item\": \"").Append(Esc(ItemName(requirement != null ? requirement.m_resItem : null))).Append("\"");
                        json.Append(", \"amount\": ").Append(requirement != null ? requirement.m_amount : 0);
                        json.Append(", \"perLevel\": ").Append(requirement != null ? requirement.m_amountPerLevel : 0);
                        json.Append("}");
                    }
                }
                json.Append("]}");
            }
            if (!first)
                json.Append("\n");
        }

        private static void WriteSmelters(StringBuilder json, List<GameObject> prefabs)
        {
            WriteWhere(json, prefabs, go =>
            {
                var smelter = go.GetComponent<Smelter>();
                if (smelter == null)
                    return false;
                json.Append("    {\"prefab\": \"").Append(Esc(go.name)).Append("\"");
                json.Append(", \"fuel\": \"").Append(Esc(ItemName(smelter.m_fuelItem))).Append("\"");
                json.Append(", \"maxOre\": ").Append(smelter.m_maxOre);
                json.Append(", \"maxFuel\": ").Append(smelter.m_maxFuel);
                json.Append(", \"fuelPerProduct\": ").Append(smelter.m_fuelPerProduct);
                json.Append(", \"secPerProduct\": ").Append(smelter.m_secPerProduct.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
                json.Append(", \"conversions\": [");
                WriteConversions(json, smelter.m_conversion, pair => pair.m_from, pair => pair.m_to);
                json.Append("]}");
                return true;
            });
        }

        private static void WriteCooking(StringBuilder json, List<GameObject> prefabs)
        {
            WriteWhere(json, prefabs, go =>
            {
                var station = go.GetComponent<CookingStation>();
                if (station == null)
                    return false;
                json.Append("    {\"prefab\": \"").Append(Esc(go.name)).Append("\"");
                json.Append(", \"requireFire\": ").Append(station.m_requireFire ? "true" : "false");
                json.Append(", \"canOvercook\": ").Append(station.m_canOvercookItems ? "true" : "false");
                json.Append(", \"conversions\": [");
                var list = station.m_conversion;
                if (list != null)
                {
                    for (var i = 0; i < list.Count; i++)
                    {
                        if (i > 0)
                            json.Append(", ");
                        var pair = list[i];
                        json.Append("{\"from\": \"").Append(Esc(ItemName(pair != null ? pair.m_from : null))).Append("\"");
                        json.Append(", \"to\": \"").Append(Esc(ItemName(pair != null ? pair.m_to : null))).Append("\"");
                        json.Append(", \"cookTime\": ").Append(pair != null ? pair.m_cookTime.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) : "0");
                        json.Append("}");
                    }
                }
                json.Append("]}");
                return true;
            });
        }

        private static void WriteFermenters(StringBuilder json, List<GameObject> prefabs)
        {
            WriteWhere(json, prefabs, go =>
            {
                var fermenter = go.GetComponent<Fermenter>();
                if (fermenter == null)
                    return false;
                json.Append("    {\"prefab\": \"").Append(Esc(go.name)).Append("\"");
                json.Append(", \"duration\": ").Append(fermenter.m_fermentationDuration.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
                json.Append(", \"conversions\": [");
                var list = fermenter.m_conversion;
                if (list != null)
                {
                    for (var i = 0; i < list.Count; i++)
                    {
                        if (i > 0)
                            json.Append(", ");
                        var pair = list[i];
                        json.Append("{\"from\": \"").Append(Esc(ItemName(pair != null ? pair.m_from : null))).Append("\"");
                        json.Append(", \"to\": \"").Append(Esc(ItemName(pair != null ? pair.m_to : null))).Append("\"");
                        json.Append(", \"produced\": ").Append(pair != null ? pair.m_producedItems : 0);
                        json.Append("}");
                    }
                }
                json.Append("]}");
                return true;
            });
        }

        private static void WritePlants(StringBuilder json, List<GameObject> prefabs)
        {
            WriteWhere(json, prefabs, go =>
            {
                var plant = go.GetComponent<Plant>();
                if (plant == null)
                    return false;
                json.Append("    {\"prefab\": \"").Append(Esc(go.name)).Append("\"");
                json.Append(", \"growTime\": ").Append(plant.m_growTime.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
                json.Append(", \"growTimeMax\": ").Append(plant.m_growTimeMax.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
                json.Append(", \"biome\": \"").Append(Esc(plant.m_biome.ToString())).Append("\"");
                json.Append(", \"cultivated\": ").Append(plant.m_needCultivatedGround ? "true" : "false");
                json.Append(", \"grown\": [");
                var grown = plant.m_grownPrefabs;
                if (grown != null)
                {
                    for (var i = 0; i < grown.Length; i++)
                    {
                        if (i > 0)
                            json.Append(", ");
                        json.Append("\"").Append(Esc(grown[i] != null ? grown[i].name : "")).Append("\"");
                    }
                }
                json.Append("]}");
                return true;
            });
        }

        private static void WriteBeehives(StringBuilder json, List<GameObject> prefabs)
        {
            WriteWhere(json, prefabs, go =>
            {
                var hive = go.GetComponent<Beehive>();
                if (hive == null)
                    return false;
                json.Append("    {\"prefab\": \"").Append(Esc(go.name)).Append("\"");
                json.Append(", \"secPerUnit\": ").Append(hive.m_secPerUnit.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
                json.Append(", \"maxHoney\": ").Append(hive.m_maxHoney);
                json.Append(", \"honey\": \"").Append(Esc(ItemName(hive.m_honeyItem))).Append("\"");
                json.Append(", \"biome\": \"").Append(Esc(hive.m_biome.ToString())).Append("\"");
                json.Append("}");
                return true;
            });
        }

        private static void WriteSap(StringBuilder json, List<GameObject> prefabs)
        {
            WriteWhere(json, prefabs, go =>
            {
                var sap = go.GetComponent<SapCollector>();
                if (sap == null)
                    return false;
                json.Append("    {\"prefab\": \"").Append(Esc(go.name)).Append("\"");
                json.Append(", \"secPerUnit\": ").Append(sap.m_secPerUnit.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
                json.Append(", \"maxLevel\": ").Append(sap.m_maxLevel);
                json.Append(", \"item\": \"").Append(Esc(ItemName(sap.m_spawnItem))).Append("\"");
                json.Append("}");
                return true;
            });
        }

        private static void WritePickables(StringBuilder json, List<GameObject> prefabs)
        {
            WriteWhere(json, prefabs, go =>
            {
                var pick = go.GetComponent<Pickable>();
                if (pick == null)
                    return false;
                json.Append("    {\"prefab\": \"").Append(Esc(go.name)).Append("\"");
                json.Append(", \"item\": \"").Append(Esc(pick.m_itemPrefab != null ? pick.m_itemPrefab.name : "")).Append("\"");
                json.Append(", \"amount\": ").Append(pick.m_amount);
                json.Append(", \"respawnMinutes\": ").Append(pick.m_respawnTimeMinutes.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
                json.Append("}");
                return true;
            });
        }

        private static void WritePieces(StringBuilder json, List<GameObject> prefabs)
        {
            WriteWhere(json, prefabs, go =>
            {
                var piece = go.GetComponent<Piece>();
                if (piece == null || piece.m_resources == null || piece.m_resources.Length == 0)
                    return false;
                json.Append("    {\"prefab\": \"").Append(Esc(go.name)).Append("\"");
                json.Append(", \"name\": \"").Append(Esc(piece.m_name)).Append("\"");
                json.Append(", \"station\": \"").Append(Esc(piece.m_craftingStation != null ? piece.m_craftingStation.m_name : "")).Append("\"");
                json.Append(", \"resources\": [");
                for (var i = 0; i < piece.m_resources.Length; i++)
                {
                    if (i > 0)
                        json.Append(", ");
                    var requirement = piece.m_resources[i];
                    json.Append("{\"item\": \"").Append(Esc(ItemName(requirement != null ? requirement.m_resItem : null))).Append("\"");
                    json.Append(", \"amount\": ").Append(requirement != null ? requirement.m_amount : 0);
                    json.Append(", \"recover\": ").Append(requirement != null && requirement.m_recover ? "true" : "false");
                    json.Append("}");
                }
                json.Append("]}");
                return true;
            });
        }

        private static void WriteCreatureDrops(StringBuilder json, List<GameObject> prefabs)
        {
            WriteWhere(json, prefabs, go =>
            {
                var drop = go.GetComponent<CharacterDrop>();
                if (drop == null || drop.m_drops == null)
                    return false;
                json.Append("    {\"prefab\": \"").Append(Esc(go.name)).Append("\", \"items\": [");
                var first = true;
                foreach (var d in drop.m_drops)
                {
                    if (d == null || d.m_prefab == null)
                        continue;
                    if (!first)
                        json.Append(", ");
                    first = false;
                    json.Append("\"").Append(Esc(ItemName(d.m_prefab.GetComponent<ItemDrop>()))).Append("\"");
                }
                json.Append("]}");
                return true;
            });
        }

        private static void WriteTraders(StringBuilder json, List<GameObject> prefabs)
        {
            WriteWhere(json, prefabs, go =>
            {
                var trader = go.GetComponent<Trader>();
                if (trader == null || trader.m_items == null)
                    return false;
                json.Append("    {\"prefab\": \"").Append(Esc(go.name)).Append("\", \"items\": [");
                for (var i = 0; i < trader.m_items.Count; i++)
                {
                    if (i > 0)
                        json.Append(", ");
                    json.Append("\"").Append(Esc(ItemName(trader.m_items[i].m_prefab))).Append("\"");
                }
                json.Append("]}");
                return true;
            });
        }

        private static void WriteConversions<T>(StringBuilder json, List<T> list, Func<T, ItemDrop> from, Func<T, ItemDrop> to)
        {
            if (list == null)
                return;
            for (var i = 0; i < list.Count; i++)
            {
                if (i > 0)
                    json.Append(", ");
                json.Append("{\"from\": \"").Append(Esc(ItemName(from(list[i])))).Append("\"");
                json.Append(", \"to\": \"").Append(Esc(ItemName(to(list[i])))).Append("\"}");
            }
        }

        private static void WriteWhere(StringBuilder json, List<GameObject> prefabs, Func<GameObject, bool> write)
        {
            var first = true;
            if (prefabs == null)
                return;
            for (var i = 0; i < prefabs.Count; i++)
            {
                var go = prefabs[i];
                if (go == null)
                    continue;
                var mark = json.Length;
                var started = first;
                if (!started)
                    json.Append(",\n");
                else
                    first = false;
                if (!write(go))
                {
                    json.Length = mark;
                    first = started;
                }
            }
            if (!first)
                json.Append("\n");
        }

        private static void Comma(StringBuilder json, ref bool first)
        {
            if (!first)
                json.Append(",\n");
            first = false;
        }

        private static string ItemName(ItemDrop drop)
        {
            if (drop == null)
                return "";
            if (drop.m_itemData != null && drop.m_itemData.m_shared != null && !string.IsNullOrEmpty(drop.m_itemData.m_shared.m_name))
                return drop.m_itemData.m_shared.m_name;
            return drop.gameObject.name;
        }

        private static string Esc(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        }
    }
}
