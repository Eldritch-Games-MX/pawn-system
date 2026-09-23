using System.Reflection;
using EldritchGames.AbilitySystem;
using EldritchGames.AbilitySystem.Definitions;
using EldritchGames.PawnSystem.AbilitySystem;
using EldritchGames.PawnSystem.Identity;
using EldritchGames.PawnSystem.TestUtilities;
using EldritchGames.PawnSystem.Vitals;
using NUnit.Framework;
using UnityEngine;

namespace EldritchGames.PawnSystem.AbilitySystem.Tests.EditMode
{
    public class AttributeVitalSourceTests
    {
        private Pawn pawn;
        private AbilitySystemComponent abilitySystem;
        private AttributeDefinition health;

        [SetUp]
        public void SetUp()
        {
            PawnDefinition definition = new PawnDefinitionBuilder().WithId("knight").WithMaxVital(999f).Build();
            pawn = TestPawns.Create(definition);

            abilitySystem = pawn.gameObject.AddComponent<AbilitySystemComponent>();
            health = CreateAttribute("health", defaultValue: 100f, min: 0f, max: 100f);
            abilitySystem.Attributes.RegisterAttribute(health);

            pawn.SetVitalSource(new AttributeVitalSource(abilitySystem, health));
            pawn.Spawn();
        }

        [TearDown]
        public void TearDown()
        {
            if (pawn != null) Object.DestroyImmediate(pawn.gameObject);
        }

        [Test]
        public void BoundPawn_ReadsItsVitalsFromTheAttribute()
        {
            Assert.AreEqual(100f, pawn.Vitals.Current, 0.0001f);
            Assert.AreEqual(100f, pawn.Vitals.Max, 0.0001f,
                "With no separate max attribute, the maximum comes from the attribute asset's own clamp.");
        }

        [Test]
        public void Damage_MovesTheAttribute()
        {
            pawn.ApplyDamage(new DamageInfo(30f));

            Assert.AreEqual(70f, abilitySystem.Attributes.GetCurrentValue(health), 0.0001f,
                "Damage and abilities move one number, not two that drift apart.");
        }

        [Test]
        public void AnAbilityDrainingTheAttribute_KillsThePawn()
        {
            abilitySystem.Attributes.ModifyBaseValue(health, -100f);

            Assert.AreEqual(PawnState.Dead, pawn.State,
                "The pawn dies when its bound attribute empties, whoever emptied it.");
        }

        [Test]
        public void Healing_MovesTheAttributeBack()
        {
            pawn.ApplyDamage(new DamageInfo(40f));

            pawn.Heal(15f);

            Assert.AreEqual(75f, abilitySystem.Attributes.GetCurrentValue(health), 0.0001f);
        }

        [Test]
        public void Spawning_RefillsTheAttribute()
        {
            pawn.ApplyDamage(new DamageInfo(60f));
            pawn.Despawn();

            pawn.Spawn();

            Assert.AreEqual(100f, abilitySystem.Attributes.GetCurrentValue(health), 0.0001f);
        }

        [Test]
        public void SetMax_WithoutAMaxAttribute_ExplainsWhyItCannot()
        {
            Assert.Throws<System.InvalidOperationException>(() => pawn.Vitals.SetMax(150f));
        }

        [Test]
        public void MaxAttribute_LetsBuffsRaiseTheCeiling()
        {
            var host = new GameObject("Buffed");

            try
            {
                var buffed = host.AddComponent<AbilitySystemComponent>();
                AttributeDefinition current = CreateAttribute("hp", 50f, 0f, float.PositiveInfinity);
                AttributeDefinition maximum = CreateAttribute("maxHp", 50f, 0f, float.PositiveInfinity);
                buffed.Attributes.RegisterAttribute(current);
                buffed.Attributes.RegisterAttribute(maximum);

                var source = new AttributeVitalSource(buffed, current, maximum);

                source.SetMax(80f, fillToMax: true);

                Assert.AreEqual(80f, source.Max, 0.0001f);
                Assert.AreEqual(80f, source.Current, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void BindingAnUnregisteredAttribute_ExplainsTheFix()
        {
            AttributeDefinition unregistered = CreateAttribute("mana", 10f, 0f, 10f);

            Assert.Throws<System.InvalidOperationException>(
                () => new AttributeVitalSource(abilitySystem, unregistered));
        }

        private static AttributeDefinition CreateAttribute(string id, float defaultValue, float min, float max)
        {
            var definition = ScriptableObject.CreateInstance<AttributeDefinition>();
            SetField(definition, "id", id);
            SetField(definition, "displayName", id);
            SetField(definition, "defaultValue", defaultValue);
            SetField(definition, "minValue", min);
            SetField(definition, "maxValue", max);
            return definition;
        }

        private static void SetField(AttributeDefinition definition, string fieldName, object value)
        {
            FieldInfo field = typeof(AttributeDefinition).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(definition, value);
        }
    }
}
