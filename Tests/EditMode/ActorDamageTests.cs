using System.Collections.Generic;
using EldritchGames.PawnSystem.Identity;
using EldritchGames.PawnSystem.TestUtilities;
using EldritchGames.PawnSystem.Vitals;
using NUnit.Framework;
using UnityEngine;

namespace EldritchGames.PawnSystem.Tests.EditMode
{
    /// <summary>
    /// Covers <see cref="Actor"/>'s damage pipeline, kill/heal, incapacitation and revival — the
    /// same contract as <c>PawnDamageTests</c>/<c>PawnIncapacitationTests</c>/<c>PawnReviveTests</c>,
    /// consolidated into one file since <see cref="Actor"/>'s surface is smaller than <see cref="Pawn"/>'s.
    /// </summary>
    public class ActorDamageTests
    {
        private Actor actor;

        [TearDown]
        public void TearDown()
        {
            if (actor != null) Object.DestroyImmediate(actor.gameObject);
        }

        [Test]
        public void ApplyDamage_ReducesVitalsAndReturnsWhatItTook()
        {
            actor = SpawnWith(new ActorDefinitionBuilder().WithId("a").WithMaxVital(100f).Build());

            float applied = actor.ApplyDamage(new DamageInfo(30f));

            Assert.AreEqual(30f, applied, 0.0001f);
            Assert.AreEqual(70f, actor.Vitals.Current, 0.0001f);
        }

        [Test]
        public void ApplyDamage_BeyondRemainingVitals_ClampsAndDestroys()
        {
            actor = SpawnWith(new ActorDefinitionBuilder().WithId("a").WithMaxVital(20f).Build());

            float applied = actor.ApplyDamage(new DamageInfo(50f));

            Assert.AreEqual(20f, applied, 0.0001f, "An actor can only lose the vitals it still has.");
            Assert.AreEqual(PawnState.Dead, actor.State);
        }

        [Test]
        public void ApplyDamage_WhenNotPositive_IsIgnored()
        {
            actor = SpawnWith(new ActorDefinitionBuilder().WithId("a").WithMaxVital(100f).Build());

            Assert.AreEqual(0f, actor.ApplyDamage(new DamageInfo(0f)));
            Assert.AreEqual(0f, actor.ApplyDamage(new DamageInfo(-10f)), "Negative damage is not healing.");
            Assert.AreEqual(100f, actor.Vitals.Current, 0.0001f);
        }

        [Test]
        public void ApplyDamage_WhenDead_IsIgnored()
        {
            actor = SpawnWith(new ActorDefinitionBuilder().WithId("a").WithMaxVital(10f).Build());
            actor.Kill();

            int raised = 0;
            actor.DamageTaken += (_, __) => raised++;

            Assert.AreEqual(0f, actor.ApplyDamage(new DamageInfo(5f)), "A wreck does not take further damage.");
            Assert.AreEqual(0, raised);
        }

