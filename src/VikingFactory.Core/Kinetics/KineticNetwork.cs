using System;
using System.Collections.Generic;
using System.Linq;

namespace VikingFactory.Core.Kinetics
{
    public enum KineticStop
    {
        Working,
        NoPower,
        Overloaded,
        Disconnected,
        InvalidPlacement,
        InvalidRotation
    }

    public interface IPlacementProbe
    {
        bool IsValid(string nodeId, string ruleId);
    }

    public sealed class RejectingPlacementProbe : IPlacementProbe
    {
        public bool IsValid(string nodeId, string ruleId)
        {
            return false;
        }
    }

    /// <summary>
    /// Fixed-load kinetic graph. Shafts reserve nothing. A closed clutch disconnects its branch.
    /// Speed and direction belong to each node. An edge factor is the speed ratio from upstream to
    /// downstream; -1 reverses. A component whose ratios or sources disagree stalls as InvalidRotation.
    /// </summary>
    public sealed class KineticNetwork
    {
        private const double Tolerance = 1e-6;

        private readonly Dictionary<string, Node> _nodes = new Dictionary<string, Node>();
        private readonly List<Edge> _edges = new List<Edge>();
        private readonly Dictionary<string, List<int>> _edgesByNode = new Dictionary<string, List<int>>();

        public int Rpm { get; } = BalanceDefaults.MilestoneRpm;
        public IPlacementProbe Placement { get; set; } = new RejectingPlacementProbe();

        public string AddSource(string id, int capacityDu, string placementRule = "")
        {
            return AddSource(id, capacityDu, placementRule, BalanceDefaults.MilestoneRpm);
        }

        public string AddSource(string id, int capacityDu, string placementRule, int rpm)
        {
            var node = Add(id, NodeRole.Source, capacityDu, placementRule);
            node.NativeRpm = rpm;
            return id;
        }

        public string AddTransmission(string id)
        {
            Add(id, NodeRole.Transmission, 0, "");
            return id;
        }

        public string AddConsumer(string id, int loadDu)
        {
            return AddConsumer(id, loadDu, int.MaxValue);
        }

        public string AddConsumer(string id, int loadDu, int maxRpm)
        {
            var node = Add(id, NodeRole.Consumer, loadDu, "");
            node.MaxRpm = maxRpm;
            return id;
        }

        public string AddClutch(string id)
        {
            Add(id, NodeRole.Clutch, 0, "");
            return id;
        }

        /// <summary>A governor fixes the component's speed. Sources slower than the setting do not contribute.</summary>
        public string AddGovernor(string id, int rpmSetting)
        {
            if (rpmSetting < 1)
                throw new ArgumentOutOfRangeException(nameof(rpmSetting));
            var node = Add(id, NodeRole.Governor, 0, "");
            node.NativeRpm = rpmSetting;
            return id;
        }

        /// <summary>Stores DU-seconds. Energy belongs to the wheel, so it survives a split or merge.</summary>
        public string AddFlywheel(string id, double storedDuSeconds)
        {
            var node = Add(id, NodeRole.Flywheel, 0, "");
            node.Stored = Math.Max(0, Math.Min(BalanceDefaults.FlywheelCapacityDuSeconds, storedDuSeconds));
            return id;
        }

        public void Connect(string upstream, string downstream)
        {
            Connect(upstream, downstream, 1.0);
        }

        /// <summary>speed(downstream) = speed(upstream) × factor.</summary>
        public void Connect(string upstream, string downstream, double factor)
        {
            if (!_nodes.ContainsKey(upstream) || !_nodes.ContainsKey(downstream))
                throw new InvalidOperationException("Both endpoints must exist.");
            if (Math.Abs(factor) < Tolerance)
                throw new ArgumentOutOfRangeException(nameof(factor), "A ratio cannot be zero.");
            var index = _edges.Count;
            _edges.Add(new Edge(upstream, downstream, factor));
            _edgesByNode[upstream].Add(index);
            _edgesByNode[downstream].Add(index);
        }

