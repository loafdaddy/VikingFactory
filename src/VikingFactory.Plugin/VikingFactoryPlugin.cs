using System;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Bootstrap;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;
using VikingFactory.Machines;

namespace VikingFactory
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class VikingFactoryPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.vikingfactory";
        public const string PluginName = "VikingFactory";
        public const string PluginVersion = "0.3.0";

        private static readonly string[] CompetingHints = { "hopper", "pipe", "automatic", "autofuel", "auto_fuel", "craftfromcontainers", "autofeed", "lazyviking", "serversideqol" };

        private void Awake()
        {
            WorkshopConfig.Bind(Config);
            var localization = new CustomLocalization(Info.Metadata);
            foreach (var spec in MachineCatalog.All)
            {
                localization.AddTranslation(spec.Token, spec.Name);
                localization.AddTranslation(spec.DescriptionToken, spec.Description);
            }

            LocalizationManager.Instance.AddLocalization(localization);
            gameObject.AddComponent<WorkshopSimulator>();
            CommandManager.Instance.AddConsoleCommand(new ExportCatalogueCommand());
            CommandManager.Instance.AddConsoleCommand(new WorkshopCommand());
            PrefabManager.OnVanillaPrefabsAvailable += AddPieces;
            Logger.LogInfo(PluginName + " " + PluginVersion + " loaded. Waiting for vanilla prefabs.");
        }

        private void Start()
        {
            // Other automation mods are not blocked. They are named so a report can say which ones were present.
            foreach (var info in Chainloader.PluginInfos.Values)
            {
                var id = (info.Metadata.GUID + " " + info.Metadata.Name).ToLowerInvariant();
                if (info.Metadata.GUID != PluginGuid && CompetingHints.Any(id.Contains))
                    Logger.LogWarning("Automation-related mod present: " + info.Metadata.Name + " " + info.Metadata.Version + ". Combined behaviour is untested.");
            }
        }

        private void AddPieces()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= AddPieces;
            try
            {
                Logger.LogInfo("Catalogue exported to " + CatalogueExport.Write());
                Logger.LogInfo("Material probe wrote " + MaterialProbe.Write());
            }
            catch (Exception ex)
            {
                Logger.LogError("Catalogue export failed: " + ex.Message);
            }

            Logger.LogInfo("Boss keys: " + Progression.Resolve());
            Planting.Reset();

            if (PrefabManager.Instance.GetPrefab("Wood") == null)
            {
                Logger.LogError("Prefab 'Wood' was not found. No workshop pieces were registered.");
                return;
            }

            var registered = 0;
            foreach (var spec in MachineCatalog.All)
            {
                try
                {
                    if (Add(spec))
                        registered++;
                }
                catch (Exception ex)
                {
                    Logger.LogError("Registering " + spec.Id + " failed: " + ex);
                }
            }

            Logger.LogInfo("Registered " + registered + " of " + MachineCatalog.All.Count + " workshop pieces.");
        }

        private bool Add(MachineSpec spec)
        {
            foreach (var cost in spec.Costs)
            {
                if (PrefabManager.Instance.GetPrefab(cost.Item) != null)
                    continue;
                Logger.LogError("Prefab '" + cost.Item + "' was not found. " + spec.Id + " was not registered.");
                return false;
            }

            var assets = Path.Combine(Path.GetDirectoryName(Info.Location) ?? "", "Assets");
            var config = new PieceConfig
            {
                Name = spec.Token,
                Description = spec.DescriptionToken,
                PieceTable = PieceTables.Hammer,
                CraftingStation = Station(spec.Station),
                Category = "Crafting",
                Usage = new[] { "Crafting" },
                Icon = LoadIcon(Path.Combine(assets, Path.Combine("icons", spec.Id + ".png")))
            };
            foreach (var cost in spec.Costs)
                config.AddRequirement(cost.Item, cost.Amount, true);

            var piece = new CustomPiece(spec.Id, true, config);
            var prefab = piece.PiecePrefab;
            var color = new Color(0.55f, 0.38f, 0.2f);
            prefab.transform.localScale = Vector3.one * 0.6f;
            var renderer = prefab.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = color;

            if (!string.IsNullOrEmpty(spec.Model))
            {
                string report;
                if (GlbPieceVisual.TryAttach(prefab, Path.Combine(assets, spec.Model), spec.Boxes, out report))
                {
                    prefab.transform.localScale = Vector3.one;
                    Logger.LogInfo("Attached " + spec.Id + " from " + spec.Model + ". " + report);
                }
                else
                    Logger.LogWarning("Kept the block visual for " + spec.Id + ". " + report);
            }

            var pieceLayer = LayerMask.NameToLayer("piece");
            if (pieceLayer >= 0)
            {
                foreach (var t in prefab.GetComponentsInChildren<Transform>(true))
                    t.gameObject.layer = pieceLayer;
            }

            var wear = prefab.GetComponent<WearNTear>() ?? prefab.AddComponent<WearNTear>();
            wear.m_health = spec.Health;
            wear.m_materialType = spec.Material;
            wear.m_noRoofWear = false;
            wear.m_noSupportWear = true;

            var machine = prefab.GetComponent<WorkshopMachine>() ?? prefab.AddComponent<WorkshopMachine>();
            machine.SpecId = spec.Id;
            machine.Spec = spec;
            WorkshopMachine.AddSnapPoints(prefab);

            PieceManager.Instance.AddPiece(piece);
            Logger.LogInfo("Registered " + spec.Id + ". Valid=" + piece.IsValid());
            return true;
        }

        private static string Station(string name)
        {
            switch (name)
            {
                case "Forge": return CraftingStations.Forge;
                case "Stonecutter": return CraftingStations.Stonecutter;
                case "ArtisanTable": return CraftingStations.ArtisanTable;
                case "BlackForge": return CraftingStations.BlackForge;
                default: return CraftingStations.Workbench;
            }
        }

        private static Sprite LoadIcon(string path)
        {
            Texture2D texture;
            if (!PngIcon.TryLoad(path, out texture))
                return SolidIcon(new Color(0.55f, 0.38f, 0.2f));
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite SolidIcon(Color color)
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
