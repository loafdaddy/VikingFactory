using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using VikingFactory.Core;
using VikingFactory.Core.Crafting;
using VikingFactory.Core.Items;
using VikingFactory.Core.Logistics;

namespace VikingFactory.Machines
{
    /// <summary>Reads recipes from the running game. Nothing is invented.</summary>
    public static class LiveRecipes
    {
        public static RecipeSpec ToSpec(Recipe recipe)
        {
            if (recipe == null || recipe.m_item == null)
                return null;
            var spec = new RecipeSpec
            {
                Id = recipe.name,
                OutputPrefab = recipe.m_item.gameObject.name,
                OutputAmount = Mathf.Max(1, recipe.m_amount),
                MaxQuality = recipe.m_item.m_itemData.m_shared.m_maxQuality,
                Station = recipe.m_craftingStation != null ? recipe.m_craftingStation.m_name : "",
                StationLevel = Mathf.Max(1, recipe.m_minStationLevel),
                RequireOnlyOneIngredient = recipe.m_requireOnlyOneIngredient,
                UpgradeOnly = recipe.m_noCraftOnlyUpgrade,
                Enabled = recipe.m_enabled
            };
            if (recipe.m_resources != null)
            {
                foreach (var requirement in recipe.m_resources)
                {
                    if (requirement == null || requirement.m_resItem == null)
                        continue;
                    spec.Ingredients.Add(new Ingredient(requirement.m_resItem.gameObject.name, requirement.m_amount, requirement.m_amountPerLevel));
                }
            }

            return spec;
        }

        public static Recipe FindById(string id)
        {
            if (ObjectDB.instance == null)
                return null;
            return ObjectDB.instance.m_recipes.FirstOrDefault(r => r != null && r.name == id);
        }

        public static Recipe FindForOutput(string prefab)
        {
            if (ObjectDB.instance == null)
                return null;
            return ObjectDB.instance.m_recipes
                .Where(r => r != null && r.m_enabled && r.m_item != null && r.m_item.gameObject.name == prefab)
                .OrderBy(r => r.m_requireOnlyOneIngredient ? 1 : 0)
                .FirstOrDefault();
        }

        private static readonly FieldInfo HaveFire = typeof(CraftingStation).GetField("m_haveFire", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>
        /// The same checks a player gets at the station: type in range, level, roof cover, and fire.
        /// Empty means the station is usable.
        /// </summary>
        public static string StationProblem(RecipeSpec spec, Vector3 position, int requiredLevel)
        {
            if (string.IsNullOrEmpty(spec.Station))
                return "";
            var station = CraftingStation.HaveBuildStationInRange(spec.Station, position);
            if (station == null)
                return "Station missing: " + Localize(spec.Station) + " in range.";
            if (station.GetLevel() < requiredLevel)
                return "Station level too low: needs " + requiredLevel + ", has " + station.GetLevel() + ".";
            if (station.m_craftRequireRoof && station.m_roofCheckPoint != null)
            {
                Cover.GetCoverForPoint(station.m_roofCheckPoint.position, out var cover, out var underRoof);
                if (!underRoof || cover < 0.7f)
                    return "Station needs a roof.";
            }

            if (station.m_craftRequireFire && HaveFire != null && !(bool)HaveFire.GetValue(station))
                return "Station needs fire.";
            return "";
        }

        public static string Localize(string token)
        {
            return Jotunn.Managers.LocalizationManager.Instance != null ? Jotunn.Managers.LocalizationManager.Instance.TryTranslate(token) : token;
        }
    }

    /// <summary>
    /// Recipe mill and advanced assembler. A player who knows a recipe teaches it by using the crafted item on
    /// the mill. Inputs arrive by belt or feeder, move into persistent escrow, and finish once. The assembler
    /// also has an upgrade mode that raises one existing item by one quality level.
    /// </summary>
    public sealed class MillBehaviour : MachineBehaviour
    {
        private static readonly int[] Quotas = { 0, 20, 50, 100, 200 };
        private RecipeMill _mill;
        private int _quotaIndex;
        private StockTarget _quota = new StockTarget(0);
        private bool _upgradeMode;
        private StackStore _upItems = new StackStore(4, 1);
        private StackStore _upIngredients = new StackStore(8, 200);
        private List<ItemStack> _upEscrow = new List<ItemStack>();
        private StackStore _upOutput = new StackStore(4, 1);
        private float _upProgress;
        private string _upReason = "";

