using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VikingFactory.Core;
using VikingFactory.Core.Agriculture;
using VikingFactory.Core.Items;
using VikingFactory.Core.Production;

namespace VikingFactory.Machines
{
    /// <summary>Shared output handling: push into whatever sits at Item_Out, and let feeders pull from it.</summary>
    public abstract class ProducerBehaviour : MachineBehaviour
    {
        protected StackStore Output = new StackStore(4, 50);
        protected string Reason = "";
        private float _pushWait;

        protected ProducerBehaviour(WorkshopMachine machine) : base(machine) { }

        protected void LoadOutput() { Output = new StackStore(4, 50, ItemCodec.Parse(M.GetString("vf_out"))); }
        protected void SaveOutput() { M.Set("vf_out", ItemCodec.Format(Output.Stacks)); }

        protected void PushOutput(float dt)
        {
            _pushWait -= dt;
            if (_pushWait > 0f || M.ItemOut.Count == 0)
                return;
            _pushWait = 1f;
            var first = Output.Peek(CanExport);
            if (first == null)
                return;
            var next = EndpointFinder.MachinePort(M, M.ItemOut[0].position, true);
            if (next == null || !next.Available(out _))
                return;
            var one = first.Copy(1);
            if (!next.CanInsert(one) || !next.TryInsert(one))
                return;
            Output.TakeOne(s => s.SameIdentity(one));
            SaveNow();
        }

        protected virtual bool CanExport(ItemStack stack) { return true; }

        public override IItemEndpoint OutputAt(Transform port)
        {
            return new StoreEndpoint(M, M.Spec.Name, () => Output, SaveNow, s => false) { };
        }

        protected ItemStack NewItem(string prefab)
        {
            return new ItemStack { Prefab = prefab, Count = 1, Durability = -1, WorldLevel = Game.m_worldLevel };
        }

        protected static bool NaturalGround(Vector3 position, out string reason)
        {
            reason = "";
            if (position.y > 3000f)
            {
                reason = "Not inside a dungeon.";
                return false;
            }

            var terrain = LayerMask.GetMask("terrain");
            if (!Physics.Raycast(position + Vector3.up * 1f, Vector3.down, 2.5f, terrain))
            {
                reason = "Needs natural ground underneath.";
                return false;
            }

            if (Floating.GetLiquidLevel(position, 1f, LiquidType.Water) > position.y + 0.2f)
            {
                reason = "Cannot work underwater.";
                return false;
            }

            return true;
        }

        protected string SiteId => M.GetZdo() != null ? M.GetZdo().m_uid.ToString() : M.GetInstanceID().ToString();

        protected bool Spaced(MachineRole role, float metres)
        {
            var sites = WorkshopRegistry.All.Where(m => m.Role == role).Select(WaterWheelBehaviour.Site).ToList();
            return SiteSpacing.IsClear(WaterWheelBehaviour.Site(M), sites, metres, out _);
        }

        public override List<ItemStack> Drain() { return Output.TakeAll(); }
    }

    /// <summary>Bedrock quarry: the mod's explicit stone abstraction. No terrain is dug.</summary>
    public sealed class QuarryBehaviour : ProducerBehaviour
    {
        private TimedProducer _producer = new TimedProducer(BalanceDefaults.QuarrySecondsPerStone, BalanceDefaults.QuarryCapacity);
        private float _check;
        private bool _siteOk;

        public QuarryBehaviour(WorkshopMachine machine) : base(machine) { }

        protected override void Load()
        {
            LoadOutput();
            _producer = new TimedProducer(BalanceDefaults.QuarrySecondsPerStone, BalanceDefaults.QuarryCapacity, M.GetFloat("vf_progress"), Output.CountOf("Stone"));
        }

        protected override void Save()
        {
            SaveOutput();
            M.Set("vf_progress", (float)_producer.Progress);
        }

        public override string FeatureBlock()
        {
            return WorkshopConfig.Flags.BedrockQuarry ? "" : "The server preset disables the bedrock quarry.";
        }

        public override void Tick(float dt)
        {
            PushOutput(dt);
            _check -= dt;
            if (_check <= 0f)
            {
                _check = 5f;
                var probe = M.GroundProbe != null ? M.GroundProbe.position : M.transform.position;
                _siteOk = NaturalGround(probe, out Reason);
                if (_siteOk && !Spaced(MachineRole.Quarry, BalanceDefaults.QuarrySpacingMetres))
                {
                    _siteOk = false;
                    Reason = "Another quarry is within 12 m.";
                }
            }

            if (!_siteOk || !M.Producing)
                return;
            var buffered = Output.CountOf("Stone");
            _producer = new TimedProducer(BalanceDefaults.QuarrySecondsPerStone, BalanceDefaults.QuarryCapacity, _producer.Progress, buffered);
            var made = _producer.Tick(dt, true);
            for (var i = 0; i < made; i++)
                Output.TryAdd(NewItem("Stone"));
            Reason = _producer.Full ? "Full: 50 stone." : "Quarrying.";
            if (made > 0 || _producer.Full)
                SaveNow();
        }

        public override string Status()
        {
            return Reason + "\nStone " + Output.CountOf("Stone") + "/" + BalanceDefaults.QuarryCapacity + ", next in " + (BalanceDefaults.QuarrySecondsPerStone - _producer.Progress).ToString("0") + " s.";
        }
    }