        [Test]
        public void ApplyDamage_WhileInvulnerable_IsIgnoredUnlessTheDamageTypeIgnoresIt()
        {
            actor = SpawnWith(new ActorDefinitionBuilder().WithId("a").WithMaxVital(100f).Build());
            actor.SetInvulnerable(true);

            Assert.AreEqual(0f, actor.ApplyDamage(new DamageInfo(40f)));

            var scripted = ScriptableObject.CreateInstance<DamageTypeDefinition>();
            typeof(DamageTypeDefinition)
                .GetField("ignoresInvulnerability", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(scripted, true);

            Assert.AreEqual(40f, actor.ApplyDamage(new DamageInfo(40f, scripted)), 0.0001f,
                "A scripted or story damage type pierces invulnerability, exactly as it does for a Pawn.");
        }

        [Test]
        public void ApplyDamage_RunsDefinitionModifiersInOrder()
        {
            var log = new List<string>();
            var halve = new FakeDamageModifier(amount => amount * 0.5f, "halve", log);
            var flat = new FakeDamageModifier(amount => amount - 5f, "flat", log);

            actor = SpawnWith(new ActorDefinitionBuilder()
                .WithId("a")
                .WithMaxVital(100f)
                .WithDamageModifiers(halve, flat)
                .Build());

            float applied = actor.ApplyDamage(new DamageInfo(100f));

            Assert.AreEqual(45f, applied, 0.0001f, "Modifiers compose in list order: halve 100 to 50, then subtract 5.");
            CollectionAssert.AreEqual(new[] { "halve", "flat" }, log);
            Assert.AreSame(actor, halve.LastTarget, "The modifier receives this actor as an IDamageTarget, the same interface a Pawn is passed through.");
        }

        [Test]
        public void Kill_IgnoresInvulnerabilityAndFiresDiedOnce()
        {
            actor = SpawnWith(new ActorDefinitionBuilder().WithId("a").WithMaxVital(100f).Build());
            actor.SetInvulnerable(true);

            int raised = 0;
            actor.Died += (_, __) => raised++;

            actor.Kill();
            actor.Kill();

            Assert.AreEqual(PawnState.Dead, actor.State, "Kill is the scripted-destruction path and ignores invulnerability.");
            Assert.AreEqual(0f, actor.Vitals.Current, 0.0001f);
            Assert.AreEqual(1, raised, "Died fires exactly once per death.");
        }

        [Test]
        public void Heal_RestoresVitalsUpToTheMaximum()
        {
            actor = SpawnWith(new ActorDefinitionBuilder().WithId("a").WithMaxVital(100f).Build());
            actor.ApplyDamage(new DamageInfo(60f));

            float healed = actor.Heal(100f);

            Assert.AreEqual(60f, healed, 0.0001f, "Healing stops at the maximum.");
            Assert.AreEqual(100f, actor.Vitals.Current, 0.0001f);
        }

        [Test]
        public void Heal_OnADestroyedActor_DoesNothing()
        {
            actor = SpawnWith(new ActorDefinitionBuilder().WithId("a").WithMaxVital(100f).Build());
            actor.Kill();

            Assert.AreEqual(0f, actor.Heal(50f), "A stray heal must never bring back a wreck; that is what TryRevive is for.");
            Assert.AreEqual(PawnState.Dead, actor.State);
        }

        [Test]
        public void ExternalVitalDrain_DestroysTheActor()
        {
            actor = SpawnWith(new ActorDefinitionBuilder().WithId("a").WithMaxVital(100f).Build());

            actor.Vitals.ApplyDelta(-100f);

            Assert.AreEqual(PawnState.Dead, actor.State,
                "Vitals drained by something other than ApplyDamage still destroy the actor.");
        }

        [Test]
        public void FatalBlow_OnAnIncapacitatableDefinition_DownsInsteadOfDestroying()
        {
            actor = SpawnWith(new ActorDefinitionBuilder().WithId("a").WithMaxVital(20f).WithCanBeIncapacitated(true).Build());

            int diedCount = 0;
            int incapacitatedCount = 0;
            actor.Died += (_, __) => diedCount++;
            actor.Incapacitated += (_, __) => incapacitatedCount++;

            actor.ApplyDamage(new DamageInfo(50f));

            Assert.AreEqual(PawnState.Incapacitated, actor.State);
            Assert.AreEqual(1, incapacitatedCount);
            Assert.AreEqual(0, diedCount, "A downed actor has not been destroyed yet.");
        }

        [Test]
        public void FatalBlow_WithoutCanBeIncapacitated_DestroysAsBefore()
        {
            actor = SpawnWith(new ActorDefinitionBuilder().WithId("a").WithMaxVital(20f).Build());

            actor.ApplyDamage(new DamageInfo(50f));

            Assert.AreEqual(PawnState.Dead, actor.State, "Incapacitation is opt-in; everyone else is destroyed exactly as before.");
        }

        [Test]
        public void AnyFurtherHit_WhileIncapacitated_FinishesTheActor()
        {
            actor = SpawnWith(new ActorDefinitionBuilder().WithId("a").WithMaxVital(20f).WithCanBeIncapacitated(true).Build());
            actor.ApplyDamage(new DamageInfo(50f));

            float applied = actor.ApplyDamage(new DamageInfo(1f));

            Assert.AreEqual(PawnState.Dead, actor.State, "Any qualifying hit on a downed actor finishes it.");
            Assert.AreEqual(0f, applied, 0.0001f);
        }

        [Test]
        public void TryIncapacitate_OnALivingActor_DownsItWithoutTouchingVitals()
        {
            actor = SpawnWith(new ActorDefinitionBuilder().WithId("a").WithMaxVital(20f).Build());

            bool downed = actor.TryIncapacitate();

            Assert.IsTrue(downed);
            Assert.AreEqual(PawnState.Incapacitated, actor.State, "A knock-out does not depend on CanBeIncapacitated; that flag only governs fatal blows.");
            Assert.AreEqual(20f, actor.Vitals.Current, 0.0001f, "A knock-out is not damage.");
        }

        [Test]
        public void TryIncapacitate_WhenNotAlive_ReturnsFalse()
        {
            actor = SpawnWith(new ActorDefinitionBuilder().WithId("a").WithMaxVital(20f).Build());
            actor.Kill();

            Assert.IsFalse(actor.TryIncapacitate(), "A wreck cannot be knocked out.");
        }

        [Test]
        public void TryRevive_OnADestroyedActor_BringsItBackAtFullVitals()
        {
            actor = SpawnWith(new ActorDefinitionBuilder().WithId("a").WithMaxVital(100f).Build());
            actor.Kill();

            ReviveResult result = actor.TryRevive(ReviveInfo.Full);

            Assert.AreEqual(ReviveResult.Success, result);
            Assert.AreEqual(PawnState.Alive, actor.State);
            Assert.AreEqual(100f, actor.Vitals.Current, 0.0001f);
        }

        [Test]
        public void TryRevive_OnAnIncapacitatedActor_RecoversItTheSameWayAsADestroyedOne()
        {
            actor = SpawnWith(new ActorDefinitionBuilder().WithId("a").WithMaxVital(20f).WithCanBeIncapacitated(true).Build());
            actor.ApplyDamage(new DamageInfo(50f));

            Assert.AreEqual(ReviveResult.Success, actor.TryRevive(ReviveInfo.Full));
            Assert.AreEqual(PawnState.Alive, actor.State);
        }

        [Test]
        public void TryRevive_OnALivingActor_ReturnsNotDead()
        {
            actor = SpawnWith(new ActorDefinitionBuilder().WithId("a").WithMaxVital(100f).Build());
            actor.ApplyDamage(new DamageInfo(40f));

            ReviveResult result = actor.TryRevive(ReviveInfo.Full);

            Assert.AreEqual(ReviveResult.NotDead, result);
            Assert.AreEqual(60f, actor.Vitals.Current, 0.0001f, "A refused revival must not top the actor up.");
        }

        [Test]
        public void TryRevive_OnADespawnedActor_ReturnsNotSpawned()
        {
            actor = SpawnWith(new ActorDefinitionBuilder().WithId("a").WithMaxVital(100f).Build());
            actor.Kill();
            actor.Despawn();

            Assert.AreEqual(ReviveResult.NotSpawned, actor.TryRevive(ReviveInfo.Full));
        }

        [Test]
        public void TryRevive_RaisesRevivedAndGrantsTheRequestedGracePeriod()
        {
            actor = SpawnWith(new ActorDefinitionBuilder().WithId("a").WithMaxVital(100f).Build());
            actor.Kill();

            int raised = 0;
            actor.Revived += _ => raised++;

            actor.TryRevive(new ReviveInfo(1f, invulnerabilityDuration: 3f));

            Assert.AreEqual(1, raised);
            Assert.IsTrue(actor.IsInvulnerable, "A revived actor gets a grace period so it is not destroyed again instantly.");
            Assert.AreEqual(3f, actor.InvulnerabilityRemaining, 0.0001f);
        }

        private static Actor SpawnWith(ActorDefinition definition)
        {
            Actor created = TestActors.Create(definition);
            created.Spawn();
            return created;
        }
    }
}
