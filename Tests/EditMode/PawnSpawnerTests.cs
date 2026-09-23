using System.Collections.Generic;
using EldritchGames.PawnSystem.Identity;
using EldritchGames.PawnSystem.Lifecycle;
using EldritchGames.PawnSystem.TestUtilities;
using NUnit.Framework;
using UnityEngine;

namespace EldritchGames.PawnSystem.Tests.EditMode
{
    public class PawnSpawnerTests
    {
        private PawnSpawner spawner;
        private Pawn prefab;
        private readonly List<GameObject> created = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            PawnDefinition definition = new PawnDefinitionBuilder().WithId("grunt").WithMaxVital(10f).Build();
            prefab = TestPawns.Create(definition, "GruntPrefab");
            created.Add(prefab.gameObject);

            var host = new GameObject("Spawner");
            created.Add(host);
            spawner = host.AddComponent<PawnSpawner>();
            spawner.Prefab = prefab;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Pawn pawn in new List<Pawn>(spawner.Registry.All))
                if (pawn != null)
                    Object.DestroyImmediate(pawn.gameObject);

            foreach (GameObject gameObject in created)
                if (gameObject != null)
                    Object.DestroyImmediate(gameObject);

            created.Clear();
        }

        [Test]
        public void Spawn_ProducesALivingRegisteredPawn()
        {
            Pawn pawn = spawner.Spawn(new Vector3(5f, 0f, 0f), Quaternion.identity);

            Assert.IsNotNull(pawn);
            Assert.AreEqual(PawnState.Alive, pawn.State);
            Assert.AreEqual(new Vector3(5f, 0f, 0f), pawn.transform.position);
            Assert.AreEqual(1, spawner.Registry.Count, "Spawned pawns are tracked so the game can query them.");
        }

        [Test]
        public void Spawn_WithoutASpawnPoint_DeclinesRatherThanPilingPawnsAtTheOrigin()
        {
            Assert.IsNull(spawner.Spawn(), "The default provider has no points, so there is nowhere to put the pawn.");
            Assert.AreEqual(0, spawner.Registry.Count);
        }

        [Test]
        public void Spawn_UsesTheSpawnPointProvider()
        {
            var point = new GameObject("Point");
            point.transform.position = new Vector3(0f, 0f, 12f);
            created.Add(point);

            spawner.SpawnPointProvider = new TransformSpawnPointProvider(new[] { point.transform });

            Pawn pawn = spawner.Spawn();

            Assert.IsNotNull(pawn);
            Assert.AreEqual(new Vector3(0f, 0f, 12f), pawn.transform.position);
        }

        [Test]
        public void Despawn_TakesThePawnOutOfPlayAndOutOfTheRegistry()
        {
            Pawn pawn = spawner.Spawn(Vector3.zero, Quaternion.identity);

            spawner.Despawn(pawn);

            Assert.AreEqual(0, spawner.Registry.Count);
        }

        [Test]
        public void Death_SchedulesARespawnAccordingToThePolicy()
        {
            var policy = new FakeRespawnPolicy(shouldRespawn: true, delay: 2f);
            spawner.RespawnPolicy = policy;

            Pawn pawn = spawner.Spawn(Vector3.zero, Quaternion.identity);
            pawn.Kill();

            Assert.AreEqual(1, spawner.PendingRespawnCount, "A dead pawn is queued to come back.");
            Assert.AreEqual(1, policy.ShouldRespawnCallCount, "The policy is asked once per death.");
        }

        [Test]
        public void Death_WhenThePolicyRefuses_QueuesNothing()
        {
            spawner.RespawnPolicy = new FakeRespawnPolicy(shouldRespawn: false);

            Pawn pawn = spawner.Spawn(Vector3.zero, Quaternion.identity);
            pawn.Kill();

            Assert.AreEqual(0, spawner.PendingRespawnCount, "A hardcore mode or a spent life leaves the pawn dead.");
            Assert.AreEqual(PawnState.Dead, pawn.State);
        }

        [Test]
        public void Tick_BringsThePawnBackOnceTheDelayHasElapsed()
        {
            spawner.RespawnPolicy = new FakeRespawnPolicy(shouldRespawn: true, delay: 2f);
            spawner.SpawnPointProvider = new FixedSpawnPointProvider(new Vector3(0f, 0f, 7f));

            Pawn pawn = spawner.Spawn(Vector3.zero, Quaternion.identity);
            pawn.Kill();

            spawner.Tick(1f);
            Assert.AreEqual(PawnState.Dead, pawn.State, "Half the delay is not enough.");

            spawner.Tick(1.5f);

            Assert.AreEqual(PawnState.Alive, pawn.State, "The pawn returns once its wait has been ticked away.");
            Assert.AreEqual(10f, pawn.Vitals.Current, 0.0001f, "It comes back at full vitals, like any fresh spawn.");
            Assert.AreEqual(new Vector3(0f, 0f, 7f), pawn.transform.position, "It returns at a point the provider chose.");
            Assert.AreEqual(0, spawner.PendingRespawnCount);
        }

        [Test]
        public void DelayedRespawnPolicy_StopsGrantingRespawnsOnceLivesRunOut()
        {
            var policy = new DelayedRespawnPolicy(delay: 0f, maxRespawns: 2);
            Pawn pawn = spawner.Spawn(Vector3.zero, Quaternion.identity);

            Assert.IsTrue(policy.ShouldRespawn(pawn));
            Assert.IsTrue(policy.ShouldRespawn(pawn));
            Assert.IsFalse(policy.ShouldRespawn(pawn), "A policy capped at two respawns refuses the third death.");
            Assert.AreEqual(0, policy.Remaining);
        }

        [Test]
        public void DespawnAll_ClearsEveryLivePawnAndPendingRespawn()
        {
            spawner.RespawnPolicy = new FakeRespawnPolicy(shouldRespawn: true, delay: 5f);
            Pawn first = spawner.Spawn(Vector3.zero, Quaternion.identity);
            spawner.Spawn(Vector3.one, Quaternion.identity);
            first.Kill();

            spawner.DespawnAll();

            Assert.AreEqual(0, spawner.Registry.Count);
            Assert.AreEqual(0, spawner.PendingRespawnCount);
        }

        [Test]
        public void Pool_ReusesInstancesInsteadOfInstantiating()
        {
            var pool = new SimplePawnPool();
            spawner.Pool = pool;

            Pawn first = spawner.Spawn(Vector3.zero, Quaternion.identity);
            spawner.Despawn(first);

            Assert.AreEqual(1, pool.IdleCount, "A despawned pawn goes back to the pool rather than being destroyed.");

            Pawn second = spawner.Spawn(Vector3.zero, Quaternion.identity);

            Assert.AreSame(first, second, "The pool hands the same instance back out.");
            Assert.AreEqual(PawnState.Alive, second.State, "A reused pawn is spawned again, not resurrected half-way.");
            created.Add(second.gameObject);
        }

        private sealed class FixedSpawnPointProvider : ISpawnPointProvider
        {
            private readonly Vector3 point;

            public FixedSpawnPointProvider(Vector3 point)
            {
                this.point = point;
            }

            public bool TryGetSpawnPoint(Pawn prefab, out Vector3 position, out Quaternion rotation)
            {
                position = point;
                rotation = Quaternion.identity;
                return true;
            }
        }
    }
}
