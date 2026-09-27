using System.Collections.Generic;
using UnityEngine;
using VikingFactory.Core;

namespace VikingFactory.Machines
{
    public enum MachineRole
    {
        Marker,
        Crank,
        Shaft,
        Rope,
        Cog,
        ReversingCog,
        Clutch,
        WaterWheel,
        Belt,
        IronBelt,
        BeltCorner,
        Trough,
        Feeder,
        IronFeeder,
        CookingTender,
        Basket,
        Splitter,
        Merger,
        RecipeMill,
        Assembler,
        Quarry,
        CoppiceBed,
        TimberSaw,
        Planter,
        Harvester,
        FarmGantry,
        ForageBed,
        MiningHead,
        DeepExtractor,
        SteamEngine,
        ReinforcedSteam,
        WaterIntake,
        SailWheel,
        Governor,
        Flywheel,
        EitrMotor,
        OvenExtension,
        FeedGate,
        CullingGate
    }

    public struct Cost
    {
        public string Item;
        public int Amount;

        public Cost(string item, int amount)
        {
            Item = item;
            Amount = amount;
        }
    }

    public sealed class MachineSpec
    {
        public string Id = "";
        public string Name = "";
        public string Description = "";
        public MachineRole Role;
        public string Model = "";
        public string Station = "Workbench";
        public Cost[] Costs = new Cost[0];
        public GlbPieceVisual.BoxSpec[] Boxes;
        public string[] Milestones = new string[0];
        public WearNTear.MaterialType Material = WearNTear.MaterialType.Wood;
        public float Health = 400f;

        public string Token => "$piece_" + Id;
        public string DescriptionToken => "$piece_" + Id + "_desc";
    }

    /// <summary>
    /// Every hammer piece. Costs are the master prompt's proposals in vanilla prefab ids. The prefab id is the
    /// save identity: never rename one once testers have built it.
    /// </summary>
    public static class MachineCatalog
    {
        private static readonly Dictionary<string, MachineSpec> ById = new Dictionary<string, MachineSpec>();

