using System.Collections.Generic;
using UnityEngine;

namespace VikingFactory.Machines
{
    public enum MachineRole
    {
        Crank,
        Shaft,
        Clutch,
        WaterWheel,
        Belt,
        Feeder,
        Basket
    }

    /// <summary>
    /// One placed workshop piece. The graph is rebuilt from live ports, not saved.
    /// </summary>
    public class WorkshopMachine : MonoBehaviour, Hoverable, Interactable
    {
        public MachineRole Role;
        public Transform Forward;
        public Transform Back;
        public int Supply;
        public int Reserved;
        public string Stop = "No power";
        public bool Producing;
        public WorkshopMachine InputNeighbor;
        public WorkshopMachine OutputNeighbor;
        public int ItemsMoved;

        private bool _joined;
        private float _heldUntil;
        private ZNetView _view;

        public bool CrankHeld
        {
            get { return Time.time < _heldUntil; }
        }

        public bool ClutchClosed
        {
            get { return ReadInt("vf_clutch") == 1; }
        }

        private void Awake()
        {
            BindPorts();
            _view = GetComponent<ZNetView>();
        }

        public void BindPorts()
        {
            var kineticOut = FindMarker("Kinetic_Out");
            var kineticIn = FindMarker("Kinetic_In");
            var kineticRight = FindMarker("Kinetic_Right");
            var kineticLeft = FindMarker("Kinetic_Left");
            if (kineticOut != null && kineticIn != null)
            {
                Forward = kineticOut;
                Back = kineticIn;
                return;
            }

            if (kineticRight != null && kineticLeft != null)
            {
                Forward = kineticRight;
                Back = kineticLeft;
                return;
            }

            var pickup = FindMarker("Pickup");
            var dropoff = FindMarker("Dropoff");
            if (pickup != null && dropoff != null)
            {
                Back = pickup;
                Forward = dropoff;
                return;
            }

            if (kineticOut != null)
            {
                Forward = kineticOut;
                if (Back == null)
                    Back = CreatePort("port_back", Vector3.zero);
                return;
            }

            if (Forward == null)
                Forward = CreatePort("port_forward", new Vector3(0f, 0f, 0.5f));
            if (Back == null)
                Back = CreatePort("port_back", new Vector3(0f, 0f, -0.5f));
        }

        private Transform FindMarker(string markerName)
        {
            var transforms = GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == markerName)
                    return transforms[i];
            }

            return null;
        }

        private void Update()
        {
            if (_joined)
                return;
            if (_view == null || !_view.IsValid() || _view.GetZDO() == null)
                return;
            _joined = true;
            WorkshopRegistry.Add(this);
        }

        private void OnDestroy()
        {
            if (_joined)
                WorkshopRegistry.Remove(this);
        }

        public string GetHoverName()
        {
            return DisplayName();
        }

        public string GetHoverText()
        {
            var line = DisplayName() + "\n" + Supply + " DU supply / " + Reserved + " DU reserved\n" + Stop;
            if (Role == MachineRole.Crank)
                line += "\nHold interact to turn. Releasing stops the crank.";
            if (Role == MachineRole.Clutch)
                line += ClutchClosed ? "\nClosed. The branch beyond is disconnected." : "\nOpen. Power passes through.";
            if (Role == MachineRole.Belt)
                line += "\nDraws load. Items are moved by a feeder, not by the belt surface yet.";
            if (Role == MachineRole.Feeder)
                line += "\nMoved " + ItemsMoved + " items.";
            if (Role == MachineRole.WaterWheel)
                line += "\nPlace in water. The game rejects dry ground.";
            return line;
        }

        public float GetHoverOffset()
        {
            return 1.5f;
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (Role == MachineRole.Crank && hold)
            {
                _heldUntil = Time.time + 0.35f;
                return true;
            }

            if (Role == MachineRole.Clutch && !hold)
            {
                if (_view != null && _view.IsValid() && !_view.IsOwner())
                    _view.ClaimOwnership();
                var zdo = _view != null ? _view.GetZDO() : null;
                if (zdo == null)
                    return false;
                zdo.Set("vf_clutch", ClutchClosed ? 0 : 1);
                return true;
            }

            return false;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }

        public ZDO GetZdo()
        {
            if (_view == null || !_view.IsValid())
                return null;
            return _view.GetZDO();
        }

        public bool IsLocalOwner()
        {
            return _view != null && _view.IsValid() && _view.IsOwner();
        }

        private int ReadInt(string key)
        {
            var zdo = GetZdo();
            return zdo == null ? 0 : zdo.GetInt(key, 0);
        }

        private Transform CreatePort(string portName, Vector3 local)
        {
            var port = new GameObject(portName);
            port.transform.SetParent(transform, false);
            port.transform.localPosition = local;
            return port.transform;
        }

        private string DisplayName()
        {
            switch (Role)
            {
                case MachineRole.Crank: return "Hand crank";
                case MachineRole.Shaft: return "Wooden shaft";
                case MachineRole.Clutch: return "Clutch";
                case MachineRole.WaterWheel: return "Water wheel";
                case MachineRole.Belt: return "Timber belt";
                case MachineRole.Feeder: return "Bronze feeder";
                case MachineRole.Basket: return "Catch basket";
                default: return "Workshop piece";
            }
        }
    }

    public static class WorkshopRegistry
    {
        private static readonly List<WorkshopMachine> Machines = new List<WorkshopMachine>();

        public static IReadOnlyList<WorkshopMachine> All
        {
            get { return Machines; }
        }

        public static void Add(WorkshopMachine machine)
        {
            if (!Machines.Contains(machine))
                Machines.Add(machine);
        }

        public static void Remove(WorkshopMachine machine)
        {
            Machines.Remove(machine);
        }
    }
}
