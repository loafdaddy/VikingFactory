using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using Jotunn.Managers;
using UnityEngine;
using VikingFactory.Core.Diagnostics;
using VikingFactory.Core.Production;
using VikingFactory.Machines;

namespace VikingFactory
{
    /// <summary>
    /// Builds the coverage report from the running game: every inventory item, how it is made, and whether a
    /// VikingFactory machine can drive that producer. Written as AutomationCoverage.csv beside the catalogue.
    /// </summary>
    public static class CoverageExport
    {
        public static string Write(out string summary)
        {
            var input = Build();
            var rows = CoverageClassifier.Classify(input);
            var csv = CoverageClassifier.ToCsv(rows, input);
            var directory = Path.Combine(Paths.ConfigPath, "VikingFactory");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "AutomationCoverage.csv");
            File.WriteAllText(path, csv, new UTF8Encoding(false));
            summary = string.Join(" ", csv.Split('\n').Where(l => l.StartsWith("# ", StringComparison.Ordinal)).Skip(1).Select(l => l.Substring(2)).ToArray());
            return path;
        }

        private static IEnumerable<T> All<T>() where T : Component
        {
            var map = PrefabManager.Cache.GetPrefabs(typeof(T));
            if (map == null)
                yield break;
            foreach (var pair in map)
            {
                var component = pair.Value as T;
                if (component != null)
                    yield return component;
            }
        }

        private static string Shared(ItemDrop drop)
        {
            return drop != null && drop.m_itemData != null && drop.m_itemData.m_shared != null ? drop.m_itemData.m_shared.m_name : "";
        }

        private static string Shared(string prefab)
        {
            var go = ItemBridge.Prefab(prefab);
            return go != null ? Shared(go.GetComponent<ItemDrop>()) : "";
        }

        private static CoverageInput Build()
        {
            var input = new CoverageInput { ModVersion = VikingFactoryPlugin.PluginVersion, GameBuild = Version.GetVersionString() };
            var db = ObjectDB.instance;
            if (db != null)
            {
                foreach (var go in db.m_items)
                {
                    var drop = go != null ? go.GetComponent<ItemDrop>() : null;
                    if (drop != null)
                        input.Items.Add(new CoverageItem { Prefab = go.name, SharedName = Shared(drop), MaxStack = drop.m_itemData.m_shared.m_maxStackSize });
                }

                foreach (var recipe in db.m_recipes)
                {
                    var spec = LiveRecipes.ToSpec(recipe);
                    if (spec == null)
                        continue;
                    var reason = spec.UnsupportedReason(12);
                    input.Producers.Add(new CoverageProducer
                    {
                        Kind = "recipe",
                        Station = spec.Station,
                        Output = Shared(recipe.m_item),
                        Inputs = recipe.m_resources.Where(r => r != null && r.m_resItem != null && r.m_amount > 0).Select(r => Shared(r.m_resItem)).ToList(),
                        Automated = reason.Length == 0,
                        Note = reason.Length == 0 ? "recipe mill" : reason
                    });
                }
            }

            foreach (var smelter in All<Smelter>())
            {
                var reviewed = SmelterEndpoint.Reviewed.Contains(smelter.gameObject.name);
                foreach (var conversion in smelter.m_conversion)
                {
                    if (conversion.m_from == null || conversion.m_to == null)
                        continue;
                    var inputs = new List<string> { Shared(conversion.m_from) };
                    if (smelter.m_fuelItem != null && smelter.m_maxFuel > 0)
                        inputs.Add(Shared(smelter.m_fuelItem));
                    input.Producers.Add(new CoverageProducer { Kind = "station", Station = smelter.gameObject.name, Output = Shared(conversion.m_to), Inputs = inputs, Automated = reviewed, Note = reviewed ? "feeder adapter" : "unreviewed station" });
                }
            }

            foreach (var station in All<CookingStation>())
            {
                var reviewed = CookingEndpoint.Reviewed.Contains(station.gameObject.name);
                foreach (var conversion in station.m_conversion)
                {
                    if (conversion.m_from == null || conversion.m_to == null)
                        continue;
                    input.Producers.Add(new CoverageProducer { Kind = "cooking", Station = station.gameObject.name, Output = Shared(conversion.m_to), Inputs = new List<string> { Shared(conversion.m_from) }, Automated = reviewed, Note = reviewed ? "cooking tender" : "unreviewed station" });
                }
            }

            foreach (var fermenter in All<Fermenter>())
            {
                foreach (var conversion in fermenter.m_conversion)
                {
                    if (conversion.m_from != null && conversion.m_to != null)
                        input.Producers.Add(new CoverageProducer { Kind = "fermenter", Station = fermenter.gameObject.name, Output = Shared(conversion.m_to), Inputs = new List<string> { Shared(conversion.m_from) }, Automated = true, Note = "feeder adapter" });
                }
            }

            foreach (var hive in All<Beehive>())
                input.NativeRenewable.Add(Shared(hive.m_honeyItem));
            foreach (var sap in All<SapCollector>())
                input.NativeRenewable.Add(Shared(sap.m_spawnItem));
            foreach (var pickable in All<Pickable>())
            {
                if (pickable.m_itemPrefab != null && pickable.m_respawnTimeMinutes > 0)
                    input.NativeRenewable.Add(Shared(pickable.m_itemPrefab.GetComponent<ItemDrop>()));
            }

            foreach (var plant in All<Plant>())
            {
                if (plant.m_grownPrefabs == null)
                    continue;
                foreach (var grown in plant.m_grownPrefabs)
                {
                    var pickable = grown != null ? grown.GetComponent<Pickable>() : null;
                    if (pickable != null && pickable.m_itemPrefab != null)
                        input.NativeRenewable.Add(Shared(pickable.m_itemPrefab.GetComponent<ItemDrop>()));
                }
            }

            var flags = WorkshopConfig.Flags;
            if (flags.Coppice)
            {
                foreach (var prefab in new[] { "Wood", "RoundLog", "FineWood", "Resin" })
                    input.ModRenewable.Add(Shared(prefab));
            }

            if (flags.BedrockQuarry)
                input.ModRenewable.Add(Shared("Stone"));
            if (flags.DeepExtraction)
            {
                foreach (var spec in DeepExtraction.Allowlist)
                    input.ModRenewable.Add(Shared(spec.Output));
            }

            if (flags.Forage)
            {
                foreach (var spec in Forage.Allowlist)
                    input.ModRenewable.Add(Shared(spec.Item));
            }

            foreach (var drop in All<CharacterDrop>())
            {
                foreach (var d in drop.m_drops)
                {
                    if (d != null && d.m_prefab != null)
                        input.CreatureDrops.Add(Shared(d.m_prefab.GetComponent<ItemDrop>()));
                }
            }

            foreach (var trader in All<Trader>())
            {
                foreach (var item in trader.m_items)
                    input.TraderItems.Add(Shared(item.m_prefab));
            }

            input.ModRenewable.Remove("");
            input.NativeRenewable.Remove("");
            return input;
        }
    }
}