        public static readonly List<MachineSpec> All = new List<MachineSpec>
        {
            Spec("vf_marker", MachineRole.Marker, "", "Workshop stake", "A timber stake. Two or more of your stakes mark a plantation plot for a timber saw.", C("Wood", 2)),
            Spec("vf_crank", MachineRole.Crank, "vf_hand_crank.glb", "Hand crank", "Hold interact to turn the line. 8 DU at 16 RPM. Costs 3 stamina a second.", C("Wood", 6), C("LeatherScraps", 2)),
            Spec("vf_shaft", MachineRole.Shaft, "vf_shaft_2m.glb", "Wooden shaft", "Carries rotation 2 m. No load.", C("Wood", 2)),
            Spec("vf_rope", MachineRole.Rope, "vf_rope_4m.glb", "Rope drive", "Carries rotation 4 m between two shafts. No load.", C("Wood", 4), C("LeatherScraps", 2)),
            Spec("vf_cog", MachineRole.Cog, "vf_cog.glb", "Bronze cog", "Interact to choose 1:1, 2:1, or 1:2. Faster output carries the same load.", C("Wood", 2), C("Bronze", 1)),
            Spec("vf_reversing_cog", MachineRole.ReversingCog, "vf_reversing_cog.glb", "Reversing cog", "Turns the line the other way. A source joined through it to another source stalls.", C("Wood", 2), C("Bronze", 1)),
            Spec("vf_clutch", MachineRole.Clutch, "vf_clutch.glb", "Clutch", "Interact to disconnect the branch beyond it.", C("Wood", 4), C("Bronze", 1)),
            Spec("vf_water_wheel", MachineRole.WaterWheel, "vf_water_wheel.glb", "Water wheel", "48 DU while its paddles sit in real water, 6 m from other wheels, with the wheel clear.", C("Wood", 30), C("RoundLog", 10), C("Bronze", 4), C("DeerHide", 4)),
            Spec("vf_belt", MachineRole.Belt, "vf_conveyor_2m.glb", "Timber belt", "Carries items 2 m, 30 a minute, while the line turns. 1 DU.", C("Wood", 4), C("LeatherScraps", 2), C("BronzeNails", 2)),
            Spec("vf_belt_corner", MachineRole.BeltCorner, "vf_conveyor_corner.glb", "Timber belt corner", "Turns a belt line. 1 DU.", C("Wood", 4), C("LeatherScraps", 2), C("BronzeNails", 2)),
            Spec("vf_iron_belt", MachineRole.IronBelt, "vf_iron_belt_2m.glb", "Iron belt", "Carries items 2 m, 60 a minute. 1 DU.", C("Wood", 4), C("LeatherScraps", 2), C("IronNails", 2)),
            Spec("vf_trough", MachineRole.Trough, "vf_gravity_trough.glb", "Gravity trough", "Moves items downhill only, 15 a minute. No power.", C("Wood", 4)),
            Spec("vf_feeder", MachineRole.Feeder, "vf_feeder.glb", "Bronze feeder", "Moves one item from its back to its front while the line turns. 6 DU, 30 a minute. Use an item to set a filter. Interact to set a stock target.", C("Wood", 6), C("Bronze", 2), C("LeatherScraps", 2)),
            Spec("vf_iron_feeder", MachineRole.IronFeeder, "vf_iron_feeder.glb", "Iron feeder", "As the bronze feeder, 60 a minute. 8 DU.", C("FineWood", 6), C("Iron", 3), C("IronNails", 4)),
            Spec("vf_basket", MachineRole.Basket, "vf_catch_basket.glb", "Catch basket", "A small buffer of eight stacks.", C("Wood", 6), C("LeatherScraps", 2)),
            Spec("vf_splitter", MachineRole.Splitter, "vf_splitter.glb", "Splitter", "Three outputs. Interact: fair, priority (manifold), or filter. Use an item to set the filter. Needs a turning line.", C("Wood", 4), C("BronzeNails", 4)),
            Spec("vf_merger", MachineRole.Merger, "vf_merger.glb", "Merger", "Three inputs taken in turn onto one output. Needs a turning line.", C("Wood", 4), C("BronzeNails", 4)),
            Spec("vf_recipe_mill", MachineRole.RecipeMill, "vf_recipe_mill.glb", "Recipe mill", "Use a crafted item you know to teach its recipe. Crafts from belt inputs in range of the real station. 16 DU.", C("Wood", 20), C("Bronze", 6), C("Stone", 5)),
            Spec("vf_assembler", MachineRole.Assembler, "vf_advanced_assembler.glb", "Advanced assembler", "A recipe mill with more buffers and up to 4× speed. Upgrade mode raises one item a quality. 32 DU.", "ArtisanTable", new[] { "Moder" }, C("FineWood", 20), C("BlackMetal", 10), C("Iron", 5)),
            Spec("vf_quarry", MachineRole.Quarry, "vf_quarry.glb", "Bedrock quarry", "One stone every 10 working seconds, holds 50. Natural ground, 12 m apart. 32 DU. A mod-added source.", "Stonecutter", new[] { "Elder" }, C("Stone", 30), C("RoundLog", 20), C("Iron", 8), C("IronNails", 10)),
            Spec("vf_coppice_bed", MachineRole.CoppiceBed, "vf_coppice_bed.glb", "Coppice bed", "Use 5 beech seeds, pine cones, birch seeds, or acorns to plant it. A timber saw harvests it. Interact to switch resin tapping. A mod-added source.", C("Wood", 20), C("Stone", 10)),
            Spec("vf_timber_saw", MachineRole.TimberSaw, "vf_timber_saw.glb", "Timber saw", "Harvests mature coppice beds and cuts trees inside your staked plot. 24 DU.", "Forge", new[] { "Elder" }, C("RoundLog", 15), C("Iron", 6), C("Stone", 5)),
            Spec("vf_planter", MachineRole.Planter, "vf_planter.glb", "Crop planter", "Plants seeds from its input on cultivated ground in a 4 × 4 m field. Interact to change policy. 8 DU.", C("Wood", 10), C("Bronze", 3), C("Stone", 5)),
            Spec("vf_harvester", MachineRole.Harvester, "vf_harvester.glb", "Crop harvester", "Picks mature crops in its 4 × 4 m field and forage beds nearby. 12 DU.", C("Wood", 10), C("Bronze", 4), C("LeatherScraps", 2)),
            Spec("vf_farm_gantry", MachineRole.FarmGantry, "vf_farm_gantry.glb", "Farm gantry", "Plants and harvests an 8 × 8 m field. 20 DU.", "Workbench", new[] { "Moder" }, C("Wood", 30), C("BlackMetal", 10), C("LinenThread", 10)),
            Spec("vf_forage_bed", MachineRole.ForageBed, "vf_forage_bed.glb", "Forage bed", "Use 5 raspberries, blueberries, mushrooms, thistle, or cloudberries to found it. Grows one every 10 minutes or the native respawn, holds 5. A mod-added source.", "Workbench", new[] { "Elder" }, C("Wood", 10), C("Stone", 10)),
            Spec("vf_mining_head", MachineRole.MiningHead, "vf_finite_mining_head.glb", "Mining head", "Strikes the real rock or deposit in front of it with a pickaxe's damage. Depletes normally. 48 DU.", "Forge", new[] { "Elder" }, C("RoundLog", 20), C("Iron", 10), C("Stone", 10)),
            Spec("vf_deep_extractor", MachineRole.DeepExtractor, "vf_deep_extractor.glb", "Deep extractor", "Use 10 of an older ore to commission it. Slow trace output after the right boss, in the right biome, 32 m apart. 96 DU.", "Forge", new[] { "Moder" }, C("Stone", 30), C("Iron", 20), C("BlackMetal", 10), C("LinenThread", 10)),
            Spec("vf_steam_engine", MachineRole.SteamEngine, "vf_steam_engine.glb", "Steam engine", "160 DU at 32 RPM. One coal per 30 running seconds, a water intake within 4 m, and a clear chimney.", "Forge", new[] { "Elder" }, C("Stone", 30), C("Iron", 10), C("Copper", 10), C("SurtlingCore", 2)),
            Spec("vf_reinforced_steam", MachineRole.ReinforcedSteam, "vf_reinforced_steam_engine.glb", "Reinforced steam engine", "320 DU at 64 RPM. One coal per 15 running seconds.", "BlackForge", new[] { "Queen" }, C("Grausten", 40), C("FlametalNew", 15), C("Iron", 10), C("SurtlingCore", 4)),
            Spec("vf_water_intake", MachineRole.WaterIntake, "vf_water_intake.glb", "Water intake", "Its probe must sit in real water. Feeds a steam engine within 4 m.", "Forge", new string[0], C("Wood", 6), C("Iron", 2)),
            Spec("vf_sail_wheel", MachineRole.SailWheel, "vf_sail_wheel.glb", "Sail wheel", "32–256 DU at 32 RPM from the wind and how open the sails are. Nothing when becalmed.", "Workbench", new[] { "Moder" }, C("Wood", 30), C("FineWood", 20), C("LinenThread", 10), C("IronNails", 10)),
            Spec("vf_governor", MachineRole.Governor, "vf_governor.glb", "Governor", "Fixes the line's speed. Interact to choose 8, 16, 32, or 64 RPM. Sources faster than the setting are held back.", "Forge", new[] { "Bonemass" }, C("FineWood", 5), C("Iron", 2), C("Silver", 2)),
            Spec("vf_flywheel", MachineRole.Flywheel, "vf_flywheel.glb", "Flywheel", "Stores 2400 DU·s from spare drive and gives back up to 80 DU in a lull.", "Forge", new[] { "Bonemass" }, C("FineWood", 20), C("Iron", 8), C("Silver", 4)),
            Spec("vf_eitr_motor", MachineRole.EitrMotor, "vf_eitr_motor.glb", "Eitr motor", "320 DU at 64 RPM. One refined eitr per 120 running seconds.", "BlackForge", new[] { "Yagluth" }, C("BlackMarble", 20), C("BlackMetal", 10), C("Eitr", 5), C("BlackCore", 1)),
            Spec("vf_cooking_tender", MachineRole.CookingTender, "vf_cooking_tender.glb", "Cooking tender", "Loads raw food onto a rack or oven only when it has room for the cooked result, and takes it off when done. If the line stalls, food can burn. 8 DU.", C("Wood", 10), C("Bronze", 2), C("LeatherScraps", 2)),
            Spec("vf_oven_extension", MachineRole.OvenExtension, "vf_oven_extension.glb", "Oven tender extension", "A cooking tender within 3 m reaches 2.5 m farther.", "Forge", new string[0], C("Iron", 4), C("Stone", 5)),
            Spec("vf_feed_gate", MachineRole.FeedGate, "vf_livestock_feed_gate.glb", "Livestock feed gate", "Drops one food from its input onto the pad when a tame animal nearby is hungry. 6 DU.", C("Wood", 10), C("Bronze", 2)),
            Spec("vf_culling_gate", MachineRole.CullingGate, "vf_livestock_culling_gate.glb", "Culling gate", "Off until you enable it. Takes only unnamed tame adult boars above a breeding reserve. 16 DU.", "Forge", new string[0], C("RoundLog", 10), C("Iron", 6))
        };

