using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using VikingFactory.Core.Items;

namespace VikingFactory.Core.Crafting
{
    public sealed class Ingredient
    {
        public Ingredient(string prefab, int amount, int amountPerLevel = 0)
        {
            Prefab = prefab;
            Amount = amount;
            AmountPerLevel = amountPerLevel;
        }

        public string Prefab { get; }
        public int Amount { get; }
        public int AmountPerLevel { get; }
    }

    /// <summary>
    /// A recipe as read from the running game. The mill never invents one.
    /// </summary>
    public sealed class RecipeSpec
    {
        public string Id = "";
        public string OutputPrefab = "";
        public int OutputAmount = 1;
        public int MaxQuality = 1;
        public string Station = "";
        public int StationLevel = 1;
        public bool RequireOnlyOneIngredient;
        public bool UpgradeOnly;
        public bool Enabled = true;
        public List<Ingredient> Ingredients = new List<Ingredient>();

        public int DistinctIngredients => Ingredients.Count(i => i.Amount > 0);

        /// <summary>Why the mill will not run this recipe, or empty when it can.</summary>
        public string UnsupportedReason(int ingredientSlots)
        {
            if (!Enabled)
                return "Recipe is disabled in this game.";
            if (string.IsNullOrEmpty(OutputPrefab))
                return "Recipe has no output item.";
            if (UpgradeOnly)
                return "Upgrade-only recipe. Use upgrade mode.";
            if (RequireOnlyOneIngredient)
                return "Any-one-ingredient recipes are not supported yet.";
            if (DistinctIngredients == 0)
                return "Recipe has no ingredients.";
            if (DistinctIngredients > ingredientSlots)
                return "Needs " + DistinctIngredients + " ingredient types. This mill holds " + ingredientSlots + ".";
            return "";
        }
    }

    public enum MillState
    {
        NoRecipe,
        WaitingForInput,
        QuotaReached,
        OutputFull,
        Running,
        Paused
    }

    public struct MillConditions
    {
        public bool Powered;
        public double SpeedMultiplier;
        /// <summary>Empty when the station check passed; otherwise the reason, such as "Station missing".</summary>
        public string StationProblem;
        /// <summary>False when a stock quota says stop starting new crafts.</summary>
        public bool QuotaAllows;
    }

    /// <summary>
    /// A recipe mill's logical state: dynamic ingredient buffers, one job in persistent escrow,
    /// and an output buffer. Ingredients move into escrow only when output room is already there.
    /// The job finishes once. Cancelling, changing recipe, or destroying the mill returns escrow once.
    /// </summary>
    public sealed class RecipeMill
    {
        private readonly Dictionary<string, List<ItemStack>> _inputs = new Dictionary<string, List<ItemStack>>();
        private readonly List<ItemStack> _escrow = new List<ItemStack>();
        private readonly List<ItemStack> _output = new List<ItemStack>();
        private readonly List<ItemStack> _recovery = new List<ItemStack>();

        public RecipeMill(int ingredientSlots = 8, double maxSpeedMultiplier = BalanceDefaults.MillMaxSpeedMultiplier)
        {
            IngredientSlots = ingredientSlots;
            MaxSpeedMultiplier = maxSpeedMultiplier;
        }

        public int IngredientSlots { get; }
        public double MaxSpeedMultiplier { get; }
        public RecipeSpec? Recipe { get; private set; }
        public long TeacherId { get; private set; }
        public string TeacherName { get; private set; } = "";
        public MillState State { get; private set; } = MillState.NoRecipe;
        public string Reason { get; private set; } = "No recipe taught.";
        public double Progress { get; private set; }
        public int CraftsCompleted { get; private set; }
        public bool HasJob => _escrow.Count > 0;
        public IReadOnlyList<ItemStack> Escrow => _escrow;
        public IReadOnlyList<ItemStack> Output => _output;
        public IReadOnlyList<ItemStack> Recovery => _recovery;

