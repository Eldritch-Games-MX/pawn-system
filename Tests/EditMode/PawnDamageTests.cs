using System.Collections.Generic;
using EldritchGames.PawnSystem.Identity;
using EldritchGames.PawnSystem.TestUtilities;
using EldritchGames.PawnSystem.Vitals;
using NUnit.Framework;
using UnityEngine;

namespace EldritchGames.PawnSystem.Tests.EditMode
{
    public class PawnDamageTests
    {
        private Pawn pawn;

        [TearDown]
        public void TearDown()
        {
            if (pawn != null) Object.DestroyImmediate(pawn.gameObject);
        }

        [Test]
        public void ApplyDamage_ReducesVitalsAndReturnsWhatItTook()
        {
            pawn = SpawnWith(new PawnDefinitionBuilder().WithId("a").WithMaxVital(100f).Build());

            float applied = pawn.ApplyDamage(new DamageInfo(30f));

            Assert.AreEqual(30f, applied, 0.0001f);
            Assert.AreEqual(70f, pawn.Vitals.Current, 0.0001f);
        }

        [Test]
        public void ApplyDamage_BeyondRemainingVitals_ClampsAndKills()
        {
            pawn = SpawnWith(new PawnDefinitionBuilder().WithId("a").WithMaxVital(20f).Build());

            float applied = pawn.ApplyDamage(new DamageInfo(50f));

            Assert.AreEqual(20f, applied, 0.0001f, "A pawn can only lose the vitals it still has.");
            Assert.AreEqual(PawnState.Dead, pawn.State);
        }

        [Test]
        public void ApplyDamage_WhenFatal_ReportsOverkillOnTheDeathRecord()
        {
            pawn = SpawnWith(new PawnDefinitionBuilder().WithId("a").WithMaxVital(20f).Build());

            DeathInfo captured = default;
            pawn.Died += (_, info) => captured = info;

            pawn.ApplyDamage(new DamageInfo(35f));

            Assert.AreEqual(15f, captured.Overkill, 0.0001f, "Overkill is the part of the final blow that went past zero.");
        }

        [Test]
        public void ApplyDamage_WhenNotPositive_IsIgnored()
        {
            pawn = SpawnWith(new PawnDefinitionBuilder().WithId("a").WithMaxVital(100f).Build());

            Assert.AreEqual(0f, pawn.ApplyDamage(new DamageInfo(0f)));
            Assert.AreEqual(0f, pawn.ApplyDamage(new DamageInfo(-10f)), "Negative damage is not healing.");
            Assert.AreEqual(100f, pawn.Vitals.Current, 0.0001f);
        }

        [Test]
        public void ApplyDamage_WhenDead_IsIgnored()
        {
            pawn = SpawnWith(new PawnDefinitionBuilder().WithId("a").WithMaxVital(10f).Build());
            pawn.Kill();

            int raised = 0;
            pawn.DamageTaken += (_, __) => raised++;

            Assert.AreEqual(0f, pawn.ApplyDamage(new DamageInfo(5f)), "Corpses do not take damage.");
            Assert.AreEqual(0, raised);
        }

        [Test]
        public void ApplyDamage_WhileInvulnerable_IsIgnored()
        {
            pawn = SpawnWith(new PawnDefinitionBuilder().WithId("a").WithMaxVital(100f).Build());
            pawn.SetInvulnerable(true);

            Assert.AreEqual(0f, pawn.ApplyDamage(new DamageInfo(40f)));
            Assert.AreEqual(100f, pawn.Vitals.Current, 0.0001f);
        }

        [Test]
        public void ApplyDamage_WhileInvulnerable_StillLandsWhenTheDamageTypeIgnoresIt()
        {
            pawn = SpawnWith(new PawnDefinitionBuilder().WithId("a").WithMaxVital(100f).Build());
            pawn.SetInvulnerable(true);

            DamageTypeDefinition scripted = CreateDamageType(ignoresInvulnerability: true);

            Assert.AreEqual(40f, pawn.ApplyDamage(new DamageInfo(40f, scripted)), 0.0001f,
                "A scripted or story damage type pierces invulnerability.");
        }

        [Test]
        public void ApplyDamage_RunsDefinitionModifiersInOrder()
        {
            var log = new List<string>();
            var halve = new FakeDamageModifier(amount => amount * 0.5f, "halve", log);
            var flat = new FakeDamageModifier(amount => amount - 5f, "flat", log);

            pawn = SpawnWith(new PawnDefinitionBuilder()
                .WithId("a")
                .WithMaxVital(100f)
                .WithDamageModifiers(halve, flat)
                .Build());

            float applied = pawn.ApplyDamage(new DamageInfo(100f));

            Assert.AreEqual(45f, applied, 0.0001f, "Modifiers compose in list order: halve 100 to 50, then subtract 5.");
            CollectionAssert.AreEqual(new[] { "halve", "flat" }, log, "Order is part of the damage pipeline contract.");
        }

