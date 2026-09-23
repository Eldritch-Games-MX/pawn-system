using EldritchGames.PawnSystem.TestUtilities;
using EldritchGames.PawnSystem.Vitals;
using NUnit.Framework;
using UnityEngine;

namespace EldritchGames.PawnSystem.Tests.EditMode
{
    public class PawnResourceIntegrationTests
    {
        private Pawn pawn;
        private ResourceDefinition mana;

        [SetUp]
        public void SetUp()
        {
            mana = TestResources.Create("mana", defaultMax: 30f);
        }

        [TearDown]
        public void TearDown()
        {
            if (pawn != null) UnityEngine.Object.DestroyImmediate(pawn.gameObject);
        }

        [Test]
        public void Spawn_RegistersInitialResourcesFull()
        {
            pawn = TestPawns.Create(new PawnDefinitionBuilder().WithId("a").WithInitialResources(mana).Build());

            pawn.Spawn();

            Assert.IsTrue(pawn.Resources.IsRegistered(mana));
            Assert.AreEqual(30f, pawn.Resources.GetCurrent(mana), 0.0001f);
        }

        [Test]
        public void Respawn_RefillsInitialResources()
        {
            pawn = TestPawns.Create(new PawnDefinitionBuilder().WithId("a").WithInitialResources(mana).Build());
            pawn.Spawn();
            pawn.Resources.ApplyDelta(mana, -25f);
            pawn.Despawn();

            pawn.Spawn();

            Assert.AreEqual(30f, pawn.Resources.GetCurrent(mana), 0.0001f, "A fresh spawn refills secondary resources exactly like vitals.");
        }

        [Test]
        public void PawnWithNoInitialResources_HasAnEmptyPoolByDefault()
        {
            pawn = TestPawns.CreateSpawned(new PawnDefinitionBuilder().WithId("a").Build());

            Assert.IsFalse(pawn.Resources.IsRegistered(mana), "Resources are opt-in; nothing is registered unless the definition lists it.");
        }
    }
}
