using System;
using System.Collections.Generic;
using UnityEngine;
using VikingFactory.Core.Items;

namespace VikingFactory.Machines
{
    /// <summary>
    /// A place one item can go into or come out of. Insert returns true only when the destination
    /// confirmed it holds the item. Remove returns true only when the source no longer holds it.
    /// </summary>
    public interface IItemEndpoint
    {
        string Label { get; }
        bool Available(out string reason);
        bool CanInsert(ItemStack one);
        bool TryInsert(ItemStack one);
        ItemStack Peek(Func<ItemStack, bool> filter);
        bool TryRemove(ItemStack one);
        int Count(string prefab);
    }

    public abstract class EndpointBase : IItemEndpoint
    {
        public abstract string Label { get; }

        public virtual bool Available(out string reason)
        {
            reason = "";
            return true;
        }

        public virtual bool CanInsert(ItemStack one) { return false; }
        public virtual bool TryInsert(ItemStack one) { return false; }
        public virtual ItemStack Peek(Func<ItemStack, bool> filter) { return null; }
        public virtual bool TryRemove(ItemStack one) { return false; }
        public virtual int Count(string prefab) { return -1; }

        protected static bool Owned(ZNetView view, out string reason)
        {
            reason = "";
            if (view == null || !view.IsValid())
            {
                reason = "Target is not loaded.";
                return false;
            }

            if (!view.IsOwner())
            {
                reason = "Waiting for ownership.";
                return false;
            }

            return true;
        }

        protected static string CleanName(GameObject go)
        {
            return go == null ? "" : go.name.Replace("(Clone)", "").Trim();
        }
    }

    /// <summary>A machine's own StackStore, persisted by the machine.</summary>
    public sealed class StoreEndpoint : EndpointBase
    {
        private readonly WorkshopMachine _machine;
        private readonly Func<StackStore> _store;
        private readonly Action _save;
        private readonly Func<ItemStack, bool> _accept;
        private readonly bool _allowRemove;
        private readonly string _label;

        public StoreEndpoint(WorkshopMachine machine, string label, Func<StackStore> store, Action save, Func<ItemStack, bool> accept = null, bool allowRemove = true)
        {
            _machine = machine;
            _label = label;
            _store = store;
            _save = save;
            _accept = accept;
            _allowRemove = allowRemove;
        }

        public override string Label => _label;

        public override bool Available(out string reason)
        {
            return _machine.CanActLocally(out reason);
        }

        public override bool CanInsert(ItemStack one)
        {
            return (_accept == null || _accept(one)) && _store().CanAccept(one);
        }

        public override bool TryInsert(ItemStack one)
        {
            if (!CanInsert(one) || !_store().TryAdd(one))
                return false;
            _save();
            return true;
        }

        public override ItemStack Peek(Func<ItemStack, bool> filter)
        {
            if (!_allowRemove)
                return null;
            var stack = _store().Peek(filter);
            return stack == null ? null : stack.Copy(1);
        }

        public override bool TryRemove(ItemStack one)
        {
            if (!_allowRemove)
                return false;
            var taken = _store().TakeOne(s => s.SameIdentity(one));
            if (taken == null)
                return false;
            _save();
            return true;
        }

        public override int Count(string prefab)
        {
            return _store().CountOf(prefab);
        }
    }

    /// <summary>A delegate-backed endpoint for machine ports whose logic lives in a behaviour.</summary>
    public sealed class DelegateEndpoint : EndpointBase
    {
        public string Name = "";
        public WorkshopMachine Machine;
        public Func<ItemStack, bool> CanInsertFn;
        public Func<ItemStack, bool> InsertFn;
        public Func<Func<ItemStack, bool>, ItemStack> PeekFn;
        public Func<ItemStack, bool> RemoveFn;
        public Func<string, int> CountFn;

        public override string Label => Name;

        public override bool Available(out string reason)
        {
            return Machine.CanActLocally(out reason);
        }