        public static double CycleSeconds(int distinctIngredients)
        {
            return Math.Max(BalanceDefaults.MillBaseSeconds, BalanceDefaults.MillBaseSeconds + BalanceDefaults.MillSecondsPerIngredientType * distinctIngredients);
        }

        public double EffectiveCycleSeconds(double speedMultiplier)
        {
            if (Recipe == null)
                return double.PositiveInfinity;
            var multiplier = Math.Max(0.25, Math.Min(MaxSpeedMultiplier, speedMultiplier));
            return Math.Max(BalanceDefaults.MillMinimumSeconds, CycleSeconds(Recipe.DistinctIngredients) / multiplier);
        }

        public int OutputCapacity => Recipe == null ? 0 : Math.Max(20, Recipe.OutputAmount * 4);

        /// <summary>
        /// Teach a recipe. A running job is cancelled into recovery first, and inputs the new recipe
        /// does not use go to recovery. Nothing is lost and nothing is refunded twice.
        /// </summary>
        public bool Teach(RecipeSpec recipe, long teacherId, string teacherName, out string reason)
        {
            reason = recipe.UnsupportedReason(IngredientSlots);
            if (reason.Length > 0)
                return false;
            if (Recipe != null && Recipe.Id == recipe.Id)
            {
                TeacherId = teacherId;
                TeacherName = teacherName ?? "";
                return true;
            }

            Cancel();
            var keep = new HashSet<string>(recipe.Ingredients.Select(i => i.Prefab));
            foreach (var prefab in _inputs.Keys.ToList())
            {
                if (keep.Contains(prefab))
                    continue;
                _recovery.AddRange(_inputs[prefab]);
                _inputs.Remove(prefab);
            }

            Recipe = recipe;
            TeacherId = teacherId;
            TeacherName = teacherName ?? "";
            State = MillState.WaitingForInput;
            Reason = "Waiting for ingredients.";
            return true;
        }

        public int Needed(string prefab)
        {
            if (Recipe == null)
                return 0;
            var ingredient = Recipe.Ingredients.FirstOrDefault(i => i.Prefab == prefab && i.Amount > 0);
            return ingredient == null ? 0 : ingredient.Amount;
        }

        public int Buffered(string prefab)
        {
            return _inputs.TryGetValue(prefab, out var list) ? list.Sum(s => s.Count) : 0;
        }

        /// <summary>Two crafts' worth of each ingredient may wait in the buffer.</summary>
        public bool CanAcceptInput(ItemStack item)
        {
            var need = Needed(item.Prefab);
            return need > 0 && Buffered(item.Prefab) + item.Count <= need * 2;
        }

        public bool TryAcceptInput(ItemStack item)
        {
            if (item == null || item.Count < 1 || !CanAcceptInput(item))
                return false;
            if (!_inputs.TryGetValue(item.Prefab, out var list))
            {
                list = new List<ItemStack>();
                _inputs[item.Prefab] = list;
            }

            var existing = list.FirstOrDefault(s => s.SameIdentity(item));
            if (existing != null)
                existing.Count += item.Count;
            else
                list.Add(item.Copy(item.Count));
            return true;
        }

        public ItemStack? PeekOutput()
        {
            return _output.Count == 0 ? null : _output[0];
        }

        public ItemStack? TakeOneOutput()
        {
            if (_output.Count == 0)
                return null;
            var first = _output[0];
            var one = first.Copy(1);
            first.Count -= 1;
            if (first.Count <= 0)
                _output.RemoveAt(0);
            return one;
        }

        public ItemStack? PeekRecovery()
        {
            return _recovery.Count == 0 ? null : _recovery[0];
        }

        public ItemStack? TakeOneRecovery()
        {
            if (_recovery.Count == 0)
                return null;
            var first = _recovery[0];
            var one = first.Copy(1);
            first.Count -= 1;
            if (first.Count <= 0)
                _recovery.RemoveAt(0);
            return one;
        }

