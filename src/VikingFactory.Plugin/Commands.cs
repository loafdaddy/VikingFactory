using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using VikingFactory.Core.Items;
using VikingFactory.Machines;

namespace VikingFactory
{
    /// <summary>
    /// vf status | network | exportcatalog | coverage | validate | recover.
    /// Recovery lists held items and never spawns missing ones.
    /// </summary>
    public sealed class WorkshopCommand : ConsoleCommand
    {
        public override string Name => "vf";

        public override string Help => "VikingFactory: status, network, exportcatalog, coverage, validate, recover";

        public override void Run(string[] args)
        {
            var verb = args.Length > 0 ? args[0].ToLowerInvariant() : "status";
            try
            {
                switch (verb)
                {
                    case "network": Network(); break;
                    case "exportcatalog": Print("Catalogue: " + CatalogueExport.Write()); break;
                    case "coverage":
                        var path = CoverageExport.Write(out var summary);
                        Print("Coverage: " + path);
                        Print(summary);
                        break;
                    case "validate": Validate(); break;
                    case "recover": Recover(args.Skip(1).ToArray()); break;
                    default: Status(); break;
                }
            }
            catch (Exception ex)
            {
                Print("vf " + verb + " failed: " + ex.Message);
            }
        }

        public override List<string> CommandOptionList()
        {
            return new List<string> { "status", "network", "exportcatalog", "coverage", "validate", "recover" };
        }

        private static void Print(string text)
        {
            if (global::Console.instance != null)
                global::Console.instance.Print(text);
            Debug.Log("[VikingFactory] " + text);
        }

        private static void Status()
        {
            var machines = WorkshopRegistry.All;
            Print("VikingFactory " + VikingFactoryPlugin.PluginVersion + ": " + machines.Count + " loaded machines, " + machines.Count(m => m.IsLocalOwner()) + " owned here.");
            foreach (var group in machines.GroupBy(m => m.Spec != null ? m.Spec.Name : "?").OrderBy(g => g.Key))
                Print("  " + group.Key + ": " + group.Count() + ", producing " + group.Count(m => m.Producing));
            var sim = WorkshopSimulator.Instance;
            if (sim != null)
                Print("Step " + sim.LastStepMs.ToString("0.00") + " ms, worst recent " + sim.WorstStepMs.ToString("0.00") + " ms, dropped time jumps " + sim.Discontinuities + ". Preset " + (WorkshopConfig.PresetEntry != null ? WorkshopConfig.PresetEntry.Value.ToString() : "?"));
        }

        private static void Network()
        {
            foreach (var group in WorkshopRegistry.All.GroupBy(m => m.Component).OrderBy(g => g.Key))
            {
                var first = group.First();
                Print("Line " + group.Key + ": " + group.Count() + " pieces, " + first.Supply + " DU / " + first.Reserved + " DU, " + first.Stop
                    + " RPM " + group.Max(m => Math.Abs(m.Speed)).ToString("0"));
            }
        }

        private static void Validate()
        {
            var problems = 0;
            foreach (var spec in MachineCatalog.All)
            {
                if (PrefabManager.Instance.GetPrefab(spec.Id) == null)
                {
                    Print("Not registered: " + spec.Id);
                    problems++;
                }
            }

            Print("Boss keys: " + Progression.Resolve());
            foreach (var name in SmelterEndpoint.Reviewed.Concat(CookingEndpoint.Reviewed).Concat(new[] { "fermenter", "piece_beehive", "piece_sapcollector" }))
            {
                if (PrefabManager.Instance.GetPrefab(name) == null)
                {
                    Print("Reviewed station missing from this game: " + name);
                    problems++;
                }
            }

            foreach (var machine in WorkshopRegistry.All)
            {
                var status = machine.Behaviour != null ? machine.Behaviour.Status() : "";
                if (status.Contains("Holding") || status.Contains("Recovery"))
                {
                    Print("Held item at " + machine.transform.position.ToString("0") + " (" + machine.Spec.Name + "): " + status.Replace("\n", " "));
                    problems++;
                }
            }

            Print(problems == 0 ? "Validate: no problems." : "Validate: " + problems + " notes above.");
        }

        private static void Recover(string[] args)
        {
            if (!SynchronizationManager.Instance.PlayerIsAdmin)
            {
                Print("Recovery is admin-only.");
                return;
            }

            var player = Player.m_localPlayer;
            if (player == null)
                return;
            var nearest = WorkshopRegistry.All.Where(m => m.IsLocalOwner()).OrderBy(m => Vector3.Distance(m.transform.position, player.transform.position)).FirstOrDefault();
            if (nearest == null || Vector3.Distance(nearest.transform.position, player.transform.position) > 5f)
            {
                Print("Stand within 5 m of a machine this peer owns.");
                return;
            }

            nearest.Behaviour.EnsureLoaded();
            if (args.Length == 0 || args[0] != "confirm")
            {
                Print(nearest.Spec.Name + ": " + nearest.Behaviour.Status().Replace("\n", " "));
                Print("Run 'vf recover confirm' to drop everything this machine holds, once, at its position.");
                return;
            }

            var held = nearest.Behaviour.Drain();
            nearest.Behaviour.SaveNow();
            foreach (var stack in held)
                ItemBridge.Drop(stack, nearest.transform.position);
            Print("Dropped " + held.Sum(s => s.Count) + " held items from " + nearest.Spec.Name + ".");
        }
    }
}