        public void SetEnabled(string id, bool enabled)
        {
            _nodes[id].Enabled = enabled;
        }

        public void SetCapacity(string id, int capacityDu)
        {
            _nodes[id].Amount = Math.Max(0, capacityDu);
        }

        /// <summary>
        /// Closed means the branch is disconnected, matching the design spec.
        /// </summary>
        public void SetClutchClosed(string id, bool closed)
        {
            var node = _nodes[id];
            if (node.Role != NodeRole.Clutch)
                throw new InvalidOperationException(id + " is not a clutch.");
            node.ClutchClosed = closed;
        }

        public void Solve()
        {
            Solve(0);
        }

        /// <summary>Solves every component. dtSeconds only moves flywheel energy.</summary>
        public void Solve(double dtSeconds)
        {
            foreach (var node in _nodes.Values)
            {
                node.SupplyDu = 0;
                node.ReservedDu = 0;
                node.Producing = false;
                node.Contributing = false;
                node.Speed = 0;
                node.FlywheelFlowDu = 0;
                node.Stop = KineticStop.NoPower;
            }

            var seen = new HashSet<string>();
            foreach (var id in _nodes.Keys.OrderBy(k => k, StringComparer.Ordinal))
            {
                if (!seen.Add(id))
                    continue;
                var component = Collect(id, seen);
                Evaluate(component, Math.Max(0, dtSeconds));
            }
        }

        public bool IsProducing(string id)
        {
            return _nodes[id].Producing;
        }

        public KineticStop StopReason(string id)
        {
            return _nodes[id].Stop;
        }

        public int SupplyDu(string id)
        {
            return _nodes[id].SupplyDu;
        }

        public int ReservedDu(string id)
        {
            return _nodes[id].ReservedDu;
        }

        /// <summary>Signed RPM. Negative turns the other way. Zero when stalled or conflicted.</summary>
        public double Speed(string id)
        {
            return _nodes[id].Speed;
        }

        /// <summary>Speed a consumer actually uses, after its own cap.</summary>
        public double UsefulRpm(string id)
        {
            var node = _nodes[id];
            return Math.Min(Math.Abs(node.Speed), node.MaxRpm);
        }

        /// <summary>True when this source added its capacity to the component.</summary>
        public bool IsContributing(string id)
        {
            return _nodes[id].Contributing;
        }

        public double StoredEnergy(string id)
        {
            return _nodes[id].Stored;
        }

        /// <summary>Positive while charging, negative while discharging, in DU.</summary>
        public double FlywheelFlow(string id)
        {
            return _nodes[id].FlywheelFlowDu;
        }

        private Node Add(string id, NodeRole role, int amount, string placementRule)
        {
            if (_nodes.ContainsKey(id))
                throw new InvalidOperationException("Duplicate node " + id);
            var node = new Node(id, role, amount, placementRule);
            _nodes[id] = node;
            _edgesByNode[id] = new List<int>();
            return node;
        }

        private List<string> Collect(string start, HashSet<string> seen)
        {
            var component = new List<string>();
            var stack = new Stack<string>();
            stack.Push(start);
            while (stack.Count > 0)
            {
                var id = stack.Pop();
                component.Add(id);
                foreach (var hop in Hops(id))
                {
                    if (seen.Add(hop.Other))
                        stack.Push(hop.Other);
                }
            }

            component.Sort(StringComparer.Ordinal);
            return component;
        }

        private IEnumerable<Hop> Hops(string id)
        {
            var node = _nodes[id];
            if (node.Role == NodeRole.Clutch && node.ClutchClosed)
                yield break;
            foreach (var index in _edgesByNode[id])
            {
                var edge = _edges[index];
                if (edge.From == id)
                {
                    var other = _nodes[edge.To];
                    if (other.Role == NodeRole.Clutch && other.ClutchClosed)
                        continue;
                    yield return new Hop(edge.To, edge.Factor);
                }
                else
                {
                    var other = _nodes[edge.From];
                    if (other.Role == NodeRole.Clutch && other.ClutchClosed)
                        continue;
                    yield return new Hop(edge.From, 1.0 / edge.Factor);
                }
            }
        }