        public override bool CanInsert(ItemStack one) { return CanInsertFn != null && CanInsertFn(one); }
        public override bool TryInsert(ItemStack one) { return InsertFn != null && CanInsert(one) && InsertFn(one); }
        public override ItemStack Peek(Func<ItemStack, bool> filter) { return PeekFn == null ? null : PeekFn(filter); }
        public override bool TryRemove(ItemStack one) { return RemoveFn != null && RemoveFn(one); }
        public override int Count(string prefab) { return CountFn == null ? -1 : CountFn(prefab); }
    }

    /// <summary>
    /// A vanilla chest the feeder is physically docked to. Tombstones are excluded. A private chest
    /// is used only by a machine built by the chest's owner. A ward that denies access pauses the dock.
    /// </summary>
    public sealed class ContainerEndpoint : EndpointBase
    {
        private readonly Container _container;
        private readonly long _machineCreator;

        public ContainerEndpoint(Container container, long machineCreator)
        {
            _container = container;
            _machineCreator = machineCreator;
        }

        public override string Label => _container != null ? _container.m_name : "chest";

        public static bool Eligible(Container container)
        {
            return container != null && container.GetComponent<TombStone>() == null && container.GetInventory() != null;
        }

        public override bool Available(out string reason)
        {
            reason = "";
            if (!Eligible(_container))
            {
                reason = "Not a workshop chest.";
                return false;
            }

            if (!_container.IsOwner())
            {
                reason = "Waiting for chest ownership.";
                return false;
            }

            if (_container.m_privacy == Container.PrivacySetting.Private)
            {
                var piece = _container.GetComponent<Piece>();
                if (piece == null || piece.GetCreator() != _machineCreator)
                {
                    reason = "Private chest belongs to someone else.";
                    return false;
                }
            }

            if (_container.m_checkGuardStone && !PrivateArea.CheckAccess(_container.transform.position, 0f, false))
            {
                reason = "A ward denies access to this chest.";
                return false;
            }

            return true;
        }

        public override bool CanInsert(ItemStack one)
        {
            return ItemBridge.CanAdd(_container.GetInventory(), one);
        }

        public override bool TryInsert(ItemStack one)
        {
            return ItemBridge.Add(_container.GetInventory(), one);
        }

        public override ItemStack Peek(Func<ItemStack, bool> filter)
        {
            var items = _container.GetInventory().GetAllItems();
            for (var i = 0; i < items.Count; i++)
            {
                var stack = ItemBridge.FromItemData(items[i], 1);
                if (stack.Prefab.Length == 0)
                    continue;
                if (filter == null || filter(stack))
                    return stack;
            }

            return null;
        }

        public override bool TryRemove(ItemStack one)
        {
            return ItemBridge.RemoveOne(_container.GetInventory(), one);
        }

        public override int Count(string prefab)
        {
            var total = 0;
            foreach (var item in _container.GetInventory().GetAllItems())
            {
                if (ItemBridge.PrefabName(item) == prefab)
                    total += item.m_stack;
            }

            return total;
        }
    }

    /// <summary>
    /// Items a native station has already spawned at its own output point. Only the station's product
    /// prefabs within the radius are taken, never a player's dropped items elsewhere. An item is removed
    /// only while this peer owns it, so it cannot be taken twice.
    /// </summary>
    public class DropEndpoint : EndpointBase
    {
        private static int _itemMask = -1;
        private readonly Func<Vector3> _center;
        private readonly float _radius;
        private readonly HashSet<string> _prefabs;
        private readonly string _label;
        private ItemDrop _peeked;

        public DropEndpoint(string label, Func<Vector3> center, float radius, HashSet<string> prefabs)
        {
            _label = label;
            _center = center;
            _radius = radius;
            _prefabs = prefabs;
        }

        public override string Label => _label;

        public override ItemStack Peek(Func<ItemStack, bool> filter)
        {
            _peeked = null;
            if (_itemMask < 0)
                _itemMask = LayerMask.GetMask("item");
            var hits = Physics.OverlapSphere(_center(), _radius, _itemMask);
            for (var i = 0; i < hits.Length; i++)
            {
                var drop = hits[i].GetComponentInParent<ItemDrop>();
                if (drop == null || drop.m_itemData == null)
                    continue;
                var name = CleanName(drop.gameObject);
                if (_prefabs != null && !_prefabs.Contains(name))
                    continue;
                if (!drop.CanPickup(false))
                {
                    drop.RequestOwn();
                    continue;
                }

                drop.Load();
                var stack = ItemBridge.FromItemData(drop.m_itemData, 1);
                if (stack.Prefab.Length == 0)
                    stack.Prefab = name;
                if (filter != null && !filter(stack))
                    continue;
                _peeked = drop;
                return stack;
            }

            return null;
        }

