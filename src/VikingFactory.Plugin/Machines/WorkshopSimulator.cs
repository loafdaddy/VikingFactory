using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using VikingFactory.Core;
using VikingFactory.Core.Kinetics;
using VikingFactory.Core.Runtime;

namespace VikingFactory.Machines
{
    /// <summary>
    /// Runs the workshop at a fixed 5 Hz. A long frame gap, such as sleeping, is dropped rather than replayed.
    /// Links are rebuilt from live kinetic ports when pieces load or unload. Every peer solves the same graph for
    /// visuals; only a piece's owner moves items or spends fuel.
    /// </summary>
    public class WorkshopSimulator : MonoBehaviour
    {
        public const float StepSeconds = 0.2f;
        private const float LinkReach = 0.45f;

        private readonly FixedStepClock _clock = new FixedStepClock(StepSeconds, 5, 2.0);
        private readonly List<Link> _links = new List<Link>();
        private readonly Dictionary<string, WorkshopMachine> _byId = new Dictionary<string, WorkshopMachine>();
        private int _linkedVersion = -1;
        private float _relinkTimer;
        private readonly Stopwatch _watch = new Stopwatch();

        public static WorkshopSimulator Instance { get; private set; }
        public KineticNetwork LastNetwork { get; private set; }
        public double LastStepMs { get; private set; }
        public double WorstStepMs { get; private set; }
        public int Discontinuities => _clock.Discontinuities;

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            if (ZNet.instance == null)
                return;
            var steps = _clock.Advance(Time.deltaTime);
            for (var i = 0; i < steps; i++)
                Step(StepSeconds);
        }

        private void Step(float dt)
        {
            _watch.Reset();
            _watch.Start();
            var machines = WorkshopRegistry.All;
            _relinkTimer -= dt;
            if (_linkedVersion != WorkshopRegistry.Version || _relinkTimer <= 0f)
                Relink(machines);

            var network = new KineticNetwork { Placement = new WaterProbe(this) };
            for (var i = 0; i < machines.Count; i++)
            {
                var machine = machines[i];
                if (machine == null || machine.Behaviour == null)
                    continue;
                machine.ObserveOwnership();
                machine.Behaviour.EnsureLoaded();
                var id = machine.NodeId;
                if (network.Contains(id))
                    continue;
                machine.Behaviour.AddNode(network, id);
                if (!network.Contains(id))
                    network.AddTransmission(id);

                var block = machine.Behaviour.FeatureBlock();
                if (block.Length == 0 && !Progression.IsUnlocked(machine.Spec.Milestones, out var missing))
                    block = "Locked until " + missing + " is defeated.";
                machine.Enabled = block.Length == 0;
                machine.DisabledReason = block;
                if (!machine.Enabled)
                    network.SetEnabled(id, false);
            }

            foreach (var link in _links)
            {
                if (link.A == null || link.B == null || link.A.Behaviour == null || link.B.Behaviour == null)
                    continue;
                if (!network.Contains(link.A.NodeId) || !network.Contains(link.B.NodeId))
                    continue;
                var factor = link.A.Behaviour.PortMultiplier(link.PortA) / link.B.Behaviour.PortMultiplier(link.PortB);
                network.Connect(link.A.NodeId, link.B.NodeId, factor);
            }

            for (var i = 0; i < machines.Count; i++)
            {
                var machine = machines[i];
                if (machine != null && machine.Behaviour != null && machine.Enabled)
                    machine.Behaviour.BeforeSolve(network, machine.NodeId);
            }

            network.Solve(dt);
            LastNetwork = network;

            for (var i = 0; i < machines.Count; i++)
            {
                var machine = machines[i];
                if (machine == null || machine.Behaviour == null || !network.Contains(machine.NodeId))
                    continue;
                var id = machine.NodeId;
                var stop = network.StopReason(id);
                machine.Supply = network.SupplyDu(id);
                machine.Reserved = network.ReservedDu(id);
                machine.Speed = network.Speed(id);
                machine.UsefulRpm = network.UsefulRpm(id);
                machine.Component = network.ComponentOf(id);
                machine.Producing = machine.Enabled && network.IsProducing(id);
                machine.Turning = stop == KineticStop.Working && Math.Abs(machine.Speed) > 0.01;
                machine.Stop = machine.Enabled ? Describe(stop) : machine.DisabledReason;
                machine.Behaviour.AfterSolve(network, id, dt);
                if (machine.Enabled && machine.CanActLocally(out _))
                {
                    try
                    {
                        machine.Behaviour.Tick(dt);
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError("[VikingFactory] " + machine.Spec.Id + " tick failed: " + ex);
                    }
                }
            }

            _watch.Stop();
            LastStepMs = _watch.Elapsed.TotalMilliseconds;
            WorstStepMs = Math.Max(WorstStepMs * 0.999, LastStepMs);
        }

