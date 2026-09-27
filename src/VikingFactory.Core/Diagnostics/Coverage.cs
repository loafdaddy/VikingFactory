using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace VikingFactory.Core.Diagnostics
{
    public enum CoverageCategory
    {
        RenewableChain,
        NativeRenewableCollection,
        FiniteSupplyAutomatedProcessing,
        CombatOrHusbandrySupplyAutomatedProcessing,
        TraderSupplyAutomatedProcessing,
        ManualOrUniqueByDesign,
        UnsupportedPendingAdapter
    }

    public sealed class CoverageItem
    {
        public string Prefab = "";
        public string SharedName = "";
        public int MaxStack;
    }

    /// <summary>One way an item can be made: a recipe, a station conversion, or a collection.</summary>
    public sealed class CoverageProducer
    {
        public string Kind = "";
        public string Station = "";
        public string Output = "";
        public List<string> Inputs = new List<string>();
        /// <summary>True when a VikingFactory machine can drive this producer.</summary>
        public bool Automated;
        public string Note = "";
    }

    public sealed class CoverageInput
    {
        public List<CoverageItem> Items = new List<CoverageItem>();
        public List<CoverageProducer> Producers = new List<CoverageProducer>();
        /// <summary>Shared names with a mod-added renewable source (coppice, quarry, deep extraction, forage).</summary>
        public HashSet<string> ModRenewable = new HashSet<string>();
        /// <summary>Shared names from native renewables: respawning pickables, crops, hives, sap.</summary>
        public HashSet<string> NativeRenewable = new HashSet<string>();
        public HashSet<string> CreatureDrops = new HashSet<string>();
        public HashSet<string> TraderItems = new HashSet<string>();
        public string GameBuild = "";
        public string ModVersion = "";
    }

    public sealed class CoverageRow
    {
        public string Prefab = "";
        public string SharedName = "";
        public CoverageCategory Category;
        public string Reason = "";
    }

    /// <summary>
    /// Classifies every inventory item into one of the master prompt's seven categories. The denominator
    /// is items with a "$item_" shared name; attacks, VFX, and debug prefabs are excluded. The report never
    /// claims more than the data supports: an item with no known source is not called automated.
    /// </summary>
    public static class CoverageClassifier
    {
        private static readonly string[] UniqueMarkers = { "trophy", "key", "wishbone", "dragontear", "yagluththing", "queen_drop", "fader_drop", "chest_hildir", "mechanicalspring", "dragonegg", "vegvisir", "megingjord", "demister", "beltstrength" };

        public static bool InDenominator(CoverageItem item)
        {
            return item.SharedName.StartsWith("$item_", StringComparison.Ordinal);
        }

        public static List<CoverageRow> Classify(CoverageInput input)
        {
            var bySource = new Dictionary<string, List<CoverageProducer>>();
            foreach (var producer in input.Producers)
            {
                if (!bySource.TryGetValue(producer.Output, out var list))
                {
                    list = new List<CoverageProducer>();
                    bySource[producer.Output] = list;
                }

                list.Add(producer);
            }

            var renewable = new HashSet<string>(input.ModRenewable);
            var nativeRenewable = new HashSet<string>(input.NativeRenewable);
            // Fixpoint: an automated producer whose inputs are all renewable yields a renewable item.
            var changed = true;
            while (changed)
            {
                changed = false;
                foreach (var producer in input.Producers)
                {
                    if (!producer.Automated || producer.Inputs.Count == 0)
                        continue;
                    if (producer.Inputs.All(renewable.Contains) && renewable.Add(producer.Output))
                        changed = true;
                    else if (producer.Inputs.All(i => renewable.Contains(i) || nativeRenewable.Contains(i))
                        && !renewable.Contains(producer.Output) && nativeRenewable.Add(producer.Output))
                        changed = true;
                }
            }

            var rows = new List<CoverageRow>();
            foreach (var item in input.Items.Where(InDenominator).OrderBy(i => i.Prefab, StringComparer.Ordinal))
            {
                var row = new CoverageRow { Prefab = item.Prefab, SharedName = item.SharedName };
                bySource.TryGetValue(item.SharedName, out var producers);
                producers = producers ?? new List<CoverageProducer>();
                var automated = producers.Where(p => p.Automated).ToList();
                var lower = item.Prefab.ToLowerInvariant() + " " + item.SharedName.ToLowerInvariant();

                if (renewable.Contains(item.SharedName))
                {
                    row.Category = CoverageCategory.RenewableChain;
                    row.Reason = input.ModRenewable.Contains(item.SharedName) ? "Mod renewable source" : "Automated chain from renewable inputs: " + Describe(automated);
                }
                else if (nativeRenewable.Contains(item.SharedName))
                {
                    row.Category = CoverageCategory.NativeRenewableCollection;
                    row.Reason = input.NativeRenewable.Contains(item.SharedName) ? "Native respawn, crop, hive, or sap source" : "Automated chain from native renewable inputs: " + Describe(automated);
                }
                else if (UniqueMarkers.Any(lower.Contains))
                {
                    row.Category = CoverageCategory.ManualOrUniqueByDesign;
                    row.Reason = "Trophy, key, boss drop, or unique progression item";
                }
                else if (automated.Count > 0)
                {
                    var inputs = automated.SelectMany(p => p.Inputs).Distinct().ToList();
                    if (inputs.Any(input.TraderItems.Contains) || input.TraderItems.Contains(item.SharedName))
                    {
                        row.Category = CoverageCategory.TraderSupplyAutomatedProcessing;
                        row.Reason = "Processing automated; a trader supplies an input";
                    }
                    else if (inputs.Any(input.CreatureDrops.Contains))
                    {
                        row.Category = CoverageCategory.CombatOrHusbandrySupplyAutomatedProcessing;
                        row.Reason = "Processing automated; creature drops supply an input";
                    }
                    else
                    {
                        row.Category = CoverageCategory.FiniteSupplyAutomatedProcessing;
                        row.Reason = "Processing automated: " + Describe(automated);
                    }
                }
                else if (input.TraderItems.Contains(item.SharedName))
                {
                    row.Category = CoverageCategory.TraderSupplyAutomatedProcessing;
                    row.Reason = "Bought from a trader; storage and downstream recipes only";
                }
                else if (input.CreatureDrops.Contains(item.SharedName))
                {
                    row.Category = CoverageCategory.CombatOrHusbandrySupplyAutomatedProcessing;
                    row.Reason = "Creature drop; collection and downstream processing only";
                }
                else if (producers.Count > 0)
                {
                    row.Category = CoverageCategory.UnsupportedPendingAdapter;
                    row.Reason = "Made only by an unreviewed producer: " + Describe(producers);
                }
                else
                {
                    row.Category = CoverageCategory.FiniteSupplyAutomatedProcessing;
                    row.Reason = "Found or mined in the world; no producer recorded. Belts, feeders, and docks can move it.";
                }

                rows.Add(row);
            }

            return rows;
        }

        public static string ToCsv(IEnumerable<CoverageRow> rows, CoverageInput input)
        {
            var list = rows.ToList();
            var builder = new StringBuilder();
            builder.Append("# VikingFactory automation coverage. Mod ").Append(input.ModVersion).Append(". Game ").Append(input.GameBuild).Append('\n');
            builder.Append("# Denominator: ").Append(list.Count).Append(" items with a $item_ shared name. Attacks, VFX, and debug prefabs excluded.\n");
            foreach (CoverageCategory category in Enum.GetValues(typeof(CoverageCategory)))
            {
                var count = list.Count(r => r.Category == category);
                var percent = list.Count == 0 ? 0 : 100.0 * count / list.Count;
                builder.Append("# ").Append(category).Append(": ").Append(count).Append(" (").Append(percent.ToString("0.0", CultureInfo.InvariantCulture)).Append("%)\n");
            }

            builder.Append("prefab,shared_name,category,reason\n");
            foreach (var row in list)
                builder.Append(Csv(row.Prefab)).Append(',').Append(Csv(row.SharedName)).Append(',').Append(row.Category).Append(',').Append(Csv(row.Reason)).Append('\n');
            return builder.ToString();
        }

        private static string Describe(IEnumerable<CoverageProducer> producers)
        {
            return string.Join("; ", producers.Select(p => p.Kind + (p.Station.Length > 0 ? " at " + p.Station : "") + (p.Note.Length > 0 ? " (" + p.Note + ")" : "")).Distinct().Take(3).ToArray());
        }

        private static string Csv(string value)
        {
            var text = value ?? "";
            if (text.IndexOfAny(new[] { ',', '"', '\n' }) < 0)
                return text;
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }
    }
}