        public override bool TryRemove(ItemStack one)
        {
            if (_peeked == null || !_peeked.CanPickup(false))
                return false;
            _peeked.Load();
            if (!ItemBridge.Matches(one, _peeked.m_itemData))
                return false;
            var removed = _peeked.RemoveOne();
            _peeked = null;
            return removed;
        }
    }

    /// <summary>
    /// Smelter-family adapter: kiln, smelter, blast furnace, windmill, spinning wheel, eitr refinery.
    /// Fuel and input are read from the station's own definitions. Insert is only attempted while this
    /// peer owns the station, and it counts only when the station's queue or fuel actually rose.
    /// </summary>
    public sealed class SmelterEndpoint : DropEndpoint
    {
        public static readonly HashSet<string> Reviewed = new HashSet<string> { "smelter", "charcoal_kiln", "blastfurnace", "windmill", "piece_spinningwheel", "eitrrefinery" };

        private readonly Smelter _smelter;
        private readonly ZNetView _view;

        public SmelterEndpoint(Smelter smelter)
            : base(smelter.m_name, () => smelter.m_outputPoint != null ? smelter.m_outputPoint.position : smelter.transform.position, 1.2f, Outputs(smelter))
        {
            _smelter = smelter;
            _view = smelter.GetComponent<ZNetView>();
        }

        private static HashSet<string> Outputs(Smelter smelter)
        {
            var set = new HashSet<string>();
            foreach (var conversion in smelter.m_conversion)
            {
                if (conversion.m_to != null)
                    set.Add(conversion.m_to.gameObject.name);
            }

            return set;
        }

        public override bool Available(out string reason)
        {
            if (!Reviewed.Contains(CleanName(_smelter.gameObject)))
            {
                reason = "Unsupported adapter for " + CleanName(_smelter.gameObject) + ".";
                return false;
            }

            return Owned(_view, out reason);
        }

        private bool IsFuel(ItemStack one)
        {
            return _smelter.m_fuelItem != null && _smelter.m_maxFuel > 0 && _smelter.m_fuelItem.gameObject.name == one.Prefab;
        }

        private bool IsInput(ItemStack one)
        {
            foreach (var conversion in _smelter.m_conversion)
            {
                if (conversion.m_from != null && conversion.m_from.gameObject.name == one.Prefab)
                    return true;
            }

            return false;
        }

        private float Fuel => _view.GetZDO().GetFloat(ZDOVars.s_fuel);
        private int Queue => _view.GetZDO().GetInt(ZDOVars.s_queued);

        public override bool CanInsert(ItemStack one)
        {
            if (!Available(out _))
                return false;
            if (IsFuel(one))
                return Fuel <= _smelter.m_maxFuel - 1;
            return IsInput(one) && Queue < _smelter.m_maxOre;
        }

        public override bool TryInsert(ItemStack one)
        {
            if (!CanInsert(one))
                return false;
            if (IsFuel(one))
            {
                var before = Fuel;
                _view.InvokeRPC("RPC_AddFuel");
                return Fuel > before;
            }

            var queued = Queue;
            _view.InvokeRPC("RPC_AddOre", one.Prefab, one.Cheated);
            return Queue > queued;
        }

        public override ItemStack Peek(Func<ItemStack, bool> filter)
        {
            if (!Available(out _))
                return null;
            var found = base.Peek(filter);
            if (found == null && _view.GetZDO().GetInt(ZDOVars.s_spawnAmount) > 0)
                _view.InvokeRPC("RPC_EmptyProcessed");
            return found;
        }
    }