        public List<ItemStack> TakeRecovery()
        {
            var all = new List<ItemStack>(_recovery);
            _recovery.Clear();
            return all;
        }

        /// <summary>Authorised cancellation. Escrow goes to recovery exactly once.</summary>
        public void Cancel()
        {
            if (_escrow.Count > 0)
            {
                _recovery.AddRange(_escrow);
                _escrow.Clear();
            }

            Progress = 0;
            if (Recipe != null)
            {
                State = MillState.WaitingForInput;
                Reason = "Cancelled. Ingredients are in recovery.";
            }
        }

        /// <summary>Everything the mill holds, for a single drop when it is destroyed.</summary>
        public List<ItemStack> DrainAll()
        {
            var all = new List<ItemStack>();
            foreach (var list in _inputs.Values)
                all.AddRange(list);
            all.AddRange(_escrow);
            all.AddRange(_output);
            all.AddRange(_recovery);
            _inputs.Clear();
            _escrow.Clear();
            _output.Clear();
            _recovery.Clear();
            Progress = 0;
            return all;
        }

        public void Tick(double dtSeconds, MillConditions conditions)
        {
            if (Recipe == null)
            {
                State = MillState.NoRecipe;
                Reason = "No recipe taught.";
                return;
            }

            if (!HasJob)
            {
                if (!conditions.QuotaAllows)
                {
                    State = MillState.QuotaReached;
                    Reason = "Stock target reached.";
                    return;
                }

                if (OutputCount() + Recipe.OutputAmount > OutputCapacity)
                {
                    State = MillState.OutputFull;
                    Reason = "Output full.";
                    return;
                }

                var missing = Recipe.Ingredients.FirstOrDefault(i => i.Amount > 0 && Buffered(i.Prefab) < i.Amount);
                if (missing != null)
                {
                    State = MillState.WaitingForInput;
                    Reason = "Missing input: " + missing.Prefab + " " + Buffered(missing.Prefab) + "/" + missing.Amount;
                    return;
                }

                if (!string.IsNullOrEmpty(conditions.StationProblem))
                {
                    State = MillState.Paused;
                    Reason = conditions.StationProblem;
                    return;
                }

                if (!conditions.Powered)
                {
                    State = MillState.Paused;
                    Reason = "No power.";
                    return;
                }

                foreach (var ingredient in Recipe.Ingredients.Where(i => i.Amount > 0))
                    _escrow.AddRange(TakeInput(ingredient.Prefab, ingredient.Amount));
                Progress = 0;
            }

            if (!string.IsNullOrEmpty(conditions.StationProblem))
            {
                State = MillState.Paused;
                Reason = conditions.StationProblem + " Ingredients held.";
                return;
            }

            if (!conditions.Powered)
            {
                State = MillState.Paused;
                Reason = "No power. Ingredients held.";
                return;
            }

            State = MillState.Running;
            Progress += dtSeconds;
            var cycle = EffectiveCycleSeconds(conditions.SpeedMultiplier);
            Reason = "Crafting " + Recipe.OutputPrefab + " " + Math.Min(100, (int)(Progress / cycle * 100)) + "%";
            if (Progress < cycle)
                return;

            var cheated = _escrow.Any(s => s.Cheated);
            _output.Add(new ItemStack
            {
                Prefab = Recipe.OutputPrefab,
                Count = Recipe.OutputAmount,
                Quality = 1,
                Durability = -1,
                CrafterId = TeacherId,
                CrafterName = TeacherName,
                Cheated = cheated
            });
            _escrow.Clear();
            Progress = 0;
            CraftsCompleted++;
            State = MillState.WaitingForInput;
            Reason = "Finished one craft.";
        }

        public int OutputCount()
        {
            return _output.Sum(s => s.Count);
        }

