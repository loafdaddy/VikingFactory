using System;
using System.Collections.Generic;
using System.Linq;

namespace VikingFactory.Core.Husbandry
{
    public sealed class Animal
    {
        public string Id = "";
        public string Species = "";
        public bool Tamed;
        public bool Adult;
        public bool Named;
        public bool Pregnant;
        public int Level = 1;
    }

    /// <summary>
    /// Culling is opt-in and conservative: reviewed species only, tamed unnamed adults only,
    /// and at least MinBreeders adults of the species are always kept.
    /// </summary>
    public sealed class CullingPolicy
    {
        public static readonly IReadOnlyList<string> ReviewedSpecies = new[] { "Boar" };

        public bool Enabled;
        public int MinBreeders = 2;
        public int KeepAbove = 4;

        /// <summary>Returns at most one animal to cull, or null. Deterministic: lowest level, then id.</summary>
        public Animal? SelectSurplus(IEnumerable<Animal> animals, out string reason)
        {
            reason = "";
            if (!Enabled)
            {
                reason = "Culling is off. Interact to enable it.";
                return null;
            }

            var list = animals.ToList();
            foreach (var group in list.Where(a => a.Tamed && ReviewedSpecies.Contains(a.Species)).GroupBy(a => a.Species))
            {
                var adults = group.Where(a => a.Adult).ToList();
                var keep = Math.Max(MinBreeders, KeepAbove);
                if (adults.Count <= keep)
                    continue;
                var candidate = adults
                    .Where(a => !a.Named && !a.Pregnant)
                    .OrderBy(a => a.Level)
                    .ThenBy(a => a.Id, StringComparer.Ordinal)
                    .FirstOrDefault();
                if (candidate != null)
                    return candidate;
            }

            reason = "No surplus. Named, young, pregnant, and untamed animals are never taken.";
            return null;
        }
    }

    /// <summary>Releases one food only while a tame animal is hungry and nothing is already on the pad.</summary>
    public static class FeedPolicy
    {
        public static bool ShouldRelease(int hungryTameAnimals, int foodOnPad)
        {
            return hungryTameAnimals > 0 && foodOnPad == 0;
        }
    }
}
