using EldritchGames.PawnSystem.Identity;
using EldritchGames.PawnSystem.TestUtilities;
using EldritchGames.PawnSystem.Vitals;
using NUnit.Framework;
using UnityEngine;

namespace EldritchGames.PawnSystem.Tests.EditMode
{
    public class PawnIncapacitationTests
    {
        private Pawn pawn;

        [TearDown]
        public void TearDown()
        {
            if (pawn != null) UnityEngine.Object.DestroyImmediate(pawn.gameObject);
        }

        [Test]
        public void FatalBlow_OnAnIncapacitatableDefinition_DownsInsteadOfKilling()
        {
            pawn = SpawnWith(new PawnDefinitionBuilder().WithId("a").WithMaxVital(20f).WithCanBeIncapacitated(true).Build());

            int diedCount = 0;
            int incapacitatedCount = 0;
            pawn.Died += (_, __) => diedCount++;
            pawn.Incapacitated += (_, __) => incapacitatedCount++;

            pawn.ApplyDamage(new DamageInfo(50f));

            Assert.AreEqual(PawnState.Incapacitated, pawn.State);
            Assert.AreEqual(1, incapacitatedCount);
            Assert.AreEqual(0, diedCount, "A downed pawn has not died yet.");
        }

        [Test]
        public void FatalBlow_WithoutCanBeIncapacitated_KillsAsBefore()
        {
            pawn = SpawnWith(new PawnDefinitionBuilder().WithId("a").WithMaxVital(20f).Build());

            pawn.ApplyDamage(new DamageInfo(50f));

            Assert.AreEqual(PawnState.Dead, pawn.State, "Incapacitation is opt-in; everyone else dies exactly as before.");
        }

        [Test]
        public void AnyFurtherHit_WhileIncapacitated_FinishesThePawn()
        {
            pawn = SpawnWith(new PawnDefinitionBuilder().WithId("a").WithMaxVital(20f).WithCanBeIncapacitated(true).Build());
            pawn.ApplyDamage(new DamageInfo(50f));

            float applied = pawn.ApplyDamage(new DamageInfo(1f));

            Assert.AreEqual(PawnState.Dead, pawn.State, "Any qualifying hit on a downed pawn finishes it.");
            Assert.AreEqual(0f, applied, 0.0001f, "There is nothing left to remove from vitals.");
        }

        [Test]
        public void FinishingBlow_WhileIncapacitated_SkipsTheModifierPipeline()
        {
            var passthrough = new FakeDamageModifier(amount => amount, "passthrough");
            pawn = SpawnWith(new PawnDefinitionBuilder()
                .WithId("a").WithMaxVital(20f).WithCanBeIncapacitated(true)
                .WithDamageModifiers(passthrough).Build());

            pawn.ApplyDamage(new DamageInfo(50f));
            Assert.AreEqual(PawnState.Incapacitated, pawn.State, "Sanity check: the first blow downs the pawn, running the pipeline as usual.");
            int callsBeforeFinish = passthrough.ModifyCallCount;

            pawn.ApplyDamage(new DamageInfo(1f));

            Assert.AreEqual(PawnState.Dead, pawn.State, "Armor does not save a downed target — a finishing blow bypasses the pipeline entirely.");
            Assert.AreEqual(callsBeforeFinish, passthrough.ModifyCallCount, "The modifier pipeline never runs for a finishing blow.");
        }

        [Test]
        public void WhileIncapacitated_InvulnerabilityStillBlocksAFinishingBlow()
        {
            pawn = SpawnWith(new PawnDefinitionBuilder().WithId("a").WithMaxVital(20f).WithCanBeIncapacitated(true).Build());
            pawn.ApplyDamage(new DamageInfo(50f));
            pawn.SetInvulnerable(true);

            pawn.ApplyDamage(new DamageInfo(50f));

            Assert.AreEqual(PawnState.Incapacitated, pawn.State, "Invulnerability still protects a downed pawn.");
        }

        [Test]
        public void BleedOutTimer_TicksDownAndKillsWhenItExpires()
        {
            pawn = SpawnWith(new PawnDefinitionBuilder()
                .WithId("a").WithMaxVital(20f).WithCanBeIncapacitated(true).WithIncapacitationDuration(3f).Build());
            pawn.ApplyDamage(new DamageInfo(50f));

            pawn.Tick(2f);
            Assert.AreEqual(PawnState.Incapacitated, pawn.State, "Two of three seconds have elapsed.");
            Assert.AreEqual(1f, pawn.IncapacitationRemaining, 0.0001f);

            pawn.Tick(1f);
            Assert.AreEqual(PawnState.Dead, pawn.State, "The bleed-out timer finished the pawn on its own.");
        }

        [Test]
        public void NoBleedOutTimer_StaysDownIndefinitely()
        {
            pawn = SpawnWith(new PawnDefinitionBuilder()
                .WithId("a").WithMaxVital(20f).WithCanBeIncapacitated(true).WithIncapacitationDuration(0f).Build());
            pawn.ApplyDamage(new DamageInfo(50f));

            pawn.Tick(1000f);

            Assert.AreEqual(PawnState.Incapacitated, pawn.State, "A zero duration disables the bleed-out timer entirely.");
        }

        [Test]
        public void TryRevive_RecoversAnIncapacitatedPawn()
        {
            pawn = SpawnWith(new PawnDefinitionBuilder().WithId("a").WithMaxVital(20f).WithCanBeIncapacitated(true).Build());
            pawn.ApplyDamage(new DamageInfo(50f));

            int revivedCount = 0;
            pawn.Revived += _ => revivedCount++;

            ReviveResult result = pawn.TryRevive(new ReviveInfo(0.5f));

            Assert.AreEqual(ReviveResult.Success, result);
            Assert.AreEqual(PawnState.Alive, pawn.State);
            Assert.AreEqual(10f, pawn.Vitals.Current, 0.0001f);
            Assert.AreEqual(1, revivedCount, "The same Revived event covers recovering from either dead or incapacitated.");
        }