        static MachineCatalog()
        {
            foreach (var spec in All)
                ById[spec.Id] = spec;
            Get("vf_crank").Boxes = new[] { Box(new Vector3(0f, 0.55f, 0f), new Vector3(0.7f, 1.1f, 0.65f)) };
            Get("vf_shaft").Boxes = new[] { Box(new Vector3(0f, 0.3f, 0f), new Vector3(0.42f, 0.6f, 2f)) };
            Get("vf_clutch").Boxes = new[] { Box(new Vector3(0f, 0.35f, 0f), new Vector3(0.5f, 0.7f, 0.8f)) };
            Get("vf_water_wheel").Boxes = new[]
            {
                Box(new Vector3(-0.95f, 1.075f, 0f), new Vector3(0.3f, 2.15f, 1.95f)),
                Box(new Vector3(0.95f, 1.075f, 0f), new Vector3(0.3f, 2.15f, 1.95f))
            };
            Get("vf_belt").Boxes = new[] { Box(new Vector3(0f, 0.73f, 0f), new Vector3(1.1f, 0.25f, 2f)) };
            Get("vf_feeder").Boxes = new[] { Box(new Vector3(0f, 0.4f, 0f), new Vector3(0.8f, 0.8f, 0.8f)) };
            Get("vf_basket").Boxes = new[]
            {
                Box(new Vector3(-0.48f, 0.4f, 0f), new Vector3(0.1f, 0.75f, 0.95f)),
                Box(new Vector3(0.48f, 0.4f, 0f), new Vector3(0.1f, 0.75f, 0.95f)),
                Box(new Vector3(0f, 0.4f, -0.43f), new Vector3(0.95f, 0.75f, 0.1f)),
                Box(new Vector3(0f, 0.4f, 0.43f), new Vector3(0.95f, 0.75f, 0.1f)),
                Box(new Vector3(0f, 0.05f, 0f), new Vector3(1f, 0.1f, 0.9f))
            };
            foreach (var spec in All)
            {
                if (HasAny(spec, "Iron", "BlackMetal", "FlametalNew", "Silver"))
                {
                    spec.Material = WearNTear.MaterialType.Iron;
                    spec.Health = 1000f;
                }
                else if (HasAny(spec, "Stone", "Grausten", "BlackMarble") && spec.Costs[0].Item != "Wood")
                {
                    spec.Material = WearNTear.MaterialType.Stone;
                    spec.Health = 1000f;
                }
            }
        }

