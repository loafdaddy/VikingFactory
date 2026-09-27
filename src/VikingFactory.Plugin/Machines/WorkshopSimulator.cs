using System.Collections.Generic;
using UnityEngine;
using VikingFactory.Core.Kinetics;

namespace VikingFactory.Machines
{
    /// <summary>
    /// Rebuilds the live kinetic graph at 5 Hz. Water drive is granted only to wheels the game allowed in water.
    /// </summary>
    public class WorkshopSimulator : MonoBehaviour
    {
        private const float TickSeconds = 0.2f;
        private const float PortReach = 0.45f;
        private const float DockReach = 1.25f;
        private float _wait;
        private readonly Dictionary<string, bool> _waterSites = new Dictionary<string, bool>();

        private void Update()
        {
            _wait += Time.deltaTime;
            if (_wait < TickSeconds)
                return;
            _wait = 0f;
            Solve();
        }

        private void Solve()
        {
            var machines = WorkshopRegistry.All;
            var network = new KineticNetwork();
            network.Placement = new AllowedWater(this);
            _waterSites.Clear();

            for (var i = 0; i < machines.Count; i++)
            {
                var machine = machines[i];
                machine.InputNeighbor = null;
                machine.OutputNeighbor = null;
                var id = Id(machine);
                switch (machine.Role)
                {
                    case MachineRole.Crank:
                        network.AddSource(id, VikingFactory.Core.BalanceDefaults.HandCrankDu);
                        network.SetEnabled(id, machine.CrankHeld);
                        break;
                    case MachineRole.WaterWheel:
                        network.AddSource(id, VikingFactory.Core.BalanceDefaults.WaterWheelDu, "valid-water");
                        _waterSites[id] = true;
                        network.SetEnabled(id, true);
                        break;
                    case MachineRole.Shaft:
                        network.AddTransmission(id);
                        break;
                    case MachineRole.Clutch:
                        network.AddClutch(id);
                        network.SetClutchClosed(id, machine.ClutchClosed);
                        break;
                    case MachineRole.Belt:
                        network.AddConsumer(id, VikingFactory.Core.BalanceDefaults.TimberBeltDuPer2M);
                        break;
                    case MachineRole.Feeder:
                        network.AddConsumer(id, VikingFactory.Core.BalanceDefaults.BronzeFeederDu);
                        break;
                    case MachineRole.Basket:
                        network.AddTransmission(id);
                        break;
                }
            }

            for (var i = 0; i < machines.Count; i++)
            {
                var machine = machines[i];
                var other = FindPortNeighbor(machines, machine.Forward, machine);
                if (other == null)
                    continue;
                machine.OutputNeighbor = other;
                other.InputNeighbor = machine;
                network.Connect(Id(machine), Id(other));
            }

            network.Solve();

            for (var i = 0; i < machines.Count; i++)
            {
                var machine = machines[i];
                var id = Id(machine);
                machine.Supply = network.SupplyDu(id);
                machine.Reserved = network.ReservedDu(id);
                machine.Producing = network.IsProducing(id);
                machine.Stop = Describe(network.StopReason(id), machine);
                if (machine.Role == MachineRole.Feeder && machine.Producing && machine.IsLocalOwner())
                    TryMove(machine);
            }
        }

        private static void TryMove(WorkshopMachine feeder)
        {
            if (!feeder.IsLocalOwner())
                return;
            var clock = feeder.GetComponent<FeederClock>();
            if (clock == null)
            {
                clock = feeder.gameObject.AddComponent<FeederClock>();
                clock.Next = 0f;
            }
            if (Time.time < clock.Next)
                return;

            var sourceBasket = feeder.InputNeighbor != null && feeder.InputNeighbor.Role == MachineRole.Basket ? feeder.InputNeighbor : null;
            var destBasket = feeder.OutputNeighbor != null && feeder.OutputNeighbor.Role == MachineRole.Basket ? feeder.OutputNeighbor : null;
            var sourceChest = sourceBasket == null ? Dock(feeder.Back) : null;
            var destChest = destBasket == null ? Dock(feeder.Forward) : null;

            if (ItemMover.TryMove(sourceBasket, sourceChest, destBasket, destChest))
            {
                feeder.ItemsMoved += 1;
                clock.Next = Time.time + 2f;
            }
        }

        private static Container Dock(Transform port)
        {
            if (port == null)
                return null;
            var hits = Physics.OverlapSphere(port.position, DockReach);
            Container best = null;
            var bestDistance = DockReach;
            for (var i = 0; i < hits.Length; i++)
            {
                var container = hits[i].GetComponentInParent<Container>();
                if (container == null)
                    continue;
                var distance = Vector3.Distance(port.position, container.transform.position);
                if (distance < bestDistance)
                {
                    best = container;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private static WorkshopMachine FindPortNeighbor(IReadOnlyList<WorkshopMachine> machines, Transform port, WorkshopMachine self)
        {
            if (port == null)
                return null;
            WorkshopMachine best = null;
            var bestDistance = PortReach;
            for (var i = 0; i < machines.Count; i++)
            {
                var candidate = machines[i];
                if (candidate == self)
                    continue;
                var distance = DistanceToPort(port, candidate);
                if (distance < bestDistance)
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private static float DistanceToPort(Transform port, WorkshopMachine candidate)
        {
            var forward = Vector3.Distance(port.position, candidate.Forward.position);
            var back = Vector3.Distance(port.position, candidate.Back.position);
            return forward < back ? forward : back;
        }

        private static string Id(WorkshopMachine machine)
        {
            return machine.GetInstanceID().ToString();
        }

        private static string Describe(KineticStop stop, WorkshopMachine machine)
        {
            switch (stop)
            {
                case KineticStop.Working:
                    return "Turning.";
                case KineticStop.NoPower:
                    return "No power.";
                case KineticStop.Overloaded:
                    return "Overloaded.";
                case KineticStop.InvalidPlacement:
                    return "Water wheel is not in a verified site.";
                default:
                    return "Disconnected.";
            }
        }

        private sealed class AllowedWater : IPlacementProbe
        {
            private readonly WorkshopSimulator _simulator;

            public AllowedWater(WorkshopSimulator simulator)
            {
                _simulator = simulator;
            }

            public bool IsValid(string nodeId, string ruleId)
            {
                return ruleId == "valid-water" && _simulator._waterSites.ContainsKey(nodeId);
            }
        }
    }

    public class FeederClock : MonoBehaviour
    {
        public float Next;
    }
}