        [Test]
        public void TryRevive_StopsTheBleedOutTimer()
        {
            pawn = SpawnWith(new PawnDefinitionBuilder()
                .WithId("a").WithMaxVital(20f).WithCanBeIncapacitated(true).WithIncapacitationDuration(3f).Build());
            pawn.ApplyDamage(new DamageInfo(50f));

            pawn.TryRevive(ReviveInfo.Full);
            pawn.Tick(1000f);

            Assert.AreEqual(PawnState.Alive, pawn.State, "A revived pawn must not bleed out from a timer that no longer applies.");
        }

        [Test]
        public void Kill_FinishesAnIncapacitatedPawnDirectly()
        {
            pawn = SpawnWith(new PawnDefinitionBuilder().WithId("a").WithMaxVital(20f).WithCanBeIncapacitated(true).Build());
            pawn.ApplyDamage(new DamageInfo(50f));

            pawn.Kill();

            Assert.AreEqual(PawnState.Dead, pawn.State);
        }

        [Test]
        public void Kill_OnALivingIncapacitatablePawn_GoesStraightToDead()
        {
            pawn = SpawnWith(new PawnDefinitionBuilder().WithId("a").WithMaxVital(20f).WithCanBeIncapacitated(true).Build());

            pawn.Kill();

            Assert.AreEqual(PawnState.Dead, pawn.State, "Kill never routes through Incapacitated, even when the pawn supports it.");
        }

        [Test]
        public void Possession_IsBlockedWhileIncapacitated()
        {
            pawn = SpawnWith(new PawnDefinitionBuilder().WithId("a").WithMaxVital(20f).WithCanBeIncapacitated(true).Build());
            pawn.ApplyDamage(new DamageInfo(50f));

            Assert.AreEqual(PossessionResult.PawnIncapacitated, pawn.TryPossess(new FakePawnPossessor()));
        }

        [Test]
        public void Spawn_WhileIncapacitated_IsRejectedAsAlreadySpawned()
        {
            pawn = SpawnWith(new PawnDefinitionBuilder().WithId("a").WithMaxVital(20f).WithCanBeIncapacitated(true).Build());
            pawn.ApplyDamage(new DamageInfo(50f));

            Assert.AreEqual(SpawnResult.AlreadySpawned, pawn.Spawn());
        }

        [Test]
        public void ReleaseOnIncapacitation_Default_KeepsThePossessorUntilDeath()
        {
            pawn = SpawnWith(new PawnDefinitionBuilder().WithId("a").WithMaxVital(20f).WithCanBeIncapacitated(true).Build());
            var possessor = new FakePawnPossessor();
            pawn.TryPossess(possessor);

            pawn.ApplyDamage(new DamageInfo(50f));

            Assert.AreSame(possessor, pawn.CurrentPossessor, "By default a downed pawn keeps crawling under its own control.");

            pawn.ApplyDamage(new DamageInfo(1f));

            Assert.IsNull(pawn.CurrentPossessor, "Actual death still releases the possessor.");
        }

        [Test]
        public void ReleaseOnIncapacitation_WhenSet_ReleasesImmediately()
        {
            pawn = SpawnWith(new PawnDefinitionBuilder()
                .WithId("a").WithMaxVital(20f).WithCanBeIncapacitated(true).WithReleaseOnIncapacitation(true).Build());
            var possessor = new FakePawnPossessor();
            pawn.TryPossess(possessor);

            pawn.ApplyDamage(new DamageInfo(50f));

            Assert.IsNull(pawn.CurrentPossessor);
            Assert.AreEqual(1, possessor.ReleasedCount);
        }

        [Test]
        public void ExternalVitalDrain_OnAnIncapacitatablePawn_DownsInsteadOfKilling()
        {
            pawn = SpawnWith(new PawnDefinitionBuilder().WithId("a").WithMaxVital(20f).WithCanBeIncapacitated(true).Build());

            pawn.Vitals.ApplyDelta(-100f);

            Assert.AreEqual(PawnState.Incapacitated, pawn.State,
                "Vitals drained by something other than ApplyDamage — an ability cost, a bound attribute — still routes through incapacitation.");
        }

        [Test]
        public void Heal_WhileIncapacitated_DoesNothing()
        {
            pawn = SpawnWith(new PawnDefinitionBuilder().WithId("a").WithMaxVital(20f).WithCanBeIncapacitated(true).Build());
            pawn.ApplyDamage(new DamageInfo(50f));

            Assert.AreEqual(0f, pawn.Heal(10f), "A stray heal must not pick a downed pawn back up; TryRevive is the only path.");
            Assert.AreEqual(PawnState.Incapacitated, pawn.State);
        }

        [Test]
        public void IsIncapacitated_ReflectsState()
        {
            pawn = SpawnWith(new PawnDefinitionBuilder().WithId("a").WithMaxVital(20f).WithCanBeIncapacitated(true).Build());

            Assert.IsFalse(pawn.IsIncapacitated);

            pawn.ApplyDamage(new DamageInfo(50f));

            Assert.IsTrue(pawn.IsIncapacitated);
        }

        private static Pawn SpawnWith(PawnDefinition definition)
        {
            Pawn created = TestPawns.Create(definition);
            created.Spawn();
            return created;
        }
    }
}
