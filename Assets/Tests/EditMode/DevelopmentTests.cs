using System;
using Game.Core;
using NUnit.Framework;

namespace Game.Tests
{
    public class DevelopmentTests
    {
        private static DevelopmentProject NewProject(int qaWeeks = 0, DevelopmentBalance balance = null)
        {
            return new DevelopmentProject(M2TestData.Concept(), balance ?? new DevelopmentBalance(), 1f, 1f, qaWeeks);
        }

        private static void Finish(DevelopmentProject p)
        {
            for (int i = 0; i < 100 && !p.IsDone; i++) p.AdvanceWeek();
        }

        [TestCase(99)]
        [TestCase(101)]
        public void Allocation_MustSumTo100(int first)
        {
            var p = NewProject();
            Assert.Throws<ArgumentException>(() => p.SetAllocation(DevPhase.PreProduction, new[] { first, 0, 0 }));
            Assert.DoesNotThrow(() => p.SetAllocation(DevPhase.PreProduction, new[] { 100, 0, 0 }));
        }

        [Test]
        public void Allocation_RejectsNegativeValuesAndWrongLength()
        {
            var p = NewProject();
            Assert.Throws<ArgumentException>(() => p.SetAllocation(DevPhase.Production, new[] { 120, -20, 0, 0 }));
            Assert.Throws<ArgumentException>(() => p.SetAllocation(DevPhase.Production, new[] { 50, 50 }));
            Assert.Throws<ArgumentException>(() => p.SetAllocation(DevPhase.QA, new[] { 100 }));
        }

        [Test]
        public void DefaultAllocation_SumsTo100InEveryPhase()
        {
            var p = NewProject();
            foreach (var phase in DevPhases.WorkPhases)
                Assert.That(DevPhases.IsValidAllocation(phase, p.GetAllocation(phase)), Is.True, phase.ToString());
        }

        [Test]
        public void ToPercents_AlwaysSumsTo100()
        {
            var rng = new SeededRandom(5);
            for (int i = 0; i < 200; i++)
            {
                var shares = new float[rng.Next(1, 6)];
                for (int j = 0; j < shares.Length; j++) shares[j] = rng.Range(0f, 3f);
                var percents = DevPhases.ToPercents(shares);
                int sum = 0;
                foreach (int p in percents) sum += p;
                Assert.That(sum, Is.EqualTo(100));
            }
            CollectionAssert.AreEqual(new[] { 34, 33, 33 }, DevPhases.ToPercents(new float[3]));
        }

        [Test]
        public void SmallGame_TakesEightToSixteenWeeks()
        {
            var balance = new DevelopmentBalance();
            for (int qa = 0; qa <= balance.MaxQaWeeks; qa++)
            {
                var p = NewProject(qa, balance);
                int weeks = 0;
                while (!p.IsDone)
                {
                    p.AdvanceWeek();
                    weeks++;
                }
                Assert.That(weeks, Is.EqualTo(p.TotalWeeks));
                Assert.That(weeks, Is.InRange(8, 16), $"QA {qa}");
            }
        }

        [Test]
        public void RealBalance_SmallGameFitsEightToSixteenWeeks()
        {
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Data.GameDatabase>(ContentAssetTests.DatabasePath);
            var dev = asset.ToCore().Balance.Development;
            int min = dev.PreProductionWeeks + dev.ProductionWeeks + dev.FinishingWeeks;
            Assert.That(min, Is.GreaterThanOrEqualTo(8));
            Assert.That(min + dev.MaxQaWeeks, Is.LessThanOrEqualTo(16));
        }

        [Test]
        public void AreaPoints_AreTimeTimesSkillTimesTechnology_SplitByAllocation()
        {
            var balance = new DevelopmentBalance { PreProductionWeeks = 2, PointsPerWeek = 10f };
            var p = new DevelopmentProject(M2TestData.Concept(), balance, 1.5f, 2f);
            p.SetAllocation(DevPhase.PreProduction, new[] { 50, 30, 20 });
            p.AdvanceWeek();
            p.AdvanceWeek();

            float perWeek = 10f * 1.5f * 2f;
            Assert.That(p.GetAreaPoints(DevArea.GameplayDesign), Is.EqualTo(2 * perWeek * 0.5f).Within(1e-4));
            Assert.That(p.GetAreaPoints(DevArea.StoryAndWorld), Is.EqualTo(2 * perWeek * 0.3f).Within(1e-4));
            Assert.That(p.GetAreaPoints(DevArea.Technology), Is.EqualTo(2 * perWeek * 0.2f).Within(1e-4));
            Assert.That(p.GetAreaPoints(DevArea.Gameplay), Is.EqualTo(0f));
            Assert.That(p.Phase, Is.EqualTo(DevPhase.Production));
        }

        [Test]
        public void Bugs_GrowDuringWork_AndQaWeeksReduceThem()
        {
            var noQa = NewProject(0);
            var withQa = NewProject(5);
            Finish(noQa);
            Finish(withQa);

            Assert.That(noQa.Bugs, Is.GreaterThan(0f));
            Assert.That(withQa.Bugs, Is.LessThan(noQa.Bugs));

            var p = NewProject(3);
            float last = float.MaxValue;
            while (!p.IsDone)
            {
                p.AdvanceWeek();
                if (p.Phase == DevPhase.QA || p.IsDone)
                {
                    Assert.That(p.Bugs, Is.LessThanOrEqualTo(last));
                    last = p.Bugs;
                }
            }
        }

        [Test]
        public void FinishedPhase_AndStartedQa_CannotBeChanged()
        {
            var p = NewProject(2);
            for (int i = 0; i < p.WeeksOf(DevPhase.PreProduction); i++) p.AdvanceWeek();
            Assert.Throws<InvalidOperationException>(() => p.SetAllocation(DevPhase.PreProduction, new[] { 100, 0, 0 }));
            Assert.DoesNotThrow(() => p.SetQaWeeks(4));

            while (p.Phase != DevPhase.QA) p.AdvanceWeek();
            Assert.Throws<InvalidOperationException>(() => p.SetQaWeeks(0));
        }
    }
}
