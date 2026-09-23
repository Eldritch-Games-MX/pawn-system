using EldritchGames.PawnSystem.Identity;
using EldritchGames.PawnSystem.TestUtilities;
using NUnit.Framework;
using UnityEngine;

namespace EldritchGames.PawnSystem.Tests.EditMode
{
    public class TeamRelationshipTests
    {
        private TeamDefinition players;
        private TeamDefinition monsters;
        private TeamDefinition townsfolk;
        private Pawn hero;
        private Pawn rat;
        private Pawn baker;

        [SetUp]
        public void SetUp()
        {
            players = TestTeams.Create("players");
            monsters = TestTeams.Create("monsters", hostileTeams: new[] { players });
            townsfolk = TestTeams.Create("townsfolk", friendlyTeams: new[] { players });

            hero = TestPawns.CreateSpawned(new PawnDefinitionBuilder().WithId("hero").WithTeam(players).Build(), "Hero");
            rat = TestPawns.CreateSpawned(new PawnDefinitionBuilder().WithId("rat").WithTeam(monsters).Build(), "Rat");
            baker = TestPawns.CreateSpawned(new PawnDefinitionBuilder().WithId("baker").WithTeam(townsfolk).Build(), "Baker");
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Pawn pawn in new[] { hero, rat, baker })
                if (pawn != null)
                    Object.DestroyImmediate(pawn.gameObject);
        }

        [Test]
        public void SameTeam_IsFriendly()
        {
            Assert.AreEqual(PawnRelationship.Friendly, hero.RelationshipTo(hero), "A pawn is always on its own side.");
        }

        [Test]
        public void ListedHostileTeam_IsHostile()
        {
            Assert.IsTrue(rat.IsHostileTo(hero));
        }

        [Test]
        public void ListedFriendlyTeam_IsFriendly()
        {
            Assert.IsTrue(baker.IsFriendlyTo(hero));
        }

        [Test]
        public void UnlistedTeam_IsNeutral()
        {
            Assert.AreEqual(PawnRelationship.Neutral, hero.RelationshipTo(rat),
                "Relationships are authored per team and are not forced to be symmetric — the hero has no opinion about rats.");
        }

        [Test]
        public void PawnWithoutATeam_IsNeutralToEveryone()
        {
            Pawn stray = TestPawns.CreateSpawned(new PawnDefinitionBuilder().WithId("stray").Build(), "Stray");

            try
            {
                Assert.AreEqual(PawnRelationship.Neutral, stray.RelationshipTo(hero));
                Assert.AreEqual(PawnRelationship.Neutral, hero.RelationshipTo(stray));
            }
            finally
            {
                Object.DestroyImmediate(stray.gameObject);
            }
        }

        [Test]
        public void ChangingTeamAtRuntime_ChangesTheRelationship()
        {
            rat.Team = players;

            Assert.AreEqual(PawnRelationship.Friendly, rat.RelationshipTo(hero),
                "Mind control and defections are a plain team assignment.");
        }

        [Test]
        public void CustomResolver_OverridesTheDefault()
        {
            hero.TeamResolver = new DeathmatchResolver();

            Assert.IsTrue(hero.IsHostileTo(baker), "A game with its own rules replaces the resolver, not the pawn.");
        }

        private sealed class DeathmatchResolver : ITeamResolver
        {
            public PawnRelationship Resolve(Pawn subject, Pawn other) =>
                subject == other ? PawnRelationship.Friendly : PawnRelationship.Hostile;
        }
    }
}
