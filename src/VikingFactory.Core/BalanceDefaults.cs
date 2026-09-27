namespace VikingFactory.Core
{
    /// <summary>
    /// Proposed balance from the master prompt. These are configuration defaults,
    /// not measured Valheim rates.
    /// </summary>
    public static class BalanceDefaults
    {
        public const int MilestoneRpm = 16;

        public const int HandCrankDu = 8;
        public const int WaterWheelDu = 48;
        public const int BronzeFeederDu = 6;
        public const int TimberBeltDuPer2M = 1;

        public const int HandCrankRunsOneFeeder = HandCrankDu;
        public const int TwoFeedersDu = BronzeFeederDu * 2;
    }
}
