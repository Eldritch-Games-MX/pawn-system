using EldritchGames.PawnSystem.Identity;
using EldritchGames.PawnSystem.TestUtilities;
using NUnit.Framework;
using UnityEngine;

namespace EldritchGames.PawnSystem.Tests.EditMode
{
    public class ActorLifecycleTests
    {
        private ActorDefinition definition;
        private Actor actor;

        [SetUp]
        public void SetUp()
        {
            definition = new ActorDefinitionBuilder().WithId("crate").WithMaxVital(100f).Build();
            actor = TestActors.Create(definition);
        }

        [TearDown]
        public void TearDown()
        {
            if (actor != null) Object.DestroyImmediate(actor.gameObject);
        }

        [Test]
        public void NewActor_IsUnspawned()
        {
            Assert.AreEqual(PawnState.Unspawned, actor.State, "An actor that nothing has spawned is not in play yet.");
            Assert.IsFalse(actor.IsAlive, "An unspawned actor is not alive.");
        }

        [Test]
        public void Spawn_FromUnspawned_MakesTheActorAliveAtFullVitals()
        {
            SpawnResult result = actor.Spawn();

            Assert.AreEqual(SpawnResult.Success, result);
            Assert.AreEqual(PawnState.Alive, actor.State);
            Assert.AreEqual(100f, actor.Vitals.Current, 0.0001f, "Spawning fills vitals to the definition's maximum.");
        }

        [Test]
        public void Spawn_PlacesTheActorAtTheGivenPose()
        {
            actor.Spawn(new Vector3(3f, 0f, 4f), Quaternion.Euler(0f, 90f, 0f));

            Assert.AreEqual(new Vector3(3f, 0f, 4f), actor.transform.position);
            Assert.AreEqual(90f, actor.transform.rotation.eulerAngles.y, 0.001f);
        }

        [Test]
        public void Spawn_WhenAlreadyAlive_IsRejectedAndChangesNothing()
        {
            actor.Spawn();
            actor.ApplyDamage(new Vitals.DamageInfo(40f));

            SpawnResult result = actor.Spawn();

            Assert.AreEqual(SpawnResult.AlreadySpawned, result);
            Assert.AreEqual(60f, actor.Vitals.Current, 0.0001f, "A rejected spawn must not refill vitals.");
        }

        [Test]
        public void Spawn_WithoutDefinition_ReturnsMissingDefinition()
        {
            Actor orphan = TestActors.Create(null, "Orphan");

            try
            {
                Assert.AreEqual(SpawnResult.MissingDefinition, orphan.Spawn());
                Assert.AreEqual(PawnState.Unspawned, orphan.State, "A failed spawn leaves the actor untouched.");
            }
            finally
            {
                Object.DestroyImmediate(orphan.gameObject);
            }
        }

        [Test]
        public void Spawn_CopiesTagsFromTheDefinition()
        {
            ActorDefinition tagged = new ActorDefinitionBuilder()
                .WithId("explosive-crate")
                .WithTags("Class.Explosive", "Material.Wood")
                .Build();

            Actor explosive = TestActors.Create(tagged, "ExplosiveCrate");

            try
            {
                explosive.Spawn();

                Assert.IsTrue(explosive.Tags.Has("Class"), "Tags are seeded from the definition and match hierarchically.");
                Assert.IsTrue(explosive.Tags.Has("Material.Wood"));
            }
            finally
            {
                Object.DestroyImmediate(explosive.gameObject);
            }
        }

        [Test]
        public void Spawn_RaisesSpawnedOnce()
        {
            int raised = 0;
            actor.Spawned += _ => raised++;

            actor.Spawn();
            actor.Spawn();

            Assert.AreEqual(1, raised, "The rejected second spawn must not raise the event again.");
        }

        [Test]
        public void Despawn_TakesTheActorOutOfPlayAndDeactivatesTheGameObject()
        {
            actor.Spawn();

            actor.Despawn();

            Assert.AreEqual(PawnState.Despawned, actor.State);
            Assert.IsFalse(actor.gameObject.activeSelf, "The definition asks for deactivation on despawn by default.");
        }

        [Test]
        public void Despawn_WhenAlreadyDespawned_RaisesNothing()
        {
            actor.Spawn();
            actor.Despawn();

            int raised = 0;
            actor.Despawned += _ => raised++;
            actor.Despawn();

            Assert.AreEqual(0, raised, "Despawning twice is a no-op.");
        }

        [Test]
        public void Spawn_AfterDespawn_BringsTheActorBack()
        {
            actor.Spawn();
            actor.ApplyDamage(new Vitals.DamageInfo(30f));
            actor.Despawn();

            SpawnResult result = actor.Spawn();

            Assert.AreEqual(SpawnResult.Success, result, "A despawned actor can be pooled and spawned again.");
            Assert.AreEqual(PawnState.Alive, actor.State);
            Assert.AreEqual(100f, actor.Vitals.Current, 0.0001f, "Respawning restores full vitals.");
            Assert.IsTrue(actor.gameObject.activeSelf);
        }

        [Test]
        public void Tick_CountsDownTheSpawnGracePeriod()
        {
            ActorDefinition guarded = new ActorDefinitionBuilder().WithId("guarded").WithSpawnInvulnerability(2f).Build();
            Actor protectedActor = TestActors.Create(guarded, "Guarded");

            try
            {
                protectedActor.Spawn();
                Assert.IsTrue(protectedActor.IsInvulnerable, "An actor is protected immediately after spawning.");

                protectedActor.Tick(1.5f);
                Assert.IsTrue(protectedActor.IsInvulnerable, "Half the grace period has not elapsed yet.");

                protectedActor.Tick(1f);
                Assert.IsFalse(protectedActor.IsInvulnerable, "The grace period ends once its duration has been ticked away.");
            }
            finally
            {
                Object.DestroyImmediate(protectedActor.gameObject);
            }
        }
    }
}