        public MillBehaviour(WorkshopMachine machine) : base(machine)
        {
            _mill = NewMill();
        }

        private bool IsAssembler => M.Role == MachineRole.Assembler;

        private RecipeMill NewMill()
        {
            return IsAssembler ? new RecipeMill(12, BalanceDefaults.AssemblerMaxSpeedMultiplier) : new RecipeMill(8, BalanceDefaults.MillMaxSpeedMultiplier);
        }

        protected override void Load()
        {
            _mill = NewMill();
            _mill.Load(M.GetString("vf_mill"), M.GetString("vf_in"), M.GetString("vf_escrow"), M.GetString("vf_out"), M.GetString("vf_recovery"),
                id => LiveRecipes.ToSpec(LiveRecipes.FindById(id)));
            _quotaIndex = Mathf.Clamp(M.GetInt("vf_quota"), 0, Quotas.Length - 1);
            _quota = new StockTarget(Quotas[_quotaIndex]);
            _upgradeMode = IsAssembler && M.GetInt("vf_upgrade") == 1;
            _upItems = new StackStore(4, 1, ItemCodec.Parse(M.GetString("vf_up_items")));
            _upIngredients = new StackStore(8, 200, ItemCodec.Parse(M.GetString("vf_up_in")));
            _upEscrow = ItemCodec.Parse(M.GetString("vf_up_escrow"));
            _upOutput = new StackStore(4, 1, ItemCodec.Parse(M.GetString("vf_up_out")));
            _upProgress = M.GetFloat("vf_up_progress");
        }

        protected override void Save()
        {
            M.Set("vf_mill", _mill.SaveState());
            M.Set("vf_in", _mill.SaveInputs());
            M.Set("vf_escrow", _mill.SaveEscrow());
            M.Set("vf_out", _mill.SaveOutput());
            M.Set("vf_recovery", _mill.SaveRecovery());
            M.Set("vf_quota", _quotaIndex);
            M.Set("vf_upgrade", _upgradeMode ? 1 : 0);
            M.Set("vf_up_items", ItemCodec.Format(_upItems.Stacks));
            M.Set("vf_up_in", ItemCodec.Format(_upIngredients.Stacks));
            M.Set("vf_up_escrow", ItemCodec.Format(_upEscrow));
            M.Set("vf_up_out", ItemCodec.Format(_upOutput.Stacks));
            M.Set("vf_up_progress", _upProgress);
        }

        private IItemEndpoint OutputTarget()
        {
            return M.ItemOut.Count == 0 ? null : EndpointFinder.MachinePort(M, M.ItemOut[0].position, true) ?? ChestAt(M.ItemOut[0].position);
        }

        private IItemEndpoint ChestAt(Vector3 position)
        {
            var endpoint = EndpointFinder.FindInsert(M, position, 0f);
            return endpoint is ContainerEndpoint ? endpoint : null;
        }

        public override void Tick(float dt)
        {
            PushOutput();
            if (_upgradeMode)
            {
                TickUpgrade(dt);
                return;
            }

            var recipe = _mill.Recipe;
            var conditions = new MillConditions
            {
                Powered = M.Producing,
                SpeedMultiplier = M.UsefulRpm / BalanceDefaults.MilestoneRpm,
                StationProblem = recipe == null ? "" : LiveRecipes.StationProblem(recipe, M.transform.position, recipe.StationLevel),
                QuotaAllows = QuotaAllows()
            };
            var crafts = _mill.CraftsCompleted;
            var hadJob = _mill.HasJob;
            _mill.Tick(dt, conditions);
            if (crafts != _mill.CraftsCompleted || hadJob != _mill.HasJob)
                SaveNow();
        }

        private bool QuotaAllows()
        {
            if (!_quota.Enabled || _mill.Recipe == null)
                return true;
            var target = OutputTarget();
            var downstream = target != null ? Math.Max(0, target.Count(_mill.Recipe.OutputPrefab)) : 0;
            return _quota.ShouldDeliver(downstream + _mill.OutputCount());
        }

        private void PushOutput()
        {
            var target = OutputTarget();
            if (target == null || !target.Available(out _))
                return;
            var recovery = _mill.PeekRecovery();
            if (recovery != null)
            {
                // Recovered ingredients leave through the output before new products.
                var one = recovery.Copy(1);
                if (target.CanInsert(one) && target.TryInsert(one))
                {
                    _mill.TakeOneRecovery();
                    SaveNow();
                }

                return;
            }

            var product = _mill.PeekOutput() != null ? _mill.PeekOutput().Copy(1) : null;
            if (product != null && target.CanInsert(product) && target.TryInsert(product))
            {
                _mill.TakeOneOutput();
                SaveNow();
                return;
            }

            var upgraded = _upOutput.Peek();
            if (upgraded != null && target.CanInsert(upgraded) && target.TryInsert(upgraded.Copy(1)))
            {
                _upOutput.TakeOne(s => s.SameIdentity(upgraded));
                SaveNow();
            }
        }

