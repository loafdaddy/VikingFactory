using System;
using System.IO;
using BepInEx;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using VikingFactory.Machines;

namespace VikingFactory
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    public class VikingFactoryPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.vikingfactory";
        public const string PluginName = "VikingFactory";
        public const string PluginVersion = "0.2.0";

        private void Awake()
        {
            var localization = new CustomLocalization(Info.Metadata);
            localization.AddTranslation("$piece_vf_marker", "Workshop marker");
            localization.AddTranslation("$piece_vf_marker_desc", "A timber marker. It does not move items yet.");
            localization.AddTranslation("$piece_vf_crank", "Hand crank");
            localization.AddTranslation("$piece_vf_crank_desc", "Hold interact to turn the connected line. It stops when you let go.");
            localization.AddTranslation("$piece_vf_shaft", "Wooden shaft");
            localization.AddTranslation("$piece_vf_shaft_desc", "Carries rotation. Point the forward end at the next piece.");
            localization.AddTranslation("$piece_vf_clutch", "Clutch");
            localization.AddTranslation("$piece_vf_clutch_desc", "Interact to disconnect the branch beyond the forward end.");
            localization.AddTranslation("$piece_vf_wheel", "Water wheel");
            localization.AddTranslation("$piece_vf_wheel_desc", "Stands on the ground. It is meant for a stream.");
            localization.AddTranslation("$piece_vf_belt", "Timber belt");
            localization.AddTranslation("$piece_vf_belt_desc", "Draws a little power along the line. A feeder moves the items.");
            localization.AddTranslation("$piece_vf_feeder", "Bronze feeder");
            localization.AddTranslation("$piece_vf_feeder_desc", "Moves one item every two seconds from the back port to the forward port while the line turns.");
            localization.AddTranslation("$piece_vf_basket", "Catch basket");
            localization.AddTranslation("$piece_vf_basket_desc", "A small buffer. Point a feeder at it, or stand it against a chest.");
            LocalizationManager.Instance.AddLocalization(localization);

            gameObject.AddComponent<WorkshopSimulator>();
            CommandManager.Instance.AddConsoleCommand(new ExportCatalogueCommand());
            PrefabManager.OnVanillaPrefabsAvailable += AddPieces;
            Logger.LogInfo(PluginName + " " + PluginVersion + " loaded. Waiting for vanilla prefabs.");
        }

        private void AddPieces()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= AddPieces;
            try
            {
                var catalogue = CatalogueExport.Write();
                Logger.LogInfo("Catalogue exported to " + catalogue);
                var materials = MaterialProbe.Write();
                Logger.LogInfo("Material probe wrote " + materials);
            }
            catch (Exception ex)
            {
                Logger.LogError("Catalogue export failed: " + ex.Message);
            }

            if (PrefabManager.Instance.GetPrefab("Wood") == null)
            {
                Logger.LogError("Prefab 'Wood' was not found. No workshop pieces were registered.");
                return;
            }

            Add("vf_marker", "$piece_vf_marker", "$piece_vf_marker_desc", null, new Vector3(0.6f, 0.6f, 0.6f), new Color(0.55f, 0.38f, 0.2f), new[] { Req("Wood", 2) }, null, null);
            Add("vf_crank", "$piece_vf_crank", "$piece_vf_crank_desc", MachineRole.Crank, new Vector3(0.4f, 0.9f, 0.4f), new Color(0.62f, 0.42f, 0.22f), new[] { Req("Wood", 6), Req("LeatherScraps", 2) }, "vf_hand_crank.glb", new[]
            {
                Box(new Vector3(0f, 0.55f, 0f), new Vector3(0.7f, 1.1f, 0.65f))
            });
            Add("vf_shaft", "$piece_vf_shaft", "$piece_vf_shaft_desc", MachineRole.Shaft, new Vector3(0.2f, 0.2f, 2f), new Color(0.45f, 0.3f, 0.16f), new[] { Req("Wood", 2) }, "vf_shaft_2m.glb", new[]
            {
                Box(new Vector3(0f, 0.3f, 0f), new Vector3(0.42f, 0.6f, 2f))
            });
            Add("vf_clutch", "$piece_vf_clutch", "$piece_vf_clutch_desc", MachineRole.Clutch, new Vector3(0.45f, 0.45f, 0.7f), new Color(0.55f, 0.4f, 0.18f), new[] { Req("Wood", 4), Req("Bronze", 1) }, "vf_clutch.glb", new[]
            {
                Box(new Vector3(0f, 0.35f, 0f), new Vector3(0.5f, 0.7f, 0.8f))
            });
            Add("vf_water_wheel", "$piece_vf_wheel", "$piece_vf_wheel_desc", MachineRole.WaterWheel, new Vector3(2f, 2f, 0.35f), new Color(0.35f, 0.28f, 0.16f), new[] { Req("Wood", 30), Req("RoundLog", 10), Req("Bronze", 4), Req("DeerHide", 4) }, "vf_water_wheel.glb", new[]
            {
                Box(new Vector3(-0.95f, 1.075f, 0f), new Vector3(0.3f, 2.15f, 1.95f)),
                Box(new Vector3(0.95f, 1.075f, 0f), new Vector3(0.3f, 2.15f, 1.95f))
            });
            Add("vf_belt", "$piece_vf_belt", "$piece_vf_belt_desc", MachineRole.Belt, new Vector3(0.7f, 0.15f, 2f), new Color(0.42f, 0.28f, 0.14f), new[] { Req("Wood", 4), Req("LeatherScraps", 2), Req("BronzeNails", 2) }, "vf_conveyor_2m.glb", new[]
            {
                Box(new Vector3(0f, 0.73f, 0f), new Vector3(1.1f, 0.25f, 2f))
            });
            Add("vf_feeder", "$piece_vf_feeder", "$piece_vf_feeder_desc", MachineRole.Feeder, new Vector3(0.55f, 0.8f, 0.7f), new Color(0.72f, 0.48f, 0.22f), new[] { Req("Wood", 6), Req("Bronze", 2), Req("LeatherScraps", 2) }, "vf_feeder.glb", new[]
            {
                Box(new Vector3(0f, 0.4f, 0f), new Vector3(0.8f, 0.8f, 0.8f))
            });
            Add("vf_basket", "$piece_vf_basket", "$piece_vf_basket_desc", MachineRole.Basket, new Vector3(0.8f, 0.45f, 0.8f), new Color(0.5f, 0.34f, 0.18f), new[] { Req("Wood", 6), Req("LeatherScraps", 2) }, "vf_catch_basket.glb", new[]
            {
                Box(new Vector3(-0.48f, 0.4f, 0f), new Vector3(0.1f, 0.75f, 0.95f)),
                Box(new Vector3(0.48f, 0.4f, 0f), new Vector3(0.1f, 0.75f, 0.95f)),
                Box(new Vector3(0f, 0.4f, -0.43f), new Vector3(0.95f, 0.75f, 0.1f)),
                Box(new Vector3(0f, 0.4f, 0.43f), new Vector3(0.95f, 0.75f, 0.1f)),
                Box(new Vector3(0f, 0.05f, 0f), new Vector3(1f, 0.1f, 0.9f))
            });
        }

        private void Add(string name, string title, string description, MachineRole? role, Vector3 scale, Color color, Requirement[] requirements, string modelFile, GlbPieceVisual.BoxSpec[] boxes)
        {
            for (var i = 0; i < requirements.Length; i++)
            {
                if (PrefabManager.Instance.GetPrefab(requirements[i].Item) != null)
                    continue;
                Logger.LogError("Prefab '" + requirements[i].Item + "' was not found. " + name + " was not registered.");
                return;
            }

            // Valheim's water-piece flag rejects dry ground unless Shift is held, and it
            // forces the pivot 3 metres above the surface. The wheel's pivot is its feet.
            var config = new PieceConfig
            {
                Name = title,
                Description = description,
                PieceTable = PieceTables.Hammer,
                CraftingStation = CraftingStations.Workbench,
                Category = "Crafting",
                Usage = new[] { "Crafting" },
                Icon = LoadIcon(Path.Combine(Path.GetDirectoryName(Info.Location) ?? "", "Assets", "icons", name + ".png"), color)
            };
            for (var i = 0; i < requirements.Length; i++)
                config.AddRequirement(requirements[i].Item, requirements[i].Amount, true);

            var piece = new CustomPiece(name, true, config);
            var prefab = piece.PiecePrefab;
            prefab.transform.localScale = scale;
            var renderer = prefab.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = color;
            WorkshopMachine machine = null;
            if (role.HasValue)
            {
                machine = prefab.AddComponent<WorkshopMachine>();
                machine.Role = role.Value;
            }

            if (!string.IsNullOrEmpty(modelFile))
            {
                var path = Path.Combine(Path.GetDirectoryName(Info.Location) ?? "", "Assets", modelFile);
                string report;
                if (GlbPieceVisual.TryAttach(prefab, path, boxes, out report))
                {
                    prefab.transform.localScale = Vector3.one;
                    if (machine != null)
                        machine.BindPorts();
                    Logger.LogInfo("Attached " + name + " from " + modelFile + ". " + report);
                }
                else
                    Logger.LogWarning("Kept the block visual for " + name + ". " + report);
            }

            PieceManager.Instance.AddPiece(piece);
            Logger.LogInfo("Registered " + name + ". Valid=" + piece.IsValid());
        }

        private static GlbPieceVisual.BoxSpec Box(Vector3 center, Vector3 size)
        {
            return new GlbPieceVisual.BoxSpec { Center = center, Size = size };
        }

        private static Requirement Req(string item, int amount)
        {
            return new Requirement { Item = item, Amount = amount };
        }

        private static Sprite LoadIcon(string path, Color fallback)
        {
            Texture2D texture;
            if (!PngIcon.TryLoad(path, out texture))
                return SolidIcon(fallback);
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

        private struct Requirement
        {
            public string Item;
            public int Amount;
        }
    }
}
