using EldritchGames.PawnSystem.TestUtilities;
using NUnit.Framework;
using UnityEngine;

namespace EldritchGames.PawnSystem.Tests.EditMode
{
    public class PawnAuthorityTests
    {
        private Pawn pawn;

        [SetUp]
        public void SetUp()
        {
            pawn = TestPawns.CreateSpawned(new PawnDefinitionBuilder().WithId("a").Build());
        }

        [TearDown]
        public void TearDown()
        {
            if (pawn != null) UnityEngine.Object.DestroyImmediate(pawn.gameObject);
        }

        [Test]
        public void WithNoAuthorityAssigned_HasAuthorityIsTrue()
        {
            Assert.IsNull(pawn.Authority, "A single-player or non-networked pawn never has to know this exists.");
            Assert.IsTrue(pawn.HasAuthority);
        }

        [Test]
        public void HasAuthority_ReflectsTheAssignedAuthority()
        {
            pawn.Authority = new FakePawnAuthority(hasAuthority: false);
            Assert.IsFalse(pawn.HasAuthority);

            pawn.Authority = new FakePawnAuthority(hasAuthority: true);
            Assert.IsTrue(pawn.HasAuthority);
        }

        [Test]
        public void ClearingAuthority_RevertsToAlwaysTrue()
        {
            pawn.Authority = new FakePawnAuthority(hasAuthority: false);
            pawn.Authority = null;

            Assert.IsTrue(pawn.HasAuthority);
        }

        [Test]
        public void Authority_IsAMarkerOnly_NothingEnforcesIt()
        {
            pawn.Authority = new FakePawnAuthority(hasAuthority: false);

            // The package never checks HasAuthority itself — every operation still works locally.
            float applied = pawn.ApplyDamage(new Vitals.DamageInfo(10f));

            Assert.AreEqual(10f, applied, 0.0001f, "Enforcing authority is the game's job, not this package's.");
        }
    }
}
