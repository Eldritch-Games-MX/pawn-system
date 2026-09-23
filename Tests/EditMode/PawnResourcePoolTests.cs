using System;
using EldritchGames.PawnSystem.TestUtilities;
using EldritchGames.PawnSystem.Vitals;
using NUnit.Framework;

namespace EldritchGames.PawnSystem.Tests.EditMode
{
    public class PawnResourcePoolTests
    {
        private PawnResourcePool pool;
        private ResourceDefinition mana;

        [SetUp]
        public void SetUp()
        {
            pool = new PawnResourcePool();
            mana = TestResources.Create("mana", defaultMax: 50f);
        }

        [Test]
        public void Register_UsesTheDefinitionsDefaultMax_AndStartsFull()
        {
            pool.Register(mana);

            Assert.IsTrue(pool.IsRegistered(mana));
            Assert.AreEqual(50f, pool.GetCurrent(mana), 0.0001f);
            Assert.AreEqual(50f, pool.GetMax(mana), 0.0001f);
        }

        [Test]
        public void Register_IsIdempotent()
        {
            pool.Register(mana);
            pool.ApplyDelta(mana, -20f);

            pool.Register(mana);

            Assert.AreEqual(30f, pool.GetCurrent(mana), 0.0001f, "Registering an already-registered resource must not reset it.");
        }

        [Test]
        public void Register_WithExplicitValues_OverridesTheDefinitionDefault()
        {
            pool.Register(mana, max: 100f, current: 10f);

            Assert.AreEqual(100f, pool.GetMax(mana), 0.0001f);
            Assert.AreEqual(10f, pool.GetCurrent(mana), 0.0001f);
        }

        [Test]
        public void RegisterOrRefill_RegistersWhenNew()
        {
            pool.RegisterOrRefill(mana);

            Assert.IsTrue(pool.IsRegistered(mana));
            Assert.AreEqual(50f, pool.GetCurrent(mana), 0.0001f);
        }

        [Test]
        public void RegisterOrRefill_RefillsAnExistingPoolToTheDefinitionsDefaultMax()
        {
            pool.Register(mana);
            pool.ApplyDelta(mana, -40f);

            pool.RegisterOrRefill(mana);

            Assert.AreEqual(50f, pool.GetCurrent(mana), 0.0001f, "A respawn refills secondary resources exactly like vitals.");
        }

        [Test]
        public void Restore_OverwritesAnAlreadyRegisteredPool()
        {
            pool.Register(mana);

            pool.Restore(mana, max: 80f, current: 15f);

            Assert.AreEqual(80f, pool.GetMax(mana), 0.0001f);
            Assert.AreEqual(15f, pool.GetCurrent(mana), 0.0001f, "Unlike Register, Restore always applies the given values.");
        }

        [Test]
        public void ApplyDelta_ClampsAndReturnsWhatWasActuallyApplied()
        {
            pool.Register(mana);

            Assert.AreEqual(-50f, pool.ApplyDelta(mana, -1000f), 0.0001f);
            Assert.IsTrue(pool.IsDepleted(mana));
        }

        [Test]
        public void Changed_FiresWithTheResourceThatChanged()
        {
            pool.Register(mana);
            ResourceDefinition raised = null;
            pool.Changed += (resource, current, max) => raised = resource;

            pool.ApplyDelta(mana, -5f);

            Assert.AreSame(mana, raised);
        }

        [Test]
        public void UnregisteredResource_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => pool.GetCurrent(mana));
        }

        [Test]
        public void NullDefinition_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => pool.Register(null));
            Assert.Throws<ArgumentNullException>(() => pool.RegisterOrRefill(null));
            Assert.Throws<ArgumentNullException>(() => pool.Restore(null, 10f, 10f));
        }
    }
}
