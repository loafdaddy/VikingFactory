using System;
using System.Collections.Generic;
using UnityEngine;
using VikingFactory.Core;
using VikingFactory.Core.Items;
using VikingFactory.Core.Kinetics;
using VikingFactory.Core.Production;

namespace VikingFactory.Machines
{
    public sealed class PassiveBehaviour : MachineBehaviour
    {
        public PassiveBehaviour(WorkshopMachine machine) : base(machine) { }

        public override void AddNode(KineticNetwork network, string id)
        {
            network.AddTransmission(id);
        }

        public override string Status()
        {
            if (M.Role == MachineRole.Marker)
                return "Two or more of your stakes mark a plantation plot for a timber saw.";
            if (M.Role == MachineRole.OvenExtension)
                return "Cooking tenders within 3 m reach farther.";
            return "";
        }
    }

    /// <summary>Hand crank. Held on the crank's owner, recorded in world time so every peer solves the same line.</summary>
    public sealed class CrankBehaviour : MachineBehaviour
    {
        private const string HeldKey = "vf_crank_until";
        private const float StaminaPerSecond = 3f;
        private float _lastHold;

        public CrankBehaviour(WorkshopMachine machine) : base(machine) { }

        private static double Now => ZNet.instance != null ? ZNet.instance.GetTimeSeconds() : Time.time;

        public bool Held => M.GetFloat(HeldKey) > Now;

        public override void AddNode(KineticNetwork network, string id)
        {
            network.AddSource(id, BalanceDefaults.HandCrankDu, "", BalanceDefaults.MilestoneRpm);
            network.SetEnabled(id, Held);
        }

        public override bool Interact(Humanoid user, bool hold, bool alt)
        {
            M.Claim();
            var elapsed = _lastHold > 0f ? Mathf.Clamp(Time.time - _lastHold, 0f, 0.5f) : 0.2f;
            _lastHold = Time.time;
            var cost = StaminaPerSecond * elapsed;
            var player = user as Player;
            if (player != null)
            {
                if (!player.HaveStamina(cost))
                {
                    WorkshopMachine.Message(user, "Too tired to turn the crank.");
                    return false;
                }

                player.UseStamina(cost);
            }

            M.Set(HeldKey, (float)(Now + 0.4));
            return true;
        }

        public override string Status()
        {
            return Held ? "Turning by hand." : "Hold interact to turn. 3 stamina a second.";
        }
    }

    /// <summary>Bronze cog: 1:1, 2:1, or 1:2 at its output port. Load does not change with speed.</summary>
    public sealed class CogBehaviour : MachineBehaviour
    {
        private static readonly double[] Ratios = { 1.0, 2.0, 0.5 };
        private int _setting;

        public CogBehaviour(WorkshopMachine machine) : base(machine) { }

        protected override void Load() { _setting = Mathf.Clamp(M.GetInt("vf_ratio"), 0, Ratios.Length - 1); }
        protected override void Save() { M.Set("vf_ratio", _setting); }

        public override double PortMultiplier(Transform port)
        {
            return port.name == "Kinetic_Out" ? Ratios[_setting] : 1.0;
        }

