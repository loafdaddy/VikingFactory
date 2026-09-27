using System;

namespace VikingFactory.Core.Kinetics
{
    /// <summary>
    /// Paid running time for a fuelled engine. One fuel item buys a fixed interval.
    /// Fuel is taken only when the paid interval is spent and drive is wanted. The remaining
    /// interval is persisted, so a stall or a reload never takes a second item for the same time.
    /// </summary>
    public sealed class FuelledSource
    {
        public FuelledSource(double secondsPerFuel, double paidSeconds = 0)
        {
            if (secondsPerFuel <= 0)
                throw new ArgumentOutOfRangeException(nameof(secondsPerFuel));
            SecondsPerFuel = secondsPerFuel;
            PaidSeconds = Math.Max(0, paidSeconds);
        }

        public double SecondsPerFuel { get; }
        public double PaidSeconds { get; private set; }
        public int FuelConsumed { get; private set; }

        /// <summary>
        /// Called before the network is solved. Returns true when the engine can offer drive.
        /// Blocked exhaust or a missing water intake should skip this call entirely.
        /// </summary>
        public bool Prepare(Func<bool> tryConsumeFuel)
        {
            if (PaidSeconds > 0)
                return true;
            if (tryConsumeFuel == null || !tryConsumeFuel())
                return false;
            PaidSeconds += SecondsPerFuel;
            FuelConsumed++;
            return true;
        }

        /// <summary>Only running time is charged. A stalled engine keeps its paid interval.</summary>
        public void Accrue(double dtSeconds, bool running)
        {
            if (!running || dtSeconds <= 0)
                return;
            PaidSeconds = Math.Max(0, PaidSeconds - dtSeconds);
        }
    }
}
