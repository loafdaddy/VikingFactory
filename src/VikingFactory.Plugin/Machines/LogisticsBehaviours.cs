using System;
using System.Collections.Generic;
using UnityEngine;
using VikingFactory.Core;
using VikingFactory.Core.Items;
using VikingFactory.Core.Logistics;

namespace VikingFactory.Machines
{
    /// <summary>Catch basket: eight stacks on ZDO key vf_items, the same key 0.1 and 0.2 used.</summary>
    public sealed class BasketBehaviour : MachineBehaviour
    {
        public const int SlotLimit = 8;
        private StackStore _store = new StackStore(SlotLimit, 50);

        public BasketBehaviour(WorkshopMachine machine) : base(machine) { }

        protected override void Load() { _store = new StackStore(SlotLimit, 50, ItemCodec.Parse(M.GetString("vf_items"))); }
        protected override void Save() { M.Set("vf_items", ItemCodec.Format(_store.Stacks)); }

        public override IItemEndpoint Body => new StoreEndpoint(M, "basket", () => _store, SaveNow);
        public override List<ItemStack> Drain() { return _store.TakeAll(); }

        public override bool Interact(Humanoid user, bool hold, bool alt)
        {
            // Hand one stack back to the player. The basket is not a chest UI.
            if (hold || user == null)
                return false;
            var stack = _store.Peek();
            if (stack == null)
                return false;
            var inventory = user.GetInventory();
            var one = stack.Copy(stack.Count);
            if (!ItemBridge.CanAdd(inventory, one))
            {
                WorkshopMachine.Message(user, "Your inventory is full.");
                return true;
            }

            for (var i = 0; i < one.Count; i++)
                _store.TakeOne(s => s.SameIdentity(one));
            SaveNow();
            ItemBridge.Add(inventory, one);
            return true;
        }

        public override string Status()
        {
            return "Holds: " + Describe(_store.Stacks) + "\nInteract to take a stack.";
        }
    }

    /// <summary>
    /// Timber belt, corner, iron belt, and gravity trough. The logical queue owns the items. The front item
    /// moves into whatever sits at the output port: another belt, a splitter, a machine input, or a docked chest.
    /// </summary>
    public sealed class BeltBehaviour : MachineBehaviour
    {
        private ConveyorQueue _queue;

        public BeltBehaviour(WorkshopMachine machine) : base(machine)
        {
            _queue = NewQueue();
        }

        private bool IsTrough => M.Role == MachineRole.Trough;

        private ConveyorQueue NewQueue()
        {
            var rate = M.Role == MachineRole.IronBelt ? BalanceDefaults.IronBeltItemsPerMinute
                : IsTrough ? BalanceDefaults.GravityTroughItemsPerMinute : BalanceDefaults.TimberBeltItemsPerMinute;
            return new ConveyorQueue(4, 60.0 / rate, rate);
        }

        protected override void Load()
        {
            _queue = NewQueue();
            _queue.Load(M.GetString("vf_queue"));
        }

        protected override void Save() { M.Set("vf_queue", _queue.Save()); }

        public override void AddNode(Core.Kinetics.KineticNetwork network, string id)
        {
            if (IsTrough)
                network.AddTransmission(id);
            else
                base.AddNode(network, id);
        }

        public bool Downhill
        {
            get
            {
                if (M.ItemIn.Count == 0 || M.ItemOut.Count == 0)
                    return false;
                return M.ItemOut[0].position.y < M.ItemIn[0].position.y - 0.1f;
            }
        }

        private bool Moving => IsTrough ? Downhill : M.Producing;

        public override void Tick(float dt)
        {
            var speed = IsTrough ? 1.0 : Math.Max(0.25, Math.Min(1.0, M.UsefulRpm / BalanceDefaults.MilestoneRpm));
            _queue.Advance(dt, Moving, speed);
            var front = _queue.PeekReady();
            if (front == null || M.ItemOut.Count == 0)
                return;
            var next = EndpointFinder.MachinePort(M, M.ItemOut[0].position, true) ?? ChestAt(M.ItemOut[0].position);
            if (next == null || !next.Available(out _) || !next.CanInsert(front))
                return;
            // Receiver first, then this queue, in the same frame; then save at once so an unload cannot replay it.
            if (!next.TryInsert(front))
                return;
            _queue.TakeReady();
            SaveNow();
        }

        private IItemEndpoint ChestAt(Vector3 position)
        {
            var endpoint = EndpointFinder.FindInsert(M, position, 0f);
            return endpoint is ContainerEndpoint ? endpoint : null;
        }