        private void TickUpgrade(float dt)
        {
            var recipe = _mill.Recipe;
            if (recipe == null)
            {
                _upReason = "Teach a recipe first.";
                return;
            }

            if (_upEscrow.Count == 0)
            {
                var item = _upItems.Peek();
                if (item == null)
                {
                    _upReason = "Waiting for an item to upgrade.";
                    return;
                }

                var station = CraftingStation.HaveBuildStationInRange(recipe.Station, M.transform.position);
                var level = station != null ? station.GetLevel() : 0;
                var problem = UpgradePlan.Check(recipe, item, level);
                if (problem.Length > 0)
                {
                    _upReason = problem;
                    return;
                }

                var cost = UpgradePlan.NextLevelCost(recipe, item.Quality);
                var missing = cost.FirstOrDefault(c => _upIngredients.CountOf(c.Prefab) < c.Amount);
                if (missing != null)
                {
                    _upReason = "Missing " + missing.Prefab + " " + _upIngredients.CountOf(missing.Prefab) + "/" + missing.Amount;
                    return;
                }

                if (!_upOutput.CanAccept(UpgradePlan.Apply(item)))
                {
                    _upReason = "Output full.";
                    return;
                }

                var taken = _upItems.TakeOne(s => s.SameIdentity(item));
                _upEscrow.Add(taken);
                foreach (var c in cost)
                    _upEscrow.AddRange(_upIngredients.TakeExact(c.Prefab, c.Amount));
                _upProgress = 0f;
                SaveNow();
            }

            var target = _upEscrow[0];
            var problemNow = LiveRecipes.StationProblem(recipe, M.transform.position, UpgradePlan.RequiredStationLevel(recipe, target.Quality + 1));
            if (problemNow.Length > 0 || !M.Producing)
            {
                _upReason = (problemNow.Length > 0 ? problemNow : "No power.") + " Item and resources held.";
                return;
            }

            _upProgress += dt;
            var cycle = _mill.EffectiveCycleSeconds(M.UsefulRpm / BalanceDefaults.MilestoneRpm);
            _upReason = "Upgrading " + target.Prefab + " to quality " + (target.Quality + 1) + " " + Mathf.Min(100, (int)(_upProgress / cycle * 100)) + "%";
            if (_upProgress < cycle)
                return;
            _upOutput.TryAdd(UpgradePlan.Apply(target));
            _upEscrow.Clear();
            _upProgress = 0f;
            SaveNow();
        }

        public override IItemEndpoint InputAt(Transform port)
        {
            return new DelegateEndpoint
            {
                Name = M.Spec.Name,
                Machine = M,
                CanInsertFn = CanAccept,
                InsertFn = one =>
                {
                    bool ok;
                    if (_upgradeMode)
                        ok = IsUpgradeTarget(one) ? _upItems.TryAdd(one) : _upIngredients.TryAdd(one);
                    else
                        ok = _mill.TryAcceptInput(one);
                    if (ok)
                        SaveNow();
                    return ok;
                }
            };
        }

        /// <summary>A feeder may take finished products from the output.</summary>
        public override IItemEndpoint OutputAt(Transform port)
        {
            return new DelegateEndpoint
            {
                Name = M.Spec.Name,
                Machine = M,
                PeekFn = filter =>
                {
                    var product = _mill.PeekOutput();
                    return product != null && (filter == null || filter(product)) ? product.Copy(1) : null;
                },
                RemoveFn = one =>
                {
                    var product = _mill.PeekOutput();
                    if (product == null || !product.SameIdentity(one))
                        return false;
                    _mill.TakeOneOutput();
                    SaveNow();
                    return true;
                },
                CountFn = prefab => _mill.Output.Where(s => s.Prefab == prefab).Sum(s => s.Count)
            };
        }

        private bool IsUpgradeTarget(ItemStack one)
        {
            return _mill.Recipe != null && one.Prefab == _mill.Recipe.OutputPrefab;
        }

