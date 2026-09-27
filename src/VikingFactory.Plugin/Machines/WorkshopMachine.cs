using System;
using System.Collections.Generic;
using UnityEngine;
using VikingFactory.Core.Items;
using VikingFactory.Core.Kinetics;
using VikingFactory.Core.Runtime;

namespace VikingFactory.Machines
{
    /// <summary>
    /// One placed workshop piece. The kinetic graph is rebuilt from live ports, never saved.
    /// Items, jobs, and settings live on the piece's ZDO and are written only by its owner.
    /// </summary>
    public class WorkshopMachine : MonoBehaviour, Hoverable, Interactable
    {
        public const string SchemaKey = "vf_schema";

        public string SpecId = "";

        [NonSerialized] public MachineSpec Spec;
        [NonSerialized] public MachineBehaviour Behaviour;
        [NonSerialized] public List<Transform> Kinetic = new List<Transform>();
        [NonSerialized] public List<Transform> ItemIn = new List<Transform>();
        [NonSerialized] public List<Transform> ItemOut = new List<Transform>();
        [NonSerialized] public Transform Pickup;
        [NonSerialized] public Transform Dropoff;
        [NonSerialized] public Transform WaterProbe;
        [NonSerialized] public Transform SmokeOut;
        [NonSerialized] public Transform WindProbe;
        [NonSerialized] public Transform GroundProbe;

        [NonSerialized] public int Supply;
        [NonSerialized] public int Reserved;
        [NonSerialized] public string Stop = "No power.";
        [NonSerialized] public bool Producing;
        [NonSerialized] public bool Turning;
        [NonSerialized] public double Speed;
        [NonSerialized] public double UsefulRpm;
        [NonSerialized] public bool Enabled = true;
        [NonSerialized] public string DisabledReason = "";
        [NonSerialized] public string NodeId = "";
        [NonSerialized] public int Component = -1;
        [NonSerialized] public readonly OwnershipTracker Ownership = new OwnershipTracker();

        private bool _joined;
        private bool _schemaPaused;
        private ZNetView _view;
        private Piece _piece;

        public MachineRole Role => Spec != null ? Spec.Role : MachineRole.Marker;
        public double VisualSpeed => Speed;
        public double VisualRpm => Math.Abs(Speed);
        public Vector3 BodyPoint => transform.position + Vector3.up * 0.5f;
        public long Creator => _piece != null ? _piece.GetCreator() : 0L;
        public ZNetView View => _view;

        private void Awake()
        {
            _view = GetComponent<ZNetView>();
            _piece = GetComponent<Piece>();
            Spec = MachineCatalog.Get(SpecId);
            BindPorts();
            if (Spec != null)
                Behaviour = MachineBehaviour.Create(this);
            var wear = GetComponent<WearNTear>();
            if (wear != null)
                wear.m_onDestroyed += OnPieceDestroyed;
        }

        public void BindPorts()
        {
            Kinetic.Clear();
            ItemIn.Clear();
            ItemOut.Clear();
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                var n = t.name;
                if (n.StartsWith("Kinetic", StringComparison.Ordinal))
                    Kinetic.Add(t);
                else if (n.StartsWith("Item_In", StringComparison.Ordinal))
                    ItemIn.Add(t);
                else if (n.StartsWith("Item_Out", StringComparison.Ordinal))
                    ItemOut.Add(t);
                else if (n == "Pickup")
                    Pickup = t;
                else if (n == "Dropoff")
                    Dropoff = t;
                else if (n == "Water_Probe")
                    WaterProbe = t;
                else if (n == "Smoke_Out")
                    SmokeOut = t;
                else if (n == "Wind_Probe")
                    WindProbe = t;
                else if (n == "Ground_Probe")
                    GroundProbe = t;
            }

            ItemIn.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            ItemOut.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            Kinetic.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        }

