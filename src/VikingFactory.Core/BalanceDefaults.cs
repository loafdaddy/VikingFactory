using System;

namespace VikingFactory.Core
{
    /// <summary>
    /// Proposed balance from the master prompt. These are configuration defaults,
    /// not measured Valheim rates.
    /// </summary>
    public static class BalanceDefaults
    {
        public const int MilestoneRpm = 16;

        // Sources
        public const int HandCrankDu = 8;
        public const int WaterWheelDu = 48;
        public const int SteamEngineDu = 160;
        public const int SteamEngineRpm = 32;
        public const int SteamSecondsPerCoal = 30;
        public const int SailWheelMinDu = 32;
        public const int SailWheelMaxDu = 256;
        public const int SailWheelRpm = 32;
        public const double SailCalmBelow = 0.1;
        public const int EitrMotorDu = 320;
        public const int EitrMotorRpm = 64;
        public const int EitrSecondsPerRefinedEitr = 120;
        public const int ReinforcedSteamDu = 320;
        public const int ReinforcedSteamRpm = 64;
        public const int ReinforcedSteamSecondsPerCoal = 15;

        public const float WaterWheelSpacingMetres = 6f;
        public const float WaterWheelMinImmersionMetres = 0.4f;

        // Flywheel
        public const double FlywheelCapacityDuSeconds = 2400;
        public const double FlywheelMaxDischargeDu = 80;
        public const double FlywheelMaxChargeDu = 80;
        public const double FlywheelChargeEfficiency = 0.8;

        // Consumers
        public const int BronzeFeederDu = 6;
        public const int IronFeederDu = 8;
        public const int TimberBeltDuPer2M = 1;
        public const int IronBeltDuPer2M = 1;
        public const int GravityTroughDu = 0;
        public const int SplitterDu = 0;
        public const int RecipeMillDu = 16;
        public const int AdvancedAssemblerDu = 32;
        public const int PlanterDu = 8;
        public const int HarvesterDu = 12;
        public const int FarmGantryDu = 20;
        public const int TimberSawDu = 24;
        public const int QuarryDu = 32;
        public const int FiniteMiningDu = 48;
        public const int DeepExtractorDu = 96;
        public const int CookingTenderDu = 8;
        public const int CollectionArmDu = 6;
        public const int FeedGateDu = 6;
        public const int CullingGateDu = 16;
        public const int FishingWinchDu = 24;

        // Rates, items per minute
        public const double TimberBeltItemsPerMinute = 30;
        public const double IronBeltItemsPerMinute = 60;
        public const double GravityTroughItemsPerMinute = 15;
        public const double BronzeFeederItemsPerMinute = 30;
        public const double IronFeederItemsPerMinute = 60;

        // Recipe mill
        public const double MillBaseSeconds = 4;
        public const double MillSecondsPerIngredientType = 2;
        public const double MillMinimumSeconds = 2;
        public const double MillMaxSpeedMultiplier = 2;
        public const double AssemblerMaxSpeedMultiplier = 4;

        // Production
        public const double QuarrySecondsPerStone = 10;
        public const int QuarryCapacity = 50;
        public const float QuarrySpacingMetres = 12f;
        public const float ExtractorSpacingMetres = 32f;
        public const int ExtractorCapacity = 20;
        public const int ExtractorCommissionCount = 10;
        public const int ForageCommissionCount = 5;
        public const int ForageCapacity = 5;
        public const double ForageMinSeconds = 600;
        public const int CoppiceFoundingCount = 5;
        public const double CoppiceResinSeconds = 600;
        public const int CoppiceResinCapacity = 5;
        public const double PlanterSecondsPerAction = 4;
        public const double HarvesterSecondsPerAction = 4;
        public const double TimberSawSecondsPerStroke = 3;
        public const double MiningSecondsPerStroke = 4;
        public const double TenderSecondsPerAction = 2;

        public const int HandCrankRunsOneFeeder = HandCrankDu;
        public const int TwoFeedersDu = BronzeFeederDu * 2;

        /// <summary>
        /// Sail capacity follows wind and exposure, both 0..1. Becalmed below 10 % gives nothing.
        /// Otherwise 32–256 DU. Low wind limits load. It is not a constant output.
        /// </summary>
        public static int SailCapacity(double wind, double exposure)
        {
            var effective = Clamp01(wind) * Clamp01(exposure);
            if (effective < SailCalmBelow)
                return 0;
            var du = (int)Math.Round(SailWheelMaxDu * effective);
            return Math.Max(SailWheelMinDu, Math.Min(SailWheelMaxDu, du));
        }

        /// <summary>Seconds between moves for a rate in items per minute, scaled by useful RPM.</summary>
        public static double SecondsPerItem(double itemsPerMinute, double usefulRpm, double baseRpm = MilestoneRpm)
        {
            if (itemsPerMinute <= 0 || usefulRpm <= 0)
                return double.PositiveInfinity;
            var speed = Math.Max(0.25, Math.Min(1.0, usefulRpm / baseRpm));
            return 60.0 / itemsPerMinute / speed;
        }

        private static double Clamp01(double value)
        {
            return value < 0 ? 0 : value > 1 ? 1 : value;
        }
    }
}
