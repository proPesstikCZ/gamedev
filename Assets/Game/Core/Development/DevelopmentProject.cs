using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// A game in development (GDD 6.2). Each week of a work phase adds points to the phase's areas
    /// by the player's split: points = time x skill x technology. Work adds bugs, QA weeks remove them.
    /// Serializable so a save can hold a game in progress.
    /// </summary>
    [Serializable]
    public sealed class DevelopmentProject
    {
        [SerializeField] private GameConcept concept;
        [SerializeField] private int phase;
        [SerializeField] private int weekInPhase;
        [SerializeField] private int weeksWorked;
        [SerializeField] private int[] phaseWeeks = new int[3];
        [SerializeField] private int qaWeeks;
        [SerializeField] private int maxQaWeeks;
        [SerializeField] private float pointsPerWeek;
        [SerializeField] private float skill;
        [SerializeField] private float technology;
        [SerializeField] private float bugsPerWeek;
        [SerializeField] private float qaBugReduction;
        [SerializeField] private int[] allocation = new int[DevAreas.Count];
        [SerializeField] private float[] areaPoints = new float[DevAreas.Count];
        [SerializeField] private float bugs;

        public GameConcept Concept => concept;
        public DevPhase Phase => (DevPhase)phase;
        public int WeekInPhase => weekInPhase;
        public int WeeksWorked => weeksWorked;
        public int QaWeeks => qaWeeks;
        public int MaxQaWeeks => maxQaWeeks;
        public float Skill => skill;
        public float Technology => technology;
        public float Bugs => bugs;
        public bool IsDone => Phase == DevPhase.Done;
        /// <summary>Full length including QA.</summary>
        public int TotalWeeks => phaseWeeks[0] + phaseWeeks[1] + phaseWeeks[2] + qaWeeks;
        /// <summary>Points earned per work week: time x skill x technology.</summary>
        public float PointsPerWorkWeek => pointsPerWeek * skill * technology;

        private DevelopmentProject() { }

        public DevelopmentProject(GameConcept concept, DevelopmentBalance balance, float skill, float technology, int qaWeeks = 0)
        {
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            if (!(skill > 0f) || float.IsInfinity(skill)) throw new ArgumentOutOfRangeException(nameof(skill), skill, "Must be positive.");
            if (!(technology > 0f) || float.IsInfinity(technology)) throw new ArgumentOutOfRangeException(nameof(technology), technology, "Must be positive.");
            this.concept = concept ?? throw new ArgumentNullException(nameof(concept));
            phaseWeeks = new[] { Math.Max(1, balance.PreProductionWeeks), Math.Max(1, balance.ProductionWeeks), Math.Max(1, balance.FinishingWeeks) };
            maxQaWeeks = Math.Max(0, balance.MaxQaWeeks);
            pointsPerWeek = balance.PointsPerWeek;
            this.skill = skill;
            this.technology = technology;
            bugsPerWeek = balance.BugsPerWeek;
            qaBugReduction = Mathf.Clamp01(balance.QaBugReductionPerWeek);
            foreach (var p in DevPhases.WorkPhases) WriteAllocation(p, DevPhases.EvenSplit(p));
            SetQaWeeks(qaWeeks);
        }

        public static float TechnologyFor(EngineChoice engine, PlatformData platform, DevelopmentBalance balance)
        {
            float engineTech = engine == EngineChoice.Licensed ? balance.LicensedEngineTech : balance.CustomEngineTech;
            float ceiling = platform != null ? platform.TechCeiling : 0f;
            return engineTech * (1f + balance.PlatformTechWeight * ceiling);
        }

        public int WeeksOf(DevPhase p)
        {
            if (DevPhases.IsWorkPhase(p)) return phaseWeeks[(int)p];
            return p == DevPhase.QA ? qaWeeks : 0;
        }

        public float GetAreaPoints(DevArea area) => areaPoints[(int)area];

        public float[] CopyAreaPoints() => (float[])areaPoints.Clone();

        public int GetAllocation(DevArea area) => allocation[(int)area];

        /// <summary>Percents for the areas of the phase, in the order of <see cref="DevPhases.AreasOf"/>.</summary>
        public int[] GetAllocation(DevPhase p)
        {
            var areas = DevPhases.AreasOf(p);
            var result = new int[areas.Count];
            for (int i = 0; i < areas.Count; i++) result[i] = allocation[(int)areas[i]];
            return result;
        }

        /// <summary>Sets the split of a current or future work phase. Percents must sum to 100.</summary>
        public void SetAllocation(DevPhase p, IReadOnlyList<int> percents)
        {
            if (!DevPhases.IsWorkPhase(p)) throw new ArgumentException($"{p} has no areas.", nameof(p));
            if (p < Phase) throw new InvalidOperationException($"{p} is already finished.");
            if (!DevPhases.IsValidAllocation(p, percents))
                throw new ArgumentException($"Allocation for {p} needs {DevPhases.AreasOf(p).Count} non-negative values summing to 100.", nameof(percents));
            WriteAllocation(p, percents);
        }

        /// <summary>QA length can change until QA starts.</summary>
        public void SetQaWeeks(int weeks)
        {
            if (Phase >= DevPhase.QA) throw new InvalidOperationException("QA has already started.");
            if (weeks < 0 || weeks > maxQaWeeks) throw new ArgumentOutOfRangeException(nameof(weeks), weeks, $"Must be 0..{maxQaWeeks}.");
            qaWeeks = weeks;
        }

        /// <summary>Works one week. Returns true when the game became ready for release this week.</summary>
        public bool AdvanceWeek()
        {
            if (IsDone) return false;

            if (DevPhases.IsWorkPhase(Phase))
            {
                float points = PointsPerWorkWeek;
                foreach (var area in DevPhases.AreasOf(Phase))
                    areaPoints[(int)area] += points * allocation[(int)area] / 100f;
                bugs += bugsPerWeek / skill;
            }
            else if (Phase == DevPhase.QA)
            {
                bugs *= 1f - qaBugReduction;
            }

            weekInPhase++;
            weeksWorked++;
            SkipFinishedPhases();
            return IsDone;
        }

        private void SkipFinishedPhases()
        {
            while (!IsDone && weekInPhase >= WeeksOf(Phase))
            {
                phase++;
                weekInPhase = 0;
            }
        }

        private void WriteAllocation(DevPhase p, IReadOnlyList<int> percents)
        {
            var areas = DevPhases.AreasOf(p);
            for (int i = 0; i < areas.Count; i++) allocation[(int)areas[i]] = percents[i];
        }

        /// <summary>Checks data loaded from a save.</summary>
        internal bool IsStructurallyValid()
        {
            if (concept == null || phaseWeeks == null || phaseWeeks.Length != 3 || allocation == null || areaPoints == null) return false;
            if (allocation.Length != DevAreas.Count || areaPoints.Length != DevAreas.Count) return false;
            if (phase < 0 || phase > (int)DevPhase.Done || weekInPhase < 0 || qaWeeks < 0 || qaWeeks > maxQaWeeks) return false;
            if (!(skill > 0f) || !(technology > 0f) || bugs < 0f || float.IsNaN(bugs)) return false;
            foreach (var p in DevPhases.WorkPhases)
                if (phaseWeeks[(int)p] < 1 || !DevPhases.IsValidAllocation(p, GetAllocation(p))) return false;
            return true;
        }
    }
}