    /// <summary>
    /// Managed coppice. Seeds are consumed once as rootstock. Growth runs on loaded, active time and never
    /// faster with power. A timber saw within 6 m takes the batch.
    /// </summary>
    public sealed class CoppiceBehaviour : MachineBehaviour
    {
        private CoppiceBed _bed = new CoppiceBed();
        private float _saveTimer;
        private float _check;
        private string _reason = "";
        private bool _siteOk = true;

        public CoppiceBehaviour(WorkshopMachine machine) : base(machine) { }

        protected override void Load()
        {
            _bed = CoppiceBed.Load(M.GetString("vf_bed"), 0);
            _bed.NativeGrowSeconds = NativeGrow(_bed.Current);
        }

        protected override void Save() { M.Set("vf_bed", _bed.Save()); }

        private static double NativeGrow(CoppiceSpecies species)
        {
            if (species == null || ZNetScene.instance == null)
                return 0;
            var longest = 0.0;
            foreach (var name in species.SaplingPrefab.Split('|'))
            {
                var prefab = ZNetScene.instance.GetPrefab(name);
                var plant = prefab != null ? prefab.GetComponent<Plant>() : null;
                if (plant != null)
                    longest = Math.Max(longest, plant.m_growTimeMax);
            }

            return longest;
        }

        public override string FeatureBlock()
        {
            return WorkshopConfig.Flags.Coppice ? "" : "The server preset disables managed coppice.";
        }

        public override void AddNode(Core.Kinetics.KineticNetwork network, string id) { network.AddTransmission(id); }

        public override void Tick(float dt)
        {
            _check -= dt;
            if (_check <= 0f)
            {
                _check = 10f;
                _siteOk = CheckSite(out _reason);
            }

            if (!_siteOk || !_bed.Commissioned)
                return;
            _bed.Tick(dt);
            _saveTimer += dt;
            if (_saveTimer >= 10f)
            {
                _saveTimer = 0f;
                SaveNow();
            }
        }

        private bool CheckSite(out string reason)
        {
            reason = "Growing.";
            var center = M.transform.position;
            var roof = LayerMask.GetMask("piece", "Default", "static_solid");
            if (Physics.Raycast(center + Vector3.up * 0.5f, Vector3.up, 30f, roof))
            {
                reason = "Needs open sky. No roofed plantations.";
                return false;
            }

            if (!Physics.Raycast(center + Vector3.up, Vector3.down, 2.5f, LayerMask.GetMask("terrain")))
            {
                reason = "Needs natural ground.";
                return false;
            }

            if (_bed.Current != null && ZNetScene.instance != null)
            {
                var sapling = ZNetScene.instance.GetPrefab(_bed.Current.SaplingPrefab.Split('|')[0]);
                var plant = sapling != null ? sapling.GetComponent<Plant>() : null;
                if (plant != null && (plant.m_biome & Heightmap.FindBiome(center)) == 0)
                {
                    reason = "Wrong biome for this species.";
                    return false;
                }
            }

            foreach (var other in WorkshopRegistry.All)
            {
                if (other != M && other.Role == MachineRole.CoppiceBed && Vector3.Distance(other.transform.position, center) < 8f
                    && string.CompareOrdinal(Id(other), Id(M)) < 0)
                {
                    reason = "Overlaps another coppice bed. Beds need 8 × 8 m each.";
                    return false;
                }
            }

            return true;
        }

        private static string Id(WorkshopMachine machine)
        {
            return machine.GetZdo() != null ? machine.GetZdo().m_uid.ToString() : "";
        }

        public bool Mature => _siteOk && _bed.Mature;

        /// <summary>Called by a saw on the same owner. The whole batch goes, or nothing does.</summary>
        public bool TryHarvest(StackStore destination)
        {
            if (!M.CanActLocally(out _))
                return false;
            if (_bed.Mode == CoppiceMode.Resin)
            {
                var resin = new ItemStack { Prefab = "Resin", Count = 1, WorldLevel = Game.m_worldLevel };
                if (_bed.ResinBuffered <= 0 || !destination.CanAccept(resin))
                    return false;
                _bed.TakeResin();
                destination.TryAdd(resin);
                SaveNow();
                return true;
            }

            if (!Mature || _bed.Current == null)
                return false;
            var batch = _bed.Current.Harvest.Select(p => new ItemStack { Prefab = p.Key, Count = p.Value, WorldLevel = Game.m_worldLevel }).ToList();
            var probe = new StackStore(destination.SlotLimit, destination.MaxPerSlot, destination.Stacks.Select(s => s.Copy(s.Count)));
            foreach (var stack in batch)
            {
                if (!probe.TryAdd(stack))
                    return false;
            }

            _bed.HarvestTimber();
            foreach (var stack in batch)
                destination.TryAdd(stack);
            SaveNow();
            return true;
        }

        public override bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            var prefab = ItemBridge.PrefabName(item);
            var species = CoppiceBed.SpeciesForSeed(prefab);
            var player = user as Player;
            if (species != null && player != null)
            {
                foreach (var known in species.RequiredKnownItems)
                {
                    var go = ItemBridge.Prefab(known);
                    if (go != null && !player.IsMaterialKnown(go.GetComponent<ItemDrop>().m_itemData.m_shared.m_name))
                    {
                        WorkshopMachine.Message(user, "You have not discovered " + known + " yet.");
                        return true;
                    }
                }
            }

