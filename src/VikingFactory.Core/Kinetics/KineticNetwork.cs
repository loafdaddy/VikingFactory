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
        InvalidPlacement
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
    /// Fixed-load kinetic graph. One supported speed. Shafts reserve nothing.
    /// A closed clutch disconnects its downstream branch.
    /// </summary>
    public sealed class KineticNetwork
    {
        private readonly Dictionary<string, Node> _nodes = new Dictionary<string, Node>();
        private readonly List<Edge> _edges = new List<Edge>();

        public int Rpm { get; } = BalanceDefaults.MilestoneRpm;
        public IPlacementProbe Placement { get; set; } = new RejectingPlacementProbe();

        public string AddSource(string id, int capacityDu, string placementRule = "")
        {
            return Add(id, NodeRole.Source, capacityDu, placementRule);
        }

        public string AddTransmission(string id)
        {
            return Add(id, NodeRole.Transmission, 0, "");
        }

        public string AddConsumer(string id, int loadDu)
        {
            return Add(id, NodeRole.Consumer, loadDu, "");
        }

        public string AddClutch(string id)
        {
            return Add(id, NodeRole.Clutch, 0, "");
        }

        public void Connect(string upstream, string downstream)
        {
            if (!_nodes.ContainsKey(upstream) || !_nodes.ContainsKey(downstream))
                throw new InvalidOperationException("Both endpoints must exist.");
            _edges.Add(new Edge(upstream, downstream));
        }

        public void SetEnabled(string id, bool enabled)
        {
            _nodes[id].Enabled = enabled;
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
            foreach (var node in _nodes.Values)
            {
                node.SupplyDu = 0;
                node.ReservedDu = 0;
                node.Producing = false;
                node.Stop = KineticStop.NoPower;
            }

            var seen = new HashSet<string>();
            foreach (var id in _nodes.Keys)
            {
                if (!seen.Add(id))
                    continue;
                var component = Collect(id, seen);
                Evaluate(component);
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

        private string Add(string id, NodeRole role, int amount, string placementRule)
        {
            if (_nodes.ContainsKey(id))
                throw new InvalidOperationException("Duplicate node " + id);
            _nodes[id] = new Node(id, role, amount, placementRule);
            return id;
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
                foreach (var next in Neighbors(id))
                {
                    if (seen.Add(next))
                        stack.Push(next);
                }
            }

            return component;
        }

        private IEnumerable<string> Neighbors(string id)
        {
            var node = _nodes[id];
            foreach (var edge in _edges)
            {
                if (edge.From == id)
                {
                    if (node.Role == NodeRole.Clutch && node.ClutchClosed)
                        continue;
                    yield return edge.To;
                }
                else if (edge.To == id)
                {
                    var other = _nodes[edge.From];
                    if (other.Role == NodeRole.Clutch && other.ClutchClosed)
                        continue;
                    yield return edge.From;
                }
            }
        }

        private void Evaluate(List<string> component)
        {
            var supply = 0;
            var reserved = 0;
            var placementBlocked = false;

            foreach (var id in component)
            {
                var node = _nodes[id];
                if (node.Role == NodeRole.Source && node.Enabled)
                {
                    if (!string.IsNullOrEmpty(node.PlacementRule) && !Placement.IsValid(node.Id, node.PlacementRule))
                    {
                        placementBlocked = true;
                        continue;
                    }

                    supply += node.Amount;
                }

                if (node.Role == NodeRole.Consumer && node.Enabled)
                    reserved += node.Amount;
            }

            var stop = KineticStop.Working;
            if (supply <= 0)
                stop = placementBlocked ? KineticStop.InvalidPlacement : KineticStop.NoPower;
            else if (supply < reserved)
                stop = KineticStop.Overloaded;

            foreach (var id in component)
            {
                var node = _nodes[id];
                node.SupplyDu = supply;
                node.ReservedDu = reserved;
                node.Stop = stop;
                node.Producing = stop == KineticStop.Working && node.Role == NodeRole.Consumer && node.Enabled;
            }
        }

        private enum NodeRole
        {
            Source,
            Transmission,
            Consumer,
            Clutch
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
            }

            public string Id { get; }
            public NodeRole Role { get; }
            public int Amount { get; }
            public string PlacementRule { get; }
            public bool Enabled { get; set; }
            public bool ClutchClosed { get; set; }
            public int SupplyDu { get; set; }
            public int ReservedDu { get; set; }
            public bool Producing { get; set; }
            public KineticStop Stop { get; set; }
        }

        private readonly struct Edge
        {
            public Edge(string from, string to)
            {
                From = from;
                To = to;
            }

            public string From { get; }
            public string To { get; }
        }
    }
}
