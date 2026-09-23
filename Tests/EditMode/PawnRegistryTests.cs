using System.Collections.Generic;
using EldritchGames.PawnSystem.Identity;
using EldritchGames.PawnSystem.Lifecycle;
using EldritchGames.PawnSystem.TestUtilities;
using NUnit.Framework;
using UnityEngine;

namespace EldritchGames.PawnSystem.Tests.EditMode
{
    public class PawnRegistryTests
    {
        private PawnRegistry registry;
        private TeamDefinition players;
        private TeamDefinition monsters;
        private readonly List<Pawn> created = new List<Pawn>();

        [SetUp]
        public void SetUp()
        {
            registry = new PawnRegistry();
            players = TestTeams.Create("players");
            monsters = TestTeams.Create("monsters");
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Pawn pawn in created)
                if (pawn != null)
                    Object.DestroyImmediate(pawn.gameObject);

            created.Clear();
        }

        [Test]
        public void Register_AddsThePawnOnceAndReportsIt()
        {
            Pawn pawn = Spawn("hero", players);

            int raised = 0;
            registry.Registered += _ => raised++;

            Assert.IsTrue(registry.Register(pawn));
            Assert.IsFalse(registry.Register(pawn), "A pawn is only registered once.");
            Assert.AreEqual(1, registry.Count);
            Assert.AreEqual(1, raised);
        }

        [Test]
        public void DespawningAPawn_UnregistersItAutomatically()
        {
            Pawn pawn = Spawn("hero", players);
            registry.Register(pawn);

            pawn.Despawn();

            Assert.AreEqual(0, registry.Count, "A pawn that has left the world removes itself from the registry.");
        }

        [Test]
        public void DeadPawns_StayRegistered()
        {
            Pawn pawn = Spawn("hero", players);
            registry.Register(pawn);

            pawn.Kill();

            Assert.AreEqual(1, registry.Count, "Corpses are still in the world — looting and revival need to find them.");
        }

        [Test]
        public void Query_FiltersByTeamTagAndState()
        {
            Pawn hero = Spawn("hero", players, "Class.Knight");
            Pawn rogue = Spawn("rogue", players, "Class.Rogue");
            Pawn rat = Spawn("rat", monsters, "Beast");
            registry.Register(hero);
            registry.Register(rogue);
            registry.Register(rat);
            rogue.Kill();

            var results = new List<Pawn>();

            Assert.AreEqual(2, registry.Query(results, team: players), "Both party members are on the players team.");
            Assert.AreEqual(1, registry.Query(results, team: players, state: PawnState.Alive), "Only one of them is still standing.");
            Assert.AreEqual(2, registry.Query(results, tag: "Class"), "The parent tag matches both classed pawns.");
            Assert.AreEqual(1, registry.Query(results, tag: "Beast"));
        }

        [Test]
        public void Query_ClearsTheCallersBufferBeforeFillingIt()
        {
            Pawn hero = Spawn("hero", players);
            registry.Register(hero);

            var results = new List<Pawn> { null, null };
            registry.Query(results);

            Assert.AreEqual(1, results.Count, "The buffer is reused, not appended to.");
        }

        [Test]
        public void FindNearest_ReturnsTheClosestMatch()
        {
            Pawn near = Spawn("near", monsters);
            Pawn far = Spawn("far", monsters);
            near.transform.position = new Vector3(2f, 0f, 0f);
            far.transform.position = new Vector3(20f, 0f, 0f);
            registry.Register(near);
            registry.Register(far);

            Assert.AreSame(near, registry.FindNearest(Vector3.zero));
        }

        [Test]
        public void FindNearest_RespectsTheFilter()
        {
            Pawn ally = Spawn("ally", players);
            Pawn enemy = Spawn("enemy", monsters);
            ally.transform.position = new Vector3(1f, 0f, 0f);
            enemy.transform.position = new Vector3(10f, 0f, 0f);
            registry.Register(ally);
            registry.Register(enemy);

            Pawn found = registry.FindNearest(Vector3.zero, p => p.Team == monsters);

            Assert.AreSame(enemy, found, "The nearest pawn the filter accepts, not the nearest pawn overall.");
        }

        [Test]
        public void FindNearest_WithNothingRegistered_ReturnsNull()
        {
            Assert.IsNull(registry.FindNearest(Vector3.zero));
        }

        private Pawn Spawn(string id, TeamDefinition team, params PawnTag[] tags)
        {
            PawnDefinition definition = new PawnDefinitionBuilder().WithId(id).WithTeam(team).WithTags(tags).Build();
            Pawn pawn = TestPawns.Create(definition, id);
            pawn.Spawn();
            created.Add(pawn);
            return pawn;
        }
    }
}