    /// <summary>
    /// Cooking rack, iron rack, and oven. Raw food goes in only with fire or fuel and a free slot, the same
    /// checks the player gets. Done or burnt food is removed through the station's own RPC, then collected.
    /// A stalled tender leaves food on the heat, so it can burn.
    /// </summary>
    public sealed class CookingEndpoint : DropEndpoint
    {
        public static readonly HashSet<string> Reviewed = new HashSet<string> { "piece_cookingstation", "piece_cookingstation_iron", "piece_oven" };

        private readonly CookingStation _station;
        private readonly ZNetView _view;
        private readonly Func<Vector3> _collector;

        public CookingEndpoint(CookingStation station, Func<Vector3> collector)
            : base(station.m_name, () => station.transform.position, 2.5f, Outputs(station))
        {
            _station = station;
            _view = station.GetComponent<ZNetView>();
            _collector = collector;
        }

        private static HashSet<string> Outputs(CookingStation station)
        {
            var set = new HashSet<string>();
            foreach (var conversion in station.m_conversion)
            {
                if (conversion.m_to != null)
                    set.Add(conversion.m_to.gameObject.name);
            }

            if (station.m_overCookedItem != null)
                set.Add(station.m_overCookedItem.gameObject.name);
            return set;
        }

        public string CookedFrom(string raw)
        {
            foreach (var conversion in _station.m_conversion)
            {
                if (conversion.m_from != null && conversion.m_from.gameObject.name == raw && conversion.m_to != null)
                    return conversion.m_to.gameObject.name;
            }

            return "";
        }

        public override bool Available(out string reason)
        {
            if (!Reviewed.Contains(CleanName(_station.gameObject)))
            {
                reason = "Unsupported adapter for " + CleanName(_station.gameObject) + ".";
                return false;
            }

            return Owned(_view, out reason);
        }

        private int Filled()
        {
            var filled = 0;
            for (var i = 0; i < _station.m_slots.Length; i++)
            {
                if (_view.GetZDO().GetString("slot" + i) != "")
                    filled++;
            }

            return filled;
        }

        private bool HasFire()
        {
            if (!_station.m_requireFire)
                return !_station.m_useFuel || _view.GetZDO().GetFloat(ZDOVars.s_fuel) > 0f;
            var points = _station.m_fireCheckPoints;
            if (points != null && points.Length > 0)
            {
                foreach (var point in points)
                {
                    if (!EffectArea.IsPointInsideArea(point.position, EffectArea.Type.Burning, _station.m_fireCheckRadius))
                        return false;
                }

                return true;
            }

            return EffectArea.IsPointInsideArea(_station.transform.position, EffectArea.Type.Burning, _station.m_fireCheckRadius);
        }

        private bool IsFuel(ItemStack one)
        {
            return _station.m_useFuel && _station.m_fuelItem != null && _station.m_fuelItem.gameObject.name == one.Prefab;
        }

        public override bool CanInsert(ItemStack one)
        {
            if (!Available(out _))
                return false;
            if (IsFuel(one))
                return _view.GetZDO().GetFloat(ZDOVars.s_fuel) <= _station.m_maxFuel - 1;
            if (CookedFrom(one.Prefab).Length == 0)
                return false;
            return Filled() < _station.m_slots.Length && HasFire();
        }

        public override bool TryInsert(ItemStack one)
        {
            if (!CanInsert(one))
                return false;
            if (IsFuel(one))
            {
                var before = _view.GetZDO().GetFloat(ZDOVars.s_fuel);
                _view.InvokeRPC("RPC_AddFuel");
                return _view.GetZDO().GetFloat(ZDOVars.s_fuel) > before;
            }

            var filled = Filled();
            _view.InvokeRPC("RPC_AddItem", one.Prefab, one.Cheated);
            return Filled() > filled;
        }

        public override ItemStack Peek(Func<ItemStack, bool> filter)
        {
            if (!Available(out _))
                return null;
            var found = base.Peek(filter);
            if (found != null)
                return found;
            for (var i = 0; i < _station.m_slots.Length; i++)
            {
                if (_view.GetZDO().GetString("slot" + i) == "")
                    continue;
                var status = _view.GetZDO().GetInt("slotstatus" + i);
                if (status == 0)
                    continue;
                // 1 is done, 2 is burnt. The station spawns the item itself, once.
                _view.InvokeRPC("RPC_RemoveDoneItem", _collector(), 1);
                break;
            }

            return null;
        }
    }