        public static MachineSpec Get(string id)
        {
            MachineSpec spec;
            return id != null && ById.TryGetValue(id, out spec) ? spec : null;
        }

        /// <summary>DU a consumer reserves while enabled. Sources and transmissions return zero.</summary>
        public static int LoadDu(MachineRole role)
        {
            switch (role)
            {
                case MachineRole.Belt:
                case MachineRole.BeltCorner:
                    return BalanceDefaults.TimberBeltDuPer2M;
                case MachineRole.IronBelt: return BalanceDefaults.IronBeltDuPer2M;
                case MachineRole.Feeder: return BalanceDefaults.BronzeFeederDu;
                case MachineRole.IronFeeder: return BalanceDefaults.IronFeederDu;
                case MachineRole.CookingTender: return BalanceDefaults.CookingTenderDu;
                case MachineRole.Splitter:
                case MachineRole.Merger:
                    return BalanceDefaults.SplitterDu;
                case MachineRole.RecipeMill: return BalanceDefaults.RecipeMillDu;
                case MachineRole.Assembler: return BalanceDefaults.AdvancedAssemblerDu;
                case MachineRole.Quarry: return BalanceDefaults.QuarryDu;
                case MachineRole.TimberSaw: return BalanceDefaults.TimberSawDu;
                case MachineRole.Planter: return BalanceDefaults.PlanterDu;
                case MachineRole.Harvester: return BalanceDefaults.HarvesterDu;
                case MachineRole.FarmGantry: return BalanceDefaults.FarmGantryDu;
                case MachineRole.MiningHead: return BalanceDefaults.FiniteMiningDu;
                case MachineRole.DeepExtractor: return BalanceDefaults.DeepExtractorDu;
                case MachineRole.FeedGate: return BalanceDefaults.FeedGateDu;
                case MachineRole.CullingGate: return BalanceDefaults.CullingGateDu;
                default: return 0;
            }
        }

