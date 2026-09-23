using System.Collections.Generic;
using System.Reflection;
using EldritchGames.PawnSystem.Identity;
using UnityEngine;

namespace EldritchGames.PawnSystem.TestUtilities
{
    /// <summary>
    /// Creates <see cref="TeamDefinition"/> assets in memory, with their relationship lists filled in.
    /// </summary>
    /// <remarks>
    /// <code>
    /// TeamDefinition players = TestTeams.Create("players");
    /// TeamDefinition monsters = TestTeams.Create("monsters", hostileTeams: new[] { players });
    /// </code>
    /// Relationships are authored per team and are not forced to be symmetric, so a test can set up
    /// a one-sided grudge by listing the team on only one side.
    /// </remarks>
    public static class TestTeams
    {
        /// <summary>Creates a team.</summary>
        /// <param name="id">The save-data identifier, also used as the display name.</param>
        /// <param name="hostileTeams">Teams this one treats as enemies.</param>
        /// <param name="friendlyTeams">Teams this one treats as allies, beyond itself.</param>
        /// <returns>The in-memory team asset.</returns>
        public static TeamDefinition Create(
            string id,
            IEnumerable<TeamDefinition> hostileTeams = null,
            IEnumerable<TeamDefinition> friendlyTeams = null)
        {
            var team = ScriptableObject.CreateInstance<TeamDefinition>();
            SetField(team, "id", id);
            SetField(team, "displayName", id);

            if (hostileTeams != null) SetField(team, "hostileTeams", new List<TeamDefinition>(hostileTeams));
            if (friendlyTeams != null) SetField(team, "friendlyTeams", new List<TeamDefinition>(friendlyTeams));

            return team;
        }

        private static void SetField(TeamDefinition team, string fieldName, object value)
        {
            FieldInfo field = typeof(TeamDefinition).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(team, value);
        }
    }
}