        private bool CanAccept(ItemStack one)
        {
            if (!_upgradeMode)
                return _mill.CanAcceptInput(one);
            if (_mill.Recipe == null)
                return false;
            if (IsUpgradeTarget(one))
                return one.Quality < _mill.Recipe.MaxQuality && _upItems.CanAccept(one);
            return _mill.Recipe.Ingredients.Any(i => i.Prefab == one.Prefab && i.AmountPerLevel > 0) && _upIngredients.CanAccept(one);
        }

        public override bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            var player = user as Player;
            if (player == null)
                return false;
            var prefab = ItemBridge.PrefabName(item);
            if (IsAssembler && _mill.Recipe != null && _mill.Recipe.OutputPrefab == prefab && !_upgradeMode && _mill.Recipe.MaxQuality > 1 && !_mill.HasJob)
            {
                _upgradeMode = true;
                SaveNow();
                WorkshopMachine.Message(user, "Upgrade mode: feed " + prefab + " and its upgrade resources.");
                return true;
            }

            if (!PrivateArea.CheckAccess(M.transform.position, 0f, true))
                return true;
            var recipe = LiveRecipes.FindForOutput(prefab);
            if (recipe == null)
            {
                WorkshopMachine.Message(user, "No recipe makes that.");
                return true;
            }

            if (Jotunn.Managers.ItemManager.Instance.GetRecipe(recipe.name) != null && !WorkshopConfig.ModdedRecipeAllowed(recipe.name))
            {
                WorkshopMachine.Message(user, "Recipe " + recipe.name + " comes from another mod. The server must list it in ModdedRecipeAllowlist.");
                return true;
            }

            if (!player.IsRecipeKnown(recipe.m_item.m_itemData.m_shared.m_name) && !player.NoCostCheat())
            {
                WorkshopMachine.Message(user, "You do not know that recipe.");
                return true;
            }

            var spec = LiveRecipes.ToSpec(recipe);
            if (!_mill.Teach(spec, player.GetPlayerID(), player.GetPlayerName(), out var reason))
            {
                WorkshopMachine.Message(user, reason);
                return true;
            }

            _upgradeMode = false;
            SaveNow();
            WorkshopMachine.Message(user, "Taught " + LiveRecipes.Localize(recipe.m_item.m_itemData.m_shared.m_name));
            return true;
        }

        public override bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold)
                return false;
            if (alt)
            {
                _mill.Cancel();
                if (_upEscrow.Count > 0)
                {
                    foreach (var stack in _upEscrow)
                        ItemBridge.Drop(stack, M.transform.position);
                    _upEscrow.Clear();
                }

                if (_upgradeMode)
                {
                    _upgradeMode = false;
                    WorkshopMachine.Message(user, "Craft mode.");
                }
                else
                    WorkshopMachine.Message(user, "Cancelled. Ingredients leave through the output.");
                SaveNow();
                return true;
            }

            _quotaIndex = (_quotaIndex + 1) % Quotas.Length;
            _quota = new StockTarget(Quotas[_quotaIndex]);
            SaveNow();
            WorkshopMachine.Message(user, Quotas[_quotaIndex] == 0 ? "No stock target" : "Stop at " + Quotas[_quotaIndex] + " in the output chest");
            return true;
        }

        public override List<ItemStack> Drain()
        {
            var all = _mill.DrainAll();
            all.AddRange(_upItems.TakeAll());
            all.AddRange(_upIngredients.TakeAll());
            all.AddRange(_upEscrow);
            _upEscrow.Clear();
            all.AddRange(_upOutput.TakeAll());
            return all;
        }

        public override string Status()
        {
            var recipe = _mill.Recipe;
            var text = recipe == null ? "Use a crafted item you know on the mill to teach it." : "Recipe: " + recipe.OutputPrefab + " ×" + recipe.OutputAmount + (recipe.Station.Length > 0 ? " at " + LiveRecipes.Localize(recipe.Station) + " " + recipe.StationLevel : "");
            text += "\n" + (_upgradeMode ? _upReason : _mill.Reason);
            if (_mill.OutputCount() > 0)
                text += "\nOutput: " + _mill.OutputCount();
            if (_mill.Recovery.Count > 0)
                text += "\nRecovery: " + Describe(_mill.Recovery);
            if (_quotaIndex > 0)
                text += "\nStock target " + Quotas[_quotaIndex] + ".";
            text += "\nInteract: stock target. Shift+interact: cancel." + (IsAssembler ? " Use the taught item again: upgrade mode." : "");
            return text;
        }
    }
}
