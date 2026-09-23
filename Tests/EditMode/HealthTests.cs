using System;
using EldritchGames.PawnSystem.Vitals;
using NUnit.Framework;

namespace EldritchGames.PawnSystem.Tests.EditMode
{
    public class HealthTests
    {
        [Test]
        public void Constructor_ClampsTheStartingValueIntoRange()
        {
            Assert.AreEqual(50f, new Health(80f, 50f).Current, 0.0001f, "A starting value above the maximum is clamped down.");
            Assert.AreEqual(0f, new Health(-10f, 50f).Current, 0.0001f, "A negative starting value is clamped to zero.");
        }

        [Test]
        public void Constructor_WithOnlyAMaximum_StartsFull()
        {
            var health = new Health(75f);

            Assert.AreEqual(75f, health.Current, 0.0001f);
            Assert.AreEqual(75f, health.Max, 0.0001f);
        }

        [Test]
        public void Constructor_WithANonPositiveMaximum_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Health(0f));
        }

        [Test]
        public void ApplyDelta_ReturnsTheAmountActuallyApplied()
        {
            var health = new Health(10f, 100f);

            Assert.AreEqual(-10f, health.ApplyDelta(-30f), 0.0001f, "Only the remaining ten could be removed.");
            Assert.AreEqual(0f, health.Current, 0.0001f);
            Assert.IsTrue(health.IsDepleted);
        }

        [Test]
        public void ApplyDelta_AboveTheMaximum_ClampsAndReportsTheAppliedPart()
        {
            var health = new Health(90f, 100f);

            Assert.AreEqual(10f, health.ApplyDelta(50f), 0.0001f);
            Assert.AreEqual(100f, health.Current, 0.0001f);
        }

        [Test]
        public void ApplyDelta_WhenNothingChanges_RaisesNothing()
        {
            var health = new Health(100f);
            int raised = 0;
            health.Changed += (_, __) => raised++;

            health.ApplyDelta(0f);
            health.ApplyDelta(25f);

            Assert.AreEqual(0, raised, "Changed reports real movement, not attempted movement.");
        }

        [Test]
        public void ApplyDelta_RaisesChangedWithTheNewValues()
        {
            var health = new Health(100f);
            float reportedCurrent = -1f;
            float reportedMax = -1f;
            health.Changed += (current, max) =>
            {
                reportedCurrent = current;
                reportedMax = max;
            };

            health.ApplyDelta(-40f);

            Assert.AreEqual(60f, reportedCurrent, 0.0001f);
            Assert.AreEqual(100f, reportedMax, 0.0001f);
        }

        [Test]
        public void SetMax_WithoutFilling_ClampsTheCurrentValue()
        {
            var health = new Health(100f);

            health.SetMax(40f);

            Assert.AreEqual(40f, health.Current, 0.0001f, "Lowering the maximum below the current value drags it down.");
        }

        [Test]
        public void SetMax_WithFilling_TopsThePoolUp()
        {
            var health = new Health(20f, 100f);

            health.SetMax(150f, fillToMax: true);

            Assert.AreEqual(150f, health.Current, 0.0001f);
        }

        [Test]
        public void Fill_SetsTheCurrentValueToAFractionOfTheMaximum()
        {
            var health = new Health(0f, 200f);

            health.Fill(0.25f);

            Assert.AreEqual(50f, health.Current, 0.0001f);
        }

        [Test]
        public void Fill_ClampsTheFraction()
        {
            var health = new Health(0f, 100f);

            health.Fill(3f);
            Assert.AreEqual(100f, health.Current, 0.0001f);

            health.Fill(-1f);
            Assert.AreEqual(0f, health.Current, 0.0001f);
        }
    }
}