    /// <summary>Fermenter: loads one mead base when empty, taps when the native timer says ready.</summary>
    public sealed class FermenterEndpoint : DropEndpoint
    {
        private readonly Fermenter _fermenter;
        private readonly ZNetView _view;

        public FermenterEndpoint(Fermenter fermenter)
            : base(fermenter.m_name, () => fermenter.m_outputPoint != null ? fermenter.m_outputPoint.position : fermenter.transform.position, 1.5f, Outputs(fermenter))
        {
            _fermenter = fermenter;
            _view = fermenter.GetComponent<ZNetView>();
        }

        private static HashSet<string> Outputs(Fermenter fermenter)
        {
            var set = new HashSet<string>();
            foreach (var conversion in fermenter.m_conversion)
            {
                if (conversion.m_to != null)
                    set.Add(conversion.m_to.gameObject.name);
            }

            return set;
        }

        public override bool Available(out string reason)
        {
            return Owned(_view, out reason);
        }

        public override bool CanInsert(ItemStack one)
        {
            if (!Available(out _) || _view.GetZDO().GetInt(ZDOVars.s_content) != 0)
                return false;
            foreach (var conversion in _fermenter.m_conversion)
            {
                if (conversion.m_from != null && conversion.m_from.gameObject.name == one.Prefab)
                    return true;
            }

            return false;
        }

        public override bool TryInsert(ItemStack one)
        {
            if (!CanInsert(one))
                return false;
            _view.InvokeRPC("RPC_AddItem", one.Prefab.GetStableHashCode(), one.Cheated);
            return _view.GetZDO().GetInt(ZDOVars.s_content) != 0;
        }

        public override ItemStack Peek(Func<ItemStack, bool> filter)
        {
            if (!Available(out _))
                return null;
            var found = base.Peek(filter);
            if (found != null)
                return found;
            var zdo = _view.GetZDO();
            var start = zdo.GetLong(ZDOVars.s_startTime, 0L);
            if (zdo.GetInt(ZDOVars.s_content) != 0 && start != 0L
                && (ZNet.instance.GetTime() - new DateTime(start)).TotalSeconds > _fermenter.m_fermentationDuration)
                _view.InvokeRPC("RPC_Tap");
            return null;
        }
    }

    /// <summary>Beehive and bird nest: extract only what the native hive already made.</summary>
    public sealed class HiveEndpoint : DropEndpoint
    {
        private readonly ZNetView _view;

        public HiveEndpoint(Beehive hive)
            : base(hive.m_name, () => hive.m_spawnPoint != null ? hive.m_spawnPoint.position : hive.transform.position, 1.2f,
                new HashSet<string> { hive.m_honeyItem != null ? hive.m_honeyItem.gameObject.name : "" })
        {
            _view = hive.GetComponent<ZNetView>();
        }

        public override bool Available(out string reason)
        {
            return Owned(_view, out reason);
        }

        public override ItemStack Peek(Func<ItemStack, bool> filter)
        {
            if (!Available(out _))
                return null;
            var found = base.Peek(filter);
            if (found == null && _view.GetZDO().GetInt(ZDOVars.s_level) > 0)
                _view.InvokeRPC("RPC_Extract");
            return found;
        }
    }

    /// <summary>Sap collector on a real root. Regeneration and depletion stay native.</summary>
    public sealed class SapEndpoint : DropEndpoint
    {
        private readonly ZNetView _view;

        public SapEndpoint(SapCollector sap)
            : base(sap.m_name, () => sap.m_spawnPoint != null ? sap.m_spawnPoint.position : sap.transform.position, 1.2f,
                new HashSet<string> { sap.m_spawnItem != null ? sap.m_spawnItem.gameObject.name : "" })
        {
            _view = sap.GetComponent<ZNetView>();
        }

        public override bool Available(out string reason)
        {
            return Owned(_view, out reason);
        }