        public override bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold)
                return false;
            _setting = (_setting + 1) % Ratios.Length;
            SaveNow();
            WorkshopMachine.Message(user, "Ratio " + Label());
            return true;
        }

        private string Label()
        {
            return _setting == 0 ? "1:1" : _setting == 1 ? "2:1 (output twice as fast)" : "1:2 (output half speed)";
        }

        public override string Status() { return "Ratio " + Label() + ". Interact to change."; }
    }

    public sealed class ReversingCogBehaviour : MachineBehaviour
    {
        public ReversingCogBehaviour(WorkshopMachine machine) : base(machine) { }

        public override double PortMultiplier(Transform port)
        {
            return port.name == "Kinetic_Out" ? -1.0 : 1.0;
        }

        public override string Status() { return "The output turns the other way."; }
    }

    public sealed class ClutchBehaviour : MachineBehaviour
    {
        private bool _closed;

        public ClutchBehaviour(WorkshopMachine machine) : base(machine) { }

        protected override void Load() { _closed = M.GetInt("vf_clutch") == 1; }
        protected override void Save() { M.Set("vf_clutch", _closed ? 1 : 0); }

        public override void AddNode(KineticNetwork network, string id)
        {
            network.AddClutch(id);
            network.SetClutchClosed(id, _closed);
        }

        public override bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold)
                return false;
            _closed = !_closed;
            SaveNow();
            return true;
        }

        public override string Status()
        {
            return _closed ? "Closed. The branch beyond is disconnected." : "Open. Power passes through.";
        }
    }

    /// <summary>
    /// Water wheel. Drive only when the lower paddles sit in real water, the water is deeper than a puddle,
    /// no other wheel is within 6 m, and nothing built blocks the wheel. There is no current vector and no freeze rule.
    /// </summary>
    public sealed class WaterWheelBehaviour : MachineBehaviour
    {
        private static int _blockMask = -1;
        private float _nextCheck;
        private bool _valid;
        private string _reason = "Not checked yet.";

        public WaterWheelBehaviour(WorkshopMachine machine) : base(machine) { }

        public override void AddNode(KineticNetwork network, string id)
        {
            network.AddSource(id, BalanceDefaults.WaterWheelDu, "valid-water", BalanceDefaults.MilestoneRpm);
        }

        public bool SiteValid()
        {
            if (Time.time < _nextCheck)
                return _valid;
            _nextCheck = Time.time + 2f;
            _valid = Check(out _reason);
            return _valid;
        }

        private bool Check(out string reason)
        {
            var basePos = M.transform.position;
            var paddle = basePos + Vector3.up * 0.25f;
            var water = Floating.GetLiquidLevel(paddle, 1f, LiquidType.Water);
            if (water < paddle.y + BalanceDefaults.WaterWheelMinImmersionMetres - 0.25f)
            {
                reason = "Paddles are not in water.";
                return false;
            }

            if (ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(basePos, out var ground) && water - ground < 0.5f)
            {
                reason = "Water is too shallow.";
                return false;
            }

            var sites = new List<SiteSpacing.Site>();
            foreach (var other in WorkshopRegistry.All)
            {
                if (other.Role == MachineRole.WaterWheel)
                    sites.Add(Site(other));
            }

            if (!SiteSpacing.IsClear(Site(M), sites, BalanceDefaults.WaterWheelSpacingMetres, out _))
            {
                reason = "Another water wheel is within 6 m.";
                return false;
            }

            if (_blockMask < 0)
                _blockMask = LayerMask.GetMask("piece", "piece_nonsolid", "Default", "static_solid");
            var hits = Physics.OverlapBox(basePos + Vector3.up * 2f, new Vector3(0.7f, 1.6f, 1.6f), M.transform.rotation, _blockMask);
            foreach (var hit in hits)
            {
                if (hit.GetComponentInParent<WorkshopMachine>() == M)
                    continue;
                if (hit.GetComponentInParent<Piece>() != null)
                {
                    reason = "Something built blocks the wheel.";
                    return false;
                }
            }

            reason = "In water.";
            return true;
        }

        public static SiteSpacing.Site Site(WorkshopMachine machine)
        {
            var zdo = machine.GetZdo();
            var id = zdo != null ? zdo.m_uid.ToString() : machine.GetInstanceID().ToString();
            return new SiteSpacing.Site(id, machine.transform.position.x, machine.transform.position.z);
        }

        public override string Status() { return _reason; }
    }

    /// <summary>
    /// Steam engines and the eitr motor. Paid running time is kept on the ZDO, so a stall or reload
    /// never burns a second fuel item for the same seconds. Manual fueling always works.
    /// </summary>
    public sealed class EngineBehaviour : MachineBehaviour
    {
        private StackStore _fuel;
        private FuelledSource _engine;
        private bool _offered;
        private string _reason = "";
        private float _nextCheck;
        private bool _siteOk;

        public EngineBehaviour(WorkshopMachine machine) : base(machine)
        {
            _fuel = new StackStore(1, 20);
            _engine = new FuelledSource(SecondsPerFuel);
        }

        private string FuelPrefab => M.Role == MachineRole.EitrMotor ? "Eitr" : "Coal";
        private double SecondsPerFuel => M.Role == MachineRole.EitrMotor ? BalanceDefaults.EitrSecondsPerRefinedEitr
            : M.Role == MachineRole.ReinforcedSteam ? BalanceDefaults.ReinforcedSteamSecondsPerCoal : BalanceDefaults.SteamSecondsPerCoal;
        private int Capacity => M.Role == MachineRole.SteamEngine ? BalanceDefaults.SteamEngineDu : M.Role == MachineRole.ReinforcedSteam ? BalanceDefaults.ReinforcedSteamDu : BalanceDefaults.EitrMotorDu;
        private int Rpm => M.Role == MachineRole.SteamEngine ? BalanceDefaults.SteamEngineRpm : M.Role == MachineRole.ReinforcedSteam ? BalanceDefaults.ReinforcedSteamRpm : BalanceDefaults.EitrMotorRpm;

        protected override void Load()
        {
            _fuel = new StackStore(1, 20, ItemCodec.Parse(M.GetString("vf_fuel")));
            _engine = new FuelledSource(SecondsPerFuel, M.GetFloat("vf_paid"));
        }

        protected override void Save()
        {
            M.Set("vf_fuel", ItemCodec.Format(_fuel.Stacks));
            M.Set("vf_paid", (float)_engine.PaidSeconds);
        }

        public override void AddNode(KineticNetwork network, string id)
        {
            network.AddSource(id, Capacity, "", Rpm);
        }

        public override void BeforeSolve(KineticNetwork network, string id)
        {
            _offered = false;
            if (!SiteReady())
            {
                network.SetEnabled(id, false);
                return;
            }

            if (M.CanActLocally(out _))
            {
                var before = _engine.FuelConsumed;
                _offered = _engine.Prepare(() => _fuel.TakeOne() != null);
                if (_engine.FuelConsumed != before)
                    SaveNow();
            }
            else
                _offered = M.GetFloat("vf_paid") > 0f || _fuel.Total() > 0;

            network.SetEnabled(id, _offered);
            if (!_offered)
                _reason = "Out of " + FuelPrefab + ".";
        }

        public override void AfterSolve(KineticNetwork network, string id, float dt)
        {
            if (!_offered || !M.CanActLocally(out _))
                return;
            var running = network.IsContributing(id);
            if (!running)
                return;
            _engine.Accrue(dt, true);
            SaveNow();
        }

        private bool SiteReady()
        {
            if (Time.time < _nextCheck)
                return _siteOk;
            _nextCheck = Time.time + 2f;
            _siteOk = CheckSite(out _reason);
            return _siteOk;
        }

        private bool CheckSite(out string reason)
        {
            reason = "Running.";
            if (M.Role != MachineRole.EitrMotor)
            {
                var intake = false;
                foreach (var other in WorkshopRegistry.All)
                {
                    if (other.Role != MachineRole.WaterIntake || Vector3.Distance(other.transform.position, M.transform.position) > 4f)
                        continue;
                    if (WaterIntakeBehaviour.InWater(other))
                    {
                        intake = true;
                        break;
                    }
                }

                if (!intake)
                {
                    reason = "No water intake in real water within 4 m.";
                    return false;
                }
            }

            if (M.SmokeOut != null)
            {
                var mask = LayerMask.GetMask("piece", "Default", "static_solid", "terrain");
                var hits = Physics.OverlapSphere(M.SmokeOut.position + Vector3.up * 0.7f, 0.45f, mask);
                foreach (var hit in hits)
                {
                    if (hit.GetComponentInParent<WorkshopMachine>() == M)
                        continue;
                    reason = "Exhaust blocked.";
                    return false;
                }
            }

            return true;
        }

        private bool IsFuel(ItemStack one) { return one.Prefab == FuelPrefab; }

        public override IItemEndpoint InputAt(Transform port)
        {
            return new StoreEndpoint(M, "fuel", () => _fuel, SaveNow, IsFuel, false);
        }

        public override bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            var stack = ItemBridge.FromItemData(item, 1);
            if (!IsFuel(stack))
            {
                WorkshopMachine.Message(user, "This engine burns " + FuelPrefab + ".");
                return true;
            }

            if (!_fuel.CanAccept(stack) || !TakeFromPlayer(user, item, 1))
                return true;
            _fuel.TryAdd(stack);
            SaveNow();
            return true;
        }

        public override List<ItemStack> Drain() { return _fuel.TakeAll(); }

        public override string Status()
        {
            return _reason + "\nFuel: " + _fuel.Total() + " " + FuelPrefab + ", paid " + M.GetFloat("vf_paid").ToString("0") + " s. Use " + FuelPrefab + " on it to refuel by hand.";
        }
    }

    public sealed class WaterIntakeBehaviour : MachineBehaviour
    {
        public WaterIntakeBehaviour(WorkshopMachine machine) : base(machine) { }

        public static bool InWater(WorkshopMachine intake)
        {
            var probe = intake.WaterProbe != null ? intake.WaterProbe.position : intake.transform.position;
            return Floating.GetLiquidLevel(probe, 1f, LiquidType.Water) > probe.y + 0.1f;
        }

        public override string Status()
        {
            return InWater(M) ? "Probe is in water." : "Probe is not in water.";
        }
    }

    /// <summary>
    /// Sail wheel. Wind is read on the peer that simulates this piece, the same environment the windmill uses.
    /// Exposure is the share of eight horizontal rays from the sail that travel 6 m unobstructed.
    /// </summary>
    public sealed class SailBehaviour : MachineBehaviour
    {
        private float _next;
        private int _capacity;
        private double _wind;
        private double _exposure;

        public SailBehaviour(WorkshopMachine machine) : base(machine) { }

        public override void AddNode(KineticNetwork network, string id)
        {
            if (Time.time >= _next)
            {
                _next = Time.time + 1f;
                _wind = EnvMan.instance != null ? EnvMan.instance.GetWindIntensity() : 0;
                _exposure = Exposure();
                _capacity = BalanceDefaults.SailCapacity(_wind, _exposure);
            }

            network.AddSource(id, _capacity, "", BalanceDefaults.SailWheelRpm);
        }

        private double Exposure()
        {
            var origin = M.WindProbe != null ? M.WindProbe.position : M.transform.position + Vector3.up * 3.5f;
            var mask = LayerMask.GetMask("piece", "Default", "static_solid", "terrain");
            var clear = 0;
            for (var i = 0; i < 8; i++)
            {
                var dir = Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward;
                var hits = Physics.RaycastAll(origin, dir, 6f, mask);
                var blocked = false;
                foreach (var hit in hits)
                {
                    if (hit.collider.GetComponentInParent<WorkshopMachine>() != M)
                    {
                        blocked = true;
                        break;
                    }
                }

                if (!blocked)
                    clear++;
            }

            return clear / 8.0;
        }

        public override string Status()
        {
            return "Wind " + (_wind * 100).ToString("0") + "%, open " + (_exposure * 100).ToString("0") + "%: " + _capacity + " DU.";
        }
    }

    public sealed class GovernorBehaviour : MachineBehaviour
    {
        private static readonly int[] Settings = { 16, 32, 64, 8 };
        private int _index;

        public GovernorBehaviour(WorkshopMachine machine) : base(machine) { }

        protected override void Load() { _index = Mathf.Clamp(M.GetInt("vf_governor"), 0, Settings.Length - 1); }
        protected override void Save() { M.Set("vf_governor", _index); }

        public override void AddNode(KineticNetwork network, string id)
        {
            network.AddGovernor(id, Settings[_index]);
        }

        public override bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold)
                return false;
            _index = (_index + 1) % Settings.Length;
            SaveNow();
            WorkshopMachine.Message(user, "Governed to " + Settings[_index] + " RPM");
            return true;
        }

        public override string Status() { return "Governed to " + Settings[_index] + " RPM. Interact to change."; }
    }

    /// <summary>Flywheel energy is stored on the wheel, so it survives splits, merges, and reloads.</summary>
    public sealed class FlywheelBehaviour : MachineBehaviour
    {
        private double _stored;
        private double _flow;

        public FlywheelBehaviour(WorkshopMachine machine) : base(machine) { }

        protected override void Load() { _stored = M.GetFloat("vf_energy"); }
        protected override void Save() { M.Set("vf_energy", (float)_stored); }

        public override void AddNode(KineticNetwork network, string id)
        {
            network.AddFlywheel(id, _stored);
        }

        public override void AfterSolve(KineticNetwork network, string id, float dt)
        {
            _flow = network.FlywheelFlow(id);
            if (!M.CanActLocally(out _))
                return;
            var stored = network.StoredEnergy(id);
            if (Math.Abs(stored - _stored) < 0.001)
                return;
            _stored = stored;
            SaveNow();
        }

        public override string Status()
        {
            var flow = _flow > 0.01 ? ", charging " + _flow.ToString("0") + " DU" : _flow < -0.01 ? ", giving " + (-_flow).ToString("0") + " DU" : "";
            return "Stored " + _stored.ToString("0") + " / " + BalanceDefaults.FlywheelCapacityDuSeconds.ToString("0") + " DU·s" + flow + ".";
        }
    }
}