        public override IItemEndpoint InputAt(Transform port)
        {
            return new DelegateEndpoint
            {
                Name = M.Spec.Name,
                Machine = M,
                CanInsertFn = one => one.Count == 1 && _queue.CanAccept() && (IsTrough || M.Producing),
                InsertFn = one =>
                {
                    if (!_queue.TryAdmit(one.Copy(1)))
                        return false;
                    SaveNow();
                    return true;
                }
            };
        }

 
        /// <summary>A feeder may take the ready front item from the belt's end.</summary>
        public override IItemEndpoint OutputAt(Transform port)
        {
            return new DelegateEndpoint
            {
                Name = M.Spec.Name,
                Machine = M,
                PeekFn = filter =>
                {
                    var front = _queue.PeekReady();
                    return front != null && (filter == null || filter(front)) ? front.Copy(1) : null;
                },
                RemoveFn = one =>
                {
                    var front = _queue.PeekReady();
                    if (front == null || !front.SameIdentity(one))
                        return false;
                    _queue.TakeReady();
                    SaveNow();
                    return true;
                }
            };
        }

        public override List<ItemStack> Drain() { return _queue.TakeAll(); }

        public override string Status()
        {
            var text = "Carrying " + _queue.Count + "/" + _queue.Capacity + ": " + Describe(_queue.Items);
            if (IsTrough && !Downhill)
                text += "\nLevel or uphill. A trough only moves items downhill.";
            return text;
        }
    }

    /// <summary>
    /// Feeders and the cooking tender. One item per swing from the back port to the front port, only while the
    /// line turns. An item is taken from the source only after the destination said it has room. If the
    /// destination then refuses, the arm keeps holding it and retries; it is never discarded or duplicated.
    /// </summary>
    public sealed class FeederBehaviour : MachineBehaviour
    {
        private ItemStack _hand;
        private ItemFilter _filter = new ItemFilter("");
        private int _targetIndex;
        private StockTarget _target = new StockTarget(0);
        private float _wait;
        private int _moved;
        private string _reason = "";
        private static readonly int[] Targets = { 0, 10, 50, 100, 250 };

        public FeederBehaviour(WorkshopMachine machine) : base(machine) { }

        private bool IsTender => M.Role == MachineRole.CookingTender;

        protected override void Load()
        {
            _hand = ItemCodec.ParseOne(M.GetString("vf_hand"));
            _filter = ItemFilter.Load(M.GetString("vf_filter"));
            _targetIndex = Mathf.Clamp(M.GetInt("vf_target"), 0, Targets.Length - 1);
            _target = new StockTarget(Targets[_targetIndex]);
            _moved = M.GetInt("vf_moved");
        }

        protected override void Save()
        {
            M.Set("vf_hand", _hand == null ? "" : ItemCodec.FormatOne(_hand));
            M.Set("vf_filter", _filter.Save());
            M.Set("vf_target", _targetIndex);
            M.Set("vf_moved", _moved);
        }

        private double ItemsPerMinute => M.Role == MachineRole.IronFeeder ? BalanceDefaults.IronFeederItemsPerMinute
            : IsTender ? 60.0 / BalanceDefaults.TenderSecondsPerAction : BalanceDefaults.BronzeFeederItemsPerMinute;

        protected override int MaxRpm => M.Role == MachineRole.IronFeeder ? 64 : 32;

        private float Reach
        {
            get
            {
                if (!IsTender)
                    return EndpointFinder.StationReach;
                foreach (var other in WorkshopRegistry.All)
                {
                    if (other.Role == MachineRole.OvenExtension && Vector3.Distance(other.transform.position, M.transform.position) < 3f)
                        return EndpointFinder.StationReach + 2.5f;
                }

                return EndpointFinder.StationReach;
            }
        }

        private Vector3 SourcePoint => (M.Pickup != null ? M.Pickup : M.transform).position;
        private Vector3 DestinationPoint => (M.Dropoff != null ? M.Dropoff : M.transform).position;

        public override void Tick(float dt)
        {
            if (!M.Producing)
            {
                _reason = "";
                return;
            }

            _wait -= dt;
            if (_wait > 0f)
                return;
            _wait = (float)BalanceDefaults.SecondsPerItem(ItemsPerMinute, M.UsefulRpm);

            if (IsTender)
            {
                TenderCycle();
                return;
            }

            var source = EndpointFinder.FindExtract(M, SourcePoint, Reach);
            var destination = EndpointFinder.FindInsert(M, DestinationPoint, Reach);
            Move(source, destination, null);
        }

        /// <summary>
        /// The tender stands between storage (back) and a rack or oven (front). It first unloads finished food to
        /// storage, then loads raw food only when storage has room for what it will become.
        /// </summary>
        private void TenderCycle()
        {
            var storage = EndpointFinder.FindExtract(M, SourcePoint, 0f);
            var station = EndpointFinder.FindInsert(M, DestinationPoint, Reach) as CookingEndpoint;
            if (station == null)
            {
                _reason = "No reviewed rack or oven in reach of the front.";
                return;
            }

            if (storage == null)
            {
                _reason = "No storage at the back.";
                return;
            }

            if (_hand == null)
            {
                var done = station.Peek(null);
                if (done != null && storage.CanInsert(done))
                {
                    Move(station, storage, null);
                    return;
                }
            }

            Move(storage, station, raw =>
            {
                var cooked = station.CookedFrom(raw.Prefab);
                if (cooked.Length == 0)
                    return true;
                return storage.CanInsert(new ItemStack { Prefab = cooked, Count = 1 });
            });
        }

