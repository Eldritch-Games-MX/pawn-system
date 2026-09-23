using System.Collections;
using EldritchGames.PawnSystem.Identity;
using EldritchGames.PawnSystem.Lifecycle;
using EldritchGames.PawnSystem.TestUtilities;
using EldritchGames.PawnSystem.Vitals;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EldritchGames.PawnSystem.Tests.PlayMode
{
    public class PawnLifecyclePlayModeTests
    {
        private const float FixedStep = 0.05f;

        private GameObject host;
        private Pawn prefab;

        [SetUp]
        public void SetUp()
        {
            PawnDefinition definition = new PawnDefinitionBuilder().WithId("grunt").WithMaxVital(20f).Build();
            prefab = TestPawns.Create(definition, "GruntPrefab");
            prefab.gameObject.SetActive(false);

            host = new GameObject("Spawner");
        }

        [TearDown]
        public void TearDown()
        {
            if (prefab != null) Object.Destroy(prefab.gameObject);
            if (host != null) Object.Destroy(host);
        }

        [UnityTest]
        public IEnumerator ScenePlacedPawn_SpawnsItselfOnStart()
        {
            var placed = new GameObject("Placed");
            Pawn pawn = placed.AddComponent<Pawn>();
            typeof(Pawn)
                .GetField("definition", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(pawn, new PawnDefinitionBuilder().WithId("placed").WithMaxVital(5f).Build());

            yield return null;

            try
            {
                Assert.AreEqual(PawnState.Alive, pawn.State,
                    "A pawn dropped into a scene is in play without anyone having to spawn it.");
            }
            finally
            {
                Object.Destroy(placed);
            }
        }

        [UnityTest]
        public IEnumerator PossessMoveDieRespawnRepossess_WorksEndToEnd()
        {
            PawnSpawner spawner = host.AddComponent<PawnSpawner>();
            spawner.Prefab = prefab;
            spawner.RespawnPolicy = new DelayedRespawnPolicy(delay: 0.2f);

            Pawn pawn = spawner.Spawn(new Vector3(0f, 0f, 0f), Quaternion.identity);
            Assert.IsNotNull(pawn, "The spawner produced a pawn.");

            var motor = new FakePawnMotor();
            pawn.SetMotor(motor);

            var possessor = new FakePawnPossessor();
            Assert.AreEqual(PossessionResult.Success, pawn.TryPossess(possessor));

            yield return null;

            pawn.Motor.Move(Vector3.forward * 2f, FixedStep);
            Assert.AreEqual(1, motor.MoveCallCount, "The possessed pawn is being driven.");

            pawn.ApplyDamage(new DamageInfo(25f));
            Assert.AreEqual(PawnState.Dead, pawn.State);
            Assert.AreEqual(1, possessor.ReleasedCount, "Death hands the body back.");

            for (int i = 0; i < 20 && pawn.State != PawnState.Alive; i++)
            {
                spawner.Tick(FixedStep);
                yield return null;
            }

            Assert.AreEqual(PawnState.Alive, pawn.State, "The respawn delay elapsed on the frame clock.");
            Assert.AreEqual(20f, pawn.Vitals.Current, 0.0001f);
            Assert.AreEqual(PossessionResult.Success, pawn.TryPossess(possessor), "The player takes the new body.");

            spawner.DespawnAll();
        }

        [UnityTest]
        public IEnumerator DestroyingAPossessedPawn_ReleasesThePossessor()
        {
            Pawn pawn = TestPawns.CreateSpawned(new PawnDefinitionBuilder().WithId("doomed").Build(), "Doomed");
            var possessor = new FakePawnPossessor();
            pawn.TryPossess(possessor);

            Object.DestroyImmediate(pawn.gameObject);
            yield return null;

            Assert.AreEqual(1, possessor.ReleasedCount,
                "A destroyed pawn must not leave a controller holding a reference to nothing.");
        }
    }
}
