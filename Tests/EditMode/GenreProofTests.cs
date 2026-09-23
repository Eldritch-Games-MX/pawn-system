using System.Collections.Generic;
using EldritchGames.PawnSystem.Identity;
using EldritchGames.PawnSystem.Lifecycle;
using EldritchGames.PawnSystem.TestUtilities;
using EldritchGames.PawnSystem.Vitals;
using NUnit.Framework;
using UnityEngine;

namespace EldritchGames.PawnSystem.Tests.EditMode
{
    /// <summary>
    /// The same pawn, driven by two clocks that have nothing in common: a turn-based round counter
    /// and a real-time frame clock. If both of these pass, the core is genre-agnostic in the only
    /// sense that matters.
    /// </summary>
    public class GenreProofTests
    {
        private readonly List<GameObject> created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject gameObject in created)
                if (gameObject != null)
                    Object.DestroyImmediate(gameObject);

            created.Clear();
        }

        [Test]
        public void TurnBased_APawnSurvivesAFullRoundOfCombat()
        {
            TeamDefinition party = TestTeams.Create("party");
            TeamDefinition foes = TestTeams.Create("foes", hostileTeams: new[] { party });

            Pawn knight = Spawn("knight", party, maxVital: 30f, invulnerability: 1f);
            Pawn goblin = Spawn("goblin", foes, maxVital: 8f);

            var brain = new FakePawnPossessor();
            goblin.TryPossess(brain);

            // Round 1: the knight's entry protection is still up, so the goblin's attack does nothing.
            knight.ApplyDamage(new DamageInfo(5f, instigator: goblin));
            Assert.AreEqual(30f, knight.Vitals.Current, 0.0001f, "The spawn grace period is measured in ticks, not seconds.");

            knight.Tick(1f);
            goblin.Tick(1f);

            // Round 2: protection has expired and both sides connect.
            knight.ApplyDamage(new DamageInfo(5f, instigator: goblin));
            goblin.ApplyDamage(new DamageInfo(8f, instigator: knight));

            Assert.AreEqual(25f, knight.Vitals.Current, 0.0001f);
            Assert.AreEqual(PawnState.Dead, goblin.State);
            Assert.AreEqual(1, brain.ReleasedCount, "The goblin's brain is handed its body back when the goblin dies.");
            Assert.IsTrue(goblin.IsHostileTo(knight));
        }

        [Test]
        public void RealTime_APawnDiesAndRespawnsOnAFrameClock()
        {
            Pawn prefab = TestPawns.Create(
                new PawnDefinitionBuilder().WithId("grunt").WithMaxVital(20f).Build(), "GruntPrefab");
            created.Add(prefab.gameObject);

            var host = new GameObject("Spawner");
            created.Add(host);
            PawnSpawner spawner = host.AddComponent<PawnSpawner>();
            spawner.Prefab = prefab;
            spawner.RespawnPolicy = new DelayedRespawnPolicy(delay: 0.5f);

            Pawn grunt = spawner.Spawn(Vector3.zero, Quaternion.identity);
            created.Add(grunt.gameObject);

            var player = new FakePawnPossessor();
            grunt.TryPossess(player);

            const float frame = 1f / 60f;
            for (int i = 0; i < 10; i++)
            {
                grunt.ApplyDamage(new DamageInfo(3f));
                grunt.Tick(frame);
                spawner.Tick(frame);
            }

            Assert.AreEqual(PawnState.Dead, grunt.State, "Twenty vitals do not survive thirty damage.");

            for (int i = 0; i < 60; i++) spawner.Tick(frame);

            Assert.AreEqual(PawnState.Alive, grunt.State, "The respawn delay elapsed on the same frame clock.");
            Assert.AreEqual(20f, grunt.Vitals.Current, 0.0001f);
            Assert.AreEqual(PossessionResult.Success, grunt.TryPossess(player),
                "The player can take the respawned body again — possession is not tied to the old life.");
        }

        [Test]
        public void ThePawnItselfNeverLearnsWhichGenreItIsIn()
        {
            Pawn pawn = Spawn("subject", null, maxVital: 10f);

            Assert.IsFalse(pawn.TryGet<IPawnPossessor>(out _),
                "A pawn exposes only capabilities composed onto it — it has no built-in knowledge of who drives it.");
            Assert.IsNull(pawn.Motor, "Movement is optional; a turn-based portrait is a pawn with no motor at all.");
        }

        private Pawn Spawn(string id, TeamDefinition team, float maxVital, float invulnerability = 0f)
        {
            PawnDefinition definition = new PawnDefinitionBuilder()
                .WithId(id)
                .WithTeam(team)
                .WithMaxVital(maxVital)
                .WithSpawnInvulnerability(invulnerability)
                .Build();

            Pawn pawn = TestPawns.Create(definition, id);
            created.Add(pawn.gameObject);
            pawn.Spawn();
            return pawn;
        }
    }
}
