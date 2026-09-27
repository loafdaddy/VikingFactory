using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using Jotunn.Extensions;
using Jotunn.Managers;
using VikingFactory.Core.Production;
using VikingFactory.Core.Runtime;

namespace VikingFactory
{
    /// <summary>
    /// Server-enforced settings through Jötunn's admin-only sync. Clients cannot lower machine costs or
    /// turn on a supply the server's preset forbids.
    /// </summary>
    public static class WorkshopConfig
    {
        public static ConfigEntry<Preset> PresetEntry;
        public static ConfigEntry<bool> BedrockStone;
        public static ConfigEntry<int> MiningToolTier;
        public static ConfigEntry<int> SawToolTier;
        public static ConfigEntry<string> ModdedRecipeAllowlist;

        public static void Bind(ConfigFile config)
        {
            PresetEntry = config.BindConfig("Balance", "Preset", Preset.BalancedIndustry,
                "BalancedIndustry adds coppice, forage beds, and late deep extraction. VanillaSupply keeps the same automation without them.", true);
            BedrockStone = config.BindConfig("Balance", "BedrockQuarry", true, "Allow the renewable bedrock quarry.", true);
            MiningToolTier = config.BindConfig("Balance", "MiningToolTier", 2, "Pickaxe tier the mining head strikes with. 2 is an iron pickaxe.", true);
            SawToolTier = config.BindConfig("Balance", "SawToolTier", 2, "Axe tier the timber saw cuts with.", true);
            ModdedRecipeAllowlist = config.BindConfig("Compatibility", "ModdedRecipeAllowlist", "",
                "Comma-separated recipe names from other mods the recipe mill may run. Unlisted modded recipes are refused.", true);
        }

        public static FeatureFlags Flags => FeatureFlags.For(PresetEntry != null ? PresetEntry.Value : Preset.BalancedIndustry, BedrockStone == null || BedrockStone.Value);

        public static bool ModdedRecipeAllowed(string recipeName)
        {
            if (ModdedRecipeAllowlist == null || string.IsNullOrEmpty(ModdedRecipeAllowlist.Value))
                return false;
            foreach (var part in ModdedRecipeAllowlist.Value.Split(','))
            {
                if (part.Trim() == recipeName)
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Boss milestones resolved from the boss prefabs' own defeat keys in this game. Nothing is guessed:
    /// an unresolved milestone keeps its machines locked.
    /// </summary>
    public static class Progression
    {
        private static readonly Dictionary<string, string> Bosses = new Dictionary<string, string>
        {
            { "Eikthyr", "Eikthyr" },
            { "Elder", "gd_king" },
            { "Bonemass", "Bonemass" },
            { "Moder", "Dragon" },
            { "Yagluth", "GoblinKing" },
            { "Queen", "SeekerQueen" },
            { "Fader", "Fader" }
        };

        public static readonly ProgressionGate Gate = new ProgressionGate();

        public static string Resolve()
        {
            var report = new List<string>();
            foreach (var pair in Bosses)
            {
                var prefab = PrefabManager.Instance.GetPrefab(pair.Value);
                var character = prefab != null ? prefab.GetComponent<Character>() : null;
                var key = character != null ? character.m_defeatSetGlobalKey : "";
                Gate.Resolve(pair.Key, key);
                report.Add(pair.Key + "=" + (string.IsNullOrEmpty(key) ? "UNRESOLVED" : key));
            }

            return string.Join(", ", report.ToArray());
        }

        public static bool IsUnlocked(string[] milestones, out string missing)
        {
            missing = "";
            if (milestones == null || milestones.Length == 0)
                return true;
            if (ZoneSystem.instance == null)
            {
                missing = "world not loaded";
                return false;
            }

            return Gate.IsUnlocked(milestones, key => ZoneSystem.instance.GetGlobalKey(key), out missing);
        }
    }
}