        private void Evaluate(List<string> component, double dt)
        {
            var placementBlocked = false;
            var sources = new List<Node>();
            Node? governor = null;
            var consumers = new List<Node>();
            var flywheels = new List<Node>();

            foreach (var id in component)
            {
                var node = _nodes[id];
                switch (node.Role)
                {
                    case NodeRole.Source:
                        if (!node.Enabled)
                            break;
                        if (!string.IsNullOrEmpty(node.PlacementRule) && !Placement.IsValid(node.Id, node.PlacementRule))
                        {
                            placementBlocked = true;
                            break;
                        }

                        sources.Add(node);
                        break;
                    case NodeRole.Governor:
                        if (node.Enabled && (governor == null || node.NativeRpm < governor.NativeRpm))
                            governor = node;
                        break;
                    case NodeRole.Consumer:
                        if (node.Enabled)
                            consumers.Add(node);
                        break;
                    case NodeRole.Flywheel:
                        flywheels.Add(node);
                        break;
                }
            }

            var reserved = consumers.Sum(c => c.Amount);
            if (sources.Count == 0)
            {
                Finish(component, 0, reserved, placementBlocked ? KineticStop.InvalidPlacement : KineticStop.NoPower, null);
                return;
            }

            var reference = governor ?? sources[0];
            var referenceRpm = (double)reference.NativeRpm;
            var speeds = Propagate(component, reference.Id, referenceRpm);
            if (speeds == null)
            {
                Finish(component, 0, reserved, KineticStop.InvalidRotation, null);
                return;
            }

            var supply = 0;
            foreach (var source in sources)
            {
                var speed = speeds[source.Id];
                if (speed < -Tolerance)
                {
                    // A source that would spin against the network does not connect. It stalls the line.
                    Finish(component, 0, reserved, KineticStop.InvalidRotation, null);
                    return;
                }

                if (governor == null)
                {
                    if (Math.Abs(speed - source.NativeRpm) > Tolerance)
                    {
                        Finish(component, 0, reserved, KineticStop.InvalidRotation, null);
                        return;
                    }
                }
                else if (speed > source.NativeRpm + Tolerance)
                {
                    // Governed faster than this source can turn. It idles rather than stalling the line.
                    continue;
                }

                source.Contributing = true;
                supply += source.Amount;
            }

            var stop = KineticStop.Working;
            var discharge = 0.0;
            if (supply < reserved)
            {
                var deficit = reserved - supply;
                var available = 0.0;
                if (dt > 0)
                {
                    foreach (var wheel in flywheels)
                        available += Math.Min(BalanceDefaults.FlywheelMaxDischargeDu, wheel.Stored / dt);
                }

                if (available + Tolerance >= deficit)
                {
                    discharge = deficit;
                    Discharge(flywheels, deficit, dt);
                }
                else
                {
                    stop = supply <= 0 ? KineticStop.NoPower : KineticStop.Overloaded;
                }
            }
            else if (dt > 0)
            {
                Charge(flywheels, supply - reserved, dt);
            }

            if (supply <= 0 && discharge <= 0 && reserved > 0)
                stop = KineticStop.NoPower;

            var reportedSupply = supply + (int)Math.Round(discharge);
            Finish(component, reportedSupply, reserved, stop, stop == KineticStop.Working ? speeds : null);
        }

        private Dictionary<string, double>? Propagate(List<string> component, string referenceId, double referenceRpm)
        {
            var speeds = new Dictionary<string, double> { [referenceId] = referenceRpm };
            var queue = new Queue<string>();
            queue.Enqueue(referenceId);
            while (queue.Count > 0)
            {
                var id = queue.Dequeue();
                var speed = speeds[id];
                foreach (var hop in Hops(id))
                {
                    var next = speed * hop.Factor;
                    if (speeds.TryGetValue(hop.Other, out var existing))
                    {
                        if (Math.Abs(existing - next) > Tolerance * Math.Max(1, Math.Abs(next)))
                            return null;
                        continue;
                    }

                    speeds[hop.Other] = next;
                    queue.Enqueue(hop.Other);
                }
            }

            foreach (var id in component)
            {
                if (!speeds.ContainsKey(id))
                    speeds[id] = 0;
            }

            return speeds;
        }