        public static bool IsConsumer(MachineRole role)
        {
            switch (role)
            {
                case MachineRole.Belt:
                case MachineRole.BeltCorner:
                case MachineRole.IronBelt:
                case MachineRole.Feeder:
                case MachineRole.IronFeeder:
                case MachineRole.CookingTender:
                case MachineRole.Splitter:
                case MachineRole.Merger:
                case MachineRole.RecipeMill:
                case MachineRole.Assembler:
                case MachineRole.Quarry:
                case MachineRole.TimberSaw:
                case MachineRole.Planter:
                case MachineRole.Harvester:
                case MachineRole.FarmGantry:
                case MachineRole.MiningHead:
                case MachineRole.DeepExtractor:
                case MachineRole.FeedGate:
                case MachineRole.CullingGate:
                    return true;
                default:
                    return false;
            }
        }

        private static bool HasAny(MachineSpec spec, params string[] items)
        {
            foreach (var cost in spec.Costs)
            {
                foreach (var item in items)
                {
                    if (cost.Item == item)
                        return true;
                }
            }

            return false;
        }

        private static MachineSpec Spec(string id, MachineRole role, string model, string name, string description, params Cost[] costs)
        {
            return Spec(id, role, model, name, description, "Workbench", new string[0], costs);
        }

        private static MachineSpec Spec(string id, MachineRole role, string model, string name, string description, string station, string[] milestones, params Cost[] costs)
        {
            return new MachineSpec { Id = id, Role = role, Model = model, Name = name, Description = description, Station = station, Milestones = milestones, Costs = costs };
        }

        private static Cost C(string item, int amount)
        {
            return new Cost(item, amount);
        }

        private static GlbPieceVisual.BoxSpec Box(Vector3 center, Vector3 size)
        {
            return new GlbPieceVisual.BoxSpec { Center = center, Size = size };
        }
    }
}
