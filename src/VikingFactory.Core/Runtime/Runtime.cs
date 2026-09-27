using System;

namespace VikingFactory.Core.Runtime
{
    /// <summary>
    /// Fixed-step scheduler with bounded catch-up. A jump larger than the discontinuity threshold,
    /// such as sleeping or reloading an area, is dropped rather than replayed as fictitious work.
    /// </summary>
    public sealed class FixedStepClock
    {
        private double _accumulator;

        public FixedStepClock(double stepSeconds = 0.2, int maxCatchUpSteps = 5, double discontinuitySeconds = 2.0)
        {
            if (stepSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(stepSeconds));
            StepSeconds = stepSeconds;
            MaxCatchUpSteps = Math.Max(1, maxCatchUpSteps);
            DiscontinuitySeconds = discontinuitySeconds;
        }

        public double StepSeconds { get; }
        public int MaxCatchUpSteps { get; }
        public double DiscontinuitySeconds { get; }
        public int Discontinuities { get; private set; }

        /// <summary>Returns the number of fixed steps to run for this real delta.</summary>
        public int Advance(double deltaSeconds)
        {
            if (deltaSeconds <= 0)
                return 0;
            if (deltaSeconds > DiscontinuitySeconds)
            {
                Discontinuities++;
                _accumulator = 0;
                return 0;
            }

            _accumulator += deltaSeconds;
            var steps = (int)Math.Floor(_accumulator / StepSeconds);
            if (steps > MaxCatchUpSteps)
            {
                steps = MaxCatchUpSteps;
                _accumulator = 0;
                return steps;
            }

            _accumulator -= steps * StepSeconds;
            return steps;
        }
    }

    /// <summary>
    /// Ownership epoch for one networked object. A machine acts only after it has owned the object
    /// for a full observation, so state saved by the previous owner is read before anything moves.
    /// </summary>
    public sealed class OwnershipTracker
    {
        private long _owner = long.MinValue;
        private int _stableObservations;

        public int Epoch { get; private set; }

        public void Observe(long ownerId, bool isLocalOwner)
        {
            if (ownerId != _owner)
            {
                _owner = ownerId;
                Epoch++;
                _stableObservations = 0;
            }

            _stableObservations = isLocalOwner ? _stableObservations + 1 : 0;
        }

        /// <summary>True from the second consecutive observation as owner.</summary>
        public bool CanAct => _stableObservations >= 2;

        /// <summary>True on the first observation after gaining ownership: reload state now.</summary>
        public bool ShouldRehydrate => _stableObservations == 1;
    }

    public enum SchemaDecision
    {
        Current,
        Migrate,
        PauseNewer
    }

    /// <summary>Per-machine save schema. Unknown newer data pauses the machine instead of being overwritten.</summary>
    public static class SchemaGate
    {
        public const int Current = 1;

        public static SchemaDecision Check(int stored)
        {
            if (stored == Current)
                return SchemaDecision.Current;
            if (stored > Current)
                return SchemaDecision.PauseNewer;
            return SchemaDecision.Migrate;
        }
    }

    public enum Preset
    {
        BalancedIndustry,
        VanillaSupply
    }

    /// <summary>Which new-supply features the server preset allows.</summary>
    public sealed class FeatureFlags
    {
        public bool BedrockQuarry;
        public bool Coppice;
        public bool Forage;
        public bool DeepExtraction;

        public static FeatureFlags For(Preset preset, bool allowBedrockStone)
        {
            var balanced = preset == Preset.BalancedIndustry;
            return new FeatureFlags
            {
                BedrockQuarry = allowBedrockStone,
                Coppice = balanced,
                Forage = balanced,
                DeepExtraction = balanced
            };
        }
    }
}