        private static void Charge(List<Node> flywheels, double surplus, double dt)
        {
            foreach (var wheel in flywheels)
            {
                if (surplus <= 0)
                    break;
                var room = (BalanceDefaults.FlywheelCapacityDuSeconds - wheel.Stored) / (BalanceDefaults.FlywheelChargeEfficiency * dt);
                var rate = Math.Min(Math.Min(BalanceDefaults.FlywheelMaxChargeDu, surplus), Math.Max(0, room));
                if (rate <= 0)
                    continue;
                wheel.Stored = Math.Min(BalanceDefaults.FlywheelCapacityDuSeconds, wheel.Stored + rate * BalanceDefaults.FlywheelChargeEfficiency * dt);
                wheel.FlywheelFlowDu = rate;
                surplus -= rate;
            }
        }

        private static void Discharge(List<Node> flywheels, double deficit, double dt)
        {
            var remaining = deficit;
            foreach (var wheel in flywheels.OrderByDescending(w => w.Stored).ThenBy(w => w.Id, StringComparer.Ordinal))
            {
                if (remaining <= 0)
                    break;
                var rate = Math.Min(Math.Min(BalanceDefaults.FlywheelMaxDischargeDu, wheel.Stored / dt), remaining);
                wheel.Stored = Math.Max(0, wheel.Stored - rate * dt);
                wheel.FlywheelFlowDu = -rate;
                remaining -= rate;
            }
        }

        private void Finish(List<string> component, int supply, int reserved, KineticStop stop, Dictionary<string, double>? speeds)
        {
            foreach (var id in component)
            {
                var node = _nodes[id];
                node.SupplyDu = supply;
                node.ReservedDu = reserved;
                node.Stop = stop;
                node.Speed = speeds != null && speeds.TryGetValue(id, out var speed) ? speed : 0;
                node.Producing = stop == KineticStop.Working && node.Role == NodeRole.Consumer && node.Enabled;
                if (stop != KineticStop.Working && node.Role == NodeRole.Source)
                    node.Contributing = false;
            }
        }

        private enum NodeRole
        {
            Source,
            Transmission,
            Consumer,
            Clutch,
            Governor,
            Flywheel
        }

        private sealed class Node
        {
            public Node(string id, NodeRole role, int amount, string placementRule)
            {
                Id = id;
                Role = role;
                Amount = amount;
                PlacementRule = placementRule;
                Enabled = true;
                MaxRpm = int.MaxValue;
                NativeRpm = BalanceDefaults.MilestoneRpm;
            }

            public string Id { get; }
            public NodeRole Role { get; }
            public int Amount { get; set; }
            public string PlacementRule { get; }
            public int NativeRpm { get; set; }
            public int MaxRpm { get; set; }
            public bool Enabled { get; set; }
            public bool ClutchClosed { get; set; }
            public double Stored { get; set; }
            public int SupplyDu { get; set; }
            public int ReservedDu { get; set; }
            public bool Producing { get; set; }
            public bool Contributing { get; set; }
            public double Speed { get; set; }
            public double FlywheelFlowDu { get; set; }
            public KineticStop Stop { get; set; }
        }

        private readonly struct Edge
        {
            public Edge(string from, string to, double factor)
            {
                From = from;
                To = to;
                Factor = factor;
            }

            public string From { get; }
            public string To { get; }
            public double Factor { get; }
        }

        private readonly struct Hop
        {
            public Hop(string other, double factor)
            {
                Other = other;
                Factor = factor;
            }

            public string Other { get; }
            public double Factor { get; }
        }
    }
}