        private void Move(IItemEndpoint source, IItemEndpoint destination, Func<ItemStack, bool> interlock)
        {
            if (destination == null)
            {
                _reason = "Nothing at the front port.";
                return;
            }

            if (!destination.Available(out var why))
            {
                _reason = why;
                return;
            }

            if (_hand != null)
            {
                if (destination.CanInsert(_hand) && destination.TryInsert(_hand))
                {
                    _hand = null;
                    _moved++;
                    _reason = "Delivered the held item.";
                }
                else
                    _reason = "Holding " + _hand.Prefab + ". The front refused it.";
                SaveNow();
                return;
            }

            if (source == null)
            {
                _reason = "Nothing at the back port.";
                return;
            }

            if (!source.Available(out why))
            {
                _reason = why;
                return;
            }

            if (_target.Enabled)
            {
                var prefab = _filter.IsEmpty ? null : _filter.Prefab;
                if (prefab != null)
                {
                    var stock = destination.Count(prefab);
                    if (stock >= 0 && !_target.ShouldDeliver(stock))
                    {
                        _reason = "Stock target reached: " + stock + " " + prefab + ".";
                        return;
                    }
                }
            }

            var candidate = source.Peek(item => (_filter.IsEmpty || _filter.Matches(item)) && destination.CanInsert(item) && (interlock == null || interlock(item)));
            if (candidate == null)
            {
                _reason = "Nothing to move that the front accepts.";
                return;
            }

            if (!source.TryRemove(candidate))
            {
                _reason = "The source changed before the swing.";
                return;
            }

            // The item now exists only in the arm. Persist that before trying the destination.
            _hand = candidate;
            SaveNow();
            if (destination.TryInsert(candidate))
            {
                _hand = null;
                _moved++;
                _reason = "Moved " + candidate.Prefab + ".";
            }
            else
                _reason = "Holding " + candidate.Prefab + ". The front refused it.";
            SaveNow();
        }

        public override bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            var prefab = ItemBridge.PrefabName(item);
            _filter = _filter.Prefab == prefab ? new ItemFilter("") : new ItemFilter(prefab);
            SaveNow();
            WorkshopMachine.Message(user, _filter.IsEmpty ? "Filter cleared" : "Filter: " + prefab);
            return true;
        }

