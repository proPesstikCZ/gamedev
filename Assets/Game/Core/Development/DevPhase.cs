using System;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>Development phases (GDD 6.2). Done = ready for release.</summary>
    public enum DevPhase
    {
        PreProduction = 0,
        Production = 1,
        Finishing = 2,
        QA = 3,
        Done = 4,
    }

    public static class DevPhases
    {
        private static readonly DevArea[] PreProductionAreas = { DevArea.GameplayDesign, DevArea.StoryAndWorld, DevArea.Technology };
        private static readonly DevArea[] ProductionAreas = { DevArea.Gameplay, DevArea.Graphics, DevArea.Sound, DevArea.LevelDesign };
        private static readonly DevArea[] FinishingAreas = { DevArea.AiAndSystems, DevArea.DialogueAndText, DevArea.Polish };
        private static readonly DevArea[] NoAreas = new DevArea[0];

        /// <summary>The three phases where the player splits points between areas.</summary>
        public static readonly DevPhase[] WorkPhases = { DevPhase.PreProduction, DevPhase.Production, DevPhase.Finishing };

        public static bool IsWorkPhase(DevPhase phase) => phase >= DevPhase.PreProduction && phase <= DevPhase.Finishing;

        public static IReadOnlyList<DevArea> AreasOf(DevPhase phase)
        {
            switch (phase)
            {
                case DevPhase.PreProduction: return PreProductionAreas;
                case DevPhase.Production: return ProductionAreas;
                case DevPhase.Finishing: return FinishingAreas;
                default: return NoAreas;
            }
        }

        public static DevPhase PhaseOf(DevArea area)
        {
            if (area <= DevArea.Technology) return DevPhase.PreProduction;
            if (area <= DevArea.LevelDesign) return DevPhase.Production;
            return DevPhase.Finishing;
        }

        /// <summary>True when the list has one non-negative value per area of the phase and they sum to 100.</summary>
        public static bool IsValidAllocation(DevPhase phase, IReadOnlyList<int> percents)
        {
            if (!IsWorkPhase(phase) || percents == null || percents.Count != AreasOf(phase).Count) return false;
            int sum = 0;
            foreach (int p in percents)
            {
                if (p < 0) return false;
                sum += p;
            }
            return sum == 100;
        }

        /// <summary>
        /// Turns non-negative shares into whole percents that sum to exactly 100 (largest remainder).
        /// All zero shares give an even split.
        /// </summary>
        public static int[] ToPercents(IReadOnlyList<float> shares)
        {
            if (shares == null || shares.Count == 0) throw new ArgumentException("No shares.", nameof(shares));
            int n = shares.Count;
            double total = 0;
            foreach (float s in shares)
            {
                if (float.IsNaN(s) || float.IsInfinity(s) || s < 0f) throw new ArgumentException("Shares must be finite and non-negative.", nameof(shares));
                total += s;
            }

            var result = new int[n];
            var remainders = new double[n];
            int used = 0;
            for (int i = 0; i < n; i++)
            {
                double exact = total > 0 ? shares[i] / total * 100.0 : 100.0 / n;
                result[i] = (int)Math.Floor(exact);
                remainders[i] = exact - result[i];
                used += result[i];
            }
            for (int left = 100 - used; left > 0; left--)
            {
                int best = 0;
                for (int i = 1; i < n; i++) if (remainders[i] > remainders[best]) best = i;
                result[best]++;
                remainders[best] = -1;
            }
            return result;
        }

        public static int[] EvenSplit(DevPhase phase) => ToPercents(new float[AreasOf(phase).Count]);
    }
}
