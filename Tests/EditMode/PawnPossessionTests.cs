using System;
using EldritchGames.PawnSystem.Identity;
using EldritchGames.PawnSystem.TestUtilities;
using NUnit.Framework;
using UnityEngine;

namespace EldritchGames.PawnSystem.Tests.EditMode
{
    public class PawnPossessionTests
    {
        private PawnDefinition definition;
        private Pawn pawn;
        private FakePawnPossessor possessor;

        [SetUp]
        public void SetUp()
        {
            definition = new PawnDefinitionBuilder().WithId("knight").WithMaxVital(50f).Build();
            pawn = TestPawns.Create(definition);
            possessor = new FakePawnPossessor();
        }

        [TearDown]
        public void TearDown()
        {
            if (pawn != null) UnityEngine.Object.DestroyImmediate(pawn.gameObject);
        }

        [Test]
        public void TryPossess_OnALivingPawn_SucceedsAndNotifiesThePossessor()
        {
            pawn.Spawn();

            PossessionResult result = pawn.TryPossess(possessor);

            Assert.AreEqual(PossessionResult.Success, result);
            Assert.AreSame(possessor, pawn.CurrentPossessor);
            Assert.AreEqual(1, possessor.PossessedCount);
            Assert.AreSame(pawn, possessor.CurrentPawn, "The possessor is told which pawn it took.");
        }

        [Test]
        public void TryPossess_WhenUnspawned_ReturnsNotSpawned()
        {
            PossessionResult result = pawn.TryPossess(possessor);

            Assert.AreEqual(PossessionResult.NotSpawned, result);
            Assert.IsNull(pawn.CurrentPossessor, "A failed possession leaves the pawn unpossessed.");
            Assert.AreEqual(0, possessor.PossessedCount, "A failed possession must not notify the possessor.");
        }

        [Test]
        public void TryPossess_WhenDead_ReturnsPawnDead()
        {
            pawn.Spawn();
            pawn.Kill();

            Assert.AreEqual(PossessionResult.PawnDead, pawn.TryPossess(possessor));
        }

        [Test]
        public void TryPossess_WhenAnotherPossessorHoldsThePawn_ReturnsAlreadyPossessed()
        {
            pawn.Spawn();
            pawn.TryPossess(possessor);

            var other = new FakePawnPossessor();
            PossessionResult result = pawn.TryPossess(other);

            Assert.AreEqual(PossessionResult.AlreadyPossessed, result);
            Assert.AreSame(possessor, pawn.CurrentPossessor, "The sitting possessor keeps the pawn.");
            Assert.AreEqual(0, possessor.ReleasedCount, "A refused possession must not disturb the current possessor.");
        }

        [Test]
        public void TryPossess_ByTheSamePossessorTwice_ReturnsAlreadyOwner()
        {
            pawn.Spawn();
            pawn.TryPossess(possessor);

            Assert.AreEqual(PossessionResult.AlreadyOwner, pawn.TryPossess(possessor));
            Assert.AreEqual(1, possessor.PossessedCount, "Re-possessing by the same possessor is a no-op.");
        }

        [Test]
        public void TryPossess_WithNull_Throws()
        {
            pawn.Spawn();

            Assert.Throws<ArgumentNullException>(() => pawn.TryPossess(null));
        }

        [Test]
        public void Possess_SwapsTheSittingPossessorOut()
        {
            pawn.Spawn();
            pawn.TryPossess(possessor);

            var other = new FakePawnPossessor();
            PossessionResult result = pawn.Possess(other);

            Assert.AreEqual(PossessionResult.Success, result);
            Assert.AreSame(other, pawn.CurrentPossessor);
            Assert.AreEqual(1, possessor.ReleasedCount, "The previous possessor is told it lost the pawn.");
        }

        [Test]
        public void Release_NotifiesThePossessorAndClearsIt()
        {
            pawn.Spawn();
            pawn.TryPossess(possessor);

            pawn.Release();

            Assert.IsNull(pawn.CurrentPossessor);
            Assert.AreEqual(1, possessor.ReleasedCount);
            Assert.AreSame(pawn, possessor.LastReleasedPawn);
        }

        [Test]
        public void Release_WhenUnpossessed_RaisesNothing()
        {
            pawn.Spawn();

            int raised = 0;
            pawn.Released += (_, __) => raised++;
            pawn.Release();

            Assert.AreEqual(0, raised, "Releasing a pawn nothing holds is a no-op.");
        }

        [Test]
        public void Death_ReleasesThePossessorByDefault()
        {
            pawn.Spawn();
            pawn.TryPossess(possessor);

            pawn.Kill();

            Assert.IsNull(pawn.CurrentPossessor, "A controller does not drive a corpse by default.");
            Assert.AreEqual(1, possessor.ReleasedCount);
        }

        [Test]
        public void Death_WithReleaseOnDeathOff_KeepsThePossessor()
        {
            PawnDefinition clingy = new PawnDefinitionBuilder().WithId("ghost").WithReleaseOnDeath(false).Build();
            Pawn ghost = TestPawns.Create(clingy, "Ghost");

            try
            {
                ghost.Spawn();
                ghost.TryPossess(possessor);
                ghost.Kill();

                Assert.AreSame(possessor, ghost.CurrentPossessor, "Games that act through corpses keep the possessor.");
                Assert.AreEqual(0, possessor.ReleasedCount);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(ghost.gameObject);
            }
        }

        [Test]
        public void Despawn_ReleasesThePossessor()
        {
            pawn.Spawn();
            pawn.TryPossess(possessor);

            pawn.Despawn();

            Assert.IsNull(pawn.CurrentPossessor, "Leaving the world always hands the body back.");
            Assert.AreEqual(1, possessor.ReleasedCount);
        }

        [Test]
        public void Possession_IsTheSameForPlayersAndAi()
        {
            pawn.Spawn();

            var brain = new FakePawnPossessor();
            var player = new FakePawnPossessor();

            Assert.AreEqual(PossessionResult.Success, pawn.TryPossess(brain), "An AI takes a body the same way a player does.");
            Assert.AreEqual(PossessionResult.Success, pawn.Possess(player), "A player can take over a body an AI was driving.");
            Assert.AreEqual(1, brain.ReleasedCount, "The AI is told it lost the body.");
        }
    }
}