        public override bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold)
                return false;
            _targetIndex = (_targetIndex + 1) % Targets.Length;
            _target = new StockTarget(Targets[_targetIndex]);
            SaveNow();
            WorkshopMachine.Message(user, Targets[_targetIndex] == 0 ? "No stock target" : "Stock target " + Targets[_targetIndex] + " of the filtered item");
            return true;
        }

        public override List<ItemStack> Drain()
        {
            var list = new List<ItemStack>();
            if (_hand != null)
                list.Add(_hand);
            _hand = null;
            return list;
        }

        public override string Status()
        {
            var text = "Moved " + _moved + ". Filter: " + (_filter.IsEmpty ? "any" : _filter.Save());
            if (_targetIndex > 0)
                text += ". Stock target " + Targets[_targetIndex] + ".";
            if (_hand != null)
                text += "\nHolding " + _hand.Prefab + ".";
            if (!string.IsNullOrEmpty(_reason))
                text += "\n" + _reason;
            return text;
        }
    }

    /// <summary>Splitter: one-item buffer, three outputs, fair, priority, or filter. Needs a turning line.</summary>
    public sealed class SplitterBehaviour : MachineBehaviour
    {
        private readonly Splitter _splitter = new Splitter(3);
        private ItemStack _held;

        public SplitterBehaviour(WorkshopMachine machine) : base(machine) { }

        protected override void Load()
        {
            _held = ItemCodec.ParseOne(M.GetString("vf_held"));
            _splitter.Mode = (SplitMode)Mathf.Clamp(M.GetInt("vf_mode"), 0, 2);
            _splitter.Filter = ItemFilter.Load(M.GetString("vf_filter"));
        }

        protected override void Save()
        {
            M.Set("vf_held", _held == null ? "" : ItemCodec.FormatOne(_held));
            M.Set("vf_mode", (int)_splitter.Mode);
            M.Set("vf_filter", _splitter.Filter.Save());
        }

        private Transform Output(int index)
        {
            var name = index == 0 ? "Item_Out_Forward" : index == 1 ? "Item_Out_Left" : "Item_Out_Right";
            foreach (var port in M.ItemOut)
            {
                if (port.name == name)
                    return port;
            }

            return index < M.ItemOut.Count ? M.ItemOut[index] : null;
        }

        public override void Tick(float dt)
        {
            if (_held == null || !M.Producing)
                return;
            var targets = new IItemEndpoint[3];
            for (var i = 0; i < 3; i++)
            {
                var port = Output(i);
                if (port == null)
                    continue;
                var endpoint = EndpointFinder.MachinePort(M, port.position, true);
                if (endpoint != null && endpoint.Available(out _))
                    targets[i] = endpoint;
            }

            var held = _held;
            var index = _splitter.Choose(held, i => targets[i] != null && targets[i].CanInsert(held));
            if (index < 0)
                return;
            if (targets[index].TryInsert(held))
            {
                _held = null;
                SaveNow();
            }
        }

        public override IItemEndpoint InputAt(Transform port)
        {
            return new DelegateEndpoint
            {
                Name = "splitter",
                Machine = M,
                CanInsertFn = one => _held == null && one.Count == 1 && M.Producing,
                InsertFn = one =>
                {
                    _held = one.Copy(1);
                    SaveNow();
                    return true;
                }
            };
        }

        public override bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold)
                return false;
            _splitter.Mode = (SplitMode)(((int)_splitter.Mode + 1) % 3);
            SaveNow();
            WorkshopMachine.Message(user, ModeName());
            return true;
        }

        public override bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            var prefab = ItemBridge.PrefabName(item);
            _splitter.Filter = _splitter.Filter.Prefab == prefab ? new ItemFilter("") : new ItemFilter(prefab);
            _splitter.Mode = SplitMode.Filter;
            SaveNow();
            WorkshopMachine.Message(user, _splitter.Filter.IsEmpty ? "Filter cleared" : "Filter: " + prefab + " goes forward");
            return true;
        }

        private string ModeName()
        {
            switch (_splitter.Mode)
            {
                case SplitMode.Priority: return "Priority: forward first, then overflow left and right";
                case SplitMode.Filter: return "Filter: " + (_splitter.Filter.IsEmpty ? "(use an item to set)" : _splitter.Filter.Prefab) + " forward, the rest left and right";
                default: return "Fair: forward, left, right in turn";
            }
        }

        public override List<ItemStack> Drain()
        {
            var list = new List<ItemStack>();
            if (_held != null)
                list.Add(_held);
            _held = null;
            return list;
        }

        public override string Status() { return ModeName() + (_held != null ? "\nHolding " + _held.Prefab + "." : ""); }
    }

    /// <summary>Merger: one slot per input, taken in turn so no input starves.</summary>
    public sealed class MergerBehaviour : MachineBehaviour
    {
        private readonly Merger _merger = new Merger(3);
        private readonly ItemStack[] _slots = new ItemStack[3];

        public MergerBehaviour(WorkshopMachine machine) : base(machine) { }

        protected override void Load()
        {
            var list = M.GetString("vf_slots").Split('\n');
            for (var i = 0; i < 3; i++)
                _slots[i] = i < list.Length ? ItemCodec.ParseOne(list[i]) : null;
        }

        protected override void Save()
        {
            var rows = new string[3];
            for (var i = 0; i < 3; i++)
                rows[i] = _slots[i] == null ? "" : ItemCodec.FormatOne(_slots[i]);
            M.Set("vf_slots", string.Join("\n", rows));
        }

        public override void Tick(float dt)
        {
            if (!M.Producing || M.ItemOut.Count == 0)
                return;
            var next = EndpointFinder.MachinePort(M, M.ItemOut[0].position, true);
            if (next == null || !next.Available(out _))
                return;
            var index = _merger.Choose(i => _slots[i] != null && next.CanInsert(_slots[i]));
            if (index < 0)
                return;
            if (next.TryInsert(_slots[index]))
            {
                _slots[index] = null;
                SaveNow();
            }
        }

        public override IItemEndpoint InputAt(Transform port)
        {
            var index = Mathf.Max(0, M.ItemIn.IndexOf(port));
            return new DelegateEndpoint
            {
                Name = "merger",
                Machine = M,
                CanInsertFn = one => _slots[index] == null && one.Count == 1 && M.Producing,
                InsertFn = one =>
                {
                    _slots[index] = one.Copy(1);
                    SaveNow();
                    return true;
                }
            };
        }

        public override List<ItemStack> Drain()
        {
            var list = new List<ItemStack>();
            for (var i = 0; i < 3; i++)
            {
                if (_slots[i] != null)
                    list.Add(_slots[i]);
                _slots[i] = null;
            }

            return list;
        }

        public override string Status() { return "Inputs: " + Describe(Array.FindAll(_slots, s => s != null)); }
    }
}