            var available = item.m_stack;
            var taken = _bed.Plant(prefab, available, out var reason);
            if (taken <= 0)
            {
                WorkshopMachine.Message(user, string.IsNullOrEmpty(reason) ? "Not a coppice seed." : reason);
                return true;
            }

            if (!TakeFromPlayer(user, item, taken))
            {
                // The player's stack changed. Rebuild from saved state so nothing is planted for free.
                ForceReload();
                return true;
            }

            _bed.NativeGrowSeconds = NativeGrow(_bed.Current);
            SaveNow();
            WorkshopMachine.Message(user, _bed.Commissioned ? "Planted. The first batch takes " + (_bed.CycleSeconds / 60).ToString("0") + " minutes." : "Planted " + taken + ". Needs " + BalanceDefaults.CoppiceFoundingCount + ".");
            return true;
        }

        public override bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold)
                return false;
            var target = _bed.Mode == CoppiceMode.Timber ? CoppiceMode.Resin : CoppiceMode.Timber;
            if (_bed.SetMode(target, out var reason))
            {
                SaveNow();
                WorkshopMachine.Message(user, target == CoppiceMode.Resin ? "Tapping for resin." : "Growing timber.");
            }
            else
                WorkshopMachine.Message(user, reason);
            return true;
        }

        public override string Status()
        {
            if (_bed.Current == null)
                return "Use 5 beech seeds, pine cones, birch seeds, or acorns to plant.";
            if (!_bed.Commissioned)
                return _bed.Current.Id + " coppice: planted " + (_bed.Founding != null ? _bed.Founding.Committed : 0) + "/" + BalanceDefaults.CoppiceFoundingCount + ".";
            var text = _bed.Current.Id + " coppice. " + _reason;
            if (_bed.Mode == CoppiceMode.Resin)
                return text + "\nTapping: " + _bed.ResinBuffered + " resin ready. Interact to switch after harvesting.";
            return text + "\n" + (_bed.Mature ? "Mature. Waiting for a timber saw." : "Growth " + (_bed.Growth / _bed.CycleSeconds * 100).ToString("0") + "% of " + (_bed.CycleSeconds / 60).ToString("0") + " min.");
        }
    }

    /// <summary>
    /// Timber saw. Harvests mature coppice beds within 6 m. Inside a plot marked by two or more of the saw
    /// owner's stakes, it cuts trees and logs with the native chop damage and tool tier, so native drops and
    /// yields apply once. It replants felled trees from seeds in its input.
    /// </summary>
    public sealed class SawBehaviour : ProducerBehaviour
    {
        private static readonly HashSet<string> WoodDrops = new HashSet<string> { "Wood", "RoundLog", "FineWood", "ElderBark", "YggdrasilWood", "Resin", "BeechSeeds", "PineCone", "BirchSeeds", "Acorn", "FirCone", "AncientSeed" };
        private StackStore _seeds = new StackStore(2, 50);
        private float _wait;
        private readonly List<KeyValuePair<Vector3, float>> _recentHits = new List<KeyValuePair<Vector3, float>>();
        private readonly List<KeyValuePair<Vector3, string>> _felled = new List<KeyValuePair<Vector3, string>>();
        private TreeBase _cutting;
        private string _cuttingName = "";
        private Vector3 _cuttingPos;

        public SawBehaviour(WorkshopMachine machine) : base(machine) { }

        protected override void Load()
        {
            LoadOutput();
            _seeds = new StackStore(2, 50, ItemCodec.Parse(M.GetString("vf_seeds")));
        }

        protected override void Save()
        {
            SaveOutput();
            M.Set("vf_seeds", ItemCodec.Format(_seeds.Stacks));
        }

        public override void Tick(float dt)
        {
            PushOutput(dt);
            if (!M.Producing)
                return;
            _wait -= dt;
            if (_wait > 0f)
                return;
            _wait = (float)BalanceDefaults.TimberSawSecondsPerStroke;

            if (_cutting == null && _cuttingName.Length > 0)
            {
                _felled.Add(new KeyValuePair<Vector3, string>(_cuttingPos, _cuttingName));
                _cuttingName = "";
            }

            foreach (var bed in WorkshopRegistry.All.Where(m => m.Role == MachineRole.CoppiceBed && Vector3.Distance(m.transform.position, M.transform.position) < 6f))
            {
                var coppice = bed.Behaviour as CoppiceBehaviour;
                if (coppice != null && coppice.TryHarvest(Output))
                {
                    Reason = "Harvested a coppice bed.";
                    SaveNow();
                    return;
                }
            }

            var collected = 0;
            _recentHits.RemoveAll(h => Time.time - h.Value > 30f);
            foreach (var hit in _recentHits)
                collected += WorldDrops.Collect(hit.Key, 4f, WoodDrops, Output);
            if (collected > 0)
                SaveNow();

            if (!Plot(out var bounds))
            {
                Reason = Reason.StartsWith("Harvested") ? Reason : "No mature coppice in 6 m and no plot of your stakes.";
                return;
            }

            if (Replant(bounds))
                return;
            Cut(bounds);
        }

        private bool Plot(out Bounds bounds)
        {
            bounds = new Bounds();
            var stakes = WorkshopRegistry.All.Where(m => m.Role == MachineRole.Marker && m.Creator == M.Creator
                && Vector3.Distance(m.transform.position, M.transform.position) < 24f).ToList();
            if (stakes.Count < 2)
                return false;
            bounds = new Bounds(stakes[0].transform.position, Vector3.zero);
            foreach (var stake in stakes)
                bounds.Encapsulate(stake.transform.position);
            bounds.Expand(new Vector3(1f, 60f, 1f));
            return bounds.size.x > 1.5f && bounds.size.z > 1.5f;
        }

        private void Cut(Bounds bounds)
        {
            var tier = WorkshopConfig.SawToolTier != null ? WorkshopConfig.SawToolTier.Value : 2;
            Component best = null;
            var bestDistance = float.MaxValue;
            foreach (var hit in Physics.OverlapBox(bounds.center, bounds.extents))
            {
                Component target = hit.GetComponentInParent<TreeLog>();
                if (target == null)
                    target = hit.GetComponentInParent<TreeBase>();
                if (target == null)
                    continue;
                var minTier = target is TreeLog ? ((TreeLog)target).m_minToolTier : ((TreeBase)target).m_minToolTier;
                if (minTier > tier)
                {
                    Reason = "A tree in the plot needs a better axe than this saw.";
                    continue;
                }

                var distance = Vector3.Distance(target.transform.position, M.transform.position);
                if (distance < bestDistance)
                {
                    best = target;
                    bestDistance = distance;
                }
            }

            if (best == null)
            {
                Reason = "Plot has nothing this saw can cut.";
                return;
            }

            var damage = new HitData();
            damage.m_damage.m_chop = 60f;
            damage.m_toolTier = (short)tier;
            damage.m_point = best.transform.position + Vector3.up * 0.5f;
            damage.m_dir = (best.transform.position - M.transform.position).normalized;
            var tree = best as TreeBase;
            if (tree != null)
            {
                _cutting = tree;
                _cuttingName = tree.gameObject.name.Replace("(Clone)", "").Trim();
                _cuttingPos = tree.transform.position;
                tree.Damage(damage);
            }
            else
                ((TreeLog)best).Damage(damage);
            _recentHits.Add(new KeyValuePair<Vector3, float>(best.transform.position, Time.time));
            Reason = "Cutting " + best.gameObject.name.Replace("(Clone)", "").Trim() + ".";
        }

        private bool Replant(Bounds bounds)
        {
            for (var i = 0; i < _felled.Count; i++)
            {
                var spot = _felled[i];
                foreach (var seed in _seeds.Stacks.ToList())
                {
                    foreach (var sapling in Planting.SaplingsFor(seed.Prefab))
                    {
                        var plant = sapling.GetComponent<Plant>();
                        if (plant == null || plant.m_grownPrefabs == null || !plant.m_grownPrefabs.Any(g => g != null && g.name == spot.Value))
                            continue;
                        var cost = Planting.Cost(sapling);
                        if (_seeds.CountOf(seed.Prefab) < cost || !Planting.CanPlant(sapling, spot.Key, out _))
                            continue;
                        _seeds.TakeExact(seed.Prefab, cost);
                        Planting.Plant(sapling, spot.Key);
                        _felled.RemoveAt(i);
                        SaveNow();
                        Reason = "Replanted " + spot.Value + ".";
                        return true;
                    }
                }
            }

            if (_felled.Count > 0)
                Reason = "Waiting for seeds to replant " + _felled.Count + " stump(s).";
            return false;
        }

        public override IItemEndpoint InputAt(Transform port)
        {
            return new StoreEndpoint(M, "saw seeds", () => _seeds, SaveNow, s => Planting.IsSeed(s.Prefab), false);
        }

        public override List<ItemStack> Drain()
        {
            var all = Output.TakeAll();
            all.AddRange(_seeds.TakeAll());
            return all;
        }

        public override string Status()
        {
            return Reason + "\nOutput: " + Describe(Output.Stacks) + "\nSeeds: " + Describe(_seeds.Stacks);
        }
    }

    /// <summary>
    /// Planter, harvester, and farm gantry on a visible field centred on the machine. Planting uses the game's
    /// saplings; harvesting uses the pickable's own pick, so drops and bonuses stay native.
    /// </summary>
    public sealed class FieldBehaviour : ProducerBehaviour
    {
        private StackStore _seeds = new StackStore(4, 100);
        private FieldPolicy _policy = FieldPolicy.ReplantSame;
        private int _tile;
        private float _plantWait;
        private float _harvestWait;
        private string _crop = "";

        public FieldBehaviour(WorkshopMachine machine) : base(machine) { }

        private bool Plants => M.Role != MachineRole.Harvester;
        private bool Harvests => M.Role != MachineRole.Planter;
        private float Size => M.Role == MachineRole.FarmGantry ? 8f : 4f;
        private Vector3 Center => M.GroundProbe != null ? M.GroundProbe.position : M.transform.position;

        protected override void Load()
        {
            LoadOutput();
            _seeds = new StackStore(4, 100, ItemCodec.Parse(M.GetString("vf_seeds")));
            _policy = (FieldPolicy)Mathf.Clamp(M.GetInt("vf_policy"), 0, 2);
            _tile = M.GetInt("vf_tile");
            _crop = M.GetString("vf_crop");
        }

        protected override void Save()
        {
            SaveOutput();
            M.Set("vf_seeds", ItemCodec.Format(_seeds.Stacks));
            M.Set("vf_policy", (int)_policy);
            M.Set("vf_tile", _tile);
            M.Set("vf_crop", _crop);
        }

        public override void Tick(float dt)
        {
            PushOutput(dt);
            if (!M.Producing)
                return;
            if (Harvests)
            {
                _harvestWait -= dt;
                if (_harvestWait <= 0f)
                {
                    _harvestWait = (float)BalanceDefaults.HarvesterSecondsPerAction;
                    Harvest();
                }
            }

            if (Plants)
            {
                _plantWait -= dt;
                if (_plantWait <= 0f)
                {
                    _plantWait = (float)BalanceDefaults.PlanterSecondsPerAction;
                    PlantNext();
                }
            }
        }

        private List<Vector3> Tiles(float radius)
        {
            return FieldPlanning.Tiles(Size, Size, radius).Select(t => Center + M.transform.rotation * new Vector3(t.Key, 0f, t.Value)).ToList();
        }

        private void PlantNext()
        {
            // The gantry plants from its own harvest, reserving seed before anything is exported.
            var source = M.Role == MachineRole.FarmGantry && _seeds.Total() == 0 ? Output : _seeds;
            var seed = source.Stacks.FirstOrDefault(s => (_crop.Length == 0 || s.Prefab == _crop) && Planting.IsSeed(s.Prefab));
            if (seed == null)
            {
                Reason = "No seed to plant" + (_crop.Length > 0 ? " (" + _crop + ")" : "") + ".";
                return;
            }

            var sapling = Planting.SaplingsFor(seed.Prefab).FirstOrDefault(s => s.GetComponent<Plant>() != null && s.GetComponent<TreeBase>() == null);
            if (sapling == null)
                return;
            var cost = Planting.Cost(sapling);
            var tiles = Tiles(sapling.GetComponent<Plant>().m_growRadius);
            for (var attempt = 0; attempt < tiles.Count; attempt++)
            {
                _tile = (_tile + 1) % tiles.Count;
                var position = tiles[_tile];
                position.y = WorldDrops.GroundHeight(position);
                if (!Planting.CanPlant(sapling, position, out var why))
                {
                    Reason = why;
                    continue;
                }

                if (source.CountOf(seed.Prefab) < cost)
                    return;
                var taken = source.TakeExact(seed.Prefab, cost);
                if (!Planting.Plant(sapling, position))
                {
                    foreach (var stack in taken)
                        source.TryAdd(stack);
                    return;
                }

                if (_crop.Length == 0)
                    _crop = seed.Prefab;
                Reason = "Planted " + sapling.name + ".";
                SaveNow();
                return;
            }
        }

        private void Harvest()
        {
            if (Player.m_localPlayer == null)
            {
                Reason = "Harvesting waits for a player on this peer.";
                return;
            }

            var half = new Vector3(Size / 2f, 2f, Size / 2f);
            foreach (var hit in Physics.OverlapBox(Center + Vector3.up, half, M.transform.rotation))
            {
                var pickable = hit.GetComponentInParent<Pickable>();
                if (pickable == null || !pickable.CanBePicked())
                    continue;
                var view = pickable.GetComponent<ZNetView>();
                if (view == null || !view.IsValid() || !view.IsOwner())
                    continue;
                var names = new HashSet<string>();
                if (pickable.m_itemPrefab != null)
                    names.Add(pickable.m_itemPrefab.name);
                names.UnionWith(WorldDrops.Names(pickable.m_extraDrops));
                if (!Output.CanAccept(NewItem(pickable.m_itemPrefab != null ? pickable.m_itemPrefab.name : "Wood")))
                {
                    Reason = "Output full.";
                    return;
                }

                view.InvokeRPC("RPC_Pick", 0);
                WorldDrops.Collect(pickable.transform.position, 1.5f, names, Output);
                Reason = "Harvested " + pickable.name.Replace("(Clone)", "").Trim() + ".";
                SaveNow();
                return;
            }

            foreach (var bed in WorkshopRegistry.All.Where(m => m.Role == MachineRole.ForageBed && Vector3.Distance(m.transform.position, Center) < Size))
            {
                var endpoint = bed.Behaviour.Body;
                if (endpoint == null || !endpoint.Available(out _))
                    continue;
                var item = endpoint.Peek(s => Output.CanAccept(s));
                if (item != null && endpoint.TryRemove(item))
                {
                    Output.TryAdd(item);
                    SaveNow();
                    Reason = "Picked from a forage bed.";
                    return;
                }
            }

            // Collect what the native pick left on the ground in this field.
            if (WorldDrops.Collect(Center, Size / 2f, CropYields(), Output) > 0)
                SaveNow();
            Reason = Reason.StartsWith("Planted") ? Reason : "Nothing ripe.";
        }

        private HashSet<string> CropYields()
        {
            var set = new HashSet<string>();
            if (_crop.Length > 0)
            {
                set.Add(_crop);
                foreach (var sapling in Planting.SaplingsFor(_crop))
                    set.Add(Planting.YieldOf(sapling));
            }

            return set;
        }

        protected override bool CanExport(ItemStack stack)
        {
            if (M.Role != MachineRole.FarmGantry || _crop.Length == 0 || stack.Prefab != _crop)
                return true;
            var sapling = Planting.SaplingsFor(_crop).FirstOrDefault();
            if (sapling == null)
                return true;
            var crop = new CropSpec { PlantingItem = _crop, PlantingCost = Planting.Cost(sapling), YieldItem = Planting.YieldOf(sapling) };
            var empty = Tiles(sapling.GetComponent<Plant>().m_growRadius).Count(p => Planting.CanPlant(sapling, new Vector3(p.x, WorldDrops.GroundHeight(p), p.z), out _));
            return FieldPlanning.Exportable(_policy, crop, stack.Prefab, Output.CountOf(stack.Prefab), empty) > 0;
        }

        public override IItemEndpoint InputAt(Transform port)
        {
            if (!Plants)
                return null;
            return new StoreEndpoint(M, "seed input", () => _seeds, SaveNow, s => Planting.IsSeed(s.Prefab) && (_crop.Length == 0 || s.Prefab == _crop), false);
        }

        public override IItemEndpoint OutputAt(Transform port)
        {
            if (!Harvests)
                return null;
            return new DelegateEndpoint
            {
                Name = M.Spec.Name,
                Machine = M,
                PeekFn = filter =>
                {
                    var stack = Output.Peek(s => CanExport(s) && (filter == null || filter(s)));
                    return stack == null ? null : stack.Copy(1);
                },
                RemoveFn = one =>
                {
                    if (Output.TakeOne(s => s.SameIdentity(one)) == null)
                        return false;
                    SaveNow();
                    return true;
                },
                CountFn = prefab => Output.CountOf(prefab)
            };
        }

        public override bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold)
                return false;
            if (alt)
            {
                _crop = "";
                SaveNow();
                WorkshopMachine.Message(user, "Crop cleared. The next seed sets it.");
                return true;
            }

            _policy = (FieldPolicy)(((int)_policy + 1) % 3);
            SaveNow();
            WorkshopMachine.Message(user, PolicyName());
            return true;
        }

        private string PolicyName()
        {
            switch (_policy)
            {
                case FieldPolicy.SeedProduction: return "Seed field: grows seed for another field.";
                case FieldPolicy.FoodProduction: return "Food field: seed arrives by belt, all yield leaves.";
                default: return "Replant: keeps next planting plus 10% before exporting.";
            }
        }

        public override List<ItemStack> Drain()
        {
            var all = Output.TakeAll();
            all.AddRange(_seeds.TakeAll());
            return all;
        }

        public override string Status()
        {
            var text = Size + " × " + Size + " m field. " + Reason;
            if (Plants)
                text += "\nCrop: " + (_crop.Length == 0 ? "not set" : _crop) + ". " + PolicyName() + " Seeds: " + Describe(_seeds.Stacks);
            if (Harvests || M.Role == MachineRole.FarmGantry)
                text += "\nHeld: " + Describe(Output.Stacks);
            return text + "\nInteract: policy. Shift+interact: clear crop.";
        }
    }

    /// <summary>
    /// Forage bed: a mod-added nursery for an allowlisted wild plant, founded with five specimens.
    /// It grows on active time, never faster than the native respawn, and holds five for a powered arm.
    /// </summary>
    public sealed class ForageBehaviour : MachineBehaviour
    {
        private string _item = "";
        private Commission _commission;
        private TimedProducer _producer;
        private float _saveTimer;

        public ForageBehaviour(WorkshopMachine machine) : base(machine) { }

        protected override void Load()
        {
            _item = M.GetString("vf_species");
            var spec = Forage.Find(_item);
            _commission = spec == null ? null : Commission.Load(spec.Item, BalanceDefaults.ForageCommissionCount, M.GetString("vf_commission"));
            _producer = spec == null ? null : new TimedProducer(Seconds(spec), BalanceDefaults.ForageCapacity, M.GetFloat("vf_progress"), M.GetInt("vf_ready"));
        }

        protected override void Save()
        {
            M.Set("vf_species", _item);
            M.Set("vf_commission", _commission != null ? _commission.Save() : "");
            M.Set("vf_progress", _producer != null ? (float)_producer.Progress : 0f);
            M.Set("vf_ready", _producer != null ? _producer.Buffered : 0);
        }

        private static double Seconds(ForageSpec spec)
        {
            var prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(spec.PickablePrefab) : null;
            var pickable = prefab != null ? prefab.GetComponent<Pickable>() : null;
            return Forage.SecondsPerItem(pickable != null ? pickable.m_respawnTimeMinutes : 0f);
        }

        public override string FeatureBlock()
        {
            return WorkshopConfig.Flags.Forage ? "" : "The server preset disables forage beds.";
        }

        public override void AddNode(Core.Kinetics.KineticNetwork network, string id) { network.AddTransmission(id); }

        public override void Tick(float dt)
        {
            if (_producer == null || _commission == null || !_commission.Complete)
                return;
            var made = _producer.Tick(dt, true);
            _saveTimer += dt;
            if (made > 0 || _saveTimer > 15f)
            {
                _saveTimer = 0f;
                SaveNow();
            }
        }

        public override IItemEndpoint Body => new DelegateEndpoint
        {
            Name = "forage bed",
            Machine = M,
            PeekFn = filter =>
            {
                if (_producer == null || _producer.Buffered <= 0)
                    return null;
                var item = new ItemStack { Prefab = _item, Count = 1, WorldLevel = Game.m_worldLevel, Durability = -1 };
                return filter == null || filter(item) ? item : null;
            },
            RemoveFn = one =>
            {
                if (_producer == null || one.Prefab != _item || !_producer.TakeOne())
                    return false;
                SaveNow();
                return true;
            },
            CountFn = prefab => prefab == _item && _producer != null ? _producer.Buffered : 0
        };

        public override bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            var prefab = ItemBridge.PrefabName(item);
            var spec = Forage.Find(_item.Length > 0 ? _item : prefab);
            if (spec == null)
            {
                WorkshopMachine.Message(user, "Only raspberries, blueberries, mushrooms, thistle, and cloudberries can be cultivated.");
                return true;
            }

            if (!Progression.IsUnlocked(spec.Milestones, out var missing))
            {
                WorkshopMachine.Message(user, "Locked until " + missing + ".");
                return true;
            }

            if (_commission == null)
            {
                _item = spec.Item;
                _commission = new Commission(spec.Item, BalanceDefaults.ForageCommissionCount);
                _producer = new TimedProducer(Seconds(spec), BalanceDefaults.ForageCapacity);
            }

            var taken = _commission.Offer(prefab, item.m_stack, out var result);
            if (taken <= 0)
            {
                WorkshopMachine.Message(user, result == CommissionResult.WrongItem ? "This bed grows " + _item + "." : "Already founded.");
                return true;
            }

            if (!TakeFromPlayer(user, item, taken))
            {
                ForceReload();
                return true;
            }

            SaveNow();
            WorkshopMachine.Message(user, _commission.Complete ? "Founded." : "Founding " + _commission.Committed + "/" + BalanceDefaults.ForageCommissionCount);
            return true;
        }

        public override string Status()
        {
            if (_commission == null)
                return "Use five specimens to found it.";
            if (!_commission.Complete)
                return _item + ": founding " + _commission.Committed + "/" + BalanceDefaults.ForageCommissionCount + ".";
            return _item + ": " + _producer.Buffered + "/" + BalanceDefaults.ForageCapacity + " ready, one every " + (_producer.SecondsPerItem / 60).ToString("0") + " min. A feeder or harvester collects it.";
        }
    }

    /// <summary>
    /// Mining head. Strikes the real rock in front with pickaxe damage and the configured tier. Depletion,
    /// drops, and tier rules stay native. Dungeon interiors are excluded.
    /// </summary>
    public sealed class MiningBehaviour : ProducerBehaviour
    {
        private float _wait;
        private Vector3 _lastHit;
        private float _lastHitTime = -100f;
        private HashSet<string> _drops = new HashSet<string>();

        public MiningBehaviour(WorkshopMachine machine) : base(machine) { }

        protected override void Load() { LoadOutput(); }
        protected override void Save() { SaveOutput(); }

        public override void Tick(float dt)
        {
            PushOutput(dt);
            if (Time.time - _lastHitTime < 20f && WorldDrops.Collect(_lastHit, 5f, _drops, Output) > 0)
                SaveNow();
            if (!M.Producing)
                return;
            _wait -= dt;
            if (_wait > 0f)
                return;
            _wait = (float)BalanceDefaults.MiningSecondsPerStroke;
            if (M.transform.position.y > 3000f)
            {
                Reason = "Mining heads do not work inside dungeons.";
                return;
            }

            var tier = WorkshopConfig.MiningToolTier != null ? WorkshopConfig.MiningToolTier.Value : 2;
            var face = M.transform.position + M.transform.forward * 1.8f + Vector3.up * 0.5f;
            foreach (var hit in Physics.OverlapSphere(face, 1.8f))
            {
                var rock5 = hit.GetComponentInParent<MineRock5>();
                var rock = rock5 == null ? hit.GetComponentInParent<MineRock>() : null;
                if (rock5 == null && rock == null)
                    continue;
                var minTier = rock5 != null ? rock5.m_minToolTier : rock.m_minToolTier;
                if (minTier > tier)
                {
                    Reason = "This deposit needs a better pickaxe tier.";
                    continue;
                }

                if (!Output.CanAccept(NewItem("Stone")))
                {
                    Reason = "Output full.";
                    return;
                }

                var damage = new HitData();
                damage.m_damage.m_pickaxe = 60f;
                damage.m_toolTier = (short)tier;
                damage.m_point = hit.ClosestPoint(face);
                damage.m_dir = M.transform.forward;
                _drops = WorldDrops.Names(rock5 != null ? rock5.m_dropItems : rock.m_dropItems);
                if (rock5 != null)
                    rock5.Damage(damage);
                else
                    rock.Damage(damage);
                _lastHit = damage.m_point;
                _lastHitTime = Time.time;
                Reason = "Mining " + (rock5 != null ? rock5.name : rock.name).Replace("(Clone)", "").Trim() + ".";
                return;
            }

            Reason = "No rock or deposit in front.";
        }

        public override string Status() { return Reason + "\nHeld: " + Describe(Output.Stacks); }
    }

    /// <summary>
    /// Deep extractor: the late, explicit recovery of trace older ores. Commissioned once per site with ten
    /// samples, never refunded. Boss key, biome, and 32 m spacing are checked. RPM does not speed it up.
    /// </summary>
    public sealed class ExtractorBehaviour : ProducerBehaviour
    {
        private string _resource = "";
        private Commission _commission;
        private TimedProducer _producer;
        private float _check;
        private bool _siteOk;

        public ExtractorBehaviour(WorkshopMachine machine) : base(machine) { }

        protected override void Load()
        {
            LoadOutput();
            _resource = M.GetString("vf_resource");
            var spec = DeepExtraction.Find(_resource);
            _commission = spec == null ? null : Commission.Load(spec.CommissionItem, spec.CommissionCount, M.GetString("vf_commission"));
            _producer = spec == null ? null : new TimedProducer(spec.SecondsPerItem, BalanceDefaults.ExtractorCapacity, M.GetFloat("vf_progress"), Output.CountOf(_resource));
        }

        protected override void Save()
        {
            SaveOutput();
            M.Set("vf_resource", _resource);
            M.Set("vf_commission", _commission != null ? _commission.Save() : "");
            M.Set("vf_progress", _producer != null ? (float)_producer.Progress : 0f);
        }

        public override string FeatureBlock()
        {
            return WorkshopConfig.Flags.DeepExtraction ? "" : "The server preset disables deep extraction.";
        }

        public override void Tick(float dt)
        {
            PushOutput(dt);
            var spec = DeepExtraction.Find(_resource);
            if (spec == null || _commission == null || !_commission.Complete)
                return;
            _check -= dt;
            if (_check <= 0f)
            {
                _check = 5f;
                _siteOk = CheckSite(spec, out Reason);
            }

            if (!_siteOk || !M.Producing)
                return;
            _producer = new TimedProducer(spec.SecondsPerItem, BalanceDefaults.ExtractorCapacity, _producer.Progress, Output.CountOf(_resource));
            var made = _producer.Tick(dt, true);
            for (var i = 0; i < made; i++)
                Output.TryAdd(NewItem(_resource));
            Reason = _producer.Full ? "Full." : "Extracting " + _resource + ".";
            SaveNow();
        }

        private bool CheckSite(ExtractionSpec spec, out string reason)
        {
            if (!Progression.IsUnlocked(spec.Milestones, out var missing))
            {
                reason = "Locked until " + missing + ".";
                return false;
            }

            var probe = M.GroundProbe != null ? M.GroundProbe.position : M.transform.position;
            if (!NaturalGround(probe, out reason))
                return false;
            if (Heightmap.FindBiome(probe).ToString() != spec.Biome)
            {
                reason = spec.Output + " is only extracted in the " + spec.Biome + ".";
                return false;
            }

            if (!Spaced(MachineRole.DeepExtractor, BalanceDefaults.ExtractorSpacingMetres))
            {
                reason = "Another extractor is within 32 m.";
                return false;
            }

            reason = "Site valid.";
            return true;
        }

        public override bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            var prefab = ItemBridge.PrefabName(item);
            var spec = DeepExtraction.Find(_resource.Length > 0 ? _resource : prefab);
            if (spec == null)
            {
                WorkshopMachine.Message(user, "Commission with copper ore, tin ore, scrap iron, or silver ore.");
                return true;
            }

            if (!Progression.IsUnlocked(spec.Milestones, out var missing))
            {
                WorkshopMachine.Message(user, spec.Output + " extraction is locked until " + missing + ".");
                return true;
            }

            if (_commission == null)
            {
                _resource = spec.Output;
                _commission = new Commission(spec.CommissionItem, spec.CommissionCount);
                _producer = new TimedProducer(spec.SecondsPerItem, BalanceDefaults.ExtractorCapacity);
            }

            var taken = _commission.Offer(prefab, item.m_stack, out var result);
            if (taken <= 0)
            {
                WorkshopMachine.Message(user, result == CommissionResult.WrongItem ? "This extractor is set to " + _resource + "." : "Already commissioned.");
                return true;
            }

            if (!TakeFromPlayer(user, item, taken))
            {
                ForceReload();
                return true;
            }

            SaveNow();
            WorkshopMachine.Message(user, "Commissioned " + _commission.Committed + "/" + spec.CommissionCount + ". Samples are consumed and not refunded.");
            return true;
        }

        public override string Status()
        {
            if (_commission == null)
                return "Use 10 of an older ore to commission. The samples are consumed.";
            if (!_commission.Complete)
                return _resource + ": commissioned " + _commission.Committed + "/" + _commission.RequiredCount + ".";
            return Reason + "\n" + _resource + " " + Output.CountOf(_resource) + "/" + BalanceDefaults.ExtractorCapacity + ", one per " + _producer.SecondsPerItem.ToString("0") + " s.";
        }
    }
}
