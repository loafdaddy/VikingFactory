using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using VikingFactory.Core;
using VikingFactory.Core.Diagnostics;
using VikingFactory.Core.Production;

namespace VikingFactory.Diagnostics
{
    /// <summary>
    /// Startup probe. It does not invent recipe data when the game is absent.
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length >= 1 && args[0] == "coverage")
                return Coverage(args.Length > 1 ? args[1] : "", args.Length > 2 ? args[2] : Path.Combine("docs", "AutomationCoverage.csv"));

            Console.WriteLine("VikingFactory diagnostic");
            var version = typeof(BalanceDefaults).Assembly.GetName().Version?.ToString(3) ?? "unknown";
            Console.WriteLine("Core assembly: VikingFactory.Core " + version);
            var install = ReadProp("VALHEIM_INSTALL");
            if (string.IsNullOrWhiteSpace(install))
            {
                Console.WriteLine("VALHEIM_INSTALL is empty. No live item, recipe, or station catalogue was exported.");
                Console.WriteLine("Next: install Valheim, copy Environment.props.example to Environment.props, and set VALHEIM_INSTALL.");
                return 2;
            }

            var managed = Path.Combine(install, "valheim_Data", "Managed", "assembly_valheim.dll");
            if (!File.Exists(managed))
                managed = Path.Combine(install, "Valheim_Data", "Managed", "assembly_valheim.dll");

            if (!File.Exists(managed))
            {
                Console.WriteLine("Game assembly not found under " + install);
                return 2;
            }

            Console.WriteLine("Found " + managed);
            Console.WriteLine("Live catalogue export runs inside the game, after vanilla prefabs load, to BepInEx/config/VikingFactory/catalogue.json.");
            return 0;
        }

        /// <summary>
        /// Classifies the catalogue the plugin exported from the running game. The in-game "vf coverage"
        /// command builds the same report from live objects and is authoritative.
        /// </summary>
        private static int Coverage(string cataloguePath, string outPath)
        {
            if (string.IsNullOrEmpty(cataloguePath))
            {
                var install = ReadProp("VALHEIM_INSTALL");
                cataloguePath = Path.Combine(install, "BepInEx", "config", "VikingFactory", "catalogue.json");
            }

            if (!File.Exists(cataloguePath))
            {
                Console.WriteLine("No catalogue at " + cataloguePath + ". Launch the game with the plugin once.");
                return 2;
            }

            using var doc = JsonDocument.Parse(File.ReadAllText(cataloguePath));
            var root = doc.RootElement;
            var input = new CoverageInput
            {
                ModVersion = typeof(BalanceDefaults).Assembly.GetName().Version?.ToString(3) ?? "",
                GameBuild = "catalogue " + File.GetLastWriteTimeUtc(cataloguePath).ToString("yyyy-MM-dd")
            };
            var sharedByPrefab = new Dictionary<string, string>();
            foreach (var item in Array(root, "items"))
            {
                var prefab = Str(item, "prefab");
                var shared = Str(item, "name");
                sharedByPrefab[prefab] = shared;
                input.Items.Add(new CoverageItem { Prefab = prefab, SharedName = shared });
            }

            string Shared(string prefab) => sharedByPrefab.TryGetValue(prefab, out var s) ? s : "";

            foreach (var recipe in Array(root, "recipes"))
            {
                var output = Str(recipe, "output");
                if (output.Length == 0)
                    continue;
                var inputs = Array(recipe, "resources").Where(r => r.GetProperty("amount").GetInt32() > 0).Select(r => Str(r, "item")).ToList();
                var enabled = Bool(recipe, "enabled");
                var onlyOne = Bool(recipe, "onlyOne");
                var upgradeOnly = Bool(recipe, "upgradeOnly");
                var ok = enabled && !onlyOne && !upgradeOnly && inputs.Count > 0 && inputs.Count <= 12;
                input.Producers.Add(new CoverageProducer
                {
                    Kind = "recipe",
                    Station = Str(recipe, "station"),
                    Output = output,
                    Inputs = inputs,
                    Automated = ok,
                    Note = ok ? "recipe mill" : !enabled ? "disabled recipe" : onlyOne ? "any-one-ingredient recipe" : upgradeOnly ? "upgrade only" : "no ingredients"
                });
            }

            foreach (var smelter in Array(root, "smelters"))
            {
                var name = Str(smelter, "prefab");
                var reviewed = ReviewedStations.Smelters.Contains(name);
                var fuel = Str(smelter, "fuel");
                foreach (var c in Array(smelter, "conversions"))
                {
                    var inputs = new List<string> { Str(c, "from") };
                    if (fuel.Length > 0)
                        inputs.Add(fuel);
                    input.Producers.Add(new CoverageProducer { Kind = "station", Station = name, Output = Str(c, "to"), Inputs = inputs, Automated = reviewed, Note = reviewed ? "feeder adapter" : "unreviewed station" });
                }
            }

            foreach (var station in Array(root, "cooking"))
            {
                var name = Str(station, "prefab");
                var reviewed = ReviewedStations.Cooking.Contains(name);
                foreach (var c in Array(station, "conversions"))
                    input.Producers.Add(new CoverageProducer { Kind = "cooking", Station = name, Output = Str(c, "to"), Inputs = new List<string> { Str(c, "from") }, Automated = reviewed, Note = reviewed ? "cooking tender" : "unreviewed station" });
            }

            foreach (var fermenter in Array(root, "fermenters"))
            {
                foreach (var c in Array(fermenter, "conversions"))
                    input.Producers.Add(new CoverageProducer { Kind = "fermenter", Station = Str(fermenter, "prefab"), Output = Str(c, "to"), Inputs = new List<string> { Str(c, "from") }, Automated = true, Note = "feeder adapter" });
            }

            foreach (var hive in Array(root, "beehives"))
                input.NativeRenewable.Add(Str(hive, "honey"));
            foreach (var sap in Array(root, "sapCollectors"))
                input.NativeRenewable.Add(Str(sap, "item"));
            var pickableItem = new Dictionary<string, string>();
            foreach (var pickable in Array(root, "pickables"))
            {
                pickableItem[Str(pickable, "prefab")] = Str(pickable, "item");
                if (pickable.GetProperty("respawnMinutes").GetDouble() > 0)
                    input.NativeRenewable.Add(Shared(Str(pickable, "item")));
            }

            foreach (var plant in Array(root, "plants"))
            {
                foreach (var grown in plant.GetProperty("grown").EnumerateArray())
                {
                    if (pickableItem.TryGetValue(grown.GetString() ?? "", out var item))
                        input.NativeRenewable.Add(Shared(item));
                }
            }

            foreach (var prefab in new[] { "Wood", "RoundLog", "FineWood", "Resin", "Stone" })
                input.ModRenewable.Add(Shared(prefab));
            foreach (var spec in DeepExtraction.Allowlist)
                input.ModRenewable.Add(Shared(spec.Output));
            foreach (var spec in Forage.Allowlist)
                input.ModRenewable.Add(Shared(spec.Item));
            foreach (var creature in Array(root, "creatureDrops"))
            {
                foreach (var item in creature.GetProperty("items").EnumerateArray())
                    input.CreatureDrops.Add(item.GetString() ?? "");
            }

            foreach (var trader in Array(root, "traders"))
            {
                foreach (var item in trader.GetProperty("items").EnumerateArray())
                    input.TraderItems.Add(item.GetString() ?? "");
            }

            input.ModRenewable.Remove("");
            input.NativeRenewable.Remove("");
            var rows = CoverageClassifier.Classify(input);
            var csv = CoverageClassifier.ToCsv(rows, input);
            File.WriteAllText(outPath, csv);
            Console.WriteLine("Wrote " + outPath);
            foreach (var line in csv.Split('\n').Where(l => l.StartsWith("# ")))
                Console.WriteLine(line);
            return 0;
        }

        private static IEnumerable<JsonElement> Array(JsonElement element, string name)
        {
            return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array ? value.EnumerateArray() : Enumerable.Empty<JsonElement>();
        }

        private static string Str(JsonElement element, string name)
        {
            return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";
        }

        private static bool Bool(JsonElement element, string name)
        {
            return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True;
        }

        private static string ReadProp(string name)
        {
            var fromEnv = Environment.GetEnvironmentVariable(name);
            if (!string.IsNullOrWhiteSpace(fromEnv))
                return fromEnv;

            var path = Path.Combine(Directory.GetCurrentDirectory(), "Environment.props");
            if (!File.Exists(path))
                return "";

            var text = File.ReadAllText(path);
            var token = "<" + name + ">";
            var start = text.IndexOf(token, StringComparison.Ordinal);
            if (start < 0)
                return "";
            start += token.Length;
            var end = text.IndexOf("</" + name + ">", start, StringComparison.Ordinal);
            if (end < 0)
                return "";
            return text.Substring(start, end - start).Trim();
        }
    }
}