        private List<ItemStack> TakeInput(string prefab, int count)
        {
            var list = _inputs[prefab];
            var taken = new List<ItemStack>();
            var remaining = count;
            for (var i = 0; i < list.Count && remaining > 0; i++)
            {
                var stack = list[i];
                var take = Math.Min(stack.Count, remaining);
                taken.Add(stack.Copy(take));
                stack.Count -= take;
                remaining -= take;
                if (stack.Count <= 0)
                {
                    list.RemoveAt(i);
                    i--;
                }
            }

            if (list.Count == 0)
                _inputs.Remove(prefab);
            return taken;
        }

        // Persistence. Recipe identity is saved; the caller resolves it against the live game on load.

        public string SaveState()
        {
            var builder = new StringBuilder();
            builder.Append("v1\n");
            builder.Append(Recipe != null ? Recipe.Id : "").Append('\n');
            builder.Append(TeacherId.ToString(CultureInfo.InvariantCulture)).Append('\n');
            builder.Append(ItemCodec.Escape(TeacherName)).Append('\n');
            builder.Append(Progress.ToString("R", CultureInfo.InvariantCulture)).Append('\n');
            builder.Append(CraftsCompleted.ToString(CultureInfo.InvariantCulture)).Append('\n');
            return builder.ToString();
        }

        public string SaveInputs()
        {
            return ItemCodec.Format(_inputs.Values.SelectMany(l => l));
        }

        public string SaveEscrow()
        {
            return ItemCodec.Format(_escrow);
        }

        public string SaveOutput()
        {
            return ItemCodec.Format(_output);
        }

        public string SaveRecovery()
        {
            return ItemCodec.Format(_recovery);
        }

        /// <summary>
        /// Restores a saved mill. A recipe that no longer resolves keeps its escrow in recovery
        /// rather than finishing under rules that may have changed.
        /// </summary>
        public void Load(string state, string inputs, string escrow, string output, string recovery, Func<string, RecipeSpec?> resolve)
        {
            _inputs.Clear();
            _escrow.Clear();
            _output.Clear();
            _recovery.Clear();
            Recipe = null;
            Progress = 0;
            var lines = (state ?? "").Split('\n');
            string recipeId = "";
            if (lines.Length >= 6 && lines[0] == "v1")
            {
                recipeId = lines[1];
                long.TryParse(lines[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var teacher);
                TeacherId = teacher;
                TeacherName = ItemCodec.Unescape(lines[3]);
                double.TryParse(lines[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var progress);
                Progress = progress;
                int.TryParse(lines[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out var crafts);
                CraftsCompleted = crafts;
            }

            foreach (var stack in ItemCodec.Parse(inputs))
            {
                if (!_inputs.TryGetValue(stack.Prefab, out var list))
                {
                    list = new List<ItemStack>();
                    _inputs[stack.Prefab] = list;
                }

                list.Add(stack);
            }

            _escrow.AddRange(ItemCodec.Parse(escrow));
            _output.AddRange(ItemCodec.Parse(output));
            _recovery.AddRange(ItemCodec.Parse(recovery));

            if (recipeId.Length > 0)
                Recipe = resolve(recipeId);
            if (Recipe == null)
            {
                if (_escrow.Count > 0)
                {
                    _recovery.AddRange(_escrow);
                    _escrow.Clear();
                }

                foreach (var list in _inputs.Values)
                    _recovery.AddRange(list);
                _inputs.Clear();
                Progress = 0;
                State = MillState.NoRecipe;
                Reason = recipeId.Length > 0 ? "Recipe " + recipeId + " is not in this game. Ingredients are in recovery." : "No recipe taught.";
                return;
            }

            State = HasJob ? MillState.Paused : MillState.WaitingForInput;
            Reason = HasJob ? "Resuming." : "Waiting for ingredients.";
        }

        /// <summary>For conservation tests: every item the mill is responsible for.</summary>
        public int HeldCount(string prefab)
        {
            return Buffered(prefab)
                + _escrow.Where(s => s.Prefab == prefab).Sum(s => s.Count)
                + _output.Where(s => s.Prefab == prefab).Sum(s => s.Count)
                + _recovery.Where(s => s.Prefab == prefab).Sum(s => s.Count);
        }
    }
}