        /// <summary>Adds hammer snap points at every port so pieces line up by their ports.</summary>
        public static void AddSnapPoints(GameObject prefab)
        {
            foreach (var t in prefab.GetComponentsInChildren<Transform>(true))
            {
                var n = t.name;
                if (!n.StartsWith("Kinetic", StringComparison.Ordinal) && !n.StartsWith("Item_", StringComparison.Ordinal))
                    continue;
                var snap = new GameObject("_snappoint_" + n);
                snap.transform.SetParent(prefab.transform, false);
                snap.transform.position = t.position;
                snap.tag = "snappoint";
            }
        }

        private void Update()
        {
            if (_joined)
                return;
            if (_view == null || !_view.IsValid() || _view.GetZDO() == null || Spec == null)
                return;
            _joined = true;
            WorkshopRegistry.Add(this);
        }

        private void OnDestroy()
        {
            if (_joined)
                WorkshopRegistry.Remove(this);
        }

        /// <summary>Real destruction, not unloading. Held items return once, as world drops.</summary>
        private void OnPieceDestroyed()
        {
            if (Behaviour == null || _view == null || !_view.IsValid() || !_view.IsOwner())
                return;
            Behaviour.EnsureLoaded();
            var held = Behaviour.Drain();
            foreach (var stack in held)
                ItemBridge.Drop(stack, transform.position);
            if (held.Count > 0)
                Behaviour.SaveNow();
        }

        public ZDO GetZdo()
        {
            return _view != null && _view.IsValid() ? _view.GetZDO() : null;
        }

        public bool IsLocalOwner()
        {
            return _view != null && _view.IsValid() && _view.IsOwner();
        }

        /// <summary>Observes ownership once per simulation step, and gates saves written by a newer version.</summary>
        public void ObserveOwnership()
        {
            var zdo = GetZdo();
            if (zdo == null)
                return;
            Ownership.Observe(zdo.GetOwner(), IsLocalOwner());
            if (!Ownership.ShouldRehydrate)
                return;
            var schema = zdo.GetInt(SchemaKey, 0);
            switch (SchemaGate.Check(schema))
            {
                case SchemaDecision.PauseNewer:
                    _schemaPaused = true;
                    break;
                case SchemaDecision.Migrate:
                    // 0.1 and 0.2 wrote the same basket and clutch keys this version reads.
                    zdo.Set(SchemaKey, SchemaGate.Current);
                    _schemaPaused = false;
                    break;
                default:
                    _schemaPaused = false;
                    break;
            }

            if (Behaviour != null)
                Behaviour.ForceReload();
        }

        public bool CanActLocally(out string reason)
        {
            reason = "";
            if (!IsLocalOwner())
            {
                reason = "Waiting for ownership.";
                return false;
            }

            if (_schemaPaused)
            {
                reason = "Saved by a newer VikingFactory. Paused.";
                return false;
            }

            if (!Ownership.CanAct)
            {
                reason = "Taking over from the previous owner.";
                return false;
            }

            return true;
        }

        public void Claim()
        {
            if (_view != null && _view.IsValid() && !_view.IsOwner())
            {
                _view.ClaimOwnership();
                if (Behaviour != null)
                    Behaviour.ForceReload();
            }
        }

        public string GetHoverName()
        {
            return Spec != null ? Spec.Name : "Workshop piece";
        }

        public string GetHoverText()
        {
            if (Spec == null)
                return "Workshop piece";
            if (Behaviour != null)
                Behaviour.EnsureLoaded();
            var text = Spec.Name;
            if (Role != MachineRole.Marker && Role != MachineRole.Basket && Role != MachineRole.CoppiceBed && Role != MachineRole.ForageBed
                && Role != MachineRole.WaterIntake && Role != MachineRole.OvenExtension && Role != MachineRole.Trough)
            {
                text += "\n" + Supply + " DU supply / " + Reserved + " DU reserved";
                text += "\n" + (Enabled ? Stop : DisabledReason);
                if (Turning && Math.Abs(Speed) > 0.01)
                    text += "\n" + Math.Abs(Speed).ToString("0") + " RPM, " + (Speed > 0 ? "clockwise" : "counter-clockwise") + " seen from its forward port";
            }
            else if (!Enabled)
                text += "\n" + DisabledReason;

            var status = Behaviour != null ? Behaviour.Status() : "";
            if (!string.IsNullOrEmpty(status))
                text += "\n" + status;
            return text;
        }