        private void Relink(IReadOnlyList<WorkshopMachine> machines)
        {
            _linkedVersion = WorkshopRegistry.Version;
            _relinkTimer = 2f;
            _links.Clear();
            _byId.Clear();
            var cells = new Dictionary<Vector3Int, List<KeyValuePair<WorkshopMachine, Transform>>>();
            for (var i = 0; i < machines.Count; i++)
            {
                var machine = machines[i];
                if (machine == null)
                    continue;
                var zdo = machine.GetZdo();
                machine.NodeId = zdo != null ? zdo.m_uid.ToString() : "local:" + machine.GetInstanceID();
                _byId[machine.NodeId] = machine;
                foreach (var port in machine.Kinetic)
                {
                    var cell = Cell(port.position);
                    if (!cells.TryGetValue(cell, out var list))
                    {
                        list = new List<KeyValuePair<WorkshopMachine, Transform>>();
                        cells[cell] = list;
                    }

                    list.Add(new KeyValuePair<WorkshopMachine, Transform>(machine, port));
                }
            }

            var seen = new HashSet<string>();
            foreach (var pair in cells)
            {
                foreach (var a in pair.Value)
                {
                    for (var dx = -1; dx <= 1; dx++)
                    for (var dy = -1; dy <= 1; dy++)
                    for (var dz = -1; dz <= 1; dz++)
                    {
                        if (!cells.TryGetValue(pair.Key + new Vector3Int(dx, dy, dz), out var near))
                            continue;
                        foreach (var b in near)
                        {
                            if (a.Key == b.Key || Vector3.Distance(a.Value.position, b.Value.position) > LinkReach)
                                continue;
                            var key = string.CompareOrdinal(a.Key.NodeId, b.Key.NodeId) < 0
                                ? a.Key.NodeId + "|" + b.Key.NodeId
                                : b.Key.NodeId + "|" + a.Key.NodeId;
                            if (!seen.Add(key))
                                continue;
                            _links.Add(new Link { A = a.Key, PortA = a.Value, B = b.Key, PortB = b.Value });
                        }
                    }
                }
            }
        }

        private static Vector3Int Cell(Vector3 position)
        {
            return new Vector3Int(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y), Mathf.FloorToInt(position.z));
        }

        public static string Describe(KineticStop stop)
        {
            switch (stop)
            {
                case KineticStop.Working: return "Turning.";
                case KineticStop.NoPower: return "No power.";
                case KineticStop.Overloaded: return "Overloaded: load exceeds drive. Everything on this line stops.";
                case KineticStop.InvalidPlacement: return "Water wheel is not in a valid site.";
                case KineticStop.InvalidRotation: return "Rotation conflict: a ratio loop, or sources turning against each other or at different speeds. Add a governor or remove the loop.";
                default: return "Disconnected.";
            }
        }

        private sealed class Link
        {
            public WorkshopMachine A;
            public Transform PortA;
            public WorkshopMachine B;
            public Transform PortB;
        }

        private sealed class WaterProbe : IPlacementProbe
        {
            private readonly WorkshopSimulator _simulator;

            public WaterProbe(WorkshopSimulator simulator)
            {
                _simulator = simulator;
            }

            public bool IsValid(string nodeId, string ruleId)
            {
                if (ruleId != "valid-water" || !_simulator._byId.TryGetValue(nodeId, out var machine))
                    return false;
                var wheel = machine.Behaviour as WaterWheelBehaviour;
                return wheel != null && wheel.SiteValid();
            }
        }
    }
}
