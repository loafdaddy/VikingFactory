using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace VikingFactory.Core.Production
{
    /// <summary>
    /// A fixed-interval producer with a hard buffer cap. Progress accrues only on active powered
    /// time and stops when the buffer is full, so a full machine does not bank invisible output.
    /// </summary>
    public sealed class TimedProducer
    {
        public TimedProducer(double secondsPerItem, int capacity, double progress = 0, int buffered = 0)
        {
            if (secondsPerItem <= 0)
                throw new ArgumentOutOfRangeException(nameof(secondsPerItem));
            SecondsPerItem = secondsPerItem;
            Capacity = Math.Max(1, capacity);
            Progress = Math.Max(0, Math.Min(secondsPerItem, progress));
            Buffered = Math.Max(0, buffered);
        }

        public double SecondsPerItem { get; }
        public int Capacity { get; }
        public double Progress { get; private set; }
        public int Buffered { get; private set; }
        public int Produced { get; private set; }
        public bool Full => Buffered >= Capacity;

        /// <summary>Returns items made this step. Never more than one per interval, never past the cap.</summary>
        public int Tick(double dtSeconds, bool running)
        {
            if (!running || dtSeconds <= 0 || Full)
                return 0;
            Progress += dtSeconds;
            var made = 0;
            while (Progress >= SecondsPerItem && !Full)
            {
                Progress -= SecondsPerItem;
                Buffered++;
                Produced++;
                made++;
            }

            if (Full)
                Progress = 0;
            return made;
        }

        public bool TakeOne()
        {
            if (Buffered <= 0)
                return false;
            Buffered--;
            return true;
        }
    }

    public static class SiteSpacing
    {
        public struct Site
        {
            public Site(string id, float x, float z)
            {
                Id = id;
                X = x;
                Z = z;
            }

            public string Id;
            public float X;
            public float Z;
        }

        /// <summary>
        /// Deterministic spacing. When two sites are too close, the one with the lower stable id
        /// keeps working and the other is blocked. Both peers reach the same answer.
        /// </summary>
        public static bool IsClear(Site self, IEnumerable<Site> others, float minDistance, out string blockedBy)
        {
            blockedBy = "";
            var min2 = minDistance * minDistance;
            foreach (var other in others)
            {
                if (other.Id == self.Id)
                    continue;
                var dx = other.X - self.X;
                var dz = other.Z - self.Z;
                if (dx * dx + dz * dz >= min2)
                    continue;
                if (string.CompareOrdinal(other.Id, self.Id) < 0)
                {
                    blockedBy = other.Id;
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Boss milestones resolved against keys read from the running game. An unresolved milestone
    /// fails closed. Key names are not guessed here.
    /// </summary>
    public sealed class ProgressionGate
    {
        private readonly Dictionary<string, string> _milestoneKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public void Resolve(string milestone, string globalKey)
        {
            if (!string.IsNullOrEmpty(milestone) && !string.IsNullOrEmpty(globalKey))
                _milestoneKeys[milestone] = globalKey;
        }

        public bool IsResolved(string milestone)
        {
            return _milestoneKeys.ContainsKey(milestone);
        }

        public string KeyFor(string milestone)
        {
            return _milestoneKeys.TryGetValue(milestone, out var key) ? key : "";
        }

        public bool IsUnlocked(IEnumerable<string> milestones, Func<string, bool> keySet, out string missing)
        {
            missing = "";
            foreach (var milestone in milestones)
            {
                if (!_milestoneKeys.TryGetValue(milestone, out var key))
                {
                    missing = milestone + " (key not found in this game)";
                    return false;
                }

                if (!keySet(key))
                {
                    missing = milestone;
                    return false;
                }
            }

            return true;
        }
    }

    public sealed class ExtractionSpec
    {
        public string Output = "";
        public string[] Milestones = new string[0];
        public string Biome = "";
        public double SecondsPerItem;
        public string CommissionItem = "";
        public int CommissionCount = BalanceDefaults.ExtractorCommissionCount;
    }

    /// <summary>
    /// Explicit allowlist for late renewable older ores. Unknown outputs are denied.
    /// </summary>
    public static class DeepExtraction
    {
        public static readonly IReadOnlyList<ExtractionSpec> Allowlist = new List<ExtractionSpec>
        {
            new ExtractionSpec { Output = "CopperOre", Milestones = new[] { "Moder" }, Biome = "BlackForest", SecondsPerItem = 120, CommissionItem = "CopperOre" },
            new ExtractionSpec { Output = "TinOre", Milestones = new[] { "Moder" }, Biome = "BlackForest", SecondsPerItem = 120, CommissionItem = "TinOre" },
            new ExtractionSpec { Output = "IronScrap", Milestones = new[] { "Yagluth" }, Biome = "Swamp", SecondsPerItem = 180, CommissionItem = "IronScrap" },
            new ExtractionSpec { Output = "SilverOre", Milestones = new[] { "Fader" }, Biome = "Mountain", SecondsPerItem = 240, CommissionItem = "SilverOre" }
        };

        public static ExtractionSpec? Find(string output)
        {
            return Allowlist.FirstOrDefault(s => s.Output == output);
        }
    }

    public enum CommissionResult
    {
        Accepted,
        AlreadyCommissioned,
        WrongItem,
        NotEnough
    }

    /// <summary>
    /// A one-time founding investment tied to one machine and site. It is consumed, never refunded,
    /// and a rebuilt machine starts uncommissioned.
    /// </summary>
    public sealed class Commission
    {
        public Commission(string requiredItem, int requiredCount, string committedItem = "", int committed = 0)
        {
            RequiredItem = requiredItem;
            RequiredCount = requiredCount;
            CommittedItem = committedItem;
            Committed = committed;
        }

        public string RequiredItem { get; private set; }
        public int RequiredCount { get; }
        public string CommittedItem { get; private set; }
        public int Committed { get; private set; }
        public bool Complete => Committed >= RequiredCount;

        /// <summary>
        /// Offer items one stack at a time. Returns how many were consumed. The species or ore is fixed
        /// by the first accepted item when RequiredItem lists alternatives separated by '|'.
        /// </summary>
        public int Offer(string item, int count, out CommissionResult result)
        {
            if (Complete)
            {
                result = CommissionResult.AlreadyCommissioned;
                return 0;
            }

            var allowed = RequiredItem.Split('|');
            if (CommittedItem.Length > 0 ? item != CommittedItem : Array.IndexOf(allowed, item) < 0)
            {
                result = CommissionResult.WrongItem;
                return 0;
            }

            var take = Math.Min(count, RequiredCount - Committed);
            if (take <= 0)
            {
                result = CommissionResult.NotEnough;
                return 0;
            }

            CommittedItem = item;
            Committed += take;
            result = CommissionResult.Accepted;
            return take;
        }

        public string Save()
        {
            return CommittedItem + "|" + Committed.ToString(CultureInfo.InvariantCulture);
        }

        public static Commission Load(string requiredItem, int requiredCount, string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return new Commission(requiredItem, requiredCount);
            var cut = raw.LastIndexOf('|');
            if (cut < 0 || !int.TryParse(raw.Substring(cut + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var count))
                return new Commission(requiredItem, requiredCount);
            return new Commission(requiredItem, requiredCount, raw.Substring(0, cut), count);
        }
    }

    public sealed class CoppiceSpecies
    {
        public string Id = "";
        public string FoundingItem = "";
        public string SaplingPrefab = "";
        public double MinCycleSeconds;
        public List<KeyValuePair<string, int>> Harvest = new List<KeyValuePair<string, int>>();
        public bool CanTapResin;
        public string[] RequiredKnownItems = new string[0];
    }

    public enum CoppiceMode
    {
        Timber,
        Resin
    }

    /// <summary>
    /// Managed coppice. Founding stock becomes rootstock once. Growth runs on active simulation time,
    /// never faster with RPM, and a mature bed waits rather than stacking batches.
    /// </summary>
    public sealed class CoppiceBed
    {
        public static readonly IReadOnlyList<CoppiceSpecies> Species = new List<CoppiceSpecies>
        {
            new CoppiceSpecies
            {
                Id = "common", FoundingItem = "BeechSeeds", SaplingPrefab = "Beech_Sapling", MinCycleSeconds = 30 * 60,
                Harvest = new List<KeyValuePair<string, int>> { new KeyValuePair<string, int>("Wood", 20) }, CanTapResin = true
            },
            new CoppiceSpecies
            {
                Id = "pine", FoundingItem = "PineCone", SaplingPrefab = "PineTree_Sapling", MinCycleSeconds = 40 * 60,
                Harvest = new List<KeyValuePair<string, int>> { new KeyValuePair<string, int>("RoundLog", 10), new KeyValuePair<string, int>("Wood", 10) },
                RequiredKnownItems = new[] { "RoundLog" }
            },
            new CoppiceSpecies
            {
                Id = "fine", FoundingItem = "BirchSeeds|Acorn", SaplingPrefab = "Birch_Sapling|Oak_Sapling", MinCycleSeconds = 50 * 60,
                Harvest = new List<KeyValuePair<string, int>> { new KeyValuePair<string, int>("FineWood", 8), new KeyValuePair<string, int>("Wood", 12) },
                RequiredKnownItems = new[] { "FineWood" }
            }
        };

        public CoppiceBed(double nativeGrowSeconds = 0)
        {
            NativeGrowSeconds = Math.Max(0, nativeGrowSeconds);
        }

        public CoppiceSpecies? Current { get; private set; }
        public Commission? Founding { get; private set; }
        public double NativeGrowSeconds { get; set; }
        public double Growth { get; private set; }
        public CoppiceMode Mode { get; private set; } = CoppiceMode.Timber;
        public double ResinProgress { get; private set; }
        public int ResinBuffered { get; private set; }
        public bool Commissioned => Founding != null && Founding.Complete;
        public double CycleSeconds => Current == null ? double.PositiveInfinity : Math.Max(Current.MinCycleSeconds, NativeGrowSeconds);
        public bool Mature => Commissioned && Mode == CoppiceMode.Timber && Growth >= CycleSeconds;

        public static CoppiceSpecies? SpeciesForSeed(string item)
        {
            return Species.FirstOrDefault(s => Array.IndexOf(s.FoundingItem.Split('|'), item) >= 0);
        }

        /// <summary>Consumes up to the missing founding count. The first seed fixes the species.</summary>
        public int Plant(string item, int count, out string reason)
        {
            reason = "";
            if (Current == null)
            {
                var species = SpeciesForSeed(item);
                if (species == null)
                {
                    reason = "Not a coppice seed.";
                    return 0;
                }

                Current = species;
                Founding = new Commission(species.FoundingItem, BalanceDefaults.CoppiceFoundingCount);
            }

            var taken = Founding!.Offer(item, count, out var result);
            if (result == CommissionResult.WrongItem)
                reason = "This bed is fixed to " + Current.Id + ".";
            else if (result == CommissionResult.AlreadyCommissioned)
                reason = "Already planted.";
            return taken;
        }

        public void Tick(double dtSeconds)
        {
            if (!Commissioned || dtSeconds <= 0)
                return;
            if (Mode == CoppiceMode.Timber)
            {
                Growth = Math.Min(CycleSeconds, Growth + dtSeconds);
                return;
            }

            if (ResinBuffered >= BalanceDefaults.CoppiceResinCapacity)
                return;
            ResinProgress += dtSeconds;
            while (ResinProgress >= BalanceDefaults.CoppiceResinSeconds && ResinBuffered < BalanceDefaults.CoppiceResinCapacity)
            {
                ResinProgress -= BalanceDefaults.CoppiceResinSeconds;
                ResinBuffered++;
            }

            if (ResinBuffered >= BalanceDefaults.CoppiceResinCapacity)
                ResinProgress = 0;
        }

        /// <summary>A powered head takes the whole batch once. Growth restarts from zero.</summary>
        public List<KeyValuePair<string, int>> HarvestTimber()
        {
            if (!Mature || Current == null)
                return new List<KeyValuePair<string, int>>();
            Growth = 0;
            return new List<KeyValuePair<string, int>>(Current.Harvest);
        }

        public bool TakeResin()
        {
            if (ResinBuffered <= 0)
                return false;
            ResinBuffered--;
            return true;
        }

        /// <summary>Mode changes only at the start of a cycle, so timber and resin never both pay out.</summary>
        public bool SetMode(CoppiceMode mode, out string reason)
        {
            reason = "";
            if (Current == null || !Commissioned)
            {
                reason = "Plant the bed first.";
                return false;
            }

            if (mode == Mode)
                return true;
            if (mode == CoppiceMode.Resin && !Current.CanTapResin)
            {
                reason = "Only a common coppice can be tapped.";
                return false;
            }

            if (Growth > 0 || ResinProgress > 0 || ResinBuffered > 0)
            {
                reason = "Finish or harvest the current cycle first.";
                return false;
            }

            Mode = mode;
            return true;
        }

        public string Save()
        {
            return string.Join(";", new[]
            {
                Current?.Id ?? "",
                Founding?.Save() ?? "",
                Growth.ToString("R", CultureInfo.InvariantCulture),
                ((int)Mode).ToString(CultureInfo.InvariantCulture),
                ResinProgress.ToString("R", CultureInfo.InvariantCulture),
                ResinBuffered.ToString(CultureInfo.InvariantCulture)
            });
        }

        public static CoppiceBed Load(string raw, double nativeGrowSeconds)
        {
            var bed = new CoppiceBed(nativeGrowSeconds);
            if (string.IsNullOrEmpty(raw))
                return bed;
            var parts = raw.Split(';');
            if (parts.Length < 6)
                return bed;
            bed.Current = Species.FirstOrDefault(s => s.Id == parts[0]);
            if (bed.Current != null)
                bed.Founding = Commission.Load(bed.Current.FoundingItem, BalanceDefaults.CoppiceFoundingCount, parts[1]);
            double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var growth);
            int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var mode);
            double.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var resin);
            int.TryParse(parts[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out var resinBuffered);
            bed.Growth = Math.Max(0, growth);
            bed.Mode = mode == 1 ? CoppiceMode.Resin : CoppiceMode.Timber;
            bed.ResinProgress = Math.Max(0, resin);
            bed.ResinBuffered = Math.Max(0, resinBuffered);
            return bed;
        }
    }

    public sealed class ForageSpec
    {
        public string Item = "";
        public string PickablePrefab = "";
        public string[] Milestones = new string[0];
    }

    /// <summary>Strict forage allowlist. Anything else stays wild.</summary>
    public static class Forage
    {
        public static readonly IReadOnlyList<ForageSpec> Allowlist = new List<ForageSpec>
        {
            new ForageSpec { Item = "Raspberry", PickablePrefab = "RaspberryBush" },
            new ForageSpec { Item = "Blueberries", PickablePrefab = "BlueberryBush" },
            new ForageSpec { Item = "Mushroom", PickablePrefab = "Pickable_Mushroom" },
            new ForageSpec { Item = "Thistle", PickablePrefab = "Pickable_Thistle" },
            new ForageSpec { Item = "Cloudberry", PickablePrefab = "CloudberryBush", Milestones = new[] { "Moder" } }
        };

        public static ForageSpec? Find(string item)
        {
            return Allowlist.FirstOrDefault(s => s.Item == item);
        }

        /// <summary>Never faster than the native respawn, and never faster than ten minutes.</summary>
        public static double SecondsPerItem(double nativeRespawnMinutes)
        {
            return Math.Max(BalanceDefaults.ForageMinSeconds, nativeRespawnMinutes * 60);
        }
    }
}