        [Test]
        public void ApplyDamage_WhenModifiersAbsorbEverything_StillRaisesDamageTaken()
        {
            var absorb = new FakeDamageModifier(_ => 0f, "absorb");
            pawn = SpawnWith(new PawnDefinitionBuilder()
                .WithId("a")
                .WithMaxVital(100f)
                .WithDamageModifiers(absorb)
                .Build());

            DamageInfo captured = new DamageInfo(-1f);
            pawn.DamageTaken += (_, info) => captured = info;

            float applied = pawn.ApplyDamage(new DamageInfo(60f));

            Assert.AreEqual(0f, applied, 0.0001f);
            Assert.AreEqual(0f, captured.Amount, 0.0001f, "A fully blocked hit still reports itself so feedback can play.");
        }

        [Test]
        public void ApplyDamage_CarriesProvenanceIntoTheDeathRecord()
        {
            Pawn attacker = TestPawns.CreateSpawned(new PawnDefinitionBuilder().WithId("attacker").Build(), "Attacker");
            pawn = SpawnWith(new PawnDefinitionBuilder().WithId("victim").WithMaxVital(10f).Build());

            DamageTypeDefinition fire = CreateDamageType();
            DeathInfo captured = default;
            pawn.Died += (_, info) => captured = info;

            try
            {
                pawn.ApplyDamage(new DamageInfo(10f, fire, attacker, bodyPart: "Body.Head"));

                Assert.AreSame(attacker, captured.Instigator, "Kill credit comes from the damage that landed.");
                Assert.AreSame(fire, captured.DamageType);
                Assert.AreEqual("Body.Head", captured.BodyPart.Value);
            }
            finally
            {
                Object.DestroyImmediate(attacker.gameObject);
            }
        }

        [Test]
        public void Kill_IgnoresInvulnerabilityAndFiresDiedOnce()
        {
            pawn = SpawnWith(new PawnDefinitionBuilder().WithId("a").WithMaxVital(100f).Build());
            pawn.SetInvulnerable(true);

            int raised = 0;
            pawn.Died += (_, __) => raised++;

            pawn.Kill();
            pawn.Kill();

            Assert.AreEqual(PawnState.Dead, pawn.State, "Kill is the scripted-death path and ignores invulnerability.");
            Assert.AreEqual(0f, pawn.Vitals.Current, 0.0001f);
            Assert.AreEqual(1, raised, "Died fires exactly once per death.");
        }

        [Test]
        public void Heal_RestoresVitalsUpToTheMaximum()
        {
            pawn = SpawnWith(new PawnDefinitionBuilder().WithId("a").WithMaxVital(100f).Build());
            pawn.ApplyDamage(new DamageInfo(60f));

            float healed = pawn.Heal(100f);

            Assert.AreEqual(60f, healed, 0.0001f, "Healing stops at the maximum.");
            Assert.AreEqual(100f, pawn.Vitals.Current, 0.0001f);
        }

        [Test]
        public void Heal_OnADeadPawn_DoesNothing()
        {
            pawn = SpawnWith(new PawnDefinitionBuilder().WithId("a").WithMaxVital(100f).Build());
            pawn.Kill();

            Assert.AreEqual(0f, pawn.Heal(50f), "A stray heal must never resurrect a corpse; that is what TryRevive is for.");
            Assert.AreEqual(PawnState.Dead, pawn.State);
        }

        [Test]
        public void ExternalVitalDrain_KillsThePawn()
        {
            pawn = SpawnWith(new PawnDefinitionBuilder().WithId("a").WithMaxVital(100f).Build());

            pawn.Vitals.ApplyDelta(-100f);

            Assert.AreEqual(PawnState.Dead, pawn.State,
                "Vitals drained by something other than ApplyDamage — an ability cost, a bound attribute — still kill the pawn.");
        }

        private static Pawn SpawnWith(PawnDefinition definition)
        {
            Pawn created = TestPawns.Create(definition);
            created.Spawn();
            return created;
        }

        private static DamageTypeDefinition CreateDamageType(bool ignoresInvulnerability = false)
        {
            var type = ScriptableObject.CreateInstance<DamageTypeDefinition>();
            typeof(DamageTypeDefinition)
                .GetField("ignoresInvulnerability", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(type, ignoresInvulnerability);
            return type;
        }
    }
}
