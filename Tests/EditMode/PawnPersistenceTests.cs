using EldritchGames.PawnSystem.Identity;
using EldritchGames.PawnSystem.Persistence;
using EldritchGames.PawnSystem.TestUtilities;
using EldritchGames.PawnSystem.Vitals;
using NUnit.Framework;
using UnityEngine;

namespace EldritchGames.PawnSystem.Tests.EditMode
{
    public class PawnPersistenceTests
    {
        private PawnDefinition definition;
        private TeamDefinition players;
        private TeamDefinition monsters;
        private Pawn pawn;

        [SetUp]
        public void SetUp()
        {
            players = TestTeams.Create("players");
            monsters = TestTeams.Create("monsters");
            definition = new PawnDefinitionBuilder().WithId("knight").WithMaxVital(100f).WithTeam(players).Build();
            pawn = TestPawns.Create(definition);
        }

        [TearDown]
        public void TearDown()
        {
            if (pawn != null) Object.DestroyImmediate(pawn.gameObject);
        }

        [Test]
        public void CaptureState_RecordsIdentityStateVitalsAndPose()
        {
            pawn.Spawn(new Vector3(1f, 2f, 3f), Quaternion.identity);
            pawn.ApplyDamage(new DamageInfo(40f));
            pawn.Tags.Add("Status.Cursed");

            PawnSaveData data = pawn.CaptureState();

            Assert.AreEqual("knight", data.pawnId);
            Assert.AreEqual((int)PawnState.Alive, data.state);
            Assert.AreEqual(60f, data.vitalCurrent, 0.0001f);
            Assert.AreEqual(100f, data.vitalMax, 0.0001f);
            Assert.AreEqual("players", data.teamId);
            Assert.AreEqual(new Vector3(1f, 2f, 3f), data.position);
            CollectionAssert.Contains(data.tags, "Status.Cursed");
        }

        [Test]
        public void RestoreState_PutsALivingPawnBackWhereAndHowItWas()
        {
            pawn.Spawn(new Vector3(4f, 0f, 5f), Quaternion.identity);
            pawn.ApplyDamage(new DamageInfo(25f));
            pawn.Tags.Add("Quest.Escort");
            PawnSaveData data = pawn.CaptureState();

            Pawn loaded = TestPawns.Create(definition, "Loaded");
            try
            {
                loaded.RestoreState(data, new TestTeamLookup(players, monsters));

                Assert.AreEqual(PawnState.Alive, loaded.State);
                Assert.AreEqual(75f, loaded.Vitals.Current, 0.0001f);
                Assert.AreEqual(new Vector3(4f, 0f, 5f), loaded.transform.position);
                Assert.IsTrue(loaded.Tags.Has("Quest.Escort"));
                Assert.AreSame(players, loaded.Team);
            }
            finally
            {
                Object.DestroyImmediate(loaded.gameObject);
            }
        }

        [Test]
        public void RestoreState_RestoresADeadPawnAsDead()
        {
            pawn.Spawn();
            pawn.Kill();
            PawnSaveData data = pawn.CaptureState();

            Pawn loaded = TestPawns.Create(definition, "Loaded");
            try
            {
                loaded.RestoreState(data);

                Assert.AreEqual(PawnState.Dead, loaded.State, "A save made after a wipe loads back as a wipe.");
            }
            finally
            {
                Object.DestroyImmediate(loaded.gameObject);
            }
        }

        [Test]
        public void RestoreState_RestoresADespawnedPawnAsDespawned()
        {
            pawn.Spawn();
            pawn.Despawn();
            PawnSaveData data = pawn.CaptureState();

            Pawn loaded = TestPawns.Create(definition, "Loaded");
            try
            {
                loaded.Spawn();
                loaded.RestoreState(data);

                Assert.AreEqual(PawnState.Despawned, loaded.State);
            }
            finally
            {
                Object.DestroyImmediate(loaded.gameObject);
            }
        }

        [Test]
        public void RestoreState_RestoresAChangedTeam()
        {
            pawn.Spawn();
            pawn.Team = monsters;
            PawnSaveData data = pawn.CaptureState();

            Pawn loaded = TestPawns.Create(definition, "Loaded");
            try
            {
                loaded.RestoreState(data, new TestTeamLookup(players, monsters));

                Assert.AreSame(monsters, loaded.Team, "A defection survives a save, rather than reverting to the definition.");
            }
            finally
            {
                Object.DestroyImmediate(loaded.gameObject);
            }
        }

        [Test]
        public void RestoreState_WithAnUnknownTeamId_LeavesTheDefaultTeamInPlace()
        {
            pawn.Spawn();
            PawnSaveData data = pawn.CaptureState();
            data.teamId = "renamed-since-the-save";

            Pawn loaded = TestPawns.Create(definition, "Loaded");
            try
            {
                loaded.RestoreState(data, new TestTeamLookup(monsters));

                Assert.AreSame(players, loaded.Team, "An unresolvable team must not fail the whole load.");
            }
            finally
            {
                Object.DestroyImmediate(loaded.gameObject);
            }
        }

        private sealed class TestTeamLookup : ITeamLookup
        {
            private readonly TeamDefinition[] teams;

            public TestTeamLookup(params TeamDefinition[] teams)
            {
                this.teams = teams;
            }

            public TeamDefinition FindTeam(string id)
            {
                foreach (TeamDefinition team in teams)
                    if (team.Id == id)
                        return team;

                return null;
            }
        }
    }
}
