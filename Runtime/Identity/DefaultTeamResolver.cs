namespace EldritchGames.PawnSystem.Identity
{
    /// <summary>
    /// The built-in <see cref="ITeamResolver"/>: same team is friendly, anything listed on the
    /// subject's <see cref="TeamDefinition"/> is friendly or hostile as authored, everything else
    /// is neutral.
    /// </summary>
    /// <remarks>
    /// Resolution order, first match wins:
    /// <list type="number">
    /// <item><description>Either pawn is <c>null</c> or has no team → <see cref="PawnRelationship.Neutral"/>.</description></item>
    /// <item><description>Same <see cref="TeamDefinition"/> asset → <see cref="PawnRelationship.Friendly"/>.</description></item>
    /// <item><description>Listed in <see cref="TeamDefinition.HostileTeams"/> → <see cref="PawnRelationship.Hostile"/>.</description></item>
    /// <item><description>Listed in <see cref="TeamDefinition.FriendlyTeams"/> → <see cref="PawnRelationship.Friendly"/>.</description></item>
    /// <item><description>Otherwise → <see cref="PawnRelationship.Neutral"/>.</description></item>
    /// </list>
    /// A pawn always regards itself as friendly, since its own team matches.
    /// </remarks>
    public sealed class DefaultTeamResolver : ITeamResolver
    {
        /// <inheritdoc/>
        public PawnRelationship Resolve(Pawn subject, Pawn other)
        {
            if (subject == null || other == null) return PawnRelationship.Neutral;

            TeamDefinition subjectTeam = subject.Team;
            TeamDefinition otherTeam = other.Team;
            if (subjectTeam == null || otherTeam == null) return PawnRelationship.Neutral;
            if (subjectTeam == otherTeam) return PawnRelationship.Friendly;

            if (Contains(subjectTeam.HostileTeams, otherTeam)) return PawnRelationship.Hostile;
            if (Contains(subjectTeam.FriendlyTeams, otherTeam)) return PawnRelationship.Friendly;

            return PawnRelationship.Neutral;
        }

        private static bool Contains(System.Collections.Generic.IReadOnlyList<TeamDefinition> teams, TeamDefinition team)
        {
            if (teams == null) return false;
            for (int i = 0; i < teams.Count; i++)
                if (teams[i] == team)
                    return true;
            return false;
        }
    }
}