        public float GetHoverOffset()
        {
            return 1.5f;
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (Behaviour == null)
                return false;
            if (!hold)
                Claim();
            Behaviour.EnsureLoaded();
            return Behaviour.Interact(user, hold, alt);
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            if (Behaviour == null || item == null)
                return false;
            Claim();
            Behaviour.EnsureLoaded();
            return Behaviour.UseItem(user, item);
        }

        public string GetString(string key, string fallback = "")
        {
            var zdo = GetZdo();
            return zdo == null ? fallback : zdo.GetString(key, fallback);
        }

        public int GetInt(string key, int fallback = 0)
        {
            var zdo = GetZdo();
            return zdo == null ? fallback : zdo.GetInt(key, fallback);
        }

        public float GetFloat(string key, float fallback = 0f)
        {
            var zdo = GetZdo();
            return zdo == null ? fallback : zdo.GetFloat(key, fallback);
        }

        public void Set(string key, string value)
        {
            var zdo = GetZdo();
            if (zdo != null && IsLocalOwner())
                zdo.Set(key, value ?? "");
        }

        public void Set(string key, int value)
        {
            var zdo = GetZdo();
            if (zdo != null && IsLocalOwner())
                zdo.Set(key, value);
        }

        public void Set(string key, float value)
        {
            var zdo = GetZdo();
            if (zdo != null && IsLocalOwner())
                zdo.Set(key, value);
        }