        public override ItemStack Peek(Func<ItemStack, bool> filter)
        {
            if (!Available(out _))
                return null;
            var found = base.Peek(filter);
            if (found == null && _view.GetZDO() != null && _view.GetZDO().GetInt(ZDOVars.s_level) > 0)
                _view.InvokeRPC("RPC_Extract");
            return found;
        }
    }

    /// <summary>Finds what sits at a port: another machine's port, a docked chest, or a reviewed native station.</summary>
    public static class EndpointFinder
    {
        public const float PortReach = 0.6f;
        public const float DockReach = 1.25f;
        public const float StationReach = 2.0f;

        public static IItemEndpoint FindInsert(WorkshopMachine self, Vector3 position, float stationReach = StationReach)
        {
            var machine = MachinePort(self, position, true);
            if (machine != null)
                return machine;
            var chest = Chest(self, position);
            if (chest != null)
                return chest;
            return Station(self, position, true, stationReach);
        }

        public static IItemEndpoint FindExtract(WorkshopMachine self, Vector3 position, float stationReach = StationReach)
        {
            var machine = MachinePort(self, position, false);
            if (machine != null)
                return machine;
            var chest = Chest(self, position);
            if (chest != null)
                return chest;
            return Station(self, position, false, stationReach);
        }

        public static IItemEndpoint MachinePort(WorkshopMachine self, Vector3 position, bool insert)
        {
            var machines = WorkshopRegistry.All;
            IItemEndpoint best = null;
            var bestDistance = PortReach;
            for (var i = 0; i < machines.Count; i++)
            {
                var other = machines[i];
                if (other == self || other.Behaviour == null)
                    continue;
                var ports = insert ? other.ItemIn : other.ItemOut;
                for (var p = 0; p < ports.Count; p++)
                {
                    var distance = Vector3.Distance(ports[p].position, position);
                    if (distance >= bestDistance)
                        continue;
                    var endpoint = insert ? other.Behaviour.InputAt(ports[p]) : other.Behaviour.OutputAt(ports[p]);
                    if (endpoint == null)
                        continue;
                    best = endpoint;
                    bestDistance = distance;
                }

                var body = other.Behaviour.Body;
                if (body != null)
                {
                    var distance = Vector3.Distance(other.BodyPoint, position);
                    if (distance < Mathf.Max(bestDistance, 0.9f) && (best == null || distance < bestDistance))
                    {
                        best = body;
                        bestDistance = distance;
                    }
                }
            }

            return best;
        }

        private static IItemEndpoint Chest(WorkshopMachine self, Vector3 position)
        {
            var hits = Physics.OverlapSphere(position, DockReach);
            Container best = null;
            var bestDistance = DockReach;
            for (var i = 0; i < hits.Length; i++)
            {
                var container = hits[i].GetComponentInParent<Container>();
                if (!ContainerEndpoint.Eligible(container))
                    continue;
                var distance = Vector3.Distance(position, container.transform.position);
                if (distance < bestDistance)
                {
                    best = container;
                    bestDistance = distance;
                }
            }

            return best == null ? null : new ContainerEndpoint(best, self.Creator);
        }

        private static IItemEndpoint Station(WorkshopMachine self, Vector3 position, bool insert, float reach)
        {
            var hits = Physics.OverlapSphere(position, reach);
            for (var i = 0; i < hits.Length; i++)
            {
                var smelter = hits[i].GetComponentInParent<Smelter>();
                if (smelter != null)
                    return new SmelterEndpoint(smelter);
                var cooking = hits[i].GetComponentInParent<CookingStation>();
                if (cooking != null)
                {
                    var from = position;
                    return new CookingEndpoint(cooking, () => from);
                }

                var fermenter = hits[i].GetComponentInParent<Fermenter>();
                if (fermenter != null)
                    return new FermenterEndpoint(fermenter);
                if (insert)
                    continue;
                var hive = hits[i].GetComponentInParent<Beehive>();
                if (hive != null)
                    return new HiveEndpoint(hive);
                var sap = hits[i].GetComponentInParent<SapCollector>();
                if (sap != null)
                    return new SapEndpoint(sap);
            }

            return null;
        }
    }
}
