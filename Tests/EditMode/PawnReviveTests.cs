using EldritchGames.PawnSystem.Identity;
using EldritchGames.PawnSystem.TestUtilities;
using EldritchGames.PawnSystem.Vitals;
using NUnit.Framework;
using UnityEngine;

namespace EldritchGames.PawnSystem.Tests.EditMode
{
    public class PawnReviveTests
    {
        private Pawn pawn;

        [SetUp]
        public void SetUp()
        {
            PawnDefinition definition = new PawnDefinitionBuilder().WithId("knight").WithMaxVital(100f).Build();
            pawn = TestPawns.Create(definition);
            pawn.Spawn();
        }

        [TearDown]
        public void TearDown()
        {
            if (pawn != null) Object.DestroyImmediate(pawn.gameObject);
        }

        [Test]
        public void TryRevive_OnADeadPawn_BringsItBackAtFullVitals()
        {
            pawn.Kill();

            ReviveResult result = pawn.TryRevive(ReviveInfo.Full);

            Assert.AreEqual(ReviveResult.Success, result);
            Assert.AreEqual(PawnState.Alive, pawn.State);
            Assert.AreEqual(100f, pawn.Vitals.Current, 0.0001f);
        }

        [Test]
        public void TryRevive_WithAFraction_RestoresThatShareOfTheMaximum()
        {
            pawn.Kill();

            pawn.TryRevive(new ReviveInfo(0.25f));

            Assert.AreEqual(25f, pawn.Vitals.Current, 0.0001f);
        }

        [Test]
        public void TryRevive_WithADefaultRecord_RevivesToFull()
        {
            pawn.Kill();

            pawn.TryRevive(default);

            Assert.AreEqual(100f, pawn.Vitals.Current, 0.0001f,
                "A fraction of zero means 'come back whole', so default(ReviveInfo) is a full revive.");
        }

        [Test]
        public void TryRevive_OnALivingPawn_ReturnsNotDead()
        {
            pawn.ApplyDamage(new DamageInfo(40f));

            ReviveResult result = pawn.TryRevive(ReviveInfo.Full);

            Assert.AreEqual(ReviveResult.NotDead, result);
            Assert.AreEqual(60f, pawn.Vitals.Current, 0.0001f, "A refused revival must not top the pawn up.");
        }

        [Test]
        public void TryRevive_OnADespawnedPawn_ReturnsNotSpawned()
        {
            pawn.Kill();
            pawn.Despawn();

            Assert.AreEqual(ReviveResult.NotSpawned, pawn.TryRevive(ReviveInfo.Full));
        }

        [Test]
        public void TryRevive_RaisesRevivedAndGrantsTheRequestedGracePeriod()
        {
            pawn.Kill();

            int raised = 0;
            pawn.Revived += _ => raised++;

            pawn.TryRevive(new ReviveInfo(1f, invulnerabilityDuration: 3f));

            Assert.AreEqual(1, raised);
            Assert.IsTrue(pawn.IsInvulnerable, "A revived pawn gets a grace period so it is not killed again instantly.");
            Assert.AreEqual(3f, pawn.InvulnerabilityRemaining, 0.0001f);
        }

        [Test]
        public void Died_FiresAgainAfterAReviveAndASecondDeath()
        {
            int deaths = 0;
            pawn.Died += (_, __) => deaths++;

            pawn.Kill();
            pawn.TryRevive(ReviveInfo.Full);
            pawn.Kill();

            Assert.AreEqual(2, deaths, "Each death is its own event; revival resets the pawn's ability to die.");
        }

        [Test]
        public void TryRevive_AllowsPossessionAgain()
        {
            var possessor = new FakePawnPossessor();
            pawn.TryPossess(possessor);
            pawn.Kill();

            pawn.TryRevive(ReviveInfo.Full);

            Assert.AreEqual(PossessionResult.Success, pawn.TryPossess(possessor),
                "A revived body can be driven again by whoever was thrown out when it died.");
        }
    }
}