        public static void Message(Humanoid user, string text)
        {
            if (user != null)
                user.Message(MessageHud.MessageType.Center, text);
        }
    }

    /// <summary>
    /// Role logic. State is read from the ZDO whenever the ZDO changed under us, and written back by the owner.
    /// </summary>
    public abstract class MachineBehaviour
    {
        protected readonly WorkshopMachine M;
        private uint _revision = uint.MaxValue;

        protected MachineBehaviour(WorkshopMachine machine)
        {
            M = machine;
        }

        public static MachineBehaviour Create(WorkshopMachine machine)
        {
            switch (machine.Role)
            {
                case MachineRole.Crank: return new CrankBehaviour(machine);
                case MachineRole.Cog: return new CogBehaviour(machine);
                case MachineRole.ReversingCog: return new ReversingCogBehaviour(machine);
                case MachineRole.Clutch: return new ClutchBehaviour(machine);
                case MachineRole.WaterWheel: return new WaterWheelBehaviour(machine);
                case MachineRole.SteamEngine:
                case MachineRole.ReinforcedSteam:
                case MachineRole.EitrMotor:
                    return new EngineBehaviour(machine);
                case MachineRole.SailWheel: return new SailBehaviour(machine);
                case MachineRole.Governor: return new GovernorBehaviour(machine);
                case MachineRole.Flywheel: return new FlywheelBehaviour(machine);
                case MachineRole.WaterIntake: return new WaterIntakeBehaviour(machine);
                case MachineRole.Basket: return new BasketBehaviour(machine);
                case MachineRole.Belt:
                case MachineRole.BeltCorner:
                case MachineRole.IronBelt:
                case MachineRole.Trough:
                    return new BeltBehaviour(machine);
                case MachineRole.Feeder:
                case MachineRole.IronFeeder:
                case MachineRole.CookingTender:
                    return new FeederBehaviour(machine);
                case MachineRole.Splitter: return new SplitterBehaviour(machine);
                case MachineRole.Merger: return new MergerBehaviour(machine);
                case MachineRole.RecipeMill:
                case MachineRole.Assembler:
                    return new MillBehaviour(machine);
                case MachineRole.Quarry: return new QuarryBehaviour(machine);
                case MachineRole.CoppiceBed: return new CoppiceBehaviour(machine);
                case MachineRole.TimberSaw: return new SawBehaviour(machine);
                case MachineRole.Planter:
                case MachineRole.Harvester:
                case MachineRole.FarmGantry:
                    return new FieldBehaviour(machine);
                case MachineRole.ForageBed: return new ForageBehaviour(machine);
                case MachineRole.MiningHead: return new MiningBehaviour(machine);
                case MachineRole.DeepExtractor: return new ExtractorBehaviour(machine);
                case MachineRole.FeedGate: return new FeedGateBehaviour(machine);
                case MachineRole.CullingGate: return new CullingGateBehaviour(machine);
                default: return new PassiveBehaviour(machine);
            }
        }

        public void EnsureLoaded()
        {
            var zdo = M.GetZdo();
            if (zdo == null || zdo.DataRevision == _revision)
                return;
            Load();
            _revision = zdo.DataRevision;
        }

        public void ForceReload()
        {
            _revision = uint.MaxValue;
            EnsureLoaded();
        }

        /// <summary>Writes state to the ZDO. Only the owner writes.</summary>
        public void SaveNow()
        {
            if (!M.IsLocalOwner())
                return;
            Save();
            var zdo = M.GetZdo();
            if (zdo != null)
                _revision = zdo.DataRevision;
        }

        protected virtual void Load() { }
        protected virtual void Save() { }

        /// <summary>Kinetic node. Transmissions by default; roles override.</summary>
        public virtual void AddNode(KineticNetwork network, string id)
        {
            var load = MachineCatalog.LoadDu(M.Role);
            if (MachineCatalog.IsConsumer(M.Role))
                network.AddConsumer(id, load, MaxRpm);
            else
                network.AddTransmission(id);
        }

        protected virtual int MaxRpm => int.MaxValue;

        /// <summary>Speed multiplier at a port relative to the machine's own speed.</summary>
        public virtual double PortMultiplier(Transform port) { return 1.0; }

        /// <summary>Before the solve: offer drive, spend nothing yet except a paid fuel start.</summary>
        public virtual void BeforeSolve(KineticNetwork network, string id) { }

        /// <summary>After the solve, on every peer. Owners also get Tick.</summary>
        public virtual void AfterSolve(KineticNetwork network, string id, float dt) { }

        /// <summary>Owner-only work. Called once per fixed step when CanActLocally.</summary>
        public virtual void Tick(float dt) { }

        public virtual string Status() { return ""; }
        public virtual bool Interact(Humanoid user, bool hold, bool alt) { return false; }
        public virtual bool UseItem(Humanoid user, ItemDrop.ItemData item) { return false; }
        public virtual IItemEndpoint InputAt(Transform port) { return null; }
        public virtual IItemEndpoint OutputAt(Transform port) { return null; }
        public virtual IItemEndpoint Body => null;

        /// <summary>Everything held, removed from the machine. Called once on destruction.</summary>
        public virtual List<ItemStack> Drain() { return new List<ItemStack>(); }

        /// <summary>A server preset or boss gate can switch a machine off. Empty means allowed.</summary>
        public virtual string FeatureBlock() { return ""; }

        protected static string Describe(IEnumerable<ItemStack> stacks)
        {
            var parts = new List<string>();
            foreach (var stack in stacks)
                parts.Add(stack.Prefab + " ×" + stack.Count);
            return parts.Count == 0 ? "empty" : string.Join(", ", parts.ToArray());
        }

        /// <summary>Takes one item from the player's hand-selected stack for commissioning or fuel.</summary>
        protected static bool TakeFromPlayer(Humanoid user, ItemDrop.ItemData item, int count)
        {
            var inventory = user != null ? user.GetInventory() : null;
            return inventory != null && inventory.RemoveItem(item, count);
        }
    }

    public static class WorkshopRegistry
    {
        private static readonly List<WorkshopMachine> Machines = new List<WorkshopMachine>();

        public static int Version { get; private set; }

        public static IReadOnlyList<WorkshopMachine> All => Machines;

        public static void Add(WorkshopMachine machine)
        {
            if (Machines.Contains(machine))
                return;
            Machines.Add(machine);
            Version++;
        }

        public static void Remove(WorkshopMachine machine)
        {
            if (Machines.Remove(machine))
                Version++;
        }
    }
}
