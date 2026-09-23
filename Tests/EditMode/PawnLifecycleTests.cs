using EldritchGames.PawnSystem.Identity;
using EldritchGames.PawnSystem.TestUtilities;
using NUnit.Framework;
using UnityEngine;

namespace EldritchGames.PawnSystem.Tests.EditMode
{
    public class PawnLifecycleTests
    {
        private PawnDefinition definition;
        private Pawn pawn;

        [SetUp]
        public void SetUp()
        {
            definition = new PawnDefinitionBuilder().WithId("knight").WithMaxVital(100f).Build();
            pawn = TestPawns.Create(definition);
        }

        [TearDown]
        public void TearDown()
        {
            if (pawn != null) Object.DestroyImmediate(pawn.gameObject);
        }

        [Test]
        public void NewPawn_IsUnspawned()
        {
            Assert.AreEqual(PawnState.Unspawned, pawn.State, "A pawn that nothing has spawned is not in play yet.");
            Assert.IsFalse(pawn.IsAlive, "An unspawned pawn is not alive.");
        }

        [Test]
        public void Spawn_FromUnspawned_MakesThePawnAliveAtFullVitals()
        {
            SpawnResult result = pawn.Spawn();

            Assert.AreEqual(SpawnResult.Success, result);
            Assert.AreEqual(PawnState.Alive, pawn.State);
            Assert.AreEqual(100f, pawn.Vitals.Current, 0.0001f, "Spawning fills vitals to the definition's maximum.");
        }

        [Test]
        public void Spawn_PlacesThePawnAtTheGivenPose()
        {
            pawn.Spawn(new Vector3(3f, 0f, 4f), Quaternion.Euler(0f, 90f, 0f));

            Assert.AreEqual(new Vector3(3f, 0f, 4f), pawn.transform.position);
            Assert.AreEqual(90f, pawn.transform.rotation.eulerAngles.y, 0.001f);
        }

        [Test]
        public void Spawn_WhenAlreadyAlive_IsRejectedAndChangesNothing()
        {
            pawn.Spawn();
            pawn.ApplyDamage(new Vitals.DamageInfo(40f));

            SpawnResult result = pawn.Spawn();

            Assert.AreEqual(SpawnResult.AlreadySpawned, result);
            Assert.AreEqual(60f, pawn.Vitals.Current, 0.0001f, "A rejected spawn must not refill vitals.");
        }

        [Test]
        public void Spawn_WithoutDefinition_ReturnsMissingDefinition()
        {
            Pawn orphan = TestPawns.Create(null, "Orphan");

            try
            {
                Assert.AreEqual(SpawnResult.MissingDefinition, orphan.Spawn());
                Assert.AreEqual(PawnState.Unspawned, orphan.State, "A failed spawn leaves the pawn untouched.");
            }
            finally
            {
                Object.DestroyImmediate(orphan.gameObject);
            }
        }

        [Test]
        public void Spawn_CopiesTeamAndTagsFromTheDefinition()
        {
            TeamDefinition team = TestTeams.Create("players");
            PawnDefinition tagged = new PawnDefinitionBuilder()
                .WithId("rogue")
                .WithTeam(team)
                .WithTags("Class.Rogue", "Faction.Guild")
                .Build();

            Pawn rogue = TestPawns.Create(tagged, "Rogue");

            try
            {
                rogue.Spawn();

                Assert.AreSame(team, rogue.Team, "Spawning adopts the definition's team.");
                Assert.IsTrue(rogue.Tags.Has("Class"), "Tags are seeded from the definition and match hierarchically.");
                Assert.IsTrue(rogue.Tags.Has("Faction.Guild"));
            }
            finally
            {
                Object.DestroyImmediate(rogue.gameObject);
            }
        }

        [Test]
        public void Spawn_RaisesSpawnedOnce()
        {
            int raised = 0;
            pawn.Spawned += _ => raised++;

            pawn.Spawn();
            pawn.Spawn();

            Assert.AreEqual(1, raised, "The rejected second spawn must not raise the event again.");
        }

        [Test]
        public void Despawn_TakesThePawnOutOfPlayAndDeactivatesTheGameObject()
        {
            pawn.Spawn();

            pawn.Despawn();

            Assert.AreEqual(PawnState.Despawned, pawn.State);
            Assert.IsFalse(pawn.gameObject.activeSelf, "The definition asks for deactivation on despawn by default.");
        }

        [Test]
        public void Despawn_WhenAlreadyDespawned_RaisesNothing()
        {
            pawn.Spawn();
            pawn.Despawn();

            int raised = 0;
            pawn.Despawned += _ => raised++;
            pawn.Despawn();

            Assert.AreEqual(0, raised, "Despawning twice is a no-op.");
        }

        [Test]
        public void Spawn_AfterDespawn_BringsThePawnBack()
        {
            pawn.Spawn();
            pawn.ApplyDamage(new Vitals.DamageInfo(30f));
            pawn.Despawn();

            SpawnResult result = pawn.Spawn();

            Assert.AreEqual(SpawnResult.Success, result, "A despawned pawn can be pooled and spawned again.");
            Assert.AreEqual(PawnState.Alive, pawn.State);
            Assert.AreEqual(100f, pawn.Vitals.Current, 0.0001f, "Respawning restores full vitals.");
            Assert.IsTrue(pawn.gameObject.activeSelf);
        }

        [Test]
        public void Tick_CountsDownTheSpawnGracePeriod()
        {
            PawnDefinition guarded = new PawnDefinitionBuilder().WithId("guarded").WithSpawnInvulnerability(2f).Build();
            Pawn protectedPawn = TestPawns.Create(guarded, "Guarded");

            try
            {
                protectedPawn.Spawn();
                Assert.IsTrue(protectedPawn.IsInvulnerable, "A pawn is protected immediately after spawning.");

                protectedPawn.Tick(1.5f);
                Assert.IsTrue(protectedPawn.IsInvulnerable, "Half the grace period has not elapsed yet.");

                protectedPawn.Tick(1f);
                Assert.IsFalse(protectedPawn.IsInvulnerable, "The grace period ends once its duration has been ticked away.");
            }
            finally
            {
                Object.DestroyImmediate(protectedPawn.gameObject);
            }
        }

        [Test]
        public void ViewPoint_WithoutAnAssignedTransform_FallsBackToThePawnItself()
        {
            Assert.AreSame(pawn.transform, pawn.ViewPoint, "A pawn with no explicit view point looks from its own transform.");
        }
    }
}
